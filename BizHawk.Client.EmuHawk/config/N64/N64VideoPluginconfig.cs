using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using System.IO;
using System.Security.Cryptography;

namespace BizHawk.Client.EmuHawk
{
	public partial class N64VideoPluginconfig : Form
	{
		private N64Settings s;
		private N64SyncSettings ss;

		string[] validResolutions = {
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
										"Custom"
									};

		string[] validResolutionsJabo = {
											"320 x 240",
											"400 x 300",
											"512 x 384",
											"640 x 480",
											"800 x 600",
											"1024 x 768",
											"1152 x 864",
											"1280 x 960",
											"1600 x 1200",
											"848 x 480",
											"1024 x 576",
											"1380 x 768"
										};

		private string previousPluginSelection = string.Empty;
		private bool programmaticallyChangingPluginComboBox = false;

		public N64VideoPluginconfig()
		{
			InitializeComponent();
		}

		// because mupen is a pile of garbage, this all needs to work even when N64 is not loaded
		private static N64SyncSettings GetSyncSettings()
		{
			if (Global.Emulator is N64)
			{
				return ((N64)Global.Emulator).GetSyncSettings();
			}
			else
			{
				return (N64SyncSettings)Global.Config.GetCoreSyncSettings<N64>() 
					?? new N64SyncSettings();
			}
		}

		private static N64Settings GetSettings()
		{
			if (Global.Emulator is N64)
			{
				return ((N64)Global.Emulator).GetSettings();
			}
			else
			{
				return (N64Settings)Global.Config.GetCoreSettings<N64>()
					?? new N64Settings();
			}
		}

		private static void PutSyncSettings(N64SyncSettings s)
		{
			if (Global.Emulator is N64)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(s);
			}
			else
			{
				Global.Config.PutCoreSyncSettings<N64>(s);
			}
		}

		private static void PutSettings(N64Settings s)
		{
			if (Global.Emulator is N64)
			{
				GlobalWin.MainForm.PutCoreSettings(s);
			}
			else
			{
				Global.Config.PutCoreSettings<N64>(s);
			}
		}

		private void CancelBT_Click(object sender, EventArgs e)
		{
			//Add confirmation of cancelling change
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			SaveSettings();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void SaveSettings()
		{
			// Global
			if (VideoResolutionComboBox.Text != "Custom")
			{
				var video_settings = VideoResolutionComboBox.SelectedItem.ToString();
				var strArr = video_settings.Split('x');
				s.VideoSizeX = int.Parse(strArr[0].Trim());
				s.VideoSizeY = int.Parse(strArr[1].Trim());
			}
			else
			{
				s.VideoSizeX =
					VideoResolutionXTextBox.Text.IsUnsigned() ?
					int.Parse(VideoResolutionXTextBox.Text) : 320;

				s.VideoSizeY =
					VideoResolutionYTextBox.Text.IsUnsigned() ?
					int.Parse(VideoResolutionYTextBox.Text) : 240;
			}
			switch (PluginComboBox.Text)
			{
				case "Rice": ss.VideoPlugin = PluginType.Rice; break;
				case "Glide64": ss.VideoPlugin = PluginType.Glide; break;
				case "Glide64mk2": ss.VideoPlugin = PluginType.GlideMk2; break;
				case "Jabo 1.6.1": ss.VideoPlugin = PluginType.Jabo; break;
				case "GLideN64": ss.VideoPlugin = PluginType.GLideN64; break;
			}

			// Jabo
			ss.JaboPlugin.UseDefaultHacks = JaboUseForGameCheckbox.Checked;

			ss.JaboPlugin.clear_mode = JaboClearModeDropDown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64JaboPluginSettings.Direct3DClearMode>();

			ss.JaboPlugin.anisotropic_level = JaboAnisotropicFilteringLevelDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64JaboPluginSettings.ANISOTROPIC_FILTERING_LEVEL>();

			ss.JaboPlugin.antialiasing_level = JaboAntialiasingLevelDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64JaboPluginSettings.ANTIALIASING_LEVEL>();

			ss.JaboPlugin.brightness = (int)JaboBrightnessBox.Value;
			ss.JaboPlugin.super2xsal = JaboSuper2xsalCheckbox.Checked;
			ss.JaboPlugin.texture_filter = JaboTextureFilterCheckbox.Checked;
			ss.JaboPlugin.adjust_aspect_ratio = JaboAdjustAspectRatioCheckbox.Checked;
			ss.JaboPlugin.legacy_pixel_pipeline = JaboLegacyPixelPipelineCheckbox.Checked;
			ss.JaboPlugin.alpha_blending = JaboAlphaBlendingCheckbox.Checked;
			ss.JaboPlugin.direct3d_transformation_pipeline = JaboDirect3DPipelineCheckbox.Checked;
			ss.JaboPlugin.z_compare = JaboZCompareCheckbox.Checked;
			ss.JaboPlugin.copy_framebuffer = JaboCopyFrameBufferCheckbox.Checked;
			ss.JaboPlugin.resolution_width = JaboResolutionWidthBox.ToRawInt().Value;
			ss.JaboPlugin.resolution_height = JaboResolutionHeightBox.ToRawInt().Value;

			// Rice
			ss.RicePlugin.NormalAlphaBlender = RiceNormalAlphaBlender_CB.Checked;
			ss.RicePlugin.FastTextureLoading = RiceFastTextureLoading_CB.Checked;
			ss.RicePlugin.AccurateTextureMapping = RiceAccurateTextureMapping_CB.Checked;
			ss.RicePlugin.InN64Resolution = RiceInN64Resolution_CB.Checked;
			ss.RicePlugin.SaveVRAM = RiceSaveVRAM_CB.Checked;
			ss.RicePlugin.DoubleSizeForSmallTxtrBuf = RiceDoubleSizeForSmallTxtrBuf_CB.Checked;
			ss.RicePlugin.DefaultCombinerDisable = RiceDefaultCombinerDisable_CB.Checked;
			ss.RicePlugin.EnableHacks = RiceEnableHacks_CB.Checked;
			ss.RicePlugin.WinFrameMode = RiceWinFrameMode_CB.Checked;
			ss.RicePlugin.FullTMEMEmulation = RiceFullTMEMEmulation_CB.Checked;
			ss.RicePlugin.OpenGLVertexClipper = RiceOpenGLVertexClipper_CB.Checked;
			ss.RicePlugin.EnableSSE = RiceEnableSSE_CB.Checked;
			ss.RicePlugin.EnableVertexShader = RiceEnableVertexShader_CB.Checked;
			ss.RicePlugin.SkipFrame = RiceSkipFrame_CB.Checked;
			ss.RicePlugin.TexRectOnly = RiceTexRectOnly_CB.Checked;
			ss.RicePlugin.SmallTextureOnly = RiceSmallTextureOnly_CB.Checked;
			ss.RicePlugin.LoadHiResCRCOnly = RiceLoadHiResCRCOnly_CB.Checked;
			ss.RicePlugin.LoadHiResTextures = RiceLoadHiResTextures_CB.Checked;
			ss.RicePlugin.DumpTexturesToFiles = RiceDumpTexturesToFiles_CB.Checked;

			ss.RicePlugin.FrameBufferSetting = RiceFrameBufferSetting_Combo.SelectedIndex;
			ss.RicePlugin.FrameBufferWriteBackControl = RiceFrameBufferWriteBackControl_Combo.SelectedIndex;
			ss.RicePlugin.RenderToTexture = RiceRenderToTexture_Combo.SelectedIndex;
			ss.RicePlugin.ScreenUpdateSetting = RiceScreenUpdateSetting_Combo.SelectedIndex;
			ss.RicePlugin.Mipmapping = RiceMipmapping_Combo.SelectedIndex;
			ss.RicePlugin.FogMethod = RiceFogMethod_Combo.SelectedIndex;
			ss.RicePlugin.ForceTextureFilter = RiceForceTextureFilter_Combo.SelectedIndex;
			ss.RicePlugin.TextureEnhancement = RiceTextureEnhancement_Combo.SelectedIndex;
			ss.RicePlugin.TextureEnhancementControl = RiceTextureEnhancementControl_Combo.SelectedIndex;
			ss.RicePlugin.TextureQuality = RiceTextureQuality_Combo.SelectedIndex;
			ss.RicePlugin.OpenGLDepthBufferSetting = (RiceOpenGLDepthBufferSetting_Combo.SelectedIndex + 1) * 16;
			switch (RiceMultiSampling_Combo.SelectedIndex)
			{
				case 0: ss.RicePlugin.MultiSampling = 0; break;
				case 1: ss.RicePlugin.MultiSampling = 2; break;
				case 2: ss.RicePlugin.MultiSampling = 4; break;
				case 3: ss.RicePlugin.MultiSampling = 8; break;
				case 4: ss.RicePlugin.MultiSampling = 16; break;
				default: ss.RicePlugin.MultiSampling = 0; break;
			}
			ss.RicePlugin.ColorQuality = RiceColorQuality_Combo.SelectedIndex;
			ss.RicePlugin.OpenGLRenderSetting = RiceOpenGLRenderSetting_Combo.SelectedIndex;
			ss.RicePlugin.AnisotropicFiltering = RiceAnisotropicFiltering_TB.Value;

			ss.RicePlugin.UseDefaultHacks = RiceUseDefaultHacks_CB.Checked;
			ss.RicePlugin.DisableTextureCRC = RiceDisableTextureCRC_CB.Checked;
			ss.RicePlugin.DisableCulling = RiceDisableCulling_CB.Checked;
			ss.RicePlugin.IncTexRectEdge = RiceIncTexRectEdge_CB.Checked;
			ss.RicePlugin.ZHack = RiceZHack_CB.Checked;
			ss.RicePlugin.TextureScaleHack = RiceTextureScaleHack_CB.Checked;
			ss.RicePlugin.PrimaryDepthHack = RicePrimaryDepthHack_CB.Checked;
			ss.RicePlugin.Texture1Hack = RiceTexture1Hack_CB.Checked;
			ss.RicePlugin.FastLoadTile = RiceFastLoadTile_CB.Checked;
			ss.RicePlugin.UseSmallerTexture = RiceUseSmallerTexture_CB.Checked;

			if (RiceVIWidth_Text.Text.IsSigned())
				ss.RicePlugin.VIWidth = int.Parse(RiceVIWidth_Text.Text);
			else
				ss.RicePlugin.VIWidth = -1;

			if (RiceVIHeight_Text.Text.IsSigned())
				ss.RicePlugin.VIHeight = int.Parse(RiceVIHeight_Text.Text);
			else
				ss.RicePlugin.VIHeight = -1;

			ss.RicePlugin.UseCIWidthAndRatio = RiceUseCIWidthAndRatio_Combo.SelectedIndex;
			ss.RicePlugin.FullTMEM = RiceFullTMEM_Combo.SelectedIndex;
			ss.RicePlugin.TxtSizeMethod2 = RiceTxtSizeMethod2_CB.Checked;
			ss.RicePlugin.EnableTxtLOD = RiceEnableTxtLOD_CB.Checked;
			ss.RicePlugin.FastTextureCRC = RiceFastTextureCRC_Combo.SelectedIndex;
			ss.RicePlugin.EmulateClear = RiceEmulateClear_CB.Checked;
			ss.RicePlugin.ForceScreenClear = RiceForceScreenClear_CB.Checked;
			ss.RicePlugin.AccurateTextureMappingHack = RiceAccurateTextureMappingHack_Combo.SelectedIndex;
			ss.RicePlugin.NormalBlender = RiceNormalBlender_Combo.SelectedIndex;
			ss.RicePlugin.DisableBlender = RiceDisableBlender_CB.Checked;
			ss.RicePlugin.ForceDepthBuffer = RiceForceDepthBuffer_CB.Checked;
			ss.RicePlugin.DisableObjBG = RiceDisableObjBG_CB.Checked;
			ss.RicePlugin.FrameBufferOption = RiceFrameBufferOption_Combo.SelectedIndex;
			ss.RicePlugin.RenderToTextureOption = RiceRenderToTextureOption_Combo.SelectedIndex;
			ss.RicePlugin.ScreenUpdateSettingHack = RiceScreenUpdateSettingHack_Combo.SelectedIndex;
			ss.RicePlugin.EnableHacksForGame = RiceEnableHacksForGame_Combo.SelectedIndex;

			ss.GlidePlugin.autodetect_ucode = Glide_autodetect_ucode.Checked;
			ss.GlidePlugin.ucode = Glide_ucode.SelectedIndex;
			ss.GlidePlugin.flame_corona = Glide_flame_corona.Checked;
			ss.GlidePlugin.card_id = Glide_card_id.SelectedIndex;
			ss.GlidePlugin.tex_filter = Glide_tex_filter.SelectedIndex;
			ss.GlidePlugin.wireframe = Glide_wireframe.Checked;
			ss.GlidePlugin.wfmode = Glide_wfmode.SelectedIndex;
			ss.GlidePlugin.fast_crc = Glide_fast_crc.Checked;
			ss.GlidePlugin.filter_cache = Glide_filter_cache.Checked;
			ss.GlidePlugin.unk_as_red = Glide_unk_as_red.Checked;
			ss.GlidePlugin.fb_read_always = Glide_fb_read_always.Checked;
			ss.GlidePlugin.motionblur = Glide_motionblur.Checked;
			ss.GlidePlugin.fb_render = Glide_fb_render.Checked;
			ss.GlidePlugin.noditheredalpha = Glide_noditheredalpha.Checked;
			ss.GlidePlugin.noglsl = Glide_noglsl.Checked;
			ss.GlidePlugin.fbo = Glide_fbo.Checked;
			ss.GlidePlugin.disable_auxbuf = Glide_disable_auxbuf.Checked;
			ss.GlidePlugin.fb_get_info = Glide_fb_get_info.Checked;

			ss.GlidePlugin.offset_x =
				Glide_offset_x.Text.IsSigned() ? 
				int.Parse(Glide_offset_x.Text) : 0;

			ss.GlidePlugin.offset_y =
				Glide_offset_y.Text.IsSigned() ? 
				int.Parse(Glide_offset_y.Text) : 0;

			ss.GlidePlugin.scale_x =
				Glide_scale_x.Text.IsSigned() ? 
				int.Parse(Glide_scale_x.Text) : 100000;

			ss.GlidePlugin.scale_y =
				Glide_scale_y.Text.IsSigned() ?
				int.Parse(Glide_scale_y.Text) : 100000;

			ss.GlidePlugin.UseDefaultHacks = GlideUseDefaultHacks1.Checked || GlideUseDefaultHacks2.Checked;
			ss.GlidePlugin.alt_tex_size = Glide_alt_tex_size.Checked;
			ss.GlidePlugin.buff_clear = Glide_buff_clear.Checked;
			ss.GlidePlugin.decrease_fillrect_edge = Glide_decrease_fillrect_edge.Checked;
			ss.GlidePlugin.detect_cpu_write = Glide_detect_cpu_write.Checked;
			ss.GlidePlugin.fb_clear = Glide_fb_clear.Checked;
			ss.GlidePlugin.fb_hires = Glide_fb_hires.Checked;
			ss.GlidePlugin.fb_read_alpha = Glide_fb_read_alpha.Checked;
			ss.GlidePlugin.fb_smart = Glide_fb_smart.Checked;
			ss.GlidePlugin.fillcolor_fix = Glide_fillcolor_fix.Checked;
			ss.GlidePlugin.fog = Glide_fog.Checked;
			ss.GlidePlugin.force_depth_compare = Glide_force_depth_compare.Checked;
			ss.GlidePlugin.force_microcheck = Glide_force_microcheck.Checked;
			ss.GlidePlugin.fb_hires_buf_clear = Glide_fb_hires_buf_clear.Checked;
			ss.GlidePlugin.fb_ignore_aux_copy = Glide_fb_ignore_aux_copy.Checked;
			ss.GlidePlugin.fb_ignore_previous = Glide_fb_ignore_previous.Checked;
			ss.GlidePlugin.increase_primdepth = Glide_increase_primdepth.Checked;
			ss.GlidePlugin.increase_texrect_edge = Glide_increase_texrect_edge.Checked;
			ss.GlidePlugin.fb_optimize_texrect = Glide_fb_optimize_texrect.Checked;
			ss.GlidePlugin.fb_optimize_write = Glide_fb_optimize_write.Checked;
			ss.GlidePlugin.PPL = Glide_PPL.Checked;
			ss.GlidePlugin.soft_depth_compare = Glide_soft_depth_compare.Checked;
			ss.GlidePlugin.use_sts1_only = Glide_use_sts1_only.Checked;
			ss.GlidePlugin.wrap_big_tex = Glide_wrap_big_tex.Checked;

			ss.GlidePlugin.depth_bias =
				Glide_depth_bias.Text.IsSigned() ?
				int.Parse(Glide_depth_bias.Text) : 20;

			ss.GlidePlugin.filtering = Glide_filtering.SelectedIndex;

			ss.GlidePlugin.fix_tex_coord = Glide_fix_tex_coord.Text.IsSigned() ?
				int.Parse(Glide_fix_tex_coord.Text) : 0;

			ss.GlidePlugin.lodmode = Glide_lodmode.SelectedIndex;

			ss.GlidePlugin.stipple_mode =
				Glide_stipple_mode.Text.IsSigned() ?
				int.Parse(Glide_stipple_mode.Text) : 2;

			ss.GlidePlugin.stipple_pattern =
				Glide_stipple_pattern.Text.IsSigned() ?
				int.Parse(Glide_stipple_pattern.Text) : 1041204192;

			ss.GlidePlugin.swapmode = Glide_swapmode.SelectedIndex;
			ss.GlidePlugin.enable_hacks_for_game = Glide_enable_hacks_for_game.SelectedIndex;

			ss.Glide64mk2Plugin.card_id = Glide64mk2_card_id.SelectedIndex;
			ss.Glide64mk2Plugin.wrpFBO = Glide64mk2_wrpFBO.Checked;
			ss.Glide64mk2Plugin.wrpAnisotropic = Glide64mk2_wrpAnisotropic.Checked;
			ss.Glide64mk2Plugin.fb_get_info = Glide64mk2_fb_get_info.Checked;
			ss.Glide64mk2Plugin.fb_render = Glide64mk2_fb_render.Checked;

			ss.Glide64mk2Plugin.UseDefaultHacks = Glide64mk2_UseDefaultHacks1.Checked || Glide64mk2_UseDefaultHacks2.Checked;

			ss.Glide64mk2Plugin.use_sts1_only = Glide64mk2_use_sts1_only.Checked;
			ss.Glide64mk2Plugin.optimize_texrect = Glide64mk2_optimize_texrect.Checked;
			ss.Glide64mk2Plugin.increase_texrect_edge = Glide64mk2_increase_texrect_edge.Checked;
			ss.Glide64mk2Plugin.ignore_aux_copy = Glide64mk2_ignore_aux_copy.Checked;
			ss.Glide64mk2Plugin.hires_buf_clear = Glide64mk2_hires_buf_clear.Checked;
			ss.Glide64mk2Plugin.force_microcheck = Glide64mk2_force_microcheck.Checked;
			ss.Glide64mk2Plugin.fog = Glide64mk2_fog.Checked;
			ss.Glide64mk2Plugin.fb_smart = Glide64mk2_fb_smart.Checked;
			ss.Glide64mk2Plugin.fb_read_alpha = Glide64mk2_fb_read_alpha.Checked;
			ss.Glide64mk2Plugin.fb_hires = Glide64mk2_fb_hires.Checked;
			ss.Glide64mk2Plugin.detect_cpu_write = Glide64mk2_detect_cpu_write.Checked;
			ss.Glide64mk2Plugin.decrease_fillrect_edge = Glide64mk2_decrease_fillrect_edge.Checked;
			ss.Glide64mk2Plugin.buff_clear = Glide64mk2_buff_clear.Checked;
			ss.Glide64mk2Plugin.alt_tex_size = Glide64mk2_alt_tex_size.Checked;
			ss.Glide64mk2Plugin.swapmode = Glide64mk2_swapmode.SelectedIndex;

			ss.Glide64mk2Plugin.stipple_pattern =
				Glide64mk2_stipple_pattern.Text.IsSigned() ?
				int.Parse(Glide64mk2_stipple_pattern.Text) : 1041204192;

			ss.Glide64mk2Plugin.stipple_mode =
				Glide64mk2_stipple_mode.Text.IsSigned() ?
				int.Parse(Glide64mk2_stipple_mode.Text) : 2;

			ss.Glide64mk2Plugin.lodmode = Glide64mk2_lodmode.SelectedIndex;
			ss.Glide64mk2Plugin.filtering = Glide64mk2_filtering.SelectedIndex;
			ss.Glide64mk2Plugin.correct_viewport = Glide64mk2_correct_viewport.Checked;
			ss.Glide64mk2Plugin.force_calc_sphere = Glide64mk2_force_calc_sphere.Checked;
			ss.Glide64mk2Plugin.pal230 = Glide64mk2_pal230.Checked;
			ss.Glide64mk2Plugin.texture_correction = Glide64mk2_texture_correction.Checked;
			ss.Glide64mk2Plugin.n64_z_scale = Glide64mk2_n64_z_scale.Checked;
			ss.Glide64mk2Plugin.old_style_adither = Glide64mk2_old_style_adither.Checked;
			ss.Glide64mk2Plugin.zmode_compare_less = Glide64mk2_zmode_compare_less.Checked;
			ss.Glide64mk2Plugin.adjust_aspect = Glide64mk2_adjust_aspect.Checked;
			ss.Glide64mk2Plugin.clip_zmax = Glide64mk2_clip_zmax.Checked;
			ss.Glide64mk2Plugin.clip_zmin = Glide64mk2_clip_zmin.Checked;
			ss.Glide64mk2Plugin.force_quad3d = Glide64mk2_force_quad3d.Checked;
			ss.Glide64mk2Plugin.useless_is_useless = Glide64mk2_useless_is_useless.Checked;
			ss.Glide64mk2Plugin.fb_read_always = Glide64mk2_fb_read_always.Checked;
			ss.Glide64mk2Plugin.aspectmode = Glide64mk2_aspectmode.SelectedIndex;
			ss.Glide64mk2Plugin.fb_crc_mode = Glide64mk2_fb_crc_mode.SelectedIndex;
			ss.Glide64mk2Plugin.enable_hacks_for_game = Glide64mk2_enable_hacks_for_game.SelectedIndex;
			ss.Glide64mk2Plugin.read_back_to_screen = Glide64mk2_read_back_to_screen.SelectedIndex;
			ss.Glide64mk2Plugin.fast_crc = Glide64mk2_fast_crc.Checked;

			ss.GLideN64Plugin.UseDefaultHacks = GLideN64_UseDefaultHacks.Checked;

			switch (GLideN64_MultiSampling.SelectedIndex)
			{
				case 0: ss.GLideN64Plugin.MultiSampling = 0; break;
				case 1: ss.GLideN64Plugin.MultiSampling = 2; break;
				case 2: ss.GLideN64Plugin.MultiSampling = 4; break;
				case 3: ss.GLideN64Plugin.MultiSampling = 8; break;
				case 4: ss.GLideN64Plugin.MultiSampling = 16; break;
				default: ss.GLideN64Plugin.MultiSampling = 0; break;
			}
			ss.GLideN64Plugin.AspectRatio = GLideN64_AspectRatio.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.AspectRatioMode>();
			ss.GLideN64Plugin.BufferSwapMode = GLideN64_BufferSwapMode.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.SwapMode>();
			if (GLideN64_UseNativeResolutionFactor.Text.IsSigned())
				ss.GLideN64Plugin.UseNativeResolutionFactor = int.Parse(GLideN64_UseNativeResolutionFactor.Text);
			else
				ss.GLideN64Plugin.UseNativeResolutionFactor = 0;
			ss.GLideN64Plugin.bilinearMode = GLideN64_bilinearMode.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.bilinearFilteringMode>();
			ss.GLideN64Plugin.MaxAnisotropy = GLideN64_MaxAnisotropy.Checked;
			if (GLideN64_CacheSize.Text.IsSigned())
				ss.GLideN64Plugin.CacheSize = int.Parse(GLideN64_CacheSize.Text);
			else
				ss.GLideN64Plugin.CacheSize = 500;
			ss.GLideN64Plugin.EnableNoise = GLideN64_EnableNoise.Checked;
			ss.GLideN64Plugin.EnableLOD = GLideN64_EnableLOD.Checked;
			ss.GLideN64Plugin.EnableHWLighting = GLideN64_HWLighting.Checked;
			ss.GLideN64Plugin.EnableShadersStorage = GLideN64_ShadersStorage.Checked;
			ss.GLideN64Plugin.CorrectTexrectCoords = GLideN64_CorrectTexrectCoords.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.TexrectCoordsMode>();
			ss.GLideN64Plugin.EnableNativeResTexrects = GLideN64_NativeResTexrects.Checked;
			ss.GLideN64Plugin.EnableLegacyBlending = GLideN64_LegacyBlending.Checked;
			ss.GLideN64Plugin.EnableFragmentDepthWrite = GLideN64_FragmentDepthWrite.Checked;
			ss.GLideN64Plugin.EnableFBEmulation = GLideN64_EnableFBEmulation.Checked;
			ss.GLideN64Plugin.DisableFBInfo = GLideN64_DisableFBInfo.Checked;
			ss.GLideN64Plugin.FBInfoReadColorChunk = GLideN64_FBInfoReadColorChunk.Checked;
			ss.GLideN64Plugin.FBInfoReadDepthChunk = GLideN64_FBInfoReadDepthChunk.Checked;
			ss.GLideN64Plugin.txFilterMode = GLideN64_txFilterMode.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.TextureFilterMode>();
			ss.GLideN64Plugin.txEnhancementMode = GLideN64_txEnhancementMode.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.TextureEnhancementMode>();
			ss.GLideN64Plugin.txDeposterize = GLideN64_txDeposterize.Checked;
			ss.GLideN64Plugin.txFilterIgnoreBG = GLideN64_txFilterIgnoreBG.Checked;
			if (GLideN64_txCacheSize.Text.IsSigned())
				ss.GLideN64Plugin.txCacheSize = int.Parse(GLideN64_txCacheSize.Text);
			else
				ss.GLideN64Plugin.txCacheSize = 100;
			ss.GLideN64Plugin.txHiresEnable = GLideN64_txHiresEnable.Checked;
			ss.GLideN64Plugin.txHiresFullAlphaChannel = GLideN64_txHiresFullAlphaChannel.Checked;
			ss.GLideN64Plugin.txHresAltCRC = GLideN64_txHresAltCRC.Checked;
			ss.GLideN64Plugin.txDump = GLideN64_txDump.Checked;
			ss.GLideN64Plugin.txCacheCompression = GLideN64_txCacheCompression.Checked;
			ss.GLideN64Plugin.txForce16bpp = GLideN64_txForce16bpp.Checked;
			ss.GLideN64Plugin.txSaveCache = GLideN64_txSaveCache.Checked;
			ss.GLideN64Plugin.txPath = GLideN64_txPath.Text;
			ss.GLideN64Plugin.EnableBloom = GLideN64_EnableBloom.Checked;
			ss.GLideN64Plugin.bloomThresholdLevel = GLideN64_bloomThresholdLevel.SelectedIndex + 2;
			ss.GLideN64Plugin.bloomBlendMode = GLideN64_bloomBlendMode.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.BlendMode>();
			ss.GLideN64Plugin.blurAmount = GLideN64_blurAmount.SelectedIndex + 2;
			ss.GLideN64Plugin.blurStrength = GLideN64_blurStrength.SelectedIndex + 10;
			ss.GLideN64Plugin.ForceGammaCorrection = GLideN64_ForceGammaCorrection.Checked;
			if (GLideN64_GammaCorrectionLevel.Text.IsFloat())
				ss.GLideN64Plugin.GammaCorrectionLevel = float.Parse(GLideN64_GammaCorrectionLevel.Text);
			else
				ss.GLideN64Plugin.GammaCorrectionLevel = 2.0f;

			ss.GLideN64Plugin.EnableN64DepthCompare = GLideN64_EnableN64DepthCompare.Checked;
			ss.GLideN64Plugin.EnableCopyColorToRDRAM = GLideN64_EnableCopyColorToRDRAM.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.CopyColorToRDRAMMode>();
			ss.GLideN64Plugin.EnableCopyDepthToRDRAM = GLideN64_EnableCopyDepthToRDRAM.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64GLideN64PluginSettings.CopyDepthToRDRAMMode>();
			ss.GLideN64Plugin.EnableCopyColorFromRDRAM = GLideN64_EnableCopyColorFromRDRAM.Checked;
			ss.GLideN64Plugin.EnableCopyAuxiliaryToRDRAM = GLideN64_EnableCopyAuxiliaryToRDRAM.Checked;


			ss.Core = CoreTypeDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.CoreType>();

			ss.Rsp = RspTypeDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.RspType>();

			PutSettings(s);
			PutSyncSettings(ss);
		} 

		private void N64VideoPluginconfig_Load(object sender, EventArgs e)
		{
			s = GetSettings();
			ss = GetSyncSettings();

			CoreTypeDropdown.PopulateFromEnum<N64SyncSettings.CoreType>(ss.Core);
			RspTypeDropdown.PopulateFromEnum<N64SyncSettings.RspType>(ss.Rsp);

			switch (ss.VideoPlugin)
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
				case PluginType.Jabo:
					PluginComboBox.Text = "Jabo 1.6.1";
					break;
				case PluginType.GLideN64:
					PluginComboBox.Text = "GLideN64";
					break;
			}

			VideoResolutionXTextBox.Text = s.VideoSizeX.ToString();
			VideoResolutionYTextBox.Text = s.VideoSizeY.ToString();

			var video_setting = s.VideoSizeX
						+ " x "
						+ s.VideoSizeY;

			var index = VideoResolutionComboBox.Items.IndexOf(video_setting);
			if (index >= 0)
			{
				VideoResolutionComboBox.SelectedIndex = index;
			}
			else if (PluginComboBox.SelectedIndex != 4)
			{
				VideoResolutionComboBox.SelectedIndex = 13;
				ShowCustomVideoResolutionControls();
			}

			// Jabo
			JaboUseForGameCheckbox.Checked = ss.JaboPlugin.UseDefaultHacks;
			JaboClearModeDropDown
				.PopulateFromEnum<N64SyncSettings.N64JaboPluginSettings.Direct3DClearMode>(ss.JaboPlugin.clear_mode);
			JaboResolutionWidthBox.Text = ss.JaboPlugin.resolution_width.ToString();
			JaboResolutionHeightBox.Text = ss.JaboPlugin.resolution_height.ToString();

			JaboUpdateHacksSection();

			JaboAnisotropicFilteringLevelDropdown
				.PopulateFromEnum<N64SyncSettings.N64JaboPluginSettings.ANISOTROPIC_FILTERING_LEVEL>(ss.JaboPlugin.anisotropic_level);
			JaboAntialiasingLevelDropdown
				.PopulateFromEnum<N64SyncSettings.N64JaboPluginSettings.ANTIALIASING_LEVEL>(ss.JaboPlugin.antialiasing_level);
			JaboBrightnessBox.Value = ss.JaboPlugin.brightness;
			JaboSuper2xsalCheckbox.Checked = ss.JaboPlugin.super2xsal;
			JaboTextureFilterCheckbox.Checked = ss.JaboPlugin.texture_filter;
			JaboAdjustAspectRatioCheckbox.Checked = ss.JaboPlugin.adjust_aspect_ratio;
			JaboLegacyPixelPipelineCheckbox.Checked = ss.JaboPlugin.legacy_pixel_pipeline;
			JaboAlphaBlendingCheckbox.Checked = ss.JaboPlugin.alpha_blending;
			JaboDirect3DPipelineCheckbox.Checked = ss.JaboPlugin.direct3d_transformation_pipeline;
			JaboZCompareCheckbox.Checked = ss.JaboPlugin.z_compare;
			JaboCopyFrameBufferCheckbox.Checked = ss.JaboPlugin.copy_framebuffer;
			

			//Rice
			RiceNormalAlphaBlender_CB.Checked = ss.RicePlugin.NormalAlphaBlender;
			RiceFastTextureLoading_CB.Checked = ss.RicePlugin.FastTextureLoading;
			RiceAccurateTextureMapping_CB.Checked = ss.RicePlugin.AccurateTextureMapping;
			RiceInN64Resolution_CB.Checked = ss.RicePlugin.InN64Resolution;
			RiceSaveVRAM_CB.Checked = ss.RicePlugin.SaveVRAM;
			RiceDoubleSizeForSmallTxtrBuf_CB.Checked = ss.RicePlugin.DoubleSizeForSmallTxtrBuf;
			RiceDefaultCombinerDisable_CB.Checked = ss.RicePlugin.DefaultCombinerDisable;
			RiceEnableHacks_CB.Checked = ss.RicePlugin.EnableHacks;
			RiceWinFrameMode_CB.Checked = ss.RicePlugin.WinFrameMode;
			RiceFullTMEMEmulation_CB.Checked = ss.RicePlugin.FullTMEMEmulation;
			RiceOpenGLVertexClipper_CB.Checked = ss.RicePlugin.OpenGLVertexClipper;
			RiceEnableSSE_CB.Checked = ss.RicePlugin.EnableSSE;
			RiceEnableVertexShader_CB.Checked = ss.RicePlugin.EnableVertexShader;
			RiceSkipFrame_CB.Checked = ss.RicePlugin.SkipFrame;
			RiceTexRectOnly_CB.Checked = ss.RicePlugin.TexRectOnly;
			RiceSmallTextureOnly_CB.Checked = ss.RicePlugin.SmallTextureOnly;
			RiceLoadHiResCRCOnly_CB.Checked = ss.RicePlugin.LoadHiResCRCOnly;
			RiceLoadHiResTextures_CB.Checked = ss.RicePlugin.LoadHiResTextures;
			RiceDumpTexturesToFiles_CB.Checked = ss.RicePlugin.DumpTexturesToFiles;

			RiceFrameBufferSetting_Combo.SelectedIndex = ss.RicePlugin.FrameBufferSetting;
			RiceFrameBufferWriteBackControl_Combo.SelectedIndex = ss.RicePlugin.FrameBufferWriteBackControl;
			RiceRenderToTexture_Combo.SelectedIndex = ss.RicePlugin.RenderToTexture;
			RiceScreenUpdateSetting_Combo.SelectedIndex = ss.RicePlugin.ScreenUpdateSetting;
			RiceMipmapping_Combo.SelectedIndex = ss.RicePlugin.Mipmapping;
			RiceFogMethod_Combo.SelectedIndex = ss.RicePlugin.FogMethod;
			RiceForceTextureFilter_Combo.SelectedIndex = ss.RicePlugin.ForceTextureFilter;
			RiceTextureEnhancement_Combo.SelectedIndex = ss.RicePlugin.TextureEnhancement;
			RiceTextureEnhancementControl_Combo.SelectedIndex = ss.RicePlugin.TextureEnhancementControl;
			RiceTextureQuality_Combo.SelectedIndex = ss.RicePlugin.TextureQuality;
			RiceOpenGLDepthBufferSetting_Combo.SelectedIndex = (ss.RicePlugin.OpenGLDepthBufferSetting / 16) - 1;
			switch (ss.RicePlugin.MultiSampling)
			{
				case 0: RiceMultiSampling_Combo.SelectedIndex = 0; break;
				case 2: RiceMultiSampling_Combo.SelectedIndex = 1; break;
				case 4: RiceMultiSampling_Combo.SelectedIndex = 2; break;
				case 8: RiceMultiSampling_Combo.SelectedIndex = 3; break;
				case 16: RiceMultiSampling_Combo.SelectedIndex = 4; break;
				default: RiceMultiSampling_Combo.SelectedIndex = 0; break;
			}
			RiceColorQuality_Combo.SelectedIndex = ss.RicePlugin.ColorQuality;
			RiceOpenGLRenderSetting_Combo.SelectedIndex = ss.RicePlugin.OpenGLRenderSetting;
			RiceAnisotropicFiltering_TB.Value = ss.RicePlugin.AnisotropicFiltering;
			AnisotropicFiltering_LB.Text = "Anisotropic Filtering: " + RiceAnisotropicFiltering_TB.Value;

			RiceUseDefaultHacks_CB.Checked = ss.RicePlugin.UseDefaultHacks;

			UpdateRiceHacksSection();
			if (!ss.RicePlugin.UseDefaultHacks)
			{
				RiceTexture1Hack_CB.Checked = ss.RicePlugin.Texture1Hack;

				RiceDisableTextureCRC_CB.Checked = ss.RicePlugin.DisableTextureCRC;
				RiceDisableCulling_CB.Checked = ss.RicePlugin.DisableCulling;
				RiceIncTexRectEdge_CB.Checked = ss.RicePlugin.IncTexRectEdge;
				RiceZHack_CB.Checked = ss.RicePlugin.ZHack;
				RiceTextureScaleHack_CB.Checked = ss.RicePlugin.TextureScaleHack;
				RicePrimaryDepthHack_CB.Checked = ss.RicePlugin.PrimaryDepthHack;
				RiceTexture1Hack_CB.Checked = ss.RicePlugin.Texture1Hack;
				RiceFastLoadTile_CB.Checked = ss.RicePlugin.FastLoadTile;
				RiceUseSmallerTexture_CB.Checked = ss.RicePlugin.UseSmallerTexture;
				RiceVIWidth_Text.Text = ss.RicePlugin.VIWidth.ToString();
				RiceVIHeight_Text.Text = ss.RicePlugin.VIHeight.ToString();
				RiceUseCIWidthAndRatio_Combo.SelectedIndex = ss.RicePlugin.UseCIWidthAndRatio;
				RiceFullTMEM_Combo.SelectedIndex = ss.RicePlugin.FullTMEM;
				RiceTxtSizeMethod2_CB.Checked = ss.RicePlugin.TxtSizeMethod2;
				RiceEnableTxtLOD_CB.Checked = ss.RicePlugin.EnableTxtLOD;
				RiceFastTextureCRC_Combo.SelectedIndex = ss.RicePlugin.FastTextureCRC;
				RiceEmulateClear_CB.Checked = ss.RicePlugin.EmulateClear;
				RiceForceScreenClear_CB.Checked = ss.RicePlugin.ForceScreenClear;
				RiceAccurateTextureMappingHack_Combo.SelectedIndex = ss.RicePlugin.AccurateTextureMappingHack;
				RiceNormalBlender_Combo.SelectedIndex = ss.RicePlugin.NormalBlender;
				RiceDisableBlender_CB.Checked = ss.RicePlugin.DisableBlender;
				RiceForceDepthBuffer_CB.Checked = ss.RicePlugin.ForceDepthBuffer;
				RiceDisableObjBG_CB.Checked = ss.RicePlugin.DisableObjBG;
				RiceFrameBufferOption_Combo.SelectedIndex = ss.RicePlugin.FrameBufferOption;
				RiceRenderToTextureOption_Combo.SelectedIndex = ss.RicePlugin.RenderToTextureOption;
				RiceScreenUpdateSettingHack_Combo.SelectedIndex = ss.RicePlugin.ScreenUpdateSettingHack;
				RiceEnableHacksForGame_Combo.SelectedIndex = ss.RicePlugin.EnableHacksForGame;
			}

			Glide_autodetect_ucode.Checked = ss.GlidePlugin.autodetect_ucode;
			Glide_ucode.SelectedIndex = ss.GlidePlugin.ucode;
			Glide_flame_corona.Checked = ss.GlidePlugin.flame_corona;
			Glide_card_id.SelectedIndex = ss.GlidePlugin.card_id;
			Glide_tex_filter.SelectedIndex = ss.GlidePlugin.tex_filter;
			Glide_wireframe.Checked = ss.GlidePlugin.wireframe;
			Glide_wfmode.SelectedIndex = ss.GlidePlugin.wfmode;
			Glide_fast_crc.Checked = ss.GlidePlugin.fast_crc;
			Glide_filter_cache.Checked = ss.GlidePlugin.filter_cache;
			Glide_unk_as_red.Checked = ss.GlidePlugin.unk_as_red;
			Glide_fb_read_always.Checked = ss.GlidePlugin.fb_read_always;
			Glide_motionblur.Checked = ss.GlidePlugin.motionblur;
			Glide_fb_render.Checked = ss.GlidePlugin.fb_render;
			Glide_noditheredalpha.Checked = ss.GlidePlugin.noditheredalpha;
			Glide_noglsl.Checked = ss.GlidePlugin.noglsl;
			Glide_fbo.Checked = ss.GlidePlugin.fbo;
			Glide_disable_auxbuf.Checked = ss.GlidePlugin.disable_auxbuf;
			Glide_fb_get_info.Checked = ss.GlidePlugin.fb_get_info;
			Glide_offset_x.Text = ss.GlidePlugin.offset_x.ToString();
			Glide_offset_y.Text = ss.GlidePlugin.offset_y.ToString();
			Glide_scale_x.Text = ss.GlidePlugin.scale_x.ToString();
			Glide_scale_y.Text = ss.GlidePlugin.scale_y.ToString();
			

			GlideUseDefaultHacks1.Checked = ss.GlidePlugin.UseDefaultHacks;
			GlideUseDefaultHacks2.Checked = ss.GlidePlugin.UseDefaultHacks;

			UpdateGlideHacksSection();
			if (!ss.GlidePlugin.UseDefaultHacks)
			{
				Glide_alt_tex_size.Checked = ss.GlidePlugin.alt_tex_size;
				Glide_buff_clear.Checked = ss.GlidePlugin.buff_clear;
				Glide_decrease_fillrect_edge.Checked = ss.GlidePlugin.decrease_fillrect_edge;
				Glide_detect_cpu_write.Checked = ss.GlidePlugin.detect_cpu_write;
				Glide_fb_clear.Checked = ss.GlidePlugin.fb_clear;
				Glide_fb_hires.Checked = ss.GlidePlugin.fb_hires;
				Glide_fb_read_alpha.Checked = ss.GlidePlugin.fb_read_alpha;
				Glide_fb_smart.Checked = ss.GlidePlugin.fb_smart;
				Glide_fillcolor_fix.Checked = ss.GlidePlugin.fillcolor_fix;
				Glide_fog.Checked = ss.GlidePlugin.fog;
				Glide_force_depth_compare.Checked = ss.GlidePlugin.force_depth_compare;
				Glide_force_microcheck.Checked = ss.GlidePlugin.force_microcheck;
				Glide_fb_hires_buf_clear.Checked = ss.GlidePlugin.fb_hires_buf_clear;
				Glide_fb_ignore_aux_copy.Checked = ss.GlidePlugin.fb_ignore_aux_copy;
				Glide_fb_ignore_previous.Checked = ss.GlidePlugin.fb_ignore_previous;
				Glide_increase_primdepth.Checked = ss.GlidePlugin.increase_primdepth;
				Glide_increase_texrect_edge.Checked = ss.GlidePlugin.increase_texrect_edge;
				Glide_fb_optimize_texrect.Checked = ss.GlidePlugin.fb_optimize_texrect;
				Glide_fb_optimize_write.Checked = ss.GlidePlugin.fb_optimize_write;
				Glide_PPL.Checked = ss.GlidePlugin.PPL;
				Glide_soft_depth_compare.Checked = ss.GlidePlugin.soft_depth_compare;
				Glide_use_sts1_only.Checked = ss.GlidePlugin.use_sts1_only;
				Glide_wrap_big_tex.Checked = ss.GlidePlugin.wrap_big_tex;

				Glide_depth_bias.Text = ss.GlidePlugin.depth_bias.ToString();
				Glide_filtering.SelectedIndex = ss.GlidePlugin.filtering;
				Glide_fix_tex_coord.Text = ss.GlidePlugin.fix_tex_coord.ToString();
				Glide_lodmode.SelectedIndex = ss.GlidePlugin.lodmode;
				Glide_stipple_mode.Text = ss.GlidePlugin.stipple_mode.ToString();
				Glide_stipple_pattern.Text = ss.GlidePlugin.stipple_pattern.ToString();
				Glide_swapmode.SelectedIndex = ss.GlidePlugin.swapmode;
				Glide_enable_hacks_for_game.SelectedIndex = ss.GlidePlugin.enable_hacks_for_game;
			}

			Glide64mk2_card_id.SelectedIndex = ss.Glide64mk2Plugin.card_id;
			Glide64mk2_wrpFBO.Checked = ss.Glide64mk2Plugin.wrpFBO;
			Glide64mk2_wrpAnisotropic.Checked = ss.Glide64mk2Plugin.wrpAnisotropic;
			Glide64mk2_fb_get_info.Checked = ss.Glide64mk2Plugin.fb_get_info;
			Glide64mk2_fb_render.Checked = ss.Glide64mk2Plugin.fb_render;

			Glide64mk2_UseDefaultHacks1.Checked = ss.Glide64mk2Plugin.UseDefaultHacks;
			Glide64mk2_UseDefaultHacks2.Checked = ss.Glide64mk2Plugin.UseDefaultHacks;

			UpdateGlide64mk2HacksSection();
			if (!ss.Glide64mk2Plugin.UseDefaultHacks)
			{
				Glide64mk2_use_sts1_only.Checked = ss.Glide64mk2Plugin.use_sts1_only;
				Glide64mk2_optimize_texrect.Checked = ss.Glide64mk2Plugin.optimize_texrect;
				Glide64mk2_increase_texrect_edge.Checked = ss.Glide64mk2Plugin.increase_texrect_edge;
				Glide64mk2_ignore_aux_copy.Checked = ss.Glide64mk2Plugin.ignore_aux_copy;
				Glide64mk2_hires_buf_clear.Checked = ss.Glide64mk2Plugin.hires_buf_clear;
				Glide64mk2_force_microcheck.Checked = ss.Glide64mk2Plugin.force_microcheck;
				Glide64mk2_fog.Checked = ss.Glide64mk2Plugin.fog;
				Glide64mk2_fb_smart.Checked = ss.Glide64mk2Plugin.fb_smart;
				Glide64mk2_fb_read_alpha.Checked = ss.Glide64mk2Plugin.fb_read_alpha;
				Glide64mk2_fb_hires.Checked = ss.Glide64mk2Plugin.fb_hires;
				Glide64mk2_detect_cpu_write.Checked = ss.Glide64mk2Plugin.detect_cpu_write;
				Glide64mk2_decrease_fillrect_edge.Checked = ss.Glide64mk2Plugin.decrease_fillrect_edge;
				Glide64mk2_buff_clear.Checked = ss.Glide64mk2Plugin.buff_clear;
				Glide64mk2_alt_tex_size.Checked = ss.Glide64mk2Plugin.alt_tex_size;
				Glide64mk2_swapmode.SelectedIndex = ss.Glide64mk2Plugin.swapmode;
				Glide64mk2_stipple_pattern.Text = ss.Glide64mk2Plugin.stipple_pattern.ToString();
				Glide64mk2_stipple_mode.Text = ss.Glide64mk2Plugin.stipple_mode.ToString();
				Glide64mk2_lodmode.SelectedIndex = ss.Glide64mk2Plugin.lodmode;
				Glide64mk2_filtering.SelectedIndex = ss.Glide64mk2Plugin.filtering;
				Glide64mk2_correct_viewport.Checked = ss.Glide64mk2Plugin.correct_viewport;
				Glide64mk2_force_calc_sphere.Checked = ss.Glide64mk2Plugin.force_calc_sphere;
				Glide64mk2_pal230.Checked = ss.Glide64mk2Plugin.pal230;
				Glide64mk2_texture_correction.Checked = ss.Glide64mk2Plugin.texture_correction;
				Glide64mk2_n64_z_scale.Checked = ss.Glide64mk2Plugin.n64_z_scale;
				Glide64mk2_old_style_adither.Checked = ss.Glide64mk2Plugin.old_style_adither;
				Glide64mk2_zmode_compare_less.Checked = ss.Glide64mk2Plugin.zmode_compare_less;
				Glide64mk2_adjust_aspect.Checked = ss.Glide64mk2Plugin.adjust_aspect;
				Glide64mk2_clip_zmax.Checked = ss.Glide64mk2Plugin.clip_zmax;
				Glide64mk2_clip_zmin.Checked = ss.Glide64mk2Plugin.clip_zmin;
				Glide64mk2_force_quad3d.Checked = ss.Glide64mk2Plugin.force_quad3d;
				Glide64mk2_useless_is_useless.Checked = ss.Glide64mk2Plugin.useless_is_useless;
				Glide64mk2_fb_read_always.Checked = ss.Glide64mk2Plugin.fb_read_always;
				Glide64mk2_aspectmode.SelectedIndex = ss.Glide64mk2Plugin.aspectmode;
				Glide64mk2_fb_crc_mode.SelectedIndex = ss.Glide64mk2Plugin.fb_crc_mode;
				Glide64mk2_enable_hacks_for_game.SelectedIndex = ss.Glide64mk2Plugin.enable_hacks_for_game;
				Glide64mk2_read_back_to_screen.SelectedIndex = ss.Glide64mk2Plugin.read_back_to_screen;
				Glide64mk2_fast_crc.Checked = ss.Glide64mk2Plugin.fast_crc;
			}

			// GLideN64
			GLideN64_UseDefaultHacks.Checked = ss.GLideN64Plugin.UseDefaultHacks;

			switch (ss.GLideN64Plugin.MultiSampling)
			{
				case 0: GLideN64_MultiSampling.SelectedIndex = 0; break;
				case 2: GLideN64_MultiSampling.SelectedIndex = 1; break;
				case 4: GLideN64_MultiSampling.SelectedIndex = 2; break;
				case 8: GLideN64_MultiSampling.SelectedIndex = 3; break;
				case 16: GLideN64_MultiSampling.SelectedIndex = 4; break;
				default: GLideN64_MultiSampling.SelectedIndex = 0; break;
			}
			GLideN64_AspectRatio
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.AspectRatioMode>(ss.GLideN64Plugin.AspectRatio);
			GLideN64_BufferSwapMode
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.SwapMode>(ss.GLideN64Plugin.BufferSwapMode);
			GLideN64_UseNativeResolutionFactor.Text = ss.GLideN64Plugin.UseNativeResolutionFactor.ToString();
			GLideN64_bilinearMode
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.bilinearFilteringMode>(ss.GLideN64Plugin.bilinearMode);
			GLideN64_MaxAnisotropy.Checked = ss.GLideN64Plugin.MaxAnisotropy;
			GLideN64_CacheSize.Text = ss.GLideN64Plugin.CacheSize.ToString();
			GLideN64_EnableNoise.Checked = ss.GLideN64Plugin.EnableNoise;
			GLideN64_EnableLOD.Checked = ss.GLideN64Plugin.EnableLOD;
			GLideN64_HWLighting.Checked = ss.GLideN64Plugin.EnableHWLighting;
			GLideN64_ShadersStorage.Checked = ss.GLideN64Plugin.EnableShadersStorage;
			GLideN64_CorrectTexrectCoords
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.TexrectCoordsMode>(ss.GLideN64Plugin.CorrectTexrectCoords);
			GLideN64_NativeResTexrects.Checked = ss.GLideN64Plugin.EnableNativeResTexrects;
			GLideN64_LegacyBlending.Checked = ss.GLideN64Plugin.EnableLegacyBlending;
			GLideN64_FragmentDepthWrite.Checked = ss.GLideN64Plugin.EnableFragmentDepthWrite;
			GLideN64_EnableFBEmulation.Checked = ss.GLideN64Plugin.EnableFBEmulation;
			GLideN64_DisableFBInfo.Checked = ss.GLideN64Plugin.DisableFBInfo;
			GLideN64_FBInfoReadColorChunk.Checked = ss.GLideN64Plugin.FBInfoReadColorChunk;
			GLideN64_FBInfoReadDepthChunk.Checked = ss.GLideN64Plugin.FBInfoReadDepthChunk;
			GLideN64_txFilterMode
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.TextureFilterMode>(ss.GLideN64Plugin.txFilterMode);
			GLideN64_txEnhancementMode
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.TextureEnhancementMode>(ss.GLideN64Plugin.txEnhancementMode);
			GLideN64_txDeposterize.Checked = ss.GLideN64Plugin.txDeposterize;
			GLideN64_txFilterIgnoreBG.Checked = ss.GLideN64Plugin.txFilterIgnoreBG;
			GLideN64_txCacheSize.Text = ss.GLideN64Plugin.txCacheSize.ToString();
			GLideN64_txHiresEnable.Checked = ss.GLideN64Plugin.txHiresEnable;
			GLideN64_txHiresFullAlphaChannel.Checked = ss.GLideN64Plugin.txHiresFullAlphaChannel;
			GLideN64_txHresAltCRC.Checked = ss.GLideN64Plugin.txHresAltCRC;
			GLideN64_txDump.Checked = ss.GLideN64Plugin.txDump;
			GLideN64_txCacheCompression.Checked = ss.GLideN64Plugin.txCacheCompression;
			GLideN64_txForce16bpp.Checked = ss.GLideN64Plugin.txForce16bpp;
			GLideN64_txSaveCache.Checked = ss.GLideN64Plugin.txSaveCache;
			GLideN64_txPath.Text = ss.GLideN64Plugin.txPath;
			GLideN64_EnableBloom.Checked = ss.GLideN64Plugin.EnableBloom;
			GLideN64_bloomThresholdLevel.SelectedIndex = ss.GLideN64Plugin.bloomThresholdLevel - 2;
			GLideN64_bloomBlendMode
				.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.BlendMode>(ss.GLideN64Plugin.bloomBlendMode);
			GLideN64_blurAmount.SelectedIndex = ss.GLideN64Plugin.blurAmount - 2;
			GLideN64_blurStrength.SelectedIndex = ss.GLideN64Plugin.blurStrength - 10;
			GLideN64_ForceGammaCorrection.Checked = ss.GLideN64Plugin.ForceGammaCorrection;
			GLideN64_GammaCorrectionLevel.Text = ss.GLideN64Plugin.GammaCorrectionLevel.ToString();

			UpdateGLideN64HacksSection();
			if (!ss.GLideN64Plugin.UseDefaultHacks)
			{
				GLideN64_EnableN64DepthCompare.Checked = ss.GLideN64Plugin.EnableN64DepthCompare;
				GLideN64_EnableCopyColorToRDRAM
					.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.CopyColorToRDRAMMode>(ss.GLideN64Plugin.EnableCopyColorToRDRAM);
				GLideN64_EnableCopyDepthToRDRAM
					.PopulateFromEnum<N64SyncSettings.N64GLideN64PluginSettings.CopyDepthToRDRAMMode>(ss.GLideN64Plugin.EnableCopyDepthToRDRAM);
				GLideN64_EnableCopyColorFromRDRAM.Checked = ss.GLideN64Plugin.EnableCopyColorFromRDRAM;
				GLideN64_EnableCopyAuxiliaryToRDRAM.Checked = ss.GLideN64Plugin.EnableCopyAuxiliaryToRDRAM;
			}
		}
		
