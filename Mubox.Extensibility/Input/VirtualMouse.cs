using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensibility.Input
{
    public class VirtualMouse
        : MarshalByRefObject
    {
        public event EventHandler<MouseEventArgs> InputReceived;
        private MuboxClientBridge muboxClientBridge;

        public VirtualMouse(MuboxClientBridge muboxClientBridge)
        {
            // TODO: Complete member initialization
            this.muboxClientBridge = muboxClientBridge;
        }

        internal void OnInputReceived(IMuboxClient sender, MouseEventArgs e)
        {
            if (InputReceived != null)
            {
                InputReceived(sender, e);
            }
        }
    }
}
