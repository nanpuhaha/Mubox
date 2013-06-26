using Mubox.Extensibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensions
{
    public sealed class ExtensionManager
    {
        public static Mubox.Extensions.ExtensionManager Instance { get; private set; }

        static ExtensionManager()
        {
            Instance = new ExtensionManager();
        }

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

        private void ActiveTeam_ActiveClientChanged(object sender, EventArgs e)
        {
            Task.Factory.StartNew(delegate()
            {
                var client = GetActiveClient();
                _extensions.Values
                    .ToList()
                    .ForEach(ext =>
                        {
                            (ext.Bridge as Extensibility.MuboxBridge).OnActiveClientChanged(client);
                        });
            }, TaskCreationOptions.PreferFairness);
        }

        private Extensibility.MuboxClientBridge GetActiveClient()
        {
            var clientSettings = Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam.ActiveClient;
            return clientSettings == null
                ? null
                : GetClientByName(clientSettings.Name);
        }

        private Extensibility.MuboxClientBridge GetClientByHandle(IntPtr handle)
        {
            var clientSettings = Mubox.Configuration.MuboxConfigSection.Default
                .Teams.ActiveTeam.Clients.OfType<Mubox.Configuration.ClientSettings>()
                .Where(client => client.WindowHandle == handle)
                .FirstOrDefault();
            return clientSettings == null
                ? null
                : GetClientByName(clientSettings.Name);
        }

        private Extensibility.MuboxClientBridge GetClientByName(string clientName)
        {
            return _clients
                .Where(c => c.Name == clientName)
                .FirstOrDefault();
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

        private void Server_ClientAccepted(object sender, Control.Network.Server.ServerEventArgs e)
        {
            Task.Factory.StartNew(delegate()
            {
                e.Client.IsAttachedChanged += Client_IsAttachedChanged;
                var name = "";
                e.Client.Dispatcher.Invoke((Action)delegate()
                {
                    name = e.Client.DisplayName;
                });
                var client = new Extensibility.MuboxClientBridge(
                    (key) =>
                    {
                        e.Client.Dispatch((ushort)key);
                    },
                    (me) =>
                    {
                        e.Client.Dispatch(new Model.Input.MouseInput
                        {
                            // MouseData
                            Flags = me.Flags,
                            IsAbsolute = me.IsAbsolute,
                            Time = me.Time,
                            WM = me.WM,
                            Point = new System.Windows.Point
                            {
                                X = me.X,
                                Y = me.Y,
                            },
                        });
                    })
                {
                    Name = name,
                };
                e.Client.DisplayNameChanged += (dnc_s, dnc_e) =>
                {
                    client.Name = (dnc_s as Mubox.Model.Client.ClientBase).DisplayName;
                };
                _clients.Add(client);
                _extensions.Values.ToList()
                    .ForEach(ext =>
                    {
                        ext.Bridge.Clients.Add(client);
                    });
            }, TaskCreationOptions.PreferFairness);
        }

        private void Server_ClientRemoved(object sender, Control.Network.Server.ServerEventArgs e)
        {
            Task.Factory.StartNew(delegate()
            {
                var name = "";
                e.Client.Dispatcher.Invoke((Action)delegate()
                {
                    name = e.Client.DisplayName;
                });
                e.Client.IsAttachedChanged -= Client_IsAttachedChanged;
                var client = _clients
                    .Where(c => c.Name.Equals(name))
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
            }, TaskCreationOptions.PreferFairness);
        }

        private void Client_IsAttachedChanged(object sender, EventArgs e)
        {
            Task.Factory.StartNew(delegate()
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
            }, TaskCreationOptions.PreferFairness);
        }

        public void StartAll()
        {
            throw new NotImplementedException();
        }

        internal void OnKeyboardInputReceived(object sender, Model.Input.KeyboardInput e)
        {
            Task.Factory.StartNew(delegate()
            {
                // translate input
                var client = GetClientByHandle(e.WindowHandle) ?? GetActiveClient();

                // dispatch to each extension
                _extensions.Values.ToList()
                    .ForEach(ext =>
                    {
                        try
                        {
                            var L_e = new Extensibility.Input.KeyboardEventArgs
                            {
                                Client = client,
                                Handled = e.Handled,
                                CAS = e.CAS,
                                VK = (Mubox.WinAPI.VK)e.VK,
                                WM = e.WM,
                            };
                            ext.Bridge.Keyboard.OnInputReceived(client, L_e);
                            e.Handled = e.Handled || L_e.Handled;
                        }
                        catch (Exception ex)
                        {
                            Extensibility.TraceExtensions.Log(ex);
                        }
                    });
            }, TaskCreationOptions.PreferFairness);
        }

        internal void OnMouseInputReceived(object sender, Model.Input.MouseInput e)
        {
            // do not forward mouse-move events to extensions (they are unecessary, and it wastes cpu)
            // TODO: consider dispatching on a 'tile-snapped' basis, if 'tile under cursor' would change, dispatch a single update
            if (e.WM == WinAPI.WM.MOUSEMOVE)
            {
                return;
            }

            Task.Factory.StartNew(delegate()
            {
                // translate input
                var client = GetClientByHandle(e.WindowHandle) ?? GetActiveClient();

                // dispatch to extensions
                _extensions.Values.ToList()
                    .ForEach(ext =>
                    {
                        try
                        {
                            var L_e = new Extensibility.Input.MouseEventArgs
                            {
                                Client = client,
                                Handled = e.Handled,
                                Time = e.Time,
                                IsAbsolute = e.IsAbsolute,
                                X = e.Point.X,
                                Y = e.Point.Y,
                                WM = e.WM,
                                Flags = e.Flags,
                            };
                            ext.Bridge.Mouse.OnInputReceived(client, L_e);
                            e.Handled = e.Handled || L_e.Handled;
                        }
                        catch (Exception ex)
                        {
                            Extensibility.TraceExtensions.Log(ex);
                        }
                    });
            }, TaskCreationOptions.PreferFairness);
        }
    }
}