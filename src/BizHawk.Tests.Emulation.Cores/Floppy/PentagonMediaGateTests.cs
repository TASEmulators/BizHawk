using System;
using System.Collections.Generic;
using System.Text;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

using ZX = BizHawk.Emulation.Cores.Computers.SinclairSpectrum.ZXSpectrum;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// The core routes disk images to the machine that can read them and warns (a modal message) when the
	/// wrong model is selected: TR-DOS .trd/.scl need the Pentagon, every other disk image needs the +3.
	/// These run on a 48K core (whose ROM is embedded), so no external firmware is required.
	/// </summary>
	[TestClass]
	public sealed class PentagonMediaGateTests
	{
		private sealed class StubFiles : ICoreFileProvider
		{
			public string GetRetroSaveRAMDirectory(string corePath) => throw new NotImplementedException();
			public string GetRetroSystemPath(string corePath) => throw new NotImplementedException();
			public string GetUserPath(string sysID, bool temp) => throw new NotImplementedException();
			public byte[]? GetFirmware(FirmwareID id, string? msg = null) => null;
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

		private static string LoadOn48KAndCaptureMessage(byte[] image, string ext)
		{
			var messages = new List<string>();
			var comm = new CoreComm((m) => { messages.Add(m); }, (_, _) => { }, new StubFiles(), CoreComm.CorePreferencesFlags.None, new StubGL());
			var lp = new CoreLoadParameters<ZX.ZXSpectrumSettings, ZX.ZXSpectrumSyncSettings>
			{
				Comm = comm,
				Settings = new ZX.ZXSpectrumSettings(),
				SyncSettings = new ZX.ZXSpectrumSyncSettings { MachineType = MachineType.ZXSpectrum48 },
				Roms = new List<IRomAsset>
				{
					new RomAsset { RomData = image, FileData = image, Extension = ext, RomPath = "d" + ext, Game = new GameInfo { Name = "d" } },
				},
			};
			_ = new ZX(lp); // construction loads media, firing the gate warning
			return string.Join("\n", messages);
		}

		// a minimal valid TR-DOS image: 9 sectors with the disk-info marker/type set
		private static byte[] MakeTrd()
		{
			var d = new byte[9 * 256];
			d[8 * 256 + 0xE3] = 0x16;
			d[8 * 256 + 0xE7] = 0x10;
			return d;
		}

		[TestMethod]
		public void TrDosImageOnNonPentagon_WarnsToSelectPentagon()
		{
			var msg = LoadOn48KAndCaptureMessage(MakeTrd(), ".trd");
			StringAssert.Contains(msg, "Pentagon", "a TR-DOS image on a non-Pentagon model should warn to select the Pentagon");
		}

		[TestMethod]
		public void Plus3ImageOnNonPlus3_WarnsToSelectPlus3()
		{
			// a minimal EDSK header is enough for the media identifier to route it as a +3 disk image
			var edsk = new byte[512];
			var hdr = Encoding.ASCII.GetBytes("EXTENDED CPC DSK File\r\nDisk-Info\r\n");
			Array.Copy(hdr, edsk, hdr.Length);
			var msg = LoadOn48KAndCaptureMessage(edsk, ".dsk");
			StringAssert.Contains(msg, "+3", "a +3 disk image on a non-+3 model should warn to select the +3");
		}
	}
}
