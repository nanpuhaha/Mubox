using System;

namespace Mubox.Extensibility
{
    public interface IExtension
        : IServiceProvider
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