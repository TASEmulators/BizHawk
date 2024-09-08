using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	public partial class AmstradCPC
	{
		public readonly struct CpuLink(CPCBase machine) : IZ80ALink
		{
			public byte FetchMemory(ushort address)
				=> machine.ReadMemory(address);

			public byte ReadMemory(ushort address)
				=> machine.ReadMemory(address);

			public void WriteMemory(ushort address, byte value)
				=> machine.WriteMemory(address, value);

			public byte ReadHardware(ushort address)
				=> machine.ReadPort(address);

			public void WriteHardware(ushort address, byte value)
				=> machine.WritePort(address, value);

			public byte FetchDB()
				=> machine.PushBus();

			public void OnExecFetch(ushort address)
			{
			}

			public void IRQCallback()
			{
			}

			public void NMICallback()
			{
			}

			public void IRQACKCallback()
				=> machine.GateArray.IORQA();
		}
	}
}
