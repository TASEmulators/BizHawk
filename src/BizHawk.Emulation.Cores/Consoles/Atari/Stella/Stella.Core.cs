using System;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;
using System.Runtime.CompilerServices;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella
	{
		private readonly GameInfo _game;

		internal struct CpuLink : IMOS6502XLink
		{
			private readonly Stella _atari2600;

			public byte DummyReadMemory(ushort address) => _atari2600.ReadMemory(address);

			public void OnExecFetch(ushort address) => _atari2600.ExecFetch(address);

			public byte PeekMemory(ushort address) => _atari2600.ReadMemory(address);

			public byte ReadMemory(ushort address) => _atari2600.ReadMemory(address);

			public void WriteMemory(ushort address, byte value) => _atari2600.WriteMemory(address, value);
		}

		// keeps track of tia cycles, 3 cycles per CPU cycle
		private int cyc_counter;

		internal byte BaseReadMemory(ushort addr)
		{
			return 0;
		}

		internal byte BasePeekMemory(ushort addr)
		{

			return 0;
		}

		internal void BaseWriteMemory(ushort addr, byte value)
		{
		}

		internal void BasePokeMemory(ushort addr, byte value)
		{
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte ReadMemory(ushort addr)
		{
			return 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void WriteMemory(ushort addr, byte value)
		{
		}

		internal void PokeMemory(ushort addr, byte value)
		{
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ExecFetch(ushort addr)
		{
		}

		private void RebootCore()
		{
		}

		private void HardReset()
		{
		}

		private void Cycle()
		{
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal byte ReadControls1(bool peek)
		{
			return 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal byte ReadControls2(bool peek)
		{
			return 0;
		}

		internal int ReadPot1(int pot)
		{
			return 0;
		}

		internal int ReadPot2(int pot)
		{
			return 0;
		}

		internal byte ReadConsoleSwitches(bool peek)
		{
			return 0;
		}
	}
}
