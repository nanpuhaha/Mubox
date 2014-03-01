using System;
using System.Runtime.Serialization;

namespace Mubox.Model.Input
{
    [DataContract]
    public abstract class StationInput
        : InputBase
    {
        [DataMember]
        public IntPtr WindowStationHandle { get; set; }

        [DataMember]
        public IntPtr WindowDesktopHandle { get; set; }

        [DataMember]
        public IntPtr WindowHandle { get; set; }

        public override string ToString()
        {
            return string.Format("{0:X}/{1:X}/{2:X}/{3}",
                WindowStationHandle.ToString(),
                WindowDesktopHandle.ToString(),
                WindowHandle.ToString(),
                base.ToString());
        }
    }
}