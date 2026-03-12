using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Calculators.TI83
{
	public partial class TI83
	{
		public readonly struct CpuLink(TI83 ti83) : IZ80ALink
		{
			public byte FetchMemory(ushort address)
				=> ti83.ReadMemory(address);

			public byte ReadMemory(ushort address)
				=> ti83.ReadMemory(address);

			public void WriteMemory(ushort address, byte value)
				=> ti83.WriteMemory(address, value);

			public byte ReadHardware(ushort address)
				=> ti83.ReadHardware(address);

			public void WriteHardware(ushort address, byte value)
				=> ti83.WriteHardware(address, value);

			public byte FetchDB()
				=> 0xFF;

			public void OnExecFetch(ushort address)
			{
			}

			public void IRQCallback()
				=> ti83.IRQCallback();

			public void NMICallback()
				=> ti83.NMICallback();

			public void IRQACKCallback()
			{
			}
		}
	}
}