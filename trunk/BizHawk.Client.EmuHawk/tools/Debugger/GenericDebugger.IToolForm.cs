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
		[OptionalService]
		private IMemoryCallbackSystem MCS { get; set; }

		private MemoryDomainList MemoryDomains { get { return MemoryDomainSource.MemoryDomains; } }

		private int PC
		{
			// TODO: is this okay for N64?
			get { return (int)Core.GetCpuFlagsAndRegisters()[Disassembler.PCRegisterName].Value; }
		}

		#region Implementation checking

		private bool CanDisassemble
		{
			get
			{
				if (Disassembler == null)
				{
					return false;
				}

				try
				{
					var pc = PC;
					return true;
				}
				catch (NotImplementedException)
				{
					return false;
				}

			}
		}

		#endregion

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
			// TODO
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
