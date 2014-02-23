using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        #endregion

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
                        string defaultActiveClientOnlyKeys = "W,S,A,D,OEM2,Divide,E,Return,OEM5,Escape,Tab,Tilde";
                        Debug.WriteLine("Creating Default 'Active Client Only' Keys: " + defaultActiveClientOnlyKeys);
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
