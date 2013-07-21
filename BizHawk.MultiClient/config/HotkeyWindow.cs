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
	public partial class HotkeyWindow : Form
	{
		private List<KeyValuePair<string, string>> HotkeyMappingList = new List<KeyValuePair<string, string>>(); //A list of all button mappings and the hotkey they are assigned to

		public HotkeyWindow()
		{
			InitializeComponent();

			IDW_FRAMEADVANCE.SetBindings(Global.Config.FrameAdvanceBinding);
			IDW_PAUSE.SetBindings(Global.Config.EmulatorPauseBinding);
			IDW_REBOOTCORE.SetBindings(Global.Config.RebootCoreResetBinding);
			IDW_HARDRESET.SetBindings(Global.Config.HardResetBinding);
			IDW_REWIND.SetBindings(Global.Config.RewindBinding);
			IDW_UNTHROTTLE.SetBindings(Global.Config.TurboBinding);
			IDW_MAXTURBO.SetBindings(Global.Config.MaxTurboBinding);
			IDW_FASTFORWARD.SetBindings(Global.Config.FastForwardBinding);
			IDW_SCREENSHOT.SetBindings(Global.Config.ScreenshotBinding);
			IDW_FULLSCREEN.SetBindings(Global.Config.ToggleFullscreenBinding);

			IDW_QuickSave.SetBindings(Global.Config.QuickSave);
			IDW_QuickLoad.SetBindings(Global.Config.QuickLoad);
			//Save States
			IDW_SS0.SetBindings(Global.Config.SaveSlot0);
			IDW_SS1.SetBindings(Global.Config.SaveSlot1);
			IDW_SS2.SetBindings(Global.Config.SaveSlot2);
			IDW_SS3.SetBindings(Global.Config.SaveSlot3);
			IDW_SS4.SetBindings(Global.Config.SaveSlot4);
			IDW_SS5.SetBindings(Global.Config.SaveSlot5);
			IDW_SS6.SetBindings(Global.Config.SaveSlot6);
			IDW_SS7.SetBindings(Global.Config.SaveSlot7);
			IDW_SS8.SetBindings(Global.Config.SaveSlot8);
			IDW_SS9.SetBindings(Global.Config.SaveSlot9);
			//Load States
			IDW_LS0.SetBindings(Global.Config.LoadSlot0);
			IDW_LS1.SetBindings(Global.Config.LoadSlot1);
			IDW_LS2.SetBindings(Global.Config.LoadSlot2);
			IDW_LS3.SetBindings(Global.Config.LoadSlot3);
			IDW_LS4.SetBindings(Global.Config.LoadSlot4);
			IDW_LS5.SetBindings(Global.Config.LoadSlot5);
			IDW_LS6.SetBindings(Global.Config.LoadSlot6);
			IDW_LS7.SetBindings(Global.Config.LoadSlot7);
			IDW_LS8.SetBindings(Global.Config.LoadSlot8);
			IDW_LS9.SetBindings(Global.Config.LoadSlot9);
			//Select States
			IDW_ST0.SetBindings(Global.Config.SelectSlot0);
			IDW_ST1.SetBindings(Global.Config.SelectSlot1);
			IDW_ST2.SetBindings(Global.Config.SelectSlot2);
			IDW_ST3.SetBindings(Global.Config.SelectSlot3);
			IDW_ST4.SetBindings(Global.Config.SelectSlot4);
			IDW_ST5.SetBindings(Global.Config.SelectSlot5);
			IDW_ST6.SetBindings(Global.Config.SelectSlot6);
			IDW_ST7.SetBindings(Global.Config.SelectSlot7);
			IDW_ST8.SetBindings(Global.Config.SelectSlot8);
			IDW_ST9.SetBindings(Global.Config.SelectSlot9);
			IDW_TOOLBOX.SetBindings(Global.Config.ToolBox);
			IDW_SAVENAMEDSTATE.SetBindings(Global.Config.SaveNamedState);
			IDW_LOADNAMEDSTATE.SetBindings(Global.Config.LoadNamedState);
			IDW_NEXTSLOT.SetBindings(Global.Config.NextSlot);
			IDW_PREVIOUSSLOT.SetBindings(Global.Config.PreviousSlot);
			IDW_RamWatch.SetBindings(Global.Config.RamWatch);
			IDW_RamSearch.SetBindings(Global.Config.RamSearch);
			IDW_RamPoke.SetBindings(Global.Config.RamPoke);
			IDW_HexEditor.SetBindings(Global.Config.HexEditor);
			IDW_LuaConsole.SetBindings(Global.Config.LuaConsole);
			IDW_Cheats.SetBindings(Global.Config.Cheats);
			IDW_TASTudio.SetBindings(Global.Config.TASTudio);
			IDW_OpenROM.SetBindings(Global.Config.OpenROM);
			IDW_CloseROM.SetBindings(Global.Config.CloseROM);
			IDW_DisplayFPS.SetBindings(Global.Config.FPSBinding);
			IDW_FrameCounter.SetBindings(Global.Config.FrameCounterBinding);
			IDW_LagCounter.SetBindings(Global.Config.LagCounterBinding);
			IDW_InputDisplay.SetBindings(Global.Config.InputDisplayBinding);
			IDW_TOGGLEREADONLY.SetBindings(Global.Config.ReadOnlyToggleBinding);
			IDW_PLAYMOVIE.SetBindings(Global.Config.PlayMovieBinding);
			IDW_RECORDMOVIE.SetBindings(Global.Config.RecordMovieBinding);
			IDW_STOPMOVIE.SetBindings(Global.Config.StopMovieBinding);
			IDW_PLAYBEGINNING.SetBindings(Global.Config.PlayBeginningBinding);
			IDW_VOLUP.SetBindings(Global.Config.VolUpBinding);
			IDW_VOLDOWN.SetBindings(Global.Config.VolDownBinding);
			IDW_TOGGLEMTRACK.SetBindings(Global.Config.ToggleMultiTrack);
			IDW_SELECTNONE.SetBindings(Global.Config.MTRecordNone);
			IDW_MTSELECTALL.SetBindings(Global.Config.MTRecordAll);
			IDW_MTINCPLAYER.SetBindings(Global.Config.MTIncrementPlayer);
			IDW_MTDECPLAYER.SetBindings(Global.Config.MTDecrementPlayer);
			IDW_RESET.SetBindings(Global.Config.SoftResetBinding);
			IDW_RecordAVI.SetBindings(Global.Config.AVIRecordBinding);
			IDW_StopAVI.SetBindings(Global.Config.AVIStopBinding);
			IDW_ToggleMenu.SetBindings(Global.Config.ToggleMenuBinding);
			IDW_IncreaseWindowSize.SetBindings(Global.Config.IncreaseWindowSize);
			IDW_DecreaseWindowSize.SetBindings(Global.Config.DecreaseWindowSize);
			IDW_IncSpeed.SetBindings(Global.Config.IncreaseSpeedBinding);
			IDW_DecSpeed.SetBindings(Global.Config.DecreaseSpeedBinding);
			IDW_ToggleBGInput.SetBindings(Global.Config.ToggleBackgroundInput);
			IDW_Autohold.SetBindings(Global.Config.AutoholdBinding);
			IDW_AutoholdAutofire.SetBindings(Global.Config.AutoholdAutofireBinding);
			IDW_ClearAutohold.SetBindings(Global.Config.AutoholdClear);
			IDW_SNES_ToggleBG1.SetBindings(Global.Config.ToggleSNESBG1Binding);
			IDW_SNES_ToggleBG2.SetBindings(Global.Config.ToggleSNESBG2Binding);
			IDW_SNES_ToggleBG3.SetBindings(Global.Config.ToggleSNESBG3Binding);
			IDW_SNES_ToggleBG4.SetBindings(Global.Config.ToggleSNESBG4Binding);
			IDW_SNES_ToggleOBJ1.SetBindings(Global.Config.ToggleSNESOBJ1Binding);
			IDW_SNES_ToggleOBJ2.SetBindings(Global.Config.ToggleSNESOBJ2Binding);
			IDW_SNES_ToggleOBJ3.SetBindings(Global.Config.ToggleSNESOBJ3Binding);
			IDW_SNES_ToggleOBJ4.SetBindings(Global.Config.ToggleSNESOBJ4Binding);
			IDW_SaveMovie.SetBindings(Global.Config.SaveMovieBinding);
			IDW_OpenVirtualPad.SetBindings(Global.Config.OpenVirtualPadBinding);
			IDW_MoviePokeToggle.SetBindings(Global.Config.MoviePlaybackPokeModeBinding);
			IDW_ClearFrame.SetBindings(Global.Config.ClearFrameBinding);
		}
		private void button2_Click(object sender, EventArgs e)
		{
			Global.OSD.AddMessage("Hotkey config aborted");
			Close();
		}

		private void IDB_SAVE_Click(object sender, EventArgs e)
		{
			Global.Config.FastForwardBinding = IDW_FASTFORWARD.Text;
			Global.Config.FrameAdvanceBinding = IDW_FRAMEADVANCE.Text;
			Global.Config.RebootCoreResetBinding = IDW_REBOOTCORE.Text;
			Global.Config.HardResetBinding = IDW_HARDRESET.Text;
			Global.Config.RewindBinding = IDW_REWIND.Text;
			Global.Config.TurboBinding = IDW_UNTHROTTLE.Text;
			Global.Config.MaxTurboBinding = IDW_MAXTURBO.Text;
			Global.Config.EmulatorPauseBinding = IDW_PAUSE.Text;
			Global.Config.ToggleFullscreenBinding = IDW_FULLSCREEN.Text;
			Global.Config.ScreenshotBinding = IDW_SCREENSHOT.Text;

			Global.Config.QuickLoad = IDW_QuickLoad.Text;
			Global.Config.QuickSave = IDW_QuickSave.Text;

			Global.Config.SaveSlot0 = IDW_SS0.Text;
			Global.Config.SaveSlot1 = IDW_SS1.Text;
			Global.Config.SaveSlot2 = IDW_SS2.Text;
			Global.Config.SaveSlot3 = IDW_SS3.Text;
			Global.Config.SaveSlot4 = IDW_SS4.Text;
			Global.Config.SaveSlot5 = IDW_SS5.Text;
			Global.Config.SaveSlot6 = IDW_SS6.Text;
			Global.Config.SaveSlot7 = IDW_SS7.Text;
			Global.Config.SaveSlot8 = IDW_SS8.Text;
			Global.Config.SaveSlot9 = IDW_SS9.Text;

			Global.Config.LoadSlot0 = IDW_LS0.Text;
			Global.Config.LoadSlot1 = IDW_LS1.Text;
			Global.Config.LoadSlot2 = IDW_LS2.Text;
			Global.Config.LoadSlot3 = IDW_LS3.Text;
			Global.Config.LoadSlot4 = IDW_LS4.Text;
			Global.Config.LoadSlot5 = IDW_LS5.Text;
			Global.Config.LoadSlot6 = IDW_LS6.Text;
			Global.Config.LoadSlot7 = IDW_LS7.Text;
			Global.Config.LoadSlot8 = IDW_LS8.Text;
			Global.Config.LoadSlot9 = IDW_LS9.Text;

			Global.Config.SelectSlot0 = IDW_ST0.Text;
			Global.Config.SelectSlot1 = IDW_ST1.Text;
			Global.Config.SelectSlot2 = IDW_ST2.Text;
			Global.Config.SelectSlot3 = IDW_ST3.Text;
			Global.Config.SelectSlot4 = IDW_ST4.Text;
			Global.Config.SelectSlot5 = IDW_ST5.Text;
			Global.Config.SelectSlot6 = IDW_ST6.Text;
			Global.Config.SelectSlot7 = IDW_ST7.Text;
			Global.Config.SelectSlot8 = IDW_ST8.Text;
			Global.Config.SelectSlot9 = IDW_ST9.Text;
			Global.Config.ToolBox = IDW_TOOLBOX.Text;
			Global.Config.SaveNamedState = IDW_SAVENAMEDSTATE.Text;
			Global.Config.LoadNamedState = IDW_LOADNAMEDSTATE.Text;
			Global.Config.PreviousSlot = IDW_PREVIOUSSLOT.Text;
			Global.Config.NextSlot = IDW_NEXTSLOT.Text;
			Global.Config.RamWatch = IDW_RamWatch.Text;
			Global.Config.RamSearch = IDW_RamSearch.Text;
			Global.Config.RamPoke = IDW_RamPoke.Text;
			Global.Config.HexEditor = IDW_HexEditor.Text;
			Global.Config.LuaConsole = IDW_LuaConsole.Text;
			Global.Config.Cheats = IDW_Cheats.Text;
			Global.Config.TASTudio = IDW_TASTudio.Text;
			Global.Config.OpenROM = IDW_OpenROM.Text;
			Global.Config.CloseROM = IDW_CloseROM.Text;
			Global.Config.FPSBinding = IDW_DisplayFPS.Text;
			Global.Config.FrameCounterBinding = IDW_FrameCounter.Text;
			Global.Config.LagCounterBinding = IDW_LagCounter.Text;
			Global.Config.InputDisplayBinding = IDW_InputDisplay.Text;
			Global.Config.ReadOnlyToggleBinding = IDW_TOGGLEREADONLY.Text;
			Global.Config.PlayMovieBinding = IDW_PLAYMOVIE.Text;
			Global.Config.RecordMovieBinding = IDW_RECORDMOVIE.Text;
			Global.Config.StopMovieBinding = IDW_STOPMOVIE.Text;
			Global.Config.PlayBeginningBinding = IDW_PLAYBEGINNING.Text;
			Global.Config.VolUpBinding = IDW_VOLUP.Text;
			Global.Config.VolDownBinding = IDW_VOLDOWN.Text;

			Global.Config.ToggleMultiTrack = IDW_TOGGLEMTRACK.Text;
			Global.Config.MTRecordAll = IDW_MTSELECTALL.Text;
			Global.Config.MTRecordNone = IDW_SELECTNONE.Text;
			Global.Config.MTIncrementPlayer = IDW_MTINCPLAYER.Text;
			Global.Config.MTDecrementPlayer = IDW_MTDECPLAYER.Text;
			Global.Config.SoftResetBinding = IDW_RESET.Text;
			Global.Config.AVIRecordBinding = IDW_RecordAVI.Text;
			Global.Config.AVIStopBinding = IDW_StopAVI.Text;
			Global.Config.ToggleMenuBinding = IDW_ToggleMenu.Text;
			Global.Config.SaveMovieBinding = IDW_SaveMovie.Text;

			Global.Config.IncreaseWindowSize = IDW_IncreaseWindowSize.Text;
			Global.Config.DecreaseWindowSize = IDW_DecreaseWindowSize.Text;
			Global.Config.IncreaseSpeedBinding = IDW_IncSpeed.Text;
			Global.Config.DecreaseSpeedBinding = IDW_DecSpeed.Text;
			Global.Config.ToggleBackgroundInput = IDW_ToggleBGInput.Text;

			Global.Config.AutoholdBinding = IDW_Autohold.Text;
			Global.Config.AutoholdAutofireBinding = IDW_AutoholdAutofire.Text;
			Global.Config.AutoholdClear = IDW_ClearAutohold.Text;

			Global.Config.ToggleSNESBG1Binding = IDW_SNES_ToggleBG1.Text;
			Global.Config.ToggleSNESBG2Binding = IDW_SNES_ToggleBG2.Text;
			Global.Config.ToggleSNESBG3Binding = IDW_SNES_ToggleBG3.Text;
			Global.Config.ToggleSNESBG4Binding = IDW_SNES_ToggleBG4.Text;
			Global.Config.ToggleSNESOBJ1Binding = IDW_SNES_ToggleOBJ1.Text;
			Global.Config.ToggleSNESOBJ2Binding = IDW_SNES_ToggleOBJ2.Text;
			Global.Config.ToggleSNESOBJ3Binding = IDW_SNES_ToggleOBJ3.Text;
			Global.Config.ToggleSNESOBJ4Binding = IDW_SNES_ToggleOBJ4.Text;

			Global.Config.OpenVirtualPadBinding = IDW_OpenVirtualPad.Text;
			Global.Config.MoviePlaybackPokeModeBinding = IDW_MoviePokeToggle.Text;
			Global.Config.ClearFrameBinding = IDW_ClearFrame.Text;

			Global.OSD.AddMessage("Hotkey settings saved");
			DialogResult = DialogResult.OK;
			Close();
		}

		private void hotkeyTabs_SelectedIndexChanged(object sender, EventArgs e)
		{
			//hotkeyTabs.TabPages[hotkeyTabs.SelectedIndex].Controls[0].Focus();
			string name = hotkeyTabs.TabPages[hotkeyTabs.SelectedIndex].Text;
			switch (name)
			{
				case "General":
					IDW_FRAMEADVANCE.Focus();
					break;
				case "Save States":
					IDW_SS1.Focus();
					break;
				case "Movie":
					IDW_TOGGLEREADONLY.Focus();
					break;
				case "Tools":
					IDW_RamWatch.Focus();
					break;
				case "SNES":
					IDW_SNES_ToggleBG1.Focus();
					break;
			}
		}

		private void HotkeyWindow_Load(object sender, EventArgs e)
		{
			HotkeyMappingList = Global.ClientControls.MappingList();
			SetConflictLists();
			AutoTabCheckBox.Checked = Global.Config.HotkeyConfigAutoTab;
			SetAutoTab();
		}

		private void AutoTabCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.HotkeyConfigAutoTab = AutoTabCheckBox.Checked;
			SetAutoTab();
		}

		private void SetConflictLists()
		{
			//adelikat: TODO: set up the conflict list, but it is going to have to be updated and checked on the fly as the user changes the mappings
			//for (int i = 0; i < hotkeyTabs.TabPages.Count; i++)
			//{
			//    for (int j = 0; j < hotkeyTabs.TabPages[i].Controls.Count; j++)
			//    {
			//        if (hotkeyTabs.TabPages[i].Controls[j] is InputWidget)
			//        {
			//            InputWidget w = hotkeyTabs.TabPages[i].Controls[j] as InputWidget;
			//            w.SetConflictList(HotkeyMappingList);
			//        }
			//    }
			//}
		}

		private void SetAutoTab()
		{
			for (int x = 0; x < hotkeyTabs.TabPages.Count; x++)
			{
				for (int y = 0; y < hotkeyTabs.TabPages[x].Controls.Count; y++)
				{
					if (hotkeyTabs.TabPages[x].Controls[y] is InputWidget)
					{
						InputWidget w = hotkeyTabs.TabPages[x].Controls[y] as InputWidget;
						w.AutoTab = AutoTabCheckBox.Checked;
					}
				}
			}
		}

		private void label80_Click(object sender, EventArgs e)
		{

		}
	}
}
