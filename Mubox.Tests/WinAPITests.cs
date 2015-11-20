using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mubox.Tests
{
    [TestClass]
    public class WinAPITests
    {
        [TestMethod]
        public void CloseNamedMutexBVT()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var createdNew = default(bool);
            var mutex = new System.Threading.Mutex(false, "AN-Mutex-TEST", out createdNew);
            Assert.IsTrue(createdNew);
            var mutexWasClosed = WinAPI.SandboxApi.CloseNamedMutexes(process, new string[]
                {
                    "AN-Mutex",
                    "AN-Mutex-Window-Guild Wars 2",
                    "AN-Mutex-Install-101",
                    // TODO: move list to config
                });
            Assert.IsTrue(mutexWasClosed);
        }
    }
}