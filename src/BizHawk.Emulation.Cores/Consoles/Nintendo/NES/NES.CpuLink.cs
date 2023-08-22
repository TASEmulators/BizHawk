using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES
	{
		public readonly struct CpuLink : IMOS6502XLink
		{
			private readonly NES _nes;

			public CpuLink(NES nes)
			{
				_nes = nes;
			}

			public readonly byte DummyReadMemory(ushort address) => _nes.ReadMemory(address);

			public readonly void OnExecFetch(ushort address) => _nes.ExecFetch(address);

			public readonly byte PeekMemory(ushort address) => _nes.CDL == null ? _nes.PeekMemory(address) : _nes.FetchMemory_CDL(address);

			public readonly byte ReadMemory(ushort address) => _nes.CDL == null ? _nes.ReadMemory(address) : _nes.ReadMemory_CDL(address);

			public readonly void WriteMemory(ushort address, byte value) => _nes.WriteMemory(address, value);
		}
	}
}
