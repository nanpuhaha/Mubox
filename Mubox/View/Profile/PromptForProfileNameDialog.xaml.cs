using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mubox.View.Profile
{
    /// <summary>
    /// Interaction logic for PromptForProfileNameDialog.xaml
    /// </summary>
    public partial class PromptForProfileNameDialog : Window
    {
        public PromptForProfileNameDialog()
        {
            InitializeComponent();
        }

        #region ProfileName

        /// <summary>
        /// ProfileName Dependency Property
        /// </summary>
        public static readonly DependencyProperty ProfileNameProperty =
            DependencyProperty.Register("ProfileName", typeof(string), typeof(PromptForProfileNameDialog),
                new FrameworkPropertyMetadata((string)Sanitize(Environment.MachineName)));

        /// <summary>
        /// Gets or sets the ProfileName property.  This dependency property
        /// indicates the chose Profile name.
        /// </summary>
        public string ProfileName
        {
            get { return Sanitize((string)GetValue(ProfileNameProperty)); }
            set { SetValue(ProfileNameProperty, Sanitize(value).ToUpper()); }
        }

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

        #endregion

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void textProfileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.ProfileName = textProfileName.Text;
            buttonOK.IsEnabled = !string.IsNullOrEmpty(this.ProfileName);
        }

        public static string PromptForProfileName()
        {
            Mubox.View.Profile.PromptForProfileNameDialog dlg = new Mubox.View.Profile.PromptForProfileNameDialog();
            bool cancel = !dlg.ShowDialog().GetValueOrDefault(false);
            if (string.IsNullOrEmpty(dlg.ProfileName) || cancel)
            {
                throw new ArgumentException("Cancelled", "ProfileName");
            }
            return dlg.ProfileName;
        }
    }
}
