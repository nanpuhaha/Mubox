using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using Mubox.Model;

namespace Mubox.Configuration
{
    public class MuboxConfigSection
        : ConfigurationSection, INotifyPropertyChanged
    {
        internal static System.Configuration.Configuration Configuration { get; set; }

        public static MuboxConfigSection Default
        {
            get
            {
                System.Configuration.Configuration configuration =
                    Configuration
                    ??
                    System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                Configuration = configuration;
                var config = Configuration.GetSection("MuboxConfig") as MuboxConfigSection;
                if (config == null)
                {
                    config = new MuboxConfigSection();
                    configuration.Sections.Add("MuboxConfig", config);
                    config.SectionInformation.ForceDeclaration(true);
                    config.SectionInformation.ForceSave = true;
                }
                return config;
            }
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        protected override void Init()
        {
            Profiles = new ProfileSettingsCollection();
            base.Init();
        }

        public static void Save()
        {
            // persist to disk
            Configuration.Save(ConfigurationSaveMode.Modified);
            // update cached config
            System.Configuration.ConfigurationManager.RefreshSection("MuboxConfig");
            // TODO sync CAS Modifiers with Server, if server is not running local
        }

        [ConfigurationProperty("MouseBufferMilliseconds", IsRequired = false, DefaultValue = 500.0)]
        public double ClickBufferMilliseconds
        {
            get { return (double)base["MouseBufferMilliseconds"]; }
            set { base["MouseBufferMilliseconds"] = value; }
        }

        [ConfigurationProperty("MouseMulticastMode", IsRequired = false, DefaultValue = MouseMulticastModeType.Disabled)]
        public MouseMulticastModeType MouseMulticastMode
        {
            get { return (MouseMulticastModeType)base["MouseMulticastMode"]; }
            set { base["MouseMulticastMode"] = value; }
        }

        [ConfigurationProperty("PreferredTheme", IsRequired = false, DefaultValue = "Default")]
        public string PreferredTheme
        {
            get { return (string)base["PreferredTheme"]; }
            set { base["PreferredTheme"] = value; }
        }

        [ConfigurationProperty("DisableAltTabHook", IsRequired = false, DefaultValue = false)]
        public bool DisableAltTabHook
        {
            get { return (bool)base["DisableAltTabHook"]; }
            set { base["DisableAltTabHook"] = value; }
        }

        [ConfigurationProperty("EnableMouseCapture", IsRequired = false, DefaultValue = true)]
        public bool EnableMouseCapture
        {
            get { return (bool)base["EnableMouseCapture"]; }
            set { base["EnableMouseCapture"] = value; }
        }

        [ConfigurationProperty("AutoStartServer", IsRequired = false, DefaultValue = false)]
        public bool AutoStartServer
        {
            get { return (bool)base["AutoStartServer"]; }
            set { base["AutoStartServer"] = value; }
        }

        #region AutoRunOnQuickLaunch

        [ConfigurationProperty("AutoLaunchGame", IsRequired = false, DefaultValue = default(bool))]
        public bool AutoLaunchGame
        {
            get { return (bool)base["AutoLaunchGame"]; }
            set
            {
                base["AutoLaunchGame"] = value;
                if (AutoLaunchGameChanged != null)
                {
                    AutoLaunchGameChanged(this, new EventArgs());
                }
            }
        }

        public event EventHandler<EventArgs> AutoLaunchGameChanged;

        #endregion AutoRunOnQuickLaunch

        /// <summary>
        /// <para>aka "do not broadcast a key repeat event sent by the OS"</para>
        /// </summary>
        [ConfigurationProperty("DisableRepeatKeyFiltering", IsRequired = false, DefaultValue = default(bool))]
        public bool DisableRepeatKeyFiltering
        {
            get { return (bool)base["DisableRepeatKeyFiltering"]; }
            set { base["DisableRepeatKeyFiltering"] = value; }
        }

        public bool IsCaptureEnabled { get; set; }

        [ConfigurationProperty("ReverseClientSwitching", IsRequired = false, DefaultValue = false)]
        public bool ReverseClientSwitching
        {
            get { return (bool)base["ReverseClientSwitching"]; }
            set { base["ReverseClientSwitching"] = value; }
        }

        [ConfigurationProperty("OpenFileDialogInitialDirectory", IsRequired = false)]
        public string OpenFileDialogInitialDirectory
        {
            get { return (string)base["OpenFileDialogInitialDirectory"]; }
            set { base["OpenFileDialogInitialDirectory"] = value; }
        }

        [ConfigurationProperty("Profiles")]
        public ProfileSettingsCollection Profiles
        {
            get { return (ProfileSettingsCollection)base["Profiles"]; }
            set { base["Profiles"] = value; }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged Members
    }
}