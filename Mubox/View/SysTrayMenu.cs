using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Mubox.Model;

namespace Mubox.View
{
    public class SysTrayMenu
        : ContextMenu
    {
        public SysTrayMenu(Action helpCallback, Action exitApplicationCallback)
        {
            try
            {
                Resources["imageShortcutIcon"] = new Image
                {
                    Source = (new ImageSourceConverter()).ConvertFromString("pack://application:,,,/Mubox;component/Content/Images/GotoShortcutsHS.png") as ImageSource
                };
                Resources["imageNavForwardIcon"] = new Image
                {
                    Source = (new ImageSourceConverter()).ConvertFromString("pack://application:,,,/Mubox;component/Content/Images/NavForward.png") as ImageSource
                };
                Resources["imageMenuHelpIcon"] = new Image
                {
                    Source = (new ImageSourceConverter()).ConvertFromString("pack://application:,,,/Mubox;component/Content/Images/HelpIcon.png") as ImageSource
                };
            }
            catch (Exception ex)
            {
                ex.Log();
            }

            try
            {
                System.Drawing.Point mousePosition = new System.Drawing.Point(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Right - 16, System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Bottom - 24);
                WinAPI.Cursor.GetCursorPos(out mousePosition);

                List<object> quickLaunchMenuItems = new List<object>();
                ItemsSource = quickLaunchMenuItems;

                MenuItem menuItem = null;

                foreach (var profile in Mubox.Configuration.MuboxConfigSection.Default.Profiles.OfType<Mubox.Configuration.ProfileSettings>())
                {
                    menuItem = CreateProfileShortcutMenu(profile);
                    if (menuItem != null)
                    {
                        quickLaunchMenuItems.Add(menuItem);
                    }
                }

                // New Game Profile
                quickLaunchMenuItems.Add(new Separator());
                menuItem = new MenuItem();
                menuItem.Click += (sender, e) =>
                {
                    try
                    {
                        string profileName = Mubox.View.Profile.PromptForProfileNameDialog.PromptForProfileName();
                        // TODO try and enforce "unique" profile names

                        var profileSettings = Mubox.Configuration.MuboxConfigSection.Default.Profiles.GetOrCreateNew(profileName);
                        Mubox.Configuration.MuboxConfigSection.Default.Profiles.ActiveProfile = profileSettings;
                        Mubox.Configuration.MuboxConfigSection.Save();
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                };
                menuItem.Header = "_Configure New Game Profile...";
                menuItem.Icon = Resources["imageSettingsIcon"];
                quickLaunchMenuItems.Add(menuItem);


                // Launch Mubox Server
                quickLaunchMenuItems.Add(new Separator());
                if (Mubox.View.Server.ServerWindow.Instance == null)
                {
                    menuItem = new MenuItem();
                    menuItem.Click += (sender, e) =>
                    {
                        CreateServerUI();
                    };
                    menuItem.Header = "Mubox _Server...";
                    quickLaunchMenuItems.Add(menuItem);
                }
                else
                {
                    // "Disable 'Client Switching' Feature"
                    menuItem = new MenuItem();
                    menuItem.IsCheckable = true;
                    menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.DisableAltTabHook;
                    menuItem.Click += (sender, e) =>
                    {
                        Mubox.Configuration.MuboxConfigSection.Default.DisableAltTabHook = !Mubox.Configuration.MuboxConfigSection.Default.DisableAltTabHook;
                        Mubox.Configuration.MuboxConfigSection.Save();
                    };
                    menuItem.Header = "Disable \"Client Switching\" Feature";
                    menuItem.ToolTip = "Enable this option to use the default Windows Task Switcher instead of the Mubox Server UI, this only affects Client Switching.";
                    quickLaunchMenuItems.Add(menuItem);

                    // "Reverse Client Switching"
                    menuItem = new MenuItem();
                    menuItem.IsCheckable = true;
                    menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.ReverseClientSwitching;
                    menuItem.Click += (sender, e) =>
                    {
                        Mubox.Configuration.MuboxConfigSection.Default.ReverseClientSwitching = !Mubox.Configuration.MuboxConfigSection.Default.ReverseClientSwitching;
                        Mubox.Configuration.MuboxConfigSection.Save();
                    };
                    menuItem.Header = "Reverse Client Switching";
                    menuItem.ToolTip = "Enable this option to reverse the order that Client Switcher will switch between clients.";
                    quickLaunchMenuItems.Add(menuItem);

                    // "Toggle Server UI"
                    menuItem = new MenuItem();
                    menuItem.Click += (sender, e) =>
                    {
                        if (Mubox.View.Server.ServerWindow.Instance != null)
                        {
                            Mubox.View.Server.ServerWindow.Instance.SetInputCapture((Mubox.View.Server.ServerWindow.Instance.Visibility == Visibility.Visible), (Mubox.View.Server.ServerWindow.Instance.Visibility != Visibility.Visible));
                        }
                    };
                    menuItem.Header = "Toggle Server UI";
                    menuItem.ToolTip = "Show/Hide the Server UI";
                    quickLaunchMenuItems.Add(menuItem);

                    quickLaunchMenuItems.Add(new Separator());

                    // "Enable Input Capture"
                    menuItem = new MenuItem();
                    menuItem.IsCheckable = true;
                    menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled;
                    menuItem.Click += (sender, e) =>
                    {
                        if (Mubox.View.Server.ServerWindow.Instance != null)
                        {
                            Mubox.View.Server.ServerWindow.Instance.ToggleInputCapture(false);
                        }
                    };
                    menuItem.Header = "Enable Input Capture";
                    menuItem.ToolTip = "'Input Capture' includes both Mouse and Keyboard Input";
                    quickLaunchMenuItems.Add(menuItem);

                    // "Configure Keyboard"
                    menuItem = new MenuItem();
                    menuItem.Click += (sender, e) =>
                    {
                        Mubox.View.Server.MulticastConfigDialog.ShowStaticDialog();
                    };
                    menuItem.Header = "Configure Keyboard..";
                    quickLaunchMenuItems.Add(menuItem);

                    //if (Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled)
                    {
                        // "Enable Multicast"
                        menuItem = new MenuItem();
                        menuItem.IsCheckable = true;
                        menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.Profiles.ActiveProfile.EnableMulticast;
                        menuItem.Click += (sender, e) =>
                        {
                            Mubox.Configuration.MuboxConfigSection.Default.Profiles.ActiveProfile.EnableMulticast = !Mubox.Configuration.MuboxConfigSection.Default.Profiles.ActiveProfile.EnableMulticast;
                            Mubox.Configuration.MuboxConfigSection.Save();
                        };
                        menuItem.Header = "Enable Multicast";
                        menuItem.ToolTip = "'Keyboard Multicast' replicates your Key Presses to all Clients.";
                        quickLaunchMenuItems.Add(menuItem);

                        // "Enable Mouse Capture"
                        quickLaunchMenuItems.Add(new Separator());
                        menuItem = new MenuItem();
                        menuItem.IsCheckable = true;
                        menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.EnableMouseCapture;
                        menuItem.Click += (sender, e) =>
                        {
                            Mubox.Configuration.MuboxConfigSection.Default.EnableMouseCapture = !Mubox.Configuration.MuboxConfigSection.Default.EnableMouseCapture;
                            Mubox.Configuration.MuboxConfigSection.Save();
                        };
                        menuItem.Header = "Enable Mouse Capture";
                        menuItem.ToolTip = "Disable Mouse Capture if you use a third-party application for the Mouse.";
                        quickLaunchMenuItems.Add(menuItem);

                        //if (Mubox.Configuration.MuboxConfigSection.Default.EnableMouseCapture)
                        {
                            {
                                // "Mouse Multicast" Menu
                                List<MenuItem> mouseMulticastModeMenu = new List<MenuItem>();

                                // "Disabled"
                                menuItem = new MenuItem();
                                menuItem.IsCheckable = true;
                                menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.MouseMulticastMode == MouseMulticastModeType.Disabled;
                                menuItem.Click += (sender, e) =>
                                {
                                    Mubox.Configuration.MuboxConfigSection.Default.MouseMulticastMode = MouseMulticastModeType.Disabled;
                                    Mubox.Configuration.MuboxConfigSection.Save();
                                };
                                menuItem.Header = "Disabled";
                                menuItem.ToolTip = "Use this option to Disable the Mouse Multicast feature.";
                                mouseMulticastModeMenu.Add(menuItem);

                                // "Toggled"
                                menuItem = new MenuItem();
                                menuItem.IsCheckable = true;
                                menuItem.IsChecked = (Mubox.Configuration.MuboxConfigSection.Default.MouseMulticastMode == Mubox.Model.MouseMulticastModeType.Toggled);
                                menuItem.Click += (sender, e) =>
                                {
                                    Mubox.Configuration.MuboxConfigSection.Default.MouseMulticastMode = Mubox.Model.MouseMulticastModeType.Toggled;
                                    Mubox.Configuration.MuboxConfigSection.Save();
                                };
                                menuItem.Header = "Toggled";
                                menuItem.ToolTip = "Mouse Multicast is Active while CAPS LOCK is ON, and Inactive while CAPS LOCK is OFF.";
                                mouseMulticastModeMenu.Add(menuItem);

                                // "Pressed"
                                menuItem = new MenuItem();
                                menuItem.IsCheckable = true;
                                menuItem.IsChecked = (Mubox.Configuration.MuboxConfigSection.Default.MouseMulticastMode == MouseMulticastModeType.Pressed);
                                menuItem.Click += (sender, e) =>
                                {
                                    Mubox.Configuration.MuboxConfigSection.Default.MouseMulticastMode = Mubox.Model.MouseMulticastModeType.Pressed;
                                    Mubox.Configuration.MuboxConfigSection.Save();
                                };
                                menuItem.Header = "Pressed";
                                menuItem.ToolTip = "Mouse Multicast is Active while CAPS LOCK Key is pressed, and Inactive while CAPS LOCK Key is released.";
                                mouseMulticastModeMenu.Add(menuItem);

                                menuItem = new MenuItem();
                                menuItem.Header = "Mouse Multicast";
                                menuItem.ItemsSource = mouseMulticastModeMenu;
                                quickLaunchMenuItems.Add(menuItem);
                            }
                            {
                                // "Click Buffer" Option
                                List<MenuItem> mouseClickBufferMenu = new List<MenuItem>();

                                foreach (double time in new double[] { 0.0, 100.0, 150.0, 200.0, 250.0, 500.0, 750.0, 1000.0 })
                                {
                                    CreateMouseBufferMenuItem(menuItem, mouseClickBufferMenu, time);
                                }

                                menuItem = new MenuItem();
                                menuItem.Header = "Click Buffer";
                                menuItem.ToolTip = "Click Buffer prevents Mouse Movement from interrupting a Click gesture.";
                                menuItem.ItemsSource = mouseClickBufferMenu;
                                quickLaunchMenuItems.Add(menuItem);
                            }
                        }
                    }

                    quickLaunchMenuItems.Add(new Separator());

                    // "Auto-Start Server"
                    menuItem = new MenuItem();
                    menuItem.IsCheckable = true;
                    menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.AutoStartServer;
                    menuItem.Click += (sender, e) =>
                    {
                        Mubox.Configuration.MuboxConfigSection.Default.AutoStartServer = !Mubox.Configuration.MuboxConfigSection.Default.AutoStartServer;
                        Mubox.Configuration.MuboxConfigSection.Save();
                    };
                    menuItem.Header = "Auto Start Server";
                    quickLaunchMenuItems.Add(menuItem);
                }

                // Show Help
                quickLaunchMenuItems.Add(new Separator());
                menuItem = new MenuItem();
                menuItem.Click += (sender, e) =>
                {
                    helpCallback();
                };
                menuItem.Icon = Resources["imageMenuHelpIcon"];
                menuItem.Header = "Help...";
                quickLaunchMenuItems.Add(menuItem);

                // Cancel QuickLaunch Menu
                quickLaunchMenuItems.Add(new Separator());
                menuItem = new MenuItem();
                menuItem.Click += (sender, e) =>
                {
                    // NOP
                };
                menuItem.Header = "Cancel Menu";
                quickLaunchMenuItems.Add(menuItem);

                // Exit QuickLaunch Application
                menuItem = new MenuItem();
                menuItem.Click += (sender, e) =>
                {
                    exitApplicationCallback();
                    foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                    {
                        try
                        {
                            window.Close();
                        }
                        catch (Exception ex)
                        {
                            ex.Log();
                        }
                    }
                    Mubox.View.Server.ServerWindow.Instance = null;
                    exitApplicationCallback();
                };
                menuItem.Header = "E_xit Mubox";
                quickLaunchMenuItems.Add(menuItem);
                Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
                VerticalOffset = mousePosition.Y - 2;
                HorizontalOffset = mousePosition.X - 8;
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        private MenuItem CreateProfileShortcutMenu(Configuration.ProfileSettings profile)
        {
            var menuItem = default(MenuItem);

            // Shortcuts Menu Item
            List<object> quickLaunchClientShortcuts = new List<object>();
            Mubox.Configuration.ClientSettingsCollection clients = profile.Clients;

            menuItem = new MenuItem();
            menuItem.Header = "Start All";
            menuItem.Click += (sender, e) =>
            {
                LaunchProfileClients(profile);
            };
            quickLaunchClientShortcuts.Add(menuItem);

            // "Auto-Launch Game on Client Start"
            menuItem = new MenuItem();
            menuItem.IsCheckable = true;
            menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.AutoLaunchGame;
            menuItem.Click += (sender, e) =>
            {
                Mubox.Configuration.MuboxConfigSection.Default.AutoLaunchGame = !Mubox.Configuration.MuboxConfigSection.Default.AutoLaunchGame;
                Mubox.Configuration.MuboxConfigSection.Save();
            };
            menuItem.Header = "Auto-Launch Game on Client Start";
            menuItem.ToolTip =
                "Enable this option to Automatically Launch your Game when a Client started via the Quick Launch Menu." + Environment.NewLine +
                "Note, the game will not run until the client successfully connects to the Server, once a Server Connection is established the Launch will continue.";
            quickLaunchClientShortcuts.Add(menuItem);

            // "Enable Mouse Panning Fix"
            menuItem = new MenuItem();
            menuItem.IsCheckable = true;
            menuItem.IsChecked = profile.EnableMousePanningFix;
            menuItem.Click += (sender, e) =>
            {
                profile.EnableMousePanningFix = !profile.EnableMousePanningFix;
                Mubox.Configuration.MuboxConfigSection.Save();
            };
            menuItem.Header = "Enable Mouse Panning Fix";
            menuItem.ToolTip = "Only enable this option if you experience 'erratic' behavior when panning with the mouse. Properly written games do not have this problem.";
            quickLaunchClientShortcuts.Add(menuItem);

            // "Enable CAS Fix"
            menuItem = new MenuItem();
            menuItem.IsCheckable = true;
            menuItem.IsChecked = profile.EnableCASFix;
            menuItem.Click += (sender, e) =>
            {
                profile.EnableCASFix = !profile.EnableCASFix;
                Mubox.Configuration.MuboxConfigSection.Save();
            };
            menuItem.Header = "Enable Control-Alt-Shift Fix";
            menuItem.ToolTip = "Only enable this option if you have problems with the Control, Alt and Shift keys in your game.";
            quickLaunchClientShortcuts.Add(menuItem);

            // New Mubox Client
            menuItem = new MenuItem();
            menuItem.Click += (sender, e) =>
            {
                try
                {
                    // ensure client name is unique for current profile
                    var clientName = default(string);
                    while (true)
                    {
                        clientName = Mubox.View.PromptForClientNameDialog.PromptForClientName();
                        if (string.IsNullOrEmpty(clientName))
                        {
                            return;
                        }
                        //foreach (var L_profile in Mubox.Configuration.MuboxConfigSection.Default.Profiles.Cast<Mubox.Configuration.ProfileSettings>())
                        {
                            if (profile.Clients.GetExisting(clientName) != null)
                            {
                                MessageBox.Show("Name '" + clientName + "' is already in use, choose another.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                continue;
                            }
                        }
                        break;
                    }

                    var clientSettings = profile.Clients.CreateNew(clientName);
                    clientSettings.CanLaunch = true;
                    Mubox.Configuration.MuboxConfigSection.Save();

                    ClientState clientState = new ClientState(clientSettings, profile);
                    Mubox.View.Client.ClientWindow clientWindow = new Mubox.View.Client.ClientWindow(clientState);
                    clientWindow.Show();
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            };
            menuItem.Header = "_Configure New Client...";
            menuItem.Icon = Resources["imageSettingsIcon"];
            quickLaunchClientShortcuts.Add(menuItem);


            quickLaunchClientShortcuts.Add(new Separator());
            foreach (var client in clients.Cast<Mubox.Configuration.ClientSettings>())
            {
                // NOTE: a !CanLaunch client is one we know about, but likely exists on a remote computer. we do not (yet) support remote launch, and so these entries are hidden even though they may appear in the config file.
                if (!client.CanLaunch || (Mubox.View.Client.ClientWindowCollection.Instance.Count((dlg) => dlg.ClientState.Settings.Name.ToUpper() == client.Name.ToUpper()) != 0))
                {
                    continue;
                }

                client.PerformConnectOnLoad = true;
                quickLaunchClientShortcuts.Add(
                    QuickLaunchMenu_CreateClientItem(client.Name, profile)
                    );
            }

            menuItem = new MenuItem();
            menuItem.Header = profile.Name;
            menuItem.ItemsSource = quickLaunchClientShortcuts;

            var lMenuItem = new MenuItem();
            lMenuItem.IsCheckable = true;
            lMenuItem.IsChecked = (Mubox.Configuration.MuboxConfigSection.Default.Profiles.Default.Equals(profile.Name));
            lMenuItem.Header = "Active Profile";
            lMenuItem.Click += (s, e) =>
                {
                    Mubox.Configuration.MuboxConfigSection.Default.Profiles.ActiveProfile = profile;
                    Mubox.Configuration.MuboxConfigSection.Save();
                };
            quickLaunchClientShortcuts.Insert(0, lMenuItem);

            return menuItem;
        }

        private static void LaunchProfileClients(Mubox.Configuration.ProfileSettings profile)
        {
            Mubox.Configuration.MuboxConfigSection.Default.Profiles.ActiveProfile = profile;

            foreach (var o in profile.Clients)
            {
                var character = o as Mubox.Configuration.ClientSettings;
                if (character.CanLaunch)
                {
                    if (Mubox.View.Client.ClientWindowCollection.Instance.Count((dlg) => dlg.ClientState.Settings.Name.ToUpper().Equals(character.Name.ToUpper(), StringComparison.InvariantCultureIgnoreCase)) == 0)
                    {
                        Mubox.View.Client.ClientWindow clientWindow = new Mubox.View.Client.ClientWindow(new Mubox.Model.ClientState(character, profile));
                        clientWindow.Show();
                    }
                }
            }
        }

        private static MenuItem CreateMouseBufferMenuItem(MenuItem menuItem, List<MenuItem> mouseClickBufferMenu, double time)
        {
            menuItem = new MenuItem();
            menuItem.IsCheckable = true;
            menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.ClickBufferMilliseconds == time;
            menuItem.Click += (sender, e) =>
            {
                Mubox.Configuration.MuboxConfigSection.Default.ClickBufferMilliseconds = time;
                Mubox.Configuration.MuboxConfigSection.Save();
            };
            menuItem.Header = time == 0.0 ? "Disabled" : ((int)time).ToString() + "ms";
            menuItem.ToolTip = "Use this option to set the Click Buffer to " + menuItem.Header;
            mouseClickBufferMenu.Add(menuItem);
            return menuItem;
        }

        public static void CreateServerUI()
        {
            try
            {
                if (Mubox.View.Server.ServerWindow.Instance != null)
                {
                    try
                    {
                        Mubox.View.Server.ServerWindow.Instance.Close();
                        Mubox.View.Server.ServerWindow.Instance = null;
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                }
                Mubox.View.Server.ServerWindow.Instance = new Mubox.View.Server.ServerWindow();
                Mubox.View.Server.ServerWindow.Instance.Closing += (L_s, L_e) =>
                {
                    Mubox.View.Server.ServerWindow.Instance = null;
                };
                Mubox.View.Server.ServerWindow.Instance.Show();
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        private MenuItem QuickLaunchMenu_CreateClientItem(string clientName, Mubox.Configuration.ProfileSettings profile)
        {
            RoutedEventHandler clientStartEventHandler = (sender, e) =>
            {
                var clientSettings = profile.Clients.GetExisting(clientName);
                if (clientSettings != null)
                {
                    Mubox.Configuration.MuboxConfigSection.Default.Profiles.ActiveProfile = profile;
                    Mubox.Configuration.MuboxConfigSection.Save();
                    ClientState clientState = new ClientState(clientSettings, profile);
                    Mubox.View.Client.ClientWindow clientWindow = new Mubox.View.Client.ClientWindow(clientState);
                    clientWindow.Show();
                }
            };

            RoutedEventHandler clientDeleteEventHandler = (sender, e) =>
            {
                // TODO: add confirmation dialog to avoid accidental deletions
                if (profile.Clients.Remove(clientName))
                {
                    // TODO: we need to clean up the sandbox account as well
                    Mubox.Configuration.MuboxConfigSection.Default.Profiles.ActiveProfile = profile;
                    Mubox.Configuration.MuboxConfigSection.Save();
                }
            };

            MenuItem clientMenuItem = new MenuItem();
            clientMenuItem.Header = clientName;
            // clientMenuItem.Click += clientLaunchEventHandler;

            MenuItem clientLaunchMenuItem = new MenuItem();
            clientLaunchMenuItem.Header = "_Start";
            clientLaunchMenuItem.Click += clientStartEventHandler;

            MenuItem clientDeleteMenuItem = new MenuItem();
            clientDeleteMenuItem.Header = "_Remove From List";
            clientDeleteMenuItem.Click += clientDeleteEventHandler;

            clientMenuItem.ItemsSource = new object[] {
                clientLaunchMenuItem,
                clientDeleteMenuItem
            };

            return clientMenuItem;
        }
    }
}