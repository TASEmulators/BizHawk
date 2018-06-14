
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Defines an object that can load a floppy disk image
    /// </summary>
    public interface IFDDHost
    {
        /// <summary>
        /// The currently inserted diskimage
        /// </summary>
        FloppyDisk Disk { get; set; }

        /// <summary>
        /// Parses a new disk image and loads it into this floppy drive
        /// </summary>
        /// <param name="diskData"></param>
        void FDD_LoadDisk(byte[] diskData);

        /// <summary>
        /// Ejects the current disk
        /// </summary>
        void FDD_EjectDisk();

        /// <summary>
        /// Signs whether the current active drive has a disk inserted
        /// </summary>   
        bool FDD_IsDiskLoaded { get; }
    }
}
