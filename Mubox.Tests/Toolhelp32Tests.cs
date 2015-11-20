using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mubox.Tests
{
    [TestClass]
    public class Toolhelp32Tests
    {
        [TestMethod]
        public void Toolhelp32BVT()
        {
            var result = WinAPI.Toolhelp32.GetChildProcesses(System.Diagnostics.Process.GetCurrentProcess().Id);
            Assert.IsNotNull(result);
        }
    }
}