namespace Jellyfish.Virtu
{
    public class PeripheralCard : MachineComponent
    {
		public PeripheralCard() { }
        public PeripheralCard(Machine machine) : 
            base(machine)
        {
        }

        public virtual int ReadIoRegionC0C0(int address)
        {
            // read Device Select' address $C0nX; n = slot number + 8
            return ReadFloatingBus();
        }

        public virtual int ReadIoRegionC1C7(int address)
        {
            // read I/O Select' address $CsXX; s = slot number
            return ReadFloatingBus();
        }

        public virtual int ReadIoRegionC8CF(int address)
        {
            // read I/O Strobe' address $C800-$CFFF
            return ReadFloatingBus();
        }

        public virtual void WriteIoRegionC0C0(int address, int data)
        {
            // write Device Select' address $C0nX; n = slot number + 8
        }

        public virtual void WriteIoRegionC1C7(int address, int data)
        {
            // write I/O Select' address $CsXX; s = slot number
        }

        public virtual void WriteIoRegionC8CF(int address, int data)
        {
            // write I/O Strobe' address $C800-$CFFF
        }

        protected int ReadFloatingBus()
        {
            return Machine.Video.ReadFloatingBus();
        }
    }
}
