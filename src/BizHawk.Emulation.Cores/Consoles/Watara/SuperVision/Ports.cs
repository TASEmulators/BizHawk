namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class SuperVision
	{
		public byte ReadHardware(ushort address)
		{
			return _asic.ReadPort(address);
		}

		public void WriteHardware(ushort address, byte value)
		{
			_asic.WritePort(address, value);
		}
	}
}
