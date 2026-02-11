using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

using NLua;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

// ReSharper disable UnusedMember.Global
// ReSharper disable StringLiteralTypo
namespace BizHawk.Client.EmuHawk
{
	[Description("A library for manipulating the Tastudio dialog of the EmuHawk client")]
	[LuaLibrary(released: true)]
	public sealed class TAStudioLuaLibrary : LuaLibraryBase
	{
		private const string DESC_LINE_EDIT_QUEUE_APPLY = " Edits will take effect once you call {{tastudio.applyinputchanges}}.";

		private const string DESC_LINE_EDIT_QUEUE_GLOBAL = " (For technical reasons, the queue is shared between all loaded scripts.)";

		private const string DESC_LINE_BRANCH_CHANGE_CB
			= " Your callback can have 1 parameter, which will be the index of the branch."
			+ " Calling this function a second time will replace the existing callback with the new one.";

		private static readonly IDictionary<string, Icon> _iconCache = new Dictionary<string, Icon>();

		public ToolManager Tools { get; set; }

		public TAStudioLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "tastudio";

		private TAStudio Tastudio => Tools.TAStudio;

		private struct PendingChanges
		{
			public LuaChangeTypes Type;
			public InputChangeTypes InputType;
			public int Frame;
			public int Number;
			public string Button;
			public bool ValueBool;
			public int ValueAxis;
		}

		public enum LuaChangeTypes
		{
			InputChange,
			InsertFrames,
			DeleteFrames,
			ClearFrames,
		}

		private enum InputChangeTypes
		{
			Bool,
			Axis,
		}

		public class TastudioBranchInfo
		{
			public string Id { get; set; }
			public int Frame { get; set; }
			public string Text { get; set; }
		}

		private readonly List<PendingChanges> _changeList = new List<PendingChanges>(); //TODO: Initialize it to empty list on a script reload, and have each script have it's own list

		[LuaMethodExample("if ( tastudio.engaged( ) ) then\r\n\tconsole.log( \"returns whether or not tastudio is currently engaged ( active )\" );\r\nend;")]
		[LuaMethod("engaged", "returns whether or not tastudio is currently engaged (active)")]
		public bool Engaged()
		{
			return Tools.Has<TAStudio>(); // TODO: eventually tastudio should have an engaged flag
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
				Tastudio.CurrentTasMovie.LagLog[frame] = value;
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
				if (_luaLibsImpl.IsInInputOrMemoryCallback)
				{
					throw new InvalidOperationException("tastudio.setplayback() is not allowed during input/memory callbacks");
				}


				int f;
				if (frame is long frameNumber)
				{
					f = (int)frameNumber;
				}
				else if (frame is double frameNumber2)
				{
					f = (int)frameNumber2;
				}
				else
				{
					int markerIndex = Tastudio.CurrentTasMovie.Markers.FindIndex((string)frame);
					if (markerIndex == -1) return;

					f = Tastudio.CurrentTasMovie.Markers[markerIndex].Frame;
				}

				if (f >= 0)
				{
					_luaLibsImpl.IsUpdateSupressed = true;
					Tastudio.GoToFrame(f);
					_luaLibsImpl.IsUpdateSupressed = false;
				}
			}
		}

