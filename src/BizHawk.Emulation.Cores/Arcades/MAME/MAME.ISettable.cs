using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : ISettable<object, MAME.MAMESyncSettings>
	{
		public object GetSettings() => null;
		public PutSettingsDirtyBits PutSettings(object o) => PutSettingsDirtyBits.None;
		public List<DriverSetting> CurrentDriverSettings = new List<DriverSetting>();
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

		public class MAMESyncSettings
		{
			public Dictionary<string, string> DriverSettings { get; set; } = new Dictionary<string, string>();

			public static bool NeedsReboot(MAMESyncSettings x, MAMESyncSettings y)
			{
				return !DeepEquality.DeepEquals(x.DriverSettings, y.DriverSettings);
			}

			public MAMESyncSettings Clone()
			{
				return new MAMESyncSettings
				{
					DriverSettings = new Dictionary<string, string>(DriverSettings)
				};
			}
		}

		public void FetchDefaultGameSettings()
		{
			string DIPSwitchTags = MameGetString(MAMELuaCommand.GetDIPSwitchTags);
			string[] tags = DIPSwitchTags.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (string tag in tags)
			{
				string DIPSwitchFields = MameGetString(MAMELuaCommand.GetDIPSwitchFields(tag));
				string[] fieldNames = DIPSwitchFields.Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);

				foreach (string fieldName in fieldNames)
				{
					DriverSetting setting = new DriverSetting()
					{
						Name = fieldName,
						GameName = _gameShortName,
						LuaCode = MAMELuaCommand.InputField(tag, fieldName),
						Type = SettingType.DIPSWITCH,
						DefaultValue = LibMAME.mame_lua_get_int(
							$"return { MAMELuaCommand.InputField(tag, fieldName) }.defvalue").ToString()
					};

					string DIPSwitchOptions = MameGetString(MAMELuaCommand.GetDIPSwitchOptions(tag, fieldName));
					string[] options = DIPSwitchOptions.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);

					foreach(string option in options)
					{
						string[] opt = option.Split(new char[] { '~' }, StringSplitOptions.RemoveEmptyEntries);
						setting.Options.Add(opt[0], opt[1]);
					}

					CurrentDriverSettings.Add(setting);
				}
			}
		}

		public void OverrideGameSettings()
		{
			foreach (KeyValuePair<string, string> setting in _syncSettings.DriverSettings)
			{
				DriverSetting s = CurrentDriverSettings.SingleOrDefault(s => s.LookupKey == setting.Key);

				if (s != null)
				{
					LibMAME.mame_lua_execute($"{ s.LuaCode }.user_value = { setting.Value }");
				}
			}
		}

		public class DriverSetting
		{
			public string Name { get; set; }
			public string GameName { get; set; }
			public string LuaCode { get; set; }
			public string DefaultValue { get; set; }
			public SettingType Type { get; set; }
			public Dictionary<string, string> Options { get; set; }
			public string LookupKey => $"[{ GameName }] { LuaCode }";

			public DriverSetting()
			{
				Name = null;
				GameName = null;
				DefaultValue = null;
				Options = new Dictionary<string, string>();
			}
		}

		public enum SettingType
		{
			DIPSWITCH, BIOS
		}
	}
}