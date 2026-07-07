using System.Collections.Generic;

using BizHawk.Emulation.Cores.Tapes;

namespace BizHawk.Tests.Emulation.Cores.Tape
{
	/// <summary>
	/// Unit tests for the tape loading-scheme detector. Validating against real dumps showed that most loaders
	/// are encrypted on tape (their in-memory poll-loop opcodes are absent), so detection relies on either
	/// PLAINTEXT first-stage code signatures (relocators / distinctive setup) or - more reliably - the physical
	/// PULSE signature of the turbo data block (bit-0/bit-1 cell timings, flag byte, pilot width/count), which
	/// cannot be encrypted. These tests build synthetic blocks carrying each and confirm the classification.
	/// </summary>
	[TestClass]
	public sealed class TapeProtectionTests
	{
		private static TapeProtectionScheme Detect(params TapeDataBlock[] blocks) => TapeProtection.Detect(blocks);

		// a turbo block carrying a plaintext code signature wrapped in filler
		private static TapeDataBlock CodeBlock(params byte[] core)
		{
			var d = new byte[96];
			for (int i = 0; i < d.Length; i++) d[i] = 0xC9;
			System.Array.Copy(core, 0, d, 24, core.Length);
			return new TapeDataBlock { BlockDescription = BlockType.Turbo_Speed_Data_Block, BlockData = d };
		}

		// a turbo data block with a given flag byte and dominant bit-0/bit-1 pulse pair (the physical signature).
		// BlockData is padded past 64 bytes so the flag-byte scan (which ignores tiny header blocks) sees it.
		private static TapeDataBlock DataBlock(byte flag, int bit0, int bit1, BlockType type = BlockType.Turbo_Speed_Data_Block)
		{
			var data = new byte[128];
			data[0] = flag;
			var b = new TapeDataBlock { BlockDescription = type, BlockData = data };
			for (int i = 0; i < 400; i++) { b.DataPeriods.Add(bit0); b.DataPeriods.Add(bit1); }
			return b;
		}

		private static TapeDataBlock Tone(int width, int count)
		{
			var b = new TapeDataBlock { BlockDescription = BlockType.Pure_Tone };
			for (int i = 0; i < count; i++) b.DataPeriods.Add(width);
			return b;
		}

		private static TapeDataBlock PilotOf(int count)
		{
			var b = new TapeDataBlock { BlockDescription = BlockType.Turbo_Speed_Data_Block, BlockData = new byte[] { 0xFF, 1, 2, 3 } };
			for (int i = 0; i < count; i++) b.DataPeriods.Add(2168);
			return b;
		}

		// --- plaintext code signatures ---

		[TestMethod]
		public void SearchLoader_Mask40Poll()
			=> Assert.AreEqual(TapeProtectionScheme.SearchLoader, Detect(CodeBlock(0xDB, 0xFE, 0xA9, 0xE6, 0x40)));

		[TestMethod]
		public void Bleepload_Ff15SelfMod()
			=> Assert.AreEqual(TapeProtectionScheme.Bleepload, Detect(CodeBlock(0x32, 0x15, 0xFF)));

		[TestMethod]
		public void Microprose_ScreenTable()
			=> Assert.AreEqual(TapeProtectionScheme.Microprose, Detect(CodeBlock(0xDD, 0x21, 0x03, 0xF8, 0xFD, 0x21, 0x00, 0x60)));

		[TestMethod]
		public void Rqfl_AttrClearLdir()
			=> Assert.AreEqual(TapeProtectionScheme.Rqfl, Detect(CodeBlock(0x21, 0x00, 0x58, 0x11, 0x01, 0x58, 0x01, 0xFF, 0x02, 0xED, 0xB0)));

		[TestMethod]
		public void RollerCoaster_OutInAndPoll()
			=> Assert.AreEqual(TapeProtectionScheme.RollerCoaster, Detect(CodeBlock(0xD3, 0xFE, 0xDB, 0xFE, 0xE6, 0x20)));

		[TestMethod]
		public void Zydroload_Im2Handler()
			=> Assert.AreEqual(TapeProtectionScheme.Zydroload, Detect(CodeBlock(0xFB, 0xED, 0x4D, 0xFB, 0xE1, 0xC9)));

