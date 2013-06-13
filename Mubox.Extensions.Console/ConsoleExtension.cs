using Mubox.Extensibility;
using Mubox.Extensions.Console.ViewModels;
using Mubox.Extensions.Console.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Mubox.Extensions.Console
{
    /// <summary>
    /// <para>Provides a 'Console Window' where misc Events and Data can be reviewed.</para>
    /// <para>This mainly exists for Extension Debugging within Mubox, it also makes a nice sample project.</para>
    /// </summary>
    public class ConsoleExtension
        : MarshalByRefObject, IExtension
    {
        IMubox _mubox;

        ConsoleViewModel _viewModel;
        ConsoleView _view;
        Window _presenter;
        Application _application;
        Thread _thread;
        Dispatcher _dispatcher;
        bool _exitYet;

        private ProxyEventHandler<ClientEventArgs> _onActiveClientChanged;
        private ProxyEventHandler<Extensibility.Input.KeyboardEventArgs> _onKeyboardInputReceived;
        private ProxyEventHandler<Extensibility.Input.MouseEventArgs> _onMouseInputReceived;

        public void OnLoad(IMubox mubox)
        {
            "Console::OnLoad".Log();
            _mubox = mubox;

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
            "Console::OnUnload".Log();

            _mubox.ActiveClientChanged -= _onActiveClientChanged.Proxy;
            _mubox.Keyboard.InputReceived -= _onKeyboardInputReceived.Proxy;
            _mubox.Mouse.InputReceived -= _onMouseInputReceived.Proxy;
            
            _exitYet = true;
            _presenter.Close();
            if (!_thread.Join(2500)) // 2.5 seconds
            {
                "Console Extension App Thread appears hung, not waiting for exit.".Log();
            }
        }

        public void Keyboard_InputReceived(object sender, Extensibility.Input.KeyboardEventArgs e)
        {
            //e.Log();
            _viewModel.AddMessageInternal("Keyboard Input",
                string.Format("name={0} {1}",
                    e.Client != null ? e.Client.Name : "(no client)",
                    e.ToString()));
        }

        public void Mouse_InputReceived(object sender, Extensibility.Input.MouseEventArgs e)
        {
            //e.Log();
            _viewModel.AddMessageInternal("Mouse Input",
                string.Format("name={0} {1}",
                    e.Client != null ? e.Client.Name : "(no client)",
                    e.ToString()));
        }

        public void _mubox_ActiveClientChanged(object sender, ClientEventArgs e)
        {
            //e.Log();
            _viewModel.AddMessageInternal("Active Client Changed", e.Client != null ? e.Client.Name : "(no client)");
        }

        private void Show()
        {
            _thread = new System.Threading.Thread(ConsoleExtensionAppThread);
            _thread.Name = System.Threading.Thread.CurrentThread.Name + "_UI";
            _thread.IsBackground = true;
            _thread.Priority = System.Threading.ThreadPriority.Lowest;
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        private void ConsoleExtensionAppThread(object obj)
        {
            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            "Console Extension App Thread Started".Log();
            try
            {
                _dispatcher = Dispatcher.CurrentDispatcher;
                _viewModel = new ConsoleViewModel();
                _view = new Mubox.Extensions.Console.Views.ConsoleView();
                _presenter = new System.Windows.Window();
                //_presenter.Title = (AppDomain.CurrentDomain.FriendlyName ?? "Default").Replace('.', ' ');
                _presenter.Title = "Mubox Console";
                _presenter.Topmost = true;
                _presenter.WindowStyle = WindowStyle.SingleBorderWindow;
                /* transparent window
                _presenter.WindowStyle = WindowStyle.None;
                _presenter.BorderThickness = new Thickness(1);
                _presenter.AllowsTransparency = true;
                _presenter.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
                 */
                _presenter.DataContext = _viewModel;
                _presenter.Content = _view;
                _view.Margin = new Thickness(0);
                _presenter.Closing += OnWindowClosing;
                // TODO: save/restore last-known size and position
                _presenter.WindowStartupLocation = WindowStartupLocation.Manual;
                _presenter.Width = 640.0;
                _presenter.Height = 480.0;
                _presenter.Top = 256;
                _presenter.Left = 64;
                _application = new System.Windows.Application();
                _application.Run(_presenter);
            }
            finally
            {
                "Console Extension App Thread Exiting".Log();
            }
        }

        void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // prevent closing unless OnUnload() has been called
            e.Cancel = !_exitYet;
            ("Console::OnUnload Cancelled=" + e.Cancel).Log();
        }

        public override object InitializeLifetimeService()
        {
            var lease = (System.Runtime.Remoting.Lifetime.ILease)base.InitializeLifetimeService();
            lease.InitialLeaseTime = TimeSpan.FromHours(12);
            lease.RenewOnCallTime = TimeSpan.FromHours(12);
            lease.SponsorshipTimeout = TimeSpan.FromHours(12);
            return lease;
        }

        public object GetService(Type serviceType)
        {
            return null;
        }
    }
}
