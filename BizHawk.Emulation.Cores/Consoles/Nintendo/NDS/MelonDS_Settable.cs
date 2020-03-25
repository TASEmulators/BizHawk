using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;

using BizHawk.Emulation.Common;
using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : ISettable<MelonDS.MelonSettings, MelonDS.MelonSyncSettings>
	{
		private MelonSettings _settings;

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
			if (o == null || o.screenOptions == null)
			{
				o = new MelonSettings();
				o.screenOptions = new ScreenLayoutSettings();
				o.screenOptions.locations = new Point[] { new Point(0, 0), new Point(0, NATIVE_HEIGHT) };
				o.screenOptions.order = new int[] { 0, 1 };
				o.screenOptions.finalSize = new Size(NATIVE_WIDTH, NATIVE_HEIGHT * 2);
			}

			_settings = o;
			screenArranger.layoutSettings = _settings.screenOptions;

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

		public class MelonSettings
		{
			public ScreenLayoutSettings screenOptions;
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
