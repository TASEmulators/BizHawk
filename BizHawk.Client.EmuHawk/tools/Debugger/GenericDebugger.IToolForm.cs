using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericDebugger : IToolForm
	{
		public IDictionary<Type, object> EmulatorServices { private get; set; }
		private IDebuggable Core { get { return (IDebuggable)EmulatorServices[typeof(IDebuggable)]; } }
		private IDisassemblable Disassembler { get { return (IDisassemblable)EmulatorServices[typeof(IDisassemblable)]; } }

		public void UpdateValues()
		{
			RegisterPanel.UpdateValues();
		}

		public void FastUpdate()
		{
			// TODO
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}
		}

		public bool AskSaveChanges()
		{
			// TODO
			return true;
		}

		public bool UpdateBefore
		{
			get { return false; }
		}
	}
}
