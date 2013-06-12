using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensibility
{
    /// <summary>
    /// <para>This object allows an Extension to interact with Mubox</para>
    /// <para>One instance of this object exists for each extension.</para>
    /// </summary>
    public class MuboxBridge
        : MarshalByRefObject, IMubox
    {
        public MuboxBridge()
        {
            Clients = new List<IMuboxClient>();
            Keyboard = new Input.VirtualKeyboard(null);
            Mouse = new Input.VirtualMouse(null);
        }

        public Input.VirtualKeyboard Keyboard { get; private set; }

        public Input.VirtualMouse Mouse { get; private set; }

        public ICollection<IMuboxClient> Clients { get; private set; }

        public IMuboxClient ActiveClient { get; private set; }

        public event EventHandler<ClientEventArgs> ActiveClientChanged;

        internal void OnActiveClientChanged(IMuboxClient sender)
        {
            ActiveClient = sender;
            if (ActiveClientChanged != null)
            {
                ActiveClientChanged(sender, new ClientEventArgs
                {
                    Client = sender,
                });
            }
        }

        public override object InitializeLifetimeService()
        {
            var lease = (System.Runtime.Remoting.Lifetime.ILease)base.InitializeLifetimeService();
            lease.InitialLeaseTime = TimeSpan.FromHours(12);
            lease.RenewOnCallTime = TimeSpan.FromHours(12);
            lease.SponsorshipTimeout = TimeSpan.FromHours(12);
            return lease;
        }
    }
}