		private void RiceAnisotropicFiltering_TB_Scroll_1(object sender, EventArgs e)
		{
			AnisotropicFiltering_LB.Text = "Anisotropic Filtering: " + RiceAnisotropicFiltering_TB.Value;
		}

		private void RiceUseDefaultHacks_CB_CheckedChanged(object sender, EventArgs e)
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

		public bool GetBoolFromDB(string parameter)
		{
			return Global.Game.OptionPresent(parameter) && Global.Game.OptionValue(parameter) == "true";
		}

		public int GetIntFromDB(string parameter, int defaultVal)
		{
			if (Global.Game.OptionPresent(parameter) && Global.Game.OptionValue(parameter).IsUnsigned())
			{
				return int.Parse(Global.Game.OptionValue(parameter));
			}
			else
			{
				return defaultVal;
			}
		}

		public void ToggleRiceHackCheckboxEnable (bool val)
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

		public void ToggleGlideHackCheckboxEnable(bool val)
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

		public void ToggleGlide64mk2HackCheckboxEnable(bool val)
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
			if (programmaticallyChangingPluginComboBox)
			{
				return;
			}

			if (VideoResolutionComboBox.SelectedItem == null)
			{
				VideoResolutionComboBox.SelectedIndex = 0;
			}

			var strArr = new string[] {};
			int OldSizeX, OldSizeY;

			var oldResolution = VideoResolutionComboBox.SelectedItem.ToString();
			if (oldResolution != "Custom")
			{
				strArr = oldResolution.Split('x');
				OldSizeX = int.Parse(strArr[0].Trim());
				OldSizeY = int.Parse(strArr[1].Trim());
			}
			else
			{
				OldSizeX = int.Parse(VideoResolutionXTextBox.Text);
				OldSizeY = int.Parse(VideoResolutionYTextBox.Text);
			}

			if (PluginComboBox.Text == "Jabo 1.6.1")
			{
				// Change resolution list to jabo
				VideoResolutionComboBox.Items.Clear();
				VideoResolutionComboBox.Items.AddRange(validResolutionsJabo);
			}
			else
			{
				// Change resolution list to the rest
				VideoResolutionComboBox.Items.Clear();
				VideoResolutionComboBox.Items.AddRange(validResolutions);
			}

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
					if ((string)VideoResolutionComboBox.Items[i] != "Custom")
					{
						string option = (string)VideoResolutionComboBox.Items[i];
						strArr = option.Split('x');
						int newSizeX = int.Parse(strArr[0].Trim());
						int newSizeY = int.Parse(strArr[1].Trim());
						if (OldSizeX < newSizeX || OldSizeX == newSizeX && OldSizeY < newSizeY)
						{
							if (i == 0)
							{
								bestFit = 0;
								break;
							}
							else
							{
								bestFit = i - 1;
								break;
							}
						}
					}
				}

				if (bestFit < 0)
				{
					if (PluginComboBox.Text == "Jabo 1.6.1")
					{
						// Pick 8 to avoid picking the widescreen resolutions
						VideoResolutionComboBox.SelectedIndex = 8;
					}
					else
					{
						VideoResolutionComboBox.SelectedIndex = VideoResolutionComboBox.Items.Count - 1;
					}
				}
				else
				{
					VideoResolutionComboBox.SelectedIndex = bestFit;
				}
			}

			previousPluginSelection = PluginComboBox.SelectedItem.ToString();
		}

		private void JaboUseForGameCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			JaboPerGameHacksGroupBox.Controls
				.OfType<Control>()
				.ToList()
				.ForEach(c => c.Enabled = !JaboUseForGameCheckbox.Checked);

			JaboUpdateHacksSection();
		}

		private void JaboUpdateHacksSection()
		{
			if (JaboUseForGameCheckbox.Checked)
			{
				JaboResolutionWidthBox.Text = GetIntFromDB("Jabo_Resolution_Width", -1).ToString();
				JaboResolutionHeightBox.Text = GetIntFromDB("Jabo_Resolution_Height", -1).ToString();
				JaboClearModeDropDown.SelectedItem = ((N64SyncSettings.N64JaboPluginSettings.Direct3DClearMode)GetIntFromDB("Jabo_Clear_Frame", 0)).GetDescription();
			}
		}

		private void VideoResolutionComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (VideoResolutionComboBox.Text == "Custom")
			{
				ShowCustomVideoResolutionControls();
			}
			else
			{
				HideCustomVideoResolutionControls();
				var new_resolution = VideoResolutionComboBox.SelectedItem.ToString();
				var strArr = new_resolution.Split('x');
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
