using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
		public readonly struct CpuLink(SMS sms) : IZ80ALink
		{
			public byte FetchMemory(ushort address)
				=> sms.CDL is null
					? sms.FetchMemory(address)
					: sms.FetchMemory_CDL(address);

			public byte ReadMemory(ushort address)
				=> sms.CDL is null
					? sms.ReadMemory(address)
					: sms.ReadMemory_CDL(address);

			public void WriteMemory(ushort address, byte value)
				=> sms.WriteMemory(address, value);

			public byte ReadHardware(ushort address)
				=> sms.ReadPort(address);

			public void WriteHardware(ushort address, byte value)
				=> sms.WritePort(address, value);

			public byte FetchDB()
				=> 0xFF;

			public void OnExecFetch(ushort address)
				=> sms.OnExecMemory(address);

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
