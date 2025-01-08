using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision
	{
		public readonly struct CpuLink(ColecoVision coleco) : IZ80ALink
		{
			public byte FetchMemory(ushort address)
				=> coleco.ReadMemory(address);

			public byte ReadMemory(ushort address)
				=> coleco.ReadMemory(address);

			public void WriteMemory(ushort address, byte value)
				=> coleco.WriteMemory(address, value);

			public byte ReadHardware(ushort address)
				=> coleco.ReadPort(address);

			public void WriteHardware(ushort address, byte value)
				=> coleco.WritePort(address, value);

			public byte FetchDB()
				=> 0xFF;

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
			{
			}
		}
	}
}
