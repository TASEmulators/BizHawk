
using System.IO;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This interface describes the behavior of an object that
    /// provides tape content
    /// </summary>
    public interface ITapeContentProvider
    {
        /// <summary>
        /// Tha tape set to load the content from
        /// </summary>
        string TapeSetName { get; set; }

        /// <summary>
        /// Gets a binary reader that provides tape content
        /// </summary>
        /// <returns>BinaryReader instance to obtain the content from</returns>
        BinaryReader GetTapeContent();

        void Reset();
    }
}
