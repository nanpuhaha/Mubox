using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensibility
{
    public interface IExtension
    {
        /// <summary>
        /// <para>Called when an Extension is first Loaded, this is the earliest point an Extension is allowed to communicate with Mubox</para>
        /// </summary>
        /// <param name="mubox">A reference to an IMubox instance.</param>
        void OnLoad(IMubox mubox);

        /// <summary>
        /// <para>Called when an Extension should prepare to be unloaded (release references, save data, close any open files, unregister event handlers, etc.)</para>
        /// </summary>
        void OnUnload();
    }
}
