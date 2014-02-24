using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Mubox.Model.Input;

namespace Mubox.Control.Input
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

        public static UIntPtr KeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode == 0)
                {
                    Mubox.WinAPI.WindowHook.KBDLLHOOKSTRUCT keyboardHookStruct = (Mubox.WinAPI.WindowHook.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Mubox.WinAPI.WindowHook.KBDLLHOOKSTRUCT));
                    if (OnKeyboardInputReceived((WinAPI.WM)wParam, keyboardHookStruct))
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

        private static byte[] pressedKeys = new byte[256];
        private static object pressedKeysLock = new object();

        private static Performance KeyboardInputPerformance = Performance.CreatePerformance("_KeyboardInput");
        private static Performance KeyboardHandlerPerformance = Performance.CreatePerformance("_KeyboardHandler");

        private static bool OnKeyboardInputReceived(WinAPI.WM wParam, WinAPI.WindowHook.KBDLLHOOKSTRUCT hookStruct)
        {
            // fix for 'key repeat' windows feature
            if (pressedKeys.Contains((byte)(hookStruct.vkCode & 0xFF)))
            {
                return false;
            }

            // and ignore "global desktop keys"
            Mubox.Configuration.KeySetting globalKeySetting = null;
            if (Mubox.Configuration.MuboxConfigSection.Default.Profiles.ActiveProfile.Keys.TryGetKeySetting((WinAPI.VK)hookStruct.vkCode, out globalKeySetting) && (globalKeySetting.SendToDesktop))
            {
                return false;
            }

            // filter repeated keys, we don't rebroadcast these
            if (IsRepeatKey(hookStruct) && Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled && !Mubox.Configuration.MuboxConfigSection.Default.DisableRepeatKeyFiltering)
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
                            keyboardInputEventArgs.VK = (uint)keySetting.OutputKey;
                            keyboardInputEventArgs.CAS = keySetting.OutputModifiers;
                        }
                    }
                }
                OnKeyboardInputReceivedInternal(keyboardInputEventArgs);
                return keyboardInputEventArgs.Handled;
            }

            return false;
        }

        private static bool IsRepeatKey(WinAPI.WindowHook.KBDLLHOOKSTRUCT hookStruct)
        {
            int vk = (int)(hookStruct.vkCode & 0xFF);
            lock (pressedKeysLock)
            {
                bool keyIsPressed = pressedKeys[vk] == 0x80;
                if (WinAPI.WindowHook.LLKHF.UP != (hookStruct.flags & WinAPI.WindowHook.LLKHF.UP))
                {
                    if (keyIsPressed)
                    {
                        return true;
                    }
                    else
                    {
                        pressedKeys[vk] = 0x80;
                    }
                }
                else
                {
                    if (!keyIsPressed)
                    {
                        return true;
                    }
                    else
                    {
                        pressedKeys[vk] = (byte)(WinAPI.IsToggled((WinAPI.VK)vk) ? 1 : 0);
                    }
                }
            }
            return false;
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