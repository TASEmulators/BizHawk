using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

using NLua;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	[Description("A library for manipulating the Tastudio dialog of the EmuHawk client")]
	[LuaLibrary(released: true)]
	public sealed class TastudioLuaLibrary : LuaLibraryBase
	{
		public TastudioLuaLibrary(Lua lua)
			: base(lua) { }

		public TastudioLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "tastudio";

		private TAStudio Tastudio => GlobalWin.Tools.Get<TAStudio>() as TAStudio;

		private struct PendingChanges
		{
			public LuaChangeTypes type;
			public InputChangeTypes inputType;
			public int frame;
			public int number;
			public string button;
			public bool valueBool;
			public float valueFloat;
		};

		public enum LuaChangeTypes
		{
			InputChange,
			InsertFrames,
			DeleteFrames,
		};

		public enum InputChangeTypes
		{
			Bool,
			Float,
		};

		private List<PendingChanges> changeList = new List<PendingChanges>(); //TODO: Initialize it to empty list on a script reload, and have each script have it's own list

		[LuaMethodExample("if ( tastudio.engaged( ) ) then\r\n\tconsole.log( \"returns whether or not tastudio is currently engaged ( active )\" );\r\nend;")]
		[LuaMethod("engaged", "returns whether or not tastudio is currently engaged (active)")]
		public bool Engaged()
		{
			return GlobalWin.Tools.Has<TAStudio>(); // TODO: eventually tastudio should have an engaged flag
		}

		[LuaMethodExample("if ( tastudio.getrecording( ) ) then\r\n\tconsole.log( \"returns whether or not TAStudio is in recording mode\" );\r\nend;")]
		[LuaMethod("getrecording", "returns whether or not TAStudio is in recording mode")]
		public bool GetRecording()
		{
			return Tastudio.TasPlaybackBox.RecordingMode;
		}

		[LuaMethodExample("tastudio.setrecording( true );")]
		[LuaMethod("setrecording", "sets the recording mode on/off depending on the parameter")]
		public void SetRecording(bool val)
		{
			if (Tastudio.TasPlaybackBox.RecordingMode != val)
			{
				Tastudio.ToggleReadOnly();
			}
		}

		[LuaMethodExample("tastudio.togglerecording( );")]
		[LuaMethod("togglerecording", "toggles tastudio recording mode on/off depending on its current state")]
		public void SetRecording()
		{
			Tastudio.ToggleReadOnly();
		}

		[LuaMethodExample("tastudio.setbranchtext( \"Some text\", 1 );")]
		[LuaMethod("setbranchtext", "adds the given message to the existing branch, or to the branch that will be created next if branch index is not specified")]
		public void SetBranchText(string text, int? index = null)
		{
			if (index != null)
			{
				Tastudio.CurrentTasMovie.GetBranch(index.Value).UserText = text;
			}
			else
			{
				Tastudio.CurrentTasMovie.NewBranchText = text;
			}
		}

		[LuaMethodExample("local sttasget = tastudio.getmarker( 500 );")]
		[LuaMethod("getmarker", "returns the marker text at the given frame, or an empty string if there is no marker for the given frame")]
		public string GetMarker(int frame)
		{
			if (Engaged())
			{
				var marker = Tastudio.CurrentTasMovie.Markers.Get(frame);
				if (marker != null)
				{
					return marker.Message;
				}
			}

			return "";
		}

		[LuaMethodExample("tastudio.removemarker( 500 );")]
		[LuaMethod("removemarker", "if there is a marker for the given frame, it will be removed")]
		public void RemoveMarker(int frame)
		{
			if (Engaged())
			{
				var marker = Tastudio.CurrentTasMovie.Markers.Get(frame);
				if (marker != null)
				{
					Tastudio.CurrentTasMovie.Markers.Remove(marker);
					Tastudio.UpdateValues();
				}
			}
		}

		[LuaMethodExample("tastudio.setmarker( 500, \"Some message\" );")]
		[LuaMethod("setmarker", "Adds or sets a marker at the given frame, with an optional message")]
		public void SetMarker(int frame, string message = null)
		{
			if (Engaged())
			{
				var marker = Tastudio.CurrentTasMovie.Markers.Get(frame);
				if (marker != null)
				{
					marker.Message = message;
				}
				else
				{
					Tastudio.CurrentTasMovie.Markers.Add(frame, message);
					Tastudio.UpdateValues();
				}
			}
		}

		[LuaMethodExample("local botasisl = tastudio.islag( 500 );")]
		[LuaMethod("islag", "Returns whether or not the given frame was a lag frame, null if unknown")]
		public bool? IsLag(int frame)
		{
			if (Engaged())
			{
				if (frame < Tastudio.CurrentTasMovie.InputLogLength)
				{
					return Tastudio.CurrentTasMovie[frame].Lagged;
				}
			}

			return null;
		}

		[LuaMethodExample("tastudio.setlag( 500, true );")]
		[LuaMethod("setlag", "Sets the lag information for the given frame, if the frame does not exist in the lag log, it will be added. If the value is null, the lag information for that frame will be removed")]
		public void SetLag(int frame, bool? value)
		{
			if (Engaged())
			{
				Tastudio.CurrentTasMovie.SetLag(frame, value);
			}
		}

		[LuaMethodExample("if ( tastudio.hasstate( 500 ) ) then\r\n\tconsole.log( \"Returns whether or not the given frame has a savestate associated with it\" );\r\nend;")]
		[LuaMethod("hasstate", "Returns whether or not the given frame has a savestate associated with it")]
		public bool HasState(int frame)
		{
			if (Engaged())
			{
				if (frame < Tastudio.CurrentTasMovie.InputLogLength)
				{
					return Tastudio.CurrentTasMovie[frame].HasState;
				}
			}

			return false;
		}

		[LuaMethodExample("tastudio.setplayback( 1500 );")]
		[LuaMethod("setplayback", "Seeks the given frame (a number) or marker (a string)")]
		public void SetPlayback(object frame)
		{
			if (Engaged())
			{
				int f;
				if (frame is double)
				{
					f = (int)(double)frame;
				}
				else
				{
					f = Tastudio.CurrentTasMovie.Markers.FindIndex((string)frame);
					if (f == -1)
					{
						return;
					}

					f = Tastudio.CurrentTasMovie.Markers[f].Frame;
				}

				if (f < Tastudio.CurrentTasMovie.InputLogLength && f >= 0)
				{
					Tastudio.GoToFrame(f, true);
				}
			}
		}

		[LuaMethodExample("tastudio.onqueryitembg( function( currentindex, itemname )\r\n\tconsole.log( \"called during the background draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)\" );\r\nend );")]
		[LuaMethod("onqueryitembg", "called during the background draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)")]
		public void OnQueryItemBg(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.QueryItemBgColorCallback = (int index, string name) =>
				{
					var result = luaf.Call(index, name);

					if (result != null)
					{
						var color = ToColor(result[0]);
						return color;
					}

					return null;
				};
			}
		}

		[LuaMethodExample("tastudio.onqueryitemtext( function( currentindex, itemname )\r\n\tconsole.log( \"called during the text draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)\" );\r\nend );")]
		[LuaMethod("onqueryitemtext", "called during the text draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)")]
		public void OnQueryItemText(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.QueryItemTextCallback = (int index, string name) =>
				{
					var result = luaf.Call(index, name);

					return result?[0]?.ToString();
				};
			}
		}

		[LuaMethodExample("tastudio.onqueryitemicon( function( currentindex, itemname )\r\n\tconsole.log( \"called during the icon draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)\" );\r\nend );")]
		[LuaMethod("onqueryitemicon", "called during the icon draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)")]
		public void OnQueryItemIcon(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.QueryItemIconCallback = (int index, string name) =>
				{
					var result = luaf.Call(index, name);
					if (result?[0] != null)
					{
						string path = result[0].ToString();
						Icon icon = new Icon(path);
						return icon.ToBitmap();
					}

					return (Bitmap)null;
				};
			}
		}

		[LuaMethodExample("tastudio.ongreenzoneinvalidated( function( currentindex, itemname )\r\n\tconsole.log( \"called whenever the greenzone is invalidated and returns the first frame that was invalidated\" );\r\nend );")]
		[LuaMethod("ongreenzoneinvalidated", "called whenever the greenzone is invalidated and returns the first frame that was invalidated")]
		public void OnGreenzoneInvalidated(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.GreenzoneInvalidatedCallback = (int index) =>
				{
					luaf.Call(index);
				};
			}
		}

		[LuaMethodExample("tastudio.ongreenzoneinvalidated( function( currentindex, itemname )\r\n\tconsole.log( \"called whenever the greenzone is invalidated and returns the first frame that was invalidated\" );\r\nend );")]
		[LuaMethod("onbranchload", "called whenever a branch is loaded. luaf must be a function that takes the integer branch index as a parameter")]
		public void OnBranchLoad(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.BranchLoadedCallback = (int index) =>
				{
					luaf.Call(index);
				};
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("onbranchsave", "called whenever a branch is created or updated. luaf must be a function that takes the integer branch index as a parameter")]
		public void OnBranchSave(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.BranchSavedCallback = (int index) =>
				{
					luaf.Call(index);
				};
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("onbranchremove", "called whenever a branch is removed. luaf must be a function that takes the integer branch index as a parameter")]
		public void OnBranchRemove(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.BranchRemovedCallback = (int index) =>
				{
					luaf.Call(index);
				};
			}
		}

		[LuaMethodExample("local nltasget = tastudio.getselection( );")]
		[LuaMethod("getselection", "gets the currently selected frames")]
		public LuaTable GetSelection()
		{
			LuaTable table = Lua.NewTable();

			if (Engaged())
			{
				var selection = Tastudio.GetSelection().ToList();

				for (int i = 0; i < selection.Count; i++)
				{
					table[i] = selection[i];
				}
			}

			return table;
		}

		public class TastudioBranchInfo
		{
			public string Id { get; set; }
			public int Frame { get; set; }
			public string Text { get; set; }
		}

		[LuaMethodExample("local nltasget = tastudio.getbranches( );")]
		[LuaMethod("getbranches", "Returns a list of the current tastudio branches.  Each entry will have the Id, Frame, and Text properties of the branch")]
		public LuaTable GetBranches()
		{
			var table = Lua.NewTable();

			if (Engaged())
			{
				var branches = Tastudio.CurrentTasMovie.Branches.Select(b => new
				{
					Id = b.UniqueIdentifier.ToString(),
					Frame = b.Frame,
					Text = b.UserText
				})
				.ToList();

				for (int i = 0; i < branches.Count; i++)
				{
					table[i] = branches[i];
				}
			}

			return table;
		}


		[LuaMethodExample("local nltasget = tastudio.getbranchinput( \"97021544-2454-4483-824f-47f75e7fcb6a\", 500 );")]
		[LuaMethod("getbranchinput", "Gets the controller state of the given frame with the given branch identifier")]
		public LuaTable GetBranchInput(string branchId, int frame)
		{
			var table = Lua.NewTable();

			if (Engaged())
			{
				if (Tastudio.CurrentTasMovie.Branches.Any(b => b.UniqueIdentifier.ToString() == branchId))
				{
					var branch = Tastudio.CurrentTasMovie.Branches.First(b => b.UniqueIdentifier.ToString() == branchId);
					if (frame < branch.InputLog.Count)
					{
						var input = branch.InputLog[frame];

						var adapter = new Bk2ControllerAdapter
						{
							Definition = Global.MovieSession.MovieControllerAdapter.Definition
						};

						adapter.SetControllersAsMnemonic(input);

						foreach (var button in adapter.Definition.BoolButtons)
						{
							table[button] = adapter.IsPressed(button);
						}

						foreach (var button in adapter.Definition.FloatControls)
						{
							table[button] = adapter.GetFloat(button);
						}
					}
				}
			}

			return table;
		}

		[LuaMethodExample("")]
		[LuaMethod("submitinputchange", "")]
		public void SubmitInputChange(int frame, string button, bool value)
		{
			if (Engaged())
			{
				if (frame >= 0)
				{
					PendingChanges newChange = new PendingChanges();

					if (frame < Tastudio.CurrentTasMovie.InputLogLength)
					{
						if (Tastudio.CurrentTasMovie.BoolIsPressed(frame, button) != value) //Check if the button state is not already in the state the user set in the lua script
						{
							newChange.type = LuaChangeTypes.InputChange;
							newChange.inputType = InputChangeTypes.Bool;
							newChange.frame = frame;
							newChange.button = button;
							newChange.valueBool = value;

							changeList.Add(newChange);
						}
						else
						{
							//Nothing to do here
						}
					}
					else
					{
						newChange.type = LuaChangeTypes.InputChange;
						newChange.inputType = InputChangeTypes.Bool;
						newChange.frame = frame;
						newChange.button = button;
						newChange.valueBool = value;

						changeList.Add(newChange);
					}
				}
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("submitanalogchange", "")]
		public void SubmitAnalogChange(int frame, string button, float value)
		{
			if (Engaged())
			{
				if (frame >= 0)
				{
					PendingChanges newChange = new PendingChanges();

					if (frame < Tastudio.CurrentTasMovie.InputLogLength)
					{
						if (Tastudio.CurrentTasMovie.GetFloatState(frame, button) != value) //Check if the button state is not already in the state the user set in the lua script
						{
							newChange.type = LuaChangeTypes.InputChange;
							newChange.inputType = InputChangeTypes.Float;
							newChange.frame = frame;
							newChange.button = button;
							newChange.valueFloat = value;

							changeList.Add(newChange);
						}
						else
						{
							//Nothing to do here
						}
					}
					else
					{
						newChange.type = LuaChangeTypes.InputChange;
						newChange.inputType = InputChangeTypes.Float;
						newChange.frame = frame;
						newChange.button = button;
						newChange.valueFloat = value;

						changeList.Add(newChange);
					}
				}
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("submitinsertframes", "")]
		public void SubmitInsertFrames(int frame, int number)
		{
			if (Engaged())
			{
				if (frame >= 0 && frame < Tastudio.CurrentTasMovie.InputLogLength && number > 0)
				{
					PendingChanges newChange = new PendingChanges();

					newChange.type = LuaChangeTypes.InsertFrames;
					newChange.frame = frame;
					newChange.number = number;

					changeList.Add(newChange);
				}
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("submitdeleteframes", "")]
		public void SubmitDeleteFrames(int frame, int number)
		{
			if (Engaged())
			{
				if (frame >= 0 && frame < Tastudio.CurrentTasMovie.InputLogLength && number > 0)
				{
					PendingChanges newChange = new PendingChanges();

					newChange.type = LuaChangeTypes.DeleteFrames;
					newChange.frame = frame;
					newChange.number = number;

					changeList.Add(newChange);
				}
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("applyinputchanges", "")]
		public void ApplyInputChanges()
		{
			if (Engaged())
			{
				if (changeList.Count > 0)
				{
					int size = changeList.Count;

					for (int i = 0; i < size; i++)
					{
						switch (changeList[i].type)
						{
							case LuaChangeTypes.InputChange:
								switch (changeList[i].inputType)
								{
									case InputChangeTypes.Bool:
										Tastudio.CurrentTasMovie.SetBoolState(changeList[i].frame, changeList[i].button, changeList[i].valueBool);
										break;
									case InputChangeTypes.Float:
										Tastudio.CurrentTasMovie.SetFloatState(changeList[i].frame, changeList[i].button, changeList[i].valueFloat);
										break;
								}
								break;
							case LuaChangeTypes.InsertFrames:
								Tastudio.InsertNumFrames(changeList[i].frame, changeList[i].number);
								break;
							case LuaChangeTypes.DeleteFrames:
								Tastudio.DeleteFrames(changeList[i].frame, changeList[i].number);
								break;
						}
					}
					changeList.Clear();
					Tastudio.JumpToGreenzone();
					Tastudio.DoAutoRestore();
				}


			}
		}

		[LuaMethodExample("")]
		[LuaMethod("clearinputchanges", "")]
		public void ClearInputChanges()
		{
			if (Engaged())
				changeList.Clear();
		}

		[LuaMethod("addcolumn", "")]
		public void AddColumn(string name, string text, int width)
		{
			if (Engaged())
			{
				Tastudio.AddColumn(name, text, width, ColumnType.Text);
			}
		}
	}
}
