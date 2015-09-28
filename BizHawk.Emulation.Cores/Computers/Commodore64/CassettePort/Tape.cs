using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using BizHawk.Common;


namespace BizHawk.Emulation.Cores.Computers.Commodore64.CassettePort
{
	/**
		* This class represents a tape. Only TAP-style tapes are supported for now.
		*/
	class Tape
	{
		private byte[] tapeData;
		private byte version;
		private uint pos, cycle, start, end;

		public Tape(byte version, byte[] tapeData, uint start, uint end)
		{
			this.version = version;
			this.tapeData = tapeData;
			this.start = start;
			this.end = end;
			rewind();
		}

		// Rewinds the tape back to start
		public void rewind()
		{
			pos = start;
			cycle = 0;
		}

		// Reads from tape, this will tell the caller if the flag pin should be raised
		public bool read()
		{
			if (cycle == 0)
			{
				if (pos >= end)
				{
					return true;
				}
				else
				{
					cycle = ((uint)tapeData[pos++])*8;
					if (cycle == 0)
					{
						if (version == 0)
						{
							cycle = 256 * 8; // unspecified overflow condition
						}
						else
						{
							cycle = BitConverter.ToUInt32(tapeData, (int)pos-1)>>8;
							pos += 3;
							if (cycle == 0)
							{
								throw new Exception("Bad tape data");
							}
						}
					}
				}
			}

			// Send a single negative pulse at the end of a cycle
			return --cycle != 0;
		}

		// Try to construct a tape file from file data. Returns null if not a tape file, throws exceptions for bad tape files.
		// (Note that some error conditions aren't caught right here.)
		static public Tape Load(byte[] tapeFile)
		{
			Tape result = null;

			if (System.Text.Encoding.ASCII.GetString(tapeFile, 0, 12) == "C64-TAPE-RAW")
			{
				byte version = tapeFile[12];
				if (version > 1) throw new Exception("This tape has an unsupported version");
				uint size = BitConverter.ToUInt32(tapeFile, 16);
				if (size + 20 != tapeFile.Length)
				{
					throw new Exception("Tape file header specifies a length that doesn't match the file size");
				}
				result = new Tape(version, tapeFile, 20, (uint)tapeFile.Length);
			}
			return result;
		}
	}
}
