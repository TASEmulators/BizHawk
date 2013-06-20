using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class N64VideoPluginconfig : Form
	{
		public N64VideoPluginconfig()
		{
			InitializeComponent();
		}

		private void CancelBT_Click(object sender, EventArgs e)
		{
			//Add confirmation of cancelling change
			Close();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			SaveSettings();
			Close();
		}

		private void SaveSettings()
		{
			//Global
			string video_settings = VideoResolutionComboBox.SelectedItem.ToString();
			string[] strArr = video_settings.Split('x');
			Global.Config.N64VideoSizeX = Int32.Parse(strArr[0].Trim());
			Global.Config.N64VideoSizeY = Int32.Parse(strArr[1].Trim());
			Global.Config.N64VidPlugin = PluginComboBox.Text;
			Global.MainForm.FlagNeedsReboot(); //TODO: this won't always be necessary, keep that in mind
			
			//Rice
			Global.Config.RicePlugin.NormalAlphaBlender = RiceNormalAlphaBlender_CB.Checked;
			Global.Config.RicePlugin.FastTextureLoading = RiceFastTextureLoading_CB.Checked;
			Global.Config.RicePlugin.AccurateTextureMapping = RiceAccurateTextureMapping_CB.Checked;
			Global.Config.RicePlugin.InN64Resolution = RiceInN64Resolution_CB.Checked;
			Global.Config.RicePlugin.SaveVRAM = RiceSaveVRAM_CB.Checked;
			Global.Config.RicePlugin.DoubleSizeForSmallTxtrBuf = RiceDoubleSizeForSmallTxtrBuf_CB.Checked;
			Global.Config.RicePlugin.DefaultCombinerDisable = RiceDefaultCombinerDisable_CB.Checked;
			Global.Config.RicePlugin.EnableHacks = RiceEnableHacks_CB.Checked;
			Global.Config.RicePlugin.WinFrameMode = RiceWinFrameMode_CB.Checked;
			Global.Config.RicePlugin.FullTMEMEmulation = RiceFullTMEMEmulation_CB.Checked;
			Global.Config.RicePlugin.OpenGLVertexClipper = RiceOpenGLVertexClipper_CB.Checked;
			Global.Config.RicePlugin.EnableSSE = RiceEnableSSE_CB.Checked;
			Global.Config.RicePlugin.EnableVertexShader = RiceEnableVertexShader_CB.Checked;
			Global.Config.RicePlugin.SkipFrame = RiceSkipFrame_CB.Checked;
			Global.Config.RicePlugin.TexRectOnly = RiceTexRectOnly_CB.Checked;
			Global.Config.RicePlugin.SmallTextureOnly = RiceSmallTextureOnly_CB.Checked;
			Global.Config.RicePlugin.LoadHiResCRCOnly = RiceLoadHiResCRCOnly_CB.Checked;
			Global.Config.RicePlugin.LoadHiResTextures = RiceLoadHiResTextures_CB.Checked;
			Global.Config.RicePlugin.DumpTexturesToFiles = RiceDumpTexturesToFiles_CB.Checked;

			Global.Config.RicePlugin.FrameBufferSetting = RiceFrameBufferSetting_Combo.SelectedIndex;
			Global.Config.RicePlugin.FrameBufferWriteBackControl = RiceFrameBufferWriteBackControl_Combo.SelectedIndex;
			Global.Config.RicePlugin.RenderToTexture = RiceRenderToTexture_Combo.SelectedIndex;
			Global.Config.RicePlugin.ScreenUpdateSetting = RiceScreenUpdateSetting_Combo.SelectedIndex;
			Global.Config.RicePlugin.Mipmapping = RiceMipmapping_Combo.SelectedIndex;
			Global.Config.RicePlugin.FogMethod = RiceFogMethod_Combo.SelectedIndex;
			Global.Config.RicePlugin.ForceTextureFilter = RiceForceTextureFilter_Combo.SelectedIndex;
			Global.Config.RicePlugin.TextureEnhancement = RiceTextureEnhancement_Combo.SelectedIndex;
			Global.Config.RicePlugin.TextureEnhancementControl = RiceTextureEnhancementControl_Combo.SelectedIndex;
			Global.Config.RicePlugin.TextureQuality = RiceTextureQuality_Combo.SelectedIndex;
			Global.Config.RicePlugin.OpenGLDepthBufferSetting = (RiceOpenGLDepthBufferSetting_Combo.SelectedIndex + 1) * 16;
			switch (RiceMultiSampling_Combo.SelectedIndex)
			{
				case 0: Global.Config.RicePlugin.MultiSampling = 0; break;
				case 1: Global.Config.RicePlugin.MultiSampling = 2; break;
				case 2: Global.Config.RicePlugin.MultiSampling = 4; break;
				case 3: Global.Config.RicePlugin.MultiSampling = 8; break;
				case 4: Global.Config.RicePlugin.MultiSampling = 16; break;
				default: Global.Config.RicePlugin.MultiSampling = 0; break;
			}
			Global.Config.RicePlugin.ColorQuality = RiceColorQuality_Combo.SelectedIndex;
			Global.Config.RicePlugin.OpenGLRenderSetting = RiceOpenGLRenderSetting_Combo.SelectedIndex;
			Global.Config.RicePlugin.AnisotropicFiltering = RiceAnisotropicFiltering_TB.Value;

			Global.Config.RicePlugin.UseDefaultHacks = RiceUseDefaultHacks_CB.Checked;
			Global.Config.RicePlugin.DisableTextureCRC = RiceDisableTextureCRC_CB.Checked;
			Global.Config.RicePlugin.DisableCulling = RiceDisableCulling_CB.Checked;
			Global.Config.RicePlugin.IncTexRectEdge = RiceIncTexRectEdge_CB.Checked;
			Global.Config.RicePlugin.ZHack = RiceZHack_CB.Checked;
			Global.Config.RicePlugin.TextureScaleHack = RiceTextureScaleHack_CB.Checked;
			Global.Config.RicePlugin.PrimaryDepthHack = RicePrimaryDepthHack_CB.Checked;
			Global.Config.RicePlugin.Texture1Hack = RiceTexture1Hack_CB.Checked;
			Global.Config.RicePlugin.FastLoadTile = RiceFastLoadTile_CB.Checked;
			Global.Config.RicePlugin.UseSmallerTexture = RiceUseSmallerTexture_CB.Checked;

			if (InputValidate.IsValidSignedNumber(RiceVIWidth_Text.Text))
				Global.Config.RicePlugin.VIWidth = int.Parse(RiceVIWidth_Text.Text);
			else
				Global.Config.RicePlugin.VIWidth = -1;

			if (InputValidate.IsValidSignedNumber(RiceVIHeight_Text.Text))
				Global.Config.RicePlugin.VIHeight = int.Parse(RiceVIHeight_Text.Text);
			else
				Global.Config.RicePlugin.VIHeight = -1;

			Global.Config.RicePlugin.UseCIWidthAndRatio = RiceUseCIWidthAndRatio_Combo.SelectedIndex;
			Global.Config.RicePlugin.FullTMEM = RiceFullTMEM_Combo.SelectedIndex;
			Global.Config.RicePlugin.TxtSizeMethod2 = RiceTxtSizeMethod2_CB.Checked;
			Global.Config.RicePlugin.EnableTxtLOD = RiceEnableTxtLOD_CB.Checked;
			Global.Config.RicePlugin.FastTextureCRC = RiceFastTextureCRC_Combo.SelectedIndex;
			Global.Config.RicePlugin.EmulateClear = RiceEmulateClear_CB.Checked;
			Global.Config.RicePlugin.ForceScreenClear = RiceForceScreenClear_CB.Checked;
			Global.Config.RicePlugin.AccurateTextureMappingHack = RiceAccurateTextureMappingHack_Combo.SelectedIndex;
			Global.Config.RicePlugin.NormalBlender = RiceNormalBlender_Combo.SelectedIndex;
			Global.Config.RicePlugin.DisableBlender = RiceDisableBlender_CB.Checked;
			Global.Config.RicePlugin.ForceDepthBuffer = RiceForceDepthBuffer_CB.Checked;
			Global.Config.RicePlugin.DisableObjBG = RiceDisableObjBG_CB.Checked;
			Global.Config.RicePlugin.FrameBufferOption = RiceFrameBufferOption_Combo.SelectedIndex;
			Global.Config.RicePlugin.RenderToTextureOption = RiceRenderToTextureOption_Combo.SelectedIndex;
			Global.Config.RicePlugin.ScreenUpdateSettingHack = RiceScreenUpdateSettingHack_Combo.SelectedIndex;
			Global.Config.RicePlugin.EnableHacksForGame = RiceEnableHacksForGame_Combo.SelectedIndex;

			Global.Config.GlidePlugin.autodetect_ucode = Glide_autodetect_ucode.Checked;
			Global.Config.GlidePlugin.ucode = Glide_ucode.SelectedIndex;
			Global.Config.GlidePlugin.flame_corona = Glide_flame_corona.Checked;
			Global.Config.GlidePlugin.card_id = Glide_card_id.SelectedIndex;
			Global.Config.GlidePlugin.tex_filter = Glide_tex_filter.SelectedIndex;
			Global.Config.GlidePlugin.wireframe = Glide_wireframe.Checked;
			Global.Config.GlidePlugin.wfmode = Glide_wfmode.SelectedIndex;
			Global.Config.GlidePlugin.fast_crc = Glide_fast_crc.Checked;
			Global.Config.GlidePlugin.filter_cache = Glide_filter_cache.Checked;
			Global.Config.GlidePlugin.unk_as_red = Glide_unk_as_red.Checked;
			Global.Config.GlidePlugin.fb_read_always = Glide_fb_read_always.Checked;
			Global.Config.GlidePlugin.motionblur = Glide_motionblur.Checked;
			Global.Config.GlidePlugin.fb_render = Glide_fb_render.Checked;
			Global.Config.GlidePlugin.noditheredalpha = Glide_noditheredalpha.Checked;
			Global.Config.GlidePlugin.noglsl = Glide_noglsl.Checked;
			Global.Config.GlidePlugin.fbo = Glide_fbo.Checked;
			Global.Config.GlidePlugin.disable_auxbuf = Glide_disable_auxbuf.Checked;
			Global.Config.GlidePlugin.fb_get_info = Glide_fb_get_info.Checked;

			if (InputValidate.IsValidSignedNumber(Glide_offset_x.Text))
				Global.Config.GlidePlugin.offset_x = int.Parse(Glide_offset_x.Text);
			else
				Global.Config.GlidePlugin.offset_x = 0;

			if (InputValidate.IsValidSignedNumber(Glide_offset_y.Text))
				Global.Config.GlidePlugin.offset_y = int.Parse(Glide_offset_y.Text);
			else
				Global.Config.GlidePlugin.offset_y = 0;

			if (InputValidate.IsValidSignedNumber(Glide_scale_x.Text))
				Global.Config.GlidePlugin.scale_x = int.Parse(Glide_scale_x.Text);
			else
				Global.Config.GlidePlugin.scale_x = 100000;

			if (InputValidate.IsValidSignedNumber(Glide_scale_y.Text))
				Global.Config.GlidePlugin.scale_y = int.Parse(Glide_scale_y.Text);
			else
				Global.Config.GlidePlugin.scale_y = 100000;

			Global.Config.GlidePlugin.UseDefaultHacks = GlideUseDefaultHacks1.Checked || GlideUseDefaultHacks2.Checked;
			Global.Config.GlidePlugin.alt_tex_size = Glide_alt_tex_size.Checked;
			Global.Config.GlidePlugin.buff_clear = Glide_buff_clear.Checked;
			Global.Config.GlidePlugin.decrease_fillrect_edge = Glide_decrease_fillrect_edge.Checked;
			Global.Config.GlidePlugin.detect_cpu_write = Glide_detect_cpu_write.Checked;
			Global.Config.GlidePlugin.fb_clear = Glide_fb_clear.Checked;
			Global.Config.GlidePlugin.fb_hires = Glide_fb_hires.Checked;
			Global.Config.GlidePlugin.fb_read_alpha = Glide_fb_read_alpha.Checked;
			Global.Config.GlidePlugin.fb_smart = Glide_fb_smart.Checked;
			Global.Config.GlidePlugin.fillcolor_fix = Glide_fillcolor_fix.Checked;
			Global.Config.GlidePlugin.fog = Glide_fog.Checked;
			Global.Config.GlidePlugin.force_depth_compare = Glide_force_depth_compare.Checked;
			Global.Config.GlidePlugin.force_microcheck = Glide_force_microcheck.Checked;
			Global.Config.GlidePlugin.fb_hires_buf_clear = Glide_fb_hires_buf_clear.Checked;
			Global.Config.GlidePlugin.fb_ignore_aux_copy = Glide_fb_ignore_aux_copy.Checked;
			Global.Config.GlidePlugin.fb_ignore_previous = Glide_fb_ignore_previous.Checked;
			Global.Config.GlidePlugin.increase_primdepth = Glide_increase_primdepth.Checked;
			Global.Config.GlidePlugin.increase_texrect_edge = Glide_increase_texrect_edge.Checked;
			Global.Config.GlidePlugin.fb_optimize_texrect = Glide_fb_optimize_texrect.Checked;
			Global.Config.GlidePlugin.fb_optimize_write = Glide_fb_optimize_write.Checked;
			Global.Config.GlidePlugin.PPL = Glide_PPL.Checked;
			Global.Config.GlidePlugin.soft_depth_compare = Glide_soft_depth_compare.Checked;
			Global.Config.GlidePlugin.use_sts1_only = Glide_use_sts1_only.Checked;
			Global.Config.GlidePlugin.wrap_big_tex = Glide_wrap_big_tex.Checked;

			if (InputValidate.IsValidSignedNumber(Glide_depth_bias.Text))
				Global.Config.GlidePlugin.depth_bias = int.Parse(Glide_depth_bias.Text);
			else
				Global.Config.GlidePlugin.depth_bias = 20;

			Global.Config.GlidePlugin.filtering = Glide_filtering.SelectedIndex;

			if (InputValidate.IsValidSignedNumber(Glide_fix_tex_coord.Text))
				Global.Config.GlidePlugin.fix_tex_coord = int.Parse(Glide_fix_tex_coord.Text);
			else
				Global.Config.GlidePlugin.fix_tex_coord = 0;

			Global.Config.GlidePlugin.lodmode = Glide_lodmode.SelectedIndex;

			if (InputValidate.IsValidSignedNumber(Glide_stipple_mode.Text))
				Global.Config.GlidePlugin.stipple_mode = int.Parse(Glide_stipple_mode.Text);
			else
				Global.Config.GlidePlugin.stipple_mode = 2;

			if (InputValidate.IsValidSignedNumber(Glide_stipple_pattern.Text))
				Global.Config.GlidePlugin.stipple_pattern = int.Parse(Glide_stipple_pattern.Text);
			else
				Global.Config.GlidePlugin.stipple_pattern = 1041204192;

			Global.Config.GlidePlugin.swapmode = Glide_swapmode.SelectedIndex;
			Global.Config.GlidePlugin.enable_hacks_for_game = Glide_enable_hacks_for_game.SelectedIndex;
		}

		private void N64VideoPluginconfig_Load(object sender, EventArgs e)
		{
			//Load Variables
			//Global
			string video_setting = Global.Config.N64VideoSizeX.ToString()
						+ " x "
						+ Global.Config.N64VideoSizeY.ToString();

			int index = VideoResolutionComboBox.Items.IndexOf(video_setting);
			if (index >= 0)
			{
				VideoResolutionComboBox.SelectedIndex = index;
			}
			PluginComboBox.Text = Global.Config.N64VidPlugin;

			//Rice
			Global.MainForm.FlagNeedsReboot(); //TODO: this won't always be necessary, keep that in mind
			RiceNormalAlphaBlender_CB.Checked = Global.Config.RicePlugin.NormalAlphaBlender;
			RiceFastTextureLoading_CB.Checked = Global.Config.RicePlugin.FastTextureLoading;
			RiceAccurateTextureMapping_CB.Checked = Global.Config.RicePlugin.AccurateTextureMapping;
			RiceInN64Resolution_CB.Checked = Global.Config.RicePlugin.InN64Resolution;
			RiceSaveVRAM_CB.Checked = Global.Config.RicePlugin.SaveVRAM;
			RiceDoubleSizeForSmallTxtrBuf_CB.Checked = Global.Config.RicePlugin.DoubleSizeForSmallTxtrBuf;
			RiceDefaultCombinerDisable_CB.Checked = Global.Config.RicePlugin.DefaultCombinerDisable;
			RiceEnableHacks_CB.Checked = Global.Config.RicePlugin.EnableHacks;
			RiceWinFrameMode_CB.Checked = Global.Config.RicePlugin.WinFrameMode;
			RiceFullTMEMEmulation_CB.Checked = Global.Config.RicePlugin.FullTMEMEmulation;
			RiceOpenGLVertexClipper_CB.Checked = Global.Config.RicePlugin.OpenGLVertexClipper;
			RiceEnableSSE_CB.Checked = Global.Config.RicePlugin.EnableSSE;
			RiceEnableVertexShader_CB.Checked = Global.Config.RicePlugin.EnableVertexShader;
			RiceSkipFrame_CB.Checked = Global.Config.RicePlugin.SkipFrame;
			RiceTexRectOnly_CB.Checked = Global.Config.RicePlugin.TexRectOnly;
			RiceSmallTextureOnly_CB.Checked = Global.Config.RicePlugin.SmallTextureOnly;
			RiceLoadHiResCRCOnly_CB.Checked = Global.Config.RicePlugin.LoadHiResCRCOnly;
			RiceLoadHiResTextures_CB.Checked = Global.Config.RicePlugin.LoadHiResTextures;
			RiceDumpTexturesToFiles_CB.Checked = Global.Config.RicePlugin.DumpTexturesToFiles;

			RiceFrameBufferSetting_Combo.SelectedIndex = Global.Config.RicePlugin.FrameBufferSetting;
			RiceFrameBufferWriteBackControl_Combo.SelectedIndex = Global.Config.RicePlugin.FrameBufferWriteBackControl;
			RiceRenderToTexture_Combo.SelectedIndex = Global.Config.RicePlugin.RenderToTexture;
			RiceScreenUpdateSetting_Combo.SelectedIndex = Global.Config.RicePlugin.ScreenUpdateSetting;
			RiceMipmapping_Combo.SelectedIndex = Global.Config.RicePlugin.Mipmapping;
			RiceFogMethod_Combo.SelectedIndex = Global.Config.RicePlugin.FogMethod;
			RiceForceTextureFilter_Combo.SelectedIndex = Global.Config.RicePlugin.ForceTextureFilter;
			RiceTextureEnhancement_Combo.SelectedIndex = Global.Config.RicePlugin.TextureEnhancement;
			RiceTextureEnhancementControl_Combo.SelectedIndex = Global.Config.RicePlugin.TextureEnhancementControl;
			RiceTextureQuality_Combo.SelectedIndex = Global.Config.RicePlugin.TextureQuality;
			RiceOpenGLDepthBufferSetting_Combo.SelectedIndex = (Global.Config.RicePlugin.OpenGLDepthBufferSetting / 16) - 1;
			switch (Global.Config.RicePlugin.MultiSampling)
			{
				case 0: RiceMultiSampling_Combo.SelectedIndex = 0; break;
				case 2: RiceMultiSampling_Combo.SelectedIndex = 1; break;
				case 4: RiceMultiSampling_Combo.SelectedIndex = 2; break;
				case 8: RiceMultiSampling_Combo.SelectedIndex = 3; break;
				case 16: RiceMultiSampling_Combo.SelectedIndex = 4; break;
				default: RiceMultiSampling_Combo.SelectedIndex = 0; break;
			}
			RiceColorQuality_Combo.SelectedIndex = Global.Config.RicePlugin.ColorQuality;
			RiceOpenGLRenderSetting_Combo.SelectedIndex = Global.Config.RicePlugin.OpenGLRenderSetting;
			RiceAnisotropicFiltering_TB.Value = Global.Config.RicePlugin.AnisotropicFiltering;
			AnisotropicFiltering_LB.Text = "Anisotropic Filtering: " + RiceAnisotropicFiltering_TB.Value.ToString();

			RiceUseDefaultHacks_CB.Checked = Global.Config.RicePlugin.UseDefaultHacks;

			UpdateRiceHacksSection();
			if (!Global.Config.RicePlugin.UseDefaultHacks)
			{
				RiceTexture1Hack_CB.Checked = Global.Config.RicePlugin.Texture1Hack;

				RiceDisableTextureCRC_CB.Checked = Global.Config.RicePlugin.DisableTextureCRC;
				RiceDisableCulling_CB.Checked = Global.Config.RicePlugin.DisableCulling;
				RiceIncTexRectEdge_CB.Checked = Global.Config.RicePlugin.IncTexRectEdge;
				RiceZHack_CB.Checked = Global.Config.RicePlugin.ZHack;
				RiceTextureScaleHack_CB.Checked = Global.Config.RicePlugin.TextureScaleHack;
				RicePrimaryDepthHack_CB.Checked = Global.Config.RicePlugin.PrimaryDepthHack;
				RiceTexture1Hack_CB.Checked = Global.Config.RicePlugin.Texture1Hack;
				RiceFastLoadTile_CB.Checked = Global.Config.RicePlugin.FastLoadTile;
				RiceUseSmallerTexture_CB.Checked = Global.Config.RicePlugin.UseSmallerTexture;
				RiceVIWidth_Text.Text = Global.Config.RicePlugin.VIWidth.ToString();
				RiceVIHeight_Text.Text = Global.Config.RicePlugin.VIHeight.ToString();
				RiceUseCIWidthAndRatio_Combo.SelectedIndex = Global.Config.RicePlugin.UseCIWidthAndRatio;
				RiceFullTMEM_Combo.SelectedIndex = Global.Config.RicePlugin.FullTMEM;
				RiceTxtSizeMethod2_CB.Checked = Global.Config.RicePlugin.TxtSizeMethod2;
				RiceEnableTxtLOD_CB.Checked = Global.Config.RicePlugin.EnableTxtLOD;
				RiceFastTextureCRC_Combo.SelectedIndex = Global.Config.RicePlugin.FastTextureCRC;
				RiceEmulateClear_CB.Checked = Global.Config.RicePlugin.EmulateClear;
				RiceForceScreenClear_CB.Checked = Global.Config.RicePlugin.ForceScreenClear;
				RiceAccurateTextureMappingHack_Combo.SelectedIndex = Global.Config.RicePlugin.AccurateTextureMappingHack;
				RiceNormalBlender_Combo.SelectedIndex = Global.Config.RicePlugin.NormalBlender;
				RiceDisableBlender_CB.Checked = Global.Config.RicePlugin.DisableBlender;
				RiceForceDepthBuffer_CB.Checked = Global.Config.RicePlugin.ForceDepthBuffer;
				RiceDisableObjBG_CB.Checked = Global.Config.RicePlugin.DisableObjBG;
				RiceFrameBufferOption_Combo.SelectedIndex = Global.Config.RicePlugin.FrameBufferOption;
				RiceRenderToTextureOption_Combo.SelectedIndex = Global.Config.RicePlugin.RenderToTextureOption;
				RiceScreenUpdateSettingHack_Combo.SelectedIndex = Global.Config.RicePlugin.ScreenUpdateSettingHack;
				RiceEnableHacksForGame_Combo.SelectedIndex = Global.Config.RicePlugin.EnableHacksForGame;
			}

			Glide_autodetect_ucode.Checked = Global.Config.GlidePlugin.autodetect_ucode;
			Glide_ucode.SelectedIndex = Global.Config.GlidePlugin.ucode;
			Glide_flame_corona.Checked = Global.Config.GlidePlugin.flame_corona;
			Glide_card_id.SelectedIndex = Global.Config.GlidePlugin.card_id;
			Glide_tex_filter.SelectedIndex = Global.Config.GlidePlugin.tex_filter;
			Glide_wireframe.Checked = Global.Config.GlidePlugin.wireframe;
			Glide_wfmode.SelectedIndex = Global.Config.GlidePlugin.wfmode;
			Glide_fast_crc.Checked = Global.Config.GlidePlugin.fast_crc;
			Glide_filter_cache.Checked = Global.Config.GlidePlugin.filter_cache;
			Glide_unk_as_red.Checked = Global.Config.GlidePlugin.unk_as_red;
			Glide_fb_read_always.Checked = Global.Config.GlidePlugin.fb_read_always;
			Glide_motionblur.Checked = Global.Config.GlidePlugin.motionblur;
			Glide_fb_render.Checked = Global.Config.GlidePlugin.fb_render;
			Glide_noditheredalpha.Checked = Global.Config.GlidePlugin.noditheredalpha;
			Glide_noglsl.Checked = Global.Config.GlidePlugin.noglsl;
			Glide_fbo.Checked = Global.Config.GlidePlugin.fbo;
			Glide_disable_auxbuf.Checked = Global.Config.GlidePlugin.disable_auxbuf;
			Glide_fb_get_info.Checked = Global.Config.GlidePlugin.fb_get_info;
			Glide_offset_x.Text = Global.Config.GlidePlugin.offset_x.ToString();
			Glide_offset_y.Text = Global.Config.GlidePlugin.offset_y.ToString();
			Glide_scale_x.Text = Global.Config.GlidePlugin.scale_x.ToString();
			Glide_scale_y.Text = Global.Config.GlidePlugin.scale_y.ToString();
			

			GlideUseDefaultHacks1.Checked = Global.Config.GlidePlugin.UseDefaultHacks;
			GlideUseDefaultHacks2.Checked = Global.Config.GlidePlugin.UseDefaultHacks;

			UpdateGlideHacksSection();
			if (!Global.Config.GlidePlugin.UseDefaultHacks)
			{
				Glide_alt_tex_size.Checked = Global.Config.GlidePlugin.alt_tex_size;
				Glide_buff_clear.Checked = Global.Config.GlidePlugin.buff_clear;
				Glide_decrease_fillrect_edge.Checked = Global.Config.GlidePlugin.decrease_fillrect_edge;
				Glide_detect_cpu_write.Checked = Global.Config.GlidePlugin.detect_cpu_write;
				Glide_fb_clear.Checked = Global.Config.GlidePlugin.fb_clear;
				Glide_fb_hires.Checked = Global.Config.GlidePlugin.fb_hires;
				Glide_fb_read_alpha.Checked = Global.Config.GlidePlugin.fb_read_alpha;
				Glide_fb_smart.Checked = Global.Config.GlidePlugin.fb_smart;
				Glide_fillcolor_fix.Checked = Global.Config.GlidePlugin.fillcolor_fix;
				Glide_fog.Checked = Global.Config.GlidePlugin.fog;
				Glide_force_depth_compare.Checked = Global.Config.GlidePlugin.force_depth_compare;
				Glide_force_microcheck.Checked = Global.Config.GlidePlugin.force_microcheck;
				Glide_fb_hires_buf_clear.Checked = Global.Config.GlidePlugin.fb_hires_buf_clear;
				Glide_fb_ignore_aux_copy.Checked = Global.Config.GlidePlugin.fb_ignore_aux_copy;
				Glide_fb_ignore_previous.Checked = Global.Config.GlidePlugin.fb_ignore_previous;
				Glide_increase_primdepth.Checked = Global.Config.GlidePlugin.increase_primdepth;
				Glide_increase_texrect_edge.Checked = Global.Config.GlidePlugin.increase_texrect_edge;
				Glide_fb_optimize_texrect.Checked = Global.Config.GlidePlugin.fb_optimize_texrect;
				Glide_fb_optimize_write.Checked = Global.Config.GlidePlugin.fb_optimize_write;
				Glide_PPL.Checked = Global.Config.GlidePlugin.PPL;
				Glide_soft_depth_compare.Checked = Global.Config.GlidePlugin.soft_depth_compare;
				Glide_use_sts1_only.Checked = Global.Config.GlidePlugin.use_sts1_only;
				Glide_wrap_big_tex.Checked = Global.Config.GlidePlugin.wrap_big_tex;

				Glide_depth_bias.Text = Global.Config.GlidePlugin.depth_bias.ToString();
				Glide_filtering.SelectedIndex = Global.Config.GlidePlugin.filtering;
				Glide_fix_tex_coord.Text = Global.Config.GlidePlugin.fix_tex_coord.ToString();
				Glide_lodmode.SelectedIndex = Global.Config.GlidePlugin.lodmode;
				Glide_stipple_mode.Text = Global.Config.GlidePlugin.stipple_mode.ToString();
				Glide_stipple_pattern.Text = Global.Config.GlidePlugin.stipple_pattern.ToString();
				Glide_swapmode.SelectedIndex = Global.Config.GlidePlugin.swapmode;
				Glide_enable_hacks_for_game.SelectedIndex = Global.Config.GlidePlugin.enable_hacks_for_game;
			}
		}
		
		private void RiceAnisotropicFiltering_TB_Scroll_1(object sender, EventArgs e)
		{
			AnisotropicFiltering_LB.Text = "Anisotropic Filtering: " + RiceAnisotropicFiltering_TB.Value.ToString();
		}

		private void RiceUseDefaultHacks_CB_CheckedChanged(object sender, EventArgs e)
		{
			UpdateRiceHacksSection();
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
			if (Global.Game.OptionPresent(parameter) && Global.Game.OptionValue(parameter) == "true")
				return true;
			else
				return false;
		}

		public int GetIntFromDB(string parameter, int defaultVal)
		{
			if (Global.Game.OptionPresent(parameter) && InputValidate.IsValidUnsignedNumber(Global.Game.OptionValue(parameter)))
				return int.Parse(Global.Game.OptionValue(parameter));
			else
				return defaultVal;
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

	}
}
