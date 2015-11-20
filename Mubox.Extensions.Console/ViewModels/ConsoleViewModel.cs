using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;

namespace Mubox.Extensions.Console.ViewModels
{
    public class ConsoleViewModel
        : INotifyPropertyChanged
    {
        private Dispatcher _dispatcher;

        public ObservableCollection<ConsoleMessage> Messages { get; private set; }

        public ConsoleMessage LatestMessage { get; private set; }

        public ConsoleViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            Messages = new ObservableCollection<ConsoleMessage>();
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            if (assembly == null)
            {
                assembly = System.Reflection.Assembly.GetExecutingAssembly();
            }
#if DEBUG
            var text = assembly.GetName().Version.ToString() + " DEBUG";
#else
            var text = assembly.GetName().Version.ToString() + " RELEASE";
#endif

            var message = new ConsoleMessage
            {
                Timestamp = DateTime.Now,
                Category = "Mubox",
                Text = text,
            };
            Messages.Add(message);
            LatestMessage = message;
        }

        internal void AddMessageInternal(string category, string text)
        {
#if !DEBUG
            if (category == "Verbose")
            {
                // when not compiled for debugging we throw away all Verbose console output
                return;
            }
#endif

            var message = new ConsoleMessage
                {
                    Timestamp = DateTime.Now,
                    Category = category,
                    Text = text,
                };
            _dispatcher.InvokeAsync(() =>
            {
                while (Messages.Count > 127)
                {
                    Messages.RemoveAt(0);
                }
                Messages.Add(message);
                LatestMessage = message;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("LatestMessage"));
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}