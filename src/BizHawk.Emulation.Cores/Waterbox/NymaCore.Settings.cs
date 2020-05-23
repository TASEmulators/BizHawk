using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.API.ApiHawk;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using FlatBuffers;
using Newtonsoft.Json;
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
			var forced = SettingsOverrides.TryGetValue(name, out var val);
			if (val == null)
			{
				if (forced || !_syncSettingsActual.MednafenValues.TryGetValue(name, out val))
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
			public List<SettingT> Settings { get; set; } = new List<SettingT>();
			public Dictionary<string, SettingT> SettingsByKey { get; set; } = new Dictionary<string, SettingT>();
			public HashSet<string> HiddenSettings { get; set; } = new HashSet<string>();
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

			foreach (var setting in GetSettingsData())
			{
				s.Settings.Add(setting);
				s.SettingsByKey.Add(setting.SettingsKey, setting);
			}
			s.HiddenSettings = new HashSet<string>(SettingsOverrides.Keys);
			foreach (var ss in ExtraSettings)
			{
				s.Settings.Add(ss);
				s.SettingsByKey.Add(ss.SettingsKey, ss);
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
