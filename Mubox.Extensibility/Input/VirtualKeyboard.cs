using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensibility.Input
{
    public class VirtualKeyboard
        : MarshalByRefObject
    {
        public event EventHandler<KeyboardEventArgs> InputReceived;
        
        private MuboxClientBridge _muboxClientBridge;

        public VirtualKeyboard(MuboxClientBridge muboxClientBridge)
        {
            // TODO: Complete member initialization
            _muboxClientBridge = muboxClientBridge;
        }

        internal void OnInputReceived(IMuboxClient sender, KeyboardEventArgs e)
        {
            if (InputReceived != null)
            {
                InputReceived(sender, e);
            }
        }

        public void KeyPress(WinAPI.VK key)
        {
            _muboxClientBridge.KeyPress(key);
        }

        public void MouseEvent(MouseEventArgs e)
        {
            _muboxClientBridge.MouseEvent(e);
        }

        public override object InitializeLifetimeService()
        {
            return this.InitializeDefaultLease();
        }
    }
}
