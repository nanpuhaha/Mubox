using System.Runtime.Remoting.Lifetime;

namespace System
{
    public static class LeaseExtensions
    {
        public static object InitializeLease(this object obj)
        {
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