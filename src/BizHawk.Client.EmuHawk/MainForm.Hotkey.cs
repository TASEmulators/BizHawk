using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm
	{
		private bool CheckHotkey(string trigger)
		{
			void SelectAndSaveToSlot(int slot)
			{
				SaveQuickSave(slot);
				Config.SaveSlot = slot;
				UpdateStatusSlots();
			}
			void SelectAndLoadFromSlot(int slot)
			{
				_ = LoadQuickSave(slot);
				Config.SaveSlot = slot;
				UpdateStatusSlots();
			}

			switch (trigger)
			{
				default:
					return false;

				// General
				case "Pause":
					TogglePause();
					break;
				case "Frame Inch":
					//special! allow this key to get handled as Frame Advance, too
					FrameInch = true;
					return false;
				case "Toggle Throttle":
					ToggleUnthrottled();
					break;
				case "Soft Reset":
					SoftReset();
					break;
				case "Hard Reset":
					HardReset();
					break;
				case "Quick Load":
					_ = LoadstateCurrentSlot();
					break;
				case "Quick Save":
					SavestateCurrentSlot();
					break;
				case "Clear Autohold":
					ClearAutohold();
					break;
				case "Screenshot":
					TakeScreenshot();
					break;
				case "Screen Raw to Clipboard":
					// Ctrl+C clash. any tool that has such acc must check this.
					// maybe check if mainform has focus instead?
					if (!(Tools.IsLoaded<TAStudio>() && Tools.Get<TAStudio>().ContainsFocus)) TakeScreenshotToClipboard();
					break;
				case "Screen Client to Clipboard":
					TakeScreenshotClientToClipboard();
					break;
				case "Full Screen":
					ToggleFullscreen();
					break;
				case "Open ROM":
					OpenRom();
					break;
				case "Close ROM":
					CloseRom();
					break;
				case "Load Last ROM":
					LoadMostRecentROM();
					break;
				case "Flush SaveRAM":
					FlushSaveRAM();
					break;
				case "Display FPS":
					ToggleFps();
					break;
				case "Frame Counter":
					ToggleFrameCounter();
					break;
				case "Lag Counter":
					if (Emulator.CanPollInput()) ToggleLagCounter();
					break;
				case "Input Display":
					ToggleInputDisplay();
					break;
				case "Toggle BG Input":
					ToggleBackgroundInput();
					break;
				case "Toggle Menu":
					ShowMenuContextMenuItem_Click(this, EventArgs.Empty);
					break;
				case "Volume Up":
					VolumeUp();
					break;
				case "Volume Down":
					VolumeDown();
					break;
				case "Toggle Sound":
					ToggleSound();
					break;
				case "Exit Program":
					ScheduleShutdown();
					break;
				case "Record A/V":
					RecordAv();
					break;
				case "Stop A/V":
					StopAv();
					break;
				case "Larger Window":
					IncreaseWindowSize();
					break;
				case "Smaller Window":
					DecreaseWindowSize();
					break;
				case "Increase Speed":
					IncreaseSpeed();
					break;
				case "Reset Speed":
					ResetSpeed();
					break;
				case "Decrease Speed":
					DecreaseSpeed();
					break;
				case "Reboot Core":
					RebootCore();
					break;
				case "Toggle Skip Lag Frame":
					Config.SkipLagFrame = !Config.SkipLagFrame;
					AddOnScreenMessage($"Skip Lag Frames toggled {(Config.SkipLagFrame ? "On" : "Off")}");
					break;
				case "Toggle Key Priority":
					ToggleKeyPriority();
					break;
				case "Toggle Messages":
					DisplayMessagesMenuItem_Click(this, EventArgs.Empty);
					break;
				case "Toggle Display Nothing":
					// TODO: account for 1 when implemented
					Config.DispSpeedupFeatures = Config.DispSpeedupFeatures == 0 ? 2 : 0;
					break;
				case "Accept Background Input":
					ToggleBackgroundInput();
					break;

				// Save States
				case "Save State 1":
					SelectAndSaveToSlot(1);
					break;
				case "Save State 2":
					SelectAndSaveToSlot(2);
					break;
				case "Save State 3":
					SelectAndSaveToSlot(3);
					break;
				case "Save State 4":
					SelectAndSaveToSlot(4);
					break;
				case "Save State 5":
					SelectAndSaveToSlot(5);
					break;
				case "Save State 6":
					SelectAndSaveToSlot(6);
					break;
				case "Save State 7":
					SelectAndSaveToSlot(7);
					break;
				case "Save State 8":
					SelectAndSaveToSlot(8);
					break;
				case "Save State 9":
					SelectAndSaveToSlot(9);
					break;
				case "Save State 10":
					SelectAndSaveToSlot(10);
					break;
				case "Load State 1":
					SelectAndLoadFromSlot(1);
					break;
				case "Load State 2":
					SelectAndLoadFromSlot(2);
					break;
				case "Load State 3":
					SelectAndLoadFromSlot(3);
					break;
				case "Load State 4":
					SelectAndLoadFromSlot(4);
					break;
				case "Load State 5":
					SelectAndLoadFromSlot(5);
					break;
				case "Load State 6":
					SelectAndLoadFromSlot(6);
					break;
				case "Load State 7":
					SelectAndLoadFromSlot(7);
					break;
				case "Load State 8":
					SelectAndLoadFromSlot(8);
					break;
				case "Load State 9":
					SelectAndLoadFromSlot(9);
					break;
				case "Load State 10":
					SelectAndLoadFromSlot(10);
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
				case "Select State 10":
					SelectSlot(10);
					break;
				case "Save Named State":
					SaveStateAs();
					break;
				case "Load Named State":
					_ = LoadStateAs();
					break;
				case "Previous Slot":
					PreviousSlot();
					break;
				case "Next Slot":
					NextSlot();
					break;

				// Movie
				case "Toggle read-only":
					ToggleReadOnly();
					break;
				case "Play Movie":
					PlayMovieMenuItem_Click(null, null);
					break;
				case "Record Movie":
					RecordMovieMenuItem_Click(null, null);
					break;
				case "Stop Movie":
					StopMovie();
					break;
				case "Play from beginning":
					_ = RestartMovie();
					break;
				case "Save Movie":
					SaveMovie();
					break;

				// Tools
				case "RAM Watch":
					RamWatchMenuItem_Click(this, EventArgs.Empty);
					break;
				case "RAM Search":
					RamSearchMenuItem_Click(this, EventArgs.Empty);
					break;
				case "Hex Editor":
					HexEditorMenuItem_Click(this, EventArgs.Empty);
					break;
				case "Trace Logger":
					TraceLoggerMenuItem_Click(this, EventArgs.Empty);
					break;
				case "Lua Console":
					OpenLuaConsole();
					break;
				case "Toggle Last Lua Script":
					if (Tools.IsLoaded<LuaConsole>())
					{
						Tools.LuaConsole.ToggleLastLuaScript();
					}
					break;
				case "Cheats":
					CheatsMenuItem_Click(this, EventArgs.Empty);
					break;
				case "Toggle All Cheats":
					var cheats = CheatList.Where(static c => !c.IsSeparator).ToList();
					if (cheats.Count is 0) break;
					var firstWasEnabled = cheats[0].Enabled;
					var kind = cheats.TrueForAll(c => c.Enabled == firstWasEnabled)
						? firstWasEnabled
							? "off"
							: "on"
						: "mixed";
					foreach (var x in cheats) x.Toggle();
					AddOnScreenMessage($"Cheats toggled ({kind})");
					break;
				case "TAStudio":
					TAStudioMenuItem_Click(null, null);
					break;
				case "ToolBox":
					ToolBoxMenuItem_Click(this, EventArgs.Empty);
					break;
				case "Virtual Pad":
					VirtualPadMenuItem_Click(this, EventArgs.Empty);
					break;

				// RAM Search
				case "Do Search":
					if (!Tools.IsLoaded<RamSearch>()) return false;
					Tools.RamSearch.DoSearch();
					break;
				case "New Search":
					if (!Tools.IsLoaded<RamSearch>()) return false;
					Tools.RamSearch.NewSearch();
					break;
				case "Previous Compare To":
					if (!Tools.IsLoaded<RamSearch>()) return false;
					Tools.RamSearch.NextCompareTo(reverse: true);
					break;
				case "Next Compare To":
					if (!Tools.IsLoaded<RamSearch>()) return false;
					Tools.RamSearch.NextCompareTo();
					break;
				case "Previous Operator":
					if (!Tools.IsLoaded<RamSearch>()) return false;
					Tools.RamSearch.NextOperator(reverse: true);
					break;
				case "Next Operator":
					if (!Tools.IsLoaded<RamSearch>()) return false;
					Tools.RamSearch.NextOperator();
					break;

				// TAStudio
				case "Add Branch":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.AddBranchExternal();
					break;
				case "Delete Branch":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.RemoveBranchExternal();
					break;
				case "Show Cursor":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.SetVisibleFrame();
					Tools.TAStudio.RefreshDialog();
					break;
				case "Toggle Follow Cursor":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					var playbackBox = Tools.TAStudio.TasPlaybackBox;
					playbackBox.FollowCursor = !playbackBox.FollowCursor;
					break;
				case "Toggle Auto-Restore":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					var playbackBox1 = Tools.TAStudio.TasPlaybackBox;
					playbackBox1.AutoRestore = !playbackBox1.AutoRestore;
					break;
				case "Toggle Turbo Seek":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					var playbackBox2 = Tools.TAStudio.TasPlaybackBox;
					playbackBox2.TurboSeek = !playbackBox2.TurboSeek;
					break;
				case "Undo":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.UndoExternal();
					break;
				case "Redo":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.RedoExternal();
					break;
				case "Sel. bet. Markers":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.SelectBetweenMarkersExternal();
					break;
				case "Select All":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.SelectAllExternal();
					break;
				case "Reselect Clip.":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.ReselectClipboardExternal();
					break;
				case "Clear Frames":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.ClearFramesExternal();
					break;
				case "Insert Frame":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.InsertFrameExternal();
					break;
				case "Insert # Frames":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.InsertNumFramesExternal();
					break;
				case "Delete Frames":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.DeleteFramesExternal();
					break;
				case "Clone Frames":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.CloneFramesExternal();
					break;
				case "Clone # Times":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.CloneFramesXTimesExternal();
					break;
				case "Analog Increment":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.AnalogIncrementByOne();
					break;
				case "Analog Decrement":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.AnalogDecrementByOne();
					break;
				case "Analog Incr. by 10":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.AnalogIncrementByTen();
					break;
				case "Analog Decr. by 10":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.AnalogDecrementByTen();
					break;
				case "Analog Maximum":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.AnalogMax();
					break;
				case "Analog Minimum":
					if (!Tools.IsLoaded<TAStudio>()) return false;
					Tools.TAStudio.AnalogMin();
					break;

				// SNES
				case "Toggle BG 1":
					SNES_ToggleBg(1);
					break;
				case "Toggle BG 2":
					SNES_ToggleBg(2);
					break;
				case "Toggle BG 3":
					SNES_ToggleBg(3);
					break;
				case "Toggle BG 4":
					SNES_ToggleBg(4);
					break;
				case "Toggle OBJ 1":
					SNES_ToggleObj(1);
					break;
				case "Toggle OBJ 2":
					SNES_ToggleObj(2);
					break;
				case "Toggle OBJ 3":
					SNES_ToggleObj(3);
					break;
				case "Toggle OBJ 4":
					SNES_ToggleObj(4);
					break;

				// GB
				case "GB Toggle BG":
					GB_ToggleBackgroundLayer();
					break;
				case "GB Toggle Obj":
					GB_ToggleObjectLayer();
					break;
				case "GB Toggle Window":
					GB_ToggleWindowLayer();
					break;

				// Analog
				case "Y Up Small":
					Tools.VirtualPad.BumpAnalogValue(null, Config.AnalogSmallChange);
					break;
				case "Y Up Large":
					Tools.VirtualPad.BumpAnalogValue(null, Config.AnalogLargeChange);
					break;
				case "Y Down Small":
					Tools.VirtualPad.BumpAnalogValue(null, -Config.AnalogSmallChange);
					break;
				case "Y Down Large":
					Tools.VirtualPad.BumpAnalogValue(null, -Config.AnalogLargeChange);
					break;
				case "X Up Small":
					Tools.VirtualPad.BumpAnalogValue(Config.AnalogSmallChange, null);
					break;
				case "X Up Large":
					Tools.VirtualPad.BumpAnalogValue(Config.AnalogLargeChange, null);
					break;
				case "X Down Small":
					Tools.VirtualPad.BumpAnalogValue(-Config.AnalogSmallChange, null);
					break;
				case "X Down Large":
					Tools.VirtualPad.BumpAnalogValue(-Config.AnalogLargeChange, null);
					break;

				// DS
				case "Next Screen Layout":
					NDS_IncrementLayout(1);
					break;
				case "Previous Screen Layout":
					NDS_IncrementLayout(-1);
					break;
				case "Screen Rotate":
					NDS_IncrementScreenRotate();
					break;
			}

			return true;
		}

		// Determines if the value is a hotkey  that would be handled outside of the CheckHotkey method
		private bool IsInternalHotkey(string trigger)
		{
			switch (trigger)
			{
				default:
					return false;
				case "Autohold":
				case "Autofire":
				case "Frame Advance":
				case "Turbo":
				case "Rewind":
				case "Fast Forward":
				case "Open RA Overlay":
					return true;
				case "RA Up":
				case "RA Down":
				case "RA Left":
				case "RA Right":
				case "RA Confirm":
				case "RA Cancel":
				case "RA Quit":
					// don't consider these keys outside of RAIntegration overlay being active
					return RA is RAIntegration { OverlayActive: true };
			}
		}
	}
}
