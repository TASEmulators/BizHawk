using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

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
			fixed (byte* ptr = ret.data)
				GetUserSettings(ptr);
			return ret;
		}

		public bool PutSettings(object o)
		{
			return false;
		}

		public bool PutSyncSettings(MelonSyncSettings o)
		{
			fixed (byte* ptr = o.data)
				SetUserSettings(ptr);

			return true;
		}

		[DllImport(dllPath)]
		private static extern bool GetUserSettings(byte* dst);
		[DllImport(dllPath)]
		private static extern bool SetUserSettings(byte* src);

		[DllImport(dllPath)]
		private static extern int getUserSettingsLength();
		static int userSettingsLength = getUserSettingsLength();

		unsafe public class MelonSettings
		{
		}

		public class MelonSyncSettings
		{
			public MelonSyncSettings()
			{
				data = new byte[userSettingsLength];
			}

			public byte[] data;

			public byte favoriteColor => data[2];
			public byte birthdayMonth => data[3];
			public byte birthdayDay => data[4];
			const int maxNicknameLength = 10;
			public string nickname
			{
				get
				{
					fixed (byte* ptr = data)
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
					nick.CopyTo(data, 6);
					data[0x1A] = (byte)value.Length;
				}
			}
			public short nicknameLength { get => data[0x1A]; }
		}
	}
}
