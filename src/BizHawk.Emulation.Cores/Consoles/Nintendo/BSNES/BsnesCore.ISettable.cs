using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public partial class BsnesCore : ISettable<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings>
	{
		// names need to exactly match the firmware db names
		public enum SATELLAVIEW_CARTRIDGE
		{
			Autodetect,
			Rom_BSX,
			Rom_Mahjong,
			Rom_GNEXT,
			Rom_RPG_Tsukuru,
			Rom_SameGame,
			Rom_DS96,
			Rom_Ongaku_Tsukuru,
			Rom_SoundNovel_Tsukuru,
			Rom_Tsuri
		}

		public SnesSettings GetSettings()
		{
			return _settings with {};
		}

		SNES.IBSNESForGfxDebugger.SettingsObj SNES.IBSNESForGfxDebugger.GetSettings()
			=> GetSettings();

		public SnesSyncSettings GetSyncSettings()
		{
			return _syncSettings with {};
		}

		public PutSettingsDirtyBits PutSettings(SnesSettings o)
		{
			if (o != _settings)
			{
				var enables = new BsnesApi.LayerEnables
				{
					BG1_Prio0 = o.ShowBG1_0,
					BG1_Prio1 = o.ShowBG1_1,
					BG2_Prio0 = o.ShowBG2_0,
					BG2_Prio1 = o.ShowBG2_1,
					BG3_Prio0 = o.ShowBG3_0,
					BG3_Prio1 = o.ShowBG3_1,
					BG4_Prio0 = o.ShowBG4_0,
					BG4_Prio1 = o.ShowBG4_1,
					Obj_Prio0 = o.ShowOBJ_0,
					Obj_Prio1 = o.ShowOBJ_1,
					Obj_Prio2 = o.ShowOBJ_2,
					Obj_Prio3 = o.ShowOBJ_3
				};
				Api.core.snes_set_layer_enables(ref enables);
				Api.core.snes_set_ppu_sprite_limit_enabled(!o.NoPPUSpriteLimit);
				Api.core.snes_set_overscan_enabled(o.ShowOverscan);
				Api.core.snes_set_cursor_enabled(o.ShowCursor);
			}
			_settings = o;

			return PutSettingsDirtyBits.None;
		}

		void SNES.IBSNESForGfxDebugger.PutSettings(SNES.IBSNESForGfxDebugger.SettingsObj s)
			=> PutSettings((SnesSettings) s);

		public PutSettingsDirtyBits PutSyncSettings(SnesSyncSettings o)
		{
			bool changed = o != _syncSettings;
			_syncSettings = o;
			return changed ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private SnesSettings _settings;
		private SnesSyncSettings _syncSettings;

		public sealed record class SnesSettings : SNES.IBSNESForGfxDebugger.SettingsObj
		{
			public bool ShowBG1_0 { get; set; } = true;
			public bool ShowBG2_0 { get; set; } = true;
			public bool ShowBG3_0 { get; set; } = true;
			public bool ShowBG4_0 { get; set; } = true;
			public bool ShowBG1_1 { get; set; } = true;
			public bool ShowBG2_1 { get; set; } = true;
			public bool ShowBG3_1 { get; set; } = true;
			public bool ShowBG4_1 { get; set; } = true;
			public bool ShowOBJ_0 { get; set; } = true;
			public bool ShowOBJ_1 { get; set; } = true;
			public bool ShowOBJ_2 { get; set; } = true;
			public bool ShowOBJ_3 { get; set; } = true;

			public bool AlwaysDoubleSize { get; set; }
			public bool CropSGBFrame { get; set; }
			public bool NoPPUSpriteLimit { get; set; }
			public bool ShowOverscan { get; set; }
			public bool ShowCursor { get; set; } = true;
			public BsnesApi.ASPECT_RATIO_CORRECTION AspectRatioCorrection { get; set; } = BsnesApi.ASPECT_RATIO_CORRECTION.Auto;
		}

		public sealed record class SnesSyncSettings
		{
			public BsnesApi.BSNES_PORT1_INPUT_DEVICE LeftPort { get; set; } = BsnesApi.BSNES_PORT1_INPUT_DEVICE.Gamepad;

			public BsnesApi.BSNES_INPUT_DEVICE RightPort { get; set; } = BsnesApi.BSNES_INPUT_DEVICE.None;

			public bool LimitAnalogChangeSensitivity { get; set; } = true;

			public BsnesApi.ENTROPY Entropy { get; set; } = BsnesApi.ENTROPY.Low;

			public BsnesApi.REGION_OVERRIDE RegionOverride { get; set; } = BsnesApi.REGION_OVERRIDE.Auto;

			public bool Hotfixes { get; set; } = true;

			public bool FastPPU { get; set; } = true;

			public bool FastDSP { get; set; } = true;

			public bool FastCoprocessors { get; set; } = true;

			public bool UseSGB2 { get; set; } = true;

			public SATELLAVIEW_CARTRIDGE SatellaviewCartridge { get; set; } = SATELLAVIEW_CARTRIDGE.Autodetect;

			public bool UseRealTime { get; set; } = true;

			public DateTime InitialTime { get; set; } = new(2010, 1, 1);
		}
	}
}
