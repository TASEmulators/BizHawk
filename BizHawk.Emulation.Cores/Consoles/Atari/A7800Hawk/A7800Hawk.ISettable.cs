using System;
using Newtonsoft.Json;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class A7800Hawk : IEmulator, IStatable, ISettable<A7800Hawk.A7800Settings, A7800Hawk.A7800SyncSettings>
	{
		public A7800Settings GetSettings()
		{
			return _settings.Clone();
		}

		public A7800SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(A7800Settings o)
		{
			_settings = o;
			return false;
		}

		public bool PutSyncSettings(A7800SyncSettings o)
		{
			bool ret = A7800SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret;
		}

		private A7800Settings _settings = new A7800Settings();
		public A7800SyncSettings _syncSettings = new A7800SyncSettings();

		public class A7800Settings
		{
			public A7800Settings Clone()
			{
				return (A7800Settings)MemberwiseClone();
			}

			public A7800Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class A7800SyncSettings
		{
			private string _port1 = A7800HawkControllerDeck.DefaultControllerName;
			private string _port2 = A7800HawkControllerDeck.DefaultControllerName;
			private string _Filter = "None";

			[JsonIgnore]
			public string Filter
			{
				get { return _Filter; }
				set
				{
					_Filter = value;
				}
			}

			[JsonIgnore]
			public string Port1
			{
				get { return _port1; }
				set
				{
					if (!A7800HawkControllerDeck.ValidControllerTypes.ContainsKey(value))
					{
						throw new InvalidOperationException("Invalid controller type: " + value);
					}

					_port1 = value;
				}
			}

			[JsonIgnore]
			public string Port2
			{
				get { return _port2; }
				set
				{
					if (!A7800HawkControllerDeck.ValidControllerTypes.ContainsKey(value))
					{
						throw new InvalidOperationException("Invalid controller type: " + value);
					}

					_port2 = value;
				}
			}

			public A7800SyncSettings Clone()
			{
				return (A7800SyncSettings)MemberwiseClone();
			}

			public A7800SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(A7800SyncSettings x, A7800SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
