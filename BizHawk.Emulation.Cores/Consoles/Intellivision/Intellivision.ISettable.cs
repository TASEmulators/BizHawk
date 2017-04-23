using System;
using Newtonsoft.Json;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public partial class Intellivision : IEmulator, IStatable, ISettable<Intellivision.IntvSettings, Intellivision.IntvSyncSettings>
	{
		public IntvSettings GetSettings()
		{
			return _settings.Clone();
		}

		public IntvSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(IntvSettings o)
		{
			_settings = o;
			return false;
		}

		public bool PutSyncSettings(IntvSyncSettings o)
		{
			bool ret = IntvSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret;
		}

		private IntvSettings _settings = new IntvSettings();
		private IntvSyncSettings _syncSettings = new IntvSyncSettings();

		public class IntvSettings
		{
			public IntvSettings Clone()
			{
				return (IntvSettings)MemberwiseClone();
			}
		}

		public class IntvSyncSettings
		{
			private string _port1 = IntellivisionControllerDeck.DefaultControllerName;
			private string _port2 = IntellivisionControllerDeck.DefaultControllerName;

			[JsonIgnore]
			public string Port1
			{
				get { return _port1; }
				set
				{
					if (!IntellivisionControllerDeck.ValidControllerTypes.ContainsKey(value))
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
					if (!IntellivisionControllerDeck.ValidControllerTypes.ContainsKey(value))
					{
						throw new InvalidOperationException("Invalid controller type: " + value);
					}

					_port2 = value;
				}
			}

			public IntvSyncSettings Clone()
			{
				return (IntvSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(IntvSyncSettings x, IntvSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
