using System;
using System.ComponentModel;
using System.Text;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : ISettable<MelonDS.MelonSettings, MelonDS.MelonSyncSettings>
	{
		private MelonSettings _settings = new MelonSettings();

		public MelonSettings GetSettings() => _settings.Clone();

		public MelonSyncSettings GetSyncSettings()
		{
			var ret = new MelonSyncSettings();
			fixed (byte* ptr = ret.UserSettings)
			{
				if (!GetUserSettings(ptr))
				{
					return null;
				}
			}

			ret.BootToFirmware = !GetDirectBoot();
			ret.TimeAtBoot = GetTimeAtBoot();
			return ret;
		}

		public PutSettingsDirtyBits PutSettings(MelonSettings o)
		{
			bool screenChanged = false;
			if (o != null)
			{
				screenChanged |= _settings.ScaleFactor != o.ScaleFactor;
				screenChanged |= _settings.ScreenGap != o.ScreenGap;
				screenChanged |= _settings.ScreenLayout != o.ScreenLayout;
				screenChanged |= _settings.ScreenRotation != o.ScreenRotation;
			}
			_settings = o ?? new MelonSettings();
			SetScaleFactor(_settings.ScaleFactor);
			return screenChanged ? PutSettingsDirtyBits.None : PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(MelonSyncSettings o)
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
			return PutSettingsDirtyBits.RebootCore;
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

		[DllImport(dllPath)]
		private static extern uint GetScaleFactor();
		[DllImport(dllPath)]
		private static extern void SetScaleFactor(uint value);

		public enum ScreenLayoutKind
		{
			Vertical, Horizontal, Top, Bottom
		}

		public enum ScreenRotationKind
		{
			Rotate0, Rotate90, Rotate180, Rotate270
		}

		public class MelonSettings
		{
			public MelonSettings Clone() => (MelonSettings)MemberwiseClone();

			[DisplayName("Screen Layout")]
			[Description("Adjusts the layout of the screens")]
			public ScreenLayoutKind ScreenLayout { get; set; } = ScreenLayoutKind.Vertical;

			[DisplayName("Rotation")]
			[Description("Adjusts the orientation of the screens")]
			public ScreenRotationKind ScreenRotation { get; set; } = ScreenRotationKind.Rotate0;

			[DisplayName("Screen Gap")]
			public int ScreenGap { get; set; }

			[DisplayName("Scale Factor")]
			public uint ScaleFactor { get; set; } = 1;
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
					{
						return;
					}

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
