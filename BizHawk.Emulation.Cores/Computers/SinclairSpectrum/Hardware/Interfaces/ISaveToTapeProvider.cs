
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This interface describes the behavior of an object that
    /// provides tape content
    /// </summary>
    public interface ISaveToTapeProvider
    {
        /// <summary>
        /// Creates a tape file with the specified name
        /// </summary>
        /// <returns></returns>
        void CreateTapeFile();

        /// <summary>
        /// This method sets the name of the file according to the 
        /// Spectrum SAVE HEADER information
        /// </summary>
        /// <param name="name"></param>
        void SetName(string name);

        /// <summary>
        /// Appends the tape block to the tape file
        /// </summary>
        /// <param name="block"></param>
        void SaveTapeBlock(ITapeDataSerialization block);

        /// <summary>
        /// The tape provider can finalize the tape when all 
        /// tape blocks are written.
        /// </summary>
        void FinalizeTapeFile();
    }
}
