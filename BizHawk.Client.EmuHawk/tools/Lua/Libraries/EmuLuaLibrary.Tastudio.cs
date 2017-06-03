using System;
using System.ComponentModel;
using System.Drawing;

using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	[Description("A library for manipulating the Tastudio dialog of the EmuHawk client")]
	[LuaLibraryAttributes(released: true)]
	public sealed class TastudioLuaLibrary : LuaLibraryBase
	{
		public TastudioLuaLibrary(Lua lua)
			: base(lua) { }

		public TastudioLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "tastudio";

		private TAStudio Tastudio => GlobalWin.Tools.Get<TAStudio>() as TAStudio;

		[LuaMethodAttributes("engaged", "returns whether or not tastudio is currently engaged (active)")]
		public bool Engaged()
		{
			return GlobalWin.Tools.Has<TAStudio>(); // TODO: eventually tastudio should have an engaged flag
		}

		[LuaMethodAttributes("getrecording", "returns whether or not TAStudio is in recording mode")]
		public bool GetRecording()
		{
			return Tastudio.TasPlaybackBox.RecordingMode;
		}

		[LuaMethodAttributes("setrecording", "sets the recording mode on/off depending on the parameter")]
		public void SetRecording(bool val)
		{
			if (Tastudio.TasPlaybackBox.RecordingMode != val)
			{
				Tastudio.ToggleReadOnly();
			}
		}

		[LuaMethodAttributes("togglerecording", "toggles tastudio recording mode on/off depending on its current state")]
		public void SetRecording()
		{
			Tastudio.ToggleReadOnly();
		}

		[LuaMethodAttributes("setbranchtext", "adds the given message to the existing branch, or to the branch that will be created next if branch index is not specified")]
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

		[LuaMethodAttributes("getmarker", "returns the marker text at the given frame, or an empty string if there is no marker for the given frame")]
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

		[LuaMethodAttributes("removemarker", "if there is a marker for the given frame, it will be removed")]
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

		[LuaMethodAttributes("setmarker", "Adds or sets a marker at the given frame, with an optional message")]
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

		[LuaMethodAttributes("islag", "Returns whether or not the given frame was a lag frame, null if unknown")]
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

		[LuaMethodAttributes("setlag", "Sets the lag information for the given frame, if the frame does not exist in the lag log, it will be added. If the value is null, the lag information for that frame will be removed")]
		public void SetLag(int frame, bool? value)
		{
			if (Engaged())
			{
				Tastudio.CurrentTasMovie.SetLag(frame, value);
			}
		}

		[LuaMethodAttributes("hasstate", "Returns whether or not the given frame has a savestate associated with it")]
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

		[LuaMethodAttributes("setplayback", "Seeks the given frame (a number) or marker (a string)")]
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

		[LuaMethodAttributes(
			"onqueryitembg", "called during the background draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)")]
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

		[LuaMethodAttributes(
			"onqueryitemtext", "called during the text draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)")]
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

		[LuaMethodAttributes(
			"onqueryitemicon", "called during the icon draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)")]
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

		[LuaMethodAttributes("ongreenzoneinvalidated", "called whenever the greenzone is invalidated and returns the first frame that was invalidated")]
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

		[LuaMethodAttributes("getselection", "gets the currently selected frames")]
		public LuaTable GetSelection()
		{
			LuaTable table = Lua.NewTable();

			if (Engaged())
			{
				var selection = Tastudio.GetSelection();

				foreach (var row in selection)
				{
					table[row] = row;
				}
			}

			return table;
		}

		[LuaMethodAttributes("insertframes", "inserts the given number of blank frames at the given insertion frame")]
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

		[LuaMethodAttributes("deleteframes", "deletes the given number of blank frames beginning at the given frame")]
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
	}
}
