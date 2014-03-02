using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Mubox.Model.Input;

namespace Mubox.Control.Input.Hooks
{
    public static class KeyboardInputHook
    {
        static KeyboardInputHook()
        {
            hookProc = new WinAPI.WindowHook.HookProc(KeyboardHook);
            hookProcPtr = Marshal.GetFunctionPointerForDelegate(hookProc);
        }

        private static WinAPI.WindowHook.HookProc hookProc = null;
        private static IntPtr hookProcPtr = IntPtr.Zero;

        public static UIntPtr KeyboardHook(int nCode, UIntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode == 0)
                {
                    var wm = (WinAPI.WM)wParam.ToUInt32();
                    Mubox.WinAPI.WindowHook.KBDLLHOOKSTRUCT keyboardHookStruct = (Mubox.WinAPI.WindowHook.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Mubox.WinAPI.WindowHook.KBDLLHOOKSTRUCT));
                    if (OnKeyboardInputReceived(wm, keyboardHookStruct))
                    {
                        return new UIntPtr(1);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            try
            {
                return Mubox.WinAPI.WindowHook.CallNextHookEx(hHook, nCode, wParam, lParam);
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            return new UIntPtr(1);
        }

        private static bool isStarted;
        private static IntPtr hHook = IntPtr.Zero;

        private static System.Threading.Thread keyboardHookCheckThread = null;

        private static object StartLock = new object();
        public static void Start()
        {
            lock (StartLock)
            {
                if (isStarted)
                    return;
                isStarted = true;

                if (keyboardHookCheckThread == null)
                {
                    keyboardHookCheckThread = new System.Threading.Thread((System.Threading.ThreadStart)KeyboardHookCheckProc);
                    keyboardHookCheckThread.IsBackground = true;
                    keyboardHookCheckThread.Priority = System.Threading.ThreadPriority.Lowest;
                    keyboardHookCheckThread.Start();
                }

                //                IntPtr nextHook = IntPtr.Zero // COMMENTED BY CODEIT.RIGHT;
                //IntPtr dwThreadId = Win32.Threads.GetCurrentThreadId();
                var modules = System.Reflection.Assembly.GetEntryAssembly().GetModules();
                IntPtr hModule = Marshal.GetHINSTANCE(modules[0]);
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate()
                {
                    hHook = WinAPI.WindowHook.SetWindowsHookEx(WinAPI.WindowHook.HookType.WH_KEYBOARD_LL, hookProcPtr, hModule, 0);
                    if (hHook == IntPtr.Zero)
                    {
                        // failed
                        isStarted = false;
                        ("KBHOOK: Hook Failed 0x" + Marshal.GetLastWin32Error().ToString("X")).Log();
                    }
                });
            }
        }

        private static void KeyboardHookCheckProc()
        {
            while (keyboardHookCheckThread != null)
            {
                try
                {
                    if (isStarted)
                    {
                        if (Stop())
                        {
                            Start();
                        }
                        else
                        {
                            ("KBHOOK Detected Stop").Log();
                            keyboardHookCheckThread = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
                finally
                {
                    System.Threading.Thread.Sleep(3000);
                }
            }
        }

        public static bool Stop()
        {
            lock (StartLock)
            {
                if (!isStarted)
                {
                    return false;
                }
                isStarted = false;

                if (hHook != IntPtr.Zero)
                {
                    Mubox.WinAPI.WindowHook.UnhookWindowsHookEx(hHook);
                    hHook = IntPtr.Zero;
                }

                return true;
            }
        }

        public static event EventHandler<KeyboardInput> KeyboardInputReceived;

        // TODO: initialize this to current key state on init to deal with rare/theoretical key state desync
        private static System.Collections.BitArray pressedKeys = new System.Collections.BitArray(256);
        private static object pressedKeysLock = new object();

        private static Performance KeyboardInputPerformance = Performance.CreatePerformance("_KeyboardInput");
        private static Performance KeyboardHandlerPerformance = Performance.CreatePerformance("_KeyboardHandler");

        private static bool OnKeyboardInputReceived(WinAPI.WM wParam, WinAPI.WindowHook.KBDLLHOOKSTRUCT hookStruct)
        {
            // ignore injected input
            if (hookStruct.flags.HasFlag(WinAPI.WindowHook.LLKHF.INJECTED))
            {
                return false;
            }

            // ignore "global desktop keys"
            Mubox.Configuration.KeySetting globalKeySetting = null;
            if (Mubox.Configuration.MuboxConfigSection.Default.Profiles.ActiveProfile.Keys.TryGetKeySetting((WinAPI.VK)hookStruct.vkCode, out globalKeySetting) && (globalKeySetting.SendToDesktop))
            {
                return false;
            }

            // update pressed keys
            if (!UpdatePressedKeys(hookStruct) && Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled && !Mubox.Configuration.MuboxConfigSection.Default.DisableRepeatKeyFiltering)
            {
                return true;
            }

            // count
            if (Performance.IsPerformanceEnabled)
            {
                KeyboardInputPerformance.Count(Convert.ToInt64(hookStruct.time));
            }

            // handle high-level
            if (KeyboardInputReceived != null)
            {
                KeyboardInput keyboardInputEventArgs = KeyboardInput.CreateFrom(wParam, hookStruct);
                {
                    Mubox.Configuration.KeySetting keySetting = globalKeySetting;
                    if (Mubox.Configuration.MuboxConfigSection.Default.Profiles.ActiveProfile != null)
                    {
                        Mubox.Configuration.ClientSettings activeClient = Mubox.Configuration.MuboxConfigSection.Default.Profiles.ActiveProfile.ActiveClient;
                        if (activeClient != null)
                        {
                            activeClient.Keys.TryGetKeySetting((WinAPI.VK)keyboardInputEventArgs.VK, out keySetting);
                        }
                        if (keySetting != null)
                        {
                            keyboardInputEventArgs.VK = keySetting.OutputKey;
                            keyboardInputEventArgs.CAS = keySetting.OutputModifiers;
                        }
                    }
                }
                OnKeyboardInputReceivedInternal(keyboardInputEventArgs);
                return keyboardInputEventArgs.Handled;
            }

            return false;
        }

        private static bool IsKeyPressed(WinAPI.VK vk)
        {
            return pressedKeys[(int)vk];
        }

        /// <summary>
        /// <para>Updates 'pressed keys', returns true if pressed keys was updated.</para>
        /// </summary>
        /// <param name="hookStruct"></param>
        /// <returns>True if key state has/is changed due to this event.</returns>
        private static bool UpdatePressedKeys(WinAPI.WindowHook.KBDLLHOOKSTRUCT hookStruct)
        {
            var result = false;
            if (WinAPI.WindowHook.LLKHF.UP != (hookStruct.flags & WinAPI.WindowHook.LLKHF.UP))
            {
                if (!IsKeyPressed(hookStruct.vkCode))
                {
                    result = true;
                    pressedKeys[(int)hookStruct.vkCode] = true;
                }
            }
            else
            {
                if (IsKeyPressed(hookStruct.vkCode))
                {
                    result = true;
                    pressedKeys[(int)hookStruct.vkCode] = false;
                }
            }
            return result;
        }

        private static void OnKeyboardInputReceivedInternal(KeyboardInput e)
        {
            try
            {
                if (e != null)
                {
                    KeyboardInputReceived(null, e);
                    if (Performance.IsPerformanceEnabled)
                    {
                        KeyboardHandlerPerformance.Count((long)(e.CreatedTime - DateTime.Now).TotalMilliseconds);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }
    }
}