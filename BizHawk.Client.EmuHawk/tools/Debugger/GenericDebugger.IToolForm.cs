using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericDebugger : IToolForm
	{
		[RequiredService]
		private IDebuggable Core { get; set; }
		[OptionalService]
		private IDisassemblable Disassembler { get; set; }
		[OptionalService]
		private IMemoryDomains MemoryDomainSource { get; set; }
		private MemoryDomainList MemoryDomains { get { return MemoryDomainSource.MemoryDomains; } }

		private int PC
		{
			get { return Core.GetCpuFlagsAndRegisters()[Disassembler.PCRegisterName]; }
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
