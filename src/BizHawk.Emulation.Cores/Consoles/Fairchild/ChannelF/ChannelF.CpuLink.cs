using BizHawk.Emulation.Cores.Components.FairchildF8;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF
	{
		public readonly struct CpuLink(ChannelF channelF) : IF3850Link
		{
			public byte ReadMemory(ushort address)
				=> channelF.ReadBus(address);

			public void WriteMemory(ushort address, byte value)
				=> channelF.WriteBus(address, value);

			public byte ReadHardware(ushort address)
				=> channelF.ReadPort(address);

			public void WriteHardware(ushort address, byte value)
				=> channelF.WritePort(address, value);

			public void OnExecFetch(ushort address)
			{
				// TODO: implement
			}
		}
	}
}
