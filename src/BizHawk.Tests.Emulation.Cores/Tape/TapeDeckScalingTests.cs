using System.Collections.Generic;

using BizHawk.Emulation.Cores.Tapes;

namespace BizHawk.Tests.Emulation.Cores.Tape
{
	/// <summary>
	/// White-box tests for the shared <see cref="TapeDeck"/> clock scaling - the "frequency piece". Tape
	/// formats store pulse timings against a 3.5MHz reference; a core with a different CPU clock passes a
	/// cyclesPerTapeTState ratio so the same tape plays at the correct real rate. This drives the deck through
	/// a stub host and confirms a fixed cycle budget consumes exactly budget/(period*ratio) pulses, so a faster
	/// clock (larger ratio) advances through the tape more slowly per CPU cycle - which is what keeps a 128K
	/// (3.5469MHz) and, later, a CPC (4MHz) loading at the same wall-clock speed as a 3.5MHz 48K.
	/// </summary>
	[TestClass]
	public sealed class TapeDeckScalingTests
	{
		private sealed class StubHost : ITapeHost
		{
			public long Cycles;
			public long TotalExecutedCycles => Cycles;
			public bool IsIn48kMode => false;
			public bool FastLoadAllowed => false;
			public void FeedBeeper(bool earLevel) { }
			public void NotifyPlay() { }
			public void NotifyStop() { }
			public void NotifyRewind() { }
			public void NotifyNextBlock(string blockInfo) { }
			public void NotifyPrevBlock(string blockInfo) { }
			public void NotifyPlayingBlock(string blockInfo) { }
			public void NotifySkipBlock(string blockInfo) { }
			public void NotifyStoppedAuto() { }
			public void NotifyStopCommand() { }
		}

		private const int Period = 2168;          // a pilot pulse, in 3.5MHz T-states
		private const long Budget = 2_168_000;    // exactly 1000 unscaled pilot pulses

		// Builds a deck holding one long block of equal-length pilot pulses and starts it playing at cycle 0.
		private static TapeDeck MakePlayingDeck(double ratio, StubHost host)
		{
			var deck = new TapeDeck(host, ratio);
			var block = new TapeDataBlock { BlockDescription = BlockType.Standard_Speed_Data_Block };
			bool level = false;
			for (int i = 0; i < 100_000; i++)
			{
				block.DataPeriods.Add(Period);
				block.DataLevels.Add(level);
				level = !level;
			}
			deck.DataBlocks.Add(block);
			deck.CurrentDataBlockIndex = 0;

			host.Cycles = 0;
			deck.Play();
			return deck;
		}

		// Consumes exactly floor(Budget / (Period*ratio)) pulses for the given ratio.
		private static int PulsesConsumed(double ratio)
		{
			var host = new StubHost();
			var deck = MakePlayingDeck(ratio, host);
			host.Cycles = Budget;
			deck.GetEarBit(Budget);
			return deck.Position;
		}

		[TestMethod]
		public void Ratio1_0_LeavesPeriodsUnscaled()
		{
			// a 3.5MHz host: the period is compared 1:1 to CPU cycles, so the budget consumes exactly 1000
			Assert.AreEqual((int)(Budget / Period), PulsesConsumed(1.0));
		}

		[TestMethod]
		public void HigherClockRatio_AdvancesTapeMoreSlowlyPerCycle()
		{
			double zx48 = 3_500_000.0 / 3_500_000.0;   // 1.0
			double zx128 = 3_546_900.0 / 3_500_000.0;  // ~1.0134
			double cpc = 4_000_000.0 / 3_500_000.0;    // 8/7 ~1.142857

			int p48 = PulsesConsumed(zx48);
			int p128 = PulsesConsumed(zx128);
			int pcpc = PulsesConsumed(cpc);

			// each ratio consumes exactly budget / round(period*ratio) pulses in the same cycle budget
			Assert.AreEqual(Budget / (int)(Period * zx48), p48);
			Assert.AreEqual(Budget / (int)(Period * zx128), p128);
			Assert.AreEqual(Budget / (int)(Period * cpc), pcpc);

			// a faster clock scales pulses up, so fewer complete in the same number of CPU cycles
			Assert.IsTrue(p48 > p128, $"128K should advance slower per cycle than 48K ({p48} vs {p128})");
			Assert.IsTrue(p128 > pcpc, $"CPC should advance slower per cycle than 128K ({p128} vs {pcpc})");
		}
	}
}
