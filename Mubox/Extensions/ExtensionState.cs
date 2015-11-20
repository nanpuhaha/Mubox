using Mubox.Extensibility;
using System;

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