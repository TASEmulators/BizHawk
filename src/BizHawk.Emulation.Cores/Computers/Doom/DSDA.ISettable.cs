using System.ComponentModel;

using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : ISettable<object, DSDA.DoomSyncSettings>
	{
		private DoomSyncSettings _syncSettings;

		public object GetSettings()
			=> null;

		public DoomSyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(object o)
			=> PutSettingsDirtyBits.None;

		public PutSettingsDirtyBits PutSyncSettings(DoomSyncSettings o)
		{
			var ret = DoomSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class DoomSyncSettings
		{
			[DefaultValue(DoomControllerTypes.Joystick)]
			[DisplayName("Port 1 Device")]
			[Description("The type of controller plugged into the first controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public DoomControllerTypes Port1 { get; set; }

			[DefaultValue(DoomControllerTypes.Joystick)]
			[DisplayName("Port 2 Device")]
			[Description("The type of controller plugged into the second controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public DoomControllerTypes Port2 { get; set; }

			public CInterface.InitSettings GetNativeSettings(GameInfo game)
			{
				return new CInterface.InitSettings
				{
					dummy = 1
				};
			}

			public DoomSyncSettings Clone()
				=> (DoomSyncSettings)MemberwiseClone();

			public DoomSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(DoomSyncSettings x, DoomSyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
