using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
				GetUserSettings(ptr);
			ret.bootToFirmware = !GetDirectBoot();
			return ret;
		}

		public bool PutSettings(object o)
		{
			return false;
		}

		public bool PutSyncSettings(MelonSyncSettings o)
		{
			SetDirectBoot(!o.bootToFirmware);
			// At present, no sync settings can be modified without requiring a reboot.
			return true;
		}

		[DllImport(dllPath)]
		private static extern bool GetUserSettings(byte* dst);
		[DllImport(dllPath)]
		private static extern int getUserSettingsLength();
		static int userSettingsLength = getUserSettingsLength();

		[DllImport(dllPath)]
		private static extern bool GetDirectBoot();
		[DllImport(dllPath)]
		private static extern void SetDirectBoot(bool value);

		unsafe public class MelonSettings
		{
		}

		public class MelonSyncSettings
		{
			public MelonSyncSettings()
			{
				userSettings = new byte[userSettingsLength];
			}

			public bool bootToFirmware;
			public byte[] userSettings;

			[JsonIgnore]
			public byte favoriteColor => userSettings[2];
			[JsonIgnore]
			public byte birthdayMonth => userSettings[3];
			[JsonIgnore]
			public byte birthdayDay => userSettings[4];
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
			public short nicknameLength { get => userSettings[0x1A]; }
		}
	}
}
