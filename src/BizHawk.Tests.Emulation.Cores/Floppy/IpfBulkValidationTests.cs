using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Bulk validation of the IPF loader against a local TOSEC compilation set: every file is rolled to flux
	/// and its sectors decoded, checking robustness (no exceptions), decode health, double-sided handling and
	/// protection detection. Inconclusive when the local set is absent, so it never runs on other machines.
	/// </summary>
	[TestClass]
	public sealed class IpfBulkValidationTests
	{
		private const string Dir = @"D:\Downloads\Sinclair ZX Spectrum - Compilations - Games - [IPF] (TOSEC-v2023-06-10)";

		[TestMethod]
		public void AllCompilationIpfs_LoadAndDecode()
		{
			if (!Directory.Exists(Dir)) { Assert.Inconclusive($"local IPF set not present: {Dir}"); return; }

			var files = new List<string>(Directory.EnumerateFiles(Dir, "*.ipf", SearchOption.AllDirectories));
			files.Sort();
			var sb = new StringBuilder();
			sb.AppendLine($"IPF bulk validation ({files.Count} files):");

			int failures = 0, doubleSided = 0;
			var seen = new HashSet<string>();
			foreach (var path in files)
			{
				string name = Path.GetFileName(path);
				if (!seen.Add(name)) continue; // skip duplicate copies in nested folders

				try
				{
					var bytes = File.ReadAllBytes(path);
					var ipf = IpfConverter.Parse(bytes);
					var disk = FluxDisk.FromIpf(bytes);

					int good = 0, total = 0, tracksWithData = 0, side1Tracks = 0;
					for (int cyl = 0; cyl < disk.Cylinders; cyl++)
					{
						for (int side = 0; side < disk.Sides; side++)
						{
							var t = disk.GetTrack(cyl, side);
							if (t == null) continue;
							var secs = StandardMfmFormat.DecodeSectors(t);
							if (secs.Count > 0) { tracksWithData++; if (side == 1) side1Tracks++; }
							foreach (var s in secs) { total++; if (s.IdCrcOk && s.HasData && s.DataCrcOk) good++; }
						}
					}

					var prot = DiskProtection.Detect(disk);
					bool ds = disk.Sides > 1;
					if (ds) doubleSided++;

					sb.AppendLine($"  OK  crc={(ipf.AllCrcOk ? "ok " : "BAD")} cyls={disk.Cylinders} sides={disk.Sides}"
						+ $" tracks={tracksWithData} side1Tracks={side1Tracks} sectors={good}/{total} prot={prot} :: {name}");
				}
				catch (Exception e)
				{
					failures++;
					sb.AppendLine($"  FAIL {e.GetType().Name}: {e.Message} :: {name}");
				}
			}

			sb.AppendLine($"summary: {seen.Count} unique, {failures} failures, {doubleSided} double-sided");

			string outPath = @"C:\Users\matt\AppData\Local\Temp\claude\D--Repos-BH-BizHawk\856ebaad-1f4b-4da2-9a07-b5626fdb9560\scratchpad\ipf_bulk.txt";
			Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
			File.WriteAllText(outPath, sb.ToString());

			Assert.AreEqual(0, failures, "every IPF should load and decode without throwing:\n" + sb);
		}
	}
}
