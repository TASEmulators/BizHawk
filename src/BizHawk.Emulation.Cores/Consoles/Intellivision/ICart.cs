using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	[CLSCompliant(false)]
	public interface ICart
	{
		int Parse(byte[] rom);
		ushort? ReadCart(ushort addr, bool peek);
		bool WriteCart(ushort addr, ushort value, bool poke);

		void SyncState(Serializer ser);

		string BoardName { get; }
	}
}
