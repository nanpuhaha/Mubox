﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensions
{
    public sealed class ExtensionManager
    {
        private IDictionary<string /*DllName*/, ExtensionState> _extensions;
        private ICollection<Extensibility.MuboxClientBridge> _clients;

        public IEnumerable<ExtensionState> Extensions
        {
            get
            {
                return _extensions
                    .Values
                    .ToList();
            }
        }

        public ExtensionManager()
        {
            _extensions = new Dictionary<string, ExtensionState>();
            _clients = new List<Extensibility.MuboxClientBridge>();
        }

        public void Initialize()
        {
            Extensibility.TraceExtensions.Log("Extension Manager Initialize");
            Mubox.Control.Network.Server.ClientAccepted += Server_ClientAccepted;
            Mubox.Control.Network.Server.ClientRemoved += Server_ClientRemoved;

            // TODO: to support multiple teams, this event handler & related will need to be refactored
            Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam.ActiveClientChanged += ActiveTeam_ActiveClientChanged;

            var extensionsPath = Path.Combine(Environment.CurrentDirectory, "Extensions");
            var files = System.IO.Directory.EnumerateFiles(extensionsPath, "*.dll");
            foreach (var file in files)
            {
                var friendlyName = Path.GetFileNameWithoutExtension(file);
                var appDomain = AppDomain.CreateDomain(friendlyName);
                var bridge = new Extensibility.MuboxBridge();
                var loader = (Extensibility.Loader)appDomain
                    .CreateInstanceAndUnwrap("Mubox.Extensibility", "Mubox.Extensibility.Loader");
                _clients.ToList()
                    .ForEach(bridge.Clients.Add);
                var extensionState = new ExtensionState()
                {
                    Name = friendlyName,
                    AppDomain = appDomain,
                    Loader = loader,
                    Bridge = bridge,
                };
                _extensions.Add(extensionState.Name, extensionState);

                // actual extension load and initializes at this point
                loader.Initialize(bridge, file);
            }
        }

        void ActiveTeam_ActiveClientChanged(object sender, EventArgs e)
        {
            var clientName = Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam.ActiveClient.Name;
            var client = _clients
                .Where(c => c.Name == clientName)
                .FirstOrDefault();
            _extensions.Values
                .ToList()
                .ForEach(ext =>
                    {
                        (ext.Bridge as Extensibility.MuboxBridge).OnActiveClientChanged(client);
                    });
        }

        public void Shutdown()
        {
            Extensibility.TraceExtensions.Log("Extension Manager Shutdown");
            Mubox.Control.Network.Server.ClientAccepted -= Server_ClientAccepted;
            Mubox.Control.Network.Server.ClientRemoved -= Server_ClientRemoved;
        }

        public void Start(string name)
        {
            foreach (var ext in _extensions.Values)
            {
                if (ext.Name == name)
                {
                    // NOP:
                }
            }
        }

        public void Stop(string name)
        {
            var state = default(ExtensionState);
            if (_extensions.TryGetValue(name, out state))
            {
                state.Loader.ExtensionStop();
            }
        }

        void Server_ClientAccepted(object sender, Control.Network.Server.ServerEventArgs e)
        {
            e.Client.IsAttachedChanged += Client_IsAttachedChanged;
            var client = new Extensibility.MuboxClientBridge
            {
                Name = "",
            };
            e.Client.DisplayNameChanged += (dnc_s, dnc_e) =>
            {
                //System.Windows.Application.Current.Dispatcher.Invoke(() =>
                //{
                    client.Name = (dnc_s as Mubox.Model.Client.ClientBase).DisplayName;
                //});
            };
            _clients.Add(client);
            _extensions.Values.ToList()
                .ForEach(ext =>
                {
                    ext.Bridge.Clients.Add(client);
                });
        }

        void Server_ClientRemoved(object sender, Control.Network.Server.ServerEventArgs e)
        {
            e.Client.IsAttachedChanged -= Client_IsAttachedChanged;
            var client = _clients
                .Where(c => c.Name.Equals(e.Client.DisplayName))
                .FirstOrDefault();
            if (client != null)
            {
                _clients.Remove(client);
                _extensions.Values.ToList()
                    .ForEach(ext =>
                    {
                        ext.Bridge.Clients.Remove(client);
                    });
            }
        }

        void Client_IsAttachedChanged(object sender, EventArgs e)
        {
            var based = sender as Mubox.Model.Client.ClientBase;
            var basedName = default(string);
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    basedName = based.DisplayName;
                });
            var client = _clients
                .Where(c => c.Name.Equals(basedName))
                .FirstOrDefault();
            if (client != null)
            {
                if (based.IsAttached)
                {
                    client.OnAttached();
                }
                else
                {
                    client.OnDetached();
                }
            }
        }

        public void StartAll()
        {
            throw new NotImplementedException();
        }
    }
}
