using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
    public partial class VectrexHawk
	{
		public struct CpuLink : IMOS6502XLink
		{
			private readonly VectrexHawk _Vectrex;

			public CpuLink(VectrexHawk Vectrex)
			{
				_Vectrex = Vectrex;
			}

			public byte DummyReadMemory(ushort address) => _Vectrex.ReadMemory(address);

			public void OnExecFetch(ushort address) => _Vectrex.ExecFetch(address);

			public byte PeekMemory(ushort address) => _Vectrex.CDL == null ? _Vectrex.PeekMemory(address) : _Vectrex.FetchMemory_CDL(address);

			public byte ReadMemory(ushort address) => _Vectrex.CDL == null ? _Vectrex.ReadMemory(address) : _Vectrex.ReadMemory_CDL(address);

			public void WriteMemory(ushort address, byte value) => _Vectrex.WriteMemory(address, value);
		}
	}
}