		[LuaMethodExample("local nltasget = tastudio.getselection( );")]
		[LuaMethod("getselection", "gets the currently selected frames")]
		public LuaTable GetSelection()
			=> Engaged()
				? _th.EnumerateToLuaTable(Tastudio.GetSelection(), indexFrom: 0)
				: _th.CreateTable();

#pragma warning disable MA0136 // multi-line string literals (passed to `[LuaMethodExample]`, which converts to host newlines)
		[LuaMethodExample("""
			tastudio.submitinputchange(10000, "P1 A", true);
			tastudio.applyinputchanges();
		""")]
		[LuaMethod(
			name: "submitinputchange",
			description: "Queues a hold/release button operation for the frame specified."
				+ DESC_LINE_EDIT_QUEUE_APPLY
				+ DESC_LINE_EDIT_QUEUE_GLOBAL)]
		public void SubmitInputChange(int frame, string button, bool value)
		{
			if (!Engaged() || frame < 0) return;
			if (frame >= Tastudio.CurrentTasMovie.InputLogLength // past end of movie, obviously not already set
				|| Tastudio.CurrentTasMovie.BoolIsPressed(frame, button) != value) // not already set (though TODO a pending edit could change it)
			{
				_changeList.Add(new()
				{
					Type = LuaChangeTypes.InputChange,
					InputType = InputChangeTypes.Bool,
					Frame = frame,
					Button = button,
					ValueBool = value,
				});
			}
		}

		[LuaMethodExample("""
			tastudio.submitanalogchange(10000, "P1 Stick X", 127);
			tastudio.applyinputchanges();
		""")]
		[LuaMethod(
			name: "submitanalogchange",
			description: "Queues a change axis value operation for the frame specified."
				+ DESC_LINE_EDIT_QUEUE_APPLY
				+ DESC_LINE_EDIT_QUEUE_GLOBAL)]
		public void SubmitAnalogChange(int frame, string button, float value)
		{
			if (!Engaged() || frame < 0) return;
			var value1 = (int) value; //TODO change param type to int
			if (frame >= Tastudio.CurrentTasMovie.InputLogLength // past end of movie, obviously not already set
				|| Tastudio.CurrentTasMovie.GetAxisState(frame, button) != value1) // not already set (though TODO a pending edit could change it)
			{
				_changeList.Add(new()
				{
					Type = LuaChangeTypes.InputChange,
					InputType = InputChangeTypes.Axis,
					Frame = frame,
					Button = button,
					ValueAxis = value1,
				});
			}
		}

		[LuaMethodExample("""
			tastudio.submitinsertframes(10000, 5);
			tastudio.applyinputchanges();
		""")]
		[LuaMethod(
			name: "submitinsertframes",
			description: "Queues an insert operation, creating the specified number of frames (rows) immediately before the frame specified."
				+ DESC_LINE_EDIT_QUEUE_APPLY
				+ DESC_LINE_EDIT_QUEUE_GLOBAL)]
		public void SubmitInsertFrames(int frame, int number)
		{
			if (Engaged() && 0.RangeToExclusive(Tastudio.CurrentTasMovie.InputLogLength).Contains(frame) && number > 0)
			{
				_changeList.Add(new PendingChanges
				{
					Type = LuaChangeTypes.InsertFrames,
					Frame = frame,
					Number = number,
				});
			}
		}

		[LuaMethodExample("""
			tastudio.submitdeleteframes(10000, 5);
			tastudio.applyinputchanges();
		""")]
		[LuaMethod(
			name: "submitdeleteframes",
			description: "Queues a delete operation for the specified number of frames, from the frame specified through to {{frame + number - 1}}."
				+ DESC_LINE_EDIT_QUEUE_APPLY
				+ DESC_LINE_EDIT_QUEUE_GLOBAL)]
		public void SubmitDeleteFrames(int frame, int number)
		{
			if (Engaged() && 0.RangeToExclusive(Tastudio.CurrentTasMovie.InputLogLength).Contains(frame) && number > 0)
			{
				_changeList.Add(new PendingChanges
				{
					Type = LuaChangeTypes.DeleteFrames,
					Frame = frame,
					Number = number,
				});
			}
		}

