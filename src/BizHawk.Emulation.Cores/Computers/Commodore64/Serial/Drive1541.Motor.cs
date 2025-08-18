﻿namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial
{
	public sealed partial class Drive1541
	{
		private int _tempStep;
		private int _tempPrB1;

		private void ExecuteMotor()
		{
			_tempPrB1 = Via1.PrB | ~Via1.DdrB;
			_tempStep = _tempPrB1 & 0x3;
			_diskDensity = (_tempPrB1 & 0x60) >> 5;
			_motorEnabled = (_tempPrB1 & 0x04) != 0;
			_ledEnabled = (_tempPrB1 & 0x08) != 0;

			// motor track stepping
			if (_tempStep != _motorStep)
			{
				if (_tempStep == ((_motorStep - 1) & 0x3))
				{
					_trackNumber--;
				}
				else if (_tempStep == ((_motorStep + 1) & 0x3))
				{
					_trackNumber++;
				}

				if (_trackNumber < 0)
				{
					_trackNumber = 0;
				}
				else if (_trackNumber > 83)
				{
					_trackNumber = 83;
				}

				_motorStep = _tempStep;
				UpdateMediaData();
			}
		}
	}
}
