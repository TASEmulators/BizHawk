using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using NymaTypes;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public partial class NymaCore : ISettable<NymaCore.NymaSettings, NymaCore.NymaSyncSettings>
	{
		public NymaSettingsInfo SettingsInfo { get; private set; }
		private NymaSettings _settings;
		private NymaSyncSettings _syncSettings;
		/// <summary>
		/// What this core was actually started with
		/// </summary>
		private readonly NymaSyncSettings _syncSettingsActual;
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
			var n = o.Clone();
			n.Normalize(SettingsInfo);
			var ret = NymaSettings.Reboot(_settings, n, SettingsInfo);
			var notifies = NymaSettings.ChangedKeys(_settings, n, SettingsInfo).ToList();

			_settings = n;
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
			foreach (var key in notifies)
			{
				_nyma.NotifySettingChanged(key);
			}
			return ret;
		}

		public PutSettingsDirtyBits PutSyncSettings(NymaSyncSettings o)
		{
			var n = o.Clone();
			n.Normalize(SettingsInfo);
			var ret = NymaSyncSettings.Reboot(_syncSettingsActual, n, SettingsInfo);

			_syncSettings = n;
			return ret;
		}

		public interface INymaDictionarySettings
		{
			Dictionary<string, string> MednafenValues { get; }
		}

		public class NymaSettings :  INymaDictionarySettings
		{
			public Dictionary<string, string> MednafenValues { get; set; } = new Dictionary<string, string>();
			public HashSet<string> DisabledLayers { get; set; } = new HashSet<string>();
			public NymaSettings Clone()
			{
				return new NymaSettings
				{
					MednafenValues = new Dictionary<string, string>(MednafenValues),
					DisabledLayers = new HashSet<string>(DisabledLayers),
				};
			}
			/// <summary>
			/// remove things that aren't used by the core
			/// Normally won't do anything, but can be useful in case settings change
			/// </summary>
			public void Normalize(NymaSettingsInfo info)
			{
				var toRemove = new List<string>();
				foreach (var kvp in MednafenValues)
				{
					if (!info.AllSettingsByKey.ContainsKey(kvp.Key))
					{
						toRemove.Add(kvp.Key);
					}
					else
					{
						var ovr = info.AllOverrides[kvp.Key];
						if (ovr.Hide || !ovr.NonSync)
							toRemove.Add(kvp.Key);
					}
				}
				foreach (var key in toRemove)
				{
					MednafenValues.Remove(key);
				}
				DisabledLayers = new HashSet<string>(DisabledLayers.Where(l => info.LayerNames.Contains(l)));
			}

			public static IEnumerable<string> ChangedKeys(NymaSettings x, NymaSettings y, NymaSettingsInfo info)
			{
				var possible = info.AllOverrides.Where(kvp => kvp.Value.NonSync && kvp.Value.NoRestart).Select(kvp => kvp.Key);
				return possible.Where(key =>
				{
					_ = x.MednafenValues.TryGetValue(key, out var xx);
					_ = y.MednafenValues.TryGetValue(key, out var yy);
					return xx != yy;
				});
			}

			public static PutSettingsDirtyBits Reboot(NymaSettings x, NymaSettings y, NymaSettingsInfo info)
			{
				var restarters = info.AllOverrides.Where(kvp => kvp.Value.NonSync && !kvp.Value.NoRestart).Select(kvp => kvp.Key);
				foreach (var key in restarters)
				{
					_ = x.MednafenValues.TryGetValue(key, out var xx);
					_ = y.MednafenValues.TryGetValue(key, out var yy);
					if (xx != yy)
						return PutSettingsDirtyBits.RebootCore;
				}
				return PutSettingsDirtyBits.None;
			}
		}

		public class NymaSyncSettings :  INymaDictionarySettings
		{
			public Dictionary<string, string> MednafenValues { get; set; } = new Dictionary<string, string>();
			public Dictionary<int, string> PortDevices { get; set; } = new Dictionary<int, string>();
			public NymaSyncSettings Clone()
			{
				return new NymaSyncSettings
				{
					MednafenValues = new Dictionary<string, string>(MednafenValues),
					PortDevices = new Dictionary<int, string>(PortDevices),
				};
			}
			/// <summary>
			/// remove things that aren't used by the core
			/// Normally won't do anything, but can be useful in case settings change
			/// </summary>
			public void Normalize(NymaSettingsInfo info)
			{
				var toRemove = new List<string>();
				foreach (var kvp in MednafenValues)
				{
					if (!info.AllSettingsByKey.ContainsKey(kvp.Key))
					{
						toRemove.Add(kvp.Key);
					}
					else
					{
						var ovr = info.AllOverrides[kvp.Key];
						if (ovr.Hide || ovr.NonSync)
							toRemove.Add(kvp.Key);
					}
				}
				foreach (var key in toRemove)
				{
					MednafenValues.Remove(key);
				}
				var toRemovePort = new List<int>();
				foreach (var kvp in PortDevices)
				{
					if (info.Ports.Count <= kvp.Key || info.Ports[kvp.Key].DefaultSettingsValue == kvp.Value)
						toRemovePort.Add(kvp.Key);
				}
				foreach (var key in toRemovePort)
				{
					PortDevices.Remove(key);
				}
			}

			public static PutSettingsDirtyBits Reboot(NymaSyncSettings x, NymaSyncSettings y, NymaSettingsInfo info)
			{
				var restarters = info.AllOverrides.Where(kvp => !kvp.Value.NonSync && !kvp.Value.NoRestart).Select(kvp => kvp.Key);
				foreach (var key in restarters)
				{
					_ = x.MednafenValues.TryGetValue(key, out var xx);
					_ = y.MednafenValues.TryGetValue(key, out var yy);
					if (xx != yy)
						return PutSettingsDirtyBits.RebootCore;
				}
				return x.PortDevices.OrderBy(static kvp => kvp.Key).SequenceEqual(y.PortDevices.OrderBy(static kvp => kvp.Key))
					? PutSettingsDirtyBits.None
					: PutSettingsDirtyBits.RebootCore;
			}
		}

		protected string SettingsQuery(string name)
		{
			if (!SettingsInfo.AllOverrides.TryGetValue(name, out var ovr))
				throw new InvalidOperationException($"Core asked for setting {name} which was not found in the defaults");
			string val = null;
			if (!ovr.Hide)
			{
				// try to get actual value from settings
				var dict = ovr.NonSync ? _settings.MednafenValues : _syncSettingsActual.MednafenValues;
				_ = dict.TryGetValue(name, out val);
			}
			if (val == null)
			{
				// get default
				val = ovr.Default ?? SettingsInfo.AllSettingsByKey[name].DefaultValue;
			}
			return val;
		}

		private void SettingsQuery(string name, IntPtr dest)
		{
			var val = SettingsQuery(name);
			var bytes = val is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(val);
			if (bytes.Length > 255)
			{
				throw new InvalidOperationException($"Value {val} for setting {name} was too long");
			}
			var dstSpan = Util.UnsafeSpanFromPointer(ptr: dest, length: 256);
			dstSpan.Clear();
			bytes.CopyTo(dstSpan);
		}

		private LibNymaCore.FrontendSettingQuery _settingsQueryDelegate;

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
			public List<SettingT> AllSettings { get; set; } = new List<SettingT>();
			public Dictionary<string, SettingT> AllSettingsByKey { get; set; } = new Dictionary<string, SettingT>();
			public Dictionary<string, SettingOverride> AllOverrides { get; set; } = new Dictionary<string, SettingOverride>();
			/// <summary>
			/// If true, the settings object has at least one settable value in it
			/// </summary>
			public bool HasSettings => LayerNames.Count > 0
				|| AllSettings.Select(s => AllOverrides[s.SettingsKey]).Any(o => !o.Hide && o.NonSync);
			/// <summary>
			/// If true, the syncSettings object has at least one settable value in it
			/// </summary>
			public bool HasSyncSettings => Ports.Count > 0
				|| AllSettings.Select(s => AllOverrides[s.SettingsKey]).Any(o => !o.Hide && !o.NonSync);

			public NymaSettingsInfo Clone()
			{
				return new NymaSettingsInfo
				{
					LayerNames = new(LayerNames ?? new()),
					Ports = new(Ports ?? new()),
					AllSettings = new(AllSettings ?? new()),
					AllSettingsByKey = new(AllSettingsByKey ?? new()),
					AllOverrides = new(AllOverrides ?? new())
				};
			}
		}
		private void InitAllSettingsInfo(List<NPortInfoT> allPorts)
		{
			var s = new NymaSettingsInfo();

			foreach (var kvp in ExtraOverrides.Concat(SettingOverrides))
			{
				s.AllOverrides[kvp.Key] = kvp.Value;
			}

			foreach (var portInfo in allPorts)
			{
				if (s.AllOverrides.TryGetValue(portInfo.FullName, out var portOverride) && portOverride.Default is not null)
				{
					portInfo.DefaultDeviceShortName = portOverride.Default;
				}

				s.Ports.Add(new NymaSettingsInfo.Port
				{
					Name = portInfo.FullName,
					DefaultSettingsValue = portInfo.DefaultDeviceShortName,
					AllowedDevices = portInfo.Devices.Select(dev => new NymaSettingsInfo.Device
					{
						Name = dev.FullName,
						Description = dev.Description,
						SettingValue = dev.ShortName
					}).ToList()
				});
			}

			var originalSettings = GetSettingsData();
			foreach (var setting in originalSettings.Concat(ExtraSettings))
			{
				s.AllSettingsByKey.Add(setting.SettingsKey, setting);
				s.AllSettings.Add(setting);
				if (!s.AllOverrides.ContainsKey(setting.SettingsKey))
					s.AllOverrides.Add(setting.SettingsKey, new SettingOverride());
			}
			SettingsInfo = s;
		}

		private static readonly IReadOnlyDictionary<string, SettingOverride> ExtraOverrides = new Dictionary<string, SettingOverride>
		{
			{ "nyma.constantfb", new() { NonSync = true, NoRestart = true } },
			// global setting... needs a value set if it gets used (only use case in practice is ST-V with Saturnus)
			{ "filesys.untrusted_fip_check", new() { Hide = true, Default = "0" } },
		};

		private static readonly IReadOnlyCollection<SettingT> ExtraSettings = new List<SettingT>
		{
			new SettingT
			{
				Name = "Constant Framebuffer Size",
				Description = "Output a constant framebuffer size regardless of internal resolution.",
				SettingsKey = "nyma.constantfb",
				DefaultValue = "0",
				Flags = 0,
				Type = SettingType.Bool
			},
			new SettingT
			{
				Name = "Initial Time",
				Description = "Initial time of emulation.  Only relevant when UseRealTime is false.\nEnter as IS0-8601.",
				SettingsKey = "nyma.rtcinitialtime",
				DefaultValue = "2010-01-01",
				Flags = SettingsFlags.EmuState,
				Type = SettingType.String
			},
			new SettingT
			{
				Name = "Use RealTime",
				Description = "If true, RTC clock will be based off of real time instead of emulated time.  Ignored (set to false) when recording a movie.",
				SettingsKey = "nyma.rtcrealtime",
				DefaultValue = "0",
				Flags = SettingsFlags.EmuState,
				Type = SettingType.Bool
			},
		};

		public class SettingOverride
		{
			/// <summary>
			/// If true, hide from user.  Will always be set to its default value in those cases.
			/// </summary>
			public bool Hide { get; set; }
			/// <summary>
			/// If non-null, replace the original default value with this default value.
			/// </summary>
			public string Default { get; set; }
			/// <summary>
			/// If true, put the setting in Settings and not SyncSettings
			/// </summary>
			public bool NonSync { get; set; }
			/// <summary>
			/// If true, no restart is required to apply the setting.  Only allowed when NonSync == true
			/// DON'T STEAL THIS FROM MEDNAFLAGS, IT'S NOT SET PROPERLY THERE
			/// </summary>
			public bool NoRestart { get; set; } // if true, restart is not required
		}
		protected virtual IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>();
	}
}