		[LuaMethodExample("""
			tastudio.submitclearframes(10000, 5);
			tastudio.applyinputchanges();
		""")]
		[LuaMethod(
			name: "submitclearframes",
			description: "Queues a clear operation for the specified number of frames, from the frame specified through to {{frame + number - 1}}."
				+ DESC_LINE_EDIT_QUEUE_APPLY
				+ DESC_LINE_EDIT_QUEUE_GLOBAL)]
		public void SubmitClearFrames(int frame, int number)
		{
			if (Engaged() && 0.RangeToExclusive(Tastudio.CurrentTasMovie.InputLogLength).Contains(frame) && number > 0)
			{
				_changeList.Add(new PendingChanges
				{
					Type = LuaChangeTypes.ClearFrames,
					Frame = frame,
					Number = number,
				});
			}
		}

		[LuaMethodExample("""
			tastudio.submitinputchange(10000, "P1 A", true);
			tastudio.applyinputchanges();
		""")]
		[LuaMethod(
			name: "applyinputchanges",
			description: "Applies the queued edit operations to the TAStudio project as a single batch."
				+ DESC_LINE_EDIT_QUEUE_GLOBAL)]
		public void ApplyInputChanges()
		{
			if (_changeList.Count == 0)
			{
				return;
			}

			if (Engaged())
			{
				if (_luaLibsImpl.IsInInputOrMemoryCallback)
				{
					throw new InvalidOperationException("tastudio.applyinputchanges() is not allowed during input/memory callbacks");
				}

				_luaLibsImpl.IsUpdateSupressed = true;

				Tastudio.StopRecordingOnNextEdit = false;
				Tastudio.CurrentTasMovie.SingleInvalidation(() =>
				{
					Tastudio.CurrentTasMovie.ChangeLog.BeginNewBatch("tastudio.applyinputchanges");
					int size = _changeList.Count;

					for (int i = 0; i < size; i++)
					{
						switch (_changeList[i].Type)
						{
							case LuaChangeTypes.InputChange:
								switch (_changeList[i].InputType)
								{
									case InputChangeTypes.Bool:
										Tastudio.CurrentTasMovie.SetBoolState(_changeList[i].Frame, _changeList[i].Button, _changeList[i].ValueBool);
										break;
									case InputChangeTypes.Axis:
										Tastudio.CurrentTasMovie.SetAxisState(_changeList[i].Frame, _changeList[i].Button, _changeList[i].ValueAxis);
										break;
								}
								break;
							case LuaChangeTypes.InsertFrames:
								Tastudio.CurrentTasMovie.InsertEmptyFrame(_changeList[i].Frame, _changeList[i].Number);
								break;
							case LuaChangeTypes.DeleteFrames:
								int endExclusive = _changeList[i].Frame + _changeList[i].Number;
								endExclusive = Math.Min(Tastudio.CurrentTasMovie.InputLogLength, endExclusive);
								if (_changeList[i].Frame < endExclusive)
								{
									Tastudio.CurrentTasMovie.RemoveFrames(_changeList[i].Frame, endExclusive);
								}
								break;
							case LuaChangeTypes.ClearFrames:
								endExclusive = _changeList[i].Frame + _changeList[i].Number;
								endExclusive = Math.Min(Tastudio.CurrentTasMovie.InputLogLength, endExclusive);
								for (int j = _changeList[i].Frame; j < endExclusive; j++)
								{
									Tastudio.CurrentTasMovie.ClearFrame(j);
								}
								break;
						}
					}
					_changeList.Clear();
					Tastudio.CurrentTasMovie.ChangeLog.EndBatch();
				});

				_luaLibsImpl.IsUpdateSupressed = false;
			}
		}

		[LuaMethodExample("""
			tastudio.submitinputchange(10000, "P1 A", true);
			tastudio.clearinputchanges();
			tastudio.applyinputchanges(); -- does nothing
		""")]
		[LuaMethod(
			name: "clearinputchanges",
			description: "Discards any edits queued for the TAStudio project by scripts."
				+ DESC_LINE_EDIT_QUEUE_GLOBAL)]
		public void ClearInputChanges()
		{
			if (Engaged())
			{
				_changeList.Clear();
			}
		}

