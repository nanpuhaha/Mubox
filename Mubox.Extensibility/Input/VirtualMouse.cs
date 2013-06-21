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
        private MuboxClientBridge _muboxClientBridge;

        public event EventHandler<MouseEventArgs> InputReceived;

        public VirtualMouse(MuboxClientBridge muboxClientBridge)
        {
            // TODO: Complete member initialization
            _muboxClientBridge = muboxClientBridge;
        }

        internal void OnInputReceived(IMuboxClient sender, MouseEventArgs e)
        {
            if (InputReceived != null)
            {
                InputReceived(sender, e);
            }
        }

        public override object InitializeLifetimeService()
        {
            return this.InitializeDefaultLease();
        }
    }
}
