using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Mubox.Extensions.Console.ViewModels
{
    public class ConsoleViewModel
        : INotifyPropertyChanged
    {
        Dispatcher _dispatcher;

        public ObservableCollection<ConsoleMessage> Messages { get; private set; }

        public ConsoleMessage LatestMessage { get; private set; }

        public ConsoleViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            Messages = new ObservableCollection<ConsoleMessage>();
        }

        internal void AddMessageInternal(string category, string text)
        {
            var message = new ConsoleMessage
                {
                    Timestamp = DateTime.Now,
                    Category = category,
                    Text = text,
                };
            _dispatcher.InvokeAsync(() =>
            {
                Messages.Add(message);
                LatestMessage = message;
                PropertyChanged(this, new PropertyChangedEventArgs("LatestMessage"));
                while (Messages.Count > 8192)
                {
                    Messages.RemoveAt(0);
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
