using BizHawk.Common;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This class is responsible to "play" a tape file.
    /// </summary>
    public class TapeBlockSetPlayer : ISupportsTapeBlockSetPlayback
    {
        /// <summary>
        /// All data blocks that can be played back
        /// </summary>
        public List<ISupportsTapeBlockPlayback> DataBlocks { get; }

        /// <summary>
        /// Signs that the player completed playing back the file
        /// </summary>
        private bool eof;
        public bool Eof
        {
            get { return eof; }
            set { eof = value; }
        }


        /// <summary>
        /// Gets the currently playing block's index
        /// </summary>
        private int currentBlockIndex;
        public int CurrentBlockIndex
        {
            get { return currentBlockIndex; }
            set { currentBlockIndex = value; }
        }

        /// <summary>
        /// The current playable block
        /// </summary>
        private ISupportsTapeBlockPlayback currentBlock;
        public ISupportsTapeBlockPlayback CurrentBlock
        {
            get { return DataBlocks[CurrentBlockIndex]; }
            //set { currentBlock = value; }
        }


        /// <summary>
        /// The current playing phase
        /// </summary>
        private PlayPhase playPhase;
        public PlayPhase PlayPhase
        {
            get { return playPhase; }
            set { playPhase = value; }
        }


        /// <summary>
        /// The cycle count of the CPU when playing starts
        /// </summary>
        private long startCycle;
        public long StartCycle
        {
            get { return startCycle; }
            set { startCycle = value; }
        }


        public TapeBlockSetPlayer(List<ISupportsTapeBlockPlayback> dataBlocks)
        {
            DataBlocks = dataBlocks;
            Eof = dataBlocks.Count == 0;
        }

        /// <summary>
        /// Initializes the player
        /// </summary>
        public void InitPlay(long startTact)
        {
            CurrentBlockIndex = -1;
            NextBlock(startTact);
            PlayPhase = PlayPhase.None;
            StartCycle = startTact;
        }

        /// <summary>
        /// Gets the EAR bit value for the specified cycle
        /// </summary>
        /// <param name="currentCycle">Cycles to retrieve the EAR bit</param>
        /// <returns>
        /// A tuple of the EAR bit and a flag that indicates it is time to move to the next block
        /// </returns>
        public bool GetEarBit(long currentCycle)
        {
            // --- Check for EOF
            if (CurrentBlockIndex == DataBlocks.Count - 1
                && (CurrentBlock.PlayPhase == PlayPhase.Pause || CurrentBlock.PlayPhase == PlayPhase.Completed))
            {
                Eof = true;
            }
            if (CurrentBlockIndex >= DataBlocks.Count || CurrentBlock == null)
            {
                // --- After all playable block played back, there's nothing more to do
                PlayPhase = PlayPhase.Completed;
                return true;
            }
            var earbit = CurrentBlock.GetEarBit(currentCycle);
            if (CurrentBlock.PlayPhase == PlayPhase.Completed)
            {
                NextBlock(currentCycle);
            }
            return earbit;
        }

        /// <summary>
        /// Moves the current block index to the next playable block
        /// </summary>
        /// <param name="currentCycle">Cycles time to start the next block</param>
        public void NextBlock(long currentCycle)
        {
            if (CurrentBlockIndex >= DataBlocks.Count - 1)
            {
                PlayPhase = PlayPhase.Completed;
                Eof = true;
                return;
            }
            CurrentBlockIndex++;
            CurrentBlock.InitPlay(currentCycle);
        }

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("TapeBlockSetPlayer");
            ser.Sync("eof", ref eof);
            ser.Sync("currentBlockIndex", ref currentBlockIndex);
            ser.SyncEnum<PlayPhase>("playPhase", ref playPhase);
            ser.Sync("startCycle", ref startCycle);
            currentBlock.SyncState(ser);
            ser.EndSection();
        }
    }
}