		[LuaMethodExample("""
			local cache = { [0] = "0" };
			tastudio.onqueryitemtext(function(frame, col)
				if col == "xp" then return cache[frame]; end
			end);
			tastudio.addcolumn("xp", "Experience", 30);
			--TODO each frame, set `cache[emu.framecount()]` to e.g. `tostring(memory.readbyte(addr_of_xp))`
			--TODO on loadstate, clear `cache` from `emu.framecount()` through to end
			-- or you could try to de/serialise the `cache` table to a string and sync it via `userdata`
		""")]
#pragma warning restore MA0136
		[LuaMethod(
			name: "addcolumn",
			description: "Extends the piano roll with an extra column for data visualisation."
				+ " The text parameter is used as the column header, while the name parameter is used to identify the column for {{onqueryitem*}} callbacks."
				+ " And width is obviously the width (in dp).")]
		public void AddColumn(string name, string text, int width)
		{
			if (Engaged()) Tastudio.AddColumn(name: name, widthUnscaled: width, text: text);
		}

		[LuaMethodExample("tastudio.setbranchtext( \"Some text\", 1 );")]
		[LuaMethod("setbranchtext", "adds the given message to the existing branch, or to the branch that will be created next if branch index is not specified")]
		public void SetBranchText(string text, int? index = null)
		{
			var text1 = text;
			if (index != null)
			{
				var branch = Tastudio.CurrentTasMovie.Branches[index.Value];
				branch?.UserText = text1;
			}
			else
			{
				Tastudio.CurrentTasMovie.Branches.NewBranchText = text1;
			}
		}

		[LuaMethodExample("local nltasget = tastudio.getbranches( );")]
		[LuaMethod("getbranches", "Returns a list of the current tastudio branches.  Each entry will have the Id, Frame, and Text properties of the branch")]
		public LuaTable GetBranches()
		{
			if (!Engaged()) return _th.CreateTable();
			return _th.EnumerateToLuaTable(
				Tastudio.CurrentTasMovie.Branches.Select(b =>
				{
					var table = _th.CreateTable();
					table["Id"] = b.Uuid.ToString("D");
					table["Frame"] = b.Frame;
					table["Text"] = b.UserText;
					return table;
				}),
				indexFrom: 0);
		}

		[LuaMethodExample("""
			tastudio.setbranchtext("New label", tastudio.get_branch_index_by_id(branch_id));
		""")]
		[LuaMethod(
			name: "get_branch_index_by_id",
			description: "Finds the branch with the given UUID (0-indexed). Returns nil if not found.")]
		public int? GetBranchIndexByID(string id)
		{
			if (!Guid.TryParseExact(id, format: "D", out var parsed))
			{
				Log($"not a valid UUID: {id}");
				return null;
			}
			return Tastudio.CurrentTasMovie.Branches.Index()
				.FirstOrNull(tuple => tuple.Item.Uuid == parsed)?.Index;
		}

		[LuaMethodExample("local nltasget = tastudio.getbranchinput( \"97021544-2454-4483-824f-47f75e7fcb6a\", 500 );")]
		[LuaMethod("getbranchinput", "Gets the controller state of the given frame with the given branch identifier")]
		public LuaTable GetBranchInput(string branchId, int frame)
		{
			var table = _th.CreateTable();
			if (!Engaged()) return table;

			var controller = Tastudio.GetBranchInput(branchId, frame);
			if (controller == null) return table;

			foreach (var button in controller.Definition.BoolButtons)
			{
				table[button] = controller.IsPressed(button);
			}

			foreach (var button in controller.Definition.Axes.Keys)
			{
				table[button] = controller.AxisValue(button);
			}

			return table;
		}

