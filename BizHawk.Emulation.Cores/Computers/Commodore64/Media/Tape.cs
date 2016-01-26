using System;
using System.Text;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media
{
	/**
		* This class represents a tape. Only TAP-style tapes are supported for now.
		*/
	public class Tape
	{
		private readonly byte[] _tapeData;
		private readonly byte _version;
		private int _pos, _cycle;
		private readonly int _start, _end;
	    private bool _data;

		public Tape(byte version, byte[] tapeData, int start, int end)
		{
			_version = version;
			_tapeData = tapeData;
			_start = start;
			_end = end;
			Rewind();
		}

	    public void ExecuteCycle2()
	    {
            if (_cycle == 0)
            {
                if (_pos >= _end)
                {
                    _data = true;
                    return;
                }

                _cycle = _tapeData[_pos++] * 8;
                if (_cycle == 0)
                {
                    if (_version == 0)
                    {
                        _cycle = 256 * 8; // unspecified overflow condition
                    }
                    else
                    {
                        _cycle = BitConverter.ToInt32(_tapeData, _pos - 1) >> 8;
                        _pos += 3;
                        if (_cycle == 0)
                        {
                            throw new Exception("Bad tape data");
                        }
                    }
                }
            }

            // Send a single negative pulse at the end of a cycle
            _data = --_cycle != 0;
        }

        // Rewinds the tape back to start
        public void Rewind()
		{
			_pos = _start;
			_cycle = 0;
		}

		// Reads from tape, this will tell the caller if the flag pin should be raised
		public bool Read()
		{
		    return _data;
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
			ser.Sync("pos", ref _pos);
			ser.Sync("cycle", ref _cycle);
			ser.EndSection();
		}
	}
}
