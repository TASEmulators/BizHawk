using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

// ReSharper disable StyleCop.SA1401
namespace BizHawk.Client.EmuHawk
{
	public static class GlobalWin
	{
		public static MainForm _mainForm { get; set; }

		public static IEmulator Emulator => _mainForm.Emulator;

		public static Sound Sound;

		public static Config Config { get; set; }
	}
}
