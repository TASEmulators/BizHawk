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
			// SetControllerButtons(); // not safe to change the controller during emulation, so instead make it a reboot event
			return ret;
		}

		internal PCESettings Settings;
		private PCESyncSettings _syncSettings;

		public class PCESettings
		{
			public bool ShowBG1 = true;
			public bool ShowOBJ1 = true;
			public bool ShowBG2 = true;
			public bool ShowOBJ2 = true;

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
			public ControllerSetting[] Controllers =
			{
				new ControllerSetting { IsConnected = true },
				new ControllerSetting { IsConnected = false },
				new ControllerSetting { IsConnected = false },
				new ControllerSetting { IsConnected = false },
				new ControllerSetting { IsConnected = false }
			};

			public PCESyncSettings Clone()
			{
				var ret = new PCESyncSettings();
				for (int i = 0; i < Controllers.Length; i++)
				{
					ret.Controllers[i].IsConnected = Controllers[i].IsConnected;
				}
				return ret;
			}

			public class ControllerSetting
			{
				public bool IsConnected { get; set; }
			}

			public static bool NeedsReboot(PCESyncSettings x, PCESyncSettings y)
			{
				for (int i = 0; i < x.Controllers.Length; i++)
				{
					if (x.Controllers[i].IsConnected != y.Controllers[i].IsConnected)
						return true;
				}
				return false;
			}
		}
	}
}
