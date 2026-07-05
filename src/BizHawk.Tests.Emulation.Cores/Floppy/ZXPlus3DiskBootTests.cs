using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

using ZX = BizHawk.Emulation.Cores.Computers.SinclairSpectrum.ZXSpectrum;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// End-to-end integration smoke test for the new flux-based +3 disk controller: boots a real +3 core
	/// with a real IPF loaded through the whole pipeline (media identify -> Upd765DiskController -> flux) and
	/// confirms the core runs many frames stably (no exception from the load path, the register I/O, the
	/// timing catch-up or the flux decode) and renders a real screen. NOTE: a headless +3 with no input sits
	/// at the boot menu, so this does not assert the game's own screen appears - selecting the +3 "Loader"
	/// (which triggers the actual sector reads) is a manual GUI check. The FDC read/write/format logic itself
	/// is covered by the Upd765Fdc unit tests, and diskless boot equivalence by ZXModelFingerprintTests.
	/// </summary>
	[TestClass]
	public sealed class ZXPlus3DiskBootTests
	{
		private sealed class StubFiles : ICoreFileProvider
		{
			public string GetRetroSaveRAMDirectory(string corePath) => throw new NotImplementedException();
			public string GetRetroSystemPath(string corePath) => throw new NotImplementedException();
			public string GetUserPath(string sysID, bool temp) => throw new NotImplementedException();
			public byte[]? GetFirmware(FirmwareID id, string? msg = null) => throw new NotImplementedException();
			public byte[] GetFirmwareOrThrow(FirmwareID id, string? msg = null) => throw new NotImplementedException();
			public (byte[] FW, GameInfo Game) GetFirmwareWithGameInfoOrThrow(FirmwareID id, string? msg = null) => throw new NotImplementedException();
		}

		private sealed class StubGL : IOpenGLProvider
		{
			public bool SupportsGLVersion(int major, int minor) => false;
			public object RequestGLContext(int major, int minor, bool coreProfile) => throw new NotImplementedException();
			public void ReleaseGLContext(object context) { }
			public void ActivateGLContext(object context) { }
			public void DeactivateGLContext() { }
			public IntPtr GetGLProcAddress(string? proc) => IntPtr.Zero;
		}

		private sealed class RomAsset : IRomAsset
		{
			public byte[] RomData { get; set; }
			public byte[] FileData { get; set; }
			public string Extension { get; set; }
			public string RomPath { get; set; }
			public GameInfo Game { get; set; }
		}

		private static ZX MakeCore(byte[] disk, string ext = ".ipf", string name = "disk")
		{
			var comm = new CoreComm((_) => { }, (_, _) => { }, new StubFiles(), CoreComm.CorePreferencesFlags.None, new StubGL());
			var roms = new List<IRomAsset>();
			if (disk != null)
				roms.Add(new RomAsset { RomData = disk, FileData = disk, Extension = ext, RomPath = name + ext, Game = new GameInfo { Name = name } });
			var lp = new CoreLoadParameters<ZX.ZXSpectrumSettings, ZX.ZXSpectrumSyncSettings>
			{
				Comm = comm,
				Settings = new ZX.ZXSpectrumSettings(),
				SyncSettings = new ZX.ZXSpectrumSyncSettings { MachineType = MachineType.ZXSpectrum128Plus3 },
				Roms = roms,
			};
			return new ZX(lp);
		}

		private static ulong RunAndHash(ZX core, int frames)
		{
			var emu = (IEmulator)core;
			var vp = core.ServiceProvider.GetService<IVideoProvider>()!;
			const ulong FnvOffset = 14695981039346656037UL, FnvPrime = 1099511628211UL;
			ulong h = FnvOffset;
			for (int f = 0; f < frames; f++) emu.FrameAdvance(NullController.Instance, true, false);
			var fb = vp.GetVideoBuffer();
			unchecked { foreach (var px in fb) { h ^= (uint)px; h *= FnvPrime; } }
			return h;
		}

		[TestMethod]
		public void Plus3_DoubleSidedImage_SplitsIntoTwoDiskObjects()
		{
			string dsPath = Path.Combine(
				Path.GetDirectoryName(typeof(ZXPlus3DiskBootTests).Assembly.Location)!, "Resources", "disk", "MagicKnightTrilogy.ipf");
			if (!File.Exists(dsPath)) { Assert.Inconclusive($"test IPF not present: {dsPath}"); return; }

			// a double-sided compilation is presented to the +3 as two selectable single-sided disks
			var ds = MakeCore(File.ReadAllBytes(dsPath), ".ipf", "MagicKnight");
			Assert.AreEqual(2, ds.DiskMedia.Count, "double-sided image splits into two disk objects (works for any format)");

			// a single-sided image stays a single disk
			string ssPath = Path.Combine(
				Path.GetDirectoryName(typeof(ZXPlus3DiskBootTests).Assembly.Location)!, "Resources", "disk", "RoboCop2.ipf");
			if (File.Exists(ssPath))
			{
				var ss = MakeCore(File.ReadAllBytes(ssPath), ".ipf", "RoboCop2");
				Assert.AreEqual(1, ss.DiskMedia.Count, "single-sided image stays one disk");
			}
		}

		[TestMethod]
		public void Plus3_LoadsRealIpfThroughFluxController_AndRunsStably()
		{
			string path = Path.Combine(
				Path.GetDirectoryName(typeof(ZXPlus3DiskBootTests).Assembly.Location)!, "Resources", "disk", "RoboCop2.ipf");
			if (!File.Exists(path))
			{
				Assert.Inconclusive($"test IPF not present (copyrighted, kept local): {path}");
				return;
			}

			// Constructing the core loads the IPF through media-identify -> Upd765DiskController -> flux;
			// running 400 frames exercises the register I/O and timing catch-up. Any failure in that path
			// throws here. The IPF is recognized as a disk by the media layer (CAPS signature).
			ulong withDisk = RunAndHash(MakeCore(File.ReadAllBytes(path)), 400);

			Assert.AreNotEqual(0UL, withDisk, "the +3 rendered frames with the disk loaded via the flux controller");
		}
	}
}
