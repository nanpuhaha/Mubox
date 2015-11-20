using System;

namespace Mubox.Extensibility
{
    /// <summary>
    /// <para>Mubox Client Interface</para>
    /// </summary>
    public interface IMuboxClient
    {
        /// <summary>
        /// Gets the name of the client, this is the same name as input by the User during Client Creation.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// <para>Client Affine, Virtualized Keyboard</para>
        /// <para>Events raised on this instance only for 'this Client'</para>
        /// <para>Output to this instance is only sent to 'this Client'</para>
        /// </summary>
        Input.VirtualKeyboard Keyboard { get; }

        /// <summary>
        /// <para>Client Affine, Virtualized Mouse</para>
        /// <para>Events raised on this instance only for 'this Client'</para>
        /// <para>Output to this instance is only sent to 'this Client'</para>
        /// </summary>
        Input.VirtualMouse Mouse { get; }

        /// <summary>
        /// <para>Raised when Mubox 'attaches' to a game Process, i.e. Game Launched</para>
        /// </summary>
        event EventHandler<ClientEventArgs> Attached;

        /// <summary>
        /// <para>Raised when Mubox 'detaches' from a game process, i.e. Game Exited</para>
        /// </summary>
        event EventHandler<ClientEventArgs> Detached;

        /// <summary>
        /// <para>Allows an extension to generate Keyboard Input events.</para>
        /// </summary>
        /// <param name="e">Input.KeyboardEventArgs</param>
        void KeyPress(WinAPI.VK key);

        /// <summary>
        /// <para>Allows an extension to generate Mouse Input events.</para>
        /// </summary>
        /// <param name="e">Input.KeyboardEventArgs</param>
        void MouseEvent(Input.MouseEventArgs e);
    }
}