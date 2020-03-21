using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Consoles.SNK
{
	[Core("NeoPop", "Thomas Klausner, Mednafen Team", true, true, "0.9.44.1",
		"https://mednafen.github.io/releases/", false)]
	public class NeoGeoPort : WaterboxCore,
		ISaveRam, // NGP provides its own saveram interface
		ISettable<object, NeoGeoPort.SyncSettings>
	{
		internal LibNeoGeoPort _neopop;

		[CoreConstructor("NGP")]
		public NeoGeoPort(CoreComm comm, byte[] rom, SyncSettings syncSettings, bool deterministic)
			: this(comm, rom, syncSettings, deterministic, PeRunner.CanonicalStart)
		{
		}

		internal NeoGeoPort(CoreComm comm, byte[] rom, SyncSettings syncSettings, bool deterministic, ulong startAddress)
			:base(comm, new Configuration
			{
				DefaultFpsNumerator = 6144000,
				DefaultFpsDenominator = 515 * 198,
				DefaultWidth = 160,
				DefaultHeight = 152,
				MaxWidth = 160,
				MaxHeight = 152,
				MaxSamples = 8192,
				SystemId = "NGP"
			})
		{
			if (rom.Length > 4 * 1024 * 1024)
				throw new InvalidOperationException("ROM too big!");

			_syncSettings = syncSettings ?? new SyncSettings();

			_neopop = PreInit<LibNeoGeoPort>(new PeRunnerOptions
			{
				Filename = "ngp.wbx",
				SbrkHeapSizeKB = 256,
				SealedHeapSizeKB = 5 * 1024, // must be a bit larger than the ROM size
				InvisibleHeapSizeKB = 4,
				PlainHeapSizeKB = 5 * 1024, // must be a bit larger than the ROM size
				StartAddress = startAddress
			});

			if (!_neopop.LoadSystem(rom, rom.Length, _syncSettings.Language))
				throw new InvalidOperationException("Core rejected the rom");

			PostInit();

			DeterministicEmulation = deterministic || !_syncSettings.UseRealTime;
			InitializeRtc(_syncSettings.InitialTime);
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			if (controller.IsPressed("Power"))
				_neopop.HardReset();

			return new LibNeoGeoPort.FrameInfo
			{
				FrontendTime = GetRtcTime(!DeterministicEmulation),
				Buttons = GetButtons(controller),
				SkipRendering = render ? 0 : 1,
			};
		}

		#region Controller

		private static int GetButtons(IController c)
		{
			var ret = 0;
			var val = 1;
			foreach (var s in CoreButtons)
			{
				if (c.IsPressed(s))
					ret |= val;
				val <<= 1;
			}
			return ret;
		}

		private static readonly string[] CoreButtons =
		{
			"Up", "Down", "Left", "Right", "A", "B", "Option"
		};

		private static readonly Dictionary<string, int> ButtonOrdinals = new Dictionary<string, int>
		{
			["Up"] = 1,
			["Down"] = 2,
			["Left"] = 3,
			["Right"] = 4,
			["B"] = 9,
			["A"] = 10,
			["R"] = 11,
			["L"] = 12,
			["Option"] = 13
		};

		private static readonly ControllerDefinition NeoGeoPortableController = new ControllerDefinition
		{
			Name = "NeoGeo Portable Controller",
			BoolButtons = CoreButtons
				.OrderBy(b => ButtonOrdinals[b])
				.Concat(new[] { "Power" })
				.ToList()
		};

		public override ControllerDefinition ControllerDefinition => NeoGeoPortableController;

		#endregion

		#region ISettable

		private SyncSettings _syncSettings;

		public class SyncSettings
		{
			[DisplayName("Language")]
			[Description("Language of the system.  Only affects some games.")]
			[DefaultValue(LibNeoGeoPort.Language.Japanese)]
			public LibNeoGeoPort.Language Language { get; set; }

			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.  Only relevant when UseRealTime is false.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			public DateTime InitialTime { get; set; }

			[DisplayName("Use RealTime")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time.  Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public object GetSettings()
		{
			return null;
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(object o)
		{
			return false;
		}

		public bool PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret;
		}

		#endregion

		#region ISaveram

		public new bool SaveRamModified => _neopop.HasSaveRam();

		public new byte[] CloneSaveRam()
		{
			byte[] ret = null;
			_neopop.GetSaveRam((data, size) =>
			{
				ret = new byte[size];
				Marshal.Copy(data, ret, 0, size);
			});
			return ret;
		}

		public new void StoreSaveRam(byte[] data)
		{
			if (!_neopop.PutSaveRam(data, data.Length))
				throw new InvalidOperationException("Core rejected the saveram");
		}

		#endregion
	}
}
