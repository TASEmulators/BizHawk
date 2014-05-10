using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Common;



namespace BizHawk.Client.EmuHawk
{
	public partial class N64VideoPluginconfig : Form
	{
		public N64VideoPluginconfig()
		{
			InitializeComponent();
		}

		// because mupen is a pile of garbage, this all needs to work even when N64 is not loaded
		// never do this
		static N64SyncSettings GetS()
		{
			if (Global.Emulator is N64)
				return (N64SyncSettings)Global.Emulator.GetSyncSettings();
			else
				return (N64SyncSettings)Global.Config.GetCoreSyncSettings<N64>();
		}

		// never do this
		static void PutS(N64SyncSettings s)
		{
			if (Global.Emulator is N64)
				GlobalWin.MainForm.PutCoreSyncSettings(s);
			else
				// hack, don't do!
				Global.Config.PutCoreSyncSettings<N64>(s);
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
			var s = GetS();

			//Global
			var video_settings = VideoResolutionComboBox.SelectedItem.ToString();
			var strArr = video_settings.Split('x');
			s.VideoSizeX = Int32.Parse(strArr[0].Trim());
			s.VideoSizeY = Int32.Parse(strArr[1].Trim());
			switch (PluginComboBox.Text)
			{
				case "Rice": s.VidPlugin = PLUGINTYPE.RICE; break;
				case "Glide64": s.VidPlugin = PLUGINTYPE.GLIDE; break;
				case "Glide64mk2": s.VidPlugin = PLUGINTYPE.GLIDE64MK2; break;
			}

			//Rice
			s.RicePlugin.NormalAlphaBlender = RiceNormalAlphaBlender_CB.Checked;
			s.RicePlugin.FastTextureLoading = RiceFastTextureLoading_CB.Checked;
			s.RicePlugin.AccurateTextureMapping = RiceAccurateTextureMapping_CB.Checked;
			s.RicePlugin.InN64Resolution = RiceInN64Resolution_CB.Checked;
			s.RicePlugin.SaveVRAM = RiceSaveVRAM_CB.Checked;
			s.RicePlugin.DoubleSizeForSmallTxtrBuf = RiceDoubleSizeForSmallTxtrBuf_CB.Checked;
			s.RicePlugin.DefaultCombinerDisable = RiceDefaultCombinerDisable_CB.Checked;
			s.RicePlugin.EnableHacks = RiceEnableHacks_CB.Checked;
			s.RicePlugin.WinFrameMode = RiceWinFrameMode_CB.Checked;
			s.RicePlugin.FullTMEMEmulation = RiceFullTMEMEmulation_CB.Checked;
			s.RicePlugin.OpenGLVertexClipper = RiceOpenGLVertexClipper_CB.Checked;
			s.RicePlugin.EnableSSE = RiceEnableSSE_CB.Checked;
			s.RicePlugin.EnableVertexShader = RiceEnableVertexShader_CB.Checked;
			s.RicePlugin.SkipFrame = RiceSkipFrame_CB.Checked;
			s.RicePlugin.TexRectOnly = RiceTexRectOnly_CB.Checked;
			s.RicePlugin.SmallTextureOnly = RiceSmallTextureOnly_CB.Checked;
			s.RicePlugin.LoadHiResCRCOnly = RiceLoadHiResCRCOnly_CB.Checked;
			s.RicePlugin.LoadHiResTextures = RiceLoadHiResTextures_CB.Checked;
			s.RicePlugin.DumpTexturesToFiles = RiceDumpTexturesToFiles_CB.Checked;

			s.RicePlugin.FrameBufferSetting = RiceFrameBufferSetting_Combo.SelectedIndex;
			s.RicePlugin.FrameBufferWriteBackControl = RiceFrameBufferWriteBackControl_Combo.SelectedIndex;
			s.RicePlugin.RenderToTexture = RiceRenderToTexture_Combo.SelectedIndex;
			s.RicePlugin.ScreenUpdateSetting = RiceScreenUpdateSetting_Combo.SelectedIndex;
			s.RicePlugin.Mipmapping = RiceMipmapping_Combo.SelectedIndex;
			s.RicePlugin.FogMethod = RiceFogMethod_Combo.SelectedIndex;
			s.RicePlugin.ForceTextureFilter = RiceForceTextureFilter_Combo.SelectedIndex;
			s.RicePlugin.TextureEnhancement = RiceTextureEnhancement_Combo.SelectedIndex;
			s.RicePlugin.TextureEnhancementControl = RiceTextureEnhancementControl_Combo.SelectedIndex;
			s.RicePlugin.TextureQuality = RiceTextureQuality_Combo.SelectedIndex;
			s.RicePlugin.OpenGLDepthBufferSetting = (RiceOpenGLDepthBufferSetting_Combo.SelectedIndex + 1) * 16;
			switch (RiceMultiSampling_Combo.SelectedIndex)
			{
				case 0: s.RicePlugin.MultiSampling = 0; break;
				case 1: s.RicePlugin.MultiSampling = 2; break;
				case 2: s.RicePlugin.MultiSampling = 4; break;
				case 3: s.RicePlugin.MultiSampling = 8; break;
				case 4: s.RicePlugin.MultiSampling = 16; break;
				default: s.RicePlugin.MultiSampling = 0; break;
			}
			s.RicePlugin.ColorQuality = RiceColorQuality_Combo.SelectedIndex;
			s.RicePlugin.OpenGLRenderSetting = RiceOpenGLRenderSetting_Combo.SelectedIndex;
			s.RicePlugin.AnisotropicFiltering = RiceAnisotropicFiltering_TB.Value;

			s.RicePlugin.UseDefaultHacks = RiceUseDefaultHacks_CB.Checked;
			s.RicePlugin.DisableTextureCRC = RiceDisableTextureCRC_CB.Checked;
			s.RicePlugin.DisableCulling = RiceDisableCulling_CB.Checked;
			s.RicePlugin.IncTexRectEdge = RiceIncTexRectEdge_CB.Checked;
			s.RicePlugin.ZHack = RiceZHack_CB.Checked;
			s.RicePlugin.TextureScaleHack = RiceTextureScaleHack_CB.Checked;
			s.RicePlugin.PrimaryDepthHack = RicePrimaryDepthHack_CB.Checked;
			s.RicePlugin.Texture1Hack = RiceTexture1Hack_CB.Checked;
			s.RicePlugin.FastLoadTile = RiceFastLoadTile_CB.Checked;
			s.RicePlugin.UseSmallerTexture = RiceUseSmallerTexture_CB.Checked;

			if (InputValidate.IsSigned(RiceVIWidth_Text.Text))
				s.RicePlugin.VIWidth = int.Parse(RiceVIWidth_Text.Text);
			else
				s.RicePlugin.VIWidth = -1;

			if (InputValidate.IsSigned(RiceVIHeight_Text.Text))
				s.RicePlugin.VIHeight = int.Parse(RiceVIHeight_Text.Text);
			else
				s.RicePlugin.VIHeight = -1;

			s.RicePlugin.UseCIWidthAndRatio = RiceUseCIWidthAndRatio_Combo.SelectedIndex;
			s.RicePlugin.FullTMEM = RiceFullTMEM_Combo.SelectedIndex;
			s.RicePlugin.TxtSizeMethod2 = RiceTxtSizeMethod2_CB.Checked;
			s.RicePlugin.EnableTxtLOD = RiceEnableTxtLOD_CB.Checked;
			s.RicePlugin.FastTextureCRC = RiceFastTextureCRC_Combo.SelectedIndex;
			s.RicePlugin.EmulateClear = RiceEmulateClear_CB.Checked;
			s.RicePlugin.ForceScreenClear = RiceForceScreenClear_CB.Checked;
			s.RicePlugin.AccurateTextureMappingHack = RiceAccurateTextureMappingHack_Combo.SelectedIndex;
			s.RicePlugin.NormalBlender = RiceNormalBlender_Combo.SelectedIndex;
			s.RicePlugin.DisableBlender = RiceDisableBlender_CB.Checked;
			s.RicePlugin.ForceDepthBuffer = RiceForceDepthBuffer_CB.Checked;
			s.RicePlugin.DisableObjBG = RiceDisableObjBG_CB.Checked;
			s.RicePlugin.FrameBufferOption = RiceFrameBufferOption_Combo.SelectedIndex;
			s.RicePlugin.RenderToTextureOption = RiceRenderToTextureOption_Combo.SelectedIndex;
			s.RicePlugin.ScreenUpdateSettingHack = RiceScreenUpdateSettingHack_Combo.SelectedIndex;
			s.RicePlugin.EnableHacksForGame = RiceEnableHacksForGame_Combo.SelectedIndex;

			s.GlidePlugin.autodetect_ucode = Glide_autodetect_ucode.Checked;
			s.GlidePlugin.ucode = Glide_ucode.SelectedIndex;
			s.GlidePlugin.flame_corona = Glide_flame_corona.Checked;
			s.GlidePlugin.card_id = Glide_card_id.SelectedIndex;
			s.GlidePlugin.tex_filter = Glide_tex_filter.SelectedIndex;
			s.GlidePlugin.wireframe = Glide_wireframe.Checked;
			s.GlidePlugin.wfmode = Glide_wfmode.SelectedIndex;
			s.GlidePlugin.fast_crc = Glide_fast_crc.Checked;
			s.GlidePlugin.filter_cache = Glide_filter_cache.Checked;
			s.GlidePlugin.unk_as_red = Glide_unk_as_red.Checked;
			s.GlidePlugin.fb_read_always = Glide_fb_read_always.Checked;
			s.GlidePlugin.motionblur = Glide_motionblur.Checked;
			s.GlidePlugin.fb_render = Glide_fb_render.Checked;
			s.GlidePlugin.noditheredalpha = Glide_noditheredalpha.Checked;
			s.GlidePlugin.noglsl = Glide_noglsl.Checked;
			s.GlidePlugin.fbo = Glide_fbo.Checked;
			s.GlidePlugin.disable_auxbuf = Glide_disable_auxbuf.Checked;
			s.GlidePlugin.fb_get_info = Glide_fb_get_info.Checked;

			s.GlidePlugin.offset_x =
				InputValidate.IsSigned(Glide_offset_x.Text) ? 
				int.Parse(Glide_offset_x.Text) : 0;

			s.GlidePlugin.offset_y =
				InputValidate.IsSigned(Glide_offset_y.Text) ? 
				int.Parse(Glide_offset_y.Text) : 0;

			s.GlidePlugin.scale_x =
				InputValidate.IsSigned(Glide_scale_x.Text) ? 
				int.Parse(Glide_scale_x.Text) : 100000;

			s.GlidePlugin.scale_y =
				InputValidate.IsSigned(Glide_scale_y.Text) ?
				int.Parse(Glide_scale_y.Text) : 100000;

			s.GlidePlugin.UseDefaultHacks = GlideUseDefaultHacks1.Checked || GlideUseDefaultHacks2.Checked;
			s.GlidePlugin.alt_tex_size = Glide_alt_tex_size.Checked;
			s.GlidePlugin.buff_clear = Glide_buff_clear.Checked;
			s.GlidePlugin.decrease_fillrect_edge = Glide_decrease_fillrect_edge.Checked;
			s.GlidePlugin.detect_cpu_write = Glide_detect_cpu_write.Checked;
			s.GlidePlugin.fb_clear = Glide_fb_clear.Checked;
			s.GlidePlugin.fb_hires = Glide_fb_hires.Checked;
			s.GlidePlugin.fb_read_alpha = Glide_fb_read_alpha.Checked;
			s.GlidePlugin.fb_smart = Glide_fb_smart.Checked;
			s.GlidePlugin.fillcolor_fix = Glide_fillcolor_fix.Checked;
			s.GlidePlugin.fog = Glide_fog.Checked;
			s.GlidePlugin.force_depth_compare = Glide_force_depth_compare.Checked;
			s.GlidePlugin.force_microcheck = Glide_force_microcheck.Checked;
			s.GlidePlugin.fb_hires_buf_clear = Glide_fb_hires_buf_clear.Checked;
			s.GlidePlugin.fb_ignore_aux_copy = Glide_fb_ignore_aux_copy.Checked;
			s.GlidePlugin.fb_ignore_previous = Glide_fb_ignore_previous.Checked;
			s.GlidePlugin.increase_primdepth = Glide_increase_primdepth.Checked;
			s.GlidePlugin.increase_texrect_edge = Glide_increase_texrect_edge.Checked;
			s.GlidePlugin.fb_optimize_texrect = Glide_fb_optimize_texrect.Checked;
			s.GlidePlugin.fb_optimize_write = Glide_fb_optimize_write.Checked;
			s.GlidePlugin.PPL = Glide_PPL.Checked;
			s.GlidePlugin.soft_depth_compare = Glide_soft_depth_compare.Checked;
			s.GlidePlugin.use_sts1_only = Glide_use_sts1_only.Checked;
			s.GlidePlugin.wrap_big_tex = Glide_wrap_big_tex.Checked;

			s.GlidePlugin.depth_bias =
				InputValidate.IsSigned(Glide_depth_bias.Text) ?
				int.Parse(Glide_depth_bias.Text) : 20;

			s.GlidePlugin.filtering = Glide_filtering.SelectedIndex;

			s.GlidePlugin.fix_tex_coord = InputValidate.IsSigned(Glide_fix_tex_coord.Text) ?
				int.Parse(Glide_fix_tex_coord.Text) : 0;

			s.GlidePlugin.lodmode = Glide_lodmode.SelectedIndex;

			s.GlidePlugin.stipple_mode =
				InputValidate.IsSigned(Glide_stipple_mode.Text) ?
				int.Parse(Glide_stipple_mode.Text) : 2;

			s.GlidePlugin.stipple_pattern =
				InputValidate.IsSigned(Glide_stipple_pattern.Text) ?
				int.Parse(Glide_stipple_pattern.Text) : 1041204192;

			s.GlidePlugin.swapmode = Glide_swapmode.SelectedIndex;
			s.GlidePlugin.enable_hacks_for_game = Glide_enable_hacks_for_game.SelectedIndex;

			s.Glide64mk2Plugin.card_id = Glide64mk2_card_id.SelectedIndex;
			s.Glide64mk2Plugin.wrpFBO = Glide64mk2_wrpFBO.Checked;
			s.Glide64mk2Plugin.wrpAnisotropic = Glide64mk2_wrpAnisotropic.Checked;
			s.Glide64mk2Plugin.fb_get_info = Glide64mk2_fb_get_info.Checked;
			s.Glide64mk2Plugin.fb_render = Glide64mk2_fb_render.Checked;

			s.Glide64mk2Plugin.UseDefaultHacks = Glide64mk2_UseDefaultHacks1.Checked || Glide64mk2_UseDefaultHacks2.Checked;

			s.Glide64mk2Plugin.use_sts1_only = Glide64mk2_use_sts1_only.Checked;
			s.Glide64mk2Plugin.optimize_texrect = Glide64mk2_optimize_texrect.Checked;
			s.Glide64mk2Plugin.increase_texrect_edge = Glide64mk2_increase_texrect_edge.Checked;
			s.Glide64mk2Plugin.ignore_aux_copy = Glide64mk2_ignore_aux_copy.Checked;
			s.Glide64mk2Plugin.hires_buf_clear = Glide64mk2_hires_buf_clear.Checked;
			s.Glide64mk2Plugin.force_microcheck = Glide64mk2_force_microcheck.Checked;
			s.Glide64mk2Plugin.fog = Glide64mk2_fog.Checked;
			s.Glide64mk2Plugin.fb_smart = Glide64mk2_fb_smart.Checked;
			s.Glide64mk2Plugin.fb_read_alpha = Glide64mk2_fb_read_alpha.Checked;
			s.Glide64mk2Plugin.fb_hires = Glide64mk2_fb_hires.Checked;
			s.Glide64mk2Plugin.detect_cpu_write = Glide64mk2_detect_cpu_write.Checked;
			s.Glide64mk2Plugin.decrease_fillrect_edge = Glide64mk2_decrease_fillrect_edge.Checked;
			s.Glide64mk2Plugin.buff_clear = Glide64mk2_buff_clear.Checked;
			s.Glide64mk2Plugin.alt_tex_size = Glide64mk2_alt_tex_size.Checked;
			s.Glide64mk2Plugin.swapmode = Glide64mk2_swapmode.SelectedIndex;

			s.Glide64mk2Plugin.stipple_pattern =
				InputValidate.IsSigned(Glide64mk2_stipple_pattern.Text) ?
				int.Parse(Glide64mk2_stipple_pattern.Text) : 1041204192;

			s.Glide64mk2Plugin.stipple_mode =
				InputValidate.IsSigned(Glide64mk2_stipple_mode.Text) ?
				int.Parse(Glide64mk2_stipple_mode.Text) : 2;

			s.Glide64mk2Plugin.lodmode = Glide64mk2_lodmode.SelectedIndex;
			s.Glide64mk2Plugin.filtering = Glide64mk2_filtering.SelectedIndex;
			s.Glide64mk2Plugin.correct_viewport = Glide64mk2_correct_viewport.Checked;
			s.Glide64mk2Plugin.force_calc_sphere = Glide64mk2_force_calc_sphere.Checked;
			s.Glide64mk2Plugin.pal230 = Glide64mk2_pal230.Checked;
			s.Glide64mk2Plugin.texture_correction = Glide64mk2_texture_correction.Checked;
			s.Glide64mk2Plugin.n64_z_scale = Glide64mk2_n64_z_scale.Checked;
			s.Glide64mk2Plugin.old_style_adither = Glide64mk2_old_style_adither.Checked;
			s.Glide64mk2Plugin.zmode_compare_less = Glide64mk2_zmode_compare_less.Checked;
			s.Glide64mk2Plugin.adjust_aspect = Glide64mk2_adjust_aspect.Checked;
			s.Glide64mk2Plugin.clip_zmax = Glide64mk2_clip_zmax.Checked;
			s.Glide64mk2Plugin.clip_zmin = Glide64mk2_clip_zmin.Checked;
			s.Glide64mk2Plugin.force_quad3d = Glide64mk2_force_quad3d.Checked;
			s.Glide64mk2Plugin.useless_is_useless = Glide64mk2_useless_is_useless.Checked;
			s.Glide64mk2Plugin.fb_read_always = Glide64mk2_fb_read_always.Checked;
			s.Glide64mk2Plugin.aspectmode = Glide64mk2_aspectmode.SelectedIndex;
			s.Glide64mk2Plugin.fb_crc_mode = Glide64mk2_fb_crc_mode.SelectedIndex;
			s.Glide64mk2Plugin.enable_hacks_for_game = Glide64mk2_enable_hacks_for_game.SelectedIndex;
			s.Glide64mk2Plugin.read_back_to_screen = Glide64mk2_read_back_to_screen.SelectedIndex;
			s.Glide64mk2Plugin.fast_crc = Glide64mk2_fast_crc.Checked;


			s.CoreType = EnumHelper.GetValueFromDescription<N64SyncSettings.CORETYPE>(
				CoreTypeDropdown.SelectedItem.ToString());

			PutS(s);
		} 