		[LuaMethodExample("tastudio.loadbranch(0)")]
		[LuaMethod("loadbranch", "Loads a branch at the given index, if a branch at that index exists.")]
		public void LoadBranch(int index)
		{
			if (Engaged())
			{
				if (_luaLibsImpl.IsInInputOrMemoryCallback)
				{
					throw new InvalidOperationException("tastudio.loadbranch() is not allowed during input/memory callbacks");
				}

				_luaLibsImpl.IsUpdateSupressed = true;

				Tastudio.LoadBranchByIndex(index);

				_luaLibsImpl.IsUpdateSupressed = false;
			}
		}

		[LuaMethodExample("local sttasget = tastudio.getmarker( 500 );")]
		[LuaMethod(
			name: "getmarker",
			description: "Returns the label of the marker on the given frame. This may be an empty string."
				+ " If that frame doesn't have a marker (or TAStudio isn't running), returns nil."
				+ " If branchID is specified, searches the markers in that branch instead.")]
		public string/*?*/ GetMarker(int frame, string/*?*/ branchID = null)
		{
			if (Engaged())
			{
				var marker = MarkerListForBranch(branchID)?.Get(frame);
				if (marker != null)
				{
					return marker.Message;
				}
			}

			return null;
		}

		/// <remarks>assumes a TAStudio project is loaded</remarks>
		private TasMovieMarkerList/*?*/ MarkerListForBranch(string/*?*/ branchID)
			=> Guid.TryParseExact(branchID, format: "D", out var parsed)
				? Tastudio.CurrentTasMovie.Branches.FirstOrDefault(branch => branch.Uuid == parsed)?.Markers
				: branchID is null ? Tastudio.CurrentTasMovie.Markers : null; // not a typo; null `branchID` indicates main log

		[LuaMethodExample("""
			local marker_label = tastudio.getmarker(tastudio.find_marker_on_or_before(100));
		""")]
		[LuaMethod(
			name: "find_marker_on_or_before",
			description: "Returns the frame number of the marker closest to the given frame (including that frame, but not after it)."
				+ " This may be the power-on marker at 0. Returns nil if the arguments are invalid or TAStudio isn't active."
				+ " If branchID is specified, searches the markers in that branch instead.")]
		public int? FindMarkerOnOrBefore(int frame, string/*?*/ branchID = null)
			=> Engaged() && MarkerListForBranch(branchID) is TasMovieMarkerList markers
				? markers.PreviousOrCurrent(frame)?.Frame
				: null;

