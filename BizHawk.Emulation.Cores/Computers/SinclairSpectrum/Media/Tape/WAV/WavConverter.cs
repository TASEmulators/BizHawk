using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Reponsible for WAV format conversion
    /// Based heavily on code from zxmak2: https://archive.codeplex.com/?p=zxmak2
    /// </summary>
    public class WavConverter : MediaConverter
    {
        /// <summary>
        /// The type of serializer
        /// </summary>
        private MediaConverterType _formatType = MediaConverterType.WAV;
        public override MediaConverterType FormatType
        {
            get
            {
                return _formatType;
            }
        }

        /// <summary>
        /// Signs whether this class can be used to read the data format
        /// </summary>
        public override bool IsReader { get { return true; } }

        /// <summary>
        /// Signs whether this class can be used to write the data format
        /// </summary>
        public override bool IsWriter { get { return false; } }

        /// <summary>
        /// Position counter
        /// </summary>
        private int _position = 0;

        #region Construction

        private DatacorderDevice _datacorder;

        public WavConverter(DatacorderDevice _tapeDevice)
        {
            _datacorder = _tapeDevice;
        }

        #endregion

        /// <summary>
        /// Returns TRUE if pzx header is detected
        /// </summary>
        /// <param name="data"></param>
        public override bool CheckType(byte[] data)
        {
            // WAV Header

            // check whether this is a valid wav format file by looking at the identifier in the header
            string ident = Encoding.ASCII.GetString(data, 8, 4);

            if (ident.ToUpper() != "WAVE")
            {
                // this is not a valid WAV format file
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// DeSerialization method
        /// </summary>
        /// <param name="data"></param>
        public override void Read(byte[] data)
        {
            // clear existing tape blocks
            _datacorder.DataBlocks.Clear();

            // check whether this is a valid pzx format file by looking at the identifier in the header block
            string ident = Encoding.ASCII.GetString(data, 8, 4);

            if (ident.ToUpper() != "WAVE")
            {
                // this is not a valid TZX format file
                throw new Exception(this.GetType().ToString() +
                    "This is not a valid WAV format file");
            }

            _position = 0;

            MemoryStream stream = new MemoryStream();
            stream.Write(data, 0, data.Length);
            stream.Position = 0;

            WavStreamReader reader = new WavStreamReader(stream);

            int rate = (69888 * 50) / reader.Header.sampleRate;
            int smpCounter = 0;
            int state = reader.ReadNext();

            // create the single tape block
            TapeDataBlock t = new TapeDataBlock();
            t.BlockDescription = BlockType.WAV_Recording;
            t.BlockID = 0;
            t.DataPeriods = new List<int>();

            for (int i = 0; i < reader.Count; i++)
            {
                int sample = reader.ReadNext();
                smpCounter++;
                if ((state < 0 && sample < 0) || (state >= 0 && sample >= 0))
                    continue;
                t.DataPeriods.Add(smpCounter * rate);
                smpCounter = 0;
                state = sample;
            }

            // add closing period
            t.DataPeriods.Add((69888 * 50) / 10);

            // add to datacorder
            _datacorder.DataBlocks.Add(t);
        }
    }
}
