using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			LibGPGX.RegisterInfo[] regs = new LibGPGX.RegisterInfo[Core.gpgx_getmaxnumregs()];

			int n = Core.gpgx_getregs(regs);
			if (n > regs.Length)
				throw new InvalidOperationException("A buffer overrun has occured!");
			var ret = new Dictionary<string, RegisterValue>();
			for (int i = 0; i < n; i++)
			{
				// el hacko
				string name = Marshal.PtrToStringAnsi(regs[i].Name);
				byte size = 32;
				if (name.Contains("68K SR") || name.StartsWith("Z80"))
					size = 16;

				ret[name] = new RegisterValue((ulong)regs[i].Value, size);
			}

			return ret;
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		public IMemoryCallbackSystem MemoryCallbacks
		{
			get { return _memoryCallbacks; }
		}

		public bool CanStep(StepType type) { return false; }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		[FeatureNotImplemented]
		public int TotalExecutedCycles
		{
			get { throw new NotImplementedException(); }
		}

		private readonly MemoryCallbackSystem _memoryCallbacks = new MemoryCallbackSystem();

		private LibGPGX.mem_cb ExecCallback;
		private LibGPGX.mem_cb ReadCallback;
		private LibGPGX.mem_cb WriteCallback;
		private LibGPGX.CDCallback CDCallback;

		private void InitMemCallbacks()
		{
			ExecCallback = new LibGPGX.mem_cb(a => MemoryCallbacks.CallExecutes(a));
			ReadCallback = new LibGPGX.mem_cb(a => MemoryCallbacks.CallReads(a));
			WriteCallback = new LibGPGX.mem_cb(a => MemoryCallbacks.CallWrites(a));
			_memoryCallbacks.ActiveChanged += RefreshMemCallbacks;
		}

		private void RefreshMemCallbacks()
		{
			Core.gpgx_set_mem_callback(
				MemoryCallbacks.HasReads ? ReadCallback : null,
				MemoryCallbacks.HasWrites ? WriteCallback : null,
				MemoryCallbacks.HasExecutes ? ExecCallback : null);
		}

		private void KillMemCallbacks()
		{
			Core.gpgx_set_mem_callback(null, null, null);
		}
	}
}
