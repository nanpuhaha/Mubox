using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Configuration
{
    [ConfigurationCollection(typeof(ProfileSettings))]
    public class ProfileSettingsCollection
        : ConfigurationElementCollection, INotifyPropertyChanged
    {
        [ConfigurationProperty("Default", IsRequired = true, IsKey = false)]
        public string Default
        {
            get { return (string)base["Default"]; }
            set { if (!Default.Equals(value, StringComparison.InvariantCultureIgnoreCase)) { base["Default"] = value; this.OnPropertyChanged(o => o.Default); } }
        }

        public ProfileSettings ActiveProfile
        {
            get
            {
                // TODO: HACK: need to implement team selection support :( this at least allows users to edit the config directly
                return GetOrCreateNew(Default);
            }
            set
            {
                Default = value.Name;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return (new ProfileSettings()) as ConfigurationElement;
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as ProfileSettings).Name;
        }

        public void Remove(string name)
        {
            base.BaseRemove(name);
        }

        public ProfileSettings GetOrCreateNew(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Default";
            }
            var settings = default(Configuration.ProfileSettings);
            foreach (var o in Mubox.Configuration.MuboxConfigSection.Default.Profiles)
            {
                settings = o as Configuration.ProfileSettings;
                if (settings != null)
                {
                    if (settings.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return settings;
                    }
                }
            }
            return CreateNew(name);
        }

        internal ProfileSettings CreateNew(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Invalid Name", "name");
            }
            var element = CreateNewElement();
            var settings = element as ProfileSettings;
            settings.Name = name;
            base.BaseAdd(element);
            Default = name;
            return settings;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
