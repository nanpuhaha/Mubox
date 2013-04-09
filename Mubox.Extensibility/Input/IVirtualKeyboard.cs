using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mubox.Extensibility.Input
{
    /// <summary>
    /// <para>Virtual Keyboard Interface</para>
    /// </summary>
    public interface IVirtualKeyboard
    {
        event EventHandler<KeyboardEventArgs> InputReceived;
    }
}
