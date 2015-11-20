using System;

namespace Mubox.Extensions.Console.ViewModels
{
    public class ConsoleMessage
    {
        public DateTime Timestamp { get; set; }
        public string Category { get; set; }
        public string Text { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}] {1}: {2}",
                Timestamp.ToShortTimeString(),
                Category,
                Text);
        }
    }
}