using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64 : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			// note: the approach this code takes is highly bug-prone
			// warning: tracer magically relies on these register names!
			var ret = new Dictionary<string, RegisterValue>();
			var data = new byte[32 * 8 + 4 + 4 + 8 + 8 + 4 + 4 + 32 * 4 + 32 * 8];
			api.getRegisters(data);

			for (int i = 0; i < 32; i++)
			{
				var reg = BitConverter.ToInt64(data, i * 8);
				ret.Add(GPRnames[i] + "_lo", (int)(reg));
				ret.Add(GPRnames[i] + "_hi", (int)(reg >> 32));
			}

			var PC = BitConverter.ToUInt32(data, 32 * 8);
			ret.Add("PC", (int)PC);

			ret.Add("LL", BitConverter.ToInt32(data, 32 * 8 + 4));

			var Lo = BitConverter.ToInt64(data, 32 * 8 + 4 + 4);
			ret.Add("LO_lo", (int)Lo);
			ret.Add("LO_hi", (int)(Lo >> 32));

			var Hi = BitConverter.ToInt64(data, 32 * 8 + 4 + 4 + 8);
			ret.Add("HI_lo", (int)Hi);
			ret.Add("HI_hi", (int)(Hi >> 32));

			ret.Add("FCR0", BitConverter.ToInt32(data, 32 * 8 + 4 + 4 + 8 + 8));
			ret.Add("FCR31", BitConverter.ToInt32(data, 32 * 8 + 4 + 4 + 8 + 8 + 4));

			for (int i = 0; i < 32; i++)
			{
				var reg_cop0 = BitConverter.ToUInt32(data, 32 * 8 + 4 + 4 + 8 + 8 + 4 + 4 + i * 4);
				ret.Add("CP0 REG" + i, (int)reg_cop0);
			}

			for (int i = 0; i < 32; i++)
			{
				var reg_cop1_fgr_64 = BitConverter.ToInt64(data, 32 * 8 + 4 + 4 + 8 + 8 + 4 + 4 + 32 * 4 + i * 8);
				ret.Add("CP1 FGR REG" + i + "_lo", (int)reg_cop1_fgr_64);
				ret.Add("CP1 FGR REG" + i + "_hi", (int)(reg_cop1_fgr_64 >> 32));
			}

			return ret;
		}

		public string[] GPRnames = new string[32]
		{
			"r0",
			"at",
			"v0", "v1",
			"a0", "a1", "a2", "a3",
			"t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7",
			"s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7",
			"t8", "t9",
			"k0", "k1",
			"gp",
			"sp",
			"s8",
			"ra"
		};

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		public IMemoryCallbackSystem MemoryCallbacks
		{
			get { return _memorycallbacks; }
		}

		private readonly MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem();

		public bool CanStep(StepType type) { return false; }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		[FeatureNotImplemented]
		public int TotalExecutedCycles
		{
			get { throw new NotImplementedException(); }
		}

		private mupen64plusApi.MemoryCallback _readcb;
		private mupen64plusApi.MemoryCallback _writecb;
		private mupen64plusApi.MemoryCallback _executecb;

		private void RefreshMemoryCallbacks()
		{
			var mcs = MemoryCallbacks;

			// we RefreshMemoryCallbacks() after the triggers in case the trigger turns itself off at that point
			if (mcs.HasReads)
			{
				_readcb = delegate(uint addr)
				{
					api.OnBreakpoint(new mupen64plusApi.BreakParams
					{
						_type = mupen64plusApi.BreakType.Read,
						_addr = addr,
						_mcs = mcs
					});
				};
			}
			else
			{
				_readcb = null;
			}

			if (mcs.HasWrites)
			{
				_writecb = delegate(uint addr)
				{
					api.OnBreakpoint(new mupen64plusApi.BreakParams
					{
						_type = mupen64plusApi.BreakType.Write,
						_addr = addr,
						_mcs = mcs
					});
				};
			}
			else
			{
				_writecb = null;
			}

			if (mcs.HasExecutes)
			{
				_executecb = delegate(uint addr)
				{
					api.OnBreakpoint(new mupen64plusApi.BreakParams
					{
						_type = mupen64plusApi.BreakType.Execute,
						_addr = addr,
						_mcs = mcs
					});
				};
			}
			else
			{
				_executecb = null;
			}

			api.setReadCallback(_readcb);
			api.setWriteCallback(_writecb);
			api.setExecuteCallback(_executecb);
		}
	}
}
