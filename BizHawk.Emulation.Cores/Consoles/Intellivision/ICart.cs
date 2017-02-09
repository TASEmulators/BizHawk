using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public interface ICart
	{
		int Parse(byte[] Rom);
		ushort? ReadCart(ushort addr, bool peek);
		bool WriteCart(ushort addr, ushort value, bool poke);

		void SyncState(Serializer ser);
	}
}
