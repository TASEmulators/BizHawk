using System.Collections.Generic;
using System.Text;

using RefZ80 = BizHawk.Emulation.Cores.Components.Z80A.Z80A<BizHawk.Tests.Emulation.Cores.Z80ATests.TestLink>;
using NewZ80 = BizHawk.Emulation.Cores.Components.Z80AOpt.Z80AOpt<BizHawk.Tests.Emulation.Cores.Z80ATests.TestLink>;

// TestBus / TestLink are defined in Z80TestBus.cs

namespace BizHawk.Tests.Emulation.Cores.Z80ATests
{
	/// <summary>
	/// Differential regression harness for the Z80 CPU cores.
	///
	/// The original <c>Z80A</c> is the trusted reference (validated over years against real
	/// ZX Spectrum software, ZEXALL, and FUSE). The forked <c>Z80AOpt</c> — the sandbox we make
	/// performance changes in, and the CPU the ZX Spectrum core now runs — MUST remain
	/// bit-for-bit and cycle-for-cycle identical to the reference.
	///
	/// These tests drive both cores in lockstep over pseudo-random programs and directed
	/// opcode/prefix coverage, and assert that their observable architectural state matches
	/// after EVERY T-state (register file, flags, cycle count, halt/interrupt state), plus
	/// their memory + I/O write streams at the end of each run.
	///
	/// Because the comparison is per-T-state, any change to <c>Z80AOpt</c> that alters WHEN a
	/// register is written, or the total cycle count, or a computed value, fails immediately —
	/// which is exactly the invariant a timing- and accuracy-preserving optimisation must hold.
	///
	/// The comparison is deliberately restricted to OBSERVABLE architectural state, NOT internal
	/// micro-op bookkeeping (the <c>cur_instr</c> queue, pointers, prefix latches). That lets a
	/// valid optimisation change the internal representation while this harness still enforces
	/// identical externally-visible behaviour.
	/// </summary>
	[TestClass]
	public sealed class Z80AEquivalenceTests
	{
		// Programmer-visible register indices to randomise for the initial state.
		// (Deliberately excludes the always-zero ZERO reg and the fixed IRQ/NMI vectors so
		// runs stay "sensible"; every value is applied identically to both cores regardless.)
		private static readonly int[] ArchRegs =
		{
			0, 1,   // PCl PCh
			2, 3,   // SPl SPh
			4, 5,   // A F
			6, 7, 8, 9, 10, 11, // B C D E H L
			15, 16, 17, 18,     // Ixl Ixh Iyl Iyh
			20, 21,             // R I
			24, 25, 26, 27, 28, 29, 30, 31, // shadow AF BC DE HL
		};

		[DataTestMethod]
		[DataRow(0xC0FFEE)]
		[DataRow(0x1234)]
		[DataRow(0xBADF00D)]
		[DataRow(0x0)]
		[DataRow(0x5EED)]
		[DataRow(0xACE)]
		[DataRow(0x2BADBEEF)]
		[DataRow(0x1DEA)]
		public void RandomProgram_CoresAgreeCycleByCycle(int seed)
		{
			const int Cycles = 200_000;
			var rng = new System.Random(seed);

			var busRef = new TestBus();
			var busNew = new TestBus();
			rng.NextBytes(busRef.Mem);
			System.Array.Copy(busRef.Mem, busNew.Mem, busRef.Mem.Length);

			var cpuRef = new RefZ80(new TestLink(busRef));
			var cpuNew = new NewZ80(new TestLink(busNew));
			foreach (var idx in ArchRegs)
			{
				var v = (ushort)rng.Next(0, 256);
				cpuRef.Regs[idx] = v;
				cpuNew.Regs[idx] = v;
			}

			for (long cyc = 0; cyc < Cycles; cyc++)
			{
				// Periodically raise interrupts (identically on both) to exercise the IRQ/NMI paths.
				if ((cyc & 0x3FFF) == 0x3FFF)
				{
					cpuRef.FlagI = cpuNew.FlagI = true;
				}
				if ((cyc & 0xFFFF) == 0xABCD % 0x10000)
				{
					cpuRef.NonMaskableInterrupt = cpuNew.NonMaskableInterrupt = true;
				}

				cpuRef.ExecuteOne();
				cpuNew.ExecuteOne();

				AssertStateEqual(cpuRef, cpuNew, seed, cyc);
			}

			CollectionAssert.AreEqual(busRef.MemWrites, busNew.MemWrites,
				$"[seed 0x{seed:X}] memory write stream diverged");
			CollectionAssert.AreEqual(busRef.PortWrites, busNew.PortWrites,
				$"[seed 0x{seed:X}] port write stream diverged");
			CollectionAssert.AreEqual(busRef.Mem, busNew.Mem,
				$"[seed 0x{seed:X}] final memory diverged");
		}

