using System;
using System.Runtime.Serialization;

namespace Mubox.Model.Input
{
    /// <summary>
    /// State Information for a Button (e.g. a Mouse Button)
    /// </summary>
    [DataContract]
    public class ButtonState
    {
        /// <summary>
        /// True if the button is currently down.
        /// </summary>
        public bool IsDown { get { return LastDownTimestamp.Ticks > LastUpTimestamp.Ticks; } }

        /// <summary>
        /// Timestamp of the last time IsDown transitioned into a True state from a False state.
        /// </summary>
        public DateTime LastDownTimestamp { get; set; }

        /// <summary>
        /// Timestamp of the last time IsDown transitioned into a False state from a True state.
        /// </summary>
        public DateTime LastUpTimestamp { get; set; }

        /// <summary>
        /// Timestamp of the last time IsClick transitioned into a True state from a False state.
        /// </summary>
        public DateTime LastClickTimestamp { get; set; }

        /// <summary>
        /// Timestamp of the last time IsDoubleClick transitioned into a True state from a False state.
        /// </summary>
        public DateTime LastDoubleClickTimestamp { get; set; }

        /// <summary>
        /// True if the button transitioned from a Down state to an Up state, as an 'atomic' "Click" gesture.
        /// </summary>
        public bool IsClick { get; set; }

        /// <summary>
        /// True if the button transitioned from a Down state to an Up state, twice, as an 'atomic' "Double Click" gesture.
        /// </summary>
        public bool IsDoubleClick { get; set; }

        /// <summary>
        /// True if the button is Down due to a Multicast (e.g. Mouse Multicast, Key Multicast).
        /// </summary>
        public bool IsMulticast { get; set; }
    }
}