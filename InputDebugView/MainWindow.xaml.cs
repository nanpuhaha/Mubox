using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace InputDebugTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Mubox.Configuration.KeySettingCollection keysettings;

        private void Window_KeyUpDown(object sender, KeyEventArgs e)
        {
            var vk = (Mubox.WinAPI.VK)System.Windows.Input.KeyInterop.VirtualKeyFromKey(e.Key);
            var key = keysettings.GetOrCreateNew(vk);
            key.ActiveClientOnly = e.IsDown;
            keys.InitializeButtonState(
                keysettings,
                (k) => k.ActiveClientOnly,
                (k) =>
                {
                },
                (k) =>
                {
                });
        }

        public MainWindow()
        {
            InitializeComponent();
            keysettings = new Mubox.Configuration.KeySettingCollection();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(canvas);
            mouse.SetValue(Canvas.LeftProperty, position.X - 16);
            mouse.SetValue(Canvas.TopProperty, position.Y - 16);
            mouse.SetValue(Canvas.LeftProperty, position.X - 16);
            mouse.SetValue(Canvas.TopProperty, position.Y - 16);
        }

        private void Window_MouseUpDown(object sender, MouseButtonEventArgs e)
        {
            var ellipse = default(Ellipse);
            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    ellipse = lmb;
                    break;

                case MouseButton.Middle:
                    ellipse = mmb;
                    break;

                case MouseButton.Right:
                    ellipse = rmb;
                    break;

                case MouseButton.XButton1:
                    ellipse = xb1;
                    break;

                case MouseButton.XButton2:
                    ellipse = xb2;
                    break;
            }
            switch (e.ButtonState)
            {
                case MouseButtonState.Pressed:
                    ellipse.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    break;

                case MouseButtonState.Released:
                    ellipse.SetValue(UIElement.VisibilityProperty, Visibility.Hidden);
                    break;
            }
        }
    }
}