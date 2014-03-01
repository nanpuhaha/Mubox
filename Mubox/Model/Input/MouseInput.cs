using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Mubox.Model.Input
{
    [DataContract]
    public class MouseInput
        : StationInput
    {
        public static MouseInput CreateFrom(WinAPI.WM wm, Mubox.WinAPI.WindowHook.MSLLHOOKSTRUCT hookStruct)
        {
            return new MouseInput
            {
                WM = wm,
                Point = new System.Windows.Point(hookStruct.pt.X, hookStruct.pt.Y),
                MouseData = hookStruct.mouseData,
                Time = hookStruct.time,
            };
        }

        [DataMember]
        public bool IsClickEvent { get; set; }

        [DataMember]
        public bool IsDoubleClickEvent { get; set; }

        [DataMember]
        public WinAPI.WM WM { get; set; }

        [DataMember]
        public System.Windows.Point Point { get; set; }

        [DataMember]
        public uint MouseData { get; set; }

        public WinAPI.SendInputApi.MouseEventFlags Flags
        {
            get
            {
                WinAPI.SendInputApi.MouseEventFlags flags = IsAbsolute
                    ? WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_ABSOLUTE
                    : WinAPI.SendInputApi.MouseEventFlags.NotSet;

                switch (WM)
                {
                    case WinAPI.WM.MOUSEMOVE:
                        flags |= WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_MOVE;
                        break;
                    case WinAPI.WM.LBUTTONDOWN:
                        flags |= WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
                        break;
                    case WinAPI.WM.LBUTTONUP:
                        flags |= WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_LEFTUP;
                        break;
                    case WinAPI.WM.RBUTTONDOWN:
                        flags |= WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_RIGHTDOWN;
                        break;
                    case WinAPI.WM.RBUTTONUP:
                        flags |= WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_RIGHTUP;
                        break;
                    case WinAPI.WM.MBUTTONDOWN:
                        flags |= WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_MIDDLEDOWN;
                        break;
                    case WinAPI.WM.MBUTTONUP:
                        flags |= WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_MIDDLEUP;
                        break;
                    case WinAPI.WM.XBUTTONDOWN:
                        flags |= WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_XDOWN;
                        break;
                    case WinAPI.WM.XBUTTONUP:
                        flags |= WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_XUP;
                        break;
                    case WinAPI.WM.MOUSEWHEEL:
                    case WinAPI.WM.MOUSEHWHEEL:
                        flags |= WinAPI.SendInputApi.MouseEventFlags.MOUSEEVENTF_WHEEL;
                        break;
                    case WinAPI.WM.LBUTTONDBLCLK:
                    case WinAPI.WM.MBUTTONDBLCLK:
                    case WinAPI.WM.RBUTTONDBLCLK:
                    case WinAPI.WM.XBUTTONDBLCLK:
                    default:
                        // TODO: ?
                        break;
                }

                return flags;
            }
            set
            {
                Flags = value;
            }
        }

        [DataMember]
        public bool IsAbsolute { get; set; }

        public override string ToString()
        {
            return string.Format("{0}/{1:X}/{2:X}/{3}/{4}/{5}/{6}",
                            IsDoubleClickEvent
                                ? "DBL" // double-click event
                                : IsClickEvent
                                    ? "CLK" // single-click event
                                    : "MS", // non-click event
                            Flags,
                            MouseData,
                            (int)Point.X,
                            (int)Point.Y,
                            Time,
                            base.ToString());
        }
    }
}