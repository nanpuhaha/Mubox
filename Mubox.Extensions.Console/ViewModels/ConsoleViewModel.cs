using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Mubox.Extensions.Console.ViewModels
{
    public class ConsoleViewModel
    {
        Dispatcher _dispatcher;

        public ObservableCollection<string> Messages { get; private set; }

        public ConsoleViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            Messages = new ObservableCollection<string>();
        }

        internal void AddMessageInternal(string format, params object[] args)
        {
            _dispatcher.Invoke(() =>
            {
                Messages.Add(string.Format(format, args));
                while (Messages.Count > 8192)
                {
                    Messages.RemoveAt(0);
                }
            });
        }
    }
}
