using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Mubox.Extensibility
{
    public class Loader
        : MarshalByRefObject, IServiceProvider
    {
        private Assembly _assembly;
        private IExtension _extension;
        private ConcurrentQueue<dynamic> _dispatchQueue;
        private Thread _dispatchThread;
        private ManualResetEvent _exitYet;
        private ManualResetEvent _shutdownYet;

        public Loader()
        {
        }

        #region Initialization

        public void Initialize(IMubox mubox, string extensionDllPath)
        {
            if (_extension != null)
            {
                return;
            }
            var inner = default(Exception);
            try
            {
                var name = Path.GetFileNameWithoutExtension(extensionDllPath);
                LoadExtensionDLL(extensionDllPath);
                if (CreateExtensionInstance())
                {
                    InitializeDispatchRuntime(mubox, name);
                    return;
                }
                //mubox.AddServiceProvider(this); // TODO: need to attempt to remove this when loader is unloading
            }
            catch (Exception ex)
            {
                ex.Log();
                inner = ex;
            }
            throw new Exception("No extension found or other error occured, check Inner Exception property.", inner);
        }

        private bool CreateExtensionInstance()
        {
            foreach (var type in _assembly.GetTypes())
            {
                if (type.IsClass && !type.IsAbstract)
                {
                    var iface = type.GetInterface("Mubox.Extensibility.IExtension");
                    if (iface != null)
                    {
                        _extension = (IExtension)Activator.CreateInstance(type);
                        return true;
                    }
                }
            }
            ("Failed to locate Extension Objects in Assembly: " + _assembly.FullName).Log();
            return false;
        }

        private void InitializeDispatchRuntime(IMubox mubox, string name)
        {
            _dispatchQueue = new ConcurrentQueue<dynamic>();
            _exitYet = new ManualResetEvent(false);
            _shutdownYet = new ManualResetEvent(false);
            _dispatchThread = new Thread(DispatchThreadMain);
            _dispatchThread.IsBackground = true;
            _dispatchThread.Name = name;
            _dispatchThread.Start(mubox);
        }

        private void DispatchThreadMain(object obj)
        {
            ("Dispatch Thread Starting: " + Thread.CurrentThread.Name).Log();
            try
            {
                if (_extension == null)
                {
                    ("Extension Not Loaded, Dispatch Runtime Aborting").Log();
                }
                else
                {
                    _extension.OnLoad((IMubox)obj);
                    while (!_exitYet.WaitOne(50)) // 20fps
                    {
                        dynamic command = default(dynamic);
                        while (_dispatchQueue.TryDequeue(out command))
                        {
                            switch ((string)command.Instruction)
                            {
                                case "STOP":
                                    _exitYet.Set();
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    _extension.OnUnload();
                }
            }
            finally
            {
                ("Dispatch Thread \"" + Thread.CurrentThread.Name + "\" Stopping").Log();
                _shutdownYet.Set();
            }
        }

        private void LoadExtensionDLL(string extensionDllPath)
        {
            var extensionPdbPath = extensionDllPath.Replace(".dll", ".pdb");
            var pdb = default(byte[]);
            try
            {
                if (File.Exists(extensionDllPath))
                {
                    using (var stream = File.Open(extensionPdbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        pdb = new byte[stream.Length];
                        for (int i = 0; i < pdb.Length; i += stream.Read(pdb, i, pdb.Length - i)) ;
                    }
                }
            }
            catch
            {
                pdb = default(byte[]);
            }

            var dll = default(byte[]);
            using (var stream = File.Open(extensionDllPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                dll = new byte[stream.Length];
                for (int i = 0; i < dll.Length; i += stream.Read(dll, i, dll.Length - i)) ;
            }

            _assembly = Assembly.Load(dll, pdb);
        }

        #endregion Initialization

        public void ExtensionStop()
        {
            _dispatchQueue.Enqueue(new { Instruction = "STOP" });
            if (!_shutdownYet.WaitOne(10000))
            {
                ("Extension DLL \"" + _dispatchThread.Name + "\" is taking too long to unload, not waiting.").Log();
            }
        }

        public override object InitializeLifetimeService()
        {
            return base.InitializeLifetimeService().InitializeLease();
        }

        public object GetService(Type serviceType)
        {
            if (_extension != null)
            {
                try
                {
                    return _extension.GetService(serviceType);
                }
                catch { }
            }
            return null;
        }
    }
}