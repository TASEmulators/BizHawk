using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This class reads a TZX file
    /// </summary>
    public class TzxReader
    {
        private readonly BinaryReader _reader;

        public static Dictionary<byte, Type> DataBlockTypes = new Dictionary<byte, Type>
        {
            {0x10, typeof(TzxStandardSpeedDataBlock)},
            {0x11, typeof(TzxTurboSpeedDataBlock)},
            {0x12, typeof(TzxPureToneDataBlock)},
            {0x13, typeof(TzxPulseSequenceDataBlock)},
            {0x14, typeof(TzxPureDataBlock)},
            {0x15, typeof(TzxDirectRecordingDataBlock)},
            {0x16, typeof(TzxC64RomTypeDataBlock)},
            {0x17, typeof(TzxC64TurboTapeDataBlock)},
            {0x18, typeof(TzxCswRecordingDataBlock)},
            {0x19, typeof(TzxGeneralizedDataBlock)},
            {0x20, typeof(TzxSilenceDataBlock)},
            {0x21, typeof(TzxGroupStartDataBlock)},
            {0x22, typeof(TzxGroupEndDataBlock)},
            {0x23, typeof(TzxJumpDataBlock)},
            {0x24, typeof(TzxLoopStartDataBlock)},
            {0x25, typeof(TzxLoopEndDataBlock)},
            {0x26, typeof(TzxCallSequenceDataBlock)},
            {0x27, typeof(TzxReturnFromSequenceDataBlock)},
            {0x28, typeof(TzxSelectDataBlock)},
            {0x2A, typeof(TzxStopTheTape48DataBlock)},
            {0x2B, typeof(TzxSetSignalLevelDataBlock)},
            {0x30, typeof(TzxTextDescriptionDataBlock)},
            {0x31, typeof(TzxMessageDataBlock)},
            {0x32, typeof(TzxArchiveInfoDataBlock)},
            {0x33, typeof(TzxHardwareInfoDataBlock)},
            {0x34, typeof(TzxEmulationInfoDataBlock)},
            {0x35, typeof(TzxCustomInfoDataBlock)},
            {0x40, typeof(TzxSnapshotBlock)},
            {0x5A, typeof(TzxGlueDataBlock)},
        };

        /// <summary>
        /// Data blocks of this TZX file
        /// </summary>
        public IList<TzxDataBlockBase> DataBlocks { get; }

        /// <summary>
        /// Major version number of the file
        /// </summary>
        public byte MajorVersion { get; private set; }

        /// <summary>
        /// Minor version number of the file
        /// </summary>
        public byte MinorVersion { get; private set; }

        /// <summary>
        /// Initializes the player from the specified reader
        /// </summary>
        /// <param name="reader"></param>
        public TzxReader(BinaryReader reader)
        {
            _reader = reader;
            DataBlocks = new List<TzxDataBlockBase>();
        }

        /// <summary>
        /// Reads in the content of the TZX file so that it can be played
        /// </summary>
        /// <returns>True, if read was successful; otherwise, false</returns>
        public virtual bool ReadContent()
        {
            var header = new TzxHeader();
            try
            {
                header.ReadFrom(_reader);
                if (!header.IsValid)
                {
                    throw new TzxException("Invalid TZX header");
                }
                MajorVersion = header.MajorVersion;
                MinorVersion = header.MinorVersion;

                while (_reader.BaseStream.Position != _reader.BaseStream.Length)
                {
                    var blockType = _reader.ReadByte();
                    Type type;
                    if (!DataBlockTypes.TryGetValue(blockType, out type))
                    {
                        throw new TzxException($"Unkonwn TZX block type: {blockType}");
                    }

                    try
                    {
                        var block = Activator.CreateInstance(type) as TzxDataBlockBase;
                        if (block.GetType() == typeof(TzxDeprecatedDataBlockBase))
                        {
                            ((TzxDeprecatedDataBlockBase)block as TzxDeprecatedDataBlockBase).ReadThrough(_reader);
                        }
                        else
                        {
                            block?.ReadFrom(_reader);
                        }
                        DataBlocks.Add(block);
                    }
                    catch (Exception ex)
                    {
                        throw new TzxException($"Cannot read TZX data block {type}.", ex);
                    }
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
