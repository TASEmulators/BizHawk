using BizHawk.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This class recognizes .TZX and .TAP files, and playes back
    /// the content accordingly.
    /// </summary>
    public class TapeFilePlayer : ISupportsTapeBlockPlayback
    {
        private readonly BinaryReader _reader;
        private TapeBlockSetPlayer _player;
        private int _numberOfDataBlocks;

        /// <summary>
        /// Data blocks to play back
        /// </summary>
        public List<ISupportsTapeBlockPlayback> DataBlocks { get; private set; }

        /// <summary>
        /// Signs that the player completed playing back the file
        /// </summary>
        public bool Eof => _player.Eof;

        /// <summary>
        /// Initializes the player from the specified reader
        /// </summary>
        /// <param name="reader">BinaryReader instance to get tape file data from</param>
        public TapeFilePlayer(BinaryReader reader)
        {
            _reader = reader;
        }

        /// <summary>
        /// Reads in the content of the tape file so that it can be played
        /// </summary>
        /// <returns>True, if read was successful; otherwise, false</returns>
        public bool ReadContent()
        {
            // --- First try TzxReader
            var tzxReader = new TzxReader(_reader);
            var readerFound = false;
            try
            {
                readerFound = tzxReader.ReadContent();
            }
            catch (Exception)
            {
                // --- This exception is intentionally ingnored
            }

            if (readerFound)
            {
                // --- This is a .TZX format
                DataBlocks = tzxReader.DataBlocks.Where(b => b is ISupportsTapeBlockPlayback)
                    .Cast<ISupportsTapeBlockPlayback>()
                    .ToList();
                _player = new TapeBlockSetPlayer(DataBlocks);
                return true;
            }

            // --- Let's assume .TAP tap format
            _reader.BaseStream.Seek(0, SeekOrigin.Begin);
            var tapReader = new TapReader(_reader);
            readerFound = tapReader.ReadContent();
            DataBlocks = tapReader.DataBlocks.Cast<ISupportsTapeBlockPlayback>()
                .ToList();
            _player = new TapeBlockSetPlayer(DataBlocks);
            return readerFound;
        }

        /// <summary>
        /// Gets the currently playing block's index
        /// </summary>
        public int CurrentBlockIndex => _player.CurrentBlockIndex;

        /// <summary>
        /// The current playable block
        /// </summary>
        public ISupportsTapeBlockPlayback CurrentBlock => _player.CurrentBlock;

        /// <summary>
        /// The current playing phase
        /// </summary>
        public PlayPhase PlayPhase => _player.PlayPhase;

        /// <summary>
        /// The tact count of the CPU when playing starts
        /// </summary>
        public long StartCycle => _player.StartCycle;

        /// <summary>
        /// Initializes the player
        /// </summary>
        public void InitPlay(long startCycle)
        {
            _player.InitPlay(startCycle);
        }

        /// <summary>
        /// Gets the EAR bit value for the specified cycle
        /// </summary>
        /// <param name="currentCycle">Tacts to retrieve the EAR bit</param>
        /// <returns>
        /// A tuple of the EAR bit and a flag that indicates it is time to move to the next block
        /// </returns>
        public bool GetEarBit(long currentCycle) => _player.GetEarBit(currentCycle);

        /// <summary>
        /// Moves the current block index to the next playable block
        /// </summary>
        /// <param name="currentTact">Tacts time to start the next block</param>
        public void NextBlock(long currentCycle) => _player.NextBlock(currentCycle);

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("TapeFilePlayer");
            ReadContent();
            ser.Sync("_numberOfDataBlocks", ref _numberOfDataBlocks);
            _player.SyncState(ser);        
            ser.EndSection();
        }
    }
}
