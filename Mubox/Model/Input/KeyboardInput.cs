using System.Runtime.Serialization;

namespace Mubox.Model.Input
{
    [DataContract]
    public class KeyboardInput
        : StationInput
    {
        public static KeyboardInput CreateFrom(WinAPI.WM wParam, Mubox.WinAPI.WindowHook.KBDLLHOOKSTRUCT hookStruct)
        {
            KeyboardInput e = new KeyboardInput();
            e.VK = hookStruct.vkCode;
            e.Scan = hookStruct.scanCode;
            e.Flags = hookStruct.flags;
            e.Time = hookStruct.time;
            e.WM = wParam;
            return e;
        }

        [DataMember]
        public uint VK { get; set; }

        [DataMember]
        public WinAPI.CAS CAS { get; set; }

        [DataMember]
        public uint Scan { get; set; }

        [DataMember]
        public WinAPI.WindowHook.LLKHF Flags { get; set; }

        [DataMember]
        public WinAPI.WM WM { get; set; }

        public override string ToString()
        {
            return string.Format("VK/{0}/{1}/{2}/{3}/{4}/{5}/{6}",
                        VK,
                        (uint)Scan,
                        (uint)Flags,
                        (uint)Time,
                        ((uint)WM).ToString(),
                        ((byte)CAS).ToString(),
                        base.ToString());
        }
    }
}