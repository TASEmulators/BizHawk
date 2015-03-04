using System;
using System.IO;
using System.Runtime.InteropServices;

using BizHawk.Emulation.DiscSystem;

class MednadiscTester
{
	[DllImport("mednadisc.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr mednadisc_LoadCD(string path);

	[DllImport("mednadisc.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern int mednadisc_ReadSector(IntPtr disc, int lba, byte[] buf2448);

	[DllImport("mednadisc.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void mednadisc_CloseCD(IntPtr disc);


	public class QuickSubcodeReader
	{
		public QuickSubcodeReader(byte[] buffer)
		{
			this.buffer = buffer;
		}

		public void ReadLBA_SubchannelQ(int offset, ref SubchannelQ sq)
		{
			sq.q_status = buffer[offset + 0];
			sq.q_tno = buffer[offset + 1];
			sq.q_index = buffer[offset + 2];
			sq.min.BCDValue = buffer[offset + 3];
			sq.sec.BCDValue = buffer[offset + 4];
			sq.frame.BCDValue = buffer[offset + 5];
			//nothing in byte[6]
			sq.ap_min.BCDValue = buffer[offset + 7];
			sq.ap_sec.BCDValue = buffer[offset + 8];
			sq.ap_frame.BCDValue = buffer[offset + 9];

			//CRC is stored inverted and big endian.. so... do the opposite
			byte hibyte = (byte)(~buffer[offset + 10]);
			byte lobyte = (byte)(~buffer[offset + 11]);
			sq.q_crc = (ushort)((hibyte << 8) | lobyte);
		}

		byte[] buffer;
	}

	public void TestDirectory(string dpTarget)
	{
		foreach (var fi in new DirectoryInfo(dpTarget).GetFiles())
		{
			if (fi.Extension.ToLower() == ".cue") { }
			else if (fi.Extension.ToLower() == ".ccd") { }
			else continue;

			NewTest(fi.FullName);
		}
	}

	static bool NewTest(string path)
	{
		bool ret = false;
		
		Disc disc;
		if (Path.GetExtension(path).ToLower() == ".cue") disc = Disc.FromCuePath(path, new CueBinPrefs());
		else disc = Disc.FromCCDPath(path);
		IntPtr mednadisc = mednadisc_LoadCD(path);

		//TODO - test leadout a bit, or determine length some superior way
		//TODO - check length against mednadisc

		int nSectors = (int)(disc.Structure.BinarySize / 2352) - 150;
		var subbuf = new byte[96];
		var discbuf = new byte[2352 + 96];
		var monkeybuf = new byte[2352 + 96];
		var disc_qbuf = new byte[96];
		var monkey_qbuf = new byte[96];

		for (int i = 0; i < nSectors; i++)
		{
			mednadisc_ReadSector(mednadisc, i, monkeybuf);
			disc.ReadLBA_2352(i, discbuf, 0);
			disc.ReadLBA_SectorEntry(i).SubcodeSector.ReadSubcodeDeinterleaved(subbuf, 0);
			SubcodeUtils.Interleave(subbuf, 0, discbuf, 2352);
			//remove P 
			for (int q = 2352; q < 2352 + 96; q++)
			{
				discbuf[q] &= 0x7F;
				monkeybuf[q] &= 0x7F;
			}
			for (int q = 0; q < 2352 + 96; q++)
			{
				if (discbuf[q] != monkeybuf[q])
				{
					Console.WriteLine("MISMATCH: " + Path.GetFileName(path));

					//decode Q subchannels for manual investigation
					SubcodeUtils.Deinterleave(discbuf, 2352, disc_qbuf, 0);
					var asr = new QuickSubcodeReader(disc_qbuf);
					SubchannelQ disc_q = new SubchannelQ();
					asr.ReadLBA_SubchannelQ(12, ref disc_q);

					SubcodeUtils.Deinterleave(monkeybuf, 2352, monkey_qbuf, 0);
					asr = new QuickSubcodeReader(monkey_qbuf);
					SubchannelQ monkey_q = new SubchannelQ();
					asr.ReadLBA_SubchannelQ(12, ref monkey_q);

					goto END;
				}
			}
		}

		ret = true;

	END:
		disc.Dispose();
		mednadisc_CloseCD(mednadisc);

		return ret;
	}
}