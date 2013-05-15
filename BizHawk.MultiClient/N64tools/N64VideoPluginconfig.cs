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
			Global.Config.RiceNormalAlphaBlender = RiceNormalAlphaBlender_CB.Checked;
			Global.Config.RiceFastTextureLoading = RiceFastTextureLoading_CB.Checked;
			Global.Config.RiceAccurateTextureMapping = RiceAccurateTextureMapping_CB.Checked;
			Global.Config.RiceInN64Resolution = RiceInN64Resolution_CB.Checked;
			Global.Config.RiceSaveVRAM = RiceSaveVRAM_CB.Checked;
			Global.Config.RiceDoubleSizeForSmallTxtrBuf = RiceDoubleSizeForSmallTxtrBuf_CB.Checked;
			Global.Config.RiceDefaultCombinerDisable = RiceDefaultCombinerDisable_CB.Checked;
			Global.Config.RiceEnableHacks = RiceEnableHacks_CB.Checked;
			Global.Config.RiceWinFrameMode = RiceWinFrameMode_CB.Checked;
			Global.Config.RiceFullTMEMEmulation = RiceFullTMEMEmulation_CB.Checked;
			Global.Config.RiceOpenGLVertexClipper = RiceOpenGLVertexClipper_CB.Checked;
			Global.Config.RiceEnableSSE = RiceEnableSSE_CB.Checked;
			Global.Config.RiceEnableVertexShader = RiceEnableVertexShader_CB.Checked;
			Global.Config.RiceSkipFrame = RiceSkipFrame_CB.Checked;
			Global.Config.RiceTexRectOnly = RiceTexRectOnly_CB.Checked;
			Global.Config.RiceSmallTextureOnly = RiceSmallTextureOnly_CB.Checked;
			Global.Config.RiceLoadHiResCRCOnly = RiceLoadHiResCRCOnly_CB.Checked;
			Global.Config.RiceLoadHiResTextures = RiceLoadHiResTextures_CB.Checked;
			Global.Config.RiceDumpTexturesToFiles = RiceDumpTexturesToFiles_CB.Checked;

			Global.Config.RiceFrameBufferSetting = RiceFrameBufferSetting_Combo.SelectedIndex;
			Global.Config.RiceFrameBufferWriteBackControl = RiceFrameBufferWriteBackControl_Combo.SelectedIndex;
			Global.Config.RiceRenderToTexture = RiceRenderToTexture_Combo.SelectedIndex;
			Global.Config.RiceScreenUpdateSetting = RiceScreenUpdateSetting_Combo.SelectedIndex;
			Global.Config.RiceMipmapping = RiceMipmapping_Combo.SelectedIndex;
			Global.Config.RiceFogMethod = RiceFogMethod_Combo.SelectedIndex;
			Global.Config.RiceForceTextureFilter = RiceForceTextureFilter_Combo.SelectedIndex;
			Global.Config.RiceTextureEnhancement = RiceTextureEnhancement_Combo.SelectedIndex;
			Global.Config.RiceTextureEnhancementControl = RiceTextureEnhancementControl_Combo.SelectedIndex;
			Global.Config.RiceTextureQuality = RiceTextureQuality_Combo.SelectedIndex;
			Global.Config.RiceOpenGLDepthBufferSetting = (RiceOpenGLDepthBufferSetting_Combo.SelectedIndex + 1) * 16;
			switch (RiceMultiSampling_Combo.SelectedIndex)
			{
				case 0: Global.Config.RiceMultiSampling = 0; break;
				case 1: Global.Config.RiceMultiSampling = 2; break;
				case 2: Global.Config.RiceMultiSampling = 4; break;
				case 3: Global.Config.RiceMultiSampling = 8; break;
				case 4: Global.Config.RiceMultiSampling = 16; break;
				default : Global.Config.RiceMultiSampling = 0; break;
			}
			Global.Config.RiceColorQuality = RiceColorQuality_Combo.SelectedIndex;
			Global.Config.RiceOpenGLRenderSetting = RiceOpenGLRenderSetting_Combo.SelectedIndex;
			Global.Config.RiceAnisotropicFiltering = RiceAnisotropicFiltering_TB.Value;

			Global.Config.RiceUseDefaultHacks = RiceUseDefaultHacks_CB.Checked;
			Global.Config.RiceDisableTextureCRC = RiceDisableTextureCRC_CB.Checked;
			Global.Config.RiceDisableCulling = RiceDisableCulling_CB.Checked;
			Global.Config.RiceIncTexRectEdge = RiceIncTexRectEdge_CB.Checked;
			Global.Config.RiceZHack = RiceZHack_CB.Checked;
			Global.Config.RiceTextureScaleHack = RiceTextureScaleHack_CB.Checked;
			Global.Config.RicePrimaryDepthHack = RicePrimaryDepthHack_CB.Checked;
			Global.Config.RiceTexture1Hack = RiceTexture1Hack_CB.Checked;
			Global.Config.RiceFastLoadTile = RiceFastLoadTile_CB.Checked;
			Global.Config.RiceUseSmallerTexture = RiceUseSmallerTexture_CB.Checked;

			if (InputValidate.IsValidSignedNumber(RiceVIWidth_Text.Text))
				Global.Config.RiceVIWidth = int.Parse(RiceVIWidth_Text.Text);
			else
				Global.Config.RiceVIWidth = -1;

			if (InputValidate.IsValidSignedNumber(RiceVIHeight_Text.Text))
				Global.Config.RiceVIHeight = int.Parse(RiceVIHeight_Text.Text);
			else
				Global.Config.RiceVIHeight = -1;

			Global.Config.RiceUseCIWidthAndRatio = RiceUseCIWidthAndRatio_Combo.SelectedIndex;
			Global.Config.RiceFullTMEM = RiceFullTMEM_Combo.SelectedIndex;
			Global.Config.RiceTxtSizeMethod2 = RiceTxtSizeMethod2_CB.Checked;
			Global.Config.RiceEnableTxtLOD = RiceEnableTxtLOD_CB.Checked;
			Global.Config.RiceFastTextureCRC = RiceFastTextureCRC_Combo.SelectedIndex;
			Global.Config.RiceEmulateClear = RiceEmulateClear_CB.Checked;
			Global.Config.RiceForceScreenClear = RiceForceScreenClear_CB.Checked;
			Global.Config.RiceAccurateTextureMappingHack = RiceAccurateTextureMappingHack_Combo.SelectedIndex;
			Global.Config.RiceNormalBlender = RiceNormalBlender_Combo.SelectedIndex;
			Global.Config.RiceDisableBlender = RiceDisableBlender_CB.Checked;
			Global.Config.RiceForceDepthBuffer = RiceForceDepthBuffer_CB.Checked;
			Global.Config.RiceDisableObjBG = RiceDisableObjBG_CB.Checked;
			Global.Config.RiceFrameBufferOption = RiceFrameBufferOption_Combo.SelectedIndex;
			Global.Config.RiceRenderToTextureOption = RiceRenderToTextureOption_Combo.SelectedIndex;
			Global.Config.RiceScreenUpdateSettingHack = RiceScreenUpdateSettingHack_Combo.SelectedIndex;
			Global.Config.RiceEnableHacksForGame = RiceEnableHacksForGame_Combo.SelectedIndex;
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
			RiceNormalAlphaBlender_CB.Checked = Global.Config.RiceNormalAlphaBlender;
			RiceFastTextureLoading_CB.Checked = Global.Config.RiceFastTextureLoading;
			RiceAccurateTextureMapping_CB.Checked = Global.Config.RiceAccurateTextureMapping;
			RiceInN64Resolution_CB.Checked = Global.Config.RiceInN64Resolution;
			RiceSaveVRAM_CB.Checked = Global.Config.RiceSaveVRAM;
			RiceDoubleSizeForSmallTxtrBuf_CB.Checked = Global.Config.RiceDoubleSizeForSmallTxtrBuf;
			RiceDefaultCombinerDisable_CB.Checked = Global.Config.RiceDefaultCombinerDisable;
			RiceEnableHacks_CB.Checked = Global.Config.RiceEnableHacks;
			RiceWinFrameMode_CB.Checked = Global.Config.RiceWinFrameMode;
			RiceFullTMEMEmulation_CB.Checked = Global.Config.RiceFullTMEMEmulation;
			RiceOpenGLVertexClipper_CB.Checked = Global.Config.RiceOpenGLVertexClipper;
			RiceEnableSSE_CB.Checked = Global.Config.RiceEnableSSE;
			RiceEnableVertexShader_CB.Checked = Global.Config.RiceEnableVertexShader;
			RiceSkipFrame_CB.Checked = Global.Config.RiceSkipFrame;
			RiceTexRectOnly_CB.Checked = Global.Config.RiceTexRectOnly;
			RiceSmallTextureOnly_CB.Checked = Global.Config.RiceSmallTextureOnly;
			RiceLoadHiResCRCOnly_CB.Checked = Global.Config.RiceLoadHiResCRCOnly;
			RiceLoadHiResTextures_CB.Checked = Global.Config.RiceLoadHiResTextures;
			RiceDumpTexturesToFiles_CB.Checked = Global.Config.RiceDumpTexturesToFiles;

			RiceFrameBufferSetting_Combo.SelectedIndex = Global.Config.RiceFrameBufferSetting;
			RiceFrameBufferWriteBackControl_Combo.SelectedIndex = Global.Config.RiceFrameBufferWriteBackControl;
			RiceRenderToTexture_Combo.SelectedIndex = Global.Config.RiceRenderToTexture;
			RiceScreenUpdateSetting_Combo.SelectedIndex = Global.Config.RiceScreenUpdateSetting;
			RiceMipmapping_Combo.SelectedIndex = Global.Config.RiceMipmapping;
			RiceFogMethod_Combo.SelectedIndex = Global.Config.RiceFogMethod;
			RiceForceTextureFilter_Combo.SelectedIndex = Global.Config.RiceForceTextureFilter;
			RiceTextureEnhancement_Combo.SelectedIndex = Global.Config.RiceTextureEnhancement;
			RiceTextureEnhancementControl_Combo.SelectedIndex = Global.Config.RiceTextureEnhancementControl;
			RiceTextureQuality_Combo.SelectedIndex = Global.Config.RiceTextureQuality;
			RiceOpenGLDepthBufferSetting_Combo.SelectedIndex = (Global.Config.RiceOpenGLDepthBufferSetting /16) -1;
			switch (Global.Config.RiceMultiSampling)
			{
				case 0: RiceMultiSampling_Combo.SelectedIndex = 0; break;
				case 2: RiceMultiSampling_Combo.SelectedIndex = 1; break;
				case 4: RiceMultiSampling_Combo.SelectedIndex = 2; break;
				case 8: RiceMultiSampling_Combo.SelectedIndex = 3; break;
				case 16: RiceMultiSampling_Combo.SelectedIndex = 4; break;
				default: RiceMultiSampling_Combo.SelectedIndex = 0; break;
			}
			RiceColorQuality_Combo.SelectedIndex = Global.Config.RiceColorQuality;
			RiceOpenGLRenderSetting_Combo.SelectedIndex = Global.Config.RiceOpenGLRenderSetting;
			RiceAnisotropicFiltering_TB.Value = Global.Config.RiceAnisotropicFiltering;
			AnisotropicFiltering_LB.Text = "Anisotropic Filtering: " + RiceAnisotropicFiltering_TB.Value.ToString();

			RiceUseDefaultHacks_CB.Checked = Global.Config.RiceUseDefaultHacks;

			UpdateHacksSection();
			if (!Global.Config.RiceUseDefaultHacks)
			{
				RiceTexture1Hack_CB.Checked = Global.Config.RiceTexture1Hack;

				RiceDisableTextureCRC_CB.Checked = Global.Config.RiceDisableTextureCRC;
				RiceDisableCulling_CB.Checked = Global.Config.RiceDisableCulling;
				RiceIncTexRectEdge_CB.Checked = Global.Config.RiceIncTexRectEdge;
				RiceZHack_CB.Checked = Global.Config.RiceZHack;
				RiceTextureScaleHack_CB.Checked = Global.Config.RiceTextureScaleHack;
				RicePrimaryDepthHack_CB.Checked = Global.Config.RicePrimaryDepthHack;
				RiceTexture1Hack_CB.Checked = Global.Config.RiceTexture1Hack;
				RiceFastLoadTile_CB.Checked = Global.Config.RiceFastLoadTile;
				RiceUseSmallerTexture_CB.Checked = Global.Config.RiceUseSmallerTexture;
				RiceVIWidth_Text.Text = Global.Config.RiceVIWidth.ToString();
				RiceVIHeight_Text.Text = Global.Config.RiceVIHeight.ToString();
				RiceUseCIWidthAndRatio_Combo.SelectedIndex = Global.Config.RiceUseCIWidthAndRatio;
				RiceFullTMEM_Combo.SelectedIndex = Global.Config.RiceFullTMEM;
				RiceTxtSizeMethod2_CB.Checked = Global.Config.RiceTxtSizeMethod2;
				RiceEnableTxtLOD_CB.Checked = Global.Config.RiceEnableTxtLOD;
				RiceFastTextureCRC_Combo.SelectedIndex = Global.Config.RiceFastTextureCRC;
				RiceEmulateClear_CB.Checked = Global.Config.RiceEmulateClear;
				RiceForceScreenClear_CB.Checked = Global.Config.RiceForceScreenClear;
				RiceAccurateTextureMappingHack_Combo.SelectedIndex = Global.Config.RiceAccurateTextureMappingHack;
				RiceNormalBlender_Combo.SelectedIndex = Global.Config.RiceNormalBlender;
				RiceDisableBlender_CB.Checked = Global.Config.RiceDisableBlender;
				RiceForceDepthBuffer_CB.Checked = Global.Config.RiceForceDepthBuffer;
				RiceDisableObjBG_CB.Checked = Global.Config.RiceDisableObjBG;
				RiceFrameBufferOption_Combo.SelectedIndex = Global.Config.RiceFrameBufferOption;
				RiceRenderToTextureOption_Combo.SelectedIndex = Global.Config.RiceRenderToTextureOption;
				RiceScreenUpdateSettingHack_Combo.SelectedIndex = Global.Config.RiceScreenUpdateSettingHack;
				RiceEnableHacksForGame_Combo.SelectedIndex = Global.Config.RiceEnableHacksForGame;
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
