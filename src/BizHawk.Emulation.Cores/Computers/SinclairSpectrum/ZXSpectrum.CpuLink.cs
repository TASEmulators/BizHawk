using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	public partial class ZXSpectrum
	{
		public readonly struct CpuLink(ZXSpectrum spectrum, SpectrumBase machine) : IZ80ALink
		{
			public byte FetchMemory(ushort address)
				=> machine.ReadMemory(address);

			public byte ReadMemory(ushort address)
				=> spectrum._cdl == null
					? machine.ReadMemory(address)
					: spectrum.ReadMemory_CDL(address);

			public void WriteMemory(ushort address, byte value)
				=> machine.WriteMemory(address, value);

			public byte ReadHardware(ushort address)
				=> machine.ReadPort(address);

			public void WriteHardware(ushort address, byte value)
				=> machine.WritePort(address, value);

			public byte FetchDB()
				=> machine.PushBus();

			public void OnExecFetch(ushort address)
				=> machine.CPUMon.OnExecFetch(address);

			public void IRQCallback()
			{
			}

			public void NMICallback()
			{
			}

			public void IRQACKCallback()
			{
			}
		}
	}
}
