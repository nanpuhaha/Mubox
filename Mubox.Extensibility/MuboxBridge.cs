﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mubox.Extensibility
{
    /// <summary>
    /// <para>This object allows an Extension to interact with Mubox</para>
    /// <para>One instance of this object exists for each extension.</para>
    /// </summary>
    public class MuboxBridge
        : MarshalByRefObject, IMubox
    {
        public MuboxBridge()
        {
            Clients = new List<IMuboxClient>();
            Keyboard = new Input.VirtualKeyboard(null);
            Mouse = new Input.VirtualMouse(null);
        }

        public Input.IVirtualKeyboard Keyboard { get; private set; }

        public Input.IVirtualMouse Mouse { get; private set; }

        public ICollection<IMuboxClient> Clients { get; private set; }

        public IMuboxClient ActiveClient { get; private set; }

        public event EventHandler<ClientEventArgs> ActiveClientChanged;

        internal void OnActiveClientChanged(IMuboxClient sender)
        {
            ActiveClient = sender;
            if (ActiveClientChanged != null)
            {
                ActiveClientChanged(sender, new ClientEventArgs
                {
                    Client = sender,
                });
            }
        }
    }
}
