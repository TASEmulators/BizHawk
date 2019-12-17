using System;
using System.Windows.Forms;

using BizHawk.Common.StringExtensions;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class N64VideoPluginConfig : Form
	{
		private readonly MainForm _mainForm;
		private readonly Config _config;
		private readonly IEmulator _emulator;
		private readonly N64Settings _s;
		private readonly N64SyncSettings _ss;

		private const string CustomResItemName = "Custom";

		private static readonly string[] ValidResolutions =
		{
			"320 x 240",
			"400 x 300",
			"480 x 360",
			"512 x 384",
			"640 x 480",
			"800 x 600",
			"1024 x 768",
			"1152 x 864",
			"1280 x 960",
			"1400 x 1050",
			"1600 x 1200",
			"1920 x 1440",
			"2048 x 1536",
			"2880 x 2160",
			CustomResItemName
		};

		private bool _programmaticallyChangingPluginComboBox = false;

		public N64VideoPluginConfig(
			MainForm mainForm,
			Config config,
			IEmulator emulator)
		{
			_mainForm = mainForm;
			_config = config;
			_emulator = emulator;

			// because mupen is a pile of garbage, this all needs to work even when N64 is not loaded
			if (_emulator is N64 n64)
			{
				_s = n64.GetSettings();
				_ss = n64.GetSyncSettings();
			}
			else
			{
				_s = (N64Settings)_config.GetCoreSettings<N64>()
					?? new N64Settings();
				_ss = (N64SyncSettings)_config.GetCoreSyncSettings<N64>()
					?? new N64SyncSettings();
			}

			InitializeComponent();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			// Add confirmation of cancelling change
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void Button1_Click(object sender, EventArgs e)
		{
			SaveSettings();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void SaveSettings()
		{
			// Global
			if (VideoResolutionComboBox.Text != CustomResItemName)
			{
				var videoSettings = VideoResolutionComboBox.SelectedItem.ToString();
				var strArr = videoSettings.Split('x');
				_s.VideoSizeX = int.Parse(strArr[0].Trim());
				_s.VideoSizeY = int.Parse(strArr[1].Trim());
			}
			else
			{
				_s.VideoSizeX =
					VideoResolutionXTextBox.Text.IsUnsigned() ?
					int.Parse(VideoResolutionXTextBox.Text) : 320;

				_s.VideoSizeY =
					VideoResolutionYTextBox.Text.IsUnsigned() ?
					int.Parse(VideoResolutionYTextBox.Text) : 240;
			}

			switch (PluginComboBox.Text)
			{
				case "Rice":
					_ss.VideoPlugin = PluginType.Rice;
					break;
				case "Glide64":
					_ss.VideoPlugin = PluginType.Glide;
					break;
				case "Glide64mk2":
					_ss.VideoPlugin = PluginType.GlideMk2;
					break;
				case "GLideN64":
					_ss.VideoPlugin = PluginType.GLideN64;
					break;
			}

			// Rice
			_ss.RicePlugin.NormalAlphaBlender = RiceNormalAlphaBlender_CB.Checked;
			_ss.RicePlugin.FastTextureLoading = RiceFastTextureLoading_CB.Checked;
			_ss.RicePlugin.AccurateTextureMapping = RiceAccurateTextureMapping_CB.Checked;
			_ss.RicePlugin.InN64Resolution = RiceInN64Resolution_CB.Checked;
			_ss.RicePlugin.SaveVRAM = RiceSaveVRAM_CB.Checked;
			_ss.RicePlugin.DoubleSizeForSmallTxtrBuf = RiceDoubleSizeForSmallTxtrBuf_CB.Checked;
			_ss.RicePlugin.DefaultCombinerDisable = RiceDefaultCombinerDisable_CB.Checked;
			_ss.RicePlugin.EnableHacks = RiceEnableHacks_CB.Checked;
			_ss.RicePlugin.WinFrameMode = RiceWinFrameMode_CB.Checked;
			_ss.RicePlugin.FullTMEMEmulation = RiceFullTMEMEmulation_CB.Checked;
			_ss.RicePlugin.OpenGLVertexClipper = RiceOpenGLVertexClipper_CB.Checked;
			_ss.RicePlugin.EnableSSE = RiceEnableSSE_CB.Checked;
			_ss.RicePlugin.EnableVertexShader = RiceEnableVertexShader_CB.Checked;
			_ss.RicePlugin.SkipFrame = RiceSkipFrame_CB.Checked;
			_ss.RicePlugin.TexRectOnly = RiceTexRectOnly_CB.Checked;
			_ss.RicePlugin.SmallTextureOnly = RiceSmallTextureOnly_CB.Checked;
			_ss.RicePlugin.LoadHiResCRCOnly = RiceLoadHiResCRCOnly_CB.Checked;
			_ss.RicePlugin.LoadHiResTextures = RiceLoadHiResTextures_CB.Checked;
			_ss.RicePlugin.DumpTexturesToFiles = RiceDumpTexturesToFiles_CB.Checked;

			_ss.RicePlugin.FrameBufferSetting = RiceFrameBufferSetting_Combo.SelectedIndex;
			_ss.RicePlugin.FrameBufferWriteBackControl = RiceFrameBufferWriteBackControl_Combo.SelectedIndex;
			_ss.RicePlugin.RenderToTexture = RiceRenderToTexture_Combo.SelectedIndex;
			_ss.RicePlugin.ScreenUpdateSetting = RiceScreenUpdateSetting_Combo.SelectedIndex;
			_ss.RicePlugin.Mipmapping = RiceMipmapping_Combo.SelectedIndex;
			_ss.RicePlugin.FogMethod = RiceFogMethod_Combo.SelectedIndex;
			_ss.RicePlugin.ForceTextureFilter = RiceForceTextureFilter_Combo.SelectedIndex;
			_ss.RicePlugin.TextureEnhancement = RiceTextureEnhancement_Combo.SelectedIndex;
			_ss.RicePlugin.TextureEnhancementControl = RiceTextureEnhancementControl_Combo.SelectedIndex;
			_ss.RicePlugin.TextureQuality = RiceTextureQuality_Combo.SelectedIndex;
			_ss.RicePlugin.OpenGLDepthBufferSetting = (RiceOpenGLDepthBufferSetting_Combo.SelectedIndex + 1) * 16;
			switch (RiceMultiSampling_Combo.SelectedIndex)
			{
				case 0:
					_ss.RicePlugin.MultiSampling = 0;
					break;
				case 1:
					_ss.RicePlugin.MultiSampling = 2;
					break;
				case 2:
					_ss.RicePlugin.MultiSampling = 4;
					break;
				case 3:
					_ss.RicePlugin.MultiSampling = 8;
					break;
				case 4:
					_ss.RicePlugin.MultiSampling = 16;
					break;
				default:
					_ss.RicePlugin.MultiSampling = 0;
					break;
			}

			_ss.RicePlugin.ColorQuality = RiceColorQuality_Combo.SelectedIndex;
			_ss.RicePlugin.OpenGLRenderSetting = RiceOpenGLRenderSetting_Combo.SelectedIndex;
			_ss.RicePlugin.AnisotropicFiltering = RiceAnisotropicFiltering_TB.Value;

			_ss.RicePlugin.UseDefaultHacks = RiceUseDefaultHacks_CB.Checked;
			_ss.RicePlugin.DisableTextureCRC = RiceDisableTextureCRC_CB.Checked;
			_ss.RicePlugin.DisableCulling = RiceDisableCulling_CB.Checked;
			_ss.RicePlugin.IncTexRectEdge = RiceIncTexRectEdge_CB.Checked;
			_ss.RicePlugin.ZHack = RiceZHack_CB.Checked;
			_ss.RicePlugin.TextureScaleHack = RiceTextureScaleHack_CB.Checked;
			_ss.RicePlugin.PrimaryDepthHack = RicePrimaryDepthHack_CB.Checked;
			_ss.RicePlugin.Texture1Hack = RiceTexture1Hack_CB.Checked;
			_ss.RicePlugin.FastLoadTile = RiceFastLoadTile_CB.Checked;
			_ss.RicePlugin.UseSmallerTexture = RiceUseSmallerTexture_CB.Checked;

			_ss.RicePlugin.VIWidth = RiceVIWidth_Text.Text.IsSigned()
				? int.Parse(RiceVIWidth_Text.Text)
				: -1;

			_ss.RicePlugin.VIHeight = RiceVIHeight_Text.Text.IsSigned()
				? int.Parse(RiceVIHeight_Text.Text)
				: -1;

			_ss.RicePlugin.UseCIWidthAndRatio = RiceUseCIWidthAndRatio_Combo.SelectedIndex;
			_ss.RicePlugin.FullTMEM = RiceFullTMEM_Combo.SelectedIndex;
			_ss.RicePlugin.TxtSizeMethod2 = RiceTxtSizeMethod2_CB.Checked;
			_ss.RicePlugin.EnableTxtLOD = RiceEnableTxtLOD_CB.Checked;
			_ss.RicePlugin.FastTextureCRC = RiceFastTextureCRC_Combo.SelectedIndex;
			_ss.RicePlugin.EmulateClear = RiceEmulateClear_CB.Checked;
			_ss.RicePlugin.ForceScreenClear = RiceForceScreenClear_CB.Checked;
			_ss.RicePlugin.AccurateTextureMappingHack = RiceAccurateTextureMappingHack_Combo.SelectedIndex;
			_ss.RicePlugin.NormalBlender = RiceNormalBlender_Combo.SelectedIndex;
			_ss.RicePlugin.DisableBlender = RiceDisableBlender_CB.Checked;
			_ss.RicePlugin.ForceDepthBuffer = RiceForceDepthBuffer_CB.Checked;
			_ss.RicePlugin.DisableObjBG = RiceDisableObjBG_CB.Checked;
			_ss.RicePlugin.FrameBufferOption = RiceFrameBufferOption_Combo.SelectedIndex;
			_ss.RicePlugin.RenderToTextureOption = RiceRenderToTextureOption_Combo.SelectedIndex;
			_ss.RicePlugin.ScreenUpdateSettingHack = RiceScreenUpdateSettingHack_Combo.SelectedIndex;
			_ss.RicePlugin.EnableHacksForGame = RiceEnableHacksForGame_Combo.SelectedIndex;

			_ss.GlidePlugin.autodetect_ucode = Glide_autodetect_ucode.Checked;
			_ss.GlidePlugin.ucode = Glide_ucode.SelectedIndex;
			_ss.GlidePlugin.flame_corona = Glide_flame_corona.Checked;
			_ss.GlidePlugin.card_id = Glide_card_id.SelectedIndex;
			_ss.GlidePlugin.tex_filter = Glide_tex_filter.SelectedIndex;
			_ss.GlidePlugin.wireframe = Glide_wireframe.Checked;
			_ss.GlidePlugin.wfmode = Glide_wfmode.SelectedIndex;
			_ss.GlidePlugin.fast_crc = Glide_fast_crc.Checked;
			_ss.GlidePlugin.filter_cache = Glide_filter_cache.Checked;
			_ss.GlidePlugin.unk_as_red = Glide_unk_as_red.Checked;
			_ss.GlidePlugin.fb_read_always = Glide_fb_read_always.Checked;
			_ss.GlidePlugin.motionblur = Glide_motionblur.Checked;
			_ss.GlidePlugin.fb_render = Glide_fb_render.Checked;
			_ss.GlidePlugin.noditheredalpha = Glide_noditheredalpha.Checked;
			_ss.GlidePlugin.noglsl = Glide_noglsl.Checked;
			_ss.GlidePlugin.fbo = Glide_fbo.Checked;
			_ss.GlidePlugin.disable_auxbuf = Glide_disable_auxbuf.Checked;
			_ss.GlidePlugin.fb_get_info = Glide_fb_get_info.Checked;

			_ss.GlidePlugin.offset_x = Glide_offset_x.Text.IsSigned()
				? int.Parse(Glide_offset_x.Text)
				: 0;

			_ss.GlidePlugin.offset_y = Glide_offset_y.Text.IsSigned()
				? int.Parse(Glide_offset_y.Text)
				: 0;

			_ss.GlidePlugin.scale_x = Glide_scale_x.Text.IsSigned()
				? int.Parse(Glide_scale_x.Text)
				: 100000;

			_ss.GlidePlugin.scale_y = Glide_scale_y.Text.IsSigned()
				? int.Parse(Glide_scale_y.Text)
				: 100000;

			_ss.GlidePlugin.UseDefaultHacks = GlideUseDefaultHacks1.Checked || GlideUseDefaultHacks2.Checked;
			_ss.GlidePlugin.alt_tex_size = Glide_alt_tex_size.Checked;
			_ss.GlidePlugin.buff_clear = Glide_buff_clear.Checked;
			_ss.GlidePlugin.decrease_fillrect_edge = Glide_decrease_fillrect_edge.Checked;
			_ss.GlidePlugin.detect_cpu_write = Glide_detect_cpu_write.Checked;
			_ss.GlidePlugin.fb_clear = Glide_fb_clear.Checked;
			_ss.GlidePlugin.fb_hires = Glide_fb_hires.Checked;
			_ss.GlidePlugin.fb_read_alpha = Glide_fb_read_alpha.Checked;
			_ss.GlidePlugin.fb_smart = Glide_fb_smart.Checked;
			_ss.GlidePlugin.fillcolor_fix = Glide_fillcolor_fix.Checked;
			_ss.GlidePlugin.fog = Glide_fog.Checked;
			_ss.GlidePlugin.force_depth_compare = Glide_force_depth_compare.Checked;
			_ss.GlidePlugin.force_microcheck = Glide_force_microcheck.Checked;
			_ss.GlidePlugin.fb_hires_buf_clear = Glide_fb_hires_buf_clear.Checked;
			_ss.GlidePlugin.fb_ignore_aux_copy = Glide_fb_ignore_aux_copy.Checked;
			_ss.GlidePlugin.fb_ignore_previous = Glide_fb_ignore_previous.Checked;
			_ss.GlidePlugin.increase_primdepth = Glide_increase_primdepth.Checked;
			_ss.GlidePlugin.increase_texrect_edge = Glide_increase_texrect_edge.Checked;
			_ss.GlidePlugin.fb_optimize_texrect = Glide_fb_optimize_texrect.Checked;
			_ss.GlidePlugin.fb_optimize_write = Glide_fb_optimize_write.Checked;
			_ss.GlidePlugin.PPL = Glide_PPL.Checked;
			_ss.GlidePlugin.soft_depth_compare = Glide_soft_depth_compare.Checked;
			_ss.GlidePlugin.use_sts1_only = Glide_use_sts1_only.Checked;
			_ss.GlidePlugin.wrap_big_tex = Glide_wrap_big_tex.Checked;

			_ss.GlidePlugin.depth_bias = Glide_depth_bias.Text.IsSigned()
				? int.Parse(Glide_depth_bias.Text)
				: 20;

			_ss.GlidePlugin.filtering = Glide_filtering.SelectedIndex;

			_ss.GlidePlugin.fix_tex_coord = Glide_fix_tex_coord.Text.IsSigned()
				? int.Parse(Glide_fix_tex_coord.Text)
				: 0;

			_ss.GlidePlugin.lodmode = Glide_lodmode.SelectedIndex;

			_ss.GlidePlugin.stipple_mode = Glide_stipple_mode.Text.IsSigned()
				? int.Parse(Glide_stipple_mode.Text)
				: 2;

			_ss.GlidePlugin.stipple_pattern = Glide_stipple_pattern.Text.IsSigned()
				? int.Parse(Glide_stipple_pattern.Text)
				: 1041204192;

			_ss.GlidePlugin.swapmode = Glide_swapmode.SelectedIndex;
			_ss.GlidePlugin.enable_hacks_for_game = Glide_enable_hacks_for_game.SelectedIndex;

			_ss.Glide64mk2Plugin.card_id = Glide64mk2_card_id.SelectedIndex;
			_ss.Glide64mk2Plugin.wrpFBO = Glide64mk2_wrpFBO.Checked;
			_ss.Glide64mk2Plugin.wrpAnisotropic = Glide64mk2_wrpAnisotropic.Checked;
			_ss.Glide64mk2Plugin.fb_get_info = Glide64mk2_fb_get_info.Checked;
			_ss.Glide64mk2Plugin.fb_render = Glide64mk2_fb_render.Checked;

			_ss.Glide64mk2Plugin.UseDefaultHacks = Glide64mk2_UseDefaultHacks1.Checked || Glide64mk2_UseDefaultHacks2.Checked;

			_ss.Glide64mk2Plugin.use_sts1_only = Glide64mk2_use_sts1_only.Checked;
			_ss.Glide64mk2Plugin.optimize_texrect = Glide64mk2_optimize_texrect.Checked;
			_ss.Glide64mk2Plugin.increase_texrect_edge = Glide64mk2_increase_texrect_edge.Checked;
			_ss.Glide64mk2Plugin.ignore_aux_copy = Glide64mk2_ignore_aux_copy.Checked;
			_ss.Glide64mk2Plugin.hires_buf_clear = Glide64mk2_hires_buf_clear.Checked;
			_ss.Glide64mk2Plugin.force_microcheck = Glide64mk2_force_microcheck.Checked;
			_ss.Glide64mk2Plugin.fog = Glide64mk2_fog.Checked;
			_ss.Glide64mk2Plugin.fb_smart = Glide64mk2_fb_smart.Checked;
			_ss.Glide64mk2Plugin.fb_read_alpha = Glide64mk2_fb_read_alpha.Checked;
			_ss.Glide64mk2Plugin.fb_hires = Glide64mk2_fb_hires.Checked;
			_ss.Glide64mk2Plugin.detect_cpu_write = Glide64mk2_detect_cpu_write.Checked;
			_ss.Glide64mk2Plugin.decrease_fillrect_edge = Glide64mk2_decrease_fillrect_edge.Checked;
			_ss.Glide64mk2Plugin.buff_clear = Glide64mk2_buff_clear.Checked;
			_ss.Glide64mk2Plugin.alt_tex_size = Glide64mk2_alt_tex_size.Checked;
			_ss.Glide64mk2Plugin.swapmode = Glide64mk2_swapmode.SelectedIndex;

			_ss.Glide64mk2Plugin.stipple_pattern = Glide64mk2_stipple_pattern.Text.IsSigned()
				? int.Parse(Glide64mk2_stipple_pattern.Text)
				: 1041204192;

			_ss.Glide64mk2Plugin.stipple_mode = Glide64mk2_stipple_mode.Text.IsSigned()
				? int.Parse(Glide64mk2_stipple_mode.Text)
				: 2;

			_ss.Glide64mk2Plugin.lodmode = Glide64mk2_lodmode.SelectedIndex;
			_ss.Glide64mk2Plugin.filtering = Glide64mk2_filtering.SelectedIndex;
			_ss.Glide64mk2Plugin.correct_viewport = Glide64mk2_correct_viewport.Checked;
			_ss.Glide64mk2Plugin.force_calc_sphere = Glide64mk2_force_calc_sphere.Checked;
			_ss.Glide64mk2Plugin.pal230 = Glide64mk2_pal230.Checked;
			_ss.Glide64mk2Plugin.texture_correction = Glide64mk2_texture_correction.Checked;
			_ss.Glide64mk2Plugin.n64_z_scale = Glide64mk2_n64_z_scale.Checked;
			_ss.Glide64mk2Plugin.old_style_adither = Glide64mk2_old_style_adither.Checked;
			_ss.Glide64mk2Plugin.zmode_compare_less = Glide64mk2_zmode_compare_less.Checked;
			_ss.Glide64mk2Plugin.adjust_aspect = Glide64mk2_adjust_aspect.Checked;
			_ss.Glide64mk2Plugin.clip_zmax = Glide64mk2_clip_zmax.Checked;
			_ss.Glide64mk2Plugin.clip_zmin = Glide64mk2_clip_zmin.Checked;
			_ss.Glide64mk2Plugin.force_quad3d = Glide64mk2_force_quad3d.Checked;
			_ss.Glide64mk2Plugin.useless_is_useless = Glide64mk2_useless_is_useless.Checked;
			_ss.Glide64mk2Plugin.fb_read_always = Glide64mk2_fb_read_always.Checked;
			_ss.Glide64mk2Plugin.aspectmode = Glide64mk2_aspectmode.SelectedIndex;
			_ss.Glide64mk2Plugin.fb_crc_mode = Glide64mk2_fb_crc_mode.SelectedIndex;
			_ss.Glide64mk2Plugin.enable_hacks_for_game = Glide64mk2_enable_hacks_for_game.SelectedIndex;
			_ss.Glide64mk2Plugin.read_back_to_screen = Glide64mk2_read_back_to_screen.SelectedIndex;
			_ss.Glide64mk2Plugin.fast_crc = Glide64mk2_fast_crc.Checked;

			_ss.GLideN64Plugin.UseDefaultHacks = GLideN64_UseDefaultHacks.Checked;

			switch (GLideN64_MultiSampling.SelectedIndex)
			{
				case 0:
					_ss.GLideN64Plugin.MultiSampling = 0;
					break;
				case 1:
					_ss.GLideN64Plugin.MultiSampling = 2;
					break;
				case 2:
					_ss.GLideN64Plugin.MultiSampling = 4;
					break;
				case 3:
					_ss.GLideN64Plugin.MultiSampling = 8;
					break;
				case 4:
					_ss.GLideN64Plugin.MultiSampling = 16;
					break;
				default:
					_ss.GLideN64Plugin.MultiSampling = 0;
					break;
			}

			_ss.GLideN64Plugin.AspectRatio = GLideN64_AspectRatio.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.AspectRatioMode>();
			_ss.GLideN64Plugin.BufferSwapMode = GLideN64_BufferSwapMode.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.SwapMode>();
			_ss.GLideN64Plugin.UseNativeResolutionFactor = GLideN64_UseNativeResolutionFactor.Text.IsSigned()
				? int.Parse(GLideN64_UseNativeResolutionFactor.Text)
				: 0;
			_ss.GLideN64Plugin.bilinearMode = GLideN64_bilinearMode.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.bilinearFilteringMode>();
			_ss.GLideN64Plugin.enableHalosRemoval = GLideN64_enableHalosRemoval.Checked;
			_ss.GLideN64Plugin.MaxAnisotropy = GLideN64_MaxAnisotropy.Checked;
			_ss.GLideN64Plugin.CacheSize = GLideN64_CacheSize.Text.IsSigned()
				? int.Parse(GLideN64_CacheSize.Text)
				: 500;
			_ss.GLideN64Plugin.ShowInternalResolution = GLideN64_ShowInternalResolution.Checked;
			_ss.GLideN64Plugin.ShowRenderingResolution = GLideN64_ShowRenderingResolution.Checked;
			_ss.GLideN64Plugin.FXAA = GLideN64_FXAA.Checked;
			_ss.GLideN64Plugin.EnableNoise = GLideN64_EnableNoise.Checked;
			_ss.GLideN64Plugin.EnableLOD = GLideN64_EnableLOD.Checked;
			_ss.GLideN64Plugin.EnableHWLighting = GLideN64_HWLighting.Checked;
			_ss.GLideN64Plugin.EnableShadersStorage = GLideN64_ShadersStorage.Checked;
			_ss.GLideN64Plugin.CorrectTexrectCoords = GLideN64_CorrectTexrectCoords.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.TexrectCoordsMode>();
			_ss.GLideN64Plugin.EnableNativeResTexrects = GLideN64_NativeResTexrects.Checked;
			_ss.GLideN64Plugin.EnableLegacyBlending = GLideN64_LegacyBlending.Checked;
			_ss.GLideN64Plugin.EnableFragmentDepthWrite = GLideN64_FragmentDepthWrite.Checked;
			_ss.GLideN64Plugin.EnableFBEmulation = GLideN64_EnableFBEmulation.Checked;
			_ss.GLideN64Plugin.DisableFBInfo = GLideN64_DisableFBInfo.Checked;
			_ss.GLideN64Plugin.FBInfoReadColorChunk = GLideN64_FBInfoReadColorChunk.Checked;
			_ss.GLideN64Plugin.FBInfoReadDepthChunk = GLideN64_FBInfoReadDepthChunk.Checked;
			_ss.GLideN64Plugin.txFilterMode = GLideN64_txFilterMode.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.TextureFilterMode>();
			_ss.GLideN64Plugin.txEnhancementMode = GLideN64_txEnhancementMode.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.TextureEnhancementMode>();
			_ss.GLideN64Plugin.txDeposterize = GLideN64_txDeposterize.Checked;
			_ss.GLideN64Plugin.txFilterIgnoreBG = GLideN64_txFilterIgnoreBG.Checked;
			_ss.GLideN64Plugin.txCacheSize = GLideN64_txCacheSize.Text.IsSigned()
				? int.Parse(GLideN64_txCacheSize.Text)
				: 100;
			_ss.GLideN64Plugin.txHiresEnable = GLideN64_txHiresEnable.Checked;
			_ss.GLideN64Plugin.txHiresFullAlphaChannel = GLideN64_txHiresFullAlphaChannel.Checked;
			_ss.GLideN64Plugin.txHresAltCRC = GLideN64_txHresAltCRC.Checked;
			_ss.GLideN64Plugin.txDump = GLideN64_txDump.Checked;
			_ss.GLideN64Plugin.txCacheCompression = GLideN64_txCacheCompression.Checked;
			_ss.GLideN64Plugin.txForce16bpp = GLideN64_txForce16bpp.Checked;
			_ss.GLideN64Plugin.txSaveCache = GLideN64_txSaveCache.Checked;
			_ss.GLideN64Plugin.txPath = GLideN64_txPath.Text;
			_ss.GLideN64Plugin.EnableBloom = GLideN64_EnableBloom.Checked;
			_ss.GLideN64Plugin.bloomThresholdLevel = GLideN64_bloomThresholdLevel.SelectedIndex + 2;
			_ss.GLideN64Plugin.bloomBlendMode = GLideN64_bloomBlendMode.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.BlendMode>();
			_ss.GLideN64Plugin.blurAmount = GLideN64_blurAmount.SelectedIndex + 2;
			_ss.GLideN64Plugin.blurStrength = GLideN64_blurStrength.SelectedIndex + 10;
			_ss.GLideN64Plugin.ForceGammaCorrection = GLideN64_ForceGammaCorrection.Checked;
			_ss.GLideN64Plugin.GammaCorrectionLevel = GLideN64_GammaCorrectionLevel.Text.IsFloat()
				? float.Parse(GLideN64_GammaCorrectionLevel.Text)
				: 2.0f;

			_ss.GLideN64Plugin.EnableOverscan = GLideN64_EnableOverscan.Checked;
			_ss.GLideN64Plugin.OverscanNtscTop = GLideN64_OverscanNtscTop.Text.IsSigned()
				? int.Parse(GLideN64_OverscanNtscTop.Text)
				: 0;
			_ss.GLideN64Plugin.OverscanNtscBottom = GLideN64_OverscanNtscBottom.Text.IsSigned()
				? int.Parse(GLideN64_OverscanNtscBottom.Text)
				: 0;
			_ss.GLideN64Plugin.OverscanNtscLeft = GLideN64_OverscanNtscLeft.Text.IsSigned()
				? int.Parse(GLideN64_OverscanNtscLeft.Text)
				: 0;
			_ss.GLideN64Plugin.OverscanNtscRight = GLideN64_OverscanNtscRight.Text.IsSigned()
				? int.Parse(GLideN64_OverscanNtscRight.Text)
				: 0;
			_ss.GLideN64Plugin.OverscanPalTop = GLideN64_OverscanPalTop.Text.IsSigned()
				? int.Parse(GLideN64_OverscanPalTop.Text)
				: 0;
			_ss.GLideN64Plugin.OverscanPalBottom = GLideN64_OverscanPalBottom.Text.IsSigned()
				? int.Parse(GLideN64_OverscanPalBottom.Text)
				: 0;
			_ss.GLideN64Plugin.OverscanPalLeft = GLideN64_OverscanPalLeft.Text.IsSigned()
				? int.Parse(GLideN64_OverscanPalLeft.Text)
				: 0;
			_ss.GLideN64Plugin.OverscanPalRight = GLideN64_OverscanPalRight.Text.IsSigned()
				? int.Parse(GLideN64_OverscanPalRight.Text)
				: 0;

			_ss.GLideN64Plugin.EnableN64DepthCompare = GLideN64_EnableN64DepthCompare.Checked;
			_ss.GLideN64Plugin.EnableCopyColorToRDRAM = GLideN64_EnableCopyColorToRDRAM.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.CopyColorToRDRAMMode>();
			_ss.GLideN64Plugin.EnableCopyDepthToRDRAM = GLideN64_EnableCopyDepthToRDRAM.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.CopyDepthToRDRAMMode>();
			_ss.GLideN64Plugin.EnableCopyColorFromRDRAM = GLideN64_EnableCopyColorFromRDRAM.Checked;
			_ss.GLideN64Plugin.EnableCopyAuxiliaryToRDRAM = GLideN64_EnableCopyAuxiliaryToRDRAM.Checked;

			_ss.Core = CoreTypeDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.CoreType>();

			_ss.Rsp = RspTypeDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.RspType>();

			if (_emulator is N64)
			{
				_mainForm.PutCoreSettings(_s);
				_mainForm.PutCoreSyncSettings(_ss);
			}
			else
			{
				_config.PutCoreSettings<N64>(_s);
				_config.PutCoreSyncSettings<N64>(_ss);
			}
		} 

		private void N64VideoPluginConfig_Load(object sender, EventArgs e)
		{
			CoreTypeDropdown.PopulateFromEnum<N64SyncSettings.CoreType>(_ss.Core);
			RspTypeDropdown.PopulateFromEnum<N64SyncSettings.RspType>(_ss.Rsp);

			switch (_ss.VideoPlugin)
			{
				case PluginType.GlideMk2:
					PluginComboBox.Text = "Glide64mk2";
					break;
				case PluginType.Glide:
					PluginComboBox.Text = "Glide64";
					break;
				case PluginType.Rice:
					PluginComboBox.Text = "Rice";
					break;
				case PluginType.GLideN64:
					PluginComboBox.Text = "GLideN64";
					break;
			}

			VideoResolutionXTextBox.Text = _s.VideoSizeX.ToString();
			VideoResolutionYTextBox.Text = _s.VideoSizeY.ToString();

			var videoSetting = $"{_s.VideoSizeX} x {_s.VideoSizeY}";

			var index = VideoResolutionComboBox.Items.IndexOf(videoSetting);
			if (index >= 0)
			{
				VideoResolutionComboBox.SelectedIndex = index;
			}
			else if (PluginComboBox.SelectedIndex != 4) // wtf
			{
				VideoResolutionComboBox.SelectedIndex =
					VideoResolutionComboBox.Items.IndexOf(CustomResItemName);
				ShowCustomVideoResolutionControls();
			}

			// Rice
			RiceNormalAlphaBlender_CB.Checked = _ss.RicePlugin.NormalAlphaBlender;
			RiceFastTextureLoading_CB.Checked = _ss.RicePlugin.FastTextureLoading;
			RiceAccurateTextureMapping_CB.Checked = _ss.RicePlugin.AccurateTextureMapping;
			RiceInN64Resolution_CB.Checked = _ss.RicePlugin.InN64Resolution;
			RiceSaveVRAM_CB.Checked = _ss.RicePlugin.SaveVRAM;
			RiceDoubleSizeForSmallTxtrBuf_CB.Checked = _ss.RicePlugin.DoubleSizeForSmallTxtrBuf;
			RiceDefaultCombinerDisable_CB.Checked = _ss.RicePlugin.DefaultCombinerDisable;
			RiceEnableHacks_CB.Checked = _ss.RicePlugin.EnableHacks;
			RiceWinFrameMode_CB.Checked = _ss.RicePlugin.WinFrameMode;
			RiceFullTMEMEmulation_CB.Checked = _ss.RicePlugin.FullTMEMEmulation;
			RiceOpenGLVertexClipper_CB.Checked = _ss.RicePlugin.OpenGLVertexClipper;
			RiceEnableSSE_CB.Checked = _ss.RicePlugin.EnableSSE;
			RiceEnableVertexShader_CB.Checked = _ss.RicePlugin.EnableVertexShader;
			RiceSkipFrame_CB.Checked = _ss.RicePlugin.SkipFrame;
			RiceTexRectOnly_CB.Checked = _ss.RicePlugin.TexRectOnly;
			RiceSmallTextureOnly_CB.Checked = _ss.RicePlugin.SmallTextureOnly;
			RiceLoadHiResCRCOnly_CB.Checked = _ss.RicePlugin.LoadHiResCRCOnly;
			RiceLoadHiResTextures_CB.Checked = _ss.RicePlugin.LoadHiResTextures;
			RiceDumpTexturesToFiles_CB.Checked = _ss.RicePlugin.DumpTexturesToFiles;

			RiceFrameBufferSetting_Combo.SelectedIndex = _ss.RicePlugin.FrameBufferSetting;
			RiceFrameBufferWriteBackControl_Combo.SelectedIndex = _ss.RicePlugin.FrameBufferWriteBackControl;
			RiceRenderToTexture_Combo.SelectedIndex = _ss.RicePlugin.RenderToTexture;
			RiceScreenUpdateSetting_Combo.SelectedIndex = _ss.RicePlugin.ScreenUpdateSetting;
			RiceMipmapping_Combo.SelectedIndex = _ss.RicePlugin.Mipmapping;
			RiceFogMethod_Combo.SelectedIndex = _ss.RicePlugin.FogMethod;
			RiceForceTextureFilter_Combo.SelectedIndex = _ss.RicePlugin.ForceTextureFilter;
			RiceTextureEnhancement_Combo.SelectedIndex = _ss.RicePlugin.TextureEnhancement;
			RiceTextureEnhancementControl_Combo.SelectedIndex = _ss.RicePlugin.TextureEnhancementControl;
			RiceTextureQuality_Combo.SelectedIndex = _ss.RicePlugin.TextureQuality;
			RiceOpenGLDepthBufferSetting_Combo.SelectedIndex = (_ss.RicePlugin.OpenGLDepthBufferSetting / 16) - 1;
			switch (_ss.RicePlugin.MultiSampling)
			{
				case 0:
					RiceMultiSampling_Combo.SelectedIndex = 0;
					break;
				case 2:
					RiceMultiSampling_Combo.SelectedIndex = 1;
					break;
				case 4:
					RiceMultiSampling_Combo.SelectedIndex = 2;
					break;
				case 8:
					RiceMultiSampling_Combo.SelectedIndex = 3;
					break;
				case 16:
					RiceMultiSampling_Combo.SelectedIndex = 4;
					break;
				default:
					RiceMultiSampling_Combo.SelectedIndex = 0;
					break;
			}

			RiceColorQuality_Combo.SelectedIndex = _ss.RicePlugin.ColorQuality;
			RiceOpenGLRenderSetting_Combo.SelectedIndex = _ss.RicePlugin.OpenGLRenderSetting;
			RiceAnisotropicFiltering_TB.Value = _ss.RicePlugin.AnisotropicFiltering;
			AnisotropicFiltering_LB.Text = $"Anisotropic Filtering: {RiceAnisotropicFiltering_TB.Value}";

			RiceUseDefaultHacks_CB.Checked = _ss.RicePlugin.UseDefaultHacks;

			UpdateRiceHacksSection();
			if (!_ss.RicePlugin.UseDefaultHacks)
			{
				RiceTexture1Hack_CB.Checked = _ss.RicePlugin.Texture1Hack;

				RiceDisableTextureCRC_CB.Checked = _ss.RicePlugin.DisableTextureCRC;
				RiceDisableCulling_CB.Checked = _ss.RicePlugin.DisableCulling;
				RiceIncTexRectEdge_CB.Checked = _ss.RicePlugin.IncTexRectEdge;
				RiceZHack_CB.Checked = _ss.RicePlugin.ZHack;
				RiceTextureScaleHack_CB.Checked = _ss.RicePlugin.TextureScaleHack;
				RicePrimaryDepthHack_CB.Checked = _ss.RicePlugin.PrimaryDepthHack;
				RiceTexture1Hack_CB.Checked = _ss.RicePlugin.Texture1Hack;
				RiceFastLoadTile_CB.Checked = _ss.RicePlugin.FastLoadTile;
				RiceUseSmallerTexture_CB.Checked = _ss.RicePlugin.UseSmallerTexture;
				RiceVIWidth_Text.Text = _ss.RicePlugin.VIWidth.ToString();
				RiceVIHeight_Text.Text = _ss.RicePlugin.VIHeight.ToString();
				RiceUseCIWidthAndRatio_Combo.SelectedIndex = _ss.RicePlugin.UseCIWidthAndRatio;
				RiceFullTMEM_Combo.SelectedIndex = _ss.RicePlugin.FullTMEM;
				RiceTxtSizeMethod2_CB.Checked = _ss.RicePlugin.TxtSizeMethod2;
				RiceEnableTxtLOD_CB.Checked = _ss.RicePlugin.EnableTxtLOD;
				RiceFastTextureCRC_Combo.SelectedIndex = _ss.RicePlugin.FastTextureCRC;
				RiceEmulateClear_CB.Checked = _ss.RicePlugin.EmulateClear;
				RiceForceScreenClear_CB.Checked = _ss.RicePlugin.ForceScreenClear;
				RiceAccurateTextureMappingHack_Combo.SelectedIndex = _ss.RicePlugin.AccurateTextureMappingHack;
				RiceNormalBlender_Combo.SelectedIndex = _ss.RicePlugin.NormalBlender;
				RiceDisableBlender_CB.Checked = _ss.RicePlugin.DisableBlender;
				RiceForceDepthBuffer_CB.Checked = _ss.RicePlugin.ForceDepthBuffer;
				RiceDisableObjBG_CB.Checked = _ss.RicePlugin.DisableObjBG;
				RiceFrameBufferOption_Combo.SelectedIndex = _ss.RicePlugin.FrameBufferOption;
				RiceRenderToTextureOption_Combo.SelectedIndex = _ss.RicePlugin.RenderToTextureOption;
				RiceScreenUpdateSettingHack_Combo.SelectedIndex = _ss.RicePlugin.ScreenUpdateSettingHack;
				RiceEnableHacksForGame_Combo.SelectedIndex = _ss.RicePlugin.EnableHacksForGame;
			}

			Glide_autodetect_ucode.Checked = _ss.GlidePlugin.autodetect_ucode;
			Glide_ucode.SelectedIndex = _ss.GlidePlugin.ucode;
			Glide_flame_corona.Checked = _ss.GlidePlugin.flame_corona;
			Glide_card_id.SelectedIndex = _ss.GlidePlugin.card_id;
			Glide_tex_filter.SelectedIndex = _ss.GlidePlugin.tex_filter;
			Glide_wireframe.Checked = _ss.GlidePlugin.wireframe;
			Glide_wfmode.SelectedIndex = _ss.GlidePlugin.wfmode;
			Glide_fast_crc.Checked = _ss.GlidePlugin.fast_crc;
			Glide_filter_cache.Checked = _ss.GlidePlugin.filter_cache;
			Glide_unk_as_red.Checked = _ss.GlidePlugin.unk_as_red;
			Glide_fb_read_always.Checked = _ss.GlidePlugin.fb_read_always;
			Glide_motionblur.Checked = _ss.GlidePlugin.motionblur;
			Glide_fb_render.Checked = _ss.GlidePlugin.fb_render;
			Glide_noditheredalpha.Checked = _ss.GlidePlugin.noditheredalpha;
			Glide_noglsl.Checked = _ss.GlidePlugin.noglsl;
			Glide_fbo.Checked = _ss.GlidePlugin.fbo;
			Glide_disable_auxbuf.Checked = _ss.GlidePlugin.disable_auxbuf;
			Glide_fb_get_info.Checked = _ss.GlidePlugin.fb_get_info;
			Glide_offset_x.Text = _ss.GlidePlugin.offset_x.ToString();
			Glide_offset_y.Text = _ss.GlidePlugin.offset_y.ToString();
			Glide_scale_x.Text = _ss.GlidePlugin.scale_x.ToString();
			Glide_scale_y.Text = _ss.GlidePlugin.scale_y.ToString();
			

			GlideUseDefaultHacks1.Checked = _ss.GlidePlugin.UseDefaultHacks;
			GlideUseDefaultHacks2.Checked = _ss.GlidePlugin.UseDefaultHacks;

			UpdateGlideHacksSection();
			if (!_ss.GlidePlugin.UseDefaultHacks)
			{
				Glide_alt_tex_size.Checked = _ss.GlidePlugin.alt_tex_size;
				Glide_buff_clear.Checked = _ss.GlidePlugin.buff_clear;
				Glide_decrease_fillrect_edge.Checked = _ss.GlidePlugin.decrease_fillrect_edge;
				Glide_detect_cpu_write.Checked = _ss.GlidePlugin.detect_cpu_write;
				Glide_fb_clear.Checked = _ss.GlidePlugin.fb_clear;
				Glide_fb_hires.Checked = _ss.GlidePlugin.fb_hires;
				Glide_fb_read_alpha.Checked = _ss.GlidePlugin.fb_read_alpha;
				Glide_fb_smart.Checked = _ss.GlidePlugin.fb_smart;
				Glide_fillcolor_fix.Checked = _ss.GlidePlugin.fillcolor_fix;
				Glide_fog.Checked = _ss.GlidePlugin.fog;
				Glide_force_depth_compare.Checked = _ss.GlidePlugin.force_depth_compare;
				Glide_force_microcheck.Checked = _ss.GlidePlugin.force_microcheck;
				Glide_fb_hires_buf_clear.Checked = _ss.GlidePlugin.fb_hires_buf_clear;
				Glide_fb_ignore_aux_copy.Checked = _ss.GlidePlugin.fb_ignore_aux_copy;
				Glide_fb_ignore_previous.Checked = _ss.GlidePlugin.fb_ignore_previous;
				Glide_increase_primdepth.Checked = _ss.GlidePlugin.increase_primdepth;
				Glide_increase_texrect_edge.Checked = _ss.GlidePlugin.increase_texrect_edge;
				Glide_fb_optimize_texrect.Checked = _ss.GlidePlugin.fb_optimize_texrect;
				Glide_fb_optimize_write.Checked = _ss.GlidePlugin.fb_optimize_write;
				Glide_PPL.Checked = _ss.GlidePlugin.PPL;
				Glide_soft_depth_compare.Checked = _ss.GlidePlugin.soft_depth_compare;
				Glide_use_sts1_only.Checked = _ss.GlidePlugin.use_sts1_only;
				Glide_wrap_big_tex.Checked = _ss.GlidePlugin.wrap_big_tex;

				Glide_depth_bias.Text = _ss.GlidePlugin.depth_bias.ToString();
				Glide_filtering.SelectedIndex = _ss.GlidePlugin.filtering;
				Glide_fix_tex_coord.Text = _ss.GlidePlugin.fix_tex_coord.ToString();
				Glide_lodmode.SelectedIndex = _ss.GlidePlugin.lodmode;
				Glide_stipple_mode.Text = _ss.GlidePlugin.stipple_mode.ToString();
				Glide_stipple_pattern.Text = _ss.GlidePlugin.stipple_pattern.ToString();
				Glide_swapmode.SelectedIndex = _ss.GlidePlugin.swapmode;
				Glide_enable_hacks_for_game.SelectedIndex = _ss.GlidePlugin.enable_hacks_for_game;
			}

			Glide64mk2_card_id.SelectedIndex = _ss.Glide64mk2Plugin.card_id;
			Glide64mk2_wrpFBO.Checked = _ss.Glide64mk2Plugin.wrpFBO;
			Glide64mk2_wrpAnisotropic.Checked = _ss.Glide64mk2Plugin.wrpAnisotropic;
			Glide64mk2_fb_get_info.Checked = _ss.Glide64mk2Plugin.fb_get_info;
			Glide64mk2_fb_render.Checked = _ss.Glide64mk2Plugin.fb_render;

			Glide64mk2_UseDefaultHacks1.Checked = _ss.Glide64mk2Plugin.UseDefaultHacks;
			Glide64mk2_UseDefaultHacks2.Checked = _ss.Glide64mk2Plugin.UseDefaultHacks;

			UpdateGlide64mk2HacksSection();
			if (!_ss.Glide64mk2Plugin.UseDefaultHacks)
			{
				Glide64mk2_use_sts1_only.Checked = _ss.Glide64mk2Plugin.use_sts1_only;
				Glide64mk2_optimize_texrect.Checked = _ss.Glide64mk2Plugin.optimize_texrect;
				Glide64mk2_increase_texrect_edge.Checked = _ss.Glide64mk2Plugin.increase_texrect_edge;
				Glide64mk2_ignore_aux_copy.Checked = _ss.Glide64mk2Plugin.ignore_aux_copy;
				Glide64mk2_hires_buf_clear.Checked = _ss.Glide64mk2Plugin.hires_buf_clear;
				Glide64mk2_force_microcheck.Checked = _ss.Glide64mk2Plugin.force_microcheck;
				Glide64mk2_fog.Checked = _ss.Glide64mk2Plugin.fog;
				Glide64mk2_fb_smart.Checked = _ss.Glide64mk2Plugin.fb_smart;
				Glide64mk2_fb_read_alpha.Checked = _ss.Glide64mk2Plugin.fb_read_alpha;
				Glide64mk2_fb_hires.Checked = _ss.Glide64mk2Plugin.fb_hires;
				Glide64mk2_detect_cpu_write.Checked = _ss.Glide64mk2Plugin.detect_cpu_write;
				Glide64mk2_decrease_fillrect_edge.Checked = _ss.Glide64mk2Plugin.decrease_fillrect_edge;
				Glide64mk2_buff_clear.Checked = _ss.Glide64mk2Plugin.buff_clear;
				Glide64mk2_alt_tex_size.Checked = _ss.Glide64mk2Plugin.alt_tex_size;
				Glide64mk2_swapmode.SelectedIndex = _ss.Glide64mk2Plugin.swapmode;
				Glide64mk2_stipple_pattern.Text = _ss.Glide64mk2Plugin.stipple_pattern.ToString();
				Glide64mk2_stipple_mode.Text = _ss.Glide64mk2Plugin.stipple_mode.ToString();
				Glide64mk2_lodmode.SelectedIndex = _ss.Glide64mk2Plugin.lodmode;
				Glide64mk2_filtering.SelectedIndex = _ss.Glide64mk2Plugin.filtering;
				Glide64mk2_correct_viewport.Checked = _ss.Glide64mk2Plugin.correct_viewport;
				Glide64mk2_force_calc_sphere.Checked = _ss.Glide64mk2Plugin.force_calc_sphere;
				Glide64mk2_pal230.Checked = _ss.Glide64mk2Plugin.pal230;
				Glide64mk2_texture_correction.Checked = _ss.Glide64mk2Plugin.texture_correction;
				Glide64mk2_n64_z_scale.Checked = _ss.Glide64mk2Plugin.n64_z_scale;
				Glide64mk2_old_style_adither.Checked = _ss.Glide64mk2Plugin.old_style_adither;
				Glide64mk2_zmode_compare_less.Checked = _ss.Glide64mk2Plugin.zmode_compare_less;
				Glide64mk2_adjust_aspect.Checked = _ss.Glide64mk2Plugin.adjust_aspect;
				Glide64mk2_clip_zmax.Checked = _ss.Glide64mk2Plugin.clip_zmax;
				Glide64mk2_clip_zmin.Checked = _ss.Glide64mk2Plugin.clip_zmin;
				Glide64mk2_force_quad3d.Checked = _ss.Glide64mk2Plugin.force_quad3d;
				Glide64mk2_useless_is_useless.Checked = _ss.Glide64mk2Plugin.useless_is_useless;
				Glide64mk2_fb_read_always.Checked = _ss.Glide64mk2Plugin.fb_read_always;
				Glide64mk2_aspectmode.SelectedIndex = _ss.Glide64mk2Plugin.aspectmode;
				Glide64mk2_fb_crc_mode.SelectedIndex = _ss.Glide64mk2Plugin.fb_crc_mode;
				Glide64mk2_enable_hacks_for_game.SelectedIndex = _ss.Glide64mk2Plugin.enable_hacks_for_game;
				Glide64mk2_read_back_to_screen.SelectedIndex = _ss.Glide64mk2Plugin.read_back_to_screen;
				Glide64mk2_fast_crc.Checked = _ss.Glide64mk2Plugin.fast_crc;
			}

			// GLideN64
			GLideN64_UseDefaultHacks.Checked = _ss.GLideN64Plugin.UseDefaultHacks;

			switch (_ss.GLideN64Plugin.MultiSampling)
			{
				case 0: GLideN64_MultiSampling.SelectedIndex = 0; break;
				case 2: GLideN64_MultiSampling.SelectedIndex = 1; break;
				case 4: GLideN64_MultiSampling.SelectedIndex = 2; break;
				case 8: GLideN64_MultiSampling.SelectedIndex = 3; break;
				case 16: GLideN64_MultiSampling.SelectedIndex = 4; break;
				default: GLideN64_MultiSampling.SelectedIndex = 0; break;
			}
			GLideN64_AspectRatio
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.AspectRatioMode>(_ss.GLideN64Plugin.AspectRatio);
			GLideN64_BufferSwapMode
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.SwapMode>(_ss.GLideN64Plugin.BufferSwapMode);
			GLideN64_UseNativeResolutionFactor.Text = _ss.GLideN64Plugin.UseNativeResolutionFactor.ToString();
			GLideN64_bilinearMode
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.bilinearFilteringMode>(_ss.GLideN64Plugin.bilinearMode);
			GLideN64_enableHalosRemoval.Checked = _ss.GLideN64Plugin.enableHalosRemoval;
			GLideN64_MaxAnisotropy.Checked = _ss.GLideN64Plugin.MaxAnisotropy;
			GLideN64_CacheSize.Text = _ss.GLideN64Plugin.CacheSize.ToString();
			GLideN64_ShowInternalResolution.Checked = _ss.GLideN64Plugin.ShowInternalResolution;
			GLideN64_ShowRenderingResolution.Checked = _ss.GLideN64Plugin.ShowRenderingResolution;
			GLideN64_FXAA.Checked = _ss.GLideN64Plugin.FXAA;
			GLideN64_EnableNoise.Checked = _ss.GLideN64Plugin.EnableNoise;
			GLideN64_EnableLOD.Checked = _ss.GLideN64Plugin.EnableLOD;
			GLideN64_HWLighting.Checked = _ss.GLideN64Plugin.EnableHWLighting;
			GLideN64_ShadersStorage.Checked = _ss.GLideN64Plugin.EnableShadersStorage;
			GLideN64_CorrectTexrectCoords
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.TexrectCoordsMode>(_ss.GLideN64Plugin.CorrectTexrectCoords);
			GLideN64_NativeResTexrects.Checked = _ss.GLideN64Plugin.EnableNativeResTexrects;
			GLideN64_LegacyBlending.Checked = _ss.GLideN64Plugin.EnableLegacyBlending;
			GLideN64_FragmentDepthWrite.Checked = _ss.GLideN64Plugin.EnableFragmentDepthWrite;
			GLideN64_EnableFBEmulation.Checked = _ss.GLideN64Plugin.EnableFBEmulation;
			GLideN64_DisableFBInfo.Checked = _ss.GLideN64Plugin.DisableFBInfo;
			GLideN64_FBInfoReadColorChunk.Checked = _ss.GLideN64Plugin.FBInfoReadColorChunk;
			GLideN64_FBInfoReadDepthChunk.Checked = _ss.GLideN64Plugin.FBInfoReadDepthChunk;
			GLideN64_txFilterMode
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.TextureFilterMode>(_ss.GLideN64Plugin.txFilterMode);
			GLideN64_txEnhancementMode
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.TextureEnhancementMode>(_ss.GLideN64Plugin.txEnhancementMode);
			GLideN64_txDeposterize.Checked = _ss.GLideN64Plugin.txDeposterize;
			GLideN64_txFilterIgnoreBG.Checked = _ss.GLideN64Plugin.txFilterIgnoreBG;
			GLideN64_txCacheSize.Text = _ss.GLideN64Plugin.txCacheSize.ToString();
			GLideN64_txHiresEnable.Checked = _ss.GLideN64Plugin.txHiresEnable;
			GLideN64_txHiresFullAlphaChannel.Checked = _ss.GLideN64Plugin.txHiresFullAlphaChannel;
			GLideN64_txHresAltCRC.Checked = _ss.GLideN64Plugin.txHresAltCRC;
			GLideN64_txDump.Checked = _ss.GLideN64Plugin.txDump;
			GLideN64_txCacheCompression.Checked = _ss.GLideN64Plugin.txCacheCompression;
			GLideN64_txForce16bpp.Checked = _ss.GLideN64Plugin.txForce16bpp;
			GLideN64_txSaveCache.Checked = _ss.GLideN64Plugin.txSaveCache;
			GLideN64_txPath.Text = _ss.GLideN64Plugin.txPath;
			GLideN64_EnableBloom.Checked = _ss.GLideN64Plugin.EnableBloom;
			GLideN64_bloomThresholdLevel.SelectedIndex = _ss.GLideN64Plugin.bloomThresholdLevel - 2;
			GLideN64_bloomBlendMode
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.BlendMode>(_ss.GLideN64Plugin.bloomBlendMode);
			GLideN64_blurAmount.SelectedIndex = _ss.GLideN64Plugin.blurAmount - 2;
			GLideN64_blurStrength.SelectedIndex = _ss.GLideN64Plugin.blurStrength - 10;
			GLideN64_ForceGammaCorrection.Checked = _ss.GLideN64Plugin.ForceGammaCorrection;
			GLideN64_GammaCorrectionLevel.Text = _ss.GLideN64Plugin.GammaCorrectionLevel.ToString();

			GLideN64_OverscanNtscTop.Enabled =
			GLideN64_OverscanNtscBottom.Enabled =
			GLideN64_OverscanNtscLeft.Enabled =
			GLideN64_OverscanNtscRight.Enabled =
			GLideN64_OverscanPalTop.Enabled =
			GLideN64_OverscanPalBottom.Enabled =
			GLideN64_OverscanPalLeft.Enabled =
			GLideN64_OverscanPalRight.Enabled =
			GLideN64_EnableOverscan.Checked =
				_ss.GLideN64Plugin.EnableOverscan;
			GLideN64_OverscanNtscTop.Text = _ss.GLideN64Plugin.OverscanNtscTop.ToString();
			GLideN64_OverscanNtscBottom.Text = _ss.GLideN64Plugin.OverscanNtscBottom.ToString();
			GLideN64_OverscanNtscLeft.Text = _ss.GLideN64Plugin.OverscanNtscLeft.ToString();
			GLideN64_OverscanNtscRight.Text = _ss.GLideN64Plugin.OverscanNtscRight.ToString();
			GLideN64_OverscanPalTop.Text = _ss.GLideN64Plugin.OverscanPalTop.ToString();
			GLideN64_OverscanPalBottom.Text = _ss.GLideN64Plugin.OverscanPalBottom.ToString();
			GLideN64_OverscanPalLeft.Text = _ss.GLideN64Plugin.OverscanPalLeft.ToString();
			GLideN64_OverscanPalRight.Text = _ss.GLideN64Plugin.OverscanPalRight.ToString();

			UpdateGLideN64HacksSection();
			if (!_ss.GLideN64Plugin.UseDefaultHacks)
			{
				GLideN64_EnableN64DepthCompare.Checked = _ss.GLideN64Plugin.EnableN64DepthCompare;
				GLideN64_EnableCopyColorToRDRAM
					.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.CopyColorToRDRAMMode>(_ss.GLideN64Plugin.EnableCopyColorToRDRAM);
				GLideN64_EnableCopyDepthToRDRAM
					.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.CopyDepthToRDRAMMode>(_ss.GLideN64Plugin.EnableCopyDepthToRDRAM);
				GLideN64_EnableCopyColorFromRDRAM.Checked = _ss.GLideN64Plugin.EnableCopyColorFromRDRAM;
				GLideN64_EnableCopyAuxiliaryToRDRAM.Checked = _ss.GLideN64Plugin.EnableCopyAuxiliaryToRDRAM;
			}
		}
		
		private void RiceAnisotropicFiltering_Tb_Scroll_1(object sender, EventArgs e)
		{
			AnisotropicFiltering_LB.Text = $"Anisotropic Filtering: {RiceAnisotropicFiltering_TB.Value}";
		}

		private void RiceUseDefaultHacks_Cb_CheckedChanged(object sender, EventArgs e)
		{
			UpdateRiceHacksSection();
		}

		private void UpdateGlide64mk2HacksSection()
		{
			if (Glide64mk2_UseDefaultHacks1.Checked || Glide64mk2_UseDefaultHacks2.Checked)
			{
				Glide64mk2_use_sts1_only.Checked = Global.Game.GetBool("Glide64mk2_use_sts1_only", false);
				Glide64mk2_optimize_texrect.Checked = Global.Game.GetBool("Glide64mk2_optimize_texrect", true);
				Glide64mk2_increase_texrect_edge.Checked = Global.Game.GetBool("Glide64mk2_increase_texrect_edge", false);
				Glide64mk2_ignore_aux_copy.Checked = Global.Game.GetBool("Glide64mk2_ignore_aux_copy", false);
				Glide64mk2_hires_buf_clear.Checked = Global.Game.GetBool("Glide64mk2_hires_buf_clear", true);
				Glide64mk2_force_microcheck.Checked = Global.Game.GetBool("Glide64mk2_force_microcheck", false);
				Glide64mk2_fog.Checked = Global.Game.GetBool("Glide64mk2_fog", true);
				Glide64mk2_fb_smart.Checked = Global.Game.GetBool("Glide64mk2_fb_smart", false);
				Glide64mk2_fb_read_alpha.Checked = Global.Game.GetBool("Glide64mk2_fb_read_alpha", false);
				Glide64mk2_fb_hires.Checked = Global.Game.GetBool("Glide64mk2_fb_hires", true);
				Glide64mk2_detect_cpu_write.Checked = Global.Game.GetBool("Glide64mk2_detect_cpu_write", false);
				Glide64mk2_decrease_fillrect_edge.Checked = Global.Game.GetBool("Glide64mk2_decrease_fillrect_edge", false);
				Glide64mk2_buff_clear.Checked = Global.Game.GetBool("Glide64mk2_buff_clear", true);
				Glide64mk2_alt_tex_size.Checked = Global.Game.GetBool("Glide64mk2_alt_tex_size", true);
				Glide64mk2_swapmode.SelectedIndex = Global.Game.GetInt("Glide64mk2_swapmode", 1);
				Glide64mk2_stipple_pattern.Text = Global.Game.GetInt("Glide64mk2_stipple_pattern", 1041204192).ToString();
				Glide64mk2_stipple_mode.Text = Global.Game.GetInt("Glide64mk2_stipple_mode", 2).ToString();
				Glide64mk2_lodmode.SelectedIndex = Global.Game.GetInt("Glide64mk2_lodmode", 0);
				Glide64mk2_filtering.SelectedIndex = Global.Game.GetInt("Glide64mk2_filtering", 0);
				Glide64mk2_correct_viewport.Checked = Global.Game.GetBool("Glide64mk2_correct_viewport", false);
				Glide64mk2_force_calc_sphere.Checked = Global.Game.GetBool("Glide64mk2_force_calc_sphere", false);
				Glide64mk2_pal230.Checked = Global.Game.GetBool("Glide64mk2_pal230", false);
				Glide64mk2_texture_correction.Checked = Global.Game.GetBool("Glide64mk2_texture_correction", true);
				Glide64mk2_n64_z_scale.Checked = Global.Game.GetBool("Glide64mk2_n64_z_scale", false);
				Glide64mk2_old_style_adither.Checked = Global.Game.GetBool("Glide64mk2_old_style_adither", false);
				Glide64mk2_zmode_compare_less.Checked = Global.Game.GetBool("Glide64mk2_zmode_compare_less", false);
				Glide64mk2_adjust_aspect.Checked = Global.Game.GetBool("Glide64mk2_adjust_aspect", true);
				Glide64mk2_clip_zmax.Checked = Global.Game.GetBool("Glide64mk2_clip_zmax", true);
				Glide64mk2_clip_zmin.Checked = Global.Game.GetBool("Glide64mk2_clip_zmin", false);
				Glide64mk2_force_quad3d.Checked = Global.Game.GetBool("Glide64mk2_force_quad3d", false);
				Glide64mk2_useless_is_useless.Checked = Global.Game.GetBool("Glide64mk2_useless_is_useless", false);
				Glide64mk2_fb_read_always.Checked = Global.Game.GetBool("Glide64mk2_fb_read_always", false);
				Glide64mk2_aspectmode.SelectedIndex = Global.Game.GetInt("Glide64mk2_aspectmode", 0);
				Glide64mk2_fb_crc_mode.SelectedIndex = Global.Game.GetInt("Glide64mk2_fb_crc_mode", 1);
				Glide64mk2_enable_hacks_for_game.SelectedIndex = Global.Game.GetInt("Glide64mk2_enable_hacks_for_game", 0);
				Glide64mk2_read_back_to_screen.SelectedIndex = Global.Game.GetInt("Glide64mk2_read_back_to_screen", 0);
				Glide64mk2_fast_crc.Checked = Global.Game.GetBool("Glide64mk2_fast_crc", true);

				ToggleGlide64mk2HackCheckboxEnable(false);
			}
			else
			{
				ToggleGlide64mk2HackCheckboxEnable(true);
			}
		}

		private void UpdateGLideN64HacksSection()
		{
			if (GLideN64_UseDefaultHacks.Checked)
			{
				GLideN64_EnableN64DepthCompare.Checked = Global.Game.GetBool("GLideN64_N64DepthCompare", false);
				GLideN64_EnableCopyColorToRDRAM.SelectedItem = ((N64SyncSettings.N64GLideN64PluginSettings.CopyColorToRDRAMMode)GetIntFromDB("GLideN64_CopyColorToRDRAM", (int)N64SyncSettings.N64GLideN64PluginSettings.CopyColorToRDRAMMode.AsyncMode)).GetDescription();
				GLideN64_EnableCopyDepthToRDRAM.SelectedItem = ((N64SyncSettings.N64GLideN64PluginSettings.CopyDepthToRDRAMMode)GetIntFromDB("GLideN64_CopyDepthToRDRAM", (int)N64SyncSettings.N64GLideN64PluginSettings.CopyDepthToRDRAMMode.DoNotCopy)).GetDescription();
				GLideN64_EnableCopyColorFromRDRAM.Checked = Global.Game.GetBool("GLideN64_CopyColorFromRDRAM", false);
				GLideN64_EnableCopyAuxiliaryToRDRAM.Checked = Global.Game.GetBool("GLideN64_CopyAuxiliaryToRDRAM", false);

				ToggleGLideN64HackCheckboxEnable(false);
			}
			else
			{
				ToggleGLideN64HackCheckboxEnable(true);
			}
		}

		private void UpdateGlideHacksSection()
		{
			if (GlideUseDefaultHacks1.Checked || GlideUseDefaultHacks2.Checked)
			{
				Glide_alt_tex_size.Checked = Global.Game.GetBool("Glide_alt_tex_size", false);
				Glide_buff_clear.Checked = Global.Game.GetBool("Glide_buff_clear", true);
				Glide_decrease_fillrect_edge.Checked = Global.Game.GetBool("Glide_decrease_fillrect_edge", false);
				Glide_detect_cpu_write.Checked = Global.Game.GetBool("Glide_detect_cpu_write", false);
				Glide_fb_clear.Checked = Global.Game.GetBool("Glide_fb_clear", false);
				Glide_fb_hires.Checked = Global.Game.GetBool("Glide_fb_hires", true);
				Glide_fb_read_alpha.Checked = Global.Game.GetBool("Glide_fb_read_alpha", false);
				Glide_fb_smart.Checked = Global.Game.GetBool("Glide_fb_smart", false);
				Glide_fillcolor_fix.Checked = Global.Game.GetBool("Glide_fillcolor_fix", false);
				Glide_fog.Checked = Global.Game.GetBool("Glide_fog", true);
				Glide_force_depth_compare.Checked = Global.Game.GetBool("Glide_force_depth_compare", false);
				Glide_force_microcheck.Checked = Global.Game.GetBool("Glide_force_microcheck", false);
				Glide_fb_hires_buf_clear.Checked = Global.Game.GetBool("Glide_fb_hires_buf_clear", true);
				Glide_fb_ignore_aux_copy.Checked = Global.Game.GetBool("Glide_fb_ignore_aux_copy", false);
				Glide_fb_ignore_previous.Checked = Global.Game.GetBool("Glide_fb_ignore_previous", false);
				Glide_increase_primdepth.Checked = Global.Game.GetBool("Glide_increase_primdepth", false);
				Glide_increase_texrect_edge.Checked = Global.Game.GetBool("Glide_increase_texrect_edge", false);
				Glide_fb_optimize_texrect.Checked = Global.Game.GetBool("Glide_fb_optimize_texrect", true);
				Glide_fb_optimize_write.Checked = Global.Game.GetBool("Glide_fb_optimize_write", false);
				Glide_PPL.Checked = Global.Game.GetBool("Glide_PPL", false);
				Glide_soft_depth_compare.Checked = Global.Game.GetBool("Glide_soft_depth_compare", false);
				Glide_use_sts1_only.Checked = Global.Game.GetBool("Glide_use_sts1_only", false);
				Glide_wrap_big_tex.Checked = Global.Game.GetBool("Glide_wrap_big_tex", false);

				Glide_depth_bias.Text = Global.Game.GetInt("Glide_depth_bias", 20).ToString();
				Glide_filtering.SelectedIndex = Global.Game.GetInt("Glide_filtering", 1);
				Glide_fix_tex_coord.Text = Global.Game.GetInt("Glide_fix_tex_coord", 0).ToString();
				Glide_lodmode.SelectedIndex = Global.Game.GetInt("Glide_lodmode", 0);

				Glide_stipple_mode.Text = Global.Game.GetInt("Glide_stipple_mode", 2).ToString();
				Glide_stipple_pattern.Text = Global.Game.GetInt("Glide_stipple_pattern", 1041204192).ToString();
				Glide_swapmode.SelectedIndex = Global.Game.GetInt("Glide_swapmode", 1);
				Glide_enable_hacks_for_game.SelectedIndex = Global.Game.GetInt("Glide_enable_hacks_for_game", 0);
				
				ToggleGlideHackCheckboxEnable(false);
			}
			else
			{
				ToggleGlideHackCheckboxEnable(true);
			}
		}

		private void UpdateRiceHacksSection()
		{
			if (RiceUseDefaultHacks_CB.Checked)
			{
				RiceDisableTextureCRC_CB.Checked = GetBoolFromDB("RiceDisableTextureCRC");
				RiceDisableCulling_CB.Checked = GetBoolFromDB("RiceDisableCulling");
				RiceIncTexRectEdge_CB.Checked = GetBoolFromDB("RiceIncTexRectEdge");
				RiceZHack_CB.Checked = GetBoolFromDB("RiceZHack");
				RiceTextureScaleHack_CB.Checked = GetBoolFromDB("RiceTextureScaleHack");
				RicePrimaryDepthHack_CB.Checked = GetBoolFromDB("RicePrimaryDepthHack");
				RiceTexture1Hack_CB.Checked = GetBoolFromDB("RiceTexture1Hack");
				RiceFastLoadTile_CB.Checked = GetBoolFromDB("RiceFastLoadTile");
				RiceUseSmallerTexture_CB.Checked = GetBoolFromDB("RiceUseSmallerTexture");
				RiceVIWidth_Text.Text = GetIntFromDB("RiceVIWidth", -1).ToString();
				RiceVIHeight_Text.Text = GetIntFromDB("RiceVIHeight", -1).ToString();
				RiceUseCIWidthAndRatio_Combo.SelectedIndex = GetIntFromDB("RiceUseCIWidthAndRatio", 0);
				RiceFullTMEM_Combo.SelectedIndex = GetIntFromDB("RiceFullTMEM", 0);
				RiceTxtSizeMethod2_CB.Checked = GetBoolFromDB("RiceTxtSizeMethod2");
				RiceEnableTxtLOD_CB.Checked = GetBoolFromDB("RiceEnableTxtLOD");
				RiceFastTextureCRC_Combo.SelectedIndex = GetIntFromDB("RiceFastTextureCRC", 0);
				RiceEmulateClear_CB.Checked = GetBoolFromDB("RiceEmulateClear");
				RiceForceScreenClear_CB.Checked = GetBoolFromDB("RiceForceScreenClear");
				RiceAccurateTextureMappingHack_Combo.SelectedIndex = GetIntFromDB("RiceAccurateTextureMappingHack", 0);
				RiceNormalBlender_Combo.SelectedIndex = GetIntFromDB("RiceNormalBlender", 0);
				RiceDisableBlender_CB.Checked = GetBoolFromDB("RiceDisableBlender");
				RiceForceDepthBuffer_CB.Checked = GetBoolFromDB("RiceForceDepthBuffer");
				RiceDisableObjBG_CB.Checked = GetBoolFromDB("RiceDisableObjBG");
				RiceFrameBufferOption_Combo.SelectedIndex = GetIntFromDB("RiceFrameBufferOption", 0);
				RiceRenderToTextureOption_Combo.SelectedIndex = GetIntFromDB("RiceRenderToTextureOption", 0);
				RiceScreenUpdateSettingHack_Combo.SelectedIndex = GetIntFromDB("RiceScreenUpdateSettingHack", 0);
				RiceEnableHacksForGame_Combo.SelectedIndex = GetIntFromDB("RiceEnableHacksForGame", 0);
				
				ToggleRiceHackCheckboxEnable(false);
			}
			else
			{
				ToggleRiceHackCheckboxEnable(true);
			}
		}

		private bool GetBoolFromDB(string parameter)
		{
			return Global.Game.OptionPresent(parameter) && Global.Game.OptionValue(parameter) == "true";
		}

		private int GetIntFromDB(string parameter, int defaultVal)
		{
			if (Global.Game.OptionPresent(parameter) && Global.Game.OptionValue(parameter).IsUnsigned())
			{
				return int.Parse(Global.Game.OptionValue(parameter));
			}

			return defaultVal;
		}

		private void ToggleRiceHackCheckboxEnable (bool val)
		{
			RiceDisableTextureCRC_CB.Enabled = val;
			RiceDisableCulling_CB.Enabled = val;
			RiceIncTexRectEdge_CB.Enabled = val;
			RiceZHack_CB.Enabled = val;
			RiceTextureScaleHack_CB.Enabled = val;
			RicePrimaryDepthHack_CB.Enabled = val;
			RiceTexture1Hack_CB.Enabled = val;
			RiceFastLoadTile_CB.Enabled = val;
			RiceUseSmallerTexture_CB.Enabled = val;
			RiceVIWidth_Text.Enabled = val;
			RiceVIHeight_Text.Enabled = val;
			RiceUseCIWidthAndRatio_Combo.Enabled = val;
			RiceFullTMEM_Combo.Enabled = val;
			RiceTxtSizeMethod2_CB.Enabled = val;
			RiceEnableTxtLOD_CB.Enabled = val;
			RiceFastTextureCRC_Combo.Enabled = val;
			RiceEmulateClear_CB.Enabled = val;
			RiceForceScreenClear_CB.Enabled = val;
			RiceAccurateTextureMappingHack_Combo.Enabled = val;
			RiceNormalBlender_Combo.Enabled = val;
			RiceDisableBlender_CB.Enabled = val;
			RiceForceDepthBuffer_CB.Enabled = val;
			RiceDisableObjBG_CB.Enabled = val;
			RiceFrameBufferOption_Combo.Enabled = val;
			RiceRenderToTextureOption_Combo.Enabled = val;
			RiceScreenUpdateSettingHack_Combo.Enabled = val;
			RiceEnableHacksForGame_Combo.Enabled = val;
		}

		private void ToggleGlideHackCheckboxEnable(bool val)
		{
			Glide_alt_tex_size.Enabled = val;
			Glide_buff_clear.Enabled = val;
			Glide_decrease_fillrect_edge.Enabled = val;
			Glide_detect_cpu_write.Enabled = val;
			Glide_fb_clear.Enabled = val;
			Glide_fb_hires.Enabled = val;
			Glide_fb_read_alpha.Enabled = val;
			Glide_fb_smart.Enabled = val;
			Glide_fillcolor_fix.Enabled = val;
			Glide_fog.Enabled = val;
			Glide_force_depth_compare.Enabled = val;
			Glide_force_microcheck.Enabled = val;
			Glide_fb_hires_buf_clear.Enabled = val;
			Glide_fb_ignore_aux_copy.Enabled = val;
			Glide_fb_ignore_previous.Enabled = val;
			Glide_increase_primdepth.Enabled = val;
			Glide_increase_texrect_edge.Enabled = val;
			Glide_fb_optimize_texrect.Enabled = val;
			Glide_fb_optimize_write.Enabled = val;
			Glide_PPL.Enabled = val;
			Glide_soft_depth_compare.Enabled = val;
			Glide_use_sts1_only.Enabled = val;
			Glide_wrap_big_tex.Enabled = val;
			Glide_depth_bias.Enabled = val;
			Glide_filtering.Enabled = val;
			Glide_fix_tex_coord.Enabled = val;
			Glide_lodmode.Enabled = val;
			Glide_stipple_mode.Enabled = val;
			Glide_stipple_pattern.Enabled = val;
			Glide_swapmode.Enabled = val;
			Glide_enable_hacks_for_game.Enabled = val;
		}

		private void ToggleGlide64mk2HackCheckboxEnable(bool val)
		{
			Glide64mk2_use_sts1_only.Enabled = val;
			Glide64mk2_optimize_texrect.Enabled = val;
			Glide64mk2_increase_texrect_edge.Enabled = val;
			Glide64mk2_ignore_aux_copy.Enabled = val;
			Glide64mk2_hires_buf_clear.Enabled = val;
			Glide64mk2_force_microcheck.Enabled = val;
			Glide64mk2_fog.Enabled = val;
			Glide64mk2_fb_smart.Enabled = val;
			Glide64mk2_fb_read_alpha.Enabled = val;
			Glide64mk2_fb_hires.Enabled = val;
			Glide64mk2_detect_cpu_write.Enabled = val;
			Glide64mk2_decrease_fillrect_edge.Enabled = val;
			Glide64mk2_buff_clear.Enabled = val;
			Glide64mk2_alt_tex_size.Enabled = val;
			Glide64mk2_swapmode.Enabled = val;
			Glide64mk2_stipple_pattern.Enabled = val;
			Glide64mk2_stipple_mode.Enabled = val;
			Glide64mk2_lodmode.Enabled = val;
			Glide64mk2_filtering.Enabled = val;
			Glide64mk2_correct_viewport.Enabled = val;
			Glide64mk2_force_calc_sphere.Enabled = val;
			Glide64mk2_pal230.Enabled = val;
			Glide64mk2_texture_correction.Enabled = val;
			Glide64mk2_n64_z_scale.Enabled = val;
			Glide64mk2_old_style_adither.Enabled = val;
			Glide64mk2_zmode_compare_less.Enabled = val;
			Glide64mk2_adjust_aspect.Enabled = val;
			Glide64mk2_clip_zmax.Enabled = val;
			Glide64mk2_clip_zmin.Enabled = val;
			Glide64mk2_force_quad3d.Enabled = val;
			Glide64mk2_useless_is_useless.Enabled = val;
			Glide64mk2_fb_read_always.Enabled = val;
			Glide64mk2_aspectmode.Enabled = val;
			Glide64mk2_fb_crc_mode.Enabled = val;
			Glide64mk2_enable_hacks_for_game.Enabled = val;
			Glide64mk2_read_back_to_screen.Enabled = val;
			Glide64mk2_fast_crc.Enabled = val;
		}

		public void ToggleGLideN64HackCheckboxEnable(bool val)
		{
			GLideN64_EnableN64DepthCompare.Enabled = val;
			GLideN64_EnableCopyColorToRDRAM.Enabled = val;
			GLideN64_EnableCopyDepthToRDRAM.Enabled = val;
			GLideN64_EnableCopyColorFromRDRAM.Enabled = val;
			GLideN64_EnableCopyAuxiliaryToRDRAM.Enabled = val;
		}

		private void GLideN64_EnableOverscan_CheckedChanged(object sender, EventArgs e)
		{
			GLideN64_OverscanNtscTop.Enabled =
			GLideN64_OverscanNtscBottom.Enabled =
			GLideN64_OverscanNtscLeft.Enabled =
			GLideN64_OverscanNtscRight.Enabled =
			GLideN64_OverscanPalTop.Enabled =
			GLideN64_OverscanPalBottom.Enabled =
			GLideN64_OverscanPalLeft.Enabled =
			GLideN64_OverscanPalRight.Enabled =
				GLideN64_EnableOverscan.Checked;
		}

		private void GlideUseDefaultHacks1_CheckedChanged(object sender, EventArgs e)
		{
			GlideUseDefaultHacks2.Checked = GlideUseDefaultHacks1.Checked;
			UpdateGlideHacksSection();
		}

		private void GlideUseDefaultHacks2_CheckedChanged(object sender, EventArgs e)
		{
			GlideUseDefaultHacks1.Checked = GlideUseDefaultHacks2.Checked;
			UpdateGlideHacksSection();
		}

		private void Glide64mk2_UseDefaultHacks1_CheckedChanged(object sender, EventArgs e)
		{
			Glide64mk2_UseDefaultHacks2.Checked = Glide64mk2_UseDefaultHacks1.Checked;
			UpdateGlide64mk2HacksSection();
		}

		private void Glide64mk2_UseDefaultHacks2_CheckedChanged(object sender, EventArgs e)
		{
			Glide64mk2_UseDefaultHacks1.Checked = Glide64mk2_UseDefaultHacks2.Checked;
			UpdateGlide64mk2HacksSection();
		}

		private void GLideN64_UseDefaultHacks_CheckedChanged(object sender, EventArgs e)
		{
			UpdateGLideN64HacksSection();
		}

		private void PluginComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_programmaticallyChangingPluginComboBox)
			{
				return;
			}

			if (VideoResolutionComboBox.SelectedItem == null)
			{
				VideoResolutionComboBox.SelectedIndex = 0;
			}

			string[] strArr;
			int oldSizeX, oldSizeY;

			var oldResolution = VideoResolutionComboBox.SelectedItem?.ToString() ?? "";
			if (oldResolution != CustomResItemName)
			{
				strArr = oldResolution.Split('x');
				oldSizeX = int.Parse(strArr[0].Trim());
				oldSizeY = int.Parse(strArr[1].Trim());
			}
			else
			{
				oldSizeX = int.Parse(VideoResolutionXTextBox.Text);
				oldSizeY = int.Parse(VideoResolutionYTextBox.Text);
			}

			
			// Change resolution list to the rest
			VideoResolutionComboBox.Items.Clear();
			VideoResolutionComboBox.Items.AddRange(ValidResolutions);

			// If the given resolution is in the table, pick it.
			// Otherwise find a best fit
			var index = VideoResolutionComboBox.Items.IndexOf(oldResolution);
			if (index >= 0)
			{
				VideoResolutionComboBox.SelectedIndex = index;
			}
			else
			{
				int bestFit = -1;
				for (int i = 0; i < VideoResolutionComboBox.Items.Count; i++)
				{
					if ((string)VideoResolutionComboBox.Items[i] != CustomResItemName)
					{
						string option = (string)VideoResolutionComboBox.Items[i];
						strArr = option.Split('x');
						int newSizeX = int.Parse(strArr[0].Trim());
						int newSizeY = int.Parse(strArr[1].Trim());
						if (oldSizeX < newSizeX || oldSizeX == newSizeX && oldSizeY < newSizeY)
						{
							if (i == 0)
							{
								bestFit = 0;
								break;
							}
							
							bestFit = i - 1;
							break;
						}
					}
				}

				if (bestFit < 0)
				{
					VideoResolutionComboBox.SelectedIndex = VideoResolutionComboBox.Items.Count - 1;
				}
				else
				{
					VideoResolutionComboBox.SelectedIndex = bestFit;
				}
			}
		}

		private void VideoResolutionComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (VideoResolutionComboBox.Text == CustomResItemName)
			{
				ShowCustomVideoResolutionControls();
			}
			else
			{
				HideCustomVideoResolutionControls();
				var newResolution = VideoResolutionComboBox.SelectedItem.ToString();
				var strArr = newResolution.Split('x');
				VideoResolutionXTextBox.Text = strArr[0].Trim();
				VideoResolutionYTextBox.Text = strArr[1].Trim();
			}
		}

		private void ShowCustomVideoResolutionControls()
		{
			LabelVideoResolutionX.Visible = true;
			LabelVideoResolutionY.Visible = true;
			VideoResolutionXTextBox.Visible = true;
			VideoResolutionYTextBox.Visible = true;
		}

		private void HideCustomVideoResolutionControls()
		{
			LabelVideoResolutionX.Visible = false;
			LabelVideoResolutionY.Visible = false;
			VideoResolutionXTextBox.Visible = false;
			VideoResolutionYTextBox.Visible = false;
		}
	}
}
