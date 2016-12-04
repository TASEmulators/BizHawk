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
			return Settings.Clone();
		}

		public IntvSyncSettings GetSyncSettings()
		{
			return SyncSettings.Clone();
		}

		public bool PutSettings(IntvSettings o)
		{
			Settings = o;
			return false;
		}

		public bool PutSyncSettings(IntvSyncSettings o)
		{
			bool ret = IntvSyncSettings.NeedsReboot(SyncSettings, o);
			SyncSettings = o;
			return ret;
		}

		public IntvSettings Settings = new IntvSettings();
		public IntvSyncSettings SyncSettings = new IntvSyncSettings();

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
