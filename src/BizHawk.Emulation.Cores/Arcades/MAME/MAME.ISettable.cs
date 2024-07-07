using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : ISettable<object, MAME.MAMESyncSettings>
	{
		public object GetSettings() => null;
		public PutSettingsDirtyBits PutSettings(object o) => PutSettingsDirtyBits.None;
		public List<DriverSetting> CurrentDriverSettings = new();
		private MAMESyncSettings _syncSettings;

		public MAMESyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSyncSettings(MAMESyncSettings o)
		{
			var s = o.Clone();
			bool ret = MAMESyncSettings.NeedsReboot(s, _syncSettings);
			_syncSettings = s;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class MAMERTCSettings
		{
			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			[TypeConverter(typeof(BizDateTimeConverter))]
			public DateTime InitialTime { get; set; }

			[DisplayName("Use Real Time")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time. Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			public MAMERTCSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public MAMERTCSettings Clone()
				=> (MAMERTCSettings)MemberwiseClone();
		}

		public class MAMESyncSettings
		{
			public MAMERTCSettings RTCSettings { get; set; } = new();
			public SortedDictionary<string, string> DriverSettings { get; set; } = new();

			public static bool NeedsReboot(MAMESyncSettings x, MAMESyncSettings y)
			{
				return !DeepEquality.DeepEquals(x.RTCSettings, y.RTCSettings)
					|| !DeepEquality.DeepEquals(x.DriverSettings, y.DriverSettings);
			}

			public MAMESyncSettings Clone()
			{
				return new()
				{
					RTCSettings = RTCSettings.Clone(),
					DriverSettings = new(DriverSettings),
				};
			}
		}

		public void FetchDefaultGameSettings()
		{
			var DIPSwitchTags = MameGetString(MAMELuaCommand.GetDIPSwitchTags);
			var tags = DIPSwitchTags.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var tag in tags)
			{
				var DIPSwitchFields = MameGetString(MAMELuaCommand.GetDIPSwitchFields(tag));
				var fieldNames = DIPSwitchFields.Split(new[] { '^' }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var fieldName in fieldNames)
				{
					var setting = new DriverSetting
					{
						Name = fieldName,
						GameName = _gameShortName,
						LuaCode = MAMELuaCommand.InputField(tag, fieldName),
						Type = SettingType.DIPSWITCH,
						DefaultValue = _core.mame_lua_get_int(
							$"return { MAMELuaCommand.InputField(tag, fieldName) }.defvalue").ToString()
					};

					var DIPSwitchOptions = MameGetString(MAMELuaCommand.GetDIPSwitchOptions(tag, fieldName));
					var options = DIPSwitchOptions.Split(new[] { '@' }, StringSplitOptions.RemoveEmptyEntries);

					foreach (var option in options)
					{
						var opt = option.Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries);
						setting.Options.Add(opt[0], opt[1]);
					}

					if (options.Any())
					{
						CurrentDriverSettings.Add(setting);
					}
				}
			}
		}

		public void OverrideGameSettings()
		{
			foreach (var setting in _syncSettings.DriverSettings)
			{
				var s = CurrentDriverSettings.SingleOrDefault(s => s.LookupKey == setting.Key);

				if (s != null && s.Type == SettingType.DIPSWITCH)
				{
					_core.mame_lua_execute($"{ s.LuaCode }.user_value = { setting.Value }");
				}
			}
		}

		private void GetROMsInfo()
		{
			var ROMsInfo = MameGetString(MAMELuaCommand.GetROMsInfo);
			var ROMs = ROMsInfo.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			var tempDefault = string.Empty;

			var setting = new DriverSetting
			{
				Name = "BIOS",
				GameName = _gameShortName,
				LuaCode = LibMAME.BIOS_LUA_CODE,
				Type = SettingType.BIOS
			};

			foreach (var ROM in ROMs)
			{
				if (ROM != string.Empty)
				{
					var substrings = ROM.Split('~');
					var name = substrings[0];
					var hashdata = substrings[1];
					var flags = long.Parse(substrings[2]);

					if ((flags & LibMAME.ROMENTRY_TYPEMASK) == LibMAME.ROMENTRYTYPE_SYSTEM_BIOS
						|| (flags & LibMAME.ROMENTRY_TYPEMASK) == LibMAME.ROMENTRYTYPE_DEFAULT_BIOS)
					{
						setting.Options.Add(name, hashdata);

						// if no bios is explicitly marked as default
						// mame uses the first one in the list
						// and its index is reflected in the flags (ROM_BIOSFLAGSMASK)
						if ((flags >> LibMAME.BIOS_INDEX) == LibMAME.BIOS_FIRST)
						{
							tempDefault = name;
						}

						if ((flags & LibMAME.ROMENTRY_TYPEMASK) == LibMAME.ROMENTRYTYPE_DEFAULT_BIOS)
						{
							setting.DefaultValue = name;
						}
					}
					else
					{
						if (hashdata.EndsWithOrdinal('^'))
						{
							hashdata = hashdata.RemoveSuffix("^");
							name += " (BAD DUMP)";
						}

						hashdata = hashdata.Replace("R", "CRC:").Replace("S", " SHA:");
						_romHashes.Add(name, hashdata);
					}
				}
			}

			if (setting.Options.Count > 0)
			{
				if (setting.DefaultValue == null)
				{
					setting.DefaultValue = tempDefault;
				}

				CurrentDriverSettings.Add(setting);
			}
		}

		private void GetViewsInfo()
		{
			var ViewsInfo = MameGetString(MAMELuaCommand.GetViewsInfo);
			var Views = ViewsInfo.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

			var setting = new DriverSetting
			{
				Name = "View",
				GameName = _gameShortName,
				LuaCode = LibMAME.VIEW_LUA_CODE,
				Type = SettingType.VIEW,
				DefaultValue = MameGetString(MAMELuaCommand.GetViewName("1"))
			};

			foreach (var View in Views)
			{
				if (View != string.Empty)
				{
					var substrings = View.Split(',');
					setting.Options.Add(substrings[1], substrings[1]);
				}
			}

			if (setting.Options.Count > 0)
			{
				CurrentDriverSettings.Add(setting);
			}
		}

		public class DriverSetting
		{
			public string Name { get; set; }
			public string GameName { get; set; }
			public string LuaCode { get; set; }
			public string DefaultValue { get; set; }
			public SettingType Type { get; set; }
			public SortedDictionary<string, string> Options { get; set; }
			public string LookupKey => MAMELuaCommand.MakeLookupKey(GameName, LuaCode);

			public DriverSetting()
			{
				Name = null;
				GameName = null;
				DefaultValue = null;
				Options = new();
			}
		}

		public enum SettingType
		{
			DIPSWITCH, BIOS, VIEW
		}
	}
}