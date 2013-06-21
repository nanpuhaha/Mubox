using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensibility
{
    /// <summary>
    /// <para>This object allows an Extension to interact with Mubox</para>
    /// <para>One instance of this object exists for each extension.</para>
    /// </summary>
    public class MuboxBridge
        : MarshalByRefObject, IMubox, IServiceProvider
    {
        public MuboxBridge()
        {
            Clients = new List<IMuboxClient>();
            Keyboard = new Input.VirtualKeyboard(null);
            Mouse = new Input.VirtualMouse(null);
        }

        public Input.VirtualKeyboard Keyboard { get; private set; }

        public Input.VirtualMouse Mouse { get; private set; }

        public ICollection<IMuboxClient> Clients { get; private set; }

        public IMuboxClient ActiveClient { get; private set; }

        public event EventHandler<ClientEventArgs> ActiveClientChanged;

        internal void OnActiveClientChanged(IMuboxClient sender)
        {
            ActiveClient = sender;
            if (ActiveClientChanged != null)
            {
                ActiveClientChanged(sender, new ClientEventArgs
                {
                    Client = sender,
                });
            }
        }

        public override object InitializeLifetimeService()
        {
            return this.InitializeDefaultLease();
        }

        private List<IServiceProvider> _serviceProvider = new List<IServiceProvider>();

        public void AddServiceProvider(IServiceProvider provider)
        {
            _serviceProvider.Add(provider);
        }

        public void RemoveServiceProvider(IServiceProvider provider)
        {
            _serviceProvider.Remove(provider);
        }

        public object GetService(Type serviceType)
        {
            if (_serviceProvider != null)
            {
                foreach (var provider in _serviceProvider)
                {
                    try
                    {
                        var result = provider.GetService(serviceType);
                        if (result != null)
                        {
                            // services should be proxied, or will need to implement custom leases
                            return result;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            return null;
        }
    }
}
