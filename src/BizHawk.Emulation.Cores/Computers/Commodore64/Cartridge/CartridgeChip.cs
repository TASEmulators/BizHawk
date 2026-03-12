namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge;

public class CartridgeChip
{
	public int Address;
	public int Bank;
	public int[] Data;

	/// <summary>
	/// This exists to bridge the gap between the old int[] representation
	/// and the new byte[] representation of <see cref="Data"/>.
	/// </summary>
	public byte[] ConvertDataToBytes()
	{
		var result = new byte[Data.Length];
		for (var i = 0; i < Data.Length; i++)
			result[i] = unchecked((byte) Data[i]);
		return result;
	}
}