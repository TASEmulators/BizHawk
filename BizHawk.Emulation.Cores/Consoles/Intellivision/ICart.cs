using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public interface ICart
	{
		int Parse(byte[] Rom);
		ushort? ReadCart(ushort addr);
		bool WriteCart(ushort addr, ushort value);

		void SyncState(Serializer ser);
	}
}
