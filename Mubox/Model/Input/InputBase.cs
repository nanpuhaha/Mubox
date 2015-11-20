using System;
using System.Runtime.Serialization;

namespace Mubox.Model.Input
{
    [DataContract]
    public abstract class InputBase
        : EventArgs
    {
        private readonly DateTime _createdTime = DateTime.Now;

        public DateTime CreatedTime { get { return _createdTime; } }

        [DataMember]
        public uint Time { get; set; }

        public bool Handled { get; set; }

        public override string ToString()
        {
            return "";
        }
    }
}