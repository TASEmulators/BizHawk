using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using NymaTypes;

namespace BizHawk.Emulation.Cores.Waterbox
{
	unsafe partial class NymaCore : ISettable<NymaCore.NymaSettings, NymaCore.NymaSyncSettings>
	{
		/// <summary>
		/// Settings that we shouldn't show the user.
		/// If the value is null, use the default value, otherwise override it.
		/// </summary>
		protected virtual IDictionary<string, string> SettingsOverrides { get; } = new Dictionary<string, string>();
		/// <summary>
		/// Add any settings here that your core is not sync sensitive to.  Don't screw up and cause nondeterminism!
		/// </summary>
		protected virtual ISet<string> NonSyncSettingNames { get; } = new HashSet<string>();
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

		protected string SettingsQuery(string name)
		{
			if (SettingsOverrides.TryGetValue(name, out var val))
			{
				// use override
			}
			else
			{
				// try to get actual value from settings
				if (NonSyncSettingNames.Contains(name))
					_settings.MednafenValues.TryGetValue(name, out val);
				else
					_syncSettings.MednafenValues.TryGetValue(name, out val);
			}
			// in either case, might need defaults
			if (val == null)
			{
				SettingsInfo.AllSettingsByKey.TryGetValue(name, out var info);
				val = info?.DefaultValue;
			}
			if (val == null)
			{
				throw new InvalidOperationException($"Core asked for setting {name} which was not found in the defaults");
			}
			return val;
		}

		private void SettingsQuery(string name, IntPtr dest)
		{
			var val = SettingsQuery(name);
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
		public bool HasSettings => SettingsInfo.LayerNames.Count > 0|| SettingsInfo.Settings.Count > 0;
		/// <summary>
		/// If true, the syncSettings object has at least one settable value in it
		/// </summary>
		public bool HasSyncSettings => SettingsInfo.Ports.Count > 0 || SettingsInfo.SyncSettings.Count > 0;

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
			public List<SettingT> Settings { get; set; } = new List<SettingT>();
			public List<SettingT> SyncSettings { get; set; } = new List<SettingT>();
			public Dictionary<string, SettingT> AllSettingsByKey { get; set; } = new Dictionary<string, SettingT>();
		}
		private void InitSyncSettingsInfo(List<NPortInfoT> allPorts)
		{
			var s = new NymaSettingsInfo();

			foreach (var portInfo in allPorts)
			{
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

			foreach (var setting in GetSettingsData().Concat(ExtraSettings))
			{
				s.AllSettingsByKey.Add(setting.SettingsKey, setting);
				if (!SettingsOverrides.ContainsKey(setting.SettingsKey))
				{
					if (NonSyncSettingNames.Contains(setting.SettingsKey))
					{
						s.Settings.Add(setting);
					}
					else
					{
						s.SyncSettings.Add(setting);
					}
				}
			}
			SettingsInfo = s;
		}

		private static IReadOnlyCollection<SettingT> ExtraSettings = new List<SettingT>
		{
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
	}
}
