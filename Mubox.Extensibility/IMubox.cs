using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensibility
{
    /// <summary>
    /// <para>Provides access to Mubox</para>
    /// </summary>
    public interface IMubox
        : IServiceProvider
    {
        IMuboxClient ActiveClient { get; }

        ICollection<IMuboxClient> Clients { get; }

        /// <summary>
        /// <para>Global, Virtualized Keyboard</para>
        /// <para>Events raised on this instance regardless of 'Clients'</para>
        /// <para>Output to this instance is dispatched to 'the Active Client'</para>
        /// </summary>
        Input.VirtualKeyboard Keyboard { get; }

        /// <summary>
        /// <para>Global, Virtualized Mouse</para>
        /// <para>Events raised on this instance regardless of 'Clients'</para>
        /// <para>Output to this instance is dispatched to 'the Active Client'</para>
        /// </summary>
        Input.VirtualMouse Mouse { get; }

        /// <summary>
        /// <para>Raised when 'the Active Client' changes.</para>
        /// </summary>
        event EventHandler<ClientEventArgs> ActiveClientChanged;

        //void AddServiceProvider(IServiceProvider provider);

        //void RemoveServiceProvider(IServiceProvider provider);
    }
}
