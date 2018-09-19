
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// The different disk formats ZXHawk currently supports
    /// </summary>
    public enum DiskType
    {
        /// <summary>
        /// Standard CPCEMU disk format (used in the built-in +3 disk drive)
        /// </summary>
        CPC,

        /// <summary>
        /// Extended CPCEMU disk format (used in the built-in +3 disk drive)
        /// </summary>
        CPCExtended,

        /// <summary>
        /// Interchangeable Preservation Format
        /// </summary>
        IPF,

        /// <summary>
        /// Ultra Disk Image Format (v1.0)
        /// </summary>
        UDI,

        /// <summary>
        /// Ultra Disk Image Format (v1.1)
        /// </summary>
        UDIv1_1
    }
}
