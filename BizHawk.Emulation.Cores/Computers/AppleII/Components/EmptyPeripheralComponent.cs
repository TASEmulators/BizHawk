using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	/// <summary>
	/// Represents an unused peripheral card
	/// </summary>
	public class EmptyPeripheralCard : IPeripheralCard
	{
		// TODO: make readonly once json isn't used
		private Video _video;

		// TODO: remove when json isn't used
		public EmptyPeripheralCard() { }

		public EmptyPeripheralCard(Video video)
		{
			_video = video;
		}

		public int ReadIoRegionC0C0(int address) => _video.ReadFloatingBus();
		public int ReadIoRegionC1C7(int address) => _video.ReadFloatingBus();
		public int ReadIoRegionC8CF(int address) => _video.ReadFloatingBus();

		public void WriteIoRegionC0C0(int address, int data) { }
		public void WriteIoRegionC1C7(int address, int data) { }
		public void WriteIoRegionC8CF(int address, int data) { }
	}
}
