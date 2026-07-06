using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;
using BizHawk.Emulation.Cores.Tapes;

using ZX = BizHawk.Emulation.Cores.Computers.SinclairSpectrum.ZXSpectrum;

namespace BizHawk.Tests.Emulation.Cores.Tape
{
	/// <summary>
	/// Spec-compliance tests for the TZX reader (`TzxConverter`), guarding the fixes made against the WoS
	/// v1.20 spec: the 0x10 pilot-pulse count, the loop (0x24/0x25) repetition count, the pause "1ms opposite
	/// then low, ending low" rule, block 0x15 not desyncing the stream, and block 0x18 (CSW-in-TZX) actually
	/// reading its payload and producing matching period/level lists. A synthetic TZX is built in code and
	/// loaded through the real core; the parsed block list is read back via reflection on the private machine.
	/// </summary>
	[TestClass]
	public sealed class TzxParsingTests
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

		// Minimal TZX byte-stream builder.
		private sealed class Tzx
		{
			private readonly List<byte> _b = new List<byte>();

			public Tzx()
			{
				_b.AddRange(Encoding.ASCII.GetBytes("ZXTape!"));
				_b.Add(0x1A);
				_b.Add(0x01); // major
				_b.Add(0x14); // minor (v1.20)
			}

			private void W16(int v) { _b.Add((byte)(v & 0xFF)); _b.Add((byte)((v >> 8) & 0xFF)); }
			private void W24(int v) { _b.Add((byte)(v & 0xFF)); _b.Add((byte)((v >> 8) & 0xFF)); _b.Add((byte)((v >> 16) & 0xFF)); }
			private void W32(int v) { W16(v & 0xFFFF); W16((v >> 16) & 0xFFFF); }

			// 0x10 Standard Speed Data Block
			public Tzx Standard(int pauseMs, byte[] data)
			{
				_b.Add(0x10);
				W16(pauseMs);
				W16(data.Length);
				_b.AddRange(data);
				return this;
			}

			public Tzx LoopStart(int reps) { _b.Add(0x24); W16(reps); return this; }
			public Tzx LoopEnd() { _b.Add(0x25); return this; }

			// 0x15 Direct Recording
			public Tzx Direct(int tStatesPerSample, int pauseMs, int usedBitsLast, byte[] samples)
			{
				_b.Add(0x15);
				W16(tStatesPerSample);
				W16(pauseMs);
				_b.Add((byte)usedBitsLast);
				W24(samples.Length);
				_b.AddRange(samples);
				return this;
			}

			// 0x30 Text Description
			public Tzx Text(string s)
			{
				var t = Encoding.ASCII.GetBytes(s);
				_b.Add(0x30);
				_b.Add((byte)t.Length);
				_b.AddRange(t);
				return this;
			}

			// 0x18 CSW Recording (compression type 1 = uncompressed RLE: one byte per pulse)
			public Tzx Csw(int pauseMs, int sampleRate, byte[] pulseBytes)
			{
				_b.Add(0x18);
				W32(10 + pulseBytes.Length); // block length without these 4 bytes
				W16(pauseMs);
				W24(sampleRate);
				_b.Add(0x01); // compression type: RLE
				W32(pulseBytes.Length); // number of stored pulses
				_b.AddRange(pulseBytes);
				return this;
			}

			public Tzx Jump(int value) { _b.Add(0x23); W16(value); return this; }

			public Tzx Call(params int[] offsets)
			{
				_b.Add(0x26);
				W16(offsets.Length);
				foreach (var o in offsets) W16(o);
				return this;
			}

			public Tzx Return() { _b.Add(0x27); return this; }

			public Tzx Select(params int[] offsets)
			{
				_b.Add(0x28);
				W16(1 + offsets.Length * 3); // length: count byte + per selection (WORD offset + BYTE descLen)
				_b.Add((byte)offsets.Length);
				foreach (var o in offsets) { W16(o); _b.Add(0); } // offset + zero-length description
				return this;
			}

			public Tzx Raw(byte[] bytes) { _b.AddRange(bytes); return this; }

			// 0x12 Pure Tone
			public Tzx PureTone(int pulseLength, int count) { _b.Add(0x12); W16(pulseLength); W16(count); return this; }

