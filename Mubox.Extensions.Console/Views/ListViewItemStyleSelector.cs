using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mubox.Extensions.Console.Views
{
    public class ListViewItemStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item,
            DependencyObject container)
        {
            var foregroundSetter = new Setter
            {
                Property = ListViewItem.ForegroundProperty,
            };
            var style = new Style
            {
                TargetType = typeof(ListViewItem),
            };
            foregroundSetter.Value = Brushes.Gray;
            style.Setters.Add(foregroundSetter);
            var listView = ItemsControl.ItemsControlFromItemContainer(container) as ListView;
            if (listView != null)
            {
                var L_item = item as ViewModels.ConsoleMessage;
                if (L_item != null)
                {
                    switch (L_item.Category.ToUpperInvariant())
                    {
                        case "CRITICAL":
                            foregroundSetter.Value = Brushes.Red;
                            break;
                        case "ERROR":
                            foregroundSetter.Value = Brushes.Orange;
                            break;
                        case "WARNING":
                            foregroundSetter.Value = Brushes.Yellow;
                            break;
                        case "INFORMATION":
                        case "MUBOX":
                            foregroundSetter.Value = Brushes.CornflowerBlue;
                            break;
                        case "VERBOSE":
                            foregroundSetter.Value = Brushes.Gray;
                            break;
                        default:
                            foregroundSetter.Value = Brushes.GhostWhite;
                            break;
                    }
                }
            }
            return style;
        }
    }
}
