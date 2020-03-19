using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine : ISettable<PCEngine.PCESettings, PCEngine.PCESyncSettings>
	{
		public PCESettings GetSettings()
		{
			return Settings.Clone();
		}

		public PCESyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(PCESettings o)
		{
			bool ret;
			if (o.ArcadeCardRewindHack != Settings.ArcadeCardRewindHack
				|| o.EqualizeVolume != Settings.EqualizeVolume)
			{
				ret = true;
			}
			else
			{
				ret = false;
			}

			Settings = o;

			return ret;
		}

		public bool PutSyncSettings(PCESyncSettings o)
		{
			bool ret = PCESyncSettings.NeedsReboot(o, _syncSettings);
			_syncSettings = o;
			return ret;
		}

		public PCESettings Settings;
		private PCESyncSettings _syncSettings;

		public class PCESettings
		{
			public bool ShowBG1 = true;
			public bool ShowOBJ1 = true;
			public bool ShowBG2 = true;
			public bool ShowOBJ2 = true;

			// cropping settings
			public int Top_Line = 18;
			public int Bottom_Line = 252;

			// these three require core reboot to use
			public bool SpriteLimit = false;
			public bool EqualizeVolume = false;
			public bool ArcadeCardRewindHack = true;

			public PCESettings Clone()
			{
				return (PCESettings)MemberwiseClone();
			}
		}

		public class PCESyncSettings
		{
			[DefaultValue(PceControllerType.GamePad)]
			[DisplayName("Port 1 Device")]
			[Description("The type of controller plugged into the first controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public PceControllerType Port1 { get; set; } = PceControllerType.GamePad;

			[DefaultValue(PceControllerType.Unplugged)]
			[DisplayName("Port 2 Device")]
			[Description("The type of controller plugged into the second controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public PceControllerType Port2 { get; set; } = PceControllerType.Unplugged;

			[DefaultValue(PceControllerType.Unplugged)]
			[DisplayName("Port 3 Device")]
			[Description("The type of controller plugged into the third controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public PceControllerType Port3 { get; set; } = PceControllerType.Unplugged;

			[DefaultValue(PceControllerType.Unplugged)]
			[DisplayName("Port 4 Device")]
			[Description("The type of controller plugged into the fourth controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public PceControllerType Port4 { get; set; } = PceControllerType.Unplugged;

			[DefaultValue(PceControllerType.Unplugged)]
			[DisplayName("Port 5 Device")]
			[Description("The type of controller plugged into the fifth controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public PceControllerType Port5 { get; set; } = PceControllerType.Unplugged;

			public PCESyncSettings Clone()
			{
				return (PCESyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(PCESyncSettings x, PCESyncSettings y)
			{
				return x.Port1 != y.Port1
					|| x.Port2 != y.Port2
					|| x.Port3 != y.Port3
					|| x.Port4 != y.Port4
					|| x.Port5 != y.Port5;
			}
		}
	}
}