		[TestMethod]
		public void ZetaLoad_TableMarker()
			=> Assert.AreEqual(TapeProtectionScheme.ZetaLoad, Detect(CodeBlock(0x10, 0xFF, 0x02, 0x00, 0x80)));

		[TestMethod]
		public void TheEdge_RomEdgeCalls_And_Ff00Relocate()
			=> Assert.AreEqual(TapeProtectionScheme.TheEdge, Detect(CodeBlock(0xCD, 0xE7, 0x05, 0x11, 0x00, 0xFF, 0x01, 0x00, 0x01, 0xED, 0xB0)));

		[TestMethod]
		public void SoftwareProjects_RomEdgeCalls_And_SpFf01()
			=> Assert.AreEqual(TapeProtectionScheme.SoftwareProjects, Detect(CodeBlock(0xCD, 0xE7, 0x05, 0x31, 0x01, 0xFF)));

		[TestMethod]
		public void Novaload_AsciiSignature()
		{
			var d = new byte[64];
			var sig = System.Text.Encoding.ASCII.GetBytes("PSS NOVALOAD");
			System.Array.Copy(sig, 0, d, 8, sig.Length);
			Assert.AreEqual(TapeProtectionScheme.Novaload, Detect(new TapeDataBlock { BlockDescription = BlockType.Turbo_Speed_Data_Block, BlockData = d }));
		}

		// --- flag-byte + bit-cell (physical) signatures ---

		[TestMethod]
		public void Ftl_Flag99()
			=> Assert.AreEqual(TapeProtectionScheme.Ftl, Detect(DataBlock(0x99, 820, 1660)));

		[TestMethod]
		public void PaulOwens_Flag98()
			=> Assert.AreEqual(TapeProtectionScheme.PaulOwens, Detect(DataBlock(0x98, 760, 1520)));

		[TestMethod]
		public void Gremlin1_RomEdge_And_465_930Bits()
			=> Assert.AreEqual(TapeProtectionScheme.Gremlin,
				Detect(CodeBlock(0xCD, 0xE7, 0x05), DataBlock(0xFF, 465, 930)));

		[TestMethod]
		public void Gremlin2_620_1240Bits()
			=> Assert.AreEqual(TapeProtectionScheme.Gremlin2, Detect(DataBlock(0x00, 625, 1250)));

		[TestMethod]
		public void Alkatraz_560_1120Bits()
			=> Assert.AreEqual(TapeProtectionScheme.Alkatraz, Detect(DataBlock(0x9B, 560, 1120)));

		[TestMethod]
		public void DigitalIntegration_PureDataFlagFd()
			=> Assert.AreEqual(TapeProtectionScheme.DigitalIntegration, Detect(DataBlock(0xFD, 480, 960, BlockType.Pure_Data_Block)));

		[TestMethod]
		public void Novaload_Flag07() // Covenant / Swords & Sorcery (PSS) - Turbo block, flag #07 + ~670/1330 bits
			=> Assert.AreEqual(TapeProtectionScheme.Novaload, Detect(DataBlock(0x07, 680, 1340)));

		[TestMethod]
		public void SoftLock_PureData680_1340_NotNovaload() // SoftLock shares 680/1340 but uses Pure Data blocks
		{                                                    // (encrypted flag) vs Novaload's Turbo+flag07
			var b = DataBlock(0x00, 680, 1340, BlockType.Pure_Data_Block);
			Assert.AreEqual(TapeProtectionScheme.SoftLock, Detect(b));
		}

		[TestMethod]
		public void PowerLoad_Flag84Plus21() // Power-Load: flag #84 turbo + flag #21 data (keyed on the pair)
			=> Assert.AreEqual(TapeProtectionScheme.PowerLoad, Detect(DataBlock(0x84, 420, 860), DataBlock(0x21, 420, 860)));

		[TestMethod]
		public void EliteUniLoader_SequentialFlags808182()
			=> Assert.AreEqual(TapeProtectionScheme.EliteUniLoader,
				Detect(DataBlock(0x80, 860, 1720, BlockType.Standard_Speed_Data_Block),
					DataBlock(0x81, 860, 1720, BlockType.Standard_Speed_Data_Block),
					DataBlock(0x82, 860, 1720, BlockType.Standard_Speed_Data_Block)));

