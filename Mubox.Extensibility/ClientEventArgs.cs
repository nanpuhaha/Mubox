using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mubox.Extensibility
{
    [Serializable]
    public class ClientEventArgs
        : EventArgs
    {
        public IMuboxClient Client { get; set; }
    }
}
