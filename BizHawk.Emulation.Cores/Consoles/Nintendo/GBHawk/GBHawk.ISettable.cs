using System;
using System.ComponentModel;

using Newtonsoft.Json;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk : IEmulator, IStatable, ISettable<GBHawk.GBSettings, GBHawk.GBSyncSettings>
	{
		public GBSettings GetSettings()
		{
			return _settings.Clone();
		}

		public GBSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(GBSettings o)
		{
			_settings = o;
			return false;
		}

		public bool PutSyncSettings(GBSyncSettings o)
		{
			bool ret = GBSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret;
		}

		private GBSettings _settings = new GBSettings();
		public GBSyncSettings _syncSettings = new GBSyncSettings();

		public class GBSettings
		{
			public enum PaletteType
			{
				BW,
				Gr
			}

			[DisplayName("Console Mode")]
			[Description("Pick which console to run, 'Auto' chooses from ROM header, 'GB' and 'GBC' chooses the respective system")]
			[DefaultValue(PaletteType.BW)]
			public PaletteType Palette { get; set; }


			public GBSettings Clone()
			{
				return (GBSettings)MemberwiseClone();
			}
		}

		public class GBSyncSettings
		{
			private string _port1 = GBHawkControllerDeck.DefaultControllerName;

			[JsonIgnore]
			public string Port1
			{
				get { return _port1; }
				set
				{
					if (!GBHawkControllerDeck.ValidControllerTypes.ContainsKey(value))
					{
						throw new InvalidOperationException("Invalid controller type: " + value);
					}

					_port1 = value;
				}
			}

			public GBSyncSettings Clone()
			{
				return (GBSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(GBSyncSettings x, GBSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
