using Mubox.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensions
{
    public sealed class ExtensionState
    {
        public string Name { get; internal set; }
        public AppDomain AppDomain { get; internal set; }
        public Loader Loader { get; internal set; }
        public IMubox Bridge { get; internal set; }
    }
}
