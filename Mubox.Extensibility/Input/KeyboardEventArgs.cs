using System;

namespace Mubox.Extensibility.Input
{
    [Serializable]
    public class KeyboardEventArgs
        : ClientEventArgs
    {
        public bool Handled { get; set; }

        public Mubox.WinAPI.CAS CAS { get; set; }

        public Mubox.WinAPI.VK VK { get; set; }

        public Mubox.WinAPI.WM WM { get; set; }

        public override string ToString()
        {
            return string.Format("wm={0} vk={1} cas={2} ",
                WM,
                VK,
                CAS);
        }
    }
}