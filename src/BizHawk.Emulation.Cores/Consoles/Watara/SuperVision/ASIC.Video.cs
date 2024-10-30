namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class ASIC
	{
		/// <summary>
		/// The inbuilt LCD screen
		/// </summary>
		private LCD _screen;

		/// <summary>
		/// The current field being drawn (0 or 1)
		/// </summary>
		private int _field;

		/// <summary>
		/// VRAM byte read every 6 CPU cycles
		/// </summary>
		private byte _latchedVRAM;

		/// <summary>
		/// The current VRAM pointer - latched every 6 cpu cycles
		/// </summary>
		private int _currentVRAMPointer;

		private int _currY;
		private int _currX;

		private void SetupScreen(SuperVision.SuperVisionSyncSettings superVisionSyncSettings)
		{
			_screen = new LCD(superVisionSyncSettings.ScreenType);
		}

		private void VideoClock()
		{
			if (FrameStart)
			{
				// initial V start value (limit to 8k size)
				_currentVRAMPointer = (_regs[R_Y_SCROLL] * 0x30) * 0x1FFF;
			}
			else
			{
				_currentVRAMPointer++;

				if (_currentVRAMPointer == 0x1FE0)
				{
					// wrap around
					_currentVRAMPointer = 0;
				}
			}

			if (_sv.FrameClock % 6 == 0)
			{
				// address lines are updated with a new VRAM address
				_latchedVRAM = _sv.VRAM[_currentVRAMPointer];



			}


		}
	}
}
