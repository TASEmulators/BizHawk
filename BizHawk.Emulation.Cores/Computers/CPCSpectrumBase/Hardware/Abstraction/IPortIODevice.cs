
namespace BizHawk.Emulation.Cores.Computers.CPCSpectrumBase
{
	/// <summary>
	/// Represents a device that utilizes port IN & OUT
	/// </summary>
	public interface IPortIODevice
	{
		/// <summary>
		/// Device responds to an IN instruction
		/// </summary>
		bool ReadPort(ushort port, ref int result);

		/// <summary>
		/// Device responds to an OUT instruction
		/// </summary>
		bool WritePort(ushort port, int result);
	}
}
