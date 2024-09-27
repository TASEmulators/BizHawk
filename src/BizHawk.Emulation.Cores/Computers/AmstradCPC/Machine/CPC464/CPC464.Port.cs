
namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CPC464
	/// * Port *
	/// </summary>
	public partial class CPC464 : CPCBase
	{
		/// <summary>
		/// Reads a byte of data from a specified port address
		/// </summary>
		public override byte ReadPort(ushort port)
		{
			int result = 0xff;

			if (DecodeINPort(port) == PortDevice.GateArray)
			{
				GateArray.ReadPort(port, ref result);
			}
			else if (DecodeINPort(port) == PortDevice.CRCT)
			{
				CRTC.ReadPort(port, ref result);
			}
			else if (DecodeINPort(port) == PortDevice.ROMSelect)
			{

			}
			else if (DecodeINPort(port) == PortDevice.Printer)
			{

			}
			else if (DecodeINPort(port) == PortDevice.PPI)
			{
				PPI.ReadPort(port, ref result);
			}
			else if (DecodeINPort(port) == PortDevice.Expansion)
			{

			}

			return (byte)result;
		}

		/// <summary>
		/// Writes a byte of data to a specified port address
		/// Because of the port decoding, multiple devices can be written to
		/// </summary>
		public override void WritePort(ushort port, byte value)
		{
			var devs = DecodeOUTPort(port);

			foreach (var d in devs)
			{
				if (d == PortDevice.GateArray)
				{
					GateArray.WritePort(port, value);
				}
				else if (d == PortDevice.PAL)
				{
					// not present in the unexpanded CPC464
				}
				else if (d == PortDevice.CRCT)
				{
					CRTC.WritePort(port, value);
				}
				else if (d == PortDevice.ROMSelect)
				{

				}
				else if (d == PortDevice.Printer)
				{

				}
				else if (d == PortDevice.PPI)
				{
					PPI.WritePort(port, value);
				}
				else if (d == PortDevice.Expansion)
				{

				}
			}

			return;
		}
	}
}
