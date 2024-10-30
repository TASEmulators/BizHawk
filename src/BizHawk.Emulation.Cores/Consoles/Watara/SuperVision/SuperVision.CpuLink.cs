using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class SuperVision
	{
		public readonly struct CpuLink(SuperVision superVision) : IMOS6502XLink
		{
			public byte ReadMemory(ushort address)
				=> superVision.ReadMemory(address);

			public byte DummyReadMemory(ushort address)
				=> superVision.ReadMemory(address);

			public byte PeekMemory(ushort address)
				=> superVision.ReadMemory(address);

			public void WriteMemory(ushort address, byte value)
				=> superVision.WriteMemory(address, value);

			public byte ReadHardware(ushort address)
				=> superVision.ReadHardware(address);

			public void WriteHardware(ushort address, byte value)
				=> superVision.WriteHardware(address, value);

			public void OnExecFetch(ushort address)
			{
				// TODO: implement
			}
		}
	}
}
