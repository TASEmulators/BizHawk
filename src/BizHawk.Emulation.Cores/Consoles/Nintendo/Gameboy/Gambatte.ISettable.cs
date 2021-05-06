using System;
using System.ComponentModel;

using Newtonsoft.Json;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : ISettable<Gameboy.GambatteSettings, Gameboy.GambatteSyncSettings>
	{
		public GambatteSettings GetSettings()
		{
			return _settings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(GambatteSettings o)
		{
			_settings = o;
			_disassembler.UseRGBDSSyntax = _settings.RgbdsSyntax;
			if (IsCGBMode())
			{
				SetCGBColors(_settings.CGBColors);
			}
			else
			{
				ChangeDMGColors(_settings.GBPalette);
			}

			return PutSettingsDirtyBits.None;
		}

		public GambatteSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSyncSettings(GambatteSyncSettings o)
		{
			bool ret = GambatteSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private GambatteSettings _settings;
		private GambatteSyncSettings _syncSettings;

		public class GambatteSettings
		{
			/* Green Palette
			private static readonly int[] DefaultPalette =
			{
				10798341, 8956165, 1922333, 337157,
				10798341, 8956165, 1922333, 337157,
				10798341, 8956165, 1922333, 337157
			};
			*/
			// Grey Scale Palette
			private static readonly int[] DefaultPalette =
			{
				0xFFFFFF, 0xAAAAAA, 0x555555, 0,
				0xFFFFFF, 0xAAAAAA, 0x555555, 0,
				0xFFFFFF, 0xAAAAAA, 0x555555, 0
			};

			public int[] GBPalette;
			public GBColors.ColorType CGBColors;

			/// <summary>
			/// true to mute all audio
			/// </summary>
			public bool Muted;
			
			/// <summary>
			/// true to use rgbds syntax
			/// </summary>
			public bool RgbdsSyntax;

			public GambatteSettings()
			{
				GBPalette = (int[])DefaultPalette.Clone();
				CGBColors = GBColors.ColorType.gambatte;
				RgbdsSyntax = true;
			}


			public GambatteSettings Clone()
			{
				var ret = (GambatteSettings)MemberwiseClone();
				ret.GBPalette = (int[])GBPalette.Clone();
				return ret;
			}
		}

		public class GambatteSyncSettings
		{
			[DisplayName("Use official Nintendo BootROM")]
			[Description("Uses a provided official BootROM (or \"BIOS\") instead of built-in unofficial firmware. You must provide the BootROM. Should be used for TASing.")]
			[DefaultValue(false)]
			public bool EnableBIOS { get; set; }

			public enum ConsoleModeType
			{
				Auto,
				GB,
				GBC,
				GBA
			}

			[DisplayName("Console Mode")]
			[Description("Pick which console to run, 'Auto' chooses from ROM header; 'GB', 'GBC', and 'GBA' chooses the respective system")]
			[DefaultValue(ConsoleModeType.Auto)]
			public ConsoleModeType ConsoleMode { get; set; }

			[DisplayName("Multicart Compatibility")]
			[Description("Use special compatibility hacks for certain multicart games.  Relevant only for specific multicarts.")]
			[DefaultValue(false)]
			public bool MulticartCompat { get; set; }

			[DisplayName("Realtime RTC")]
			[Description("If true, the real time clock in MBC3 and HuC3 games will reflect real time, instead of emulated time.  Ignored (treated as false) when a movie is recording.")]
			[DefaultValue(false)]
			public bool RealTimeRTC { get; set; }

			[DisplayName("RTC Divisor Offset")]
			[Description("CPU clock frequency relative to real time clock. Base value is 2^22 Hz. Used in cycle-based RTC to sync on real hardware to account for RTC imperfections.")]
			[DefaultValue(0)]
			public int RTCDivisorOffset { get; set; }

			[JsonIgnore]
			private int _internalRTCDays;

			[JsonIgnore]
			private int _internalRTCHours;

			[JsonIgnore]
			private int _internalRTCMinutes;

			[JsonIgnore]
			private int _internalRTCSeconds;

			[JsonIgnore]
			private int _internalRTCCycles;

			[JsonIgnore]
			private int _latchedRTCDays;

			[JsonIgnore]
			private int _latchedRTCHours;

			[JsonIgnore]
			private int _latchedRTCMinutes;

			[JsonIgnore]
			private int _latchedRTCSeconds;

			[DisplayName("RTC Overflow")]
			[Description("Sets whether the internal RTC day counter has overflowed.")]
			[DefaultValue(false)]
			public bool InternalRTCOverflow { get; set; }

			[DisplayName("RTC Halt")]
			[Description("Sets whether the internal RTC has halted.")]
			[DefaultValue(false)]
			public bool InternalRTCHalt { get; set; }

			[DisplayName("RTC Days")]
			[Description("Sets the internal RTC day counter. Ranges from 0 to 511.")]
			[DefaultValue(0)]
			public int InternalRTCDays
			{
				get => _internalRTCDays;
				set => _internalRTCDays = Math.Max(0, Math.Min(511, value));
			}

			[DisplayName("RTC Hours")]
			[Description("Sets the internal RTC hour counter. Ranges from -8 to 23.")]
			[DefaultValue(0)]
			public int InternalRTCHours
			{
				get => _internalRTCHours;
				set => _internalRTCHours = Math.Max(-8, Math.Min(23, value));
			}

			[DisplayName("RTC Minutes")]
			[Description("Sets the internal RTC minute counter. Ranges from -4 to 59.")]
			[DefaultValue(0)]
			public int InternalRTCMinutes
			{
				get => _internalRTCMinutes;
				set => _internalRTCMinutes = Math.Max(-4, Math.Min(59, value));
			}

			[DisplayName("RTC Seconds")]
			[Description("Sets the internal RTC second counter. Ranges from -4 to 59.")]
			[DefaultValue(0)]
			public int InternalRTCSeconds
			{
				get => _internalRTCSeconds;
				set => _internalRTCSeconds = Math.Max(-4, Math.Min(59, value));
			}

			[DisplayName("RTC Sub-Seconds")]
			[Description("Sets the internal RTC sub-second counter, expressed in CPU cycles. Ranges from 0 to 4194303 + the set RTC divisor offset.")]
			[DefaultValue(0)]
			public int InternalRTCCycles
			{
				get => _internalRTCCycles;
				set => _internalRTCCycles = Math.Max(0, Math.Min((4194303 + RTCDivisorOffset), value));
			}

			[DisplayName("Latched RTC Overflow")]
			[Description("Sets whether the latched RTC shows an overflow.")]
			[DefaultValue(false)]
			public bool LatchedRTCOverflow { get; set; }

			[DisplayName("Latched RTC Halt")]
			[Description("Sets whether the latched RTC shows a halt.")]
			[DefaultValue(false)]
			public bool LatchedRTCHalt { get; set; }

			[DisplayName("Latched RTC Days")]
			[Description("Sets the latched RTC days. Ranges from 0 to 511.")]
			[DefaultValue(0)]
			public int LatchedRTCDays
			{
				get => _latchedRTCDays;
				set => _latchedRTCDays = Math.Max(0, Math.Min(511, value));
			}

			[DisplayName("Latched RTC Hours")]
			[Description("Sets the latched RTC hours. Ranges from 0 to 31.")]
			[DefaultValue(0)]
			public int LatchedRTCHours
			{
				get => _latchedRTCHours;
				set => _latchedRTCHours = Math.Max(0, Math.Min(63, value));
			}

			[DisplayName("Latched RTC Minutes")]
			[Description("Sets the latched RTC minutes. Ranges from 0 to 63.")]
			[DefaultValue(0)]
			public int LatchedRTCMinutes
			{
				get => _latchedRTCMinutes;
				set => _latchedRTCMinutes = Math.Max(0, Math.Min(63, value));
			}

			[DisplayName("Latched RTC Seconds")]
			[Description("Sets the latched RTC seconds. Ranges from 0 to 63.")]
			[DefaultValue(0)]
			public int LatchedRTCSeconds
			{
				get => _latchedRTCSeconds;
				set => _latchedRTCSeconds = Math.Max(0, Math.Min(63, value));
			}

			[DisplayName("Equal Length Frames")]
			[Description("When false, emulation frames sync to vblank.  Only useful for high level TASing.")]
			[DefaultValue(false)]
			public bool EqualLengthFrames
			{
				get => _equalLengthFrames;
				set => _equalLengthFrames = value;
			}

			[DisplayName("Display BG")]
			[Description("Display background")]
			[DefaultValue(true)]
			public bool DisplayBG { get; set; }

			[DisplayName("Display OBJ")]
			[Description("Display objects")]
			[DefaultValue(true)]
			public bool DisplayOBJ { get; set; }

			[DisplayName("Display Window")]
			[Description("Display window")]
			[DefaultValue(true)]
			public bool DisplayWindow { get; set; }

			[JsonIgnore]
			[DeepEqualsIgnore]
			private bool _equalLengthFrames;

			public GambatteSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public GambatteSyncSettings Clone()
			{
				return (GambatteSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(GambatteSyncSettings x, GambatteSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
