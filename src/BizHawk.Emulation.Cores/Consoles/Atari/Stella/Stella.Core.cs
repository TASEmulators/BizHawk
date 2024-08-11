using BizHawk.Emulation.Cores.Components.M6502;
using System.Runtime.CompilerServices;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella
	{
		internal struct CpuLink : IMOS6502XLink
		{
			public byte DummyReadMemory(ushort address) { return 0; }

			public void OnExecFetch(ushort address) { }

			public byte PeekMemory(ushort address) { return 0; }

			public byte ReadMemory(ushort address) { return 0; }

			public void WriteMemory(ushort address, byte value) { }
		}

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
