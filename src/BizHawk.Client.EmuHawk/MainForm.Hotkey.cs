using System;
using System.Linq;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

namespace BizHawk.Client.EmuHawk
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
					ThrottleMessage();
					break;
				case "Soft Reset":
					SoftReset();
					break;
				case "Hard Reset":
					HardReset();
					break;
				case "Quick Load": 
					LoadQuickSave($"QuickSave{Config.SaveSlot}");
					break;
				case "Quick Save": 
					SaveQuickSave($"QuickSave{Config.SaveSlot}");
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
					if (Tools.IsLoaded<TAStudio>() && Tools.Get<TAStudio>().ContainsFocus)
					{
						break;
					}

					TakeScreenshotToClipboard();
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
					LoadRomFromRecent(Config.RecentRoms.MostRecent);
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
					if (Emulator.CanPollInput())
					{
						ToggleLagCounter();
					}

					break;
				case "Input Display":
					ToggleInputDisplay();
					break;
				case "Toggle BG Input":
					ToggleBackgroundInput();
					break;
				case "Toggle Menu":
					MainMenuStrip.Visible ^= true;
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
					_exitRequestPending = true;
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
					Config.SkipLagFrame ^= true;
					AddOnScreenMessage($"Skip Lag Frames toggled {(Config.SkipLagFrame ? "On" : "Off")}");
					break;
				case "Toggle Key Priority":
					ToggleKeyPriority();
					break;

				// Save States
				case "Save State 0": 
					SaveQuickSave("QuickSave0");
					Config.SaveSlot = 0;
					UpdateStatusSlots();
					break;
				case "Save State 1": 
					SaveQuickSave("QuickSave1");
					Config.SaveSlot = 1;
					UpdateStatusSlots();
					break;
				case "Save State 2": 
					SaveQuickSave("QuickSave2");
					Config.SaveSlot = 2;
					UpdateStatusSlots();
					break;
				case "Save State 3":
					SaveQuickSave("QuickSave3");
					Config.SaveSlot = 3;
					UpdateStatusSlots();
					break;
				case "Save State 4":
					SaveQuickSave("QuickSave4");
					Config.SaveSlot = 4;
					UpdateStatusSlots();
					break;
				case "Save State 5":
					SaveQuickSave("QuickSave5");
					Config.SaveSlot = 5;
					UpdateStatusSlots();
					break;
				case "Save State 6":
					SaveQuickSave("QuickSave6");
					Config.SaveSlot = 6;
					UpdateStatusSlots();
					break;
				case "Save State 7":
					SaveQuickSave("QuickSave7");
					Config.SaveSlot = 7;
					UpdateStatusSlots();
					break;
				case "Save State 8":
					SaveQuickSave("QuickSave8");
					Config.SaveSlot = 8;
					UpdateStatusSlots();
					break;
				case "Save State 9":
					SaveQuickSave("QuickSave9");
					Config.SaveSlot = 9;
					UpdateStatusSlots();
					break;
				case "Load State 0":
					LoadQuickSave("QuickSave0");
					Config.SaveSlot = 0;
					UpdateStatusSlots();
					break;
				case "Load State 1":
					LoadQuickSave("QuickSave1");
					Config.SaveSlot = 1;
					UpdateStatusSlots();
					break;
				case "Load State 2":
					LoadQuickSave("QuickSave2");
					Config.SaveSlot = 2;
					UpdateStatusSlots();
					break;
				case "Load State 3":
					LoadQuickSave("QuickSave3");
					Config.SaveSlot = 3;
					UpdateStatusSlots();
					break;
				case "Load State 4":
					LoadQuickSave("QuickSave4");
					Config.SaveSlot = 4;
					UpdateStatusSlots();
					break;
				case "Load State 5":
					LoadQuickSave("QuickSave5");
					Config.SaveSlot = 5;
					UpdateStatusSlots();
					break;
				case "Load State 6":
					LoadQuickSave("QuickSave6");
					Config.SaveSlot = 6;
					UpdateStatusSlots();
					break;
				case "Load State 7":
					LoadQuickSave("QuickSave7");
					Config.SaveSlot = 7;
					UpdateStatusSlots();
					break;
				case "Load State 8":
					LoadQuickSave("QuickSave8");
					Config.SaveSlot = 8;
					UpdateStatusSlots();
					break;
				case "Load State 9":
					LoadQuickSave("QuickSave9");
					Config.SaveSlot = 9;
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
					RestartMovie(); 
					break;
				case "Save Movie":
					SaveMovie(); 
					break;

				// Tools
				case "RAM Watch":
					Tools.LoadRamWatch(true);
					break;
				case "RAM Search":
					Tools.Load<RamSearch>();
					break;
				case "Hex Editor":
					Tools.Load<HexEditor>();
					break;
				case "Trace Logger":
					Tools.Load<TraceLogger>();
					break;
				case "Lua Console":
					OpenLuaConsole();
					break;
				case "Cheats":
					Tools.Load<Cheats>();
					break;
				case "Toggle All Cheats":
					if (CheatList.Any())
					{
						string type = " (mixed)";
						if (CheatList.All(c => c.Enabled))
						{
							type = " (off)";
						}
						else if (CheatList.All(c => !c.Enabled))
						{
							type = " (on)";
						}

						foreach (var x in CheatList)
						{
							x.Toggle();
						}

						AddOnScreenMessage($"Cheats toggled{type}");
					}

					break;
				case "TAStudio":
					Tools.Load<TAStudio>();
					break;
				case "ToolBox":
					Tools.Load<ToolBox>();
					break;
				case "Virtual Pad":
					Tools.Load<VirtualpadTool>();
					break;

				// RAM Search
				case "Do Search":
					if (Tools.IsLoaded<RamSearch>())
					{
						Tools.RamSearch.DoSearch();
					}
					else
					{
						return false;
					}

					break;
				case "New Search":
					if (Tools.IsLoaded<RamSearch>())
					{
						Tools.RamSearch.NewSearch();
					}
					else
					{
						return false;
					}

					break;
				case "Previous Compare To":
					if (Tools.IsLoaded<RamSearch>())
					{
						Tools.RamSearch.NextCompareTo(reverse: true);
					}
					else
					{
						return false;
					}

					break;
				case "Next Compare To":
					if (Tools.IsLoaded<RamSearch>())
					{
						Tools.RamSearch.NextCompareTo();
					}
					else
					{
						return false;
					}

					break;
				case "Previous Operator":
					if (Tools.IsLoaded<RamSearch>())
					{
						Tools.RamSearch.NextOperator(reverse: true);
					}
					else
					{
						return false;
					}

					break;
				case "Next Operator":
					if (Tools.IsLoaded<RamSearch>())
					{
						Tools.RamSearch.NextOperator();
					}
					else
					{
						return false;
					}

					break;

				// TAStudio
				case "Add Branch":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.AddBranchExternal();
					}
					else
					{
						return false;
					}

					break;
				case "Delete Branch":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.RemoveBranchExternal();
					}
					else
					{
						return false;
					}

					break;
				case "Show Cursor":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.SetVisibleFrame();
						Tools.TAStudio.RefreshDialog();
					}
					else
					{
						return false;
					}

					break;
				case "Toggle Follow Cursor":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.TasPlaybackBox.FollowCursor ^= true;
					}
					else
					{
						return false;
					}

					break;
				case "Toggle Auto-Restore":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.TasPlaybackBox.AutoRestore ^= true;
					}
					else
					{
						return false;
					}

					break;
				case "Toggle Turbo Seek":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.TasPlaybackBox.TurboSeek ^= true;
					}
					else
					{
						return false;
					}

					break;
				case "Undo":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.UndoExternal();
					}
					else
					{
						return false;
					}

					break;
				case "Redo":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.RedoExternal();
					}
					else
					{
						return false;
					}

					break;
				case "Select between Markers":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.SelectBetweenMarkersExternal();
					}
					else
					{
						return false;
					}
					
					break;
				case "Select All":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.SelectAllExternal();
					}
					else
					{
						return false;
					}
					
					break;
				case "Reselect Clip.":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.ReselectClipboardExternal();
					}
					else
					{
						return false;
					}
					
					break;
				case "Clear Frames":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.ClearFramesExternal();
					}
					else
					{
						return false;
					}

					break;
				case "Insert Frame":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.InsertFrameExternal();
					}
					else
					{
						return false;
					}

					break;
				case "Insert # Frames":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.InsertNumFramesExternal();
					}
					else
					{
						return false;
					}
					break;
				case "Delete Frames":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.DeleteFramesExternal();
					}
					else
					{
						return false;
					}

					break;
				case "Clone Frames":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.CloneFramesExternal();
					}
					else
					{
						return false;
					}

					break;
				case "Analog Increment":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.AnalogIncrementByOne();
					}
					else
					{
						return false;
					}

					break;
				case "Analog Decrement":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.AnalogDecrementByOne();
					}
					else
					{
						return false;
					}

					break;
				case "Analog Incr. by 10":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.AnalogIncrementByTen();
					}
					else
					{
						return false;
					}

					break;
				case "Analog Decr. by 10":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.AnalogDecrementByTen();
					}
					else
					{
						return false;
					}

					break;
				case "Analog Maximum":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.AnalogMax();
					}
					else
					{
						return false;
					}

					break;
				case "Analog Minimum":
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.AnalogMin();
					}
					else
					{
						return false;
					}

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
					if (Emulator is Gameboy gb)
					{
						var s = gb.GetSettings();
						s.DisplayBG ^= true;
						gb.PutSettings(s);
						AddOnScreenMessage($"BG toggled {(s.DisplayBG ? "on" : "off")}");
					}

					break;
				case "GB Toggle Obj":
					if (Emulator is Gameboy gb2)
					{
						var s = gb2.GetSettings();
						s.DisplayOBJ ^= true;
						gb2.PutSettings(s);
						AddOnScreenMessage($"OBJ toggled {(s.DisplayBG ? "on" : "off")}");
					}

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
					IncrementDSLayout(1);
					break;
				case "Previous Screen Layout":
					IncrementDSLayout(-1);
					break;
				case "Screen Rotate":
					IncrementDSScreenRotate();
					break;
			}

			return true;
		}

		private void IncrementDSScreenRotate()
		{
			if (Emulator is MelonDS ds)
			{
				var settings = ds.GetSettings();
				settings.ScreenRotation = settings.ScreenRotation switch
				{
					MelonDS.ScreenRotationKind.Rotate0 => settings.ScreenRotation = MelonDS.ScreenRotationKind.Rotate90,
					MelonDS.ScreenRotationKind.Rotate90 => settings.ScreenRotation = MelonDS.ScreenRotationKind.Rotate180,
					MelonDS.ScreenRotationKind.Rotate180 => settings.ScreenRotation = MelonDS.ScreenRotationKind.Rotate270,
					MelonDS.ScreenRotationKind.Rotate270 => settings.ScreenRotation = MelonDS.ScreenRotationKind.Rotate0,
					_ => settings.ScreenRotation
				};
				ds.PutSettings(settings);
				AddOnScreenMessage($"Screen rotation to {settings.ScreenRotation}");
				FrameBufferResized();
			}
		}

		private void IncrementDSLayout(int delta)
		{
			bool decrement = delta == -1;
			if (Emulator is MelonDS ds)
			{
				var settings = ds.GetSettings();
				var num = (int)settings.ScreenLayout;
				if (decrement)
				{
					num--;
				}
				else
				{
					num++;
				}

				var next = (MelonDS.ScreenLayoutKind)Enum.Parse(typeof(MelonDS.ScreenLayoutKind), num.ToString());
				if (typeof(MelonDS.ScreenLayoutKind).IsEnumDefined(next))
				{
					settings.ScreenLayout = next;

					ds.PutSettings(settings);
					AddOnScreenMessage($"Screen layout to {next}");
					FrameBufferResized();
				}
			}
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
					return true;
			}
		}
	}
}
