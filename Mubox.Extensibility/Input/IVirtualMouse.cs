using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mubox.Extensibility.Input
{
    /// <summary>
    /// <para>Virtual Mouse Interface</para>
    /// </summary>
    public interface IVirtualMouse
    {
        event EventHandler<MouseEventArgs> InputReceived;
    }
}