			// 0x2B Set Signal Level (0 = low, 1 = high)
			public Tzx SetSignalLevel(int level) { _b.Add(0x2B); W32(1); _b.Add((byte)level); return this; }

			public byte[] Build() => _b.ToArray();
		}

		private static TapeDataBlock FindBlock(List<TapeDataBlock> blocks, BlockType type)
		{
			foreach (var b in blocks) if (b.BlockDescription == type) return b;
			return null;
		}

		private static List<string> CollectTexts(List<TapeDataBlock> blocks)
		{
			var list = new List<string>();
			foreach (var b in blocks)
			{
				if (b.BlockID == 0x30 && b.MetaData != null
					&& b.MetaData.TryGetValue(BlockDescriptorTitle.Text_Description, out var v))
				{
					list.Add(v);
				}
			}
			return list;
		}

		// A Generalized Data Block (0x19): no pilot, two data symbols (0 => two 855 pulses, 1 => two 1710
		// pulses), 1 bit per symbol, data stream "10".
		private static byte[] BuildGeneralizedBlock()
		{
			var body = new List<byte>();
			void W16(int v) { body.Add((byte)(v & 0xFF)); body.Add((byte)((v >> 8) & 0xFF)); }
			void W32(int v) { W16(v & 0xFFFF); W16((v >> 16) & 0xFFFF); }

			W16(0);      // pause
			W32(0);      // TOTP (no pilot/sync stream)
			body.Add(0); // NPP
			body.Add(0); // ASP
			W32(2);      // TOTD (2 data symbols)
			body.Add(2); // NPD (max 2 pulses per symbol)
			body.Add(2); // ASD (2 symbols in the alphabet)
			// data symbol table: flags byte + NPD pulse words
			body.Add(0x00); W16(855); W16(855);   // symbol 0
			body.Add(0x00); W16(1710); W16(1710); // symbol 1
			// data stream: 2 symbols x 1 bit, MSb first -> "1","0" = 0b10000000
			body.Add(0x80);

			var block = new List<byte> { 0x19 };
			block.Add((byte)(body.Count & 0xFF));
			block.Add((byte)((body.Count >> 8) & 0xFF));
			block.Add((byte)((body.Count >> 16) & 0xFF));
			block.Add((byte)((body.Count >> 24) & 0xFF));
			block.AddRange(body);
			return block.ToArray();
		}

