using System;
using System.Text;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : ISettable<object, MelonDS.MelonSyncSettings>
	{
		public object GetSettings()
		{
			return new object();
		}

		public MelonSyncSettings GetSyncSettings()
		{
			MelonSyncSettings ret = new MelonSyncSettings();
			fixed (byte* ptr = ret.userSettings)
			{
				if (!GetUserSettings(ptr))
					return null;
			}
			ret.bootToFirmware = !GetDirectBoot();
			ret.timeAtBoot = GetTimeAtBoot();
			return ret;
		}

		public bool PutSettings(object o)
		{
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
				fixed (byte* ptr = o.userSettings)
					SetUserSettings(ptr);
			}

			SetDirectBoot(!o.bootToFirmware);
			SetTimeAtBoot(o.timeAtBoot);

			// At present, no sync settings can be modified without requiring a reboot.
			return true;
		}

		[DllImport(dllPath)]
		private static extern bool GetUserSettings(byte* dst);
		[DllImport(dllPath)]
		private static extern int getUserSettingsLength();
		static int userSettingsLength = getUserSettingsLength();
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

		unsafe public class MelonSettings
		{
		}

		public class MelonSyncSettings
		{
			public MelonSyncSettings()
			{
				userSettings = new byte[userSettingsLength];
			}

			public MelonSyncSettings Clone() => (MelonSyncSettings)MemberwiseClone();

			public bool bootToFirmware = false;
			public uint timeAtBoot = 946684800; // 2000-01-01 00:00:00 (earliest date possible on a DS)
			public byte[] userSettings;

			[JsonIgnore]
			public byte favoriteColor
			{
				get => userSettings[2];
				set { userSettings[2] = value; }
			}

			[JsonIgnore]
			public byte birthdayMonth
			{
				get => userSettings[3];
				set { userSettings[3] = value; }
			}

			[JsonIgnore]
			public byte birthdayDay
			{
				get => userSettings[4];
				set { userSettings[4] = value; }
			}

			const int maxNicknameLength = 10;

			[JsonIgnore]
			public string nickname
			{
				get
				{
					fixed (byte* ptr = userSettings)
						return Encoding.Unicode.GetString(ptr + 6, nicknameLength * 2);
				}
				set
				{
					if (value.Length > maxNicknameLength) value = value.Substring(0, maxNicknameLength);
					byte[] nick = new byte[maxNicknameLength * 2 + 2];
					// I do not know how an actual NDS would handle characters that require more than 2 bytes to encode.
					// They can't be input normally, so I will ignore attempts to set a nickname that uses them.
					if (Encoding.Unicode.GetBytes(value, 0, value.Length, nick, 0) != value.Length * 2)
						return;
					// The extra 2 bytes on the end will overwrite nickname length, which is set immediately after
					nick.CopyTo(userSettings, 6);
					userSettings[0x1A] = (byte)value.Length;
				}
			}

			[JsonIgnore]
			public short nicknameLength => userSettings[0x1A];

			[JsonIgnore]
			public long rtcOffset
			{
				get => BitConverter.ToInt64(userSettings, 0x68);
				set => BitConverter.GetBytes(value).CopyTo(userSettings, 0x68);
			}
		}
	}
}
