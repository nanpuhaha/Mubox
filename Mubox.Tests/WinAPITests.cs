using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;

namespace Mubox.Tests
{
    [TestClass]
    public class WinAPITests
    {
        [TestMethod]
        public void GetSystemHandleInformationBVT()
        {
            var mutexWasFound = false;

            var mutexNames = new string[] 
            {
                "MBX_TEST_MUTEX",
            };
            var createdNew = default(bool);
            var mutex = new System.Threading.Mutex(true, "MBX_TEST_MUTEX", out createdNew);
            Assert.IsTrue(createdNew);
            var currentProcessHandle = System.Diagnostics.Process.GetCurrentProcess().Handle;
            var sandbox = new
            {
                Process = System.Diagnostics.Process.GetCurrentProcess(),
            };
            
            try
            {

                var result = Mubox.WinAPI.SandboxApi.GetSystemHandleInformation();
                Assert.IsTrue(result.Count > 0);
                Assert.IsNotNull(result.Handles);
                Assert.IsTrue(result.Handles.Length > 0);
                var entries = result
                    .Handles
                    .Where(entry => entry.AccessMask != 0x0012019f /* known issue with hanging file handles (pipes?), which we're not interested in */)
                    .ToList();
                Assert.IsTrue(entries.Count > 0);


                foreach (var entry in entries)
                {
                    var handle = new IntPtr(entry.HandleValue);
                    IntPtr hDupe;
                    var duped = WinAPI.SandboxApi.DuplicateHandle(
                        sandbox.Process.Handle,
                        handle,
                        System.Diagnostics.Process.GetCurrentProcess().Handle,
                        out hDupe,
                        0,
                        false,
                        WinAPI.SandboxApi.DUPLICATE_SAME_ACCESS);
                    int err = Marshal.GetLastWin32Error();
                    if (err == 0)
                    {
                        if (duped && hDupe != IntPtr.Zero)
                        {
                            var ntstatus = WinAPI.SandboxApi.NTSTATUS.InfoLengthMismatch;
                            var cbbuf = 1024;
                            var buf = Marshal.AllocHGlobal(cbbuf);
                            try
                            {
                                var returnLength = 0;
                                while (ntstatus == WinAPI.SandboxApi.NTSTATUS.InfoLengthMismatch)
                                {
                                    cbbuf *= 2;
                                    buf = Marshal.ReAllocHGlobal(buf, new IntPtr(cbbuf));
                                    ntstatus = WinAPI.SandboxApi.NtQueryObject(
                                        hDupe,
                                        WinAPI.SandboxApi.OBJECT_INFORMATION_CLASS.ObjectName,
                                        buf,
                                        cbbuf,
                                        out returnLength);
                                }
                                if (ntstatus == WinAPI.SandboxApi.NTSTATUS.Success)
                                {
                                    var typeName = Marshal.PtrToStringUni(buf + 4);
                                    if (!string.IsNullOrEmpty(typeName))
                                    {
                                        ("pid=" + entry.ProcessID + " handle=0x" + handle.ToInt64().ToString("X") + " type=" + entry.ObjectType + " name=" + typeName).Log();
                                        // if in list of mutexes, close it
                                        if (mutexNames.Count(s => typeName.Contains(s)) > 0)
                                        {
                                            IntPtr hClose;
                                            var closed = WinAPI.SandboxApi.DuplicateHandle(
                                                sandbox.Process.Handle,
                                                handle,
                                                currentProcessHandle,
                                                out hClose,
                                                0,
                                                false,
                                                WinAPI.SandboxApi.DUPLICATE_SAME_ACCESS | WinAPI.SandboxApi.DUPLICATE_CLOSE_SOURCE);
                                            if (closed)
                                            {
                                                WinAPI.SandboxApi.CloseHandle(hClose);
                                                ("Closed Remote Handle: pid=" + sandbox.Process.Id + " handle=" + handle.ToInt64().ToString("X") + " status=" + ntstatus + " type=" + entry.ObjectType + " name=" + typeName).Log();
                                                mutexWasFound = true;
                                            }
                                            else
                                            {
                                                ("Failed to Closed Remote Handle: pid=" + sandbox.Process.Id + " handle=" + handle.ToInt64().ToString("X") + " status=" + ntstatus + " type=" + entry.ObjectType + " name=" + typeName).Log();
                                            }
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                Marshal.FreeHGlobal(buf);
                            }

                            var handleClosed = WinAPI.SandboxApi.CloseHandle(hDupe);
                            Assert.IsTrue(handleClosed);
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    if (sandbox != null && sandbox.Process != null)
                    {
                        sandbox.Process.Dispose();
                        sandbox = null;
                    }
                    if (mutex != null)
                    {
                        mutex.ReleaseMutex();
                        mutex.Dispose();
                    }
                }
                catch { }
            }

            Assert.IsTrue(mutexWasFound);
        }
    }
}
