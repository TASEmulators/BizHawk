using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.ComponentModel;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class SuperVision : ISettable<object,SuperVision.SuperVisionSyncSettings>
	{
		private SuperVisionSyncSettings _syncSettings;

		public object GetSettings()
			=> null;

		public SuperVisionSyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(object o)
			=> PutSettingsDirtyBits.None;

		public PutSettingsDirtyBits PutSyncSettings(SuperVisionSyncSettings o)
		{
			var ret = SuperVisionSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public enum ScreenType
		{
			BlackAndWhite,
			Amber,
			Green
		}

		[CoreSettings]
		public class SuperVisionSyncSettings
		{
			[DisplayName("ScreenType")]
			[Description("Color of LCD screen")]
			[DefaultValue(ScreenType.BlackAndWhite)]
			public ScreenType ScreenType { get; set; }

			public SuperVisionSyncSettings Clone()
				=> (SuperVisionSyncSettings) MemberwiseClone();

			public SuperVisionSyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public static bool NeedsReboot(SuperVisionSyncSettings x, SuperVisionSyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