		[LuaMethodExample("""
			local marker_label = tastudio.getmarker(tastudio.get_frames_with_markers()[2]);
		""")]
		[LuaMethod(
			name: "get_frames_with_markers",
			description: "Returns a list of all the frames which have markers on them."
				+ " If branchID is specified, instead returns the frames which have markers in that branch.")]
		public LuaTable GetFramesWithMarkers(string/*?*/ branchID = null)
			=> Engaged() && MarkerListForBranch(branchID) is TasMovieMarkerList markers
				? _th.EnumerateToLuaTable(markers.Select(static m => m.Frame))
				: _th.CreateTable();

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
				}
			}
		}

		[LuaMethodExample("tastudio.setmarker( 500, \"Some message\" );")]
		[LuaMethod("setmarker", "Adds or sets a marker at the given frame, with an optional message")]
		public void SetMarker(int frame, string message = null)
		{
			if (Engaged())
			{
				var message1 = message;
				var marker = Tastudio.CurrentTasMovie.Markers.Get(frame);
				if (marker != null)
				{
					marker.Message = message1;
				}
				else
				{
					Tastudio.CurrentTasMovie.Markers.Add(frame, message1);
				}
			}
		}

		[LuaMethodExample("tastudio.onqueryitembg( function( currentindex, itemname )\r\n\tconsole.log( \"called during the background draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)\" );\r\nend );")]
		[LuaMethod("onqueryitembg", "called during the background draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)")]
		public void OnQueryItemBg(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.QueryItemBgColorCallback = (index, name) => _th.SafeParseColor(luaf.Call(index, name)?.FirstOrDefault());
			}
		}

		[LuaMethodExample("tastudio.onqueryitemtext( function( currentindex, itemname )\r\n\tconsole.log( \"called during the text draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)\" );\r\nend );")]
		[LuaMethod("onqueryitemtext", "Called during the text draw event of the tastudio listview. {{luaf}} must be a function that takes 2 params: {{(index, column)}}. The first is the integer row index of the listview, and the 2nd is the string column name. The callback should return a string to be displayed.")]
		public void OnQueryItemText(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.QueryItemTextCallback = (index, name) => luaf.Call(index, name)?.FirstOrDefault()?.ToString();
			}
		}

		[LuaMethodExample("tastudio.onqueryitemicon( function( currentindex, itemname )\r\n\tconsole.log( \"called during the icon draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)\" );\r\nend );")]
		[LuaMethod("onqueryitemicon", "Called during the icon draw event of the tastudio listview. {{luaf}} must be a function that takes 2 params: {{(index, column)}}. The first is the integer row index of the listview, and the 2nd is the string column name. The callback should return a string, the path to the {{.ico}} file to be displayed. The file will be cached, so if you change the file on disk, call {{tastudio.clearIconCache()}}.")]
		public void OnQueryItemIcon(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.QueryItemIconCallback = (index, name) =>
				{
					var result = luaf.Call(index, name);
					if (result?.FirstOrDefault() is not null)
					{
						return _iconCache.GetValueOrPutNew1(result[0].ToString()).ToBitmap();
					}

					return null;
				};
			}
		}

		[LuaMethodExample("tastudio.clearIconCache();")]
		[LuaMethod("clearIconCache", "Clears the cache that is built up by using {{tastudio.onqueryitemicon}}, so that changes to the icons on disk can be picked up.")]
		public void ClearIconCache()
		{
			foreach (var icon in _iconCache.Values) icon.Dispose();
			_iconCache.Clear();
		}

		[LuaMethodExample("tastudio.ongreenzoneinvalidated( function( currentindex )\r\n\tconsole.log( \"Called whenever the greenzone is invalidated.\" );\r\nend );")]
		[LuaMethod("ongreenzoneinvalidated", "Called whenever the movie is modified in a way that could invalidate savestates in the movie's state history. Called regardless of whether any states were actually invalidated. Your callback can have 1 parameter, which will be the last frame before the invalidated ones. That is, the first of the modified frames.")]
		public void OnGreenzoneInvalidated(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.GreenzoneInvalidatedCallback = index =>
				{
					luaf.Call(index);
				};
			}
		}

		[LuaMethodExample("tastudio.onbranchload( function( currentindex )\r\n\tconsole.log( \"Called whenever a branch is loaded.\" );\r\nend );")]
		[LuaMethod("onbranchload", "called whenever a branch is loaded. luaf must be a function that takes the integer branch index as a parameter")]
		public void OnBranchLoad(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.BranchLoadedCallback = index =>
				{
					luaf.Call(index);
				};
			}
		}

		[LuaMethodExample("""
			tastudio.onbranchsave(function(branch_index) console.writeline(branch_index); end);
		""")]
		[LuaMethod(
			name: "onbranchsave",
			description: "Sets a callback which fires after any branch is created or updated."
				+ DESC_LINE_BRANCH_CHANGE_CB)]
		public void OnBranchSave(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.BranchSavedCallback = index =>
				{
					luaf.Call(index);
				};
			}
		}

		[LuaMethodExample("""
			tastudio.onbranchremove(function(branch_index) console.writeline(branch_index); end);
		""")]
		[LuaMethod(
			name: "onbranchremove",
			description: "Sets a callback which fires after any branch is removed."
				+ DESC_LINE_BRANCH_CHANGE_CB)]
		public void OnBranchRemove(LuaFunction luaf)
		{
			if (Engaged())
			{
				Tastudio.BranchRemovedCallback = index =>
				{
					luaf.Call(index);
				};
			}
		}
	}
}
