
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This class reads a TAP file
    /// </summary>
    public class TapReader
    {
        private readonly BinaryReader _reader;

        /// <summary>
        /// Data blocks of this TZX file
        /// </summary>
        public IList<TapDataBlock> DataBlocks { get; }

        /// <summary>
        /// Initializes the player from the specified reader
        /// </summary>
        /// <param name="reader"></param>
        public TapReader(BinaryReader reader)
        {
            _reader = reader;
            DataBlocks = new List<TapDataBlock>();
        }

        /// <summary>
        /// Reads in the content of the TZX file so that it can be played
        /// </summary>
        /// <returns>True, if read was successful; otherwise, false</returns>
        public virtual bool ReadContent()
        {
            try
            {
                while (_reader.BaseStream.Position != _reader.BaseStream.Length)
                {
                    var tapBlock = new TapDataBlock();
                    tapBlock.ReadFrom(_reader);
                    DataBlocks.Add(tapBlock);
                }
                return true;
            }
            catch
            {
                // --- This exception is intentionally ignored
                return false;
            }
        }
    }
}
