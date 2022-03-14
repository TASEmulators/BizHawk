using System.ComponentModel;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64SyncSettings
	{
		public class N64AngrylionPluginSettings : IPluginSettings
		{
			public N64AngrylionPluginSettings()
			{
				BobDeinterlacer = true;
			}

			[DefaultValue(true)]
			[DisplayName("Use Bob Deinterlacer")]
			[Description("Uses Bob Deinterlacer if True, else a Weave Deinterlacer is used")]
			[Category("Video")]
			public bool BobDeinterlacer { get; set; }

			public N64AngrylionPluginSettings Clone()
			{
				return (N64AngrylionPluginSettings)MemberwiseClone();
			}

			public void FillPerGameHacks(GameInfo game)
			{
			}

			public PluginType GetPluginType()
			{
				return PluginType.Angrylion;
			}
		}
	}
}
