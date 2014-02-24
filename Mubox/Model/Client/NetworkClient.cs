using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using Mubox.Control;
using Mubox.Model.Input;
using System.Linq;
using Mubox.Extensibility;

namespace Mubox.Model.Client
{
    public class NetworkClient : ClientBase
    {
        #region Networking Code

        private Socket ClientSocket { get; set; }

        private Dictionary<string, Performance> ServerTxPerformance = new Dictionary<string, Performance>();
        private Dictionary<string, Performance> ServerRxPerformance = new Dictionary<string, Performance>();

        public void ServerTxPerformanceIncrement(string command)
        {
            if (!Performance.IsPerformanceEnabled)
            {
                return;
            }

            Performance performance = null;
            if (!ServerTxPerformance.TryGetValue(command, out performance))
            {
                try
                {
                    performance = Performance.CreatePerformance("_Stx" + command.ToUpper());
                    ServerTxPerformance[command] = performance;
                }
                catch { }
            }
            if (performance != null)
            {
                performance.Count(DateTime.Now.Ticks / 10000 / 1000);
            }
        }

        public void ServerRxPerformanceIncrement(string command)
        {
            if (!Performance.IsPerformanceEnabled)
            {
                return;
            }

            Performance performance = null;
            if (!ServerRxPerformance.TryGetValue(command, out performance))
            {
                try
                {
                    performance = Performance.CreatePerformance("_Srx" + command.ToUpper());
                    ServerRxPerformance[command] = performance;
                }
                catch { }
            }
            if (performance != null)
            {
                performance.Count(DateTime.Now.Ticks / 10000 / 1000);
            }
        }

        private string commandFragment = "";

        private void BeginReceiveCallback(IAsyncResult ar)
        {
            object[] args = ar.AsyncState as object[];
            Socket socket = (args[0] as Socket) ?? ClientSocket;
            string[] commands = null;
            try
            {
                if (socket != null)
                {
                    int cb = socket.EndReceive(ar);
                    byte[] receiveBuffer = args[1] as byte[];
                    string text = commandFragment + System.Text.ASCIIEncoding.ASCII.GetString(receiveBuffer, 0, cb);
                    commands = text.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                }
                commandFragment = "";
            }
            catch (Exception)
            {
                if (socket != null && socket == ClientSocket)
                {
                    Detach();
                }
            }
            if ((commands == null) || (commands.Length == 0))
            {
                return;
            }
            foreach (string command in commands)
            {
                if (!command.EndsWith("?"))
                {
                    commandFragment = command;
                    continue;
                }
                string[] parameters = command.Split('/');
                ServerRxPerformanceIncrement(parameters[0]);
                switch (parameters[0])
                {
                    case "NAME":
                        OnNameReceived(parameters);
                        break;
                    case "PONG":
                        OnPongReceived();
                        break;
                    case "STAT":
                        // ping client from non-ui thread
                        OnPerformanceInfoReceived(parameters);
                        break;
                    case "CACT":
                        OnCoerceActivationReceived();
                        break;
                    case "DC":
                        OnDetachReceived();
                        break;
                    case "ACTV":
                        OnClientActivated();
                        break;
                    default:
                        // TODO log unknown command
                        break;
                }
            }
            Attach();
        }

        public event EventHandler<EventArgs> ClientActivated;

