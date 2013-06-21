using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class LeaseExtensions
    {
        public static object InitializeDefaultLease<T>(this T mbro)
            where T : MarshalByRefObject
        {
            var obj = mbro.InitializeLifetimeService();
            var lease = obj as ILease;
            if (lease != null)
            {
                lease.InitialLeaseTime = TimeSpan.FromHours(24);
                lease.RenewOnCallTime = TimeSpan.FromHours(24);
                lease.SponsorshipTimeout = TimeSpan.FromHours(24);
            }
            return obj;
        }
    }
}
