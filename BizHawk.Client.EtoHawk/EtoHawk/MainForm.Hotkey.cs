using BizHawk.Client.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.EtoHawk
{
	public partial class MainForm
	{
		private bool CheckHotkey(string trigger)
		{
			switch (trigger)
			{
				default:
					return false;

				// General
				case "Pause":
					TogglePause();
					break;
				case "Toggle Throttle":
					_unthrottled ^= true;
					//GlobalWin.OSD.AddMessage("Unthrottled: " + _unthrottled);
					break;
				case "Soft Reset":
					//SoftReset();
					break;
				case "Hard Reset":
					//HardReset();
					break;
				case "Quick Load": 
					//LoadQuickSave("QuickSave" + Global.Config.SaveSlot); 
					break;
				case "Quick Save": 
					//SaveQuickSave("QuickSave" + Global.Config.SaveSlot);
					break;
				case "Clear Autohold":
					//ClearAutohold();
					break;
				case "Screenshot":
					//TakeScreenshot();
					break;
				case "Full Screen":
					//ToggleFullscreen();
					break;
				case "Open ROM":
					OpenRom();
					break;
				case "Close ROM":
					//CloseRom();
					break;
				case "Load Last ROM":
					//LoadRomFromRecent(Global.Config.RecentRoms.MostRecent);
					break;
				case "Display FPS":
					//ToggleFPS();
					break;
				case "Frame Counter":
					//ToggleFrameCounter();
					break;
				case "Lag Counter":
					if (Global.Emulator.CanPollInput())
					{
						//ToggleLagCounter();
					}
					break;
				case "Input Display":
					//ToggleInputDisplay();
					break;
				case "Toggle BG Input":
					//ToggleBackgroundInput();
					break;
				case "Toggle Menu":
					//MainMenuStrip.Visible ^= true;
					break;
				case "Volume Up":
					//VolumeUp(); 
					break;
				case "Volume Down":
					//VolumeDown();
					break;
				case "Toggle Sound":
					//ToggleSound();
					break;
				case "Exit Program":
                    Shutdown();
					break;
				case "Record A/V":
					//RecordAv();
					break;
				case "Stop A/V":
					//StopAv();
					break;
				case "Larger Window":
					//IncreaseWindowSize();
					break;
				case "Smaller Window":
					//DecreaseWIndowSize();
					break;
				case "Increase Speed":
					//IncreaseSpeed();
					break;
				case "Decrease Speed":
					//DecreaseSpeed();
					break;
				case "Reboot Core":
					//RebootCore();
					break;
                    /*
				// Save States
				case "Save State 0": 
					SaveQuickSave("QuickSave0");
					Global.Config.SaveSlot = 0;
					UpdateStatusSlots();
					break;
				case "Save State 1": 
					SaveQuickSave("QuickSave1");
					Global.Config.SaveSlot = 1;
					UpdateStatusSlots();
					break;
				case "Save State 2": 
					SaveQuickSave("QuickSave2");
					Global.Config.SaveSlot = 2;
					UpdateStatusSlots();
					break;
				case "Save State 3":
					SaveQuickSave("QuickSave3");
					Global.Config.SaveSlot = 3;
					UpdateStatusSlots();
					break;
				case "Save State 4":
					SaveQuickSave("QuickSave4");
					Global.Config.SaveSlot = 4;
					UpdateStatusSlots();
					break;
				case "Save State 5":
					SaveQuickSave("QuickSave5");
					Global.Config.SaveSlot = 5;
					UpdateStatusSlots();
					break;
				case "Save State 6":
					SaveQuickSave("QuickSave6");
					Global.Config.SaveSlot = 6;
					UpdateStatusSlots();
					break;
				case "Save State 7":
					SaveQuickSave("QuickSave7");
					Global.Config.SaveSlot = 7;
					UpdateStatusSlots();
					break;
				case "Save State 8":
					SaveQuickSave("QuickSave8");
					Global.Config.SaveSlot = 8;
					UpdateStatusSlots();
					break;
				case "Save State 9":
					SaveQuickSave("QuickSave9");
					Global.Config.SaveSlot = 9;
					UpdateStatusSlots();
					break;
				case "Load State 0":
					LoadQuickSave("QuickSave0");
					Global.Config.SaveSlot = 0;
					UpdateStatusSlots();
					break;
				case "Load State 1":
					LoadQuickSave("QuickSave1");
					Global.Config.SaveSlot = 1;
					UpdateStatusSlots();
					break;
				case "Load State 2":
					LoadQuickSave("QuickSave2");
					Global.Config.SaveSlot = 2;
					UpdateStatusSlots();
					break;
				case "Load State 3":
					LoadQuickSave("QuickSave3");
					Global.Config.SaveSlot = 3;
					UpdateStatusSlots();
					break;
				case "Load State 4":
					LoadQuickSave("QuickSave4");
					Global.Config.SaveSlot = 4;
					UpdateStatusSlots();
					break;
				case "Load State 5":
					LoadQuickSave("QuickSave5");
					Global.Config.SaveSlot = 5;
					UpdateStatusSlots();
					break;
				case "Load State 6":
					LoadQuickSave("QuickSave6");
					Global.Config.SaveSlot = 6;
					UpdateStatusSlots();
					break;
				case "Load State 7":
					LoadQuickSave("QuickSave7");
					Global.Config.SaveSlot = 7;
					break;
				case "Load State 8":
					LoadQuickSave("QuickSave8");
					Global.Config.SaveSlot = 8;
					UpdateStatusSlots();
					break;
				case "Load State 9":
					LoadQuickSave("QuickSave9");
					Global.Config.SaveSlot = 9;
					UpdateStatusSlots();
					break;

				case "Select State 0":
					SelectSlot(0);
					break;
				case "Select State 1":
					SelectSlot(1);
					break;
				case "Select State 2":
					SelectSlot(2);
					break;
				case "Select State 3":
					SelectSlot(3);
					break;
				case "Select State 4":
					SelectSlot(4);
					break;
				case "Select State 5":
					SelectSlot(5);
					break;
				case "Select State 6":
					SelectSlot(6);
					break;
				case "Select State 7":
					SelectSlot(7);
					break;
				case "Select State 8":
					SelectSlot(8);
					break;
				case "Select State 9":
					SelectSlot(9);
					break;
				case "Save Named State":
					SaveStateAs();
					break;
				case "Load Named State":
					LoadStateAs();
					break;
				case "Previous Slot":
					PreviousSlot();
					break;
				case "Next Slot":
					NextSlot();
					break;
                    */
				// Movie
				case "Toggle read-only":
					//ToggleReadOnly();
					break;
				case "Play Movie":
					//PlayMovieMenuItem_Click(null, null);
					break;
				case "Record Movie":
					//RecordMovieMenuItem_Click(null, null); 
					break;
				case "Stop Movie":
					//StopMovie();
					break;
				case "Play from beginning":
					//RestartMovie(); 
					break;
				case "Save Movie":
					//SaveMovie(); 
					break;
				case "Toggle MultiTrack":
					Global.MovieSession.ToggleMultitrack();
					GlobalWin.DisplayManager.NeedsToPaint = true;
					break;
				case "MT Select All":
					Global.MovieSession.MultiTrack.SelectAll();
					GlobalWin.DisplayManager.NeedsToPaint = true;
					break;
				case "MT Select None":
					Global.MovieSession.MultiTrack.SelectNone();
					GlobalWin.DisplayManager.NeedsToPaint = true;
					break;
				case "MT Increment Player":
					Global.MovieSession.MultiTrack.Increment();
					GlobalWin.DisplayManager.NeedsToPaint = true;
					break;
				case "MT Decrement Player":
					Global.MovieSession.MultiTrack.Decrement();
					GlobalWin.DisplayManager.NeedsToPaint = true;
					break;
				case "Movie Poke": 
					//ToggleModePokeMode(); 
					break;

				// Tools
				/*case "Ram Watch":
					GlobalWin.Tools.LoadRamWatch(true);
					break;
				case "Ram Search":
					GlobalWin.Tools.Load<RamSearch>();
					break;
				case "Hex Editor":
					GlobalWin.Tools.Load<HexEditor>();
					break;
				case "Trace Logger":
					GlobalWin.Tools.Load<TraceLogger>();
					break;
				case "Lua Console":
					OpenLuaConsole();
					break;
				case "Cheats":
					GlobalWin.Tools.Load<Cheats>();
					break;
				case "TAStudio":
					GlobalWin.Tools.Load<TAStudio>();
					break;
				case "ToolBox":
					GlobalWin.Tools.Load<ToolBox>();
					break;
				case "Virtual Pad":
					GlobalWin.Tools.Load<VirtualpadTool>();
					break;

				// Ram Search
				case "Do Search":
					GlobalWin.Tools.RamSearch.DoSearch();
					break;
				case "New Search":
					GlobalWin.Tools.RamSearch.NewSearch();
					break;
				case "Previous Compare To":
					GlobalWin.Tools.RamSearch.NextCompareTo(reverse: true);
					break;
				case "Next Compare To":
					GlobalWin.Tools.RamSearch.NextCompareTo();
					break;
				case "Previous Operator":
					GlobalWin.Tools.RamSearch.NextOperator(reverse: true);
					break;
				case "Next Operator":
					GlobalWin.Tools.RamSearch.NextOperator();
					break;

				// SNES
				case "Toggle BG 1":
					SNES_ToggleBG1();
					break;
				case "Toggle BG 2":
					SNES_ToggleBG2(); 
					break;
				case "Toggle BG 3":
					SNES_ToggleBG3();
					break;
				case "Toggle BG 4":
					SNES_ToggleBG4();
					break;
				case "Toggle OBJ 1":
					SNES_ToggleObj1();
					break;
				case "Toggle OBJ 2":
					SNES_ToggleObj2();
					break;
				case "Toggle OBJ 3":
					SNES_ToggleOBJ3();
					break;
				case "Toggle OBJ 4":
					SNES_ToggleOBJ4();
					break;

				// Analog
				case "Y Up Small":
					GlobalWin.Tools.VirtualPad.BumpAnalogValue(null, Global.Config.Analog_SmallChange);
					break;
				case "Y Up Large":
					GlobalWin.Tools.VirtualPad.BumpAnalogValue(null, Global.Config.Analog_LargeChange);
					break;
				case "Y Down Small":
					GlobalWin.Tools.VirtualPad.BumpAnalogValue(null, -(Global.Config.Analog_SmallChange));
					break;
				case "Y Down Large":
					GlobalWin.Tools.VirtualPad.BumpAnalogValue(null, -(Global.Config.Analog_LargeChange));
					break;
				case "X Up Small":
					GlobalWin.Tools.VirtualPad.BumpAnalogValue(Global.Config.Analog_SmallChange, null);
					break;
				case "X Up Large":
					GlobalWin.Tools.VirtualPad.BumpAnalogValue(Global.Config.Analog_LargeChange, null);
					break;
				case "X Down Small":
					GlobalWin.Tools.VirtualPad.BumpAnalogValue(-(Global.Config.Analog_SmallChange), null);
					break;
				case "X Down Large":
					GlobalWin.Tools.VirtualPad.BumpAnalogValue(-(Global.Config.Analog_LargeChange), null);
					break;*/
			}

			return true;
		}
	}
}
