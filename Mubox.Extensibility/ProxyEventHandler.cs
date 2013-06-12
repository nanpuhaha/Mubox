using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensibility
{
    public class ProxyEventHandler<TEventArgs>
        : MarshalByRefObject
    {
        public EventHandler<TEventArgs> Handler { get; protected set; }

        public ProxyEventHandler(EventHandler<TEventArgs> handler)
        {
            Handler = handler;
        }

        public void Proxy(object sender, TEventArgs args)
        {
            if (Handler != null)
            {
                Handler(sender, args);
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
