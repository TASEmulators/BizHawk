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

		}
		
		private void RiceAnisotropicFiltering_TB_Scroll_1(object sender, EventArgs e)
		{
			AnisotropicFiltering_LB.Text = "Anisotropic Filtering: " + RiceAnisotropicFiltering_TB.Value.ToString();
		}


	}
}