		[TestMethod]
		public void SingleFlag84_WithoutFlag21_NotPowerLoad() // the pair is required, so a lone #84 must not match
			=> Assert.AreNotEqual(TapeProtectionScheme.PowerLoad, Detect(DataBlock(0x84, 420, 860)));

		[TestMethod]
		public void Ftl_Flag99_OnStandardBlock() // some FTL rips deliver the flag-99 data at standard speed
			=> Assert.AreEqual(TapeProtectionScheme.Ftl, Detect(DataBlock(0x99, 860, 1720, BlockType.Standard_Speed_Data_Block)));

		[TestMethod]
		public void Players_Flag00_FastBits()
			=> Assert.AreEqual(TapeProtectionScheme.Players, Detect(DataBlock(0x00, 580, 1160)));

		[TestMethod]
		public void Alkatraz_NonZeroFlag_NotPlayers()
		{
			// Alkatraz shares Players' fast bits but always has a non-#00 flag; Players has flag #00
			Assert.AreEqual(TapeProtectionScheme.Alkatraz, Detect(DataBlock(0x9B, 560, 1120)));
			Assert.AreEqual(TapeProtectionScheme.Players, Detect(DataBlock(0x00, 560, 1120)));
		}

		[TestMethod]
		public void Haxpoc620FlagFf_NotMisreadAsGremlin2()
		{
			// Star Wars (Haxpoc) has ~620/1220 bits but flag #FF - Gremlin 2 requires flag #00, so this must NOT be Gremlin2
			Assert.AreNotEqual(TapeProtectionScheme.Gremlin2, Detect(DataBlock(0xFF, 620, 1220)));
		}

		// --- pilot width / count / tone (physical) signatures ---

		[TestMethod]
		public void Micromega_PilotWidth1739()
			=> Assert.AreEqual(TapeProtectionScheme.Micromega, Detect(Tone(1739, 512)));

		[TestMethod]
		public void Speedlock_2100SyncTone()
			=> Assert.AreEqual(TapeProtectionScheme.Speedlock, Detect(Tone(2100, 244)));

		[TestMethod]
		public void Speedlock_ManyBlocks_560_1120() // Speedlock v1-v7 encrypt their data (flag varies) but split
		{                                            // the load into 100+ small blocks - the tell vs Alkatraz/Players
			var blocks = new List<TapeDataBlock>();
			for (int i = 0; i < 70; i++) blocks.Add(new TapeDataBlock { BlockDescription = BlockType.Pause_or_Stop_the_Tape });
			blocks.Add(DataBlock(0x93, 560, 1120)); // encrypted flag byte, Speedlock 560/1120 turbo bits
			Assert.AreEqual(TapeProtectionScheme.Speedlock, TapeProtection.Detect(blocks));
		}

		[TestMethod]
		public void Alkatraz_FewBlocks_560_1120_NotSpeedlock() // same bits, few blocks + non-00 flag => Alkatraz
			=> Assert.AreEqual(TapeProtectionScheme.Alkatraz, Detect(DataBlock(0x9B, 560, 1120)));

		[TestMethod]
		public void Edos_PilotCount8193()
			=> Assert.AreEqual(TapeProtectionScheme.Edos, Detect(PilotOf(8193)));

		[TestMethod]
		public void Moonlighter_PilotCount6912()
			=> Assert.AreEqual(TapeProtectionScheme.Moonlighter, Detect(PilotOf(6912)));

		// --- negative / guard cases ---

		[TestMethod]
		public void StandardPilotCount_NotMisclassified()
			=> Assert.AreEqual(TapeProtectionScheme.None, Detect(PilotOf(3223)));

		[TestMethod]
		public void StandardRom_AllStandardBlocks()
			=> Assert.AreEqual(TapeProtectionScheme.StandardRom,
				Detect(new TapeDataBlock { BlockDescription = BlockType.Standard_Speed_Data_Block, BlockData = new byte[] { 0x00, 0xFF, 0x11 } }));

		[TestMethod]
		public void NoBlocks_ReturnsNone()
			=> Assert.AreEqual(TapeProtectionScheme.None, TapeProtection.Detect(new List<TapeDataBlock>()));
	}
}
