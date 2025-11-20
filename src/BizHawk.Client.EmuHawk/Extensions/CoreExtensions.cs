using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.BSNES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Computers.Amiga;

namespace BizHawk.Client.EmuHawk.CoreExtensions
{
	public static class CoreExtensions
	{
		public static TimeSpan EstimatedRealTimeSincePowerOn(this IEmulator core)
		{
			if (core.HasCycleTiming())
			{
				var cycleCore = core.AsCycleTiming();
				return TimeSpan.FromSeconds(cycleCore.CycleCount / cycleCore.ClockRate);
			}
			const decimal attosInSec = 1_000_000_000_000_000_000.0M;
			var frameCount = unchecked((ulong) core.Frame);
			var frameRate = core switch
			{
				MAME mame => decimal.ToDouble(attosInSec / mame.VsyncAttoseconds),
				NullEmulator => NullVideo.DefaultVsyncNum / unchecked((double) NullVideo.DefaultVsyncDen),
				_ => PlatformFrameRates.GetFrameRate(
					core.SystemId,
					pal: core.HasRegions() && core.AsRegionable().Region is DisplayType.PAL),
			};
			return TimeSpan.FromSeconds(frameCount / frameRate);
		}

		public static Bitmap Icon(this IEmulator core)
		{
			var attributes = core.Attributes();

			if (attributes is not PortedCoreAttribute)
			{
				return Properties.Resources.CorpHawkSmall;
			}

			// (select) cores A-Z by value of `CoreAttribute.CoreName`
			return core switch
			{
				BsnesCore or LibsnesCore or SubBsnesCore => Properties.Resources.Bsnes,
				Gameboy => Properties.Resources.Gambatte,
				GPGX => Properties.Resources.GenPlus,
				MAME => Properties.Resources.Mame,
				NDS => Properties.Resources.MelonDS,
				MGBAHawk => Properties.Resources.Mgba,
				QuickNES => Properties.Resources.QuickNes,
				Snes9x => Properties.Resources.Snes9X,
				UAE => Properties.Resources.Amiga,
				_ => null,
			};
		}

		public static string GetSystemDisplayName(this IEmulator emulator) => emulator switch
		{
			NullEmulator => string.Empty,
#if false
			IGameboyCommon gb when gb.IsCGBMode() => EmulatorExtensions.SystemIDToDisplayName(VSystemID.Raw.GBC),
#endif
			_ => EmulatorExtensions.SystemIDToDisplayName(emulator.SystemId),
		};
	}
}
