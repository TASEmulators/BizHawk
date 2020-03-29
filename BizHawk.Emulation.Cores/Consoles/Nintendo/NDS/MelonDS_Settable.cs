using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Text;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : ISettable<MelonDS.MelonSettings, MelonDS.MelonSyncSettings>
	{
		private MelonSettings _settings = new MelonSettings();

		public MelonSettings GetSettings()
		{
			return _settings;
		}

		public MelonSyncSettings GetSyncSettings()
		{
			MelonSyncSettings ret = new MelonSyncSettings();
			fixed (byte* ptr = ret.UserSettings)
			{
				if (!GetUserSettings(ptr))
					return null;
			}
			ret.BootToFirmware = !GetDirectBoot();
			ret.TimeAtBoot = GetTimeAtBoot();
			return ret;
		}

		public bool PutSettings(MelonSettings o)
		{
			_settings = o ?? new MelonSettings();
			return false;
		}

		public bool PutSyncSettings(MelonSyncSettings o)
		{
			if (o == null)
			{
				o = new MelonSyncSettings();
				SetUserSettings(null);
			}
			else
			{
				fixed (byte* ptr = o.UserSettings)
					SetUserSettings(ptr);
			}

			SetDirectBoot(!o.BootToFirmware);
			SetTimeAtBoot(o.TimeAtBoot);

			// At present, no sync settings can be modified without requiring a reboot.
			return true;
		}

		[DllImport(dllPath)]
		private static extern bool GetUserSettings(byte* dst);
		[DllImport(dllPath)]
		private static extern int GetUserSettingsLength();

		private static readonly int UserSettingsLength = GetUserSettingsLength();

		[DllImport(dllPath)]
		private static extern void SetUserSettings(byte* src);

		[DllImport(dllPath)]
		private static extern bool GetDirectBoot();
		[DllImport(dllPath)]
		private static extern void SetDirectBoot(bool value);

		[DllImport(dllPath)]
		private static extern void SetTimeAtBoot(uint value);
		[DllImport(dllPath)]
		private static extern uint GetTimeAtBoot();

		public enum VideoScreenOptions
		{
			Default,
			TopOnly,
			BottomOnly,
			SideBySideLR,
			SideBySideRL,
			Rotate90,
			Rotate270
		}

		public class MelonSettings
		{
			[DisplayName("Screen Configuration")]
			[Description("Adjusts the orientation of the 2 displays")]
			public VideoScreenOptions ScreenOptions { get; set; } = VideoScreenOptions.Default;

			[DisplayName("Screen Gap")]
			public int ScreenGap { get; set; }

			public Point? TouchScreenStart() =>
				ScreenOptions switch
				{
					VideoScreenOptions.TopOnly => null,
					VideoScreenOptions.BottomOnly => null,
					VideoScreenOptions.SideBySideLR => new Point(NativeWidth, 0),
					VideoScreenOptions.SideBySideRL => new Point(0, 0),
					VideoScreenOptions.Rotate90 => new Point(0, 0),
					VideoScreenOptions.Rotate270 => new Point(256, 0),
					_ => new Point(0, NativeHeight + ScreenGap)
				};
			

			public int Width() =>
				ScreenOptions switch
				{
					VideoScreenOptions.SideBySideLR => NativeWidth * 2,
					VideoScreenOptions.SideBySideRL => NativeWidth * 2,
					VideoScreenOptions.Rotate90 => NativeHeight * 2,
					VideoScreenOptions.Rotate270 => NativeHeight * 2,
					_ => NativeWidth
				};
			

			// TODO: padding
			public int Height() =>
				ScreenOptions switch
				{
					VideoScreenOptions.TopOnly => NativeHeight,
					VideoScreenOptions.BottomOnly => NativeHeight,
					VideoScreenOptions.SideBySideLR => NativeHeight,
					VideoScreenOptions.SideBySideRL => NativeHeight,
					VideoScreenOptions.Rotate90 => NativeWidth,
					VideoScreenOptions.Rotate270 => NativeWidth,
					_ => (NativeHeight * 2) + ScreenGap
				};
		}

		public class MelonSyncSettings
		{
			public MelonSyncSettings()
			{
				UserSettings = new byte[UserSettingsLength];
			}

			public MelonSyncSettings Clone() => (MelonSyncSettings)MemberwiseClone();

			public bool BootToFirmware { get; set; }
			public uint TimeAtBoot { get; set; } = 946684800; // 2000-01-01 00:00:00 (earliest date possible on a DS)

			[JsonProperty]
			internal byte[] UserSettings;

			[JsonIgnore]
			public byte FavoriteColor
			{
				get => UserSettings[2];
				set => UserSettings[2] = value;
			}

			[JsonIgnore]
			public byte BirthdayMonth
			{
				get => UserSettings[3];
				set => UserSettings[3] = value;
			}

			[JsonIgnore]
			public byte BirthdayDay
			{
				get => UserSettings[4];
				set => UserSettings[4] = value;
			}

			private const int MaxNicknameLength = 10;

			[JsonIgnore]
			public string Nickname
			{
				get
				{
					fixed (byte* ptr = UserSettings)
						return Encoding.Unicode.GetString(ptr + 6, NicknameLength * 2);
				}
				set
				{
					if (value.Length > MaxNicknameLength) value = value.Substring(0, MaxNicknameLength);
					byte[] nick = new byte[MaxNicknameLength * 2 + 2];
					// I do not know how an actual NDS would handle characters that require more than 2 bytes to encode.
					// They can't be input normally, so I will ignore attempts to set a nickname that uses them.
					if (Encoding.Unicode.GetBytes(value, 0, value.Length, nick, 0) != value.Length * 2)
						return;
					// The extra 2 bytes on the end will overwrite nickname length, which is set immediately after
					nick.CopyTo(UserSettings, 6);
					UserSettings[0x1A] = (byte)value.Length;
				}
			}

			[JsonIgnore]
			public short NicknameLength => UserSettings[0x1A];

			[JsonIgnore]
			public long RtcOffset
			{
				get => BitConverter.ToInt64(UserSettings, 0x68);
				set => BitConverter.GetBytes(value).CopyTo(UserSettings, 0x68);
			}
		}
	}
}
