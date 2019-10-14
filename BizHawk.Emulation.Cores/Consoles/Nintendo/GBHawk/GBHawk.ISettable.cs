using System;
using System.ComponentModel;

using Newtonsoft.Json;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk : IEmulator, IStatable, ISettable<GBHawk.GBSettings, GBHawk.GBSyncSettings>
	{
		public GBSettings GetSettings()
		{
			return _settings.Clone();
		}

		public GBSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(GBSettings o)
		{
			_settings = o;
			return false;
		}

		public bool PutSyncSettings(GBSyncSettings o)
		{
			bool ret = GBSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret;
		}

		private GBSettings _settings = new GBSettings();
		public GBSyncSettings _syncSettings = new GBSyncSettings();

		public class GBSettings
		{
			public enum PaletteType
			{
				BW,
				Gr
			}

			[DisplayName("Color Mode")]
			[Description("Pick Between Green scale and Grey scale colors")]
			[DefaultValue(PaletteType.BW)]
			public PaletteType Palette { get; set; }


			public GBSettings Clone()
			{
				return (GBSettings)MemberwiseClone();
			}
		}

		public class GBSyncSettings
		{
			[JsonIgnore]
			public string Port1 = GBHawkControllerDeck.DefaultControllerName;

			public enum ControllerType
			{
				Default,
				Tilt
			}

			[JsonIgnore]
			private ControllerType _GBController;

			[DisplayName("Controller")]
			[Description("Select Controller Type")]
			[DefaultValue(ControllerType.Default)]
			public ControllerType GBController
			{
				get { return _GBController; }
				set
				{
					if (value == ControllerType.Default) { Port1 = GBHawkControllerDeck.DefaultControllerName; }
					else { Port1 = "Gameboy Controller + Tilt"; }

					_GBController = value;
				}
			}

			public enum ConsoleModeType
			{
				Auto,
				GB,
				GBC
			}

			[DisplayName("Console Mode")]
			[Description("Pick which console to run, 'Auto' chooses from ROM extension, 'GB' and 'GBC' chooses the respective system")]
			[DefaultValue(ConsoleModeType.Auto)]
			public ConsoleModeType ConsoleMode { get; set; }

			[DisplayName("CGB in GBA")]
			[Description("Emulate GBA hardware running a CGB game, instead of CGB hardware.  Relevant only for titles that detect the presense of a GBA, such as Shantae.")]
			[DefaultValue(false)]
			public bool GBACGB { get; set; }

			[DisplayName("RTC Initial Time")]
			[Description("Set the initial RTC time in terms of elapsed seconds.")]
			[DefaultValue(0)]
			public int RTCInitialTime
			{
				get { return _RTCInitialTime; }
				set { _RTCInitialTime = Math.Max(0, Math.Min(1024 * 24 * 60 * 60, value)); }
			}

			[DisplayName("RTC Offset")]
			[Description("Set error in RTC clocking (-127 to 127)")]
			[DefaultValue(0)]
			public int RTCOffset
			{
				get { return _RTCOffset; }
				set { _RTCOffset = Math.Max(-127, Math.Min(127, value)); }
			}

			[DisplayName("Timer Div Initial Time")]
			[Description("Don't change from 0 unless it's hardware accurate. GBA GBC mode is known to be 8.")]
			[DefaultValue(8)]
			public int DivInitialTime
			{
				get { return _DivInitialTime; }
				set { _DivInitialTime = Math.Min((ushort)65535, (ushort)value); }
			}

			[DisplayName("Use Existing SaveRAM")]
			[Description("When true, existing SaveRAM will be loaded at boot up")]
			[DefaultValue(false)]
			public bool Use_SRAM { get; set; }


			[JsonIgnore]
			private int _RTCInitialTime;
			[JsonIgnore]
			private int _RTCOffset;
			[JsonIgnore]
			public ushort _DivInitialTime = 8;


			public GBSyncSettings Clone()
			{
				return (GBSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(GBSyncSettings x, GBSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
