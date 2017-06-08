using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;

namespace Mubox.Model
{
    public class ClientState : IDisposable
    {
        public WinAPI.SandboxApi.Sandbox Sandbox { get; set; }

        public Mubox.Configuration.ProfileSettings Profile { get; set; }

        private ClientState()
        {
            Timer = new System.Timers.Timer();
            Timer.Elapsed += Timer_Elapsed;
            Timer.Interval = 100;
            Timer.Start();
        }

        public ClientState(Mubox.Configuration.ClientSettings settings, Mubox.Configuration.ProfileSettings profile)
            : this()
        {
            Settings = settings;
            Profile = profile;
        }

        public Configuration.ClientSettings Settings { get; private set; }

        #region State Monitor

        private DateTime clientState_lastTimerTick = DateTime.Now;
        private long NetworkClientNextServerUpdateTime;
        private bool clientState_windowPositionRestored;
        private bool clientState_windowBorderRemoved;

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            MonitorGameProcess();

            // update server with current client info, this doubles as "stale socket detection" code
            if (NetworkClient != null)
            {
                // trim memory
                MonitorMemoryMB();

                try
                {
                    MonitorWindowBorder(false);
                }
                catch (Exception ex)
                {
                    ex.Log();
                }

                try
                {
                    MonitorWindowPosition();
                }
                catch (Exception ex)
                {
                    ex.Log();
                }

                try
                {
                    MonitorNetworkClient();
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }
            clientState_lastTimerTick = DateTime.Now;
        }

        public void MonitorWindowBorder(bool force)
        {
            bool windowBorderRemoved = force ? false : this.clientState_windowBorderRemoved;
            if (this.Settings.WindowHandle != IntPtr.Zero)
            {
                if (Settings.RemoveWindowBorderEnabled && !windowBorderRemoved)
                {
                    this.clientState_windowBorderRemoved = true;
                    WinAPI.Windows.WindowStyles ws = (WinAPI.Windows.WindowStyles)WinAPI.Windows.GetWindowLong(this.Settings.WindowHandle, WinAPI.Windows.GWL.GWL_STYLE);
                    WinAPI.Windows.WindowStyles wsNew = ws;
                    if (WinAPI.Windows.WindowStyles.WS_BORDER == (ws & WinAPI.Windows.WindowStyles.WS_BORDER))
                    {
                        wsNew ^= WinAPI.Windows.WindowStyles.WS_BORDER;
                    }
                    if (WinAPI.Windows.WindowStyles.WS_CAPTION == (ws & WinAPI.Windows.WindowStyles.WS_CAPTION))
                    {
                        wsNew ^= WinAPI.Windows.WindowStyles.WS_CAPTION;
                    }
                    if (WinAPI.Windows.WindowStyles.WS_EX_STATICEDGE == (ws & WinAPI.Windows.WindowStyles.WS_EX_STATICEDGE))
                    {
                        wsNew ^= WinAPI.Windows.WindowStyles.WS_EX_STATICEDGE;
                    }
                    if (WinAPI.Windows.WindowStyles.WS_SIZEBOX == (ws & WinAPI.Windows.WindowStyles.WS_SIZEBOX))
                    {
                        wsNew ^= WinAPI.Windows.WindowStyles.WS_SIZEBOX;
                    }
                    if (ws != wsNew)
                    {
                        WinAPI.Windows.SetWindowLong(this.Settings.WindowHandle, WinAPI.Windows.GWL.GWL_STYLE, (uint)wsNew);
                    }
                }
            }
            else
            {
                this.clientState_windowBorderRemoved = false;
            }
        }

        private void MonitorWindowPosition()
        {
            if (DateTime.Now.Ticks < RememberWindowPositionNextCheckTime)
            {
                return;
            }
            RememberWindowPositionNextCheckTime = DateTime.Now.AddSeconds(3).Ticks;

            if (this.Settings.RememberWindowPosition)
            {
                if (this.Settings.WindowHandle != IntPtr.Zero)
                {
                    SetGameWindowPosition(false, this.Settings.WindowPosition, this.Settings.WindowSize);
                }
                else
                {
                    clientState_windowPositionRestored = false;
                }
            }
        }

