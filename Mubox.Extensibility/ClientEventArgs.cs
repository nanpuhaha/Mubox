using System;

namespace Mubox.Extensibility
{
    [Serializable]
    public class ClientEventArgs
        : EventArgs
    {
        public IMuboxClient Client { get; set; }
    }
}