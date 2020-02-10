namespace Jellyfish.Virtu
{
	internal interface IPeripheralCard
	{
		// read Device Select' address $C0nX; n = slot number + 8
		int ReadIoRegionC0C0(int address);
		
		// read I/O Select' address $CsXX; s = slot number
		int ReadIoRegionC1C7(int address);
		
		// read I/O Strobe' address $C800-$CFFF
		int ReadIoRegionC8CF(int address);

		// write Device Select' address $C0nX; n = slot number + 8
		void WriteIoRegionC0C0(int address, int data);

		// write I/O Select' address $CsXX; s = slot number
		void WriteIoRegionC1C7(int address, int data);
		
		// write I/O Strobe' address $C800-$CFFF
		void WriteIoRegionC8CF(int address, int data);
	}

	public class EmptyPeripheralCard : IPeripheralCard
	{
		// TODO: can't be read only because of serialization?
		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		private Video _video;

		// ReSharper disable once UnusedMember.Global
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
