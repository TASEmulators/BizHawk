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

		[LuaMethod("engaged", "returns whether or not tastudio is currently engaged (active)")]
		public bool Engaged()
		{
			return GlobalWin.Tools.Has<TAStudio>(); // TODO: eventually tastudio should have an engaged flag
		}

		[LuaMethod("getrecording", "returns whether or not TAStudio is in recording mode")]
		public bool GetRecording()
		{
			return Tastudio.TasPlaybackBox.RecordingMode;
		}

		[LuaMethod("setrecording", "sets the recording mode on/off depending on the parameter")]
		public void SetRecording(bool val)
		{
			if (Tastudio.TasPlaybackBox.RecordingMode != val)
			{
				Tastudio.ToggleReadOnly();
			}
		}

		[LuaMethod("togglerecording", "toggles tastudio recording mode on/off depending on its current state")]
		public void SetRecording()
		{
			Tastudio.ToggleReadOnly();
		}

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

		[LuaMethod("setlag", "Sets the lag information for the given frame, if the frame does not exist in the lag log, it will be added. If the value is null, the lag information for that frame will be removed")]
		public void SetLag(int frame, bool? value)
		{
			if (Engaged())
			{
				Tastudio.CurrentTasMovie.SetLag(frame, value);
			}
		}

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

		[LuaMethod("insertframes", "inserts the given number of blank frames at the given insertion frame")]
		public void InsertNumFrames(int insertionFrame, int numberOfFrames)
		{
			if (Engaged())
			{
				if (insertionFrame < Tastudio.CurrentTasMovie.InputLogLength)
				{
					Tastudio.InsertNumFrames(insertionFrame, numberOfFrames);
				}
				else
				{
					Log(insertionFrame + " is out of range");
				}
			}
		}

		[LuaMethod("deleteframes", "deletes the given number of blank frames beginning at the given frame")]
		public void DeleteFrames(int beginningFrame, int numberOfFrames)
		{
			if (Engaged())
			{
				if (beginningFrame < Tastudio.CurrentTasMovie.InputLogLength)
				{
					Tastudio.DeleteFrames(beginningFrame, numberOfFrames);
				}
				else
				{
					Log(beginningFrame + " is out of range");
				}
			}
		}

		public class TastudioBranchInfo
		{
			public string Id { get; set; }
			public int Frame { get; set; }
			public string Text { get; set; }
		}

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
	}
}
