namespace BizHawk.Emulation.Consoles.Intellivision
{
	public interface ICart
	{
		int Parse(byte[] Rom);
		ushort? ReadCart(ushort addr);
		bool WriteCart(ushort addr, ushort value);
	}
}
