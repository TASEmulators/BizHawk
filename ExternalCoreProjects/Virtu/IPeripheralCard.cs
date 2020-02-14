namespace Jellyfish.Virtu
{
	public interface IPeripheralCard
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
}
