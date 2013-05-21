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
			Global.Config.RicePlugin.RiceNormalAlphaBlender = RiceNormalAlphaBlender_CB.Checked;
			Global.Config.RicePlugin.RiceFastTextureLoading = RiceFastTextureLoading_CB.Checked;
			Global.Config.RicePlugin.RiceAccurateTextureMapping = RiceAccurateTextureMapping_CB.Checked;
			Global.Config.RicePlugin.RiceInN64Resolution = RiceInN64Resolution_CB.Checked;
			Global.Config.RicePlugin.RiceSaveVRAM = RiceSaveVRAM_CB.Checked;
			Global.Config.RicePlugin.RiceDoubleSizeForSmallTxtrBuf = RiceDoubleSizeForSmallTxtrBuf_CB.Checked;
			Global.Config.RicePlugin.RiceDefaultCombinerDisable = RiceDefaultCombinerDisable_CB.Checked;
			Global.Config.RicePlugin.RiceEnableHacks = RiceEnableHacks_CB.Checked;
			Global.Config.RicePlugin.RiceWinFrameMode = RiceWinFrameMode_CB.Checked;
			Global.Config.RicePlugin.RiceFullTMEMEmulation = RiceFullTMEMEmulation_CB.Checked;
			Global.Config.RicePlugin.RiceOpenGLVertexClipper = RiceOpenGLVertexClipper_CB.Checked;
			Global.Config.RicePlugin.RiceEnableSSE = RiceEnableSSE_CB.Checked;
			Global.Config.RicePlugin.RiceEnableVertexShader = RiceEnableVertexShader_CB.Checked;
			Global.Config.RicePlugin.RiceSkipFrame = RiceSkipFrame_CB.Checked;
			Global.Config.RicePlugin.RiceTexRectOnly = RiceTexRectOnly_CB.Checked;
			Global.Config.RicePlugin.RiceSmallTextureOnly = RiceSmallTextureOnly_CB.Checked;
			Global.Config.RicePlugin.RiceLoadHiResCRCOnly = RiceLoadHiResCRCOnly_CB.Checked;
			Global.Config.RicePlugin.RiceLoadHiResTextures = RiceLoadHiResTextures_CB.Checked;
			Global.Config.RicePlugin.RiceDumpTexturesToFiles = RiceDumpTexturesToFiles_CB.Checked;

			Global.Config.RicePlugin.RiceFrameBufferSetting = RiceFrameBufferSetting_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceFrameBufferWriteBackControl = RiceFrameBufferWriteBackControl_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceRenderToTexture = RiceRenderToTexture_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceScreenUpdateSetting = RiceScreenUpdateSetting_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceMipmapping = RiceMipmapping_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceFogMethod = RiceFogMethod_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceForceTextureFilter = RiceForceTextureFilter_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceTextureEnhancement = RiceTextureEnhancement_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceTextureEnhancementControl = RiceTextureEnhancementControl_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceTextureQuality = RiceTextureQuality_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceOpenGLDepthBufferSetting = (RiceOpenGLDepthBufferSetting_Combo.SelectedIndex + 1) * 16;
			switch (RiceMultiSampling_Combo.SelectedIndex)
			{
				case 0: Global.Config.RicePlugin.RiceMultiSampling = 0; break;
				case 1: Global.Config.RicePlugin.RiceMultiSampling = 2; break;
				case 2: Global.Config.RicePlugin.RiceMultiSampling = 4; break;
				case 3: Global.Config.RicePlugin.RiceMultiSampling = 8; break;
				case 4: Global.Config.RicePlugin.RiceMultiSampling = 16; break;
				default: Global.Config.RicePlugin.RiceMultiSampling = 0; break;
			}
			Global.Config.RicePlugin.RiceColorQuality = RiceColorQuality_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceOpenGLRenderSetting = RiceOpenGLRenderSetting_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceAnisotropicFiltering = RiceAnisotropicFiltering_TB.Value;

			Global.Config.RicePlugin.RiceUseDefaultHacks = RiceUseDefaultHacks_CB.Checked;
			Global.Config.RicePlugin.RiceDisableTextureCRC = RiceDisableTextureCRC_CB.Checked;
			Global.Config.RicePlugin.RiceDisableCulling = RiceDisableCulling_CB.Checked;
			Global.Config.RicePlugin.RiceIncTexRectEdge = RiceIncTexRectEdge_CB.Checked;
			Global.Config.RicePlugin.RiceZHack = RiceZHack_CB.Checked;
			Global.Config.RicePlugin.RiceTextureScaleHack = RiceTextureScaleHack_CB.Checked;
			Global.Config.RicePlugin.RicePrimaryDepthHack = RicePrimaryDepthHack_CB.Checked;
			Global.Config.RicePlugin.RiceTexture1Hack = RiceTexture1Hack_CB.Checked;
			Global.Config.RicePlugin.RiceFastLoadTile = RiceFastLoadTile_CB.Checked;
			Global.Config.RicePlugin.RiceUseSmallerTexture = RiceUseSmallerTexture_CB.Checked;

			if (InputValidate.IsValidSignedNumber(RiceVIWidth_Text.Text))
				Global.Config.RicePlugin.RiceVIWidth = int.Parse(RiceVIWidth_Text.Text);
			else
				Global.Config.RicePlugin.RiceVIWidth = -1;

			if (InputValidate.IsValidSignedNumber(RiceVIHeight_Text.Text))
				Global.Config.RicePlugin.RiceVIHeight = int.Parse(RiceVIHeight_Text.Text);
			else
				Global.Config.RicePlugin.RiceVIHeight = -1;

			Global.Config.RicePlugin.RiceUseCIWidthAndRatio = RiceUseCIWidthAndRatio_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceFullTMEM = RiceFullTMEM_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceTxtSizeMethod2 = RiceTxtSizeMethod2_CB.Checked;
			Global.Config.RicePlugin.RiceEnableTxtLOD = RiceEnableTxtLOD_CB.Checked;
			Global.Config.RicePlugin.RiceFastTextureCRC = RiceFastTextureCRC_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceEmulateClear = RiceEmulateClear_CB.Checked;
			Global.Config.RicePlugin.RiceForceScreenClear = RiceForceScreenClear_CB.Checked;
			Global.Config.RicePlugin.RiceAccurateTextureMappingHack = RiceAccurateTextureMappingHack_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceNormalBlender = RiceNormalBlender_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceDisableBlender = RiceDisableBlender_CB.Checked;
			Global.Config.RicePlugin.RiceForceDepthBuffer = RiceForceDepthBuffer_CB.Checked;
			Global.Config.RicePlugin.RiceDisableObjBG = RiceDisableObjBG_CB.Checked;
			Global.Config.RicePlugin.RiceFrameBufferOption = RiceFrameBufferOption_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceRenderToTextureOption = RiceRenderToTextureOption_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceScreenUpdateSettingHack = RiceScreenUpdateSettingHack_Combo.SelectedIndex;
			Global.Config.RicePlugin.RiceEnableHacksForGame = RiceEnableHacksForGame_Combo.SelectedIndex;
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
			RiceNormalAlphaBlender_CB.Checked = Global.Config.RicePlugin.RiceNormalAlphaBlender;
			RiceFastTextureLoading_CB.Checked = Global.Config.RicePlugin.RiceFastTextureLoading;
			RiceAccurateTextureMapping_CB.Checked = Global.Config.RicePlugin.RiceAccurateTextureMapping;
			RiceInN64Resolution_CB.Checked = Global.Config.RicePlugin.RiceInN64Resolution;
			RiceSaveVRAM_CB.Checked = Global.Config.RicePlugin.RiceSaveVRAM;
			RiceDoubleSizeForSmallTxtrBuf_CB.Checked = Global.Config.RicePlugin.RiceDoubleSizeForSmallTxtrBuf;
			RiceDefaultCombinerDisable_CB.Checked = Global.Config.RicePlugin.RiceDefaultCombinerDisable;
			RiceEnableHacks_CB.Checked = Global.Config.RicePlugin.RiceEnableHacks;
			RiceWinFrameMode_CB.Checked = Global.Config.RicePlugin.RiceWinFrameMode;
			RiceFullTMEMEmulation_CB.Checked = Global.Config.RicePlugin.RiceFullTMEMEmulation;
			RiceOpenGLVertexClipper_CB.Checked = Global.Config.RicePlugin.RiceOpenGLVertexClipper;
			RiceEnableSSE_CB.Checked = Global.Config.RicePlugin.RiceEnableSSE;
			RiceEnableVertexShader_CB.Checked = Global.Config.RicePlugin.RiceEnableVertexShader;
			RiceSkipFrame_CB.Checked = Global.Config.RicePlugin.RiceSkipFrame;
			RiceTexRectOnly_CB.Checked = Global.Config.RicePlugin.RiceTexRectOnly;
			RiceSmallTextureOnly_CB.Checked = Global.Config.RicePlugin.RiceSmallTextureOnly;
			RiceLoadHiResCRCOnly_CB.Checked = Global.Config.RicePlugin.RiceLoadHiResCRCOnly;
			RiceLoadHiResTextures_CB.Checked = Global.Config.RicePlugin.RiceLoadHiResTextures;
			RiceDumpTexturesToFiles_CB.Checked = Global.Config.RicePlugin.RiceDumpTexturesToFiles;

			RiceFrameBufferSetting_Combo.SelectedIndex = Global.Config.RicePlugin.RiceFrameBufferSetting;
			RiceFrameBufferWriteBackControl_Combo.SelectedIndex = Global.Config.RicePlugin.RiceFrameBufferWriteBackControl;
			RiceRenderToTexture_Combo.SelectedIndex = Global.Config.RicePlugin.RiceRenderToTexture;
			RiceScreenUpdateSetting_Combo.SelectedIndex = Global.Config.RicePlugin.RiceScreenUpdateSetting;
			RiceMipmapping_Combo.SelectedIndex = Global.Config.RicePlugin.RiceMipmapping;
			RiceFogMethod_Combo.SelectedIndex = Global.Config.RicePlugin.RiceFogMethod;
			RiceForceTextureFilter_Combo.SelectedIndex = Global.Config.RicePlugin.RiceForceTextureFilter;
			RiceTextureEnhancement_Combo.SelectedIndex = Global.Config.RicePlugin.RiceTextureEnhancement;
			RiceTextureEnhancementControl_Combo.SelectedIndex = Global.Config.RicePlugin.RiceTextureEnhancementControl;
			RiceTextureQuality_Combo.SelectedIndex = Global.Config.RicePlugin.RiceTextureQuality;
			RiceOpenGLDepthBufferSetting_Combo.SelectedIndex = (Global.Config.RicePlugin.RiceOpenGLDepthBufferSetting /16) -1;
			switch (Global.Config.RicePlugin.RiceMultiSampling)
			{
				case 0: RiceMultiSampling_Combo.SelectedIndex = 0; break;
				case 2: RiceMultiSampling_Combo.SelectedIndex = 1; break;
				case 4: RiceMultiSampling_Combo.SelectedIndex = 2; break;
				case 8: RiceMultiSampling_Combo.SelectedIndex = 3; break;
				case 16: RiceMultiSampling_Combo.SelectedIndex = 4; break;
				default: RiceMultiSampling_Combo.SelectedIndex = 0; break;
			}
			RiceColorQuality_Combo.SelectedIndex = Global.Config.RicePlugin.RiceColorQuality;
			RiceOpenGLRenderSetting_Combo.SelectedIndex = Global.Config.RicePlugin.RiceOpenGLRenderSetting;
			RiceAnisotropicFiltering_TB.Value = Global.Config.RicePlugin.RiceAnisotropicFiltering;
			AnisotropicFiltering_LB.Text = "Anisotropic Filtering: " + RiceAnisotropicFiltering_TB.Value.ToString();

			RiceUseDefaultHacks_CB.Checked = Global.Config.RicePlugin.RiceUseDefaultHacks;

			UpdateHacksSection();
			if (!Global.Config.RicePlugin.RiceUseDefaultHacks)
			{
				RiceTexture1Hack_CB.Checked = Global.Config.RicePlugin.RiceTexture1Hack;

				RiceDisableTextureCRC_CB.Checked = Global.Config.RicePlugin.RiceDisableTextureCRC;
				RiceDisableCulling_CB.Checked = Global.Config.RicePlugin.RiceDisableCulling;
				RiceIncTexRectEdge_CB.Checked = Global.Config.RicePlugin.RiceIncTexRectEdge;
				RiceZHack_CB.Checked = Global.Config.RicePlugin.RiceZHack;
				RiceTextureScaleHack_CB.Checked = Global.Config.RicePlugin.RiceTextureScaleHack;
				RicePrimaryDepthHack_CB.Checked = Global.Config.RicePlugin.RicePrimaryDepthHack;
				RiceTexture1Hack_CB.Checked = Global.Config.RicePlugin.RiceTexture1Hack;
				RiceFastLoadTile_CB.Checked = Global.Config.RicePlugin.RiceFastLoadTile;
				RiceUseSmallerTexture_CB.Checked = Global.Config.RicePlugin.RiceUseSmallerTexture;
				RiceVIWidth_Text.Text = Global.Config.RicePlugin.RiceVIWidth.ToString();
				RiceVIHeight_Text.Text = Global.Config.RicePlugin.RiceVIHeight.ToString();
				RiceUseCIWidthAndRatio_Combo.SelectedIndex = Global.Config.RicePlugin.RiceUseCIWidthAndRatio;
				RiceFullTMEM_Combo.SelectedIndex = Global.Config.RicePlugin.RiceFullTMEM;
				RiceTxtSizeMethod2_CB.Checked = Global.Config.RicePlugin.RiceTxtSizeMethod2;
				RiceEnableTxtLOD_CB.Checked = Global.Config.RicePlugin.RiceEnableTxtLOD;
				RiceFastTextureCRC_Combo.SelectedIndex = Global.Config.RicePlugin.RiceFastTextureCRC;
				RiceEmulateClear_CB.Checked = Global.Config.RicePlugin.RiceEmulateClear;
				RiceForceScreenClear_CB.Checked = Global.Config.RicePlugin.RiceForceScreenClear;
				RiceAccurateTextureMappingHack_Combo.SelectedIndex = Global.Config.RicePlugin.RiceAccurateTextureMappingHack;
				RiceNormalBlender_Combo.SelectedIndex = Global.Config.RicePlugin.RiceNormalBlender;
				RiceDisableBlender_CB.Checked = Global.Config.RicePlugin.RiceDisableBlender;
				RiceForceDepthBuffer_CB.Checked = Global.Config.RicePlugin.RiceForceDepthBuffer;
				RiceDisableObjBG_CB.Checked = Global.Config.RicePlugin.RiceDisableObjBG;
				RiceFrameBufferOption_Combo.SelectedIndex = Global.Config.RicePlugin.RiceFrameBufferOption;
				RiceRenderToTextureOption_Combo.SelectedIndex = Global.Config.RicePlugin.RiceRenderToTextureOption;
				RiceScreenUpdateSettingHack_Combo.SelectedIndex = Global.Config.RicePlugin.RiceScreenUpdateSettingHack;
				RiceEnableHacksForGame_Combo.SelectedIndex = Global.Config.RicePlugin.RiceEnableHacksForGame;
			}
		}
		
		private void RiceAnisotropicFiltering_TB_Scroll_1(object sender, EventArgs e)
		{
			AnisotropicFiltering_LB.Text = "Anisotropic Filtering: " + RiceAnisotropicFiltering_TB.Value.ToString();
		}

		private void RiceUseDefaultHacks_CB_CheckedChanged(object sender, EventArgs e)
		{
			UpdateHacksSection();
		}

		private void UpdateHacksSection()
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
				
				ToggleHackCheckboxEnable(false);
			}
			else
			{
				ToggleHackCheckboxEnable(true);
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

		public void ToggleHackCheckboxEnable (bool val)
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



	}
}
