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
	public class Tape
	{
		private readonly byte[] tapeData;
		private readonly byte version;
		private int pos, cycle;
		private readonly int start, end;

		public Tape(byte version, byte[] tapeData, int start, int end)
		{
			this.version = version;
			this.tapeData = tapeData;
			this.start = start;
			this.end = end;
			Rewind();
		}

		// Rewinds the tape back to start
		public void Rewind()
		{
			pos = start;
			cycle = 0;
		}

		// Reads from tape, this will tell the caller if the flag pin should be raised
		public bool Read()
		{
			if (cycle == 0)
			{
				if (pos >= end)
				{
					return true;
				}

			    cycle = tapeData[pos++]*8;
			    if (cycle == 0)
			    {
			        if (version == 0)
			        {
			            cycle = 256 * 8; // unspecified overflow condition
			        }
			        else
			        {
			            cycle = BitConverter.ToInt32(tapeData, pos-1)>>8;
			            pos += 3;
			            if (cycle == 0)
			            {
			                throw new Exception("Bad tape data");
			            }
			        }
			    }
			}

			// Send a single negative pulse at the end of a cycle
			return --cycle != 0;
		}

		// Try to construct a tape file from file data. Returns null if not a tape file, throws exceptions for bad tape files.
		// (Note that some error conditions aren't caught right here.)
		public static Tape Load(byte[] tapeFile)
		{
			Tape result = null;

			if (Encoding.ASCII.GetString(tapeFile, 0, 12) == "C64-TAPE-RAW")
			{
				var version = tapeFile[12];
				if (version > 1) throw new Exception("This tape has an unsupported version");
				var size = BitConverter.ToUInt32(tapeFile, 16);
				if (size + 20 != tapeFile.Length)
				{
					throw new Exception("Tape file header specifies a length that doesn't match the file size");
				}
				result = new Tape(version, tapeFile, 20, tapeFile.Length);
			}
			return result;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("tape");
			ser.Sync("pos", ref pos);
			ser.Sync("cycle", ref cycle);
			ser.EndSection();
		}
	}
}
