using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensibility
{
	public interface ISysTrayMenu
	{
		/// <summary>
		/// <para>Allow extensions to create their own menu items on the system tray contextmenu.</para>
		/// </summary>
		/// <param name="header"></param>
		/// <param name="onClick"></param>
		/// <returns>Token which can be used to destroy menu item using `DestroyMenuItem` method.</returns>
		string AddMenuItem(string header, Extensibility.ProxyEventHandler<EventArgs> proxyEventHandler);

		/// <summary>
		/// <para>Same as 'AddMenuItem' but will insert item at specific position.</para>
		/// <para>Knowing correct position is guesswork at this point.</para>
		/// </summary>
		/// <param name="header"></param>
		/// <param name="proxyEventHandler"></param>
		/// <returns></returns>
		string InsertMenuItem(int index, string header, Extensibility.ProxyEventHandler<EventArgs> proxyEventHandler);

		/// <summary>
		/// <para>Destroys a menu item previously created with `CreateMenuItem` method.</para>
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		void DestroyMenuItem(string token);
	}
}
