
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// The abstract class that all emulated models will inherit from
    /// * Port Access *
    /// </summary>
    public abstract partial class SpectrumBase
    {
        /// <summary>
        /// The last OUT data that was sent to the ULA
        /// </summary>
        protected byte LastULAOutByte;
        public byte LASTULAOutByte
        {
            get { return LastULAOutByte; }
            set { LastULAOutByte = value; }
        }

        public byte Last7ffd;
        public byte LastFe;
        public byte Last1ffd;

        /// <summary>
        /// Reads a byte of data from a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public abstract byte ReadPort(ushort port);

        /// <summary>
        /// Writes a byte of data to a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <param name="value"></param>
        public abstract void WritePort(ushort port, byte value);

        /// <summary>
        /// Increments the CPU totalCycles counter by the tStates value specified
        /// </summary>
        /// <param name="tStates"></param>
        //public virtual void PortContention(int tStates)
        //{
        //    CPU.TotalExecutedCycles += tStates;
        //}

        /// <summary>
        /// Simulates IO port contention based on the supplied address
        /// This method is for 48k and 128k/+2 machines only and should be overridden for other models
        /// </summary>
        /// <param name="addr"></param>
        //public abstract void ContendPort(ushort addr);
        
    }
}
