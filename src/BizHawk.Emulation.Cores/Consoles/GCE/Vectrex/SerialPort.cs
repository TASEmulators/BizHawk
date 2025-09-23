using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public class SerialPort
	{
		[CLSCompliant(false)]
		public VectrexHawk Core { get; set; }

		public byte ReadReg(int addr)
		{
			return 0xFF;
		}

		public void WriteReg(int addr, byte value)
		{
			//TODO
		}

		public void serial_transfer_tick()
		{
			//TODO
		}

		public void Reset()
		{
			//TODO
		}

		public void SyncState(Serializer ser)
		{
			//TODO
		}
	}
}
