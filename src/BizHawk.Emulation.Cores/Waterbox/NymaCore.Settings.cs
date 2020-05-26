using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Waterbox
{
	unsafe partial class NymaCore : ISettable<NymaCore.NymaSettings, NymaCore.NymaSyncSettings>
	{
		/// <summary>
		/// Settings that we shouldn't show the user
		/// </summary>
		protected virtual ICollection<string> HiddenSettings { get; } = new string[0];
		public NymaSettingsInfo SettingsInfo { get; private set; }
		private NymaSettings _settings;
		private NymaSyncSettings _syncSettings;
		/// <summary>
		/// What this core was actually started with
		/// </summary>
		private NymaSyncSettings _syncSettingsActual;
		public NymaSettings GetSettings()
		{
			var ret = _settings.Clone();
			return ret;
		}
		public NymaSyncSettings GetSyncSettings()
		{
			var ret = _syncSettings.Clone();
			return ret;
		}

		public PutSettingsDirtyBits PutSettings(NymaSettings o)
		{
			_settings = o.Clone();
			if (SettingsInfo.LayerNames.Count > 0)
			{
				ulong layers = ~0ul;
				for (int i = 0; i < 64 && i < SettingsInfo.LayerNames.Count; i++)
				{
					if (_settings.DisabledLayers.Contains(SettingsInfo.LayerNames[i]))
						layers &= ~(1ul << i);
				}
				_nyma.SetLayers(layers);
			}
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(NymaSyncSettings o)
		{
			_syncSettings = o.Clone();
			return _syncSettings.Equals(_syncSettingsActual)
				? PutSettingsDirtyBits.None
				: PutSettingsDirtyBits.RebootCore;
		}

		public class NymaSettings
		{
			public HashSet<string> DisabledLayers { get; set; } = new HashSet<string>();
			public NymaSettings Clone()
			{
				return new NymaSettings { DisabledLayers = new HashSet<string>(DisabledLayers) };
			}
		}

		public class NymaSyncSettings
		{
			public Dictionary<string, string> MednafenValues { get; set; } = new Dictionary<string, string>();
			public Dictionary<int, string> PortDevices { get; set; } = new Dictionary<int, string>();
			public NymaSyncSettings Clone()
			{
				return new NymaSyncSettings
				{
					MednafenValues = new Dictionary<string, string>(MednafenValues),
					PortDevices = new Dictionary<int, string>(PortDevices)
				};
			}

			public override bool Equals(object obj)
			{
				if (!(obj is NymaSyncSettings x))
					return false;
				return new HashSet<KeyValuePair<int, string>>(PortDevices).SetEquals(x.PortDevices)
					&& new HashSet<KeyValuePair<string, string>>(MednafenValues).SetEquals(x.MednafenValues);
			}

			public override int GetHashCode()
			{
				return 0; // some other time, maybe
			}
		}

		private void SettingsQuery(string name, IntPtr dest)
		{
			if (!_syncSettingsActual.MednafenValues.TryGetValue(name, out var val) || HiddenSettings.Contains(name))
			{
				if (SettingsInfo.SettingsByKey.TryGetValue(name, out var info))
				{
					val = info.DefaultValue;
				}
				else
				{
					throw new InvalidOperationException($"Core asked for setting {name} which was not found in the defaults");
				}
			}
			var bytes = Encoding.UTF8.GetBytes(val);
			if (bytes.Length > 255)
				throw new InvalidOperationException($"Value {val} for setting {name} was too long");
			WaterboxUtils.ZeroMemory(dest, 256);
			Marshal.Copy(bytes, 0, dest, bytes.Length);
		}

		private LibNymaCore.FrontendSettingQuery _settingsQueryDelegate;

		/// <summary>
		/// If true, the settings object has at least one settable value in it
		/// </summary>
		public bool HasSettings => SettingsInfo.LayerNames.Any();
		/// <summary>
		/// If true, the syncSettings object has at least one settable value in it
		/// </summary>
		public bool HasSyncSettings => SettingsInfo.Ports.Any() || SettingsInfo.Settings.Any();

		public class NymaSettingsInfo
		{
			/// <summary>
			/// What layers are available to toggle.  If empty, layers cannot be set on this core.
			/// </summary>
			public List<string> LayerNames { get; set; }
			public class Device
			{
				public string Name { get; set; }
				public string Description { get; set; }
				public string SettingValue { get; set; }
			}
			public class Port
			{
				public string Name { get; set; }
				public List<Device> AllowedDevices { get; set; } = new List<Device>();
				public string DefaultSettingsValue { get; set; }
			}
			/// <summary>
			/// What devices can be plugged into each port
			/// </summary>
			public List<Port> Ports { get; set; } = new List<Port>();
			public class MednaSetting
			{
				public string Name;
				public string Description;
				public string SettingsKey;
				public string DefaultValue;
				public string Min;
				public string Max;
				[Flags]
				public enum SettingFlags : uint
				{
					NOFLAGS = 0U,	  // Always 0, makes setting definitions prettier...maybe.

					// TODO(cats)
					CAT_INPUT = (1U << 8),
					CAT_SOUND = (1U << 9),
					CAT_VIDEO = (1U << 10),
					CAT_INPUT_MAPPING = (1U << 11),	// User-configurable physical->virtual button/axes and hotkey mappings(driver-side code category mainly).

					// Setting is used as a path or filename(mostly intended for automatic charset conversion of 0.9.x settings on MS Windows).
					CAT_PATH = (1U << 12),

					EMU_STATE = (1U << 17), // If the setting affects emulation from the point of view of the emulated program
					UNTRUSTED_SAFE = (1U << 18), // If it's safe for an untrusted source to modify it, probably only used in conjunction with
															// MDFNST_EX_EMU_STATE and network play

					SUPPRESS_DOC = (1U << 19), // Suppress documentation generation for this setting.
					COMMON_TEMPLATE = (1U << 20), // Auto-generated common template setting(like nes.xscale, pce.xscale, vb.xscale, nes.enable, pce.enable, vb.enable)
					NONPERSISTENT = (1U << 21), // Don't save setting in settings file.

					// TODO:
					// WILL_BREAK_GAMES (1U << ) // If changing the value of the setting from the default value will break games/programs that would otherwise work.

					// TODO(in progress):
					REQUIRES_RELOAD = (1U << 24),	// If a game reload is required for the setting to take effect.
					REQUIRES_RESTART = (1U << 25),	// If Mednafen restart is required for the setting to take effect.
				}
				public SettingFlags Flags;
				public enum SettingType : int
				{
					INT = 0, // (signed), int8, int16, int32, int64(saved as)
					UINT, // uint8, uint16, uint32, uint64(saved as)
					/// <summary>
					/// 0 or 1
					/// </summary>
					BOOL,
					/// <summary>
					/// float64
					/// </summary>
					FLOAT,
					STRING,
					/// <summary>
					/// string value from a list of potential strings
					/// </summary>
					ENUM,
					/// <summary>
					/// TODO: How do these work
					/// </summary>
					MULTI_ENUM,
					/// <summary>
					/// Shouldn't see any of these
					/// </summary>
					ALIAS
				}
				public SettingType Type;
				public class EnumValue
				{
					public string Name;
					public string Description;
					public string Value;
					public EnumValue(MednaSettingS.EnumValueS s)
					{
						Name = Mershul.PtrToStringUtf8(s.Name);
						Description = Mershul.PtrToStringUtf8(s.Description);
						Value = Mershul.PtrToStringUtf8(s.Value);
					}
				}
				public MednaSetting(MednaSettingS s)
				{
					Name = Mershul.PtrToStringUtf8(s.Name);
					Description = Mershul.PtrToStringUtf8(s.Description);
					SettingsKey = Mershul.PtrToStringUtf8(s.SettingsKey);
					DefaultValue = Mershul.PtrToStringUtf8(s.DefaultValue);
					Min = Mershul.PtrToStringUtf8(s.Min);
					Max = Mershul.PtrToStringUtf8(s.Max);
					Flags = (SettingFlags)s.Flags;
					Type = (SettingType)s.Type;
				}
				public List<MednaSetting.EnumValue> SettingEnums { get; set; } = new List<MednaSetting.EnumValue>();
			}
			[StructLayout(LayoutKind.Sequential)]
			public class MednaSettingS
			{
				public IntPtr Name;
				public IntPtr Description;
				public IntPtr SettingsKey;
				public IntPtr DefaultValue;
				public IntPtr Min;
				public IntPtr Max;
				public uint Flags;
				public int Type;
				[StructLayout(LayoutKind.Sequential)]
				public class EnumValueS
				{
					public IntPtr Name;
					public IntPtr Description;
					public IntPtr Value;
				}
			}
			public List<MednaSetting> Settings { get; set; } = new List<MednaSetting>();
			public Dictionary<string, MednaSetting> SettingsByKey { get; set; } = new Dictionary<string, MednaSetting>();
			public HashSet<string> HiddenSettings { get; set; } = new HashSet<string>();
		}
		private void InitSyncSettingsInfo()
		{
			// TODO: Some shared logic in ControllerAdapter.  Avoidable?
			var s = new NymaSettingsInfo();

			var numPorts = _nyma.GetNumPorts();
			for (uint port = 0; port < numPorts; port++)
			{
				var portInfo = *_nyma.GetPort(port);

				s.Ports.Add(new NymaSettingsInfo.Port
				{
					Name = portInfo.FullName,
					DefaultSettingsValue = portInfo.DefaultDeviceShortName,
					AllowedDevices = Enumerable.Range(0, (int)portInfo.NumDevices)
						.Select(i =>
						{
							var dev =  *_nyma.GetDevice(port, (uint)i);
							return new NymaSettingsInfo.Device
							{
								Name = dev.FullName,
								Description = dev.Description,
								SettingValue = dev.ShortName
							};
						})
						.ToList()
				});
			}

			for (var i = 0;; i++)
			{
				var tt = new NymaSettingsInfo.MednaSettingS();
				_nyma.IterateSettings(i, tt);
				if (tt.SettingsKey == IntPtr.Zero)
					break;
				var ss = new NymaSettingsInfo.MednaSetting(tt);
				s.Settings.Add(ss);
				s.SettingsByKey.Add(ss.SettingsKey, ss);
				if (ss.Type == NymaSettingsInfo.MednaSetting.SettingType.ENUM)
				{
					var l = ss.SettingEnums;
					for (var j = 0;; j++)
					{
						var ff = new NymaSettingsInfo.MednaSettingS.EnumValueS();
						_nyma.IterateSettingEnums(i, j, ff);
						if (ff.Value == IntPtr.Zero)
							break;
						var ee = new NymaSettingsInfo.MednaSetting.EnumValue(ff);
						l.Add(ee);
					}
				}
			}

			s.HiddenSettings = new HashSet<string>(HiddenSettings);
			SettingsInfo = s;
		}
	}
}
