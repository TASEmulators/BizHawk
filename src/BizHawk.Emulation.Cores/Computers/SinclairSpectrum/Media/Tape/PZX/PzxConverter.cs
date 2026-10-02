using BizHawk.Common.NumberExtensions;

using System.Collections.Generic;
using System.Text;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Cores.Tapes;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Reponsible for PZX format serializaton
	/// Based on the information here:  http://zxds.raxoft.cz/docs/pzx.txt
	/// </summary>
	public sealed class PzxConverter : MediaConverter
	{
		/// <summary>
		/// The type of serializer
		/// </summary>
		private readonly MediaConverterType _formatType = MediaConverterType.PZX;
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
			=> nameof(PzxConverter);

		/// <summary>
		/// Position counter
		/// </summary>
		private int _position = 0;

		private readonly DatacorderDevice _datacorder;

		public PzxConverter(DatacorderDevice _tapeDevice)
		{
			_datacorder = _tapeDevice;
		}

		/// <summary>
		/// Returns TRUE if pzx header is detected
		/// </summary>
		public override bool CheckType(byte[] data)
		{
			// PZX Header - the "PZXT" tag is the first 4 bytes. Guard against shorter files: CheckType runs on
			// every tape during type-detection, so a short file of another format must not throw here.
			if (data == null || data.Length < 4)
				return false;

			string ident = Encoding.ASCII.GetString(data, 0, 4);
			return "PZXT".EqualsIgnoreCase(ident);
		}

		/// <summary>
		/// DeSerialization method
		/// </summary>
		public override void Read(byte[] data)
		{
			// clear existing tape blocks
			_datacorder.DataBlocks.Clear();

			/*
                // PZX uniform block layout
                offset type     name   meaning
                ------ ----     ----   -------
                0      u32      tag    unique identifier for the block type.
                4      u32      size   size of the block in bytes, excluding the tag and size fields themselves.
                8      u8[size] data   arbitrary amount of block data.
            */

			// check whether this is a valid pzx format file by looking at the identifier in the header block
			string ident = Encoding.ASCII.GetString(data, 0, 4);

			if (!"PZXT".EqualsIgnoreCase(ident))
			{
				// this is not a valid TZX format file
				throw new Exception($"{nameof(PzxConverter)}: This is not a valid PZX format file");
			}

			_position = 0;

			// parse all blocks out into seperate byte arrays first
			List<byte[]> bDatas = new List<byte[]>();

			while (_position < data.Length)
			{
				int startPos = _position;

				// data size
				_position += 4;
				int blockSize = GetInt32(data, _position);
				_position += 4;

				// block data
				byte[] bd = new byte[8 + blockSize];
				Array.Copy(data, startPos, bd, 0, bd.Length);
				bDatas.Add(bd);

				_position += blockSize;
			}

			// process the blocks
			foreach (var b in bDatas)
			{
				int pos = 8;
				string blockId = Encoding.ASCII.GetString(b, 0, 4);
				int blockSize = GetInt32(b, 4);

				TapeDataBlock t = new TapeDataBlock();

				switch (blockId)
				{
					// PZXT - PZX header block
					/*
                        offset type     name   meaning
                        0      u8       major  major version number (currently 1).
                        1      u8       minor  minor version number (currently 0).
                        2      u8[?]    info   tape info, see below.
                    */
					case "PZXT":

						break;

					// PULS - Pulse sequence
					/*
                        offset type   name      meaning
                        0      u16    count     bits 0-14 optional repeat count (see bit 15), always greater than zero
                                                bit 15 repeat count present: 0 not present 1 present
                        2      u16    duration1 bits 0-14 low/high (see bit 15) pulse duration bits
                                                bit 15 duration encoding: 0 duration1 1 ((duration1<<16)+duration2)
                        4      u16    duration2 optional low bits of pulse duration (see bit 15 of duration1)
                        6      ...    ...       ditto repeated until the end of the block
                    */
					case "PULS":

						t.BlockID = GetInt32(b, 0);
						t.DataPeriods = new List<int>();
						t.DataLevels = new List<bool>();

						t.InitialPulseLevel = false;
						bool pLevel = !t.InitialPulseLevel;

						var pulses = new List<(int Count, int Duration)>();

						while (pos < blockSize + 8)
						{
							int count = 1;
							int duration = GetWordValue(b, pos);
							pos += 2;

							// bit 15 of the first word => a repeat count (low 15 bits) is present; the duration follows
							if (duration >= 0x8000)
							{
								count = duration & 0x7FFF;
								duration = GetWordValue(b, pos);
								pos += 2;
							}

							// bit 15 of the duration word => extended duration ((d & 0x7FFF) << 16) | next word.
							// (kept as int, not ushort - the old code shifted a ushort left 16 and lost the high bits)
							if (duration >= 0x8000)
							{
								duration = ((duration & 0x7FFF) << 16) | GetWordValue(b, pos);
								pos += 2;
							}

							pulses.Add((count, duration));
						}

						// convert to tape block
						t.BlockDescription = BlockType.PULS;
						t.PauseInMS = 0;

						foreach (var (count, duration) in pulses)
						{
							for (int i = 0; i < count; i++)
							{
								t.DataPeriods.Add(duration);
								pLevel = !pLevel;
								t.DataLevels.Add(pLevel);
							}
						}

						_datacorder.DataBlocks.Add(t);

						break;

					// DATA - Data block
					/*
                        offset      type             name  meaning
                        0           u32              count bits 0-30 number of bits in the data stream
                                                           bit 31 initial pulse level: 0 low 1 high
                        4           u16              tail  duration of extra pulse after last bit of the block
                        6           u8               p0    number of pulses encoding bit equal to 0.
                        7           u8               p1    number of pulses encoding bit equal to 1.
                        8           u16[p0]          s0    sequence of pulse durations encoding bit equal to 0.
                        8+2*p0      u16[p1]          s1    sequence of pulse durations encoding bit equal to 1.
                        8+2*(p0+p1) u8[ceil(bits/8)] data  data stream, see below.
                    */
					case "DATA":

						t.BlockID = GetInt32(b, 0);
						t.DataPeriods = new List<int>();
						t.DataLevels = new List<bool>();

						List<ushort> s0 = new List<ushort>();
						List<ushort> s1 = new List<ushort>();

						uint initPulseLevel = 1;
						int dCount = 1;
						ushort tail = 0;

						while (pos < blockSize + 8)
						{
							dCount = GetInt32(b, pos);
							initPulseLevel = (uint)((dCount & 0x80000000) == 0 ? 0 : 1);

							t.InitialPulseLevel = initPulseLevel == 1;
							bool bLevel = !t.InitialPulseLevel;

							dCount &= 0x7FFFFFFF;
							pos += 4;

							tail = GetWordValue(b, pos);
							pos += 2;

							var p0 = b[pos++];
							var p1 = b[pos++];

							// s0 has p0 pulse durations, s1 has p1 (the s0 loop previously used p1 in error,
							// mis-reading s0 and generating the wrong pulses whenever p0 != p1)
							for (int i = 0; i < p0; i++)
							{
								var s = GetWordValue(b, pos);
								pos += 2;
								s0.Add(s);
							}

							for (int i = 0; i < p1; i++)
							{
								var s = GetWordValue(b, pos);
								pos += 2;
								s1.Add(s);
							}

							var dData = b.AsSpan(start: pos, length: (dCount + 7) >> 3/* == `ceil(dCount/8)` */);
							pos += dData.Length;

							// retain the raw data bytes so a trap/flash-loader can read the block directly
							t.BlockData = dData.ToArray();

							// emit exactly dCount bits (MSb first); the final byte may be partial
							int bitsDone = 0;
							foreach (var by in dData)
							{
								for (int i = 7; i >= 0 && bitsDone < dCount; i--, bitsDone++)
								{
									if (by.Bit(i))
									{
										foreach (var pu in s1)
										{
											t.DataPeriods.Add(pu);
											bLevel = !bLevel;
											t.DataLevels.Add(bLevel);
										}
									}
									else
									{
										foreach (var pu in s0)
										{
											t.DataPeriods.Add(pu);
											bLevel = !bLevel;
											t.DataLevels.Add(bLevel);
										}
									}
								}
							}
							if (tail > 0)
							{
								t.DataPeriods.Add(tail);
								bLevel = !bLevel;
								t.DataLevels.Add(bLevel);
							}
						}

						// convert to tape block
						t.BlockDescription = BlockType.DATA;
						t.PauseInMS = 0;

						// tail
						//t.DataPeriods.Add(tail);

						_datacorder.DataBlocks.Add(t);

						break;

					// PAUS - Pause
					/*
                        offset type   name      meaning
                        0      u32    duration  bits 0-30 duration of the pause
                                                bit 31 initial pulse level: 0 low 1 high
                    */
					case "PAUS":

						t.BlockID = GetInt32(b, 0);
						t.DataPeriods = new List<int>();

						int iniPulseLevel = 1;
						int pCount = 0;

						var d = GetInt32(b, pos);
						iniPulseLevel = ((d & 0x80000000) == 0 ? 0 : 1);
						t.InitialPulseLevel = iniPulseLevel == 1;
						bool paLevel = !t.InitialPulseLevel;
						pCount = (d & 0x7FFFFFFF);

						// convert to tape block
						t.BlockDescription = BlockType.PAUS;
						paLevel = !paLevel;
						t.DataLevels.Add(paLevel);
						t.DataPeriods.Add(0);

						paLevel = !paLevel;
						t.DataLevels.Add(paLevel);
						t.DataPeriods.Add(pCount);

						paLevel = !paLevel;
						t.DataLevels.Add(paLevel);
						t.DataPeriods.Add(0);

						_datacorder.DataBlocks.Add(t);

						break;

					// BRWS - Browse point
					/*
                        offset type   name   meaning
                        0      u8[?]  text   text describing this browse point
                    */
					case "BRWS":

						t.BlockID = GetInt32(b, 0);
						t.DataPeriods = new List<int>();

						string info = Encoding.ASCII.GetString(b, 8, blockSize);

						// convert to tape block
						t.BlockDescription = BlockType.BRWS;
						t.MetaData.Add(BlockDescriptorTitle.Comments, info);
						t.PauseInMS = 0;

						_datacorder.DataBlocks.Add(t);

						break;

					// STOP - Stop tape command
					/*
                        offset type   name   meaning
                        0      u16    flags  when exactly to stop the tape (1 48k only, other always).
                    */
					case "STOP":


						t.BlockID = GetInt32(b, 0);
						t.DataPeriods = new List<int>();

						var flags = GetWordValue(b, pos);
						if (flags == 1)
						{
							t.BlockDescription = BlockType.Stop_the_Tape_48K;
							t.Command = TapeCommand.STOP_THE_TAPE_48K;
						}
						else
						{
							t.BlockDescription = BlockType.Pause_or_Stop_the_Tape;
							t.Command = TapeCommand.STOP_THE_TAPE;
						}

						_datacorder.DataBlocks.Add(t);

						break;
				}
			}
		}
	}
}
