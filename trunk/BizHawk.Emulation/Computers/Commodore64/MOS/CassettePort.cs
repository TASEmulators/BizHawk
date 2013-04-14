using System;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public class CassettePort
	{
		public Func<bool> DeviceReadLevel;
		public Func<bool> DeviceReadMotor;
		public Action<bool> DeviceWriteButton;
		public Action<bool> DeviceWriteLevel;
		public Func<bool> SystemReadButton;
		public Func<bool> SystemReadLevel;
		public Action<bool> SystemWriteLevel;
		public Action<bool> SystemWriteMotor;

		// Connect() needs to set System functions above

		public void HardReset()
		{
		}
	}
}
