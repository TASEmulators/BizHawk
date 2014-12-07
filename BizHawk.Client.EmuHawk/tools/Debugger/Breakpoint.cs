using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public class BreakpointList : List<Breakpoint>
	{
		public Action Callback { get; set; }

		public void Add(IDebuggable core, uint address, MemoryCallbackType type)
		{
			Add(new Breakpoint(core, Callback, address, type));
		}

		public new void Clear()
		{
			foreach (var breakpoint in this)
			{
				breakpoint.Active = false;
			}

			base.Clear();
		}

		// TODO: override all ways to remove
	}

	public class Breakpoint
	{
		private bool _active;
		private readonly IDebuggable _core;

		public Breakpoint(IDebuggable core, Action callBack, uint address, MemoryCallbackType type, bool enabled = true)
		{
			_core = core;

			Callback = callBack;
			Address = address;
			Active = enabled;

			if (enabled)
			{
				AddCallback();
			}
		}

		public Action Callback { get; set; }
		public uint Address { get; set; }
		public MemoryCallbackType Type { get; set; }

		public bool Active
		{
			get
			{
				return _active;
			}

			set
			{
				if (!value)
				{
					RemoveCallback();
				}

				if (!_active && value) // If inactive and changing to active
				{
					AddCallback();
				}

				_active = value;
			}
		}

		private void AddCallback()
		{
			_core.MemoryCallbacks.Add(new MemoryCallback(Type, "Pause", Callback, Address));
		}

		private void RemoveCallback()
		{
			_core.MemoryCallbacks.Remove(Callback);
		}
	}
}
