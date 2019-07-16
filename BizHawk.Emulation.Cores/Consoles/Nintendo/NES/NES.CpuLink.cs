using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
    public partial class NES
    {
		public struct CpuLink : IMOS6502XLink
		{
			private readonly NES _nes;

			public CpuLink(NES nes)
			{
				_nes = nes;
			}

			public byte DummyReadMemory(ushort address) => _nes.ReadMemory(address);

			public void OnExecFetch(ushort address) => _nes.ExecFetch(address);

			public byte PeekMemory(ushort address) => _nes.CDL == null ? _nes.PeekMemory(address) : _nes.FetchMemory_CDL(address);

			public byte ReadMemory(ushort address) => _nes.CDL == null ? _nes.ReadMemory(address) : _nes.ReadMemory_CDL(address);

			public void WriteMemory(ushort address, byte value) => _nes.WriteMemory(address, value);
		}
	}
}
