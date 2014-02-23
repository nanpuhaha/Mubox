using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensibility
{
    public class MuboxClientBridge
        : MarshalByRefObject, IMuboxClient
    {
        public MuboxClientBridge(
            Action<WinAPI.VK> onDoKeyPress,
            Action<Input.MouseEventArgs> onDoMouseClick)
        {
            _doKeyPress = onDoKeyPress;
            _doMouseClick = onDoMouseClick;
            Keyboard = new Input.VirtualKeyboard(this);
            Mouse = new Input.VirtualMouse(this);
        }

        public string Name { get; internal set; }

        public Input.VirtualKeyboard Keyboard { get; private set; }
        public Input.VirtualMouse Mouse { get; private set; }

        public event EventHandler<ClientEventArgs> Attached;

        internal void OnAttached()
        {
            if (Attached != null)
            {
                Attached(this, new ClientEventArgs
                {
                    Client = this,
                });
            }
        }

        public event EventHandler<ClientEventArgs> Detached;

        internal void OnDetached()
        {
            if (Detached != null)
            {
                Detached(this, new ClientEventArgs
                {
                    Client = this,
                });
            }
        }

        public override object InitializeLifetimeService()
        {
            var lease = (System.Runtime.Remoting.Lifetime.ILease)base.InitializeLifetimeService();
            lease.InitialLeaseTime = TimeSpan.FromHours(72);
            lease.RenewOnCallTime = TimeSpan.FromHours(72);
            lease.SponsorshipTimeout = TimeSpan.FromHours(72);
            return lease;
        }

        public Action<WinAPI.VK> _doKeyPress;
        public Action<Input.MouseEventArgs> _doMouseClick;

        public void KeyPress(WinAPI.VK key)
        {
            if (_doKeyPress != null)
            {
                _doKeyPress(key);
            }
        }

        public void MouseEvent(Input.MouseEventArgs e)
        {
            if (_doMouseClick != null)
            {
                _doMouseClick(e);
            }
        }
    }
}
