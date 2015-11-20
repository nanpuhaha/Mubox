using System;

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
            lease.InitialLeaseTime = TimeSpan.FromHours(72);
            lease.RenewOnCallTime = TimeSpan.FromHours(72);
            lease.SponsorshipTimeout = TimeSpan.FromHours(72);
            return lease;
        }
    }
}