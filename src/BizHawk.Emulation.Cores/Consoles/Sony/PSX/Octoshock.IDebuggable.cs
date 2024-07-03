using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	public unsafe partial class Octoshock : IDebuggable
	{
		// TODO: don't cast to int, and are any of these not 32 bit?
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			Dictionary<string, RegisterValue> ret = new Dictionary<string, RegisterValue>();
			var regs = new OctoshockDll.ShockRegisters_CPU();

			OctoshockDll.shock_GetRegisters_CPU(psx, ref regs);

			//ret[ "r1"] = (int)regs.GPR[ 1]; ret[ "r2"] = (int)regs.GPR[ 2]; ret[ "r3"] = (int)regs.GPR[ 3];
			//ret[ "r4"] = (int)regs.GPR[ 4]; ret[ "r5"] = (int)regs.GPR[ 5]; ret[ "r6"] = (int)regs.GPR[ 6]; ret[ "r7"] = (int)regs.GPR[ 7];
			//ret[ "r8"] = (int)regs.GPR[ 8]; ret[ "r9"] = (int)regs.GPR[ 9]; ret["r10"] = (int)regs.GPR[10]; ret["r11"] = (int)regs.GPR[11];
			//ret["r12"] = (int)regs.GPR[12]; ret["r13"] = (int)regs.GPR[13]; ret["r14"] = (int)regs.GPR[14]; ret["r15"] = (int)regs.GPR[15];
			//ret["r16"] = (int)regs.GPR[16]; ret["r17"] = (int)regs.GPR[17]; ret["r18"] = (int)regs.GPR[18]; ret["r19"] = (int)regs.GPR[19];
			//ret["r20"] = (int)regs.GPR[20]; ret["r21"] = (int)regs.GPR[21]; ret["r22"] = (int)regs.GPR[22]; ret["r23"] = (int)regs.GPR[23];
			//ret["r24"] = (int)regs.GPR[24]; ret["r25"] = (int)regs.GPR[25]; ret["r26"] = (int)regs.GPR[26]; ret["r27"] = (int)regs.GPR[27];
			//ret["r28"] = (int)regs.GPR[28]; ret["r29"] = (int)regs.GPR[29]; ret["r30"] = (int)regs.GPR[30]; ret["r31"] = (int)regs.GPR[31];

			ret[ "at"] = (int)regs.GPR[ 1];
			ret[ "v0"] = (int)regs.GPR[ 2]; ret[ "v1"] = (int)regs.GPR[ 3];
			ret[ "a0"] = (int)regs.GPR[ 4]; ret[ "a1"] = (int)regs.GPR[ 5]; ret[ "a2"] = (int)regs.GPR[ 6]; ret[ "a3"] = (int)regs.GPR[ 7];
			ret[ "t0"] = (int)regs.GPR[ 8]; ret[ "t1"] = (int)regs.GPR[ 9]; ret[ "t2"] = (int)regs.GPR[10]; ret[ "t3"] = (int)regs.GPR[11];
			ret[ "t4"] = (int)regs.GPR[12]; ret[ "t5"] = (int)regs.GPR[13]; ret[ "t6"] = (int)regs.GPR[14]; ret[ "t7"] = (int)regs.GPR[15];
			ret[ "s0"] = (int)regs.GPR[16]; ret[ "s1"] = (int)regs.GPR[17]; ret[ "s2"] = (int)regs.GPR[18]; ret[ "s3"] = (int)regs.GPR[19];
			ret[ "s4"] = (int)regs.GPR[20]; ret[ "s5"] = (int)regs.GPR[21]; ret[ "s6"] = (int)regs.GPR[22]; ret[ "s7"] = (int)regs.GPR[23];
			ret[ "t8"] = (int)regs.GPR[24]; ret[ "t9"] = (int)regs.GPR[25];
			ret[ "k0"] = (int)regs.GPR[26]; ret[ "k1"] = (int)regs.GPR[27];
			ret[ "gp"] = (int)regs.GPR[28];
			ret[ "sp"] = (int)regs.GPR[29];
			ret[ "fp"] = (int)regs.GPR[30];
			ret[ "ra"] = (int)regs.GPR[31];

			ret[   "pc"] = (int)regs.PC;
			ret[   "lo"] = (int)regs.LO;
			ret[   "hi"] = (int)regs.HI;
			ret[   "sr"] = (int)regs.SR;
			ret["cause"] = (int)regs.CAUSE;
			ret[  "epc"] = (int)regs.EPC;

			return ret;
		}

		private static readonly Dictionary<string, int> CpuRegisterIndices = new Dictionary<string, int>() {
			{ "r1",   1 }, { "r2",   2 }, { "r3",   3 }, { "r4",   4 }, { "r5",   5 }, { "r6",   6 }, { "r7",   7 },
			{ "r8",   8 }, { "r9",   9 }, { "r10", 10 }, { "r11", 11 }, { "r12", 12 }, { "r13", 13 }, { "r14", 14 }, { "r15", 15 },
			{ "r16", 16 }, { "r17", 17 }, { "r18", 18 }, { "r19", 19 }, { "r20", 20 }, { "r21", 21 }, { "r22", 22 }, { "r23", 23 },
			{ "r24", 24 }, { "r25", 25 }, { "r26", 26 }, { "r27", 27 }, { "r28", 28 }, { "r29", 29 }, { "r30", 30 }, { "r31", 31 },

			{ "at",   1 }, { "v0",   2 }, { "v1",   3 },
			{ "a0",   4 }, { "a1",   5 }, { "a2",   6 }, { "a3",   7 },
			{ "t0",   8 }, { "t1",   9 }, { "t2",  10 }, { "t3",  11 }, { "t4",  12 }, { "t5",  13 }, { "t6",  14 }, { "t7",  15 },
			{ "s0",  16 }, { "s1",  17 }, { "s2",  18 }, { "s3",  19 }, { "s4",  20 }, { "s5",  21 }, { "s6",  22 }, { "s7",  23 },
			{ "t8",  24 }, { "t9",  25 },
			{ "k0",  26 }, { "k1",  27 },
			{ "gp",  28 }, { "sp",  29 }, { "fp",  30 }, { "ra",  31 },

			{   "pc", 32 },
			//33 - PC_NEXT
			//34 - IN_BD_SLOT
			{   "lo", 35 },
			{   "hi", 36 },
			{   "sr", 37 },
			{"cause", 38 },
			{  "epc", 39 },
		};

		public void SetCpuRegister(string register, int value)
		{
			int index = CpuRegisterIndices[register];
			OctoshockDll.shock_SetRegister_CPU(psx, index, (uint)value);
		}

		private readonly MemoryCallbackSystem _memoryCallbacks = new MemoryCallbackSystem(new[] { "System Bus" }); // Note: there is no system bus memory domain, but there's also no hard rule that the memory callback system domains have to correspond to actual domains in MemoryDomains, that could be good, or bad, but something to be careful about
		public IMemoryCallbackSystem MemoryCallbacks => _memoryCallbacks;

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		[FeatureNotImplemented]
		public long TotalExecutedCycles => throw new NotImplementedException();

		private OctoshockDll.ShockCallback_Mem mem_cb;

		private void ShockMemCallback(uint address, OctoshockDll.eShockMemCb type, uint size, uint value)
		{
			MemoryCallbackFlags flags = 0;
			switch (type)
			{
				case OctoshockDll.eShockMemCb.Read:
					flags |= MemoryCallbackFlags.AccessRead;
					break;
				case OctoshockDll.eShockMemCb.Write:
					flags |= MemoryCallbackFlags.AccessWrite;
					break;
				case OctoshockDll.eShockMemCb.Execute:
					flags |= MemoryCallbackFlags.AccessExecute;
					break;
			}

			MemoryCallbacks.CallMemoryCallbacks(address, value, (uint)flags, "System Bus");
		}

		private void InitMemCallbacks()
		{
			mem_cb = new OctoshockDll.ShockCallback_Mem(ShockMemCallback);
			_memoryCallbacks.ActiveChanged += RefreshMemCallbacks;
		}

		private void RefreshMemCallbacks()
		{
			OctoshockDll.eShockMemCb mask = OctoshockDll.eShockMemCb.None;
			if (MemoryCallbacks.HasReads) mask |= OctoshockDll.eShockMemCb.Read;
			if (MemoryCallbacks.HasWrites) mask |= OctoshockDll.eShockMemCb.Write;
			if (MemoryCallbacks.HasExecutes) mask |= OctoshockDll.eShockMemCb.Execute;
			OctoshockDll.shock_SetMemCb(psx, mem_cb, mask);
		}

		private void SetMemoryDomains()
		{
			var mmd = new List<MemoryDomain>();

			OctoshockDll.shock_GetMemData(psx, out var ptr, out var size, OctoshockDll.eMemType.MainRAM);
			mmd.Add(new MemoryDomainIntPtr("MainRAM", MemoryDomain.Endian.Little, ptr, size, true, 4));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.GPURAM);
			mmd.Add(new MemoryDomainIntPtr("GPURAM", MemoryDomain.Endian.Little, ptr, size, true, 4));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.SPURAM);
			mmd.Add(new MemoryDomainIntPtr("SPURAM", MemoryDomain.Endian.Little, ptr, size, true, 4));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.BiosROM);
			mmd.Add(new MemoryDomainIntPtr("BiosROM", MemoryDomain.Endian.Little, ptr, size, true, 4));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.PIOMem);
			mmd.Add(new MemoryDomainIntPtr("PIOMem", MemoryDomain.Endian.Little, ptr, size, true, 4));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.DCache);
			mmd.Add(new MemoryDomainIntPtr("DCache", MemoryDomain.Endian.Little, ptr, size, true, 4));

			mmd.Add(new MemoryDomainDelegate("System Bus", 0x1_0000_0000, MemoryDomain.Endian.Little,
				(a) => { OctoshockDll.shock_PeekMemory(psx, (uint)a, out byte v); return v; },
				(a, v) => { OctoshockDll.shock_PokeMemory(psx, (uint)a, v); },
				4));

			MemoryDomains = new MemoryDomainList(mmd);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}

		private IMemoryDomains MemoryDomains;
	}
}
