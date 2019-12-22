using System;
using System.ComponentModel;

using Newtonsoft.Json;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk : IEmulator, IStatable, ISettable<O2Hawk.O2Settings, O2Hawk.O2SyncSettings>
	{
		public O2Settings GetSettings()
		{
			return _settings.Clone();
		}

		public O2SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(O2Settings o)
		{
			_settings = o;
			return false;
		}

		public bool PutSyncSettings(O2SyncSettings o)
		{
			bool ret = O2SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret;
		}

		public O2Settings _settings = new O2Settings();
		public O2SyncSettings _syncSettings = new O2SyncSettings();

		public class O2Settings
		{
			[DisplayName("Display Characters")]
			[Description("When true, displays character.")]
			[DefaultValue(true)]
			public bool Show_Chars { get; set; }

			[DisplayName("Display Quad Characters")]
			[Description("When true, displays quad character.")]
			[DefaultValue(true)]
			public bool Show_Quads { get; set; }

			[DisplayName("Display Sprites")]
			[Description("When true, displays sprites.")]
			[DefaultValue(true)]
			public bool Show_Sprites { get; set; }

			public O2Settings Clone()
			{
				return (O2Settings)MemberwiseClone();
			}
		}

		public class O2SyncSettings
		{
			[DisplayName("Use Existing SaveRAM")]
			[Description("When true, existing SaveRAM will be loaded at boot up")]
			[DefaultValue(false)]
			public bool Use_SRAM { get; set; }

			public O2SyncSettings Clone()
			{
				return (O2SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(O2SyncSettings x, O2SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
