using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal static class FTXT
	{
		public static readonly Encoding E = Encoding.GetEncoding("ISO-8859-1");
	}

	public class FDSFile
	{
		private static readonly byte[] Check = FTXT.E.GetBytes("FDS\x1a");
		private static readonly byte[] CheckAlt = FTXT.E.GetBytes("\x01*NIN");

		public readonly IList<FDSDisk> Disks = new List<FDSDisk>();

		public byte[] ExtraData;

		public FDSFile(BinaryReader r)
		{
			byte[] tmp = r.ReadBytes(4);
			int numdisks;
			if (tmp.SequenceEqual(Check))
			{
				numdisks = r.ReadByte();
				ExtraData = r.ReadBytes(11);
			}
			else if (tmp.SequenceEqual(CheckAlt))
			{
				// compensate (won't write identically)
				r.BaseStream.Seek(0, SeekOrigin.Begin);
				numdisks = (int)(r.BaseStream.Length / 65500);
				ExtraData = new byte[11];
			}
			else
			{
				throw new InvalidOperationException("Bad Header!");
			}
			if (r.BaseStream.Length - r.BaseStream.Position != numdisks * 65500)
				throw new InvalidOperationException("Bad File Length!");
			while (numdisks > 0)
			{
				Disks.Add(new FDSDisk(r));
				numdisks--;
			}
		}

		public void Write(BinaryWriter w)
		{
			w.Write(Check);
			w.Write((byte)Disks.Count);
			w.Write(ExtraData);
			foreach (var disk in Disks)
				disk.Write(w);
		}
	}

	public class FDSDisk
	{
		private static readonly byte[] Check = FTXT.E.GetBytes("*NINTENDO-HVC*");

		public readonly IList<FDSChunk> Chunks = new List<FDSChunk>();

		public byte[] HeaderData;
		public byte[] ExtraData;

		public FDSDisk(BinaryReader r)
		{
			long startpos = r.BaseStream.Position;
			long endpos = startpos + 65500;

			if (r.ReadByte() != 1)
				throw new InvalidOperationException("Bad Block Code");

			if (!r.ReadBytes(14).SequenceEqual(Check))
				throw new InvalidOperationException("Bad Verification Code");

			// one could do quite a bit with this data
			HeaderData = r.ReadBytes(41);

			if (r.ReadByte() != 2)
				throw new InvalidOperationException("Bad Block Code");

			int nfiles = r.ReadByte();

			while (nfiles > 0)
			{
				Chunks.Add(new FDSChunk(r));
				nfiles--;
			}
			if (r.BaseStream.Position > endpos)
				throw new InvalidOperationException("Disk too long");

			long usedpos = r.BaseStream.Position;
			while (true)
			{
				try
				{
					var chunk = new FDSChunk(r) { Hidden = true };
					if (r.BaseStream.Position <= endpos)
					{
						Chunks.Add(chunk);
						usedpos = r.BaseStream.Position;
					}
					else
					{
						break;
					}
				}
				catch { break; }
			}

			r.BaseStream.Seek(usedpos, SeekOrigin.Begin);
			ExtraData = r.ReadBytes((int)(endpos - usedpos));
		}

		public void Write(BinaryWriter w)
		{
			w.Write((byte)1);
			w.Write(Check);
			w.Write(HeaderData);
			w.Write((byte)2);
			w.Write((byte)Chunks.Count(c => !c.Hidden));
			foreach (var chunk in Chunks)
			{
				chunk.Write(w);
			}
			w.Write(ExtraData);
		}
	}

	public class FDSChunk
	{
		/// <summary>
		/// true if BIOS will ignore this file.  this flag is not directly stored.
		/// </summary>
		public bool Hidden;

		public enum FileKindE : byte
		{
			PRAM = 0,
			CRAM = 1,
			VRAM = 2
		}

		public byte Number;
		public byte ID;
		public string Name;
		public ushort Address;

		public FileKindE FileKind;
		public byte[] Data;

		public FDSChunk(BinaryReader r)
		{
			if (r.ReadByte() != 3)
				throw new InvalidOperationException("Bad Block Code");
			Number = r.ReadByte();
			ID = r.ReadByte();
			Name = FTXT.E.GetString(r.ReadBytes(8));
			Address = r.ReadUInt16();
			int size = r.ReadUInt16();
			FileKind = (FileKindE)r.ReadByte();

			if (r.ReadByte() != 4)
				throw new InvalidOperationException("Bad Block Code");
			Data = r.ReadBytes(size);
		}

		public void Write(BinaryWriter w)
		{
			w.Write((byte)3);
			w.Write(Number);
			w.Write(ID);
			w.Write(FTXT.E.GetBytes(Name));
			w.Write(Address);
			w.Write((ushort)Data.Length);
			w.Write((byte)FileKind);
			w.Write((byte)4);
			w.Write(Data);
		}
	}

	public class FDSInspector
	{
	}
}
