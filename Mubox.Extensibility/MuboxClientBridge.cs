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
            Action<Input.KeyboardEventArgs> onKeyboardEventRequest,
            Action<Input.MouseEventArgs> onMouseEventRequest)
        {
            _raiseKeyboardEvent = onKeyboardEventRequest;
            _raiseMouseEvent = onMouseEventRequest;
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
            lease.InitialLeaseTime = TimeSpan.FromHours(1);
            lease.RenewOnCallTime = TimeSpan.FromHours(1);
            lease.SponsorshipTimeout = TimeSpan.FromHours(1);
            return lease;
        }

        public Action<Input.KeyboardEventArgs> _raiseKeyboardEvent;
        public Action<Input.MouseEventArgs> _raiseMouseEvent;

        public void KeyboardEvent(Input.KeyboardEventArgs e)
        {
            if (_raiseKeyboardEvent != null)
            {
                _raiseKeyboardEvent(e);
            }
        }

        public void MouseEvent(Input.MouseEventArgs e)
        {
            if (_raiseMouseEvent != null)
            {
                _raiseMouseEvent(e);
            }
        }
    }
}
