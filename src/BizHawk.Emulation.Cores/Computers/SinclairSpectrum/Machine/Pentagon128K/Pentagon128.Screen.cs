
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// 128K/+2 ULA
	/// </summary>
	internal class ScreenPentagon128 : ULA
	{
		public ScreenPentagon128(SpectrumBase machine)
			: base(machine)
		{
			// interrupt (Fuse: the /INT pulse is held for 36 T-states on the Pentagon)
			InterruptStartTime = 0;
			InterruptLength = 36;
			// RenderTableOffset slides the border relative to the paper by 2px (1 T-state) per unit. There is a
			// genuine 2px quantization we cannot fully resolve with this alone (the true fix is the Z80 OUT
			// sub-instruction write instant - see the Pentagon plan doc): offset 0 leaves full-screen
			// border+paper checkerboards 2px out of phase but keeps sparse-border scenes clean; offset 1 aligns
			// the checkerboards but leaves a 1-T red sliver at the paper/right-border seam of full-overscan
			// effects on alternate lines. We ship offset 1 (aligned checkerboard) as the more commonly-visible
			// case. Fuse anchors the paper at top_left_pixel 17988 = FirstPaperLine 80 * 224 + FirstPaperTState
			// 68 (already matched). The old 58 was the Sinclair ULA's pipeline fudge (pushed border ~57 T late,
			// tearing full-screen effects) and does not belong here. ContentionOffset/FloatingBusOffset unused.
			RenderTableOffset = 1;
			ContentionOffset = 6;
			FloatingBusOffset = 1;
			// timing: Pentagon Z80 runs at 3.584 MHz (3584000 / 71680 T = exactly 50 Hz), per Fuse's
			// libspectrum timings. (The old 3546900 was the Sinclair 128K clock, copied by mistake.)
			ClockSpeed = 3584000;
			FrameCycleLength = 71680;
			ScanlineTime = 224;
			// symmetric visible border (paper centred). The Pentagon's true overscan is asymmetric (Fuse:
			// left 36T/right 28T, top 64/bottom 48 lines), but showing it raw leaves the paper off-centre and
			// gives an odd buffer aspect; a symmetric 48px/48-line border keeps the image centred and cleanly
			// proportioned. Border timing accuracy (the tear fix) is independent of these display extents.
			BorderLeftTime = 24;
			BorderRightTime = 24;
			FirstPaperLine = 80;
			FirstPaperTState = 68;
			// screen layout.
			// Border4T stays FALSE so the border resolves at 2px (one colour change per T-state), matching the
			// fine diagonal border curves the real machine produces in full-screen border demos. Fuse snaps the
			// border to a 4-T-state/8px column grid (its border_change queue stores a beam column, not a tstate),
			// which hides sub-8px seams but visibly coarsens those diagonal curves - so we do NOT copy that; the
			// hardware (and the reference capture) show 2px border resolution.
			Border4T = false;
			Border4TStage = 1;
			ScreenWidth = 256;
			ScreenHeight = 192;
			BorderTopHeight = 48;
			BorderBottomHeight = 48;
			BorderLeftWidth = 48;
			BorderRightWidth = 48;
			ScanLineWidth = BorderLeftWidth + ScreenWidth + BorderRightWidth;

			// Build the render table as Pentagon128 (not ZXSpectrum128). The machine type only affects the
			// contention pattern and the floating-bus behaviour, not the render timing (which comes from the
			// ULA fields set above). With no Pentagon case, CreateContention falls through to an all-zero
			// pattern (correct: the Pentagon has no contention) and ReadFloatingBus leaves the caller's value
			// untouched (correct: an unmapped port reads 0xFF, there is no video floating bus).
			RenderingTable = new RenderTable(this,
				MachineType.Pentagon128);

			SetupScreenSize();
		}
	}
}
