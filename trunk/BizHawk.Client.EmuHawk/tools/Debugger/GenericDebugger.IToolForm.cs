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
		private MemoryDomainList MemoryDomains { get { return (EmulatorServices[typeof(IMemoryDomains)] as IMemoryDomains).MemoryDomains; } }

		private int? PC
		{
			get
			{
				var flags = Core.GetCpuFlagsAndRegisters();

				if (flags.ContainsKey("PC"))
				{
					return flags["PC"];
				}

				else if (flags.ContainsKey("R15"))
				{
					return flags["R15"];
				}

				return null;
			}
		}

		public void UpdateValues()
		{
			RegisterPanel.UpdateValues();
			UpdateDisassembler();
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
