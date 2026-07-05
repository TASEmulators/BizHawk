using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>One track/side extracted from an HFE image: the raw MFM cell bitstream as an MfmTrack.</summary>
	public sealed class HfeTrack
	{
		public int Cylinder;
		public int Side;
		public MfmTrack Track;
	}

	/// <summary>
	/// Loader for the HxC Floppy Emulator HFE v1/v2 format ("HXCPICFE"). HFE stores each track as
	/// a raw cell bitstream (clock + data), LSB first - which is exactly how MfmTrack packs its cells - so a
	/// track loads by de-interleaving the two sides (stored in alternating 256-byte halves of 512-byte
	/// blocks) with no re-encoding; the FDC reader locks phase on the A1 sync marks in the stream. HFEv3
	/// ("HXCHFEV3") adds an opcode stream and is not handled here.
	/// </summary>
	public static class HfeConverter
	{
		private const int HeaderSize = 512;
		private const int BlockSize = 512;
		private const int SideHalf = 256;

		public static bool IsHfe(byte[] d)
			=> d != null && d.Length >= HeaderSize && Match(d, "HXCPICFE");

		public static bool IsHfeV3(byte[] d)
			=> d != null && d.Length >= HeaderSize && Match(d, "HXCHFEV3");

		private static bool Match(byte[] d, string sig)
		{
			for (int i = 0; i < 8; i++) if (d[i] != (byte)sig[i]) return false;
			return true;
		}

		public static FluxDisk ToFluxDisk(byte[] hfe)
		{
			var (tracks, writeProtected) = Parse(hfe);
			var disk = new FluxDisk { WriteProtected = writeProtected };
			foreach (var t in tracks) disk.SetTrack(t.Cylinder, t.Side, t.Track);
			return disk;
		}

		public static (List<HfeTrack> Tracks, bool WriteProtected) Parse(byte[] d)
		{
			if (IsHfeV3(d)) throw new System.ArgumentException("HFEv3 (opcode stream) is not supported", nameof(d));
			if (!IsHfe(d)) throw new System.ArgumentException("not an HFE file (no HXCPICFE signature)", nameof(d));

			int numTracks = d[0x009];
			int numSides = d[0x00A];
			int lutOffset = ReadLe16(d, 0x012) * BlockSize;
			bool writeProtected = d[0x014] == 0x00; // 0x00 = write protected, 0xFF = unprotected

			var tracks = new List<HfeTrack>();
			for (int t = 0; t < numTracks; t++)
			{
				int lut = lutOffset + t * 4;
				if (lut + 4 > d.Length) break;
				int dataOffset = ReadLe16(d, lut) * BlockSize;
				int trackLen = ReadLe16(d, lut + 2);
				if (dataOffset <= 0 || trackLen <= 0 || dataOffset > d.Length) continue;

				for (int side = 0; side < numSides; side++)
				{
					byte[] cells = DeinterleaveSide(d, dataOffset, trackLen, side);
					if (cells.Length == 0) continue;
					tracks.Add(new HfeTrack { Cylinder = t, Side = side, Track = new MfmTrack(cells, cells.Length * 8) });
				}
			}
			return (tracks, writeProtected);
		}

		// Gather one side's cell bytes out of the interleaved 512-byte blocks (side 0 = first 256 bytes of a
		// block, side 1 = next 256). The bytes are already LSB-first cells, matching MfmTrack's packing.
		private static byte[] DeinterleaveSide(byte[] d, int dataOffset, int trackLen, int side)
		{
			var outb = new List<byte>(trackLen / 2 + SideHalf);
			int pos = dataOffset;
			int remaining = trackLen;
			while (remaining > 0)
			{
				int block = System.Math.Min(BlockSize, remaining);
				int sideStart = pos + (side == 0 ? 0 : SideHalf);
				int sideLen = System.Math.Min(SideHalf, System.Math.Max(0, block - (side == 0 ? 0 : SideHalf)));
				for (int i = 0; i < sideLen; i++)
				{
					int p = sideStart + i;
					if (p >= d.Length) break;
					outb.Add(d[p]);
				}
				pos += BlockSize;
				remaining -= BlockSize;
			}
			return outb.ToArray();
		}

		private static int ReadLe16(byte[] d, int o) => d[o] | (d[o + 1] << 8);
	}
}