		/// <summary>
		/// Directed coverage: exercise every lead opcode under every prefix (incl. the undocumented
		/// DDCB/FDCB group), so a table entry the random fuzz happened to miss is still checked.
		/// </summary>
		[TestMethod]
		public void AllOpcodesAndPrefixes_CoresAgree()
		{
			// prefix byte sequences placed before the opcode byte; empty = unprefixed.
			byte[][] prefixes =
			{
				System.Array.Empty<byte>(),
				new byte[] { 0xCB },
				new byte[] { 0xED },
				new byte[] { 0xDD },
				new byte[] { 0xFD },
				new byte[] { 0xDD, 0xCB, 0x05 }, // DDCB, displacement=5, opcode follows
				new byte[] { 0xFD, 0xCB, 0x05 }, // FDCB
			};

			var rng = new System.Random(unchecked((int)0xD15A55));
			foreach (var prefix in prefixes)
			{
				for (int op = 0; op <= 0xFF; op++)
				{
					var busRef = new TestBus();
					var busNew = new TestBus();
					rng.NextBytes(busRef.Mem);           // random operands / surrounding code
					int p = 0;
					foreach (var b in prefix) busRef.Mem[p++] = b;
					busRef.Mem[p] = (byte)op;            // opcode lands after the prefix bytes
					System.Array.Copy(busRef.Mem, busNew.Mem, busRef.Mem.Length);

					var cpuRef = new RefZ80(new TestLink(busRef));
					var cpuNew = new NewZ80(new TestLink(busNew));

					// enough cycles for the longest prefixed instruction to fully retire + a little
					for (long cyc = 0; cyc < 40; cyc++)
					{
						cpuRef.ExecuteOne();
						cpuNew.ExecuteOne();
						AssertStateEqual(cpuRef, cpuNew, op, cyc, prefix);
					}
				}
			}
		}

		private static void AssertStateEqual(RefZ80 a, NewZ80 b, long tag, long cyc, byte[] prefix = null)
		{
			bool ok = a.TotalExecutedCycles == b.TotalExecutedCycles
				&& a.halted == b.halted
				&& a.IFF1 == b.IFF1
				&& a.IFF2 == b.IFF2
				&& a.FlagI == b.FlagI
				&& a.FlagW == b.FlagW
				&& a.EIPending == b.EIPending;

			if (ok)
			{
				for (int i = 0; i < 36; i++)
				{
					if (a.Regs[i] != b.Regs[i]) { ok = false; break; }
				}
			}

			if (ok) return;

			var sb = new StringBuilder();
			sb.AppendLine($"Z80A vs Z80AOpt diverged (tag=0x{tag:X}, prefix=[{FormatBytes(prefix)}], T-state {cyc}):");
			sb.AppendLine($"  TotalExecutedCycles: ref={a.TotalExecutedCycles} new={b.TotalExecutedCycles}");
			sb.AppendLine($"  halted: ref={a.halted} new={b.halted}   IFF1 ref={a.IFF1} new={b.IFF1}   IFF2 ref={a.IFF2} new={b.IFF2}");
			sb.AppendLine($"  FlagI ref={a.FlagI} new={b.FlagI}   FlagW ref={a.FlagW} new={b.FlagW}   EIPending ref={a.EIPending} new={b.EIPending}");
			for (int i = 0; i < 36; i++)
			{
				if (a.Regs[i] != b.Regs[i])
					sb.AppendLine($"  Regs[{i}]: ref=0x{a.Regs[i]:X2} new=0x{b.Regs[i]:X2}");
			}
			Assert.Fail(sb.ToString());
		}

		private static string FormatBytes(byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0) return "";
			var parts = new string[bytes.Length];
			for (int i = 0; i < bytes.Length; i++) parts[i] = $"{bytes[i]:X2}";
			return string.Join(" ", parts);
		}
	}
}
