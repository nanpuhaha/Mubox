using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensibility.Input
{
    public class VirtualKeyboard
        : MarshalByRefObject, IVirtualKeyboard
    {
        public event EventHandler<KeyboardEventArgs> InputReceived;
        private MuboxClientBridge muboxClientBridge;

        public VirtualKeyboard(MuboxClientBridge muboxClientBridge)
        {
            // TODO: Complete member initialization
            this.muboxClientBridge = muboxClientBridge;
        }

        internal void OnInputReceived(IMuboxClient sender)
        {
            if (InputReceived != null)
            {
                InputReceived(sender, new KeyboardEventArgs
                {
                    Client = sender,
                });
            }
        }
    }
}
