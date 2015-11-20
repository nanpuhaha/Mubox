using Mubox.Model.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Mubox.Model.Client
{
    // TODO ClientBase and NetworkClient probably need to be factored out of the codebase
    public class ClientBase
        : DependencyObject
    {
        protected ClientBase(string profileName)
        {
            ClientId = Guid.NewGuid();
            ProfileName = profileName;
        }

        #region ClientId

        public Guid ClientId { get; set; }

        #endregion ClientId

        #region ProfileName

        public string ProfileName { get; set; }

        #endregion ProfileName

        #region Address

        /// <summary>
        /// Address Dependency Property
        /// </summary>
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register("Address", typeof(string), typeof(ClientBase),
                new FrameworkPropertyMetadata((string)"127.0.0.1"));

        /// <summary>
        /// Gets or sets the Address property.  This dependency property
        /// indicates the Address of this Machine.
        /// </summary>
        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        internal static List<string> localAddressTable = InitializeLocalAddressTable();

        private static List<string> InitializeLocalAddressTable()
        {
            List<string> localAddressTable = new List<string>();
            localAddressTable.Add("127.0.0.1");
            localAddressTable.AddRange(System.Net.Dns.GetHostAddresses(Environment.MachineName).Select((a) => a.ToString()));
            foreach (string addressString in localAddressTable)
            {
                ("Local Address: " + addressString).LogInfo();
            }
            return localAddressTable;
        }

        private bool isLocalAddress;
        private bool isLocalAddressInitialized;

        public bool IsLocalAddress
        {
            get
            {
                if (isLocalAddressInitialized)
                {
                    return isLocalAddress;
                }
                isLocalAddress = ((this.Address == "127.0.0.1") || localAddressTable.Contains(this.Address));
                isLocalAddressInitialized = true;
                return isLocalAddress;
            }
        }

        #endregion Address

        #region IsAttached

        private bool _isAttached { get; set; }

        public bool IsAttached
        {
            get
            {
                return _isAttached;
            }
            set
            {
                if (_isAttached != value)
                {
                    _isAttached = value;
                    if (IsAttachedChanged != null)
                    {
                        IsAttachedChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public event EventHandler<EventArgs> IsAttachedChanged;

        #endregion IsAttached

        #region LastActivatedTimestamp

        public long LastActivatedTimestamp { get; set; }

        #endregion LastActivatedTimestamp

        #region DisplayName

        /// <summary>
        /// DisplayName Dependency Property
        /// </summary>
        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register("DisplayName", typeof(string), typeof(ClientBase),
                new FrameworkPropertyMetadata((string)"",
                    new PropertyChangedCallback(OnDisplayNameChanged)));

        /// <summary>
        /// Gets or sets the DisplayName property.  This dependency property
        /// indicates the Display Name of the Client.
        /// </summary>
        public string DisplayName
        {
            get { return (string)GetValue(DisplayNameProperty); }
            set
            {
                SetValue(DisplayNameProperty, value);
                _displayName = value;
            }
        }

        protected string _displayName;

        public event EventHandler<EventArgs> DisplayNameChanged;

        /// <summary>
        /// Handles changes to the DisplayName property.
        /// </summary>
        private static void OnDisplayNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ClientBase clientBase = d as ClientBase;
            if (d != null)
            {
                var name = clientBase.DisplayName ?? "";
                ("SrxDisplayNameChanged for " + clientBase.ClientId.ToString() + " to " + name).Log();
                if (!string.IsNullOrEmpty(name))
                {
                    if (clientBase.DisplayNameChanged != null)
                    {
                        clientBase.DisplayNameChanged(clientBase, new EventArgs());
                    }
                }
                ((ClientBase)d).OnDisplayNameChanged(e);
            }
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the DisplayName property.
        /// </summary>
        protected virtual void OnDisplayNameChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        #endregion DisplayName

        public static string Sanitize(string text)
        {
            byte[] textBytes = WinAPI.CodePage.ConvertToCodePage(text, 1251);
            text = System.Text.Encoding.ASCII.GetString(textBytes);
            if (string.IsNullOrEmpty(text))
            {
                return "NULL";
            }
            return System.Text.RegularExpressions.Regex.Replace(text, "[^A-Za-z0-9]*", "");
        }

        public static string Sanitize(byte[] data)
        {
            if ((data == null) || (data.Length == 0))
            {
                return "NULL";
            }
            return Sanitize(Convert.ToBase64String(data));
        }

        #region WindowStationHandle

        public IntPtr WindowStationHandle { get; set; }

        #endregion WindowStationHandle

        #region WindowDesktopHandle

        public IntPtr WindowDesktopHandle { get; set; }

        #endregion WindowDesktopHandle

        #region WindowHandle

        public IntPtr WindowHandle { get; set; }

        #endregion WindowHandle

        #region CachedScreenFromClientRect

        public WinAPI.Windows.RECT CachedScreenFromClientRect { get; set; }

        public DateTime CachedScreenFromClientRectExpiry { get; set; }

        #endregion CachedScreenFromClientRect

        #region Latency

        /// <summary>
        /// Latency Dependency Property
        /// </summary>
        public static readonly DependencyProperty LatencyProperty =
            DependencyProperty.Register("Latency", typeof(long), typeof(ClientBase),
                new FrameworkPropertyMetadata((long)-1L));

        /// <summary>
        /// Gets or sets the Latency property.  This dependency property
        /// indicates Server to Client roundtrip latency.
        /// </summary>
        public long Latency
        {
            get { return (long)GetValue(LatencyProperty); }
            set { SetValue(LatencyProperty, value); }
        }

        #endregion Latency

        #region PerformanceInfo

        /// <summary>
        /// PerformanceInfo Dependency Property
        /// </summary>
        public static readonly DependencyProperty PerformanceInfoProperty =
            DependencyProperty.Register("PerformanceInfo", typeof(Model.Client.PerformanceInfo), typeof(ClientBase),
                new FrameworkPropertyMetadata((Model.Client.PerformanceInfo)null));

        /// <summary>
        /// Gets or sets the PerformanceInfo property.  This dependency property
        /// indicates Client Performance Info.
        /// </summary>
        public Model.Client.PerformanceInfo PerformanceInfo
        {
            get { return (Model.Client.PerformanceInfo)GetValue(PerformanceInfoProperty); }
            set { SetValue(PerformanceInfoProperty, value); }
        }

        #endregion PerformanceInfo

        public bool FixAltKey { get; set; }

        public virtual void Dispatch(MouseInput e)
        {
        }

        public virtual void Dispatch(KeyboardInput e)
        {
        }

        public virtual void Dispatch(CommandInput e)
        {
        }

        public virtual void Dispatch(WinAPI.VK vk)
        {
            // TODO: scan code mapping should always be done on the destination machine, since it contains hardware/driver specific values
            var scan = WinAPI.SendInputApi.MapVirtualKeyEx(vk, WinAPI.SendInputApi.MAPVK.MAPVK_VK_TO_VSC, System.Windows.Forms.InputLanguage.CurrentInputLanguage.Handle);
            Dispatch(new KeyboardInput { WM = Mubox.WinAPI.WM.KEYDOWN, VK = vk, Time = WinAPI.SendInputApi.GetTickCount(), Scan = scan });
            System.Threading.Thread.Sleep(0x4d);
            Dispatch(new KeyboardInput { WM = Mubox.WinAPI.WM.KEYUP, VK = vk, Time = WinAPI.SendInputApi.GetTickCount(), Scan = scan, Flags = WinAPI.WindowHook.LLKHF.UP });
        }

        private void Dispatch(IEnumerable<WinAPI.VK> vkSet)
        {
            foreach (WinAPI.VK vk in vkSet)
            {
                Dispatch(vk);
            }
        }

        public virtual void Activate()
        {
            LastActivatedTimestamp = DateTime.Now.Ticks;
        }

        public virtual void Deactivate()
        {
        }

        public virtual void Attach()
        {
            IsAttached = true;
        }

        public virtual void Detach()
        {
            IsAttached = false;
        }

        protected long PingSendTimestampTicks { get; set; }

        public virtual void Ping(IntPtr windowStationHandle, IntPtr windowDesktopHandle, IntPtr windowHandle)
        {
            PingSendTimestampTicks = DateTime.Now.Ticks;
        }

        public override string ToString()
        {
            return this.DisplayName + "/" + this.WindowStationHandle.ToString() + "/" + this.WindowDesktopHandle.ToString() + "/" + this.WindowHandle.ToString();
        }
    }
}