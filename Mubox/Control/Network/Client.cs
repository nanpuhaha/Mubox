using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Mubox.Control.Network
{
    public class Client
    {
        public TcpClient EndPoint { get; set; }

        public string DisplayName { get; set; }

        public string ProfileName { get; set; }

        public IntPtr WindowStationHandle { get; set; }

        public IntPtr WindowDesktopHandle { get; set; }

        public IntPtr WindowHandle
        {
            get
            {
                return _windowHandle;
            }
            set
            {
                if (_windowHandle != value)
                {
                    lock (_tiqLock)
                    {
                        _windowHandle = value;
                        if (_windowHandle == IntPtr.Zero)
                        {
                            WindowInputQueue = IntPtr.Zero;
                            return;
                        }
                        IntPtr windowInputQueue = WindowInputQueue;
                        if (WindowInputQueue == IntPtr.Zero)
                        {
                            if (_windowHandle != IntPtr.Zero)
                            {
                                var windowProcessId = IntPtr.Zero;
                                windowInputQueue = WinAPI.Threads.GetWindowThreadProcessId(_windowHandle, out windowProcessId);
                                if (windowInputQueue == IntPtr.Zero)
                                {
                                    ("GWTPID Failed for set_WindowHandle(" + _windowHandle + ") ").Log();
                                }
                                else
                                {
                                    WindowInputQueue = windowInputQueue;
                                }
                            }
                        }
                    }
                }
            }
        }

        private IntPtr _windowHandle;

        public IntPtr WindowInputQueue { get; set; }

        public static IntPtr MyInputQueue { get; set; }

        private System.Runtime.Serialization.DataContractSerializer Serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(Model.Input.InputBase),
            new Type[]
            {
                typeof(Model.Input.CommandInput),
                typeof(Model.Input.KeyboardInput),
                typeof(Model.Input.MouseInput),
            });

        public Client()
        {
        }

        public void Connect(string host, int port)
        {
            if (EndPoint == null)
            {
                EndPoint = new TcpClient();
                EndPoint.NoDelay = true;
                EndPoint.Connect(host, port);
                EndPoint.LingerState.Enabled = false;
                OnConnected();
                StartReceiving();
            }
        }

        public void Disconnect()
        {
            if (EndPoint != null)
            {
                try
                {
                    EndPoint.Close();
                }
                catch (Exception)
                {
                    // BOP
                }
                finally
                {
                    EndPoint = null;
                }
            }
            OnDisconnected();
        }

        private void StartReceiving()
        {
            try
            {
                SocketError socketError;
                byte[] receiveBuffer = new byte[4096];
                if (EndPoint != null && EndPoint.Client != null)
                {
                    EndPoint.Client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, out socketError, BeginReceiveCallback, receiveBuffer);
                }
                else
                {
                    socketError = SocketError.NetworkDown;
                }
                if (socketError != SocketError.Success)
                {
                    throw new SocketException((int)socketError);
                }
            }
            catch (Exception)
            {
                // NOP
            }
        }

        private byte[] fragmentBuffer = new byte[0];

        // TODO: re-add encryption

        private void BeginReceiveCallback(IAsyncResult ar)
        {
            Queue<Action> actionQueue = new Queue<Action>();
            try
            {
                TcpClient lEndPoint = EndPoint;
                Thread.MemoryBarrier();
                if (lEndPoint == null)
                {
                    ("NoEndPoint for " + this.DisplayName).Log();
                    fragmentBuffer = new byte[0];
                    return;
                }
                int cb = lEndPoint.Client.EndReceive(ar);

                if (cb == 0)
                {
                    //Disconnect();
                    return;
                }

                byte[] receiveBuffer = ar.AsyncState as byte[];

                if (fragmentBuffer.Length > 0)
                {
                    var temp = new byte[cb + fragmentBuffer.Length];
                    fragmentBuffer.CopyTo(temp, 0);
                    Array.ConstrainedCopy(receiveBuffer, 0, temp, fragmentBuffer.Length, cb);
                    fragmentBuffer = new byte[0];
                    receiveBuffer = temp;
                }
                else
                {
                    var temp = new byte[cb];
                    Array.ConstrainedCopy(receiveBuffer, 0, temp, 0, cb);
                    receiveBuffer = temp;
                }

                //("MCNC: receiveBuffer=" + System.Text.Encoding.ASCII.GetString(receiveBuffer)).Log();

                var stream = new System.IO.MemoryStream(receiveBuffer);
                using (var reader = new System.IO.BinaryReader(stream))
                {
                    while (stream.Position < stream.Length && stream.ReadByte() == 0x1b)
                    {
                        var lastReadPosition = stream.Position - 1;
                        object o = null;
                        try
                        {
                            var len = reader.ReadUInt16();
                            byte[] payload = new byte[len];
                            stream.Read(payload, 0, payload.Length);
                            using (var payloadStream = new System.IO.MemoryStream(payload))
                            {
                                o = Serializer.ReadObject(payloadStream);
                            }
                        }
                        catch (Exception ex)
                        {
                            //("MCNC: lastReadPosition=" + lastReadPosition + " currentPosition=" + stream.Position).Log();

                            ex.Log();
                            // TODO: log/debug exception types thrown here, not documented on MSDN and it's not clear how to handle a buffer underrun in ReadObject
                            // TODO: if the exception is due to an unknown type, the fragmentBuffer logic will result in a deadlock on the network (always retrying a bad object)
                            o = null;
                        }

                        if (o == null)
                        {
                            stream.Seek(lastReadPosition, System.IO.SeekOrigin.Begin);
                            fragmentBuffer = new byte[stream.Length - lastReadPosition];
                            stream.Read(fragmentBuffer, 0, fragmentBuffer.Length);
                            break;
                        }
                        else
                        {
                            EnqueueAction(actionQueue, o);
                        }
                    }
                }

                if (actionQueue.Count == 0)
                {
                    ("NoActions for " + this.DisplayName).Log();
                    return;
                }

                #region process action queue

                while (actionQueue.Count > 0)
                {
                    Action action = actionQueue.Dequeue();
                    if (action != null)
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            ex.Log();
                        }
                    }
                }

                #endregion process action queue
            }
            catch (SocketException)
            {
                Disconnect();
            }
            catch (ObjectDisposedException)
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            finally
            {
                StartReceiving();
            }
        }

        private void OnUnknownInputReceived(object o)
        {
            // TODO: log
        }

        private static object _tiqLock = new object();

        private void OnMouseInputReceived(Model.Input.MouseInput mouseInput)
        {
            bool useTIQ = true; //TODO: this needs test and reverification of semantics/rules because it does not appear to allow mouse broadcast

            // translate message and track MK changes
            WinAPI.WM wm = WinAPI.WM.USER;
            bool isButtonUpEvent = false;

            // strip 'absolute' flag from mask and process result
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
                    isButtonUpEvent = true;
                    CurrentMK = (CurrentMK | WinAPI.Windows.MK.MK_LBUTTON) ^ WinAPI.Windows.MK.MK_LBUTTON;
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_RIGHTDOWN:
                    wm = WinAPI.WM.RBUTTONDOWN;
                    CurrentMK |= WinAPI.Windows.MK.MK_RBUTTON;
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_RIGHTUP:
                    wm = WinAPI.WM.RBUTTONUP;
                    CurrentMK = (CurrentMK | WinAPI.Windows.MK.MK_RBUTTON) ^ WinAPI.Windows.MK.MK_RBUTTON;
                    isButtonUpEvent = true;
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_MIDDLEDOWN:
                    wm = WinAPI.WM.MBUTTONDOWN;
                    CurrentMK |= WinAPI.Windows.MK.MK_MBUTTON;
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_MIDDLEUP:
                    wm = WinAPI.WM.MBUTTONUP;
                    CurrentMK = (CurrentMK | WinAPI.Windows.MK.MK_MBUTTON) ^ WinAPI.Windows.MK.MK_MBUTTON;
                    isButtonUpEvent = true;
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_XDOWN:
                    wm = WinAPI.WM.XBUTTONDOWN;
                    {
                        var xbutton = WinAPI.MACROS.GET_XBUTTON_WPARAM(mouseInput.MouseData);
                        switch (xbutton)
                        {
                            case WinAPI.MACROS.XBUTTONS.XBUTTON1:
                                CurrentMK |= WinAPI.Windows.MK.MK_XBUTTON1;
                                break;

                            case WinAPI.MACROS.XBUTTONS.XBUTTON2:
                                CurrentMK |= WinAPI.Windows.MK.MK_XBUTTON2;
                                break;

                            default:
                                ("UnsupportedButtonDown in MouseData(" + xbutton + ") for " + this.DisplayName).Log();
                                break;
                        }
                    }
                    break;

                case WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_XUP:
                    wm = WinAPI.WM.XBUTTONUP;
                    isButtonUpEvent = true;
                    {
                        var xbutton = WinAPI.MACROS.GET_XBUTTON_WPARAM(mouseInput.MouseData);
                        switch (xbutton)
                        {
                            case WinAPI.MACROS.XBUTTONS.XBUTTON1:
                                CurrentMK = (CurrentMK | WinAPI.Windows.MK.MK_XBUTTON1) ^ WinAPI.Windows.MK.MK_XBUTTON1;
                                break;

                            case WinAPI.MACROS.XBUTTONS.XBUTTON2:
                                CurrentMK = (CurrentMK | WinAPI.Windows.MK.MK_XBUTTON2) ^ WinAPI.Windows.MK.MK_XBUTTON2;
                                break;

                            default:
                                ("UnsupportedButtonUp in MouseData(" + xbutton + ") for " + this.DisplayName).Log();
                                break;
                        }
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

            // no target window? can't use
            if (WindowHandle == IntPtr.Zero)
            {
                ("NoWindowHandle Failed OnMouseInputReceived, Input Loss for " + this.DisplayName).Log();
                return;
            }

            // denormalize coordinates to local
            WinAPI.Windows.RECT clientRect;
            WinAPI.Windows.GetClientRect(WindowHandle, out clientRect);
            int lPointX = (int)(((double)clientRect.Width / (double)65536) * mouseInput.Point.X);
            int lPointY = (int)(((double)clientRect.Height / (double)65536) * mouseInput.Point.Y);

            lock (_tiqLock)
            {
                if (useTIQ)
                {
                    // can't resolve TIQ? can't use
                    IntPtr windowInputQueue = WindowInputQueue;
                    if (windowInputQueue == IntPtr.Zero)
                    {
                        ("NoWindowInputQueue Failed OnMouseInputReceived, Input Loss for " + this.DisplayName).Log();
                        useTIQ = false;
                    }
                    else
                    {
                        // resolve TIQ
                        IntPtr foregroundWindowHandle;
                        IntPtr foregroundInputQueue;
                        if (!TryResolveTIQ(out foregroundInputQueue, out foregroundWindowHandle, DateTime.Now.AddMilliseconds(300).Ticks))
                        {
                            ("TryResolveTIQ Failed OnMouseInputReceived, Input Loss for " + this.DisplayName).Log();
                            useTIQ = false;
                        }
                        else
                        {
                            ActionViaTIQ(() =>
                            {
                                //WinAPI.SendInputApi.MouseActionViaSendInput(mouseInput.Flags, mouseInput.Time, (int)mouseInput.Point.X, (int)mouseInput.Point.Y, mouseInput.MouseData);
                                MouseActionViaPostMessage((int)mouseInput.Point.X, (int)mouseInput.Point.Y, lPointX, lPointY, wm, mouseInput.MouseData, isButtonUpEvent);
                            },
                                foregroundInputQueue, "OnMouseInputReceived");
                        }
                    }
                }

                if (!useTIQ)
                {

                    // dispatch
                    MouseActionViaSendMessage((int)mouseInput.Point.X, (int)mouseInput.Point.Y, lPointX, lPointY, wm, mouseInput.MouseData, isButtonUpEvent);
                }
            }
        }

        private void OnKeyboardInputReceived(Model.Input.KeyboardInput keyboardInput)
        {
            // coerce specialized left/right shift-state to generalized shift-state
            // TODO: this should be a profile-level option
            /*
            switch ((WinAPI.VK)keyboardInput.VK)
            {
                case WinAPI.VK.LeftShift:
                case WinAPI.VK.RightShift:
                    keyboardInput.VK = (uint)WinAPI.VK.Shift;
                    break;

                case WinAPI.VK.LeftMenu:
                case WinAPI.VK.RightMenu:
                    keyboardInput.VK = (uint)WinAPI.VK.Menu;
                    break;

                case WinAPI.VK.LeftControl:
                case WinAPI.VK.RightControl:
                    keyboardInput.VK = (uint)WinAPI.VK.Control;
                    break;
            }
             */

            // prevent windows key-repeat
            // TODO: this should be a profile-level option
            if (IsRepeatKey(keyboardInput.VK, keyboardInput.Scan, keyboardInput.Flags, keyboardInput.Time))
            {
                return;
            }

            lock (_tiqLock)
            {
                // maintain MK state
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

                IntPtr windowHandle = WindowHandle;

                // no target window
                if (windowHandle == IntPtr.Zero)
                {
                    ("NoWindowHandle Failed OnKeyboardInputReceived, using SendInput for " + this.DisplayName).Log();
                    WinAPI.SendInputApi.SendInputViaKBParams(keyboardInput.Flags, keyboardInput.Time, keyboardInput.Scan, keyboardInput.VK, keyboardInput.CAS);
                    return;
                }

                // no TIQ available
                IntPtr windowInputQueue = WindowInputQueue;
                if (windowInputQueue == IntPtr.Zero)
                {
                    ("NoWindowInputQueue Failed OnKeyboardInputReceived, using SendInput for " + this.DisplayName).Log();
                    WinAPI.SendInputApi.SendInputViaKBParams(keyboardInput.Flags, keyboardInput.Time, keyboardInput.Scan, keyboardInput.VK, keyboardInput.CAS);
                    return;
                }

                // resolve TIQ
                IntPtr foregroundWindowHandle;
                IntPtr foregroundInputQueue;
                if (!TryResolveTIQ(out foregroundInputQueue, out foregroundWindowHandle, DateTime.Now.AddMilliseconds(300).Ticks))
                {
                    ("TryResolveTIQ Failed OnKeyboardInputReceived, using SendInput for " + this.DisplayName).Log();
                    WinAPI.SendInputApi.SendInputViaKBParams(keyboardInput.Flags, keyboardInput.Time, keyboardInput.Scan, keyboardInput.VK, keyboardInput.CAS);
                    return;
                }

                // use TIQ
                Action action = () => OnKeyboardEventViaTIQ(keyboardInput.VK, keyboardInput.Flags, keyboardInput.Scan, keyboardInput.Time, keyboardInput.CAS);
                ActionViaTIQ(action, foregroundInputQueue, "OnKeyboardInputReceived");
            }
        }

        private void OnCommandInputReceived(Model.Input.CommandInput commandInput)
        {
            switch (commandInput.Text.ToUpper())
            {
                case "AC":
                    OnActivateClient();
                    break;

                case "DA":
                    OnDeactivateClient();
                    break;

                case "PING":
                    try
                    {
                        OnPing();
                    }
                    catch (SocketException ex)
                    {
                        Disconnect();
                        ex.Log();
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                    break;

                case "DC":
                    try
                    {
                        Disconnect();
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                    break;

                default:
                    ("UnknownCommand '" + (commandInput.Text ?? "") + "' for " + this.DisplayName).Log();
                    break;
            }
        }

        private void EnqueueAction(Queue<Action> queue, object input)
        {
            var action = (Action)(() =>
            {
                if (input is Model.Input.CommandInput)
                {
                    OnCommandInputReceived(input as Model.Input.CommandInput);
                }
                else if (input is Model.Input.KeyboardInput)
                {
                    OnKeyboardInputReceived(input as Model.Input.KeyboardInput);
                }
                else if (input is Model.Input.MouseInput)
                {
                    OnMouseInputReceived(input as Model.Input.MouseInput);
                }
                else
                {
                    OnUnknownInputReceived(input);
                }
            });
            queue.Enqueue(action);
        }

        public WinAPI.Windows.MK CurrentMK { get; private set; }

        public static bool TryResolveTIQ(out IntPtr foregroundInputQueue, out IntPtr foregroundWindowHandle, long activationExpiryTime)
        {
            foregroundWindowHandle = IntPtr.Zero;
            foregroundInputQueue = IntPtr.Zero;
            var wait = 0;
            do
            {
                foregroundWindowHandle = WinAPI.Windows.GetForegroundWindow();
                if (foregroundWindowHandle != IntPtr.Zero)
                {
                    var foregroundProcessId = IntPtr.Zero;
                    foregroundInputQueue = WinAPI.Threads.GetWindowThreadProcessId(foregroundWindowHandle, out foregroundProcessId);
                }
                System.Threading.Thread.Sleep(wait++);
            } while ((foregroundInputQueue == IntPtr.Zero) && (DateTime.Now.Ticks <= activationExpiryTime));
            return foregroundInputQueue != IntPtr.Zero;
        }

        private void ActionViaTIQ(Action action, IntPtr foregroundInputQueue, string callingComponent)
        {
            bool detachWIQ = false;
            bool detachMIQ = false;
            IntPtr oldFocusWindowHandle = IntPtr.Zero;
            try
            {
                if (MyInputQueue != foregroundInputQueue)
                {
                    detachMIQ = true;
                    if (!WinAPI.Windows.AttachThreadInput(MyInputQueue, foregroundInputQueue, true))
                    {
                        ("ATI MIQ Failed " + callingComponent + " for " + this.DisplayName).Log();
                    }
                }

                if (WindowInputQueue != foregroundInputQueue)
                {
                    detachWIQ = true;
                    if (!WinAPI.Windows.AttachThreadInput(foregroundInputQueue, WindowInputQueue, true))
                    {
                        ("ATI WIQ Failed " + callingComponent + " for " + this.DisplayName).Log();
                    }
                }

                // NOTE: this shouldn't be necessary as we've already attached to a foreground window (and we assume it is active, even though it may not be)
                // WinAPI.Windows.SetActiveWindow(WindowHandle);
                // TODO: not clear if it's really necessary to set focus like this, and in the case of a non-active window target will have no effect anyway
                oldFocusWindowHandle = WinAPI.Windows.SetFocus(WindowHandle);

                // send message
                action();
            }
            finally
            {
                // clean-up
                if (oldFocusWindowHandle != IntPtr.Zero)
                {
                    WinAPI.Windows.SetActiveWindow(oldFocusWindowHandle);
                    WinAPI.Windows.SetFocus(oldFocusWindowHandle);
                }
                if (detachWIQ)
                {
                    WinAPI.Windows.AttachThreadInput(foregroundInputQueue, WindowInputQueue, false);
                }
                if (detachMIQ)
                {
                    WinAPI.Windows.AttachThreadInput(MyInputQueue, foregroundInputQueue, false);
                }
            }
        }

        #region client-side 'IsRepeatKey' behavior

        private byte[] pressedKeys = new byte[256];

        private bool IsRepeatKey(uint vk, uint scan, WinAPI.WindowHook.LLKHF flags, uint time)
        {
            bool keyIsPressed = pressedKeys[vk] == 0x80;
            if (WinAPI.WindowHook.LLKHF.UP != (flags & WinAPI.WindowHook.LLKHF.UP))
            {
                if (keyIsPressed)
                {
                    return false;
                }
                else
                {
                    this.pressedKeys[vk] = 0x80;
                }
            }
            else
            {
                if (!keyIsPressed)
                {
                    return false;
                }
                else
                {
                    this.pressedKeys[vk] = (byte)(WinAPI.IsToggled((WinAPI.VK)vk) ? 1 : 0);
                }
            }
            return false;
        }

        #endregion client-side 'IsRepeatKey' behavior

        private void OnKeyboardEventViaTIQ(uint vk, WinAPI.WindowHook.LLKHF flags, uint scan, uint time, WinAPI.CAS cas)
        {
            var wParam = vk;

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
                    // TODO: only if '' is not already 'down' (get async key state)
                    OnKeyboardEventViaTIQ((uint)WinAPI.VK.Control, (WinAPI.WindowHook.LLKHF)0, (uint)0, time, (WinAPI.CAS)0);
                }
                if ((cas & WinAPI.CAS.ALT) != 0)
                {
                    // TODO: only if '' is not already 'down' (get async key state)
                    OnKeyboardEventViaTIQ((uint)WinAPI.VK.Menu, (WinAPI.WindowHook.LLKHF)0, (uint)0, time, (WinAPI.CAS)0);
                    flags |= WinAPI.WindowHook.LLKHF.ALTDOWN;
                }
                if ((cas & WinAPI.CAS.SHIFT) != 0)
                {
                    // TODO: only if '' is not already 'down' (get async key state)
                    OnKeyboardEventViaTIQ((uint)WinAPI.VK.Shift, (WinAPI.WindowHook.LLKHF)0, (uint)0, time, (WinAPI.CAS)0);
                }
            }

            // what needs this? WinAPI.SetKeyboardState(this.pressedKeys);

            // TODO: SendMessage / PostMessage bypass TIQ, technically only reason these process is because the foreground window is being waited+verified before proceeding (a requirement for TIQ to function to begin with)
            // TODO: need to interface at a lower level (e.g. SendInput
            WinAPI.Windows.SendMessage(WindowHandle, wm, new UIntPtr(wParam), new UIntPtr(lParam));

            // if keydown, translate message
            if (wm == WinAPI.WM.KEYDOWN)
            {
                var msg = new WinAPI.Windows.MSG();
                msg.hwnd = WindowHandle;
                msg.lParam = lParam;
                msg.message = wm;
                msg.pt = new WinAPI.Windows.POINT();
                msg.time = WinAPI.SendInputApi.GetTickCount();
                msg.wParam = (int)vk;
                WinAPI.Windows.TranslateMessage(ref msg);
            }

            // TODO: this expression should probably be checking for == UP, but the individual key states need to be refactored to check current state first)
            // NOTE: if subsequent keys still rely on this state, it will be re-set as expected because of the sister CASE code above
            if ((flags & WinAPI.WindowHook.LLKHF.UP) != WinAPI.WindowHook.LLKHF.UP)
            {
                if ((cas & WinAPI.CAS.CONTROL) != 0)
                {
                    // TODO: only if '' is still 'down' (get async key state)
                    OnKeyboardEventViaTIQ((uint)WinAPI.VK.Control, WinAPI.WindowHook.LLKHF.UP, (uint)0, WinAPI.SendInputApi.GetTickCount(), (WinAPI.CAS)0);
                }
                if ((cas & WinAPI.CAS.ALT) != 0)
                {
                    // TODO: only if '' is not already 'down' (get async key state)
                    OnKeyboardEventViaTIQ((uint)WinAPI.VK.Menu, WinAPI.WindowHook.LLKHF.UP, (uint)0, WinAPI.SendInputApi.GetTickCount(), (WinAPI.CAS)0);
                }
                if ((cas & WinAPI.CAS.SHIFT) != 0)
                {
                    // TODO: only if '' is not already 'down' (get async key state)
                    OnKeyboardEventViaTIQ((uint)WinAPI.VK.Shift, WinAPI.WindowHook.LLKHF.UP, (uint)0, WinAPI.SendInputApi.GetTickCount(), (WinAPI.CAS)0);
                }
            }
        }

        public static volatile IntPtr LastActivatedClientWindowHandle = IntPtr.Zero; // HACK can't mouse-click between game windows without mouse buttons getting stuck

        private static object OnMouseEventLock = new object();

        private void MouseActionViaSendMessage(int pointX, int pointY, int lPointX, int lPointY, WinAPI.WM wm, uint mouseData, bool isButtonUpEvent)
        {
            var clientRelativeCoordinates = WinAPI.MACROS.MAKELPARAM(
                (ushort)lPointX,
                (ushort)lPointY);

            //            IntPtr previousWindowCapture = Win32.Cursor.GetCapture() // COMMENTED BY CODEIT.RIGHT;
            lock (OnMouseEventLock)
            {
                //Win32.Cursor.SetCapture(windowHandle);

                // TODO: WinAPI.Windows.SendMessage(_windowHandle, WinAPI.WM.ACTIVATEAPP, _windowHandle, new UIntPtr(WinAPI.MACROS.MAKELPARAM((ushort)wm, (ushort)WinAPI.HitTestValues.HTCLIENT)));
                // TODO: WinAPI.Windows.SendMessage(_windowHandle, WinAPI.WM.ACTIVATE, _windowHandle, new UIntPtr(WinAPI.MACROS.MAKELPARAM((ushort)wm, (ushort)WinAPI.HitTestValues.HTCLIENT)));
                WinAPI.Windows.SendMessage(_windowHandle, WinAPI.WM.MOUSEACTIVATE, _windowHandle, new UIntPtr(WinAPI.MACROS.MAKELPARAM((ushort)wm, (ushort)WinAPI.HitTestValues.HTCLIENT)));
                WinAPI.Windows.SendMessage(_windowHandle, WinAPI.WM.MOUSEMOVE, new UIntPtr((uint)CurrentMK), new UIntPtr(clientRelativeCoordinates));
                WinAPI.Windows.SendMessage(_windowHandle, wm, new UIntPtr(mouseData), new UIntPtr(clientRelativeCoordinates));
                //("OnMouseEvent SendMessage(" + _windowHandle.ToString() + ", " + wm + ", " + mouseData + ", " + clientRelativeCoordinates + ", " + pointX + ", " + pointY + ", " + lPointX + ", " + lPointY + ", (" + CurrentMK + "), " + isButtonUpEvent).Log();
                //Win32.Cursor.ReleaseCapture();
            }
        }

        // NOTE: the following is reference implementation from 2009, which used to be called as a 'VIQ' actionnew UIntPtr(
        private void MouseActionViaPostMessage(int pointX, int pointY, int lPointX, int lPointY, WinAPI.WM wm, uint mouseData, bool isButtonUpEvent)
        {
            uint clientRelativeCoordinates = WinAPI.MACROS.MAKELPARAM(
                (ushort)lPointX,
                (ushort)lPointY);

            //            IntPtr previousWindowCapture = Win32.Cursor.GetCapture() // COMMENTED BY CODEIT.RIGHT;
            int hwnd = _windowHandle.ToInt32();
            lock (OnMouseEventLock)
            {
                WinAPI.Cursor.SetCapture(_windowHandle);
                try
                {
                    WinAPI.Windows.PostMessage(_windowHandle, WinAPI.WM.MOUSEACTIVATE, _windowHandle, new UIntPtr(WinAPI.MACROS.MAKELPARAM((ushort)wm, (ushort)WinAPI.HitTestValues.HTCLIENT)));
                    WinAPI.Windows.PostMessage(_windowHandle, WinAPI.WM.MOUSEMOVE, new IntPtr((int)CurrentMK), new UIntPtr(clientRelativeCoordinates));
                    WinAPI.Windows.PostMessage(_windowHandle, WinAPI.WM.SETCURSOR, _windowHandle, new UIntPtr(WinAPI.MACROS.MAKELPARAM((ushort)WinAPI.WM.MOUSEMOVE, (ushort)WinAPI.HitTestValues.HTCLIENT)));
                    WinAPI.Windows.PostMessage(_windowHandle, wm, new UIntPtr(mouseData), new UIntPtr(clientRelativeCoordinates));
                }
                finally
                {
                    WinAPI.Cursor.ReleaseCapture();
                }
                ("OnMouseEvent PostMessage(" + _windowHandle.ToString() + ", " + wm + ", " + mouseData + ", " + clientRelativeCoordinates + ", " + pointX + ", " + pointY + ", " + lPointX + ", " + lPointY + ", (" + CurrentMK + "), " + isButtonUpEvent).Log();
            }
        }


        private void OnActivateClient()
        {
            ("ReceivedActivateRequest for " + this.DisplayName).Log();
            DateTime onActivateClientReceivedTimestamp = DateTime.Now;
            lock (_tiqLock)
            {
                ("ActivateClientLock took " + onActivateClientReceivedTimestamp.Subtract(DateTime.Now) + " for " + this.DisplayName).Log();
                onActivateClientReceivedTimestamp = DateTime.Now;
                LastActivatedClientWindowHandle = _windowHandle;
                long activationExpiryTime = DateTime.Now.AddMilliseconds(1000).Ticks;
                do
                {
                    ("ActivateClientAttempt@" + WindowHandle + " for " + this.DisplayName).Log();
                    if (WindowHandle == IntPtr.Zero)
                    {
                        ("NoWindowHandle Failed OnActivateClient for " + this.DisplayName).Log();
                        return;
                    }

                    IntPtr windowInputQueue = WindowInputQueue;
                    if (windowInputQueue == IntPtr.Zero)
                    {
                        ("NoWindowInputQueue Failed OnActivateClient for " + this.DisplayName).Log();
                        return;
                    }

                    // resolve TIQ
                    IntPtr foregroundWindowHandle;
                    IntPtr foregroundInputQueue;
                    if (!TryResolveTIQ(out foregroundInputQueue, out foregroundWindowHandle, activationExpiryTime))
                    {
                        ("TryResolveTIQ Failed OnActivateClient for " + this.DisplayName).Log();
                        return;
                    }

                    // use TIQ
                    Action action = () =>
                    {
                        try
                        {
                            WinAPI.Windows.SetForegroundWindow(_windowHandle);
                            WinAPI.Windows.SetWindowPos(_windowHandle, WinAPI.Windows.Position.HWND_TOP, -1, -1, -1, -1, WinAPI.Windows.Options.SWP_NOSIZE | WinAPI.Windows.Options.SWP_NOMOVE | WinAPI.Windows.Options.SWP_SHOWWINDOW);
                            System.Threading.Thread.Sleep(1);
                            WinAPI.Windows.SetForegroundWindow(_windowHandle);
                        }
                        catch (Exception ex)
                        {
                            ex.Log();
                        }
                    };
                    ActionViaTIQ(action, foregroundInputQueue, "OnActivateClient");
                } while ((DateTime.Now.Ticks < activationExpiryTime) && (_windowHandle != WinAPI.Windows.GetForegroundWindow()));
                ("ActivateClientAction took " + onActivateClientReceivedTimestamp.Subtract(DateTime.Now) + " for " + this.DisplayName).Log();
            }
            NotifyClientActivated();
        }

        private void OnDeactivateClient()
        {
            ("ReceivedDeactivateRequest for " + this.DisplayName).Log();
            DateTime onDeactivateClientReceivedTimestamp = DateTime.Now;
            lock (_tiqLock)
            {
                ("DeactivateClientLock took " + onDeactivateClientReceivedTimestamp.Subtract(DateTime.Now) + " for " + this.DisplayName).Log();
                onDeactivateClientReceivedTimestamp = DateTime.Now;
                LastActivatedClientWindowHandle = _windowHandle;
                long activationExpiryTime = DateTime.Now.AddMilliseconds(1000).Ticks;

                ("DeactivateClientAttempt@" + WindowHandle + " for " + this.DisplayName).Log();
                if (WindowHandle == IntPtr.Zero)
                {
                    ("NoWindowHandle Failed OnDeactivateClient for " + this.DisplayName).Log();
                    return;
                }

                IntPtr windowInputQueue = WindowInputQueue;
                if (windowInputQueue == IntPtr.Zero)
                {
                    ("NoWindowInputQueue Failed OnDeactivateClient for " + this.DisplayName).Log();
                    return;
                }

                // resolve TIQ
                IntPtr foregroundWindowHandle;
                IntPtr foregroundInputQueue;
                if (!TryResolveTIQ(out foregroundInputQueue, out foregroundWindowHandle, activationExpiryTime))
                {
                    ("TryResolveTIQ Failed OnDeactivateClient for " + this.DisplayName).Log();
                    return;
                }

                // use TIQ
                Action action = () =>
                {
                    try
                    {
                        WinAPI.Windows.SetWindowPos(_windowHandle, WinAPI.Windows.Position.HWND_TOP, -1, -1, -1, -1, WinAPI.Windows.Options.SWP_NOSIZE | WinAPI.Windows.Options.SWP_NOMOVE | WinAPI.Windows.Options.SWP_NOACTIVATE);
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                };
                ActionViaTIQ(action, foregroundInputQueue, "OnDeactivateClient");
            }
            ("DeactivateClientAction took " + onDeactivateClientReceivedTimestamp.Subtract(DateTime.Now) + " for " + this.DisplayName).Log();
        }

        public event EventHandler<EventArgs> Connected;

        public event EventHandler<EventArgs> Disconnected;

        private void OnConnected()
        {
            SendClientConfig();
            if (this.Connected != null)
            {
                Connected(this, new EventArgs());
            }
        }

        public void SendClientConfig()
        {
            byte[] clientNameCommand = ASCIIEncoding.ASCII.GetBytes(string.Format("|NAME/{0}_{1}/{2}/{3}/{4}/{5}",
                this.DisplayName,
                this.ProfileName,
                this.WindowStationHandle.ToString(),
                this.WindowDesktopHandle.ToString(),
                this.WindowHandle.ToString(),
                "?"));
            if (this.EndPoint != null)
            {
                this.EndPoint.Client.Send(clientNameCommand, SocketFlags.None);
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2122")]
        public void SendPerformanceInfo(System.Diagnostics.Process process)
        {
            string[] stats = null;
            try
            {
                if (process == null)
                {
                    stats = new string[]
                            {
                                "Untitled",
                                "0",
                                "Untitled",
                                "False",
                                "0",
                                "0",
                                "0",
                                "0",
                                "0"
                                // TODO Disk Time, CPU Time
                            };
                }
                else
                {
                    if (!process.HasExited)
                    {
                        try
                        {
                            stats = new string[]
                            {
                                process.MainWindowTitle,
                                process.Id.ToString(),
                                System.IO.Path.GetFileName(process.ProcessName),
                                process.Responding.ToString(),
                                process.WorkingSet64.ToString(),
                                process.PeakWorkingSet64.ToString(),
                                process.PagedMemorySize64.ToString(),
                                process.PeakPagedMemorySize64.ToString(),
                                sendCommandTimeSpent.Ticks.ToString()
                                // TODO Disk Time, CPU Time
                            };
                        }
                        catch (Exception ex)
                        {
                            ex.Log();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }

            if (stats != null)
            {
                SendCommand("STAT",
                (sender, e) =>
                {
                    // NOP
                },
                stats);
            }
        }

        private void SendCommand(string commandName)
        {
            SendCommand(commandName, null, (string[])null);
        }

        private void SendCommand(string commandName, EventHandler<SocketAsyncEventArgs> callback)
        {
            SendCommand(commandName, callback, null);
        }

        private void SendCommand(string commandName, params string[] args)
        {
            SendCommand(commandName, null, args);
        }

        private Dictionary<string, Performance> ClientTxPerformance = new Dictionary<string, Performance>();
        private Dictionary<string, Performance> ClientRxPerformance = new Dictionary<string, Performance>();

        public void ClientTxPerformanceIncrement(string command)
        {
            if (!Performance.IsPerformanceEnabled)
            {
                return;
            }

            Performance performance = null;
            if (!ClientTxPerformance.TryGetValue(command, out performance))
            {
                try
                {
                    performance = Performance.CreatePerformance("_Ctx" + command.ToUpper());
                    ClientTxPerformance[command] = performance;
                }
                catch { }
            }
            if (performance != null)
            {
                performance.Count(DateTime.Now.Ticks / 10000 / 1000);
            }
        }

        public void ClientRxPerformanceIncrement(string command)
        {
            if (!Performance.IsPerformanceEnabled)
            {
                return;
            }
            Performance performance = null;
            if (!ClientRxPerformance.TryGetValue(command, out performance))
            {
                try
                {
                    performance = Performance.CreatePerformance("_Crx" + command.ToUpper());
                    ClientRxPerformance[command] = performance;
                }
                catch { }
            }
            if (performance != null)
            {
                performance.Count(DateTime.Now.Ticks / 10000 / 1000);
            }
        }

        private void SendCommand(string commandName, EventHandler<SocketAsyncEventArgs> callback, params string[] args)
        {
            if (commandName == null)
            {
                return;
            }
            DateTime sendCommandStartTime = DateTime.Now;
            StringBuilder format = new StringBuilder("|" + Encode(commandName));

            if ((args != null) && (args.Length > 0))
            {
                for (int i = 0; i < args.Length; i++)
                {
                    format.AppendFormat("/{0}", Encode(args[i]));
                }
            }
            format.Append("/?");

            byte[] message = ASCIIEncoding.ASCII.GetBytes(format.ToString());

            SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.SetBuffer(message, 0, message.Length);
            socketAsyncEventArgs.Completed += (sender, e) =>
                {
                    try
                    {
                        sendCommandTimeSpent = (DateTime.Now - sendCommandStartTime);
                        if (callback != null)
                        {
                            callback(sender, e);
                        }
                    }
                    catch (Exception)
                    {
                        // TODO: log
                        if (socketAsyncEventArgs != null)
                        {
                            socketAsyncEventArgs.Dispose();
                        }
                    }
                };
            if ((this.EndPoint == null) || (this.EndPoint.Client == null))
            {
                return;
            }
            if (this.EndPoint.Client.SendAsync(socketAsyncEventArgs))
            {
                sendCommandTimeSpent = (DateTime.Now - sendCommandStartTime);
                if (callback != null)
                {
                    callback(this, socketAsyncEventArgs);
                }
            }
            ClientTxPerformanceIncrement(commandName);
        }

        public static string Encode(string text)
        {
            return text.Replace(',', '_').Replace('|', '!');
        }

        public static string Decode(string text)
        {
            return text; // TODO one-way encoder
        }

        private TimeSpan sendCommandTimeSpent = TimeSpan.Zero;

        private void OnPing()
        {
            byte[] pingResponse = ASCIIEncoding.ASCII.GetBytes("|PONG/?");
            this.EndPoint.Client.Send(pingResponse, SocketFlags.None);
        }

        private void OnDisconnected()
        {
            if (this.Disconnected != null)
            {
                Disconnected(this, new EventArgs());
            }
        }

        public void CoerceActivation()
        {
            SendCommand("CACT",
                (sender, e) =>
                {
                    // NOP
                });
        }

        public event EventHandler<EventArgs> ClientActivated;

        public void NotifyClientActivated()
        {
            try
            {
                if (ClientActivated != null)
                {
                    ClientActivated(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            SendCommand("ACTV",
                (sender, e) =>
                {
                    // NOP
                });
        }
    }
}