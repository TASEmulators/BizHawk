using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	[PortedCore(CoreNames.VirtualJaguar, "Niels Wagenaar, Carwin Jones, Adam Green, James L. Hammons", "2.1.3", "https://icculus.org/virtualjaguar/", isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class VirtualJaguar : WaterboxCore, IRegionable
	{
		private readonly LibVirtualJaguar _core;

		[CoreConstructor(VSystemID.Raw.JAG)]
		public VirtualJaguar(CoreLoadParameters<object, VirtualJaguarSyncSettings> lp)
			: base(lp.Comm, new Configuration
			{
				DefaultWidth = 326,
				DefaultHeight = 240,
				MaxWidth = 1304,
				MaxHeight = 256,
				MaxSamples = 1024,
				DefaultFpsNumerator = 60,
				DefaultFpsDenominator = 1,
				SystemId = VSystemID.Raw.JAG,
			})
		{
			_syncSettings = lp.SyncSettings ?? new();

			ControllerDefinition = CreateControllerDefinition(_syncSettings.P1Active, _syncSettings.P2Active);
			Region = _syncSettings.NTSC ? DisplayType.NTSC : DisplayType.PAL;
			VsyncNumerator = _syncSettings.NTSC ? 60 : 50;

			_core = PreInit<LibVirtualJaguar>(new WaterboxOptions
			{
				Filename = "virtualjaguar.wbx",
				SbrkHeapSizeKB = 64 * 1024,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 4,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 64 * 1024,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			var bios = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("JAG", "Bios"));
			if (bios.Length != 0x20000)
			{
				throw new MissingFirmwareException("Jaguar Bios must be 131072 bytes!");
			}

			var settings = new LibVirtualJaguar.Settings()
			{
				NTSC = _syncSettings.NTSC,
				UseBIOS = !_syncSettings.SkipBIOS,
				UseFastBlitter = _syncSettings.UseFastBlitter,
			};

			var rom = lp.Roms[0].FileData;
			unsafe
			{
				fixed (byte* rp = rom, bp = bios)
				{
					if (!_core.Init(ref settings, (IntPtr)bp, (IntPtr)rp, rom.Length))
					{
						throw new Exception("Core rejected the rom!");
					}
				}
			}

			PostInit();
		}

		private static readonly IReadOnlyList<string> JaguarButtonsOrdered = new[]
		{
			"Up", "Down", "Left", "Right", "A", "B", "C", "Option", "Pause",
			"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "Asterisk", "Pound",
		};

		private static ControllerDefinition CreateControllerDefinition(bool p1, bool p2)
		{
			var ret = new ControllerDefinition("Jaguar Controller");

			if (p1)
			{
				ret.BoolButtons.AddRange(JaguarButtonsOrdered.Select(s => $"P1 {s}"));
			}

			if (p2)
			{
				ret.BoolButtons.AddRange(JaguarButtonsOrdered.Select(s => $"P2 {s}"));
			}

			ret.BoolButtons.Add("Power");
			return ret.MakeImmutable();
		}

		private LibVirtualJaguar.Buttons GetButtons(IController controller, int n)
		{
			LibVirtualJaguar.Buttons ret = 0;

			if (controller.IsPressed($"P{n} Up"))
				ret |= LibVirtualJaguar.Buttons.Up;
			if (controller.IsPressed($"P{n} Down"))
				ret |= LibVirtualJaguar.Buttons.Down;
			if (controller.IsPressed($"P{n} Left"))
				ret |= LibVirtualJaguar.Buttons.Left;
			if (controller.IsPressed($"P{n} Right"))
				ret |= LibVirtualJaguar.Buttons.Right;
			if (controller.IsPressed($"P{n} 0"))
				ret |= LibVirtualJaguar.Buttons._0;
			if (controller.IsPressed($"P{n} 1"))
				ret |= LibVirtualJaguar.Buttons._1;
			if (controller.IsPressed($"P{n} 2"))
				ret |= LibVirtualJaguar.Buttons._2;
			if (controller.IsPressed($"P{n} 3"))
				ret |= LibVirtualJaguar.Buttons._3;
			if (controller.IsPressed($"P{n} 4"))
				ret |= LibVirtualJaguar.Buttons._4;
			if (controller.IsPressed($"P{n} 5"))
				ret |= LibVirtualJaguar.Buttons._5;
			if (controller.IsPressed($"P{n} 6"))
				ret |= LibVirtualJaguar.Buttons._6;
			if (controller.IsPressed($"P{n} 7"))
				ret |= LibVirtualJaguar.Buttons._7;
			if (controller.IsPressed($"P{n} 8"))
				ret |= LibVirtualJaguar.Buttons._8;
			if (controller.IsPressed($"P{n} 9"))
				ret |= LibVirtualJaguar.Buttons._9;
			if (controller.IsPressed($"P{n} Asterisk"))
				ret |= LibVirtualJaguar.Buttons.Asterisk;
			if (controller.IsPressed($"P{n} Pound"))
				ret |= LibVirtualJaguar.Buttons.Pound;
			if (controller.IsPressed($"P{n} A"))
				ret |= LibVirtualJaguar.Buttons.A;
			if (controller.IsPressed($"P{n} B"))
				ret |= LibVirtualJaguar.Buttons.B;
			if (controller.IsPressed($"P{n} C"))
				ret |= LibVirtualJaguar.Buttons.C;
			if (controller.IsPressed($"P{n} Option"))
				ret |= LibVirtualJaguar.Buttons.Option;
			if (controller.IsPressed($"P{n} Pause"))
				ret |= LibVirtualJaguar.Buttons.Pause;

			return ret;
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			return new LibVirtualJaguar.FrameInfo()
			{
				Player1 = GetButtons(controller, 1),
				Player2 = GetButtons(controller, 2),
				Reset = controller.IsPressed("Power"),
			};
		}

		public DisplayType Region { get; }
	}
}
