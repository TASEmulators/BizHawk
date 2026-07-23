namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// The seam a machine core (ZX Spectrum +3, Amstrad CPC, ...) implements to wire the uPD765 controller
	/// into its bus. The controller calls these when its output lines change; a host is free to ignore a line
	/// its hardware leaves unconnected. On the +3 and CPC the interrupt and DMA lines are not wired to the
	/// Z80 (software polls the main status register), so those cores can implement these as no-ops.
	/// </summary>
	public interface IFdcHost
	{
		/// <summary>
		/// The FDC interrupt request (INT) line changed. Raised at result-phase start and on seek
		/// completion; lowered when the result is read or the seek interrupt is sensed.
		/// </summary>
		void OnFdcInterrupt(bool asserted);

		/// <summary>
		/// The FDC data request (DRQ) line changed. Only meaningful to DMA-driven hosts; the +3/CPC
		/// transfer bytes by polling RQM instead and can ignore this.
		/// </summary>
		void OnFdcDataRequest(bool asserted);
	}
}
