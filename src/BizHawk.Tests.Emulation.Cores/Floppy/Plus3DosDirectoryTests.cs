using System.Collections.Generic;

using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;
using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>Reads a synthesised +3DOS directory off a flux disk and confirms the file list, sizes and
	/// attributes come back correctly.</summary>
	[TestClass]
	public sealed class Plus3DosDirectoryTests
	{
		private static void WriteEntry(byte[] dir, int off, int user, string name, string ext, int records, bool readOnly = false)
		{
			dir[off] = (byte)user;
			for (int i = 0; i < 8; i++) dir[off + 1 + i] = (byte)(i < name.Length ? name[i] : ' ');
			for (int i = 0; i < 3; i++)
			{
				byte c = (byte)(i < ext.Length ? ext[i] : ' ');
				if (i == 0 && readOnly) c |= 0x80; // read-only attribute = high bit of first ext byte
				dir[off + 9 + i] = c;
			}
			dir[off + 12] = 0;              // extent low
			dir[off + 15] = (byte)records; // records in this extent
		}

		[TestMethod]
		public void Read_ListsFilesFromSynthDirectory()
		{
			// build a directory (first 512 bytes of logical sector 0) with three files, rest empty (0xE5)
			var dir = new byte[512];
			for (int i = 0; i < dir.Length; i++) dir[i] = 0xE5;
			WriteEntry(dir, 0, user: 0, name: "GAME", ext: "BAS", records: 10);
			WriteEntry(dir, 32, user: 0, name: "LOADER", ext: "BIN", records: 20, readOnly: true);
			WriteEntry(dir, 64, user: 0, name: "DISK", ext: "", records: 3);

			// a standard 40-track / 9-sector / 512-byte +3 DATA track 0; logical sector 0 = R=1 holds the dir
			var sectors = new List<TrackSector>();
			for (int r = 1; r <= 9; r++)
			{
				var data = new byte[512];
				if (r == 1) System.Array.Copy(dir, data, 512);
				else for (int i = 0; i < 512; i++) data[i] = 0xE5;
				sectors.Add(new TrackSector { C = 0, H = 0, R = (byte)r, N = 2, Data = data });
			}
			var disk = new FluxDisk();
			disk.SetTrack(0, 0, StandardMfmFormat.BuildStandardTrack(sectors));

			var files = Plus3DosDirectory.Read(disk);
			Assert.AreEqual(3, files.Count, "three files listed");

			var game = files.Find(f => f.Name == "GAME.BAS");
			Assert.IsNotNull(game, "GAME.BAS present");
			Assert.AreEqual(10 * 128, game.SizeBytes, "GAME.BAS size = records * 128");

			var loader = files.Find(f => f.Name == "LOADER.BIN");
			Assert.IsNotNull(loader);
			Assert.IsTrue(loader.ReadOnly, "LOADER.BIN read-only attribute decoded");

			Assert.IsNotNull(files.Find(f => f.Name == "DISK"), "extension-less name handled");
		}

		[TestMethod]
		public void Read_EmptyOrNonFilesystemDisk_ReturnsNothing()
		{
			// a track full of 0xE5 filler (like a formatted-but-empty / non-+3DOS disk) lists no files
			var sectors = new List<TrackSector>();
			for (int r = 1; r <= 9; r++)
			{
				var data = new byte[512];
				for (int i = 0; i < 512; i++) data[i] = 0xE5;
				sectors.Add(new TrackSector { C = 0, H = 0, R = (byte)r, N = 2, Data = data });
			}
			var disk = new FluxDisk();
			disk.SetTrack(0, 0, StandardMfmFormat.BuildStandardTrack(sectors));

			Assert.AreEqual(0, Plus3DosDirectory.Read(disk).Count, "no files on an all-filler disk");
		}
	}
}