		private void N64VideoPluginconfig_Load(object sender, EventArgs e)
		{
			var n64Settings = (N64SyncSettings)Global.Emulator.GetSyncSettings();

			CoreTypeDropdown.Items.Clear();
			CoreTypeDropdown.Items.AddRange(
				EnumHelper.GetDescriptions<N64SyncSettings.CORETYPE>()
				.ToArray());
			CoreTypeDropdown.SelectedItem = EnumHelper.GetDescription(n64Settings.CoreType);

			var s = GetS();

			//Load Variables
			//Global
			var video_setting = s.VideoSizeX
						+ " x "
						+ s.VideoSizeY;

			var index = VideoResolutionComboBox.Items.IndexOf(video_setting);
			if (index >= 0)
			{
				VideoResolutionComboBox.SelectedIndex = index;
			}
			switch (s.VidPlugin)
			{
				case PLUGINTYPE.GLIDE64MK2: PluginComboBox.Text = "Glide64mk2"; break;
				case PLUGINTYPE.GLIDE: PluginComboBox.Text = "Glide64"; break;
				case PLUGINTYPE.RICE: PluginComboBox.Text = "Rice"; break;
			}

			//Rice
			RiceNormalAlphaBlender_CB.Checked = s.RicePlugin.NormalAlphaBlender;
			RiceFastTextureLoading_CB.Checked = s.RicePlugin.FastTextureLoading;
			RiceAccurateTextureMapping_CB.Checked = s.RicePlugin.AccurateTextureMapping;
			RiceInN64Resolution_CB.Checked = s.RicePlugin.InN64Resolution;
			RiceSaveVRAM_CB.Checked = s.RicePlugin.SaveVRAM;
			RiceDoubleSizeForSmallTxtrBuf_CB.Checked = s.RicePlugin.DoubleSizeForSmallTxtrBuf;
			RiceDefaultCombinerDisable_CB.Checked = s.RicePlugin.DefaultCombinerDisable;
			RiceEnableHacks_CB.Checked = s.RicePlugin.EnableHacks;
			RiceWinFrameMode_CB.Checked = s.RicePlugin.WinFrameMode;
			RiceFullTMEMEmulation_CB.Checked = s.RicePlugin.FullTMEMEmulation;
			RiceOpenGLVertexClipper_CB.Checked = s.RicePlugin.OpenGLVertexClipper;
			RiceEnableSSE_CB.Checked = s.RicePlugin.EnableSSE;
			RiceEnableVertexShader_CB.Checked = s.RicePlugin.EnableVertexShader;
			RiceSkipFrame_CB.Checked = s.RicePlugin.SkipFrame;
			RiceTexRectOnly_CB.Checked = s.RicePlugin.TexRectOnly;
			RiceSmallTextureOnly_CB.Checked = s.RicePlugin.SmallTextureOnly;
			RiceLoadHiResCRCOnly_CB.Checked = s.RicePlugin.LoadHiResCRCOnly;
			RiceLoadHiResTextures_CB.Checked = s.RicePlugin.LoadHiResTextures;
			RiceDumpTexturesToFiles_CB.Checked = s.RicePlugin.DumpTexturesToFiles;

			RiceFrameBufferSetting_Combo.SelectedIndex = s.RicePlugin.FrameBufferSetting;
			RiceFrameBufferWriteBackControl_Combo.SelectedIndex = s.RicePlugin.FrameBufferWriteBackControl;
			RiceRenderToTexture_Combo.SelectedIndex = s.RicePlugin.RenderToTexture;
			RiceScreenUpdateSetting_Combo.SelectedIndex = s.RicePlugin.ScreenUpdateSetting;
			RiceMipmapping_Combo.SelectedIndex = s.RicePlugin.Mipmapping;
			RiceFogMethod_Combo.SelectedIndex = s.RicePlugin.FogMethod;
			RiceForceTextureFilter_Combo.SelectedIndex = s.RicePlugin.ForceTextureFilter;
			RiceTextureEnhancement_Combo.SelectedIndex = s.RicePlugin.TextureEnhancement;
			RiceTextureEnhancementControl_Combo.SelectedIndex = s.RicePlugin.TextureEnhancementControl;
			RiceTextureQuality_Combo.SelectedIndex = s.RicePlugin.TextureQuality;
			RiceOpenGLDepthBufferSetting_Combo.SelectedIndex = (s.RicePlugin.OpenGLDepthBufferSetting / 16) - 1;
			switch (s.RicePlugin.MultiSampling)
			{
				case 0: RiceMultiSampling_Combo.SelectedIndex = 0; break;
				case 2: RiceMultiSampling_Combo.SelectedIndex = 1; break;
				case 4: RiceMultiSampling_Combo.SelectedIndex = 2; break;
				case 8: RiceMultiSampling_Combo.SelectedIndex = 3; break;
				case 16: RiceMultiSampling_Combo.SelectedIndex = 4; break;
				default: RiceMultiSampling_Combo.SelectedIndex = 0; break;
			}
			RiceColorQuality_Combo.SelectedIndex = s.RicePlugin.ColorQuality;
			RiceOpenGLRenderSetting_Combo.SelectedIndex = s.RicePlugin.OpenGLRenderSetting;
			RiceAnisotropicFiltering_TB.Value = s.RicePlugin.AnisotropicFiltering;
			AnisotropicFiltering_LB.Text = "Anisotropic Filtering: " + RiceAnisotropicFiltering_TB.Value;

			RiceUseDefaultHacks_CB.Checked = s.RicePlugin.UseDefaultHacks;

			UpdateRiceHacksSection();
			if (!s.RicePlugin.UseDefaultHacks)
			{
				RiceTexture1Hack_CB.Checked = s.RicePlugin.Texture1Hack;

				RiceDisableTextureCRC_CB.Checked = s.RicePlugin.DisableTextureCRC;
				RiceDisableCulling_CB.Checked = s.RicePlugin.DisableCulling;
				RiceIncTexRectEdge_CB.Checked = s.RicePlugin.IncTexRectEdge;
				RiceZHack_CB.Checked = s.RicePlugin.ZHack;
				RiceTextureScaleHack_CB.Checked = s.RicePlugin.TextureScaleHack;
				RicePrimaryDepthHack_CB.Checked = s.RicePlugin.PrimaryDepthHack;
				RiceTexture1Hack_CB.Checked = s.RicePlugin.Texture1Hack;
				RiceFastLoadTile_CB.Checked = s.RicePlugin.FastLoadTile;
				RiceUseSmallerTexture_CB.Checked = s.RicePlugin.UseSmallerTexture;
				RiceVIWidth_Text.Text = s.RicePlugin.VIWidth.ToString();
				RiceVIHeight_Text.Text = s.RicePlugin.VIHeight.ToString();
				RiceUseCIWidthAndRatio_Combo.SelectedIndex = s.RicePlugin.UseCIWidthAndRatio;
				RiceFullTMEM_Combo.SelectedIndex = s.RicePlugin.FullTMEM;
				RiceTxtSizeMethod2_CB.Checked = s.RicePlugin.TxtSizeMethod2;
				RiceEnableTxtLOD_CB.Checked = s.RicePlugin.EnableTxtLOD;
				RiceFastTextureCRC_Combo.SelectedIndex = s.RicePlugin.FastTextureCRC;
				RiceEmulateClear_CB.Checked = s.RicePlugin.EmulateClear;
				RiceForceScreenClear_CB.Checked = s.RicePlugin.ForceScreenClear;
				RiceAccurateTextureMappingHack_Combo.SelectedIndex = s.RicePlugin.AccurateTextureMappingHack;
				RiceNormalBlender_Combo.SelectedIndex = s.RicePlugin.NormalBlender;
				RiceDisableBlender_CB.Checked = s.RicePlugin.DisableBlender;
				RiceForceDepthBuffer_CB.Checked = s.RicePlugin.ForceDepthBuffer;
				RiceDisableObjBG_CB.Checked = s.RicePlugin.DisableObjBG;
				RiceFrameBufferOption_Combo.SelectedIndex = s.RicePlugin.FrameBufferOption;
				RiceRenderToTextureOption_Combo.SelectedIndex = s.RicePlugin.RenderToTextureOption;
				RiceScreenUpdateSettingHack_Combo.SelectedIndex = s.RicePlugin.ScreenUpdateSettingHack;
				RiceEnableHacksForGame_Combo.SelectedIndex = s.RicePlugin.EnableHacksForGame;
			}

			Glide_autodetect_ucode.Checked = s.GlidePlugin.autodetect_ucode;
			Glide_ucode.SelectedIndex = s.GlidePlugin.ucode;
			Glide_flame_corona.Checked = s.GlidePlugin.flame_corona;
			Glide_card_id.SelectedIndex = s.GlidePlugin.card_id;
			Glide_tex_filter.SelectedIndex = s.GlidePlugin.tex_filter;
			Glide_wireframe.Checked = s.GlidePlugin.wireframe;
			Glide_wfmode.SelectedIndex = s.GlidePlugin.wfmode;
			Glide_fast_crc.Checked = s.GlidePlugin.fast_crc;
			Glide_filter_cache.Checked = s.GlidePlugin.filter_cache;
			Glide_unk_as_red.Checked = s.GlidePlugin.unk_as_red;
			Glide_fb_read_always.Checked = s.GlidePlugin.fb_read_always;
			Glide_motionblur.Checked = s.GlidePlugin.motionblur;
			Glide_fb_render.Checked = s.GlidePlugin.fb_render;
			Glide_noditheredalpha.Checked = s.GlidePlugin.noditheredalpha;
			Glide_noglsl.Checked = s.GlidePlugin.noglsl;
			Glide_fbo.Checked = s.GlidePlugin.fbo;
			Glide_disable_auxbuf.Checked = s.GlidePlugin.disable_auxbuf;
			Glide_fb_get_info.Checked = s.GlidePlugin.fb_get_info;
			Glide_offset_x.Text = s.GlidePlugin.offset_x.ToString();
			Glide_offset_y.Text = s.GlidePlugin.offset_y.ToString();
			Glide_scale_x.Text = s.GlidePlugin.scale_x.ToString();
			Glide_scale_y.Text = s.GlidePlugin.scale_y.ToString();
			

			GlideUseDefaultHacks1.Checked = s.GlidePlugin.UseDefaultHacks;
			GlideUseDefaultHacks2.Checked = s.GlidePlugin.UseDefaultHacks;

			UpdateGlideHacksSection();
			if (!s.GlidePlugin.UseDefaultHacks)
			{
				Glide_alt_tex_size.Checked = s.GlidePlugin.alt_tex_size;
				Glide_buff_clear.Checked = s.GlidePlugin.buff_clear;
				Glide_decrease_fillrect_edge.Checked = s.GlidePlugin.decrease_fillrect_edge;
				Glide_detect_cpu_write.Checked = s.GlidePlugin.detect_cpu_write;
				Glide_fb_clear.Checked = s.GlidePlugin.fb_clear;
				Glide_fb_hires.Checked = s.GlidePlugin.fb_hires;
				Glide_fb_read_alpha.Checked = s.GlidePlugin.fb_read_alpha;
				Glide_fb_smart.Checked = s.GlidePlugin.fb_smart;
				Glide_fillcolor_fix.Checked = s.GlidePlugin.fillcolor_fix;
				Glide_fog.Checked = s.GlidePlugin.fog;
				Glide_force_depth_compare.Checked = s.GlidePlugin.force_depth_compare;
				Glide_force_microcheck.Checked = s.GlidePlugin.force_microcheck;
				Glide_fb_hires_buf_clear.Checked = s.GlidePlugin.fb_hires_buf_clear;
				Glide_fb_ignore_aux_copy.Checked = s.GlidePlugin.fb_ignore_aux_copy;
				Glide_fb_ignore_previous.Checked = s.GlidePlugin.fb_ignore_previous;
				Glide_increase_primdepth.Checked = s.GlidePlugin.increase_primdepth;
				Glide_increase_texrect_edge.Checked = s.GlidePlugin.increase_texrect_edge;
				Glide_fb_optimize_texrect.Checked = s.GlidePlugin.fb_optimize_texrect;
				Glide_fb_optimize_write.Checked = s.GlidePlugin.fb_optimize_write;
				Glide_PPL.Checked = s.GlidePlugin.PPL;
				Glide_soft_depth_compare.Checked = s.GlidePlugin.soft_depth_compare;
				Glide_use_sts1_only.Checked = s.GlidePlugin.use_sts1_only;
				Glide_wrap_big_tex.Checked = s.GlidePlugin.wrap_big_tex;

				Glide_depth_bias.Text = s.GlidePlugin.depth_bias.ToString();
				Glide_filtering.SelectedIndex = s.GlidePlugin.filtering;
				Glide_fix_tex_coord.Text = s.GlidePlugin.fix_tex_coord.ToString();
				Glide_lodmode.SelectedIndex = s.GlidePlugin.lodmode;
				Glide_stipple_mode.Text = s.GlidePlugin.stipple_mode.ToString();
				Glide_stipple_pattern.Text = s.GlidePlugin.stipple_pattern.ToString();
				Glide_swapmode.SelectedIndex = s.GlidePlugin.swapmode;
				Glide_enable_hacks_for_game.SelectedIndex = s.GlidePlugin.enable_hacks_for_game;
			}

			Glide64mk2_card_id.SelectedIndex = s.Glide64mk2Plugin.card_id;
			Glide64mk2_wrpFBO.Checked = s.Glide64mk2Plugin.wrpFBO;
			Glide64mk2_wrpAnisotropic.Checked = s.Glide64mk2Plugin.wrpAnisotropic;
			Glide64mk2_fb_get_info.Checked = s.Glide64mk2Plugin.fb_get_info;
			Glide64mk2_fb_render.Checked = s.Glide64mk2Plugin.fb_render;

			Glide64mk2_UseDefaultHacks1.Checked = s.Glide64mk2Plugin.UseDefaultHacks;
			Glide64mk2_UseDefaultHacks2.Checked = s.Glide64mk2Plugin.UseDefaultHacks;

			UpdateGlide64mk2HacksSection();
			if (!s.Glide64mk2Plugin.UseDefaultHacks)
			{
				Glide64mk2_use_sts1_only.Checked = s.Glide64mk2Plugin.use_sts1_only;
				Glide64mk2_optimize_texrect.Checked = s.Glide64mk2Plugin.optimize_texrect;
				Glide64mk2_increase_texrect_edge.Checked = s.Glide64mk2Plugin.increase_texrect_edge;
				Glide64mk2_ignore_aux_copy.Checked = s.Glide64mk2Plugin.ignore_aux_copy;
				Glide64mk2_hires_buf_clear.Checked = s.Glide64mk2Plugin.hires_buf_clear;
				Glide64mk2_force_microcheck.Checked = s.Glide64mk2Plugin.force_microcheck;
				Glide64mk2_fog.Checked = s.Glide64mk2Plugin.fog;
				Glide64mk2_fb_smart.Checked = s.Glide64mk2Plugin.fb_smart;
				Glide64mk2_fb_read_alpha.Checked = s.Glide64mk2Plugin.fb_read_alpha;
				Glide64mk2_fb_hires.Checked = s.Glide64mk2Plugin.fb_hires;
				Glide64mk2_detect_cpu_write.Checked = s.Glide64mk2Plugin.detect_cpu_write;
				Glide64mk2_decrease_fillrect_edge.Checked = s.Glide64mk2Plugin.decrease_fillrect_edge;
				Glide64mk2_buff_clear.Checked = s.Glide64mk2Plugin.buff_clear;
				Glide64mk2_alt_tex_size.Checked = s.Glide64mk2Plugin.alt_tex_size;
				Glide64mk2_swapmode.SelectedIndex = s.Glide64mk2Plugin.swapmode;
				Glide64mk2_stipple_pattern.Text = s.Glide64mk2Plugin.stipple_pattern.ToString();
				Glide64mk2_stipple_mode.Text = s.Glide64mk2Plugin.stipple_mode.ToString();
				Glide64mk2_lodmode.SelectedIndex = s.Glide64mk2Plugin.lodmode;
				Glide64mk2_filtering.SelectedIndex = s.Glide64mk2Plugin.filtering;
				Glide64mk2_correct_viewport.Checked = s.Glide64mk2Plugin.correct_viewport;
				Glide64mk2_force_calc_sphere.Checked = s.Glide64mk2Plugin.force_calc_sphere;
				Glide64mk2_pal230.Checked = s.Glide64mk2Plugin.pal230;
				Glide64mk2_texture_correction.Checked = s.Glide64mk2Plugin.texture_correction;
				Glide64mk2_n64_z_scale.Checked = s.Glide64mk2Plugin.n64_z_scale;
				Glide64mk2_old_style_adither.Checked = s.Glide64mk2Plugin.old_style_adither;
				Glide64mk2_zmode_compare_less.Checked = s.Glide64mk2Plugin.zmode_compare_less;
				Glide64mk2_adjust_aspect.Checked = s.Glide64mk2Plugin.adjust_aspect;
				Glide64mk2_clip_zmax.Checked = s.Glide64mk2Plugin.clip_zmax;
				Glide64mk2_clip_zmin.Checked = s.Glide64mk2Plugin.clip_zmin;
				Glide64mk2_force_quad3d.Checked = s.Glide64mk2Plugin.force_quad3d;
				Glide64mk2_useless_is_useless.Checked = s.Glide64mk2Plugin.useless_is_useless;
				Glide64mk2_fb_read_always.Checked = s.Glide64mk2Plugin.fb_read_always;
				Glide64mk2_aspectmode.SelectedIndex = s.Glide64mk2Plugin.aspectmode;
				Glide64mk2_fb_crc_mode.SelectedIndex = s.Glide64mk2Plugin.fb_crc_mode;
				Glide64mk2_enable_hacks_for_game.SelectedIndex = s.Glide64mk2Plugin.enable_hacks_for_game;
				Glide64mk2_read_back_to_screen.SelectedIndex = s.Glide64mk2Plugin.read_back_to_screen;
				Glide64mk2_fast_crc.Checked = s.Glide64mk2Plugin.fast_crc;
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

		private void UpdateGlideHacksSection()
		{
			if (GlideUseDefaultHacks1.Checked || GlideUseDefaultHacks2.Checked)
			{
				Glide_alt_tex_size.Checked = Global.Game.GetBool("Glide_alt_tex_size", false);
				Glide_buff_clear.Checked = Global.Game.GetBool("Glide_buff_clear", true);
				Glide_decrease_fillrect_edge.Checked = Global.Game.GetBool("Glide_decrease_fillrect_edge", false);
				Glide_detect_cpu_write.Checked = Global.Game.GetBool("Glide_detect_cpu_write", false);
				Glide_fb_clear.Checked = Global.Game.GetBool("Glide_fb_clear", false);
				Glide_fb_hires.Checked = Global.Game.GetBool("Glide_fb_clear", true);
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
			if (Global.Game.OptionPresent(parameter) && InputValidate.IsUnsigned(Global.Game.OptionValue(parameter)))
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

	}
}
