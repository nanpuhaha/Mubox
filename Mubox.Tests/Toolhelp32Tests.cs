using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
