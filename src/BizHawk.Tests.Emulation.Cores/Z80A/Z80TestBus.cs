using System.Collections.Generic;

namespace BizHawk.Tests.Emulation.Cores.Z80ATests
{
	/// <summary>Shared 64K address space + ordered write logs for one CPU instance under test.</summary>
	public sealed class TestBus
	{
		public readonly byte[] Mem = new byte[0x10000];
		public readonly List<int> MemWrites = new();  // packed (addr << 8) | value, in order
		public readonly List<int> PortWrites = new(); // packed (addr << 8) | value, in order
		public bool Record = true;                    // benchmark sets false to avoid log growth
	}

	/// <summary>
	/// Struct link (monomorphised — the same shape ZXHawk uses). Memory reads hit the shared array;
	/// port reads and the interrupt data-bus are deterministic so both cores see identical input.
	/// Implements the shared <see cref="BizHawk.Emulation.Cores.Components.Z80A.IZ80ALink"/>, which
	/// both the reference Z80A and the forked Z80AOpt are generic over.
	/// </summary>
	public readonly struct TestLink(TestBus bus)
		: BizHawk.Emulation.Cores.Components.Z80A.IZ80ALink
	{
		public byte FetchMemory(ushort address) => bus.Mem[address];
		public byte ReadMemory(ushort address) => bus.Mem[address];
		public void WriteMemory(ushort address, byte value)
		{
			bus.Mem[address] = value;
			if (bus.Record) bus.MemWrites.Add((address << 8) | value);
		}
		public byte ReadHardware(ushort address) => (byte)(address & 0xFF); // deterministic
		public void WriteHardware(ushort address, byte value)
		{
			if (bus.Record) bus.PortWrites.Add((address << 8) | value);
		}
		public byte FetchDB() => 0xFF; // deterministic interrupt data bus
		public void OnExecFetch(ushort address) { }
		public void IRQCallback() { }
		public void NMICallback() { }
		public void IRQACKCallback() { }
	}
}
