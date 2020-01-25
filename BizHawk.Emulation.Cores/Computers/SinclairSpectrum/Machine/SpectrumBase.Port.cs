
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
            get => LastULAOutByte;
            set => LastULAOutByte = value;
        }

        public byte Last7ffd;
        public byte LastFe;
        public byte Last1ffd;

        /// <summary>
        /// Reads a byte of data from a specified port address
        /// </summary>
        public abstract byte ReadPort(ushort port);

        /// <summary>
        /// Writes a byte of data to a specified port address
        /// </summary>
        public abstract void WritePort(ushort port, byte value);
    }
}
