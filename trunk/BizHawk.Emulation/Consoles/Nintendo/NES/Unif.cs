using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	/// <summary>
	/// at least it's not iNES 2.0...
	/// </summary>
	public class Unif
	{
		NES.CartInfo ci = new NES.CartInfo();
		byte[] prgrom;
		byte[] chrrom;

		Dictionary<string, byte[]> chunks = new Dictionary<string, byte[]>();

		void TryAdd(Stream s, string key)
		{
			byte[] data;
			if (!chunks.TryGetValue(key, out data))
				return;
			s.Write(data, 0, data.Length);
		}

		public Unif(Stream s)
		{
			BinaryReader br = new BinaryReader(s, Encoding.ASCII);

			if (!Encoding.ASCII.GetBytes("UNIF").SequenceEqual(br.ReadBytes(4)))
				throw new Exception("Missing \"UNIF\" header mark!");
			int ver = br.ReadInt32();
			//if (ver != 7)
			//	throw new Exception(string.Format("Unknown UNIF version {0}!", ver));
			Console.WriteLine("Processing Version {0} UNIF...", ver);
			br.ReadBytes(32 - 4 - 4);

			while (br.PeekChar() > 0)
			{
				string chunkid = Encoding.ASCII.GetString(br.ReadBytes(4));
				int length = br.ReadInt32();
				byte[] chunkdata = br.ReadBytes(length);
				chunks.Add(chunkid, chunkdata);
			}

			MemoryStream prgs = new MemoryStream();
			MemoryStream chrs = new MemoryStream();
			for (int i = 0; i < 16; i++)
			{
				TryAdd(prgs, string.Format("PRG{0:X1}", i));
				TryAdd(chrs, string.Format("CHR{0:X1}", i));
			}
			prgs.Close();
			chrs.Close();
			prgrom = prgs.ToArray();
			chrrom = chrs.ToArray();

			ci.prg_size = (short)(prgrom.Length / 1024);
			ci.chr_size = (short)(chrrom.Length / 1024);

			byte[] tmp;

			if (chunks.TryGetValue("MIRR", out tmp))
			{
				switch (tmp[0])
				{
					case 0: // hmirror
						ci.pad_h = 0;
						ci.pad_v = 1;
						break;
					case 1: // vmirror
						ci.pad_h = 1;
						ci.pad_v = 0;
						break;
				}
			}

			if (chunks.TryGetValue("MAPR", out tmp))
				ci.board_type = Encoding.ASCII.GetString(tmp);
			ci.board_type = ci.board_type.TrimEnd('\0');
			ci.board_type = "UNIF_" + ci.board_type;

			// is there any way using System.Security.Cryptography.SHA1 to compute the hash of
			// prg concatentated with chr?  i couldn't figure it out, so this implementation is dumb
			{
				MemoryStream ms = new MemoryStream();
				ms.Write(prgrom, 0, prgrom.Length);
				ms.Write(chrrom, 0, chrrom.Length);
				ms.Close();
				byte[] all = ms.ToArray();
				ci.sha1 = "sha1:" + Util.Hash_SHA1(all, 0, all.Length);
			}
		}

		public NES.CartInfo GetCartInfo() { return ci; }
		public byte[] GetPRG() { return prgrom; }
		public byte[] GetCHR() { return chrrom; }

	}
}
