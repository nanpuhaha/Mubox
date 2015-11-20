using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

namespace Mubox.QuickLaunch
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
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
                ex.Log();
                ("CoerceCultureInfo Failed for Mubox.QuickLaunch.App").LogWarn();
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
                (new string('*', 0x4d)).LogInfo();
                (new string('*', 0x4d)).LogInfo();
                (new string('*', 0x4d)).LogInfo();
                ("Logging \"" + muboxLogFilename + "\" for Mubox.QuickLaunch.App").Log();
                ("Extensibility Trace Initialized").Log();
            }
            catch (Exception ex)
            {
                ex.Log();
                ("Logging Failed for Mubox.QuickLaunch.App").LogWarn();
            }

            try
            {
                System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                currentProcess.PriorityClass = System.Diagnostics.ProcessPriorityClass.RealTime;
            }
            catch (Exception ex)
            {
                ex.Log();
            }

            try
            {
                Mubox.Extensions.ExtensionManager.Instance.Initialize();

                Mubox.Extensions.ExtensionManager.Instance.Extensions
                    .Select(ext => ext.Name)
                    .ToList()
                    .ForEach(name => Mubox.Extensions.ExtensionManager.Instance.Start(name));
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }
    }
}