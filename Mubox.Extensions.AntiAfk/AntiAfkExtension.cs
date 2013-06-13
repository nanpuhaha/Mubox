using Mubox.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mubox.Extensions.AntiAfk
{
    public class AntiAfkExtension
        : MarshalByRefObject, IExtension
    {
        IMubox _mubox;

        Thread _thread;

        bool _exitYet;

        private ProxyEventHandler<ClientEventArgs> _onActiveClientChanged;
        private ProxyEventHandler<Extensibility.Input.KeyboardEventArgs> _onKeyboardInputReceived;
        private ProxyEventHandler<Extensibility.Input.MouseEventArgs> _onMouseInputReceived;

        public void OnLoad(IMubox mubox)
        {
            "AntiAfk::OnLoad".Log();
            _mubox = mubox;
            (_mubox as MarshalByRefObject).GetLifetimeService();

            _onActiveClientChanged = new ProxyEventHandler<ClientEventArgs>(_mubox_ActiveClientChanged);
            _mubox.ActiveClientChanged += _onActiveClientChanged.Proxy;

            _onKeyboardInputReceived = new ProxyEventHandler<Mubox.Extensibility.Input.KeyboardEventArgs>(Keyboard_InputReceived);
            _mubox.Keyboard.InputReceived += _onKeyboardInputReceived.Proxy;

            _onMouseInputReceived = new ProxyEventHandler<Mubox.Extensibility.Input.MouseEventArgs>(Mouse_InputReceived);
            _mubox.Mouse.InputReceived += _onMouseInputReceived.Proxy;

            _exitYet = false;

            Show();
        }

        public void OnUnload()
        {
            "AntiAfk::OnUnload".Log();

            _mubox.ActiveClientChanged -= _onActiveClientChanged.Proxy;
            _mubox.Keyboard.InputReceived -= _onKeyboardInputReceived.Proxy;
            _mubox.Mouse.InputReceived -= _onMouseInputReceived.Proxy;
            
            _exitYet = true;

            if (!_thread.Join(2500)) // 2.5 seconds
            {
                "AntiAfk Extension App Thread appears hung, not waiting for exit.".Log();
            }
        }

        private long _lastInputTimestamp;

        public void Keyboard_InputReceived(object sender, Extensibility.Input.KeyboardEventArgs e)
        {
             _lastInputTimestamp = DateTime.UtcNow.Ticks;
        }

        public void Mouse_InputReceived(object sender, Extensibility.Input.MouseEventArgs e)
        {
            _lastInputTimestamp = DateTime.UtcNow.Ticks;
        }

        private IMuboxClient _activeClient;

        public void _mubox_ActiveClientChanged(object sender, ClientEventArgs e)
        {
            _activeClient = e.Client;
        }

        private void Show()
        {
            _thread = new System.Threading.Thread(AntiAFkExtensionAppThread);
            _thread.Name = System.Threading.Thread.CurrentThread.Name + "_AntiAFk";
            _thread.IsBackground = true;
            _thread.Priority = System.Threading.ThreadPriority.Lowest;
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        private static WinAPI.VK[] _antiAfkKeyRotation = new[]
            {
                WinAPI.VK.W,
                WinAPI.VK.S,
                WinAPI.VK.Q,
                WinAPI.VK.E,
            };
        private static int _antiAfkKeyRotationCurrent = 0;
        
        private void AntiAFkExtensionAppThread(object obj)
        {
            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            "AntiAfk Extension App Thread Started".Log();
            try
            {
                while (!_exitYet)
                {
                    var waitTimeSeconds = 120.0; // TODO: make configurable
                    Thread.Sleep((int)(waitTimeSeconds * 1000));

                    var key = _antiAfkKeyRotation[_antiAfkKeyRotationCurrent];
                    _antiAfkKeyRotationCurrent = (_antiAfkKeyRotationCurrent + 1) % _antiAfkKeyRotation.Length;

                    if (_lastInputTimestamp < DateTime.UtcNow.AddSeconds(-2 * (waitTimeSeconds / 3.0)).Ticks)
                    {
                        "Mubox AntiAfk - Keeping you there.".Log();
                        foreach (var client in _mubox.Clients) 
                        {
                            client.KeyPress(key);
                        }
                    }
                }
            }
            finally
            {
                "AntiAfk Extension App Thread Exiting".Log();
            }
        }

        public override object InitializeLifetimeService()
        {
            var lease = (System.Runtime.Remoting.Lifetime.ILease)base.InitializeLifetimeService();
            lease.InitialLeaseTime = TimeSpan.FromHours(12);
            lease.RenewOnCallTime = TimeSpan.FromHours(12);
            lease.SponsorshipTimeout = TimeSpan.FromHours(12);
            return lease;
        }
    }
}