        private void MonitorNetworkClient()
        {
            if (DateTime.Now.Ticks < NetworkClientNextServerUpdateTime)
            {
                return;
            }
            NetworkClientNextServerUpdateTime = DateTime.Now.AddSeconds(9).Ticks;

            Mubox.Control.Network.Client networkClient = this.NetworkClient;
            if (networkClient != null)
            {
                networkClient.DisplayName = this.Settings.Name;
                networkClient.ProfileName = this.Profile.Name;
                networkClient.WindowStationHandle = this.WindowStationHandle;
                networkClient.WindowDesktopHandle = this.WindowDesktopHandle;
                networkClient.WindowHandle = this.Settings.WindowHandle;
                networkClient.SendClientConfig();
                networkClient.SendPerformanceInfo(this.GameProcess);
            }
        }

        private void MonitorMemoryMB()
        {
            if (DateTime.Now.Ticks < MemoryMBNextCheckTime)
            {
                return;
            }
            MemoryMBNextCheckTime = DateTime.Now.AddSeconds(60).Ticks;

            if (this.Settings.MemoryMB > 0)
            {
                if (this.GameProcess != null)
                {
                    try
                    {
                        this.GameProcess.MaxWorkingSet = new IntPtr(this.Settings.MemoryMB * 1024 * 1024);
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                }
            }
        }

        private System.Timers.Timer Timer { get; set; }

        #endregion State Monitor

        private Mubox.Control.Network.Client _networkClient;

        public Mubox.Control.Network.Client NetworkClient
        {
            get { return _networkClient; }
            set
            {
                if (_networkClient != value)
                {
                    if (_networkClient != null)
                    {
                        _networkClient.ClientActivated -= _networkClient_ClientActivated;
                    }
                    if (value != null)
                    {
                        value.ClientActivated += _networkClient_ClientActivated;
                    }
                    _networkClient = value;
                }
            }
        }

        private void _networkClient_ClientActivated(object sender, EventArgs e)
        {
            Profile.ActiveClient = Settings;
        }

        public override string ToString()
        {
            return (Settings.Name ?? "NULL").ToString();
        }

        public IntPtr WindowStationHandle { get; set; }

        public IntPtr WindowDesktopHandle { get; set; }

        public Window SettingsWindow { get; set; }

        private long MemoryMBNextCheckTime;
        private long RememberWindowPositionNextCheckTime;

        public void SetGameWindowPosition(bool force, Point position, Size size)
        {
            if (this.Settings.WindowHandle == IntPtr.Zero)
            {
                return;
            }

            bool windowPositionRestored = (force ? false : this.clientState_windowPositionRestored);

            WinAPI.Windows.RECT windowRect;
            if (!windowPositionRestored && (this.Settings.WindowSize.Width > 0))
            {
                WinAPI.Windows.GetWindowRect(this.Settings.WindowHandle, out windowRect);
                if (!force && ((windowRect.Top == position.Y) && (windowRect.Left == position.X) && (size.Width == (windowRect.Right - windowRect.Left)) && (size.Height == (windowRect.Bottom - windowRect.Top))))
                {
                    this.clientState_windowPositionRestored = true;
                }
                else
                {
                    WinAPI.Windows.SetWindowPos(this.Settings.WindowHandle, (IntPtr)Mubox.WinAPI.Windows.Position.HWND_TOP,
                        (int)position.X, (int)position.Y, (int)size.Width, (int)size.Height, (uint)(WinAPI.Windows.Options.SWP_ASYNCWINDOWPOS | WinAPI.Windows.Options.SWP_NOZORDER));
                }
            }
            else
            {
                if (WinAPI.Windows.GetWindowRect(this.Settings.WindowHandle, out windowRect))
                {
                    this.Settings.WindowPosition = new Point(windowRect.Left, windowRect.Top);
                    this.Settings.WindowSize = new Size(windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top);
                    Mubox.Configuration.MuboxConfigSection.Save();
                }
            }
        }

        private System.Diagnostics.Process _gameProcess;

        public System.Diagnostics.Process GameProcess
        {
            get
            {
                return _gameProcess;
            }
            set
            {
                if (_gameProcess != null && value != null && _gameProcess.Id == value.Id)
                {
                    // avoid stack overflow exception due to ElectChildProcess call, while still allowing agressive crawling of child processes whenever current game process changes
                    return;
                }
                _gameProcess = value;
                if (value != null)
                {
                    var alreadyTracking = false;
                    lock (_processes)
                    {
                        foreach (var L_process in _processes)
                        {
                            if (L_process.Id == value.Id)
                            {
                                alreadyTracking = true;
                            }
                        }
                        if (!alreadyTracking)
                        {
                            _processes.Add(value);
                        }
                    }
                    if (!alreadyTracking)
                    {
                        TryElectProcessOnExit(value);
                        ("TrackingGameProcess for " + this.Settings.Name + " found pid=" + value.Id + " hwnd=" + value.MainWindowHandle.ToString("X")).LogInfo();
                    }
                    ElectChildProcess(value.Id);
                }
            }
        }

        private long GameProcessNextCheckTime = 0L;

        public event EventHandler<EventArgs> GameProcessExited;

        public event EventHandler<EventArgs> GameProcessFound;

        private IList<Process> _processes = new List<Process>();

        [SuppressMessage("Microsoft.Security", "CA2122")]
        public void MonitorGameProcess()
        {
            if (DateTime.Now.Ticks < GameProcessNextCheckTime)
            {
                return;
            }

            GameProcessNextCheckTime = DateTime.Now.AddSeconds(5).Ticks;

            Process gameProcess = GameProcess;
            if (gameProcess == null)
            {
                ("NoGameProcess for " + this.Settings.Name).Log();
                return;
            }

            try
            {
                var pid = gameProcess.Id;
                gameProcess.Refresh();

                if (ElectChildProcess(pid))
                {
                    // gameProcess = GameProcess;
                }
                else if (gameProcess.HasExited || gameProcess.MainWindowHandle == IntPtr.Zero)
                {
                    if (gameProcess.HasExited)
                    {
                        lock (_processes)
                        {
                            _processes.Remove(gameProcess);
                        }
                    }
                    if (_processes.Count > 0)
                    {
                        var processes = _processes.ToArray();
                        foreach (var L_process in processes)
                        {
                            try
                            {
                                L_process.Refresh();
                                if (L_process.HasExited)
                                {
                                    lock (_processes)
                                    {
                                        _processes.Remove(L_process);
                                    }
                                }
                                else if (L_process.MainWindowHandle != IntPtr.Zero)
                                {
                                    ("PreviousGameProcess for " + this.Settings.Name + " pid=" + L_process.Id + " hwnd=" + L_process.MainWindowHandle.ToString("X")).LogInfo();
                                    pid = L_process.Id;
                                    GameProcess = L_process;
                                    Sandbox.Process = L_process;
                                    break;
                                }
                            }
                            catch
                            {
                                lock (_processes)
                                {
                                    _processes.Remove(L_process);
                                }
                            }
                        }
                    }
                    if (_processes.Count == 0)
                    {
                        ("GameExitDetected for " + this.Settings.Name).LogWarn();
                        GameProcess = null;
                        Sandbox.Process = null;
                        Settings.WindowHandle = IntPtr.Zero;
                        if (NetworkClient != null)
                        {
                            NetworkClient.WindowHandle = Settings.WindowHandle;
                        }
						GameProcessExited?.Invoke(this, new EventArgs());
					}
                }
                else
                {
                    IntPtr newWindowHandle = IntPtr.Zero;
                    if (gameProcess.Responding)
                    {
                        newWindowHandle = gameProcess.MainWindowHandle;
                        if (NetworkClient != null)
                        {
                            NetworkClient.WindowHandle = newWindowHandle;
                        }
                        bool newProcessWindow = Settings.WindowHandle != newWindowHandle;
                        if (newProcessWindow)
                        {
                            ("NewGameProcess for " + this.Settings.Name).Log();
                            WinAPI.SandboxApi.TryFixMultilaunch(gameProcess);
                            Settings.WindowHandle = newWindowHandle;
							GameProcessFound?.Invoke(this, new EventArgs());
						}
                        if (gameProcess.PriorityClass != ProcessPriorityClass.Idle)
                        {
                            gameProcess.PriorityClass = ProcessPriorityClass.Idle;
                        }
                        ManageProcessorAffinity(gameProcess);
                    }
                    else
                    {
                        ("GameProcessNotResponding for " + this.Settings.Name).LogWarn();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        private bool ElectChildProcess(int pid)
        {
            var electedNewProcess = false;
            var children = Mubox.WinAPI.Toolhelp32.GetChildProcesses(pid);
            if (children != null && children.Count() > 0)
            {
                foreach (var child in children)
                {
                    var hwnd = child.MainWindowHandle;
                    var alreadyTracking = false;
                    lock (_processes)
                    {
                        foreach (var L_process in _processes)
                        {
                            if (L_process.Id == child.Id)
                            {
                                alreadyTracking = true;
                            }
                        }
                        if (!alreadyTracking)
                        {
                            _processes.Add(child);
                            TryElectProcessOnExit(child);
                            ("TrackingGameProcess for " + this.Settings.Name + " found pid=" + child.Id + " hwnd=" + hwnd.ToString("X")).LogInfo();
                        }
                    }
                    if (!ElectChildProcess(child.Id) && hwnd != IntPtr.Zero && GameProcess != child && !child.HasExited)
                    {
                        ("ElectingGameProcess for " + this.Settings.Name + " found pid=" + child.Id + " hwnd=" + hwnd.ToString("X")).LogInfo();
                        GameProcess = child;
                        Sandbox.Process = child;
                        electedNewProcess = true;
                    }
                }
                return electedNewProcess;
            }
            return electedNewProcess;
        }

        private void TryElectProcessOnExit(Process child)
        {
            try
            {
                int pid = child.Id;
                // using closure for access to 'child' within following handler
                child.Exited += (s, e) =>
                    {
                        ElectChildProcess(pid);
                    };
            }
            catch { /* NOP */ }
        }

        [SuppressMessage("Microsoft.Security", "CA2122")]
        public void ManageProcessorAffinity(Process process)
        {
            if (Settings.ProcessorAffinity == 0)
            {
                uint processorAffinity = 1;
                for (int i = 1; i < Environment.ProcessorCount; i++)
                {
                    processorAffinity |= (uint)(1 << i);
                }
                process.ProcessorAffinity = new IntPtr(processorAffinity);
            }
            else if (Settings.ProcessorAffinity == 1)
            {
                process.ProcessorAffinity = new IntPtr(1);
            }
            else
            {
                process.ProcessorAffinity = new IntPtr(1 << ((int)Settings.ProcessorAffinity - 1));
            }
        }

        private static string TryResolveApplicationPath(string defaultIfNotFound)
        {
            List<string> applicationPathPermutations = new List<string>();

            applicationPathPermutations.Add(@"Users\Public\Games\World of Warcraft\wow.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar));
            applicationPathPermutations.Add(@"Program Files\World of Warcraft\wow.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar));
            applicationPathPermutations.Add(@"Program Files (x86)\World of Warcraft\wow.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar));
            applicationPathPermutations.Add(@"World of Warcraft\wow.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar));
            applicationPathPermutations.Add(@"WoW\wow.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar));
            applicationPathPermutations.Add(@"wow.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar));

            for (int i = (int)'C'; i <= (int)'Z'; i++)
            {
                string rootFolder = ((char)i) + @":\".Replace('\\', System.IO.Path.DirectorySeparatorChar);
                if (System.IO.Directory.Exists(rootFolder))
                {
                    foreach (string applicationPath in applicationPathPermutations)
                    {
                        if (System.IO.File.Exists(rootFolder + applicationPath))
                        {
                            return rootFolder + applicationPath;
                        }
                    }
                }
            }

            return defaultIfNotFound;
        }

        private static string TryResolveIsolationPath(string defaultIfNotFound)
        {
            System.IO.DriveInfo mostLikelyDrive = null;
            System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();

            foreach (System.IO.DriveInfo drive in drives)
            {
                try
                {
                    if (!drive.IsReady)
                    {
                        continue;
                    }
                    if (mostLikelyDrive == null || mostLikelyDrive.AvailableFreeSpace < drive.AvailableFreeSpace || defaultIfNotFound.StartsWith(drive.RootDirectory.FullName.Substring(0, 3)))
                    {
                        mostLikelyDrive = drive;
                    }
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }

            return mostLikelyDrive == null
                ? defaultIfNotFound
                : mostLikelyDrive.RootDirectory.FullName;
        }

        #region IDisposable Members

        ~ClientState()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        [SuppressMessage("Microsoft.Security", "CA2122")]
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
            if (Timer != null)
            {
                try
                {
                    Timer.Stop();
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
                Timer = null;
            }
            if (this.GameProcess != null)
            {
                // TODO: killing game process should only be performed when Mubox launched the process
                try
                {
                    if (!GameProcess.HasExited)
                    {
                        this.GameProcess.CloseMainWindow();
                        if (!this.GameProcess.WaitForExit(5000)) // TODO: arbitrary, and it's unclear why some games refuse to process WM_CLOSE properly
                        {
                            if (!this.GameProcess.HasExited)
                            {
                                this.GameProcess.Kill();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
                this.GameProcess = null;
            }
            if (this.NetworkClient != null)
            {
                try
                {
                    this.NetworkClient.Disconnect();
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
                this.NetworkClient = null;
            }
            if (this.SettingsWindow != null)
            {
                this.SettingsWindow = null;
            }
            if (this.Sandbox != null)
            {
                var sandbox = this.Sandbox;
                this.Sandbox = null;
            }
        }

        #endregion IDisposable Members
    }
}