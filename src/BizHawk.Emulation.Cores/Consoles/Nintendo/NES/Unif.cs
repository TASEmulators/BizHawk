using System.Collections.Generic;
using System.Text;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.IOExtensions;


namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/// <summary>
	/// at least it's not iNES 2.0...
	/// </summary>
	public class Unif
	{
		private Dictionary<string, byte[]> Chunks { get; } = new Dictionary<string, byte[]>();

		private void TryAdd(Stream s, string key)
		{
			if (!Chunks.TryGetValue(key, out var data))
			{
				return;
			}

			s.Write(data, 0, data.Length);
		}

		public Unif(Stream s)
		{
			var br = new BinaryReader(s, Encoding.ASCII);

			if (!br.ReadBytes(4).SequenceEqual("UNIF"u8))
			{
				throw new Exception("Missing \"UNIF\" header mark!");
			}

			int ver = br.ReadInt32();

			Console.WriteLine("Processing Version {0} UNIF...", ver);
			br.ReadBytes(32 - 4 - 4);

			while (br.PeekChar() > 0)
			{
				string chunkId = Encoding.ASCII.GetString(br.ReadBytes(4));
				int length = br.ReadInt32();
				byte[] chunkData = br.ReadBytes(length);
				Chunks.Add(chunkId, chunkData);
			}

			var prgs = new MemoryStream();
			var chrs = new MemoryStream();
			for (int i = 0; i < 16; i++)
			{
				TryAdd(prgs, $"PRG{i:X1}");
				TryAdd(chrs, $"CHR{i:X1}");
			}

			prgs.Close();
			chrs.Close();
			Prg = prgs.ToArray();
			Chr = chrs.ToArray();

			Cart.PrgSize = (short)(Prg.Length / 1024);
			Cart.ChrSize = (short)(Chr.Length / 1024);

			if (Chunks.TryGetValue("MIRR", out var tmp))
			{
				switch (tmp[0])
				{
					case 0: // h mirror
						Cart.PadH = 0;
						Cart.PadV = 1;
						break;
					case 1: // v mirror
						Cart.PadH = 1;
						Cart.PadV = 0;
						break;
				}
			}

			if (Chunks.TryGetValue("MAPR", out tmp))
			{
				Cart.BoardType = new BinaryReader(new MemoryStream(tmp)).ReadStringUtf8NullTerminated();
			}

			Cart.BoardType = Cart.BoardType.TrimEnd('\0');
			Cart.BoardType = "UNIF_" + Cart.BoardType;

			if (Chunks.ContainsKey("BATR"))
			{
				// apparently, this chunk just existing means battery is yes
				Cart.WramBattery = true;
			}

			Cart.Sha1 = SHA1Checksum.ComputeConcat(Prg, Chr).BytesToHexString();

			// other code will expect this
			if (Chr.Length == 0)
			{
				Chr = null;
			}
		}

		public CartInfo Cart { get; } = new CartInfo();
		public byte[] Prg { get; }
		public byte[] Chr { get; }
	}
}
