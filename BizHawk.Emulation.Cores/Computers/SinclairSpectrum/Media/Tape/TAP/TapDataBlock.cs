
using System.IO;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This class describes a TAP Block
    /// </summary>
    public sealed class TapDataBlock :
        ITapeData,
        ITapeDataSerialization,
        ISupportsTapeBlockPlayback
    {
        private TapeDataBlockPlayer _player;

        /// <summary>
        /// Block Data
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Pause after this block (given in milliseconds)
        /// </summary>
        public ushort PauseAfter => 1000;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public void ReadFrom(BinaryReader reader)
        {
            var length = reader.ReadUInt16();
            Data = reader.ReadBytes(length);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write((ushort)Data.Length);
            writer.Write(Data);
        }

        /// <summary>
        /// The index of the currently playing byte
        /// </summary>
        /// <remarks>This proprty is made public for test purposes</remarks>
        public int ByteIndex => _player.ByteIndex;

        /// <summary>
        /// The mask of the currently playing bit in the current byte
        /// </summary>
        public byte BitMask => _player.BitMask;

        /// <summary>
        /// The current playing phase
        /// </summary>
        public PlayPhase PlayPhase => _player.PlayPhase;

        /// <summary>
        /// The tact count of the CPU when playing starts
        /// </summary>
        public long StartCycle => _player.StartCycle;

        /// <summary>
        /// Last tact queried
        /// </summary>
        public long LastCycle => _player.LastCycle;

        /// <summary>
        /// Initializes the player
        /// </summary>
        public void InitPlay(long startTact)
        {
            _player = new TapeDataBlockPlayer(Data, PauseAfter);
            _player.InitPlay(startTact);
        }

        /// <summary>
        /// Gets the EAR bit value for the specified tact
        /// </summary>
        /// <param name="currentTact">Tacts to retrieve the EAR bit</param>
        /// <returns>
        /// The EAR bit value to play back
        /// </returns>
        public bool GetEarBit(long currentTact) => _player.GetEarBit(currentTact);
    }
}