		private static List<TapeDataBlock> LoadBlocks(byte[] tape, string ext = ".tzx")
		{
			var comm = new CoreComm((_) => { }, (_, _) => { }, new StubFiles(), CoreComm.CorePreferencesFlags.None, new StubGL());
			var lp = new CoreLoadParameters<ZX.ZXSpectrumSettings, ZX.ZXSpectrumSyncSettings>
			{
				Comm = comm,
				Settings = new ZX.ZXSpectrumSettings(),
				SyncSettings = new ZX.ZXSpectrumSyncSettings { MachineType = MachineType.ZXSpectrum48, AutoLoadTape = false },
				Roms = new List<IRomAsset> { new RomAsset { RomData = tape, FileData = tape, Extension = ext, RomPath = "test" + ext, Game = new GameInfo { Name = "test" } } },
			};
			var core = new ZX(lp);
			var machine = (SpectrumBase)typeof(ZX).GetField("_machine", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(core)!;
			return machine.TapeDevice.DataBlocks;
		}

		// A CSW v2 file: "Compressed Square Wave" signature, uncompressed (RLE) pulse bytes.
		private static byte[] BuildCswV2(int sampleRate, byte[] pulses)
		{
			var b = new List<byte>();
			b.AddRange(Encoding.ASCII.GetBytes("Compressed Square Wave")); // 22 bytes
			b.Add(0x1A);                        // 0x16 terminator
			b.Add(0x02); b.Add(0x00);           // 0x17/0x18 major.minor
			void W32(int v) { b.Add((byte)(v & 0xFF)); b.Add((byte)((v >> 8) & 0xFF)); b.Add((byte)((v >> 16) & 0xFF)); b.Add((byte)((v >> 24) & 0xFF)); }
			W32(sampleRate);                    // 0x19 sample rate
			W32(pulses.Length);                 // 0x1D total pulses
			b.Add(0x01);                        // 0x21 compression: RLE (uncompressed)
			b.Add(0x00);                        // 0x22 flags
			b.Add(0x00);                        // 0x23 header extension length
			b.AddRange(new byte[16]);           // 0x24 encoding application (ASCIIZ[16])
			b.AddRange(pulses);                 // 0x34 CSW data
			return b.ToArray();
		}

		// Wraps data as a PZX block: 4-byte tag, u32 size, then the data.
		private static byte[] PzxBlock(string tag, byte[] data)
		{
			var b = new List<byte>();
			b.AddRange(Encoding.ASCII.GetBytes(tag));
			int size = data.Length;
			b.Add((byte)size); b.Add((byte)(size >> 8)); b.Add((byte)(size >> 16)); b.Add((byte)(size >> 24));
			b.AddRange(data);
			return b.ToArray();
		}

		private static byte[] BuildPzx(params byte[][] blocks)
		{
			var all = new List<byte>();
			foreach (var blk in blocks) all.AddRange(blk);
			return all.ToArray();
		}

		private const int PilotPulse = 2168;

		[TestMethod]
		public void StandardBlock_HeaderUsesSpecPilotCount_8063()
		{
			// flag byte 0x00 (< 128) -> header pilot tone of 8063 pulses (spec), not 8064
			var blocks = LoadBlocks(new Tzx().Standard(pauseMs: 0, data: new byte[] { 0x00, 0x03, 0x11, 0x22 }).Build());

			var b = blocks[0];
			Assert.AreEqual(0x10, b.BlockID);
			int leadingPilots = 0;
			while (leadingPilots < b.DataPeriods.Count && b.DataPeriods[leadingPilots] == PilotPulse) leadingPilots++;
			Assert.AreEqual(8063, leadingPilots, "header pilot tone must be 8063 pulses");
		}

		[TestMethod]
		public void StandardBlock_DataUsesSpecPilotCount_3223()
		{
			// flag byte 0xFF (>= 128) -> data pilot tone of 3223 pulses
			var blocks = LoadBlocks(new Tzx().Standard(pauseMs: 0, data: new byte[] { 0xFF, 0x01, 0x02 }).Build());

			int leadingPilots = 0;
			while (leadingPilots < blocks[0].DataPeriods.Count && blocks[0].DataPeriods[leadingPilots] == PilotPulse) leadingPilots++;
			Assert.AreEqual(3223, leadingPilots, "data pilot tone must be 3223 pulses");
		}

		[TestMethod]
		public void Loop_PlaysBodyExactlyNTimes()
		{
			// a loop of 3 around a single standard block => that block should appear 3 times total, not 4
			var blocks = LoadBlocks(new Tzx()
				.LoopStart(3)
				.Standard(pauseMs: 0, data: new byte[] { 0xFF, 0xAA, 0xBB })
				.LoopEnd()
				.Build());

			int bodyCount = 0;
			foreach (var b in blocks) if (b.BlockID == 0x10) bodyCount++;
			Assert.AreEqual(3, bodyCount, "loop body must play 'repetitions' times in total");
		}

		[TestMethod]
		public void Pause_EndsAtLowLevel()
		{
			// a data block with a non-zero pause must end at the low level (spec: pause always ends low)
			var blocks = LoadBlocks(new Tzx().Standard(pauseMs: 100, data: new byte[] { 0xFF, 0x12, 0x34 }).Build());

			var b = blocks[0];
			Assert.AreEqual(b.DataPeriods.Count, b.DataLevels.Count, "period/level lists must be the same length");
			Assert.IsFalse(b.DataLevels[b.DataLevels.Count - 1], "a paused block must end at the LOW level");
		}

		[TestMethod]
		public void DirectRecording_DoesNotDesyncTheStream()
		{
			// 0x15 must advance the position by exactly its data length; a following text block must still parse
			var blocks = LoadBlocks(new Tzx()
				.Direct(tStatesPerSample: 79, pauseMs: 0, usedBitsLast: 8, samples: new byte[] { 0xA5, 0x5A, 0xFF, 0x00, 0x81 })
				.Text("END")
				.Build());

			bool foundText = false;
			foreach (var b in blocks)
			{
				if (b.BlockID == 0x30 && b.MetaData != null
					&& b.MetaData.TryGetValue(BlockDescriptorTitle.Text_Description, out var v) && v == "END")
				{
					foundText = true;
				}
			}
			Assert.IsTrue(foundText, "the text block after a Direct Recording block must parse (no desync)");
		}

		[TestMethod]
		public void Jump_SkipsBlocks()
		{
			// the jump block (index 1) jumps +2, so the "SKIP" text (index 2) is not played
			var blocks = LoadBlocks(new Tzx()
				.Text("A")
				.Jump(2)
				.Text("SKIP")
				.Text("B")
				.Build());

			var texts = CollectTexts(blocks);
			CollectionAssert.Contains(texts, "A");
			CollectionAssert.Contains(texts, "B");
			CollectionAssert.DoesNotContain(texts, "SKIP");
		}

		[TestMethod]
		public void CallReturn_PlaysSubroutineThenReturns()
		{
			// jump over the subroutine, play MAIN, call the sub (negative offset), return to AFTER
			var blocks = LoadBlocks(new Tzx()
				.Jump(3)       // 0: skip the subroutine (blocks 1,2), go to MAIN (block 3)
				.Text("SUB")   // 1
				.Return()      // 2
				.Text("MAIN")  // 3
				.Call(-3)      // 4: call block 4 + (-3) = 1 (SUB)
				.Text("AFTER") // 5
				.Build());

			CollectionAssert.AreEqual(new[] { "MAIN", "SUB", "AFTER" }, CollectTexts(blocks));
		}

		[TestMethod]
		public void Select_DefaultsToFirstOption()
		{
			// the select block defaults to its first option (+2 => CHOSEN), skipping SKIP (+1)
			var blocks = LoadBlocks(new Tzx()
				.Select(2, 1)
				.Text("SKIP")
				.Text("CHOSEN")
				.Build());

			var texts = CollectTexts(blocks);
			CollectionAssert.Contains(texts, "CHOSEN");
			CollectionAssert.DoesNotContain(texts, "SKIP");
		}

		[TestMethod]
		public void Generalized_DecodesSymbolsToPulses()
		{
			var blocks = LoadBlocks(new Tzx().Raw(BuildGeneralizedBlock()).Build());

			TapeDataBlock gen = null;
			foreach (var b in blocks) if (b.BlockID == 0x19) gen = b;
			Assert.IsNotNull(gen, "the Generalized Data Block must be present");
			Assert.AreEqual(gen.DataPeriods.Count, gen.DataLevels.Count, "period/level lists must match");
			// data stream "10": symbol 1 (two 1710 pulses) then symbol 0 (two 855 pulses)
			CollectionAssert.AreEqual(new[] { 1710, 1710, 855, 855 }, gen.DataPeriods.ToArray());
		}

		[TestMethod]
		public void SetSignalLevel_SetsCurrentLevel()
		{
			// 0x2B sets the current level (0=low, 1=high). 'signal' holds the current level and the next
			// block's first pulse edges away from it, so level=high => first pure-tone pulse is low, and
			// level=low => first pure-tone pulse is high. (The old code inverted the level.)
			var afterHigh = FindBlock(LoadBlocks(new Tzx().SetSignalLevel(1).PureTone(2168, 1).Build()), BlockType.Pure_Tone);
			var afterLow = FindBlock(LoadBlocks(new Tzx().SetSignalLevel(0).PureTone(2168, 1).Build()), BlockType.Pure_Tone);

			Assert.IsNotNull(afterHigh);
			Assert.IsNotNull(afterLow);
			Assert.IsFalse(afterHigh.DataLevels[0], "after level=high the first pulse edges low");
			Assert.IsTrue(afterLow.DataLevels[0], "after level=low the first pulse edges high");
		}

		[TestMethod]
		public void Pzx_Data_UsesSeparateP0AndP1PulseSequences()
		{
			// p0=1 (s0=[111]), p1=2 (s1=[222,333]); 2 data bits "01" (MSb first) -> s0 then s1.
			// The old converter read s0 with the p1 count, producing the wrong pulses when p0 != p1.
			var d = new List<byte>();
			void W16(int v) { d.Add((byte)(v & 0xFF)); d.Add((byte)((v >> 8) & 0xFF)); }
			void W32(int v) { W16(v & 0xFFFF); W16((v >> 16) & 0xFFFF); }
			W32(2);           // count = 2 bits (init pulse level low)
			W16(0);           // tail
			d.Add(1);         // p0
			d.Add(2);         // p1
			W16(111);         // s0[0]
			W16(222);         // s1[0]
			W16(333);         // s1[1]
			d.Add(0x40);      // data: bits (MSb first) = 0,1

			var blocks = LoadBlocks(BuildPzx(
				PzxBlock("PZXT", new byte[] { 0x01, 0x00 }),
				PzxBlock("DATA", d.ToArray())), ".pzx");

			TapeDataBlock dat = null;
			foreach (var b in blocks) if (b.BlockDescription == BlockType.DATA) dat = b;
			Assert.IsNotNull(dat, "the DATA block must be present");
			CollectionAssert.AreEqual(new[] { 111, 222, 333 }, dat.DataPeriods.ToArray());
			CollectionAssert.AreEqual(new byte[] { 0x40 }, dat.BlockData); // retained for flash loading
		}

		[TestMethod]
		public void Pzx_Puls_ExtendedDurationNotTruncated()
		{
			// count=1 (0x8001), duration1 with bit 15 set (0x8001) + duration2 (0x2345) => 0x12345 = 74565.
			// The old converter shifted a ushort left 16 and lost the high bits.
			var puls = new byte[] { 0x01, 0x80, 0x01, 0x80, 0x45, 0x23 };

			var blocks = LoadBlocks(BuildPzx(
				PzxBlock("PZXT", new byte[] { 0x01, 0x00 }),
				PzxBlock("PULS", puls)), ".pzx");

			TapeDataBlock pulsBlk = null;
			foreach (var b in blocks) if (b.BlockDescription == BlockType.PULS) pulsBlk = b;
			Assert.IsNotNull(pulsBlk, "the PULS block must be present");
			CollectionAssert.Contains(pulsBlk.DataPeriods, 74565);
		}

		[TestMethod]
		public void CswV2_Uncompressed_DecodesWithoutOverrun()
		{
			// a CSW v2 file (its decode buffer is padded by one byte, which previously misfired the extended-
			// pulse path and read past the end). Must decode cleanly into matching period/level lists.
			var blocks = LoadBlocks(BuildCswV2(sampleRate: 44100, pulses: new byte[] { 10, 20, 30, 40, 50 }), ".csw");

			var b = blocks[0];
			Assert.AreEqual(0x18, b.BlockID);
			Assert.AreEqual(b.DataPeriods.Count, b.DataLevels.Count);
			Assert.IsTrue(b.DataPeriods.Count >= 5, "the 5 pulses (plus a closing period) must be decoded");
		}

		[TestMethod]
		public void Csw_ReadsPayload_AndProducesMatchingPeriodsAndLevels()
		{
			// CSW-in-TZX must read its payload and generate a period + a level for every pulse
			var pulses = new byte[] { 10, 20, 30, 40, 50 };
			var blocks = LoadBlocks(new Tzx().Csw(pauseMs: 0, sampleRate: 44100, pulseBytes: pulses).Build());

			TapeDataBlock csw = null;
			foreach (var b in blocks) if (b.BlockID == 0x18) csw = b;
			Assert.IsNotNull(csw, "the CSW block must be present");
			Assert.IsTrue(csw.DataPeriods.Count > pulses.Length, "CSW pulses (plus the closing period) must be decoded");
			Assert.AreEqual(csw.DataPeriods.Count, csw.DataLevels.Count, "CSW period/level lists must be the same length");
			// with the little-endian 44100 rate, pilot-sized pulses are in a sane T-state range (not garbage)
			Assert.IsTrue(csw.DataPeriods[0] > 0 && csw.DataPeriods[0] < 100000, "first CSW period must be a sane T-state count");
		}
	}
}
