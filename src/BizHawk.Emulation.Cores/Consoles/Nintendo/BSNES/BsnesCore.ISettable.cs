using System.Reflection.Emit;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class BsnesCore : ISettable<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings>
	{
		public SnesSettings GetSettings()
		{
			return _settings.Clone();
		}

		public SnesSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(SnesSettings o)
		{
			// bool refreshNeeded = o.Palette != _settings.Palette;
			// _settings = o;
			// if (refreshNeeded)
			// {
			// 	RefreshPalette();
			// }

			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(SnesSyncSettings o)
		{
			bool ret = o.LeftPort != _syncSettings.LeftPort
				|| o.RightPort != _syncSettings.RightPort
				|| o.LimitAnalogChangeSensitivity != _syncSettings.LimitAnalogChangeSensitivity
				|| o.Entropy != _syncSettings.Entropy;

			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private SnesSettings _settings;
		private SnesSyncSettings _syncSettings;

		public class SnesSettings
		{
			// public bool ShowBG1_0 { get; set; } = true;
			// public bool ShowBG2_0 { get; set; } = true;
			// public bool ShowBG3_0 { get; set; } = true;
			// public bool ShowBG4_0 { get; set; } = true;
			// public bool ShowBG1_1 { get; set; } = true;
			// public bool ShowBG2_1 { get; set; } = true;
			// public bool ShowBG3_1 { get; set; } = true;
			// public bool ShowBG4_1 { get; set; } = true;
			// public bool ShowOBJ_0 { get; set; } = true;
			// public bool ShowOBJ_1 { get; set; } = true;
			// public bool ShowOBJ_2 { get; set; } = true;
			// public bool ShowOBJ_3 { get; set; } = true;

			// public bool CropSGBFrame { get; set; } = false;
			// public bool AlwaysDoubleSize { get; set; } = false;

			public SnesSettings Clone()
			{
				return (SnesSettings)MemberwiseClone();
			}
		}

		public class SnesSyncSettings
		{
			public BsnesApi.BSNES_INPUT_DEVICE LeftPort { get; set; } = BsnesApi.BSNES_INPUT_DEVICE.Gamepad;

			public BsnesApi.BSNES_INPUT_DEVICE RightPort { get; set; } = BsnesApi.BSNES_INPUT_DEVICE.None;

			public bool LimitAnalogChangeSensitivity { get; set; } = true;

			public BsnesApi.ENTROPY Entropy = BsnesApi.ENTROPY.Low;

			public SnesSyncSettings Clone()
			{
				return (SnesSyncSettings) MemberwiseClone();
			}
		}

	}
}
