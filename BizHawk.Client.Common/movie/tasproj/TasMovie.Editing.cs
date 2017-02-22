using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public partial class TasMovie
	{
		public TasMovieChangeLog ChangeLog;

		public override void RecordFrame(int frame, IController source)
		{
			if (frame != 0)
				ChangeLog.AddGeneralUndo(frame -1, frame -1, "Record Frame: " + frame);

			base.RecordFrame(frame, source);

			LagLog.RemoveFrom(frame);
			LagLog[frame] = Global.Emulator.AsInputPollable().IsLagFrame;

			if (IsRecording)
				StateManager.Invalidate(frame + 1);

			if (frame != 0)
				ChangeLog.SetGeneralRedo();
		}

		public override void Truncate(int frame)
		{
			bool endBatch = ChangeLog.BeginNewBatch("Truncate Movie: " + frame, true);
			ChangeLog.AddGeneralUndo(frame, InputLogLength - 1);

			if (frame < _log.Count - 1)
			{
				Changes = true;
			}

			base.Truncate(frame);

			LagLog.RemoveFrom(frame);
			StateManager.Invalidate(frame);
			Markers.TruncateAt(frame);

			ChangeLog.SetGeneralRedo();
			if (endBatch)
				ChangeLog.EndBatch();
		}

		public override void PokeFrame(int frame, IController source)
		{
			ChangeLog.AddGeneralUndo(frame, frame, "Set Frame At: " + frame);

			base.PokeFrame(frame, source);
			InvalidateAfter(frame);

			ChangeLog.SetGeneralRedo();
		}

		public void SetFrame(int frame, string source)
		{
			ChangeLog.AddGeneralUndo(frame, frame, "Set Frame At: " + frame);

			base.SetFrameAt(frame, source);
			InvalidateAfter(frame);

			ChangeLog.SetGeneralRedo();
		}

		public override void ClearFrame(int frame)
		{
			ChangeLog.AddGeneralUndo(frame, frame, "Clear Frame: " + frame);

			base.ClearFrame(frame);
			InvalidateAfter(frame);

			ChangeLog.SetGeneralRedo();
		}

		public void RemoveFrame(int frame)
		{
			bool endBatch = ChangeLog.BeginNewBatch("Remove Frame: " + frame, true);
			ChangeLog.AddGeneralUndo(frame, InputLogLength - 1);

			_log.RemoveAt(frame);
			if (BindMarkersToInput)
			{
				bool wasRecording = ChangeLog.IsRecording;
				ChangeLog.IsRecording = false;
				int firstIndex = Markers.FindIndex(m => m.Frame >= frame);
				if (firstIndex != -1)
				{
					for (int i = firstIndex; i < Markers.Count; i++)
					{
						TasMovieMarker m = Markers.ElementAt(i);
						if (m.Frame == frame)
							Markers.Remove(m);
						else
							Markers.Move(m.Frame, m.Frame - 1);
					}
				}
				ChangeLog.IsRecording = wasRecording;
			}

			Changes = true;
			InvalidateAfter(frame);

			ChangeLog.SetGeneralRedo();
			if (endBatch)
				ChangeLog.EndBatch();
		}

		public void RemoveFrames(int[] frames)
		{
			if (frames.Any())
			{
				var invalidateAfter = frames.Min();

				bool endBatch = ChangeLog.BeginNewBatch("Remove Multiple Frames", true);
				ChangeLog.AddGeneralUndo(invalidateAfter, InputLogLength - 1);

				foreach (var frame in frames.OrderByDescending(x => x)) // Removin them in reverse order allows us to remove by index;
				{
					if (frame < _log.Count)
						_log.RemoveAt(frame);
					if (BindMarkersToInput) // TODO: This is slow, is there a better way to do it?
					{
						bool wasRecording = ChangeLog.IsRecording;
						ChangeLog.IsRecording = false;
						int firstIndex = Markers.FindIndex(m => m.Frame >= frame);
						if (firstIndex != -1)
						{
							for (int i = firstIndex; i < Markers.Count; i++)
							{
								TasMovieMarker m = Markers.ElementAt(i);
								if (m.Frame == frame)
									Markers.Remove(m);
								else
									Markers.Move(m.Frame, m.Frame - 1);
							}
						}
						ChangeLog.IsRecording = wasRecording;
					}
				}

				Changes = true;
				InvalidateAfter(invalidateAfter);

				ChangeLog.SetGeneralRedo();
				if (endBatch)
					ChangeLog.EndBatch();
			}
		}

		public void RemoveFrames(int removeStart, int removeUpTo, bool fromHistory = false)
		{
			bool endBatch = ChangeLog.BeginNewBatch("Remove Frames: " + removeStart + "-" + removeUpTo, true);
			ChangeLog.AddGeneralUndo(removeStart, InputLogLength - 1);

			for (int i = removeUpTo - 1; i >= removeStart; i--)
				_log.RemoveAt(i);

			if (BindMarkersToInput)
			{
				bool wasRecording = ChangeLog.IsRecording;
				ChangeLog.IsRecording = false;
				int firstIndex = Markers.FindIndex(m => m.Frame >= removeStart);
				if (firstIndex != -1)
				{
					for (int i = firstIndex; i < Markers.Count; i++)
					{
						TasMovieMarker m = Markers.ElementAt(i);
						if (m.Frame < removeUpTo)
							Markers.Remove(m);
						else
							Markers.Move(m.Frame, m.Frame - (removeUpTo - removeStart), fromHistory);
					}
				}
				ChangeLog.IsRecording = wasRecording;
			}

			Changes = true;
			InvalidateAfter(removeStart);

			ChangeLog.SetGeneralRedo();
			if (endBatch)
				ChangeLog.EndBatch();
		}

		public void InsertInput(int frame, string inputState)
		{
			bool endBatch = ChangeLog.BeginNewBatch("Insert Frame: " + frame, true);
			ChangeLog.AddGeneralUndo(frame, InputLogLength);

			_log.Insert(frame, inputState);
			Changes = true;
			InvalidateAfter(frame);

			if (BindMarkersToInput)
			{
				bool wasRecording = ChangeLog.IsRecording;
				ChangeLog.IsRecording = false;
				int firstIndex = Markers.FindIndex(m => m.Frame >= frame);
				if (firstIndex != -1)
				{
					for (int i = firstIndex; i < Markers.Count; i++)
					{
						TasMovieMarker m = Markers.ElementAt(i);
						Markers.Move(m.Frame, m.Frame + 1);
					}
				}
				ChangeLog.IsRecording = wasRecording;
			}

			ChangeLog.SetGeneralRedo();
			if (endBatch)
				ChangeLog.EndBatch();
		}

		public void InsertInput(int frame, IEnumerable<string> inputLog)
		{
			bool endBatch = ChangeLog.BeginNewBatch("Insert Frame: " + frame, true);
			ChangeLog.AddGeneralUndo(frame, InputLogLength + inputLog.Count() - 1);

			_log.InsertRange(frame, inputLog);
			Changes = true;
			InvalidateAfter(frame);

			if (BindMarkersToInput)
			{
				bool wasRecording = ChangeLog.IsRecording;
				ChangeLog.IsRecording = false;
				int firstIndex = Markers.FindIndex(m => m.Frame >= frame);
				if (firstIndex != -1)
				{
					for (int i = firstIndex; i < Markers.Count; i++)
					{
						TasMovieMarker m = Markers.ElementAt(i);
						Markers.Move(m.Frame, m.Frame + inputLog.Count());
					}
				}
				ChangeLog.IsRecording = wasRecording;
			}

			ChangeLog.SetGeneralRedo();
			if (endBatch)
				ChangeLog.EndBatch();
		}

		public void InsertInput(int frame, IEnumerable<IController> inputStates)
		{
			// ChangeLog is done in the InsertInput call.
			var lg = LogGeneratorInstance();

			var inputLog = new List<string>();

			foreach (var input in inputStates)
			{
				lg.SetSource(input);
				inputLog.Add(lg.GenerateLogEntry());
			}

			InsertInput(frame, inputLog); // Sets the ChangeLog
		}

		public void CopyOverInput(int frame, IEnumerable<IController> inputStates)
		{
			ChangeLog.BeginNewBatch("Copy Over Input: " + frame);
			var lg = LogGeneratorInstance();
			var states = inputStates.ToList();

			if (_log.Count < states.Count + frame)
				ExtendMovieForEdit(states.Count + frame - _log.Count);

			ChangeLog.AddGeneralUndo(frame, frame + inputStates.Count() - 1, "Copy Over Input: " + frame);

			for (int i = 0; i < states.Count; i++)
			{
				if (_log.Count <= frame + i)
					break;
				lg.SetSource(states[i]);
				_log[frame + i] = lg.GenerateLogEntry();
			}

			ChangeLog.EndBatch();
			Changes = true;
			InvalidateAfter(frame);

			ChangeLog.SetGeneralRedo();
		}

		public void InsertEmptyFrame(int frame, int count = 1, bool fromHistory = false)
		{
			bool endBatch = ChangeLog.BeginNewBatch("Insert Empty Frame: " + frame, true);
			ChangeLog.AddGeneralUndo(frame, InputLogLength + count - 1);

			var lg = LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerInstance());

			if (frame > _log.Count())
				frame = _log.Count();

			for (int i = 0; i < count; i++)
				_log.Insert(frame, lg.EmptyEntry);

			if (BindMarkersToInput)
			{
				bool wasRecording = ChangeLog.IsRecording;
				ChangeLog.IsRecording = false;
				int firstIndex = Markers.FindIndex(m => m.Frame >= frame);
				if (firstIndex != -1)
				{
					for (int i = firstIndex; i < Markers.Count; i++)
					{
						TasMovieMarker m = Markers.ElementAt(i);
						Markers.Move(m.Frame, m.Frame + count, fromHistory);
					}
				}
				ChangeLog.IsRecording = wasRecording;
			}


			Changes = true;
			InvalidateAfter(frame - 1);

			ChangeLog.SetGeneralRedo();
			if (endBatch)
				ChangeLog.EndBatch();

			if (Global.Emulator.Frame < _log.Count) // Don't stay in recording mode? Fixes TAStudio recording after paint inserting.
				this.SwitchToPlay();
		}

		private void ExtendMovieForEdit(int numFrames)
		{
			bool endBatch = ChangeLog.BeginNewBatch("Auto-Extend Movie", true);
			int oldLength = InputLogLength;
			ChangeLog.AddGeneralUndo(oldLength, oldLength + numFrames - 1);

			var lg = LogGeneratorInstance();
            lg.SetSource(Global.MovieOutputHardpoint); // account for autohold. needs autohold pattern to be already recorded in the current frame

			for (int i = 0; i < numFrames; i++)
                _log.Add(lg.GenerateLogEntry());

			Changes = true;

			ChangeLog.SetGeneralRedo();
			if (endBatch)
				ChangeLog.EndBatch();

			if (Global.Emulator.Frame < _log.Count) // Don't stay in recording mode? Fixes TAStudio recording after paint inserting.
				this.SwitchToPlay();
		}

		public void ToggleBoolState(int frame, string buttonName)
		{
			if (frame >= _log.Count) // Insert blank frames up to this point
				ExtendMovieForEdit(frame - _log.Count + 1);

			var adapter = GetInputState(frame) as Bk2ControllerAdapter;
			adapter[buttonName] = !adapter.IsPressed(buttonName);

			var lg = LogGeneratorInstance();
			lg.SetSource(adapter);
			_log[frame] = lg.GenerateLogEntry();
			Changes = true;
			InvalidateAfter(frame);

			ChangeLog.AddBoolToggle(frame, buttonName, !adapter.IsPressed(buttonName), "Toggle " + buttonName + ": " + frame);
		}

		public void SetBoolState(int frame, string buttonName, bool val)
		{
			if (frame >= _log.Count) // Insert blank frames up to this point
				ExtendMovieForEdit(frame - _log.Count + 1);

			var adapter = GetInputState(frame) as Bk2ControllerAdapter;
			var old = adapter.IsPressed(buttonName);
			adapter[buttonName] = val;

			var lg = LogGeneratorInstance();
			lg.SetSource(adapter);
			_log[frame] = lg.GenerateLogEntry();

			if (old != val)
			{
				InvalidateAfter(frame);
				Changes = true;
				ChangeLog.AddBoolToggle(frame, buttonName, old, "Set " + buttonName + "(" + (val ? "On" : "Off") + "): " + frame);
			}
		}

		public void SetBoolStates(int frame, int count, string buttonName, bool val)
		{
			if (frame + count >= _log.Count) // Insert blank frames up to this point
				ExtendMovieForEdit(frame - _log.Count + 1);

			ChangeLog.AddGeneralUndo(frame, frame + count - 1, "Set " + buttonName + "(" + (val ? "On" : "Off") + "): " + frame + "-" + (frame + count - 1));

			int changed = -1;
			for (int i = 0; i < count; i++)
			{
				var adapter = GetInputState(frame + i) as Bk2ControllerAdapter;
				bool old = adapter.IsPressed(buttonName);
				adapter[buttonName] = val;

				var lg = LogGeneratorInstance();
				lg.SetSource(adapter);
				_log[frame + i] = lg.GenerateLogEntry();

				if (changed == -1 && old != val)
					changed = frame + i;
			}

			if (changed != -1)
			{
				InvalidateAfter(changed);
				Changes = true;
			}

			ChangeLog.SetGeneralRedo();
		}

		public void SetFloatState(int frame, string buttonName, float val)
		{
			if (frame >= _log.Count) // Insert blank frames up to this point
				ExtendMovieForEdit(frame - _log.Count + 1);

			var adapter = GetInputState(frame) as Bk2ControllerAdapter;
			var old = adapter.GetFloat(buttonName);
			adapter.SetFloat(buttonName, val);

			var lg = LogGeneratorInstance();
			lg.SetSource(adapter);
			_log[frame] = lg.GenerateLogEntry();

			if (old != val)
			{
				InvalidateAfter(frame);
				Changes = true;
				ChangeLog.AddFloatChange(frame, buttonName, old, val, "Set " + buttonName + "(" + val + "): " + frame);
			}
		}

		public void SetFloatStates(int frame, int count, string buttonName, float val)
		{
			if (frame + count >= _log.Count) // Insert blank frames up to this point
				ExtendMovieForEdit(frame - _log.Count + 1);

			ChangeLog.AddGeneralUndo(frame, frame + count - 1, "Set " + buttonName + "(" + val + "): " + frame + "-" + (frame + count - 1));

			int changed = -1;
			for (int i = 0; i < count; i++)
			{
				var adapter = GetInputState(frame + i) as Bk2ControllerAdapter;
				float old = adapter.GetFloat(buttonName);
				adapter.SetFloat(buttonName, val);

				var lg = LogGeneratorInstance();
				lg.SetSource(adapter);
				_log[frame + i] = lg.GenerateLogEntry();

				if (changed == -1 && old != val)
					changed = frame + i;
			}

			if (changed != -1)
			{
				InvalidateAfter(changed);
				Changes = true;
			}

			ChangeLog.SetGeneralRedo();
		}

		#region "LagLog"
		public void RemoveLagHistory(int frame)
		{
			LagLog.RemoveHistoryAt(frame);
		}
		public void InsertLagHistory(int frame, bool isLag)
		{
			LagLog.InsertHistoryAt(frame, isLag);
		}

		public void SetLag(int frame, bool? value)
		{
			LagLog[frame] = value;
		}
		#endregion
	}
}
