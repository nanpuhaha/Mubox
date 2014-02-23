using System;
using System.Configuration;
using System.Linq;

namespace Mubox.Configuration
{
    [ConfigurationCollection(typeof(ClientSettings))]
    public class ClientSettingsCollection
        : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return (new ClientSettings()) as ConfigurationElement;
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as ClientSettings).Name;
        }

        internal ClientSettings CreateNew(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Invalid Name", "name");
            }
            var element = CreateNewElement();
            var settings = element as ClientSettings;
            settings.Name = name;
            base.BaseAdd(element);
            return settings;
        }

        public ClientSettings GetExisting(string characterName)
        {
            foreach (var clientSettings in this.OfType<ClientSettings>())
            {
                if (clientSettings.Name.Equals(characterName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return clientSettings;
                }
            }
            return null;
        }

        public bool Remove(string name)
        {
            if (GetExisting(name) != null)
            {
                base.BaseRemove(name);
                return true;
            }
            return false;
        }
    }
}