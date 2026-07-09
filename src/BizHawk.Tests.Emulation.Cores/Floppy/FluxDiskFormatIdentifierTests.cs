using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Tests the content heuristics that pick a System for the container-agnostic flux formats (.scp/.hfe),
	/// which carry no reliable platform id. Disks are synthesized in code and fed to the FluxDisk overload.
	/// </summary>
	[TestClass]
	public sealed class FluxDiskFormatIdentifierTests
	{
		private static FluxDisk DiskFromTrack0(List<TrackSector> secs)
		{
			var disk = new FluxDisk();
			disk.SetTrack(0, 0, StandardMfmFormat.BuildStandardTrack(secs));
			return disk;
		}

		[TestMethod]
		public void TrDosInfoSector_IdentifiesZXSpectrum()
		{
			var secs = new List<TrackSector>();
			for (int r = 1; r <= 9; r++)
			{
				var data = new byte[256];
				if (r == 9) { data[0xE3] = 0x16; data[0xE7] = 0x10; } // 80DS + TR-DOS marker
				secs.Add(new TrackSector { C = 0, H = 0, R = (byte)r, N = 1, Data = data });
			}
			Assert.AreEqual(VSystemID.Raw.ZXSpectrum, FluxDiskFormatIdentifier.IdentifySystem(DiskFromTrack0(secs)));
		}

		[TestMethod]
		public void Plus3dosSignature_IdentifiesZXSpectrum()
		{
			var data = new byte[512];
			var sig = System.Text.Encoding.ASCII.GetBytes("PLUS3DOS");
			System.Array.Copy(sig, 0, data, 0, sig.Length);
			var secs = new List<TrackSector> { new() { C = 0, H = 0, R = 1, N = 2, Data = data } };
			Assert.AreEqual(VSystemID.Raw.ZXSpectrum, FluxDiskFormatIdentifier.IdentifySystem(DiskFromTrack0(secs)));
		}

		[TestMethod]
		public void CpcSectorIds_IdentifiesAmstradCPC()
		{
			var secs = new List<TrackSector>();
			for (int i = 0; i < 9; i++)
				secs.Add(new TrackSector { C = 0, H = 0, R = (byte)(0x41 + i), N = 2, Data = new byte[512] });
			Assert.AreEqual(VSystemID.Raw.AmstradCPC, FluxDiskFormatIdentifier.IdentifySystem(DiskFromTrack0(secs)));
		}

		[TestMethod]
		public void Plus3BootChecksum_IdentifiesZXSpectrum()
		{
			// a first sector (id 1, 512 bytes) whose bytes sum to 3 (mod 256) is a +3 boot record;
			// no TR-DOS marker, no PLUS3DOS string, and id 1 is not a CPC sector number
			var data = new byte[512];
			data[0] = 3;
			var secs = new List<TrackSector> { new() { C = 0, H = 0, R = 1, N = 2, Data = data } };
			Assert.AreEqual(VSystemID.Raw.ZXSpectrum, FluxDiskFormatIdentifier.IdentifySystem(DiskFromTrack0(secs)));
		}

		[TestMethod]
		public void Inconclusive_ReturnsNull()
		{
			// no decodable IBM/System-34 sectors at all -> defer to the platform chooser
			Assert.IsNull(FluxDiskFormatIdentifier.IdentifySystem(new FluxDisk()));

			// a plain data disk with no ZX/CPC markers and a neutral checksum -> also inconclusive
			var data = new byte[512]; // sums to 0
			var secs = new List<TrackSector> { new() { C = 0, H = 0, R = 1, N = 2, Data = data } };
			Assert.IsNull(FluxDiskFormatIdentifier.IdentifySystem(DiskFromTrack0(secs)));
		}
	}
}
