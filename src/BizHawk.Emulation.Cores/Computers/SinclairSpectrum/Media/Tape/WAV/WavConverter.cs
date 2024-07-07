using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Reponsible for WAV format conversion
	/// Based heavily on code from zxmak2: https://archive.codeplex.com/?p=zxmak2
	/// </summary>
	public sealed class WavConverter : MediaConverter
	{
		/// <summary>
		/// The type of serializer
		/// </summary>
		private readonly MediaConverterType _formatType = MediaConverterType.WAV;
		public override MediaConverterType FormatType => _formatType;

		/// <summary>
		/// Signs whether this class can be used to read the data format
		/// </summary>
		public override bool IsReader => true;

		/// <summary>
		/// Signs whether this class can be used to write the data format
		/// </summary>
		public override bool IsWriter => false;

		protected override string SelfTypeName
			=> nameof(WavConverter);

		/// <summary>
		/// Position counter
		/// </summary>
		//private int _position = 0;
		private readonly DatacorderDevice _datacorder;

		public WavConverter(DatacorderDevice _tapeDevice)
		{
			_datacorder = _tapeDevice;
		}

		/// <summary>
		/// Returns TRUE if pzx header is detected
		/// </summary>
		public override bool CheckType(byte[] data)
		{
			// WAV Header

			// check whether this is a valid wav format file by looking at the identifier in the header
			string ident = Encoding.ASCII.GetString(data, 8, 4);

			if (ident.ToUpperInvariant() != "WAVE")
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
		public override void Read(byte[] data)
		{
			// clear existing tape blocks
			_datacorder.DataBlocks.Clear();

			// check whether this is a valid pzx format file by looking at the identifier in the header block
			string ident = Encoding.ASCII.GetString(data, 8, 4);

			if (ident.ToUpperInvariant() != "WAVE")
			{
				// this is not a valid TZX format file
				throw new Exception($"{nameof(WavConverter)}: This is not a valid WAV format file");
			}

			//_position = 0;

			MemoryStream stream = new MemoryStream();
			stream.Write(data, 0, data.Length);
			stream.Position = 0;

			WavStreamReader reader = new WavStreamReader(stream);

			const double d = /*69888.0*/70000.0 * 50.0;
			int rate = (int) (d / reader.Header.sampleRate);
			int smpCounter = 0;
			int state = reader.ReadNext();

			// create the single tape block
			TapeDataBlock t = new TapeDataBlock();
			t.BlockDescription = BlockType.WAV_Recording;
			t.BlockID = 0;
			t.DataPeriods = new List<int>();
			t.DataLevels = new List<bool>();

			bool currLevel = false;

			for (int i = 0; i < reader.Count; i++)
			{
				int sample = reader.ReadNext();
				smpCounter++;
				if ((state < 0 && sample < 0) || (state >= 0 && sample >= 0))
					continue;
				t.DataPeriods.Add((int)((smpCounter * (double)rate) / (double)0.9838560885608856));
				currLevel = !currLevel;
				t.DataLevels.Add(currLevel);
				smpCounter = 0;
				state = sample;
			}

			// add closing period
			t.DataPeriods.Add((69888 * 50) / 10);
			currLevel = false;
			t.DataLevels.Add(currLevel);

			// add to datacorder
			_datacorder.DataBlocks.Add(t);

			/* debug stuff

			StringBuilder export = new StringBuilder();
			foreach (var b in _datacorder.DataBlocks)
			{
				for (int i = 0; i < b.DataPeriods.Count(); i++)
				{
					export.Append(b.DataPeriods[i].ToString());
					export.Append("\t\t");
					export.AppendLine(b.DataLevels[i].ToString());
				}
			}

			string o = export.ToString();
			*/
		}
	}
}
