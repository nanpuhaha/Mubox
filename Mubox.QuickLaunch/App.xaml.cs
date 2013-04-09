using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Linq;
using Mubox.Extensibility;

namespace Mubox.QuickLaunch
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static Mubox.Extensions.ExtensionManager _extensionManager;

        static App()
        {
            try
            {
                CultureInfo culture = CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                FrameworkElement.LanguageProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(
                        XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Debug.WriteLine("CoerceCultureInfo Failed for Mubox.QuickLaunch.App");
            }

            try
            {
                string muboxLogFilename = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MUBOX.LOG");
                try
                {
                    if (File.Exists(muboxLogFilename))
                    {
                        File.Delete(muboxLogFilename);
                    }
                }
                catch { }
                Stream clientStream = File.Open(muboxLogFilename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                Mubox.Diagnostics.TraceListenerStreamWriter traceListenerStreamWriter = new Mubox.Diagnostics.TraceListenerStreamWriter(clientStream);
                System.Diagnostics.Trace.Listeners.Add(traceListenerStreamWriter);
                Debug.WriteLine(new string('*', 0x4d));
                Debug.WriteLine(new string('*', 0x4d));
                Debug.WriteLine(new string('*', 0x4d));
                Debug.WriteLine("Logging \"" + muboxLogFilename + "\" for Mubox.QuickLaunch.App");
                ("Extensibility Trace Initialized").Log();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Debug.WriteLine("Logging Failed for Mubox.QuickLaunch.App");
            }

            try
            {
                System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                currentProcess.PriorityClass = System.Diagnostics.ProcessPriorityClass.RealTime;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            try
            {
                _extensionManager = new Extensions.ExtensionManager();
                _extensionManager.Initialize();

                _extensionManager.Extensions
                    .Select(ext => ext.Name)
                    .ToList()
                    .ForEach(name => _extensionManager.Start(name));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }
}