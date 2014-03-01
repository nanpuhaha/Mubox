using Mubox.Control.Network;
using Mubox.Model.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mubox.Control.Input
{
    /// <summary>
    /// <para>Manages Input State and Input Adapters for a single Client.</para>
    /// <para>Manages a thread attached to the client input queue.</para>
    /// </summary>
    public class InputQueue
    {
        public IntPtr ClientInputQueueId { get; private set; }

        public IntPtr ClientWindowHandle { get; private set; }

        public WinAPI.Windows.MK CurrentMK { get; private set; }

        // used to 'denormalize' mouseinput coordinates into clientrelative coordinates
        private WinAPI.Windows.RECT _clientWindowRect;
        private long _clientWindowRectExpiry = 0L;

        public WinAPI.Windows.RECT ClientWindowRect
        {
            get
            {
                if (_clientWindowRectExpiry <= DateTime.Now.Ticks)
                {
                    _clientWindowRectExpiry = DateTime.Now.AddSeconds(5).Ticks;
                    WinAPI.Windows.GetClientRect(ClientWindowHandle, out _clientWindowRect);
                }
                return _clientWindowRect;
            }
        }

        private ConcurrentQueue<InputBase> _inputQueue;
        private Thread _inputQueueThread;
        private bool _exitYet = false;

        public InputQueue()
        {
            _inputQueue = new ConcurrentQueue<InputBase>();
        }

        public void Attach(IntPtr clientInputQueueId, IntPtr clientWindowHandle)
        {
            Detach();
            ClientInputQueueId = clientInputQueueId;
            ClientWindowHandle = clientWindowHandle;
            _exitYet = false;
            _inputQueueThread = new Thread(InputThreadMain)
            {
                IsBackground = true,
                Name = "TIQ" + clientInputQueueId.ToInt32().ToString("X"),
                Priority = ThreadPriority.Normal,
            };
            _inputQueueThread.SetApartmentState(ApartmentState.MTA);
            _inputQueueThread.Start();
        }

        public void Detach()
        {
            if (_inputQueueThread != null)
            {
                _exitYet = true;
                _inputQueueThread.Join();
                _inputQueueThread = null;
            }
            ClientInputQueueId = IntPtr.Zero;
        }

        public void Enqueue(InputBase input)
        {
            _inputQueue.Enqueue(input);
        }

        private void InputThreadMain()
        {
            // calling into user32 to initialize thread: according to MSDN this will initialize a message queue for the thread which is necessary fro AttachThreadInput to work correctly
            WinAPI.Windows.GetActiveWindow();
            //
            IntPtr myInputQueueId = WinAPI.Threads.GetCurrentThreadId();
            bool attached = WinAPI.Windows.AttachThreadInput(myInputQueueId, ClientInputQueueId, true);
            if (!attached)
            {
                var err = Marshal.GetLastWin32Error();
                (string.Format("InputManager failed AttachThreadInput(true) err=0x{2:X} ({0:X8}) ({1:X8})", myInputQueueId, ClientInputQueueId, err)).LogCritical();
            }
            try
            {
                while (!_exitYet)
                {
                    Thread.Sleep(100);
                    if (_inputQueue.Count > 0)
                    {
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        var input = default(InputBase);
                        while (_inputQueue.TryDequeue(out input) && stopwatch.ElapsedMilliseconds < 20)
                        {
                            if (input is MouseInput)
                            {
                                Process(input as MouseInput);
                            }
                            else if (input is KeyboardInput)
                            {
                                Process(input as KeyboardInput);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.LogCritical();
            }
            finally
            {
                var detached = WinAPI.Windows.AttachThreadInput(myInputQueueId, ClientInputQueueId, false);
                if (!detached)
                {
                    var err = Marshal.GetLastWin32Error();
                    (string.Format("InputManager failed AttachThreadInput(false) err=0x{2:X} ({0:X8}) ({1:X8})", myInputQueueId, ClientInputQueueId, err)).LogCritical();
                }
            }
        }

        private void Process(MouseInput mouseInput)
        {
            // denormalize coordinates into client coordinates (we should always expect normalized coordinates, whether coordinates are absolute or relative)
            ushort x = (ushort)Math.Ceiling((double)(mouseInput.Point.X) * ((double)ClientWindowRect.Width / 65536.0));
            ushort y = (ushort)Math.Ceiling((double)(mouseInput.Point.Y) * ((double)ClientWindowRect.Height / 65536.0));

            var clientRelativeCoordinates = WinAPI.MACROS.MAKELPARAM(x, y);

            var wm = WinAPI.WM.USER;

            switch ((mouseInput.Flags | WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_ABSOLUTE) ^ WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_ABSOLUTE)
            {
                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_MOVE:
                    wm = WinAPI.WM.MOUSEMOVE;
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_LEFTDOWN:
                    wm = WinAPI.WM.LBUTTONDOWN;
                    CurrentMK |= WinAPI.Windows.MK.MK_LBUTTON;
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_LEFTUP:
                    wm = WinAPI.WM.LBUTTONUP;
                    CurrentMK = (CurrentMK | WinAPI.Windows.MK.MK_LBUTTON) ^ WinAPI.Windows.MK.MK_LBUTTON;
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_RIGHTDOWN:
                    wm = WinAPI.WM.RBUTTONDOWN;
                    CurrentMK |= WinAPI.Windows.MK.MK_RBUTTON;
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_RIGHTUP:
                    wm = WinAPI.WM.RBUTTONUP;
                    CurrentMK = (CurrentMK | WinAPI.Windows.MK.MK_RBUTTON) ^ WinAPI.Windows.MK.MK_RBUTTON;
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_MIDDLEDOWN:
                    wm = WinAPI.WM.MBUTTONDOWN;
                    CurrentMK |= WinAPI.Windows.MK.MK_MBUTTON;
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_MIDDLEUP:
                    wm = WinAPI.WM.MBUTTONUP;
                    CurrentMK = (CurrentMK | WinAPI.Windows.MK.MK_MBUTTON) ^ WinAPI.Windows.MK.MK_MBUTTON;
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_XDOWN:
                    wm = WinAPI.WM.XBUTTONDOWN;
                    switch (WinAPI.MACROS.GET_XBUTTON_WPARAM(mouseInput.MouseData))
                    {
                        case WinAPI.MACROS.XBUTTONS.XBUTTON1:
                            CurrentMK |= WinAPI.Windows.MK.MK_XBUTTON1;
                            break;

                        case WinAPI.MACROS.XBUTTONS.XBUTTON2:
                            CurrentMK |= WinAPI.Windows.MK.MK_XBUTTON2;
                            break;
                    }
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_XUP:
                    wm = WinAPI.WM.XBUTTONUP;
                    switch (WinAPI.MACROS.GET_XBUTTON_WPARAM(mouseInput.MouseData))
                    {
                        case WinAPI.MACROS.XBUTTONS.XBUTTON1:
                            CurrentMK = (CurrentMK | WinAPI.Windows.MK.MK_XBUTTON1) ^ WinAPI.Windows.MK.MK_XBUTTON1;
                            break;

                        case WinAPI.MACROS.XBUTTONS.XBUTTON2:
                            CurrentMK = (CurrentMK | WinAPI.Windows.MK.MK_XBUTTON2) ^ WinAPI.Windows.MK.MK_XBUTTON2;
                            break;
                    }
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_WHEEL:
                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_HWHEEL:
                    wm = WinAPI.WM.MOUSEWHEEL;
                    break;

                default:
                    wm = mouseInput.WM;
                    break;
            }

            WinAPI.Windows.PostMessage(ClientWindowHandle, wm, new UIntPtr(mouseInput.MouseData), new UIntPtr(clientRelativeCoordinates));
        }

        private void Process(KeyboardInput keyboardInput)
        {
            if (UpdatePressedKeys(keyboardInput.VK, keyboardInput.Scan, keyboardInput.Flags, keyboardInput.Time))
            {
                return;
            }

            switch ((WinAPI.VK)keyboardInput.VK)
            {
                case WinAPI.VK.Control:
                case WinAPI.VK.LeftControl:
                case WinAPI.VK.RightControl:
                    if ((keyboardInput.Flags & WinAPI.WindowHook.LLKHF.UP) == WinAPI.WindowHook.LLKHF.UP)
                    {
                        CurrentMK = (CurrentMK | WinAPI.Windows.MK.MK_CONTROL) ^ WinAPI.Windows.MK.MK_CONTROL;
                    }
                    else
                    {
                        CurrentMK |= WinAPI.Windows.MK.MK_CONTROL;
                    }
                    break;

                case WinAPI.VK.Shift:
                case WinAPI.VK.LeftShift:
                case WinAPI.VK.RightShift:
                    if ((keyboardInput.Flags & WinAPI.WindowHook.LLKHF.UP) == WinAPI.WindowHook.LLKHF.UP)
                    {
                        CurrentMK = (CurrentMK | WinAPI.Windows.MK.MK_SHIFT) ^ WinAPI.Windows.MK.MK_SHIFT;
                    }
                    else
                    {
                        CurrentMK |= WinAPI.Windows.MK.MK_CONTROL;
                    }
                    break;
            }

            var vk = keyboardInput.VK;
            WinAPI.WindowHook.LLKHF flags = keyboardInput.Flags;
            uint scan = keyboardInput.Scan;
            uint time = keyboardInput.Time;
            WinAPI.CAS cas = keyboardInput.CAS;

            var wParam = (uint)vk;

            WinAPI.WM wm = (((flags & WinAPI.WindowHook.LLKHF.UP) == WinAPI.WindowHook.LLKHF.UP) ? WinAPI.WM.KEYUP : WinAPI.WM.KEYDOWN); // TODO SYSKEYDOWN via Win32.WindowHook.LLKHF.AltKey ?
            uint lParam = 0x01;

            if (wm == WinAPI.WM.KEYUP)
            {
                lParam |= 0xC0000000; // TODO: this may need to change on 64bit platforms, not clear
            }

            uint scanCode = scan;
            if (scanCode > 0)
            {
                lParam |= ((scanCode & 0xFF) << 16); // TODO: this may need to change on 64bit platforms, not clear
            }

            if ((flags & WinAPI.WindowHook.LLKHF.UP) != WinAPI.WindowHook.LLKHF.UP)
            {
                // async Win32.GetKeyboardState or similar to capture actual/current CAS states
                if ((cas & WinAPI.CAS.CONTROL) != 0)
                {
                    Process(new KeyboardInput
                    {
                        VK = WinAPI.VK.LeftControl,
                        Flags = (WinAPI.WindowHook.LLKHF)0,
                        Scan = (uint)WinAPI.SendInputApi.MapVirtualKey(WinAPI.VK.LeftControl, WinAPI.SendInputApi.MAPVK.MAPVK_VK_TO_VSC),
                        Time = time,
                        CAS = (WinAPI.CAS)0,
                    });
                    //Process(new KeyboardInput
                    //{
                    //    VK = (uint)WinAPI.VK.Control,
                    //    Flags = (WinAPI.WindowHook.LLKHF)0,
                    //    Scan = (uint)0, // TODO: convert to scan?
                    //    Time = time,
                    //    CAS = (WinAPI.CAS)0,
                    //});
                }
                if ((cas & WinAPI.CAS.ALT) != 0)
                {
                    Process(new KeyboardInput
                    {
                        VK = WinAPI.VK.LeftMenu,
                        Flags = (WinAPI.WindowHook.LLKHF)0,
                        Scan = (uint)WinAPI.SendInputApi.MapVirtualKey(WinAPI.VK.LeftMenu, WinAPI.SendInputApi.MAPVK.MAPVK_VK_TO_VSC),
                        Time = time,
                        CAS = (WinAPI.CAS)0,
                    });
                    //Process(new KeyboardInput
                    //{
                    //    VK = (uint)WinAPI.VK.Menu,
                    //    Flags = (WinAPI.WindowHook.LLKHF)0,
                    //    Scan = (uint)0, // TODO: convert to scan?
                    //    Time = time,
                    //    CAS = (WinAPI.CAS)0,
                    //});
                    flags |= WinAPI.WindowHook.LLKHF.ALTDOWN;
                }
                if ((cas & WinAPI.CAS.SHIFT) != 0)
                {
                    Process(new KeyboardInput
                    {
                        VK = WinAPI.VK.LeftShift,
                        Flags = (WinAPI.WindowHook.LLKHF)0,
                        Scan = (uint)WinAPI.SendInputApi.MapVirtualKey(WinAPI.VK.LeftShift, WinAPI.SendInputApi.MAPVK.MAPVK_VK_TO_VSC),
                        Time = time,
                        CAS = (WinAPI.CAS)0,
                    });
                    //Process(new KeyboardInput
                    //{
                    //    VK = (uint)WinAPI.VK.Shift,
                    //    Flags = (WinAPI.WindowHook.LLKHF)0,
                    //    Scan = (uint)0, // TODO: convert to scan?
                    //    Time = time,
                    //    CAS = (WinAPI.CAS)0,
                    //});
                }
            }

            // NOTE: some apps may actually rely on keyboard state, AttachInputThread will clobber keyboard state. now that we don't continually attach/detach we should be able to set keyboard correctly
            // TODO: this should be a game profile level option - some games exhibit 'double entry' of input when this is called
            // WinAPI.SetKeyboardState(this.pressedKeys);

            if (this.pressedKeys[(int)WinAPI.VK.Menu])
            {
                switch (wm)
                {
                    case WinAPI.WM.KEYDOWN:
                        wm = WinAPI.WM.SYSKEYDOWN;
                        break;
                    case WinAPI.WM.KEYUP:
                        wm = WinAPI.WM.SYSKEYUP;
                        break;
                }
            }
            else
            {
                switch (wm)
                {
                    case WinAPI.WM.SYSKEYDOWN:
                        wm = WinAPI.WM.KEYDOWN;
                        break;
                    case WinAPI.WM.SYSKEYUP:
                        wm = WinAPI.WM.KEYUP;
                        break;
                }
            }
            
            WinAPI.Windows.SendMessage(ClientWindowHandle, wm, new UIntPtr(wParam), new UIntPtr(lParam));

            // if keydown, translate message
            // TODO: this should be a game profile option - some games may exhibit 'double entry' of input when this is called, most games don't function correctly without it
            if (wm == WinAPI.WM.KEYDOWN || wm == WinAPI.WM.SYSKEYDOWN)
            {
                var msg = new WinAPI.Windows.MSG();
                msg.hwnd = ClientWindowHandle;
                msg.lParam = lParam;
                msg.message = wm;
                msg.pt = new WinAPI.Windows.POINT();
                msg.time = WinAPI.SendInputApi.GetTickCount();
                msg.wParam = (int)vk;
                WinAPI.Windows.TranslateMessage(ref msg);
                //WinAPI.Windows.GetMessage(out msg, ClientWindowHandle, Mubox.WinAPI.WM.CHAR, WinAPI.WM.UNICHAR);
                //WinAPI.Windows.SendMessage(ClientWindowHandle, wm, new UIntPtr(wParam), new UIntPtr(lParam));
            }

            // TODO: this expression should probably be checking for == UP, but the individual key states need to be refactored to check current state first)
            // NOTE: if subsequent keys still rely on this state, it will be re-set as expected because of the sister CASE code above
            if ((flags & WinAPI.WindowHook.LLKHF.UP) != WinAPI.WindowHook.LLKHF.UP)
            {
                if ((cas & WinAPI.CAS.CONTROL) != 0)
                {
                    Process(new KeyboardInput
                    {
                        VK = WinAPI.VK.LeftControl,
                        Flags = WinAPI.WindowHook.LLKHF.UP,
                        Scan = (uint)WinAPI.SendInputApi.MapVirtualKey(WinAPI.VK.LeftControl, WinAPI.SendInputApi.MAPVK.MAPVK_VK_TO_VSC),
                        Time = time,
                        CAS = (WinAPI.CAS)0,
                    });
                    //Process(new KeyboardInput
                    //{
                    //    VK = (uint)WinAPI.VK.Control,
                    //    Flags = WinAPI.WindowHook.LLKHF.UP,
                    //    Scan = (uint)0, // TODO: convert to scan?
                    //    Time = time,
                    //    CAS = (WinAPI.CAS)0,
                    //});
                }
                if ((cas & WinAPI.CAS.ALT) != 0)
                {
                    Process(new KeyboardInput
                    {
                        VK = WinAPI.VK.LeftMenu,
                        Flags = WinAPI.WindowHook.LLKHF.UP,
                        Scan = (uint)WinAPI.SendInputApi.MapVirtualKey(WinAPI.VK.LeftMenu, WinAPI.SendInputApi.MAPVK.MAPVK_VK_TO_VSC),
                        Time = time,
                        CAS = (WinAPI.CAS)0,
                    });
                    //Process(new KeyboardInput
                    //{
                    //    VK = (uint)WinAPI.VK.Menu,
                    //    Flags = WinAPI.WindowHook.LLKHF.UP,
                    //    Scan = (uint)0, // TODO: convert to scan?
                    //    Time = time,
                    //    CAS = (WinAPI.CAS)0,
                    //});
                }
                if ((cas & WinAPI.CAS.SHIFT) != 0)
                {
                    Process(new KeyboardInput
                    {
                        VK = WinAPI.VK.LeftShift,
                        Flags = WinAPI.WindowHook.LLKHF.UP,
                        Scan = (uint)WinAPI.SendInputApi.MapVirtualKey(WinAPI.VK.LeftShift, WinAPI.SendInputApi.MAPVK.MAPVK_VK_TO_VSC),
                        Time = time,
                        CAS = (WinAPI.CAS)0,
                    });
                    //Process(new KeyboardInput
                    //{
                    //    VK = (uint)WinAPI.VK.Shift,
                    //    Flags = WinAPI.WindowHook.LLKHF.UP,
                    //    Scan = (uint)0, // TODO: convert to scan?
                    //    Time = time,
                    //    CAS = (WinAPI.CAS)0,
                    //});
                }
            }
        }

        #region client-side 'IsRepeatKey' AND 'GetAsyncKeyState' behavior

        private System.Collections.BitArray pressedKeys = new System.Collections.BitArray(256);

        private bool IsKeyPressed(WinAPI.VK vk)
        {
            return pressedKeys[(int)vk];
        }

        /// <summary>
        /// <para>Updates key state and returns value indicating transitions.</para>
        /// </summary>
        /// <param name="vk"></param>
        /// <param name="scan"></param>
        /// <param name="flags"></param>
        /// <param name="time"></param>
        /// <returns>True if the key state has transitioned.</returns>
        private bool UpdatePressedKeys(WinAPI.VK vk, uint scan, WinAPI.WindowHook.LLKHF flags, uint time)
        {
            var result = false;
            if (WinAPI.WindowHook.LLKHF.UP != (flags & WinAPI.WindowHook.LLKHF.UP))
            {
                if (!IsKeyPressed(vk))
                {
                    result = true;
                    pressedKeys[(int)vk] = true;
                }
            }
            else
            {
                if (IsKeyPressed(vk))
                {
                    result = true;
                    pressedKeys[(int)vk] = false;
                }
            }
            return result;
        }

        #endregion client-side 'IsRepeatKey' behavior

    }
}
