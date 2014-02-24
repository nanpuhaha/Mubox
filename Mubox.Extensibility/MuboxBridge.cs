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
    /// <para>The instance lives on the Mubox side, not on the Extension side.</para>
    /// </summary>
    public class MuboxBridge
        : MarshalByRefObject, IMubox, IServiceProvider
    {
        // allows an extensions to get services from other extensions managed by Mubox
        private IServiceProvider _serviceProvider;

        public MuboxBridge(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            LoggingExtensions.ServiceProvider = serviceProvider; // gives Trace extensions indirect access to Console Extension
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
            return base.InitializeLifetimeService().InitializeLease();
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }
    }
}
