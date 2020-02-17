using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public class SerialPort
	{
		public VectrexHawk Core { get; set; }

		public byte ReadReg(int addr)
		{
			return 0xFF;
		}

		public void WriteReg(int addr, byte value)
		{

		}

		public void serial_transfer_tick()
		{

		}

		public void Reset()
		{

		}

		public void SyncState(Serializer ser)
		{

		}
	}
}
