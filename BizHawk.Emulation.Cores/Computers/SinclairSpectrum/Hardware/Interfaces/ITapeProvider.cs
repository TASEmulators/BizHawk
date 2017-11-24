using System.IO;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This interface describes the behavior of an object that
    /// provides TZX tape content
    /// </summary>
    public interface ITapeProvider
    {
        /// <summary>
        /// Tha tape set to load the content from
        /// </summary>
        string TapeSetName { get; set; }

        /// <summary>
        /// Gets a binary reader that provider TZX content
        /// </summary>
        /// <returns>BinaryReader instance to obtain the content from</returns>
        BinaryReader GetTapeContent();

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
        /// Appends the TZX block to the tape file
        /// </summary>
        /// <param name="block"></param>
        void SaveTapeBlock(ITapeDataSerialization block);

        /// <summary>
        /// The tape provider can finalize the tape when all 
        /// TZX blocks are written.
        /// </summary>
        void FinalizeTapeFile();
    }
}