        private void OnClientActivated()
        {
            if (ClientActivated != null)
            {
                try
                {
                    ClientActivated(this, new EventArgs());
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }
        }

        private void OnDetachReceived()
        {
            try
            {
                Detach();
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        private void OnCoerceActivationReceived()
        {
            try
            {
                if (CoerceActivation != null)
                {
                    CoerceActivation(this, new EventArgs());
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        private void OnPerformanceInfoReceived(string[] parameters)
        {
            System.Threading.ThreadPool.QueueUserWorkItem((System.Threading.WaitCallback)delegate(Object state)
            {
                try
                {
                    IntPtr[] handles = state as IntPtr[];
                    Ping(handles[0], handles[1], handles[2]);
                }
                catch (Exception ex)
                {
                    ex.Log();
                    Detach();
                }
            }, new IntPtr[] { this.WindowStationHandle, this.WindowDesktopHandle, this.WindowHandle });

            this.Dispatcher.BeginInvoke((System.Threading.ThreadStart)delegate()
            {
                try
                {
                    if (this.PerformanceInfo == null)
                    {
                        this.PerformanceInfo = new PerformanceInfo();
                    }
                    if (parameters.Length > 2)
                    {
                        this.PerformanceInfo.MainWindowTitle = parameters[1];
                        this.PerformanceInfo.ProcessId = int.Parse(parameters[2]);
                        this.PerformanceInfo.ProcessName = parameters[3];
                        this.PerformanceInfo.IsWindowResponding = bool.Parse(parameters[4]) ? "" : "Not Responding";
                        try
                        {
                            this.PerformanceInfo.WorkingSet = long.Parse(parameters[5]) / 1048576;
                            this.PerformanceInfo.PeakWorkingSet = long.Parse(parameters[6]) / 1048576;
                            this.PerformanceInfo.VirtualMemorySize = long.Parse(parameters[7]) / 1048576;
                            this.PerformanceInfo.PeakVirtualMemorySize = long.Parse(parameters[8]) / 1048576;
                            long networkSendTimeAverage = ((this.PerformanceInfo.NetworkSendTime * 2) + (long.Parse(parameters[9]) / 10000)) / 3;
                            this.PerformanceInfo.NetworkSendTime = networkSendTimeAverage;
                        }
                        catch (Exception ex)
                        {
                            ex.Log();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            });
        }

        private void OnPongReceived()
        {
            this.Dispatcher.BeginInvoke((System.Threading.ThreadStart)delegate()
            {
                try
                {
                    this.Latency = (long)TimeSpan.FromTicks(DateTime.Now.Ticks - this.PingSendTimestampTicks).TotalMilliseconds;
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            });
        }

        private void OnNameReceived(string[] parameters)
        {
            this.WindowStationHandle = parameters.Length > 2 ? new IntPtr(int.Parse(parameters[2])) : IntPtr.Zero;
            this.WindowDesktopHandle = parameters.Length > 3 ? new IntPtr(int.Parse(parameters[3])) : IntPtr.Zero;
            this.WindowHandle = parameters.Length > 4 ? new IntPtr(int.Parse(parameters[4])) : IntPtr.Zero;
            this.Dispatcher.BeginInvoke((System.Threading.ThreadStart)delegate()
            {
                try
                {
                    if (this.ClientSocket != null)
                    {
                        this.Address = ((System.Net.IPEndPoint)ClientSocket.RemoteEndPoint).Address.ToString();
                    }
                    var parts = parameters[1].Split('_');
                    this.DisplayName = parts[0];
                    this.ProfileName = parts[1];
                    foreach (var L_profile in Mubox.Configuration.MuboxConfigSection.Default.Profiles.Cast<Mubox.Configuration.ProfileSettings>())
                    {
                        if (L_profile.Name == this.ProfileName)
                        {
                            Mubox.Configuration.ClientSettings settings = L_profile.Clients.GetExisting(DisplayName);
                            if (settings != null)
                            {
                                ProfileName = L_profile.Name;
                                break;
                            }
                            else
                            {
                                settings = L_profile.Clients.CreateNew(this.DisplayName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            });
        }

        public event EventHandler<EventArgs> CoerceActivation;

        private System.Runtime.Serialization.DataContractSerializer Serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(Model.Input.InputBase),
            new Type[]
            {
                typeof(Model.Input.CommandInput),
                typeof(Model.Input.KeyboardInput),
                typeof(Model.Input.MouseInput),
            });

        private void Send(InputBase input)
        {
            try
            {
                if (ClientSocket == null || !ClientSocket.Connected)
                {
                    // not connected
                    throw new SocketException();
                }

                byte[] buf = null;

                var stream = new System.IO.MemoryStream();
                using (var writer = new System.IO.BinaryWriter(stream))
                {
                    stream.Seek(3, System.IO.SeekOrigin.Begin);
                    Serializer.WriteObject(stream, input);
                    var len = (ushort)(stream.Position - 3);
                    stream.Seek(0, System.IO.SeekOrigin.Begin);
                    stream.WriteByte(0x1b);
                    writer.Write(len);
                    buf = stream.ToArray();
                }

                SocketError socketError;
                ClientSocket.BeginSend(buf, 0, buf.Length, SocketFlags.None, out socketError, BeginSendCallback, ClientSocket);
            }
            catch
            {
                this.Detach();
                throw;
            }
        }

        private void BeginSendCallback(IAsyncResult ar)
        {
            Socket socket = (ar.AsyncState as Socket) ?? ClientSocket;
            try
            {
                if (socket != null)
                {
                    socket.EndSend(ar);
                }
            }
            catch
            {
                if (socket != null && socket == ClientSocket)
                {
                    Detach();
                }
            }
        }

        #endregion

        public NetworkClient(Socket socket, string profileName)
            : base(profileName)
        {
            this.ClientSocket = socket;
        }

        public override void Dispatch(MouseInput e)
        {
#if DEBUG
            ("Dispatch(" + _displayName + ") " + Convert.ToString(e)).Log();
#endif
            ServerTxPerformanceIncrement("MOUSE");
            try
            {
                Send(e);
                base.Dispatch(e);
            }
            catch
            {
                Control.Network.Server.RemoveClient(this);
            }
        }

        public override void Dispatch(KeyboardInput e)
        {
#if DEBUG
            ("Dispatch(" + _displayName + ") " + Convert.ToString(e)).Log();
#endif
            ServerTxPerformanceIncrement("KB");
            try
            {
                Send(e);
                base.Dispatch(e);
            }
            catch
            {
                Control.Network.Server.RemoveClient(this);
            }
        }

        public override void Dispatch(CommandInput e)
        {
#if DEBUG
            ("Dispatch(" + _displayName + ") " + Convert.ToString(e)).Log();
#endif
            ServerTxPerformanceIncrement("CMD");
            try
            {
                Send(e);
            }
            catch
            {
                Control.Network.Server.RemoveClient(this);
            }
        }

        public override void Activate()
        {
            ServerTxPerformanceIncrement("AC");
            base.Activate();
            this.Dispatch(new CommandInput
            {
                Text = "AC",
                WindowStationHandle = this.WindowStationHandle,
                WindowDesktopHandle = this.WindowDesktopHandle,
                WindowHandle = this.WindowHandle
            });
        }

        public override void Deactivate()
        {
            ServerTxPerformanceIncrement("DA");
            this.Dispatch(new CommandInput
            {
                Text = "DA",
                WindowStationHandle = this.WindowStationHandle,
                WindowDesktopHandle = this.WindowDesktopHandle,
                WindowHandle = this.WindowHandle
            });
        }

        private bool ConnectAlreadySent { get; set; }

        public DateTime NextPingTime { get; set; }

        public override void Attach()
        {
            try
            {
                SocketError socketError;
                byte[] receiveBuffer = new byte[4096]; // TODO: arbitrary buffer size, should adjust if we detect excessive fragmentation
                ClientSocket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, out socketError, BeginReceiveCallback, new object[] { ClientSocket, receiveBuffer });
                if (socketError != SocketError.Success)
                {
                    throw new SocketException((int)socketError);
                }
                if (!ConnectAlreadySent)
                {
                    ConnectAlreadySent = true;
                    //byte[] connectCommand = System.Text.ASCIIEncoding.ASCII.GetBytes("|CN/\x1b");
                    //ClientSocket.Send(connectCommand, SocketFlags.None);
                }
                base.Attach();
            }
            catch (Exception ex)
            {
                ex.Log();
                this.Detach();
            }
        }

        public override void Detach()
        {
            // force disconnect client
            try
            {
                Control.Network.Server.RemoveClient(this);
                if (ClientSocket != null && ClientSocket.Connected)
                {
                    try
                    {
                        this.Dispatch(new CommandInput
                        {
                            Text = "DC",
                            WindowStationHandle = this.WindowStationHandle,
                            WindowDesktopHandle = this.WindowDesktopHandle,
                            WindowHandle = this.WindowHandle
                        });
                    }
                    catch { }
                    try
                    {
                        ClientSocket.Shutdown(SocketShutdown.Receive);
                        ClientSocket.Close(1);
                    }
                    catch { }
                    try
                    {
                        ClientSocket.Disconnect(false);
                    }
                    catch { }
                    ClientSocket = null;
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            finally
            {
                ConnectAlreadySent = false;
                base.Detach();
            }
        }

        public override void Ping(IntPtr windowStationHandle, IntPtr windowDesktopHandle, IntPtr windowHandle)
        {
            base.Ping(windowStationHandle, windowDesktopHandle, windowHandle);
            this.Dispatch(new CommandInput
            {
                Text = "PING",
                WindowStationHandle = windowStationHandle,
                WindowDesktopHandle = windowDesktopHandle,
                WindowHandle = windowHandle
            });
        }

        // TODO: re-add encryption
    }
}