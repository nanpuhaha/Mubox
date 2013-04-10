using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mubox.Extensibility.Input
{
    [Serializable]
    public class MouseEventArgs
        : ClientEventArgs
    {
        public bool Handled { get; set; }

        public uint Time { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public WinAPI.WM WM { get; set; }

        public WinAPI.SendInputApi.MouseEventFlags Flags { get; set; }

        public bool IsAbsolute { get; set; }

        public override string ToString()
        {
            return string.Format("wm={0} x={1} y={2} ",
                WM,
                X,
                Y);
        }
    }
}
