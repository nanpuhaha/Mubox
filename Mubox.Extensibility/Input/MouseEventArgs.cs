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
    }
}
