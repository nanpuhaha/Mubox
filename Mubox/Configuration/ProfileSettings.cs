using System;
using System.ComponentModel;
using System.Configuration;

namespace Mubox.Configuration
{
    public class ProfileSettings
        : ConfigurationElement, INotifyPropertyChanged
    {
        [ConfigurationProperty("Name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)base["Name"]; }
            set { if (!Name.Equals(value, StringComparison.InvariantCultureIgnoreCase)) { base["Name"] = value; this.OnPropertyChanged(o => o.Name); } }
        }

        #region Clients

        [ConfigurationProperty("Clients")]
        public ClientSettingsCollection Clients
        {
            get { return (ClientSettingsCollection)base["Clients"]; }
            set { base["Clients"] = value; }
        }

        private ClientSettings _activeClient;

        public ClientSettings ActiveClient
        {
            get
            {
                return _activeClient;
            }
            set
            {
                if (_activeClient != value)
                {
                    _activeClient = value;
                    OnActiveClientChanged();
                }
            }
        }

        public event EventHandler<EventArgs> ActiveClientChanged;

        private void OnActiveClientChanged()
        {
            if (ActiveClientChanged != null)
            {
                ActiveClientChanged(_activeClient, new EventArgs());
            }
        }

        #endregion Clients

        [ConfigurationProperty("EnableMulticast", IsRequired = false, DefaultValue = false)]
        public bool EnableMulticast
        {
            get { return (bool)base["EnableMulticast"]; }
            set
            {
                if (!EnableMulticast && value)
                {
                    if (Keys.Count == 0)
                    {
                        string defaultActiveClientOnlyKeys = "Escape";
                        ("Creating Default 'Active Client Only' Keys: " + defaultActiveClientOnlyKeys).Log();
                        foreach (string vkString in defaultActiveClientOnlyKeys.Split(','))
                        {
                            Keys.CreateNew((WinAPI.VK)Enum.Parse(typeof(WinAPI.VK), vkString, true)).ActiveClientOnly = true;
                        }
                        MuboxConfigSection.Save();
                    }
                }
                base["EnableMulticast"] = value;
            }
        }

        /// <summary>
        /// <para>
        /// Some games reset/alter the mouse position when panning with the mouse in a way that
        /// is not compatible with Mubox (and similar applications), this results in erratic
        /// 'spinning' of the camera because the game continues to read an incorrect position.
        ///
        /// This option attemps to provide a 'fix' for some of these games.
        /// </para>
        /// </summary>
        [ConfigurationProperty("EnableMousePanningFix", IsRequired = false, DefaultValue = false)]
        public bool EnableMousePanningFix
        {
            get { return (bool)base["EnableMousePanningFix"]; }
            set { if (EnableMousePanningFix != value) { base["EnableMousePanningFix"] = value; this.OnPropertyChanged(o => o.EnableMousePanningFix); } }
        }

        /// <summary>
        /// <para>
        /// Some games don't appear to respond to LeftControl, LeftMenu nor LeftShift key messages.
        ///
        /// This option performs a translation from Left/Right to less-specific messages (e.g. for LeftShift we translate to Shift, for RightMenu we translate to Menu, etc.)
        ///
        /// If necessary we will re-work this to be more specific about which keys to translate, but currently this applies to all three.
        ///
        /// By default this option is enabled because so far most games (but NOT all) appear to need this translation.
        /// </para>
        /// </summary>
        [ConfigurationProperty("EnableCASFix", IsRequired = false, DefaultValue = true)]
        public bool EnableCASFix
        {
            get { return (bool)base["EnableCASFix"]; }
            set { if (EnableCASFix != value) { base["EnableCASFix"] = value; this.OnPropertyChanged(o => o.EnableCASFix); } }
        }

        [ConfigurationProperty("Keys")]
        public KeySettingCollection Keys
        {
            get { return (KeySettingCollection)base["Keys"]; }
            set { base["Keys"] = value; }
        }

        protected override void Init()
        {
            Clients = new ClientSettingsCollection();
            Keys = new KeySettingCollection();
            base.Init();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}