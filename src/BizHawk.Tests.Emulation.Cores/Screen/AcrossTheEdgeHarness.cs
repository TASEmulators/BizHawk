using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

using ZX = BizHawk.Emulation.Cores.Computers.SinclairSpectrum.ZXSpectrum;

namespace BizHawk.Tests.Emulation.Cores.Screen
{
	/// <summary>
	/// THROWAWAY investigation harness (not a real assertion test): boots the Pentagon "Across the Edge" demo
	/// TRD headless and dumps raw framebuffers to the scratchpad so we can inspect the right-border artifact
	/// (a red column at the paper/right-border seam). Skips (Inconclusive) if the local demo file is absent.
	/// Keep until the border-timing investigation is closed, then delete along with the whole Screen folder.
	/// </summary>
	[TestClass]
	public sealed class AcrossTheEdgeHarness
	{
		private const string DemoPath = @"D:\downloads\AcrossTheEdge(fix3).trd";
		private const string OutDir = @"C:\Users\matt\AppData\Local\Temp\claude\D--Repos-BH-BizHawk\856ebaad-1f4b-4da2-9a07-b5626fdb9560\scratchpad\edge";

		private const string FirmwareDir = @"D:\Repos\BH\BizHawk\output\Firmware";

		private sealed class StubFiles : ICoreFileProvider
		{
			public string GetRetroSaveRAMDirectory(string corePath) => throw new NotImplementedException();
			public string GetRetroSystemPath(string corePath) => throw new NotImplementedException();
			public string GetUserPath(string sysID, bool temp) => throw new NotImplementedException();
			public byte[]? GetFirmware(FirmwareID id, string? msg = null)
			{
				string file = id.Firmware switch
				{
					"PentagonROM" => "pentagon.rom",
					"TRDOSROM" => "trdos.rom",
					_ => null,
				};
				return file != null && File.Exists(Path.Combine(FirmwareDir, file))
					? File.ReadAllBytes(Path.Combine(FirmwareDir, file)) : null;
			}
			public byte[] GetFirmwareOrThrow(FirmwareID id, string? msg = null) => GetFirmware(id, msg) ?? throw new Exception("missing " + id.Firmware);
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

		// A scripted controller: presses each key over a [start,end) frame window. Frame counter ticks once
		// per FrameAdvance (call Advance() after each). The demo TRD has no default BOOT block, so we rename
		// its ACROSS boot file to BOOT, enter TR-DOS from the 128 menu (Down x4 + Return), then RUN it (R + Return).
		private sealed class ScriptController : IController
		{
			private readonly List<(int Start, int End, string Key)> _script;
			public int Frame;
			public ScriptController(ControllerDefinition def, List<(int, int, string)> script) { Definition = def; _script = script; }
			public ControllerDefinition Definition { get; }
			public bool IsPressed(string button)
			{
				foreach (var (s, e, k) in _script) if (k == button && Frame >= s && Frame < e) return true;
				return false;
			}
			public int AxisValue(string name) => 0;
			public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Array.Empty<(string, int)>();
			public void SetHapticChannelStrength(string name, int strength) { }
		}

		private static byte[] PatchBootName(byte[] trd)
		{
			var copy = (byte[])trd.Clone();
			byte[] boot = { (byte)'b', (byte)'o', (byte)'o', (byte)'t', 0x20, 0x20, 0x20, 0x20 };
			Array.Copy(boot, 0, copy, 0, 8); // entry 0 name field = "boot    " (TR-DOS RUN loads the lowercase "boot")
			return copy;
		}

		private static ZX MakeCore(byte[] disk, string ext, string name)
		{
			var comm = new CoreComm((_) => { }, (_, _) => { }, new StubFiles(), CoreComm.CorePreferencesFlags.None, new StubGL());
			var roms = new List<IRomAsset>
			{
				new RomAsset { RomData = disk, FileData = disk, Extension = ext, RomPath = name + ext, Game = new GameInfo { Name = name } },
			};
			var lp = new CoreLoadParameters<ZX.ZXSpectrumSettings, ZX.ZXSpectrumSyncSettings>
			{
				Comm = comm,
				Settings = new ZX.ZXSpectrumSettings(),
				SyncSettings = new ZX.ZXSpectrumSyncSettings { MachineType = MachineType.Pentagon128, BorderType = ZX.BorderType.Full },
				Roms = roms,
			};
			return new ZX(lp);
		}

		private static void WriteBmp(string path, int[] fb, int w, int h)
		{
			// 24-bit bottom-up BMP; fb is 0xAARRGGBB
			int rowPad = (4 - (w * 3) % 4) % 4;
			int imgSize = (w * 3 + rowPad) * h;
			using var fs = new FileStream(path, FileMode.Create);
			using var bw = new BinaryWriter(fs);
			bw.Write((byte)'B'); bw.Write((byte)'M');
			bw.Write(54 + imgSize); bw.Write(0); bw.Write(54);
			bw.Write(40); bw.Write(w); bw.Write(h);
			bw.Write((short)1); bw.Write((short)24); bw.Write(0); bw.Write(imgSize);
			bw.Write(2835); bw.Write(2835); bw.Write(0); bw.Write(0);
			for (int y = h - 1; y >= 0; y--)
			{
				for (int x = 0; x < w; x++)
				{
					int px = fb[y * w + x];
					bw.Write((byte)(px & 0xFF));         // B
					bw.Write((byte)((px >> 8) & 0xFF));  // G
					bw.Write((byte)((px >> 16) & 0xFF)); // R
				}
				for (int p = 0; p < rowPad; p++) bw.Write((byte)0);
			}
		}

		[TestMethod]
		public void DumpFrames()
		{
			if (!File.Exists(DemoPath)) { Assert.Inconclusive($"demo not present: {DemoPath}"); return; }
			Directory.CreateDirectory(OutDir);

			var core = MakeCore(PatchBootName(File.ReadAllBytes(DemoPath)), ".trd", "AcrossTheEdge");
			var emu = (IEmulator)core;
			var vp = core.ServiceProvider.GetService<IVideoProvider>()!;

			// Boot script: settle to menu, Down x4 to TR-DOS, Return to enter, wait, then R + Return to RUN boot.
			var script = new List<(int, int, string)>
			{
				(80, 86, "Key Down Cursor"), (100, 106, "Key Down Cursor"), (120, 126, "Key Down Cursor"), (140, 146, "Key Down Cursor"),
				(170, 178, "Key Return"),
				(400, 415, "Key R"),
				(440, 455, "Key Return"),
			};
			var ctrl = new ScriptController(core.ControllerDefinition, script);

			int maxFrame = 5000;
			// "first right-border pixel anomaly": in a region where the border is a solid colour to the right
			// (x306==x308==x310), the first border pixel-pair (x304) should match it. Count rows where it does
			// not - that isolates the paper/border-seam glitch from legitimate wave colour transitions.
			long anomaly = 0;
			int anomalyFrames = 0;
			for (int f = 1; f <= maxFrame; f++)
			{
				ctrl.Frame = f;
				emu.FrameAdvance(ctrl, true, false);
				if (f >= 3000)
				{
					var fb = vp.GetVideoBuffer();
					int w = vp.BufferWidth, h = vp.BufferHeight;
					int frameCount = 0;
					for (int y = 0; y < h; y++)
					{
						int a = fb[y * w + 304], b = fb[y * w + 306], c = fb[y * w + 308], d = fb[y * w + 310];
						if (b == c && c == d && a != b) frameCount++;
					}
					anomaly += frameCount;
					if (frameCount > 0) anomalyFrames++;
					if (f == 3640) WriteBmp(Path.Combine(OutDir, $"wave.bmp"), fb, w, h);
					if (f == 3641) WriteBmp(Path.Combine(OutDir, $"wave2.bmp"), fb, w, h);
				}
				if (f == 2600) WriteBmp(Path.Combine(OutDir, $"cb.bmp"), vp.GetVideoBuffer(), vp.BufferWidth, vp.BufferHeight);
				if (f == 2601) WriteBmp(Path.Combine(OutDir, $"cb2.bmp"), vp.GetVideoBuffer(), vp.BufferWidth, vp.BufferHeight);
			}
			File.WriteAllText(Path.Combine(OutDir, "anomaly.txt"),
				$"RenderTableOffset sweep result\ntotal first-border anomaly rows (frames 3000-5000): {anomaly}\nframes with any anomaly: {anomalyFrames}\n");
		}
	}
}
