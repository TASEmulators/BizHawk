using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public abstract class VesCartBase
	{
		public abstract string BoardType { get; }
		public abstract void SyncState(Serializer ser);		

		public virtual void SyncByteArrayDomain(ChannelF sys)
		{
			sys.SyncByteArrayDomain("ROM", _rom);
		}

		public virtual byte[] ROM
		{
			get { return _rom; }
			protected set { _rom = value; }
		}
		protected byte[] _rom;		

		public virtual byte[] RAM
		{
			get { return _ram; }
			protected set { _ram = value; }
		}
		protected byte[] _ram;

		public abstract byte ReadBus(ushort addr);
		public abstract void WriteBus(ushort addr, byte value);
		public abstract byte ReadPort(ushort addr);
		public abstract void WritePort(ushort addr, byte data);

		public static VesCartBase Configure(GameInfo gi, byte[] rom)
		{
			// get board type
			string boardStr = gi.OptionPresent("board") ? gi.GetStringValue("board") : "STD";

			switch (boardStr)
			{
				// standard cart layout - default to this
				case "STD":
				default:					
					// any number of F3851 Program Storage Units (1KB ROM each) or F3856 Program Storage Unit (2KB ROM)
					// no on-pcb RAM and no extra IO
					return new mapper_STD(rom);

				case "HANG":

					return new mapper_HANG(rom);
			}
		}
	}
}
