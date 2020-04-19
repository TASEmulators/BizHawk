using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal partial class TasMovie
	{
		public IMovieChangeLog ChangeLog { get; set; }

		public override void RecordFrame(int frame, IController source)
		{
			if (frame != 0)
			{
				ChangeLog.AddGeneralUndo(frame - 1, frame - 1, $"Record Frame: {frame}");
			}

			base.RecordFrame(frame, source);

			TasLagLog.RemoveFrom(frame);
			TasLagLog[frame] = Global.Emulator.AsInputPollable().IsLagFrame;

			if (this.IsRecording())
			{
				TasStateManager.Invalidate(frame + 1);
			}

			if (frame != 0)
			{
				ChangeLog.SetGeneralRedo();
			}
		}

		public override void Truncate(int frame)
		{
			bool endBatch = ChangeLog.BeginNewBatch($"Truncate Movie: {frame}", true);
			ChangeLog.AddGeneralUndo(frame, InputLogLength - 1);

			if (frame < Log.Count - 1)
			{
				Changes = true;
			}

			base.Truncate(frame);

			TasLagLog.RemoveFrom(frame);
			TasStateManager.Invalidate(frame);
			Markers.TruncateAt(frame);

			ChangeLog.SetGeneralRedo();
			if (endBatch)
			{
				ChangeLog.EndBatch();
			}
		}

		public override void PokeFrame(int frame, IController source)
		{
			ChangeLog.AddGeneralUndo(frame, frame, $"Set Frame At: {frame}");

			base.PokeFrame(frame, source);
			InvalidateAfter(frame);

			ChangeLog.SetGeneralRedo();
		}

		public void SetFrame(int frame, string source)
		{
			ChangeLog.AddGeneralUndo(frame, frame, $"Set Frame At: {frame}");

			SetFrameAt(frame, source);
			InvalidateAfter(frame);

			ChangeLog.SetGeneralRedo();
		}

		public override void ClearFrame(int frame)
		{
			ChangeLog.AddGeneralUndo(frame, frame, $"Clear Frame: {frame}");

			base.ClearFrame(frame);
			InvalidateAfter(frame);

			ChangeLog.SetGeneralRedo();
		}

		public void RemoveFrame(int frame)
		{
			bool endBatch = ChangeLog.BeginNewBatch($"Remove Frame: {frame}", true);
			ChangeLog.AddGeneralUndo(frame, InputLogLength - 1);

			Log.RemoveAt(frame);
			if (BindMarkersToInput)
			{
				bool wasRecording = ChangeLog.IsRecording;
				ChangeLog.IsRecording = false;
				int firstIndex = Markers.FindIndex(m => m.Frame >= frame);
				if (firstIndex != -1)
				{
					for (int i = firstIndex; i < Markers.Count; i++)
					{
						var m = Markers[i];
						if (m.Frame == frame)
						{
							Markers.Remove(m);
						}
						else
						{
							Markers.Move(m.Frame, m.Frame - 1);
						}
					}
				}

				ChangeLog.IsRecording = wasRecording;
			}

			Changes = true;
			InvalidateAfter(frame);

			ChangeLog.SetGeneralRedo();
			if (endBatch)
			{
				ChangeLog.EndBatch();
			}
		}

		public void RemoveFrames(ICollection<int> frames)
		{
			if (frames.Any())
			{
				var invalidateAfter = frames.Min();

				bool endBatch = ChangeLog.BeginNewBatch("Remove Multiple Frames", true);
				ChangeLog.AddGeneralUndo(invalidateAfter, InputLogLength - 1);

				foreach (var frame in frames.OrderByDescending(f => f)) // Removing them in reverse order allows us to remove by index;
				{
					if (frame < Log.Count)
					{
						Log.RemoveAt(frame);
					}

					if (BindMarkersToInput) // TODO: This is slow, is there a better way to do it?
					{
						bool wasRecording = ChangeLog.IsRecording;
						ChangeLog.IsRecording = false;
						int firstIndex = Markers.FindIndex(m => m.Frame >= frame);
						if (firstIndex != -1)
						{
							for (int i = firstIndex; i < Markers.Count; i++)
							{
								TasMovieMarker m = Markers[i];
								if (m.Frame == frame)
								{
									Markers.Remove(m);
								}
								else
								{
									Markers.Move(m.Frame, m.Frame - 1);
								}
							}
						}

						ChangeLog.IsRecording = wasRecording;
					}
				}

				Changes = true;
				InvalidateAfter(invalidateAfter);

				ChangeLog.SetGeneralRedo();
				if (endBatch)
				{
					ChangeLog.EndBatch();
				}
			}
		}

		public void RemoveFrames(int removeStart, int removeUpTo, bool fromHistory = false)
		{
			bool endBatch = ChangeLog.BeginNewBatch($"Remove Frames: {removeStart}-{removeUpTo}", true);
			ChangeLog.AddGeneralUndo(removeStart, InputLogLength - 1);

			for (int i = removeUpTo - 1; i >= removeStart; i--)
			{
				Log.RemoveAt(i);
			}

			if (BindMarkersToInput)
			{
				bool wasRecording = ChangeLog.IsRecording;
				ChangeLog.IsRecording = false;
				int firstIndex = Markers.FindIndex(m => m.Frame >= removeStart);
				if (firstIndex != -1)
				{
					for (int i = firstIndex; i < Markers.Count; i++)
					{
						TasMovieMarker m = Markers[i];
						if (m.Frame < removeUpTo)
						{
							Markers.Remove(m);
						}
						else
						{
							Markers.Move(m.Frame, m.Frame - (removeUpTo - removeStart), fromHistory);
						}
					}
				}

				ChangeLog.IsRecording = wasRecording;
			}

			Changes = true;
			InvalidateAfter(removeStart);

			ChangeLog.SetGeneralRedo();
			if (endBatch)
			{
				ChangeLog.EndBatch();
			}
		}

		public void InsertInput(int frame, string inputState)
		{
			bool endBatch = ChangeLog.BeginNewBatch($"Insert Frame: {frame}", true);
			ChangeLog.AddGeneralUndo(frame, InputLogLength);

			Log.Insert(frame, inputState);
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
						TasMovieMarker m = Markers[i];
						Markers.Move(m.Frame, m.Frame + 1);
					}
				}

				ChangeLog.IsRecording = wasRecording;
			}

			ChangeLog.SetGeneralRedo();
			if (endBatch)
			{
				ChangeLog.EndBatch();
			}
		}

		public void InsertInput(int frame, IEnumerable<string> inputLog)
		{
			bool endBatch = ChangeLog.BeginNewBatch($"Insert Frame: {frame}", true);
			ChangeLog.AddGeneralUndo(frame, InputLogLength + inputLog.Count() - 1);

			Log.InsertRange(frame, inputLog);
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
						TasMovieMarker m = Markers[i];
						Markers.Move(m.Frame, m.Frame + inputLog.Count());
					}
				}

				ChangeLog.IsRecording = wasRecording;
			}

			ChangeLog.SetGeneralRedo();
			if (endBatch)
			{
				ChangeLog.EndBatch();
			}
		}

		public void InsertInput(int frame, IEnumerable<IController> inputStates)
		{
			// ChangeLog is done in the InsertInput call.
			var inputLog = new List<string>();

			foreach (var input in inputStates)
			{
				var lg = LogGeneratorInstance(input);
				inputLog.Add(lg.GenerateLogEntry());
			}

			InsertInput(frame, inputLog); // Sets the ChangeLog
		}

		public int CopyOverInput(int frame, IEnumerable<IController> inputStates)
		{
			int firstChangedFrame = -1;
			ChangeLog.BeginNewBatch($"Copy Over Input: {frame}");
			
			var states = inputStates.ToList();

			if (Log.Count < states.Count + frame)
			{
				ExtendMovieForEdit(states.Count + frame - Log.Count);
			}

			ChangeLog.AddGeneralUndo(frame, frame + states.Count - 1, $"Copy Over Input: {frame}");

			for (int i = 0; i < states.Count; i++)
			{
				if (Log.Count <= frame + i)
				{
					break;
				}

				var lg = LogGeneratorInstance(states[i]);
				var entry = lg.GenerateLogEntry();
				if (firstChangedFrame == -1 && Log[frame + i] != entry)
				{
					firstChangedFrame = frame + i;
				}

				Log[frame + i] = entry;
			}

			ChangeLog.EndBatch();
			Changes = true;
			InvalidateAfter(frame);

			ChangeLog.SetGeneralRedo();
			return firstChangedFrame;
		}

		public void InsertEmptyFrame(int frame, int count = 1, bool fromHistory = false)
		{
			bool endBatch = ChangeLog.BeginNewBatch($"Insert Empty Frame: {frame}", true);
			ChangeLog.AddGeneralUndo(frame, InputLogLength + count - 1);

			var lg = LogGeneratorInstance(Global.MovieSession.MovieController);

			if (frame > Log.Count())
			{
				frame = Log.Count();
			}

			for (int i = 0; i < count; i++)
			{
				Log.Insert(frame, lg.EmptyEntry);
			}

			if (BindMarkersToInput)
			{
				bool wasRecording = ChangeLog.IsRecording;
				ChangeLog.IsRecording = false;
				int firstIndex = Markers.FindIndex(m => m.Frame >= frame);
				if (firstIndex != -1)
				{
					for (int i = firstIndex; i < Markers.Count; i++)
					{
						TasMovieMarker m = Markers[i];
						Markers.Move(m.Frame, m.Frame + count, fromHistory);
					}
				}

				ChangeLog.IsRecording = wasRecording;
			}

			Changes = true;
			InvalidateAfter(frame);

			ChangeLog.SetGeneralRedo();
			if (endBatch)
			{
				ChangeLog.EndBatch();
			}
		}

		private void ExtendMovieForEdit(int numFrames)
		{
			bool endBatch = ChangeLog.BeginNewBatch("Auto-Extend Movie", true);
			int oldLength = InputLogLength;
			ChangeLog.AddGeneralUndo(oldLength, oldLength + numFrames - 1);

			Global.MovieSession.MovieController.SetFromSticky(Global.InputManager.AutofireStickyXorAdapter);

			// account for autohold. needs autohold pattern to be already recorded in the current frame
			var lg = LogGeneratorInstance(Global.InputManager.MovieOutputHardpoint);

			for (int i = 0; i < numFrames; i++)
			{
				Log.Add(lg.GenerateLogEntry());
			}

			Changes = true;

			ChangeLog.SetGeneralRedo();
			if (endBatch)
			{
				ChangeLog.EndBatch();
			}
		}

		public void ToggleBoolState(int frame, string buttonName)
		{
			if (frame >= Log.Count) // Insert blank frames up to this point
			{
				ExtendMovieForEdit(frame - Log.Count + 1);
			}

			var adapter = GetInputState(frame);
			adapter.SetBool(buttonName, !adapter.IsPressed(buttonName));

			var lg = LogGeneratorInstance(adapter);
			Log[frame] = lg.GenerateLogEntry();
			Changes = true;
			InvalidateAfter(frame);

			ChangeLog.AddBoolToggle(frame, buttonName, !adapter.IsPressed(buttonName), $"Toggle {buttonName}: {frame}");
		}

		public void SetBoolState(int frame, string buttonName, bool val)
		{
			if (frame >= Log.Count) // Insert blank frames up to this point
			{
				ExtendMovieForEdit(frame - Log.Count + 1);
			}

			var adapter = GetInputState(frame);
			var old = adapter.IsPressed(buttonName);
			adapter.SetBool(buttonName, val);

			var lg = LogGeneratorInstance(adapter);
			Log[frame] = lg.GenerateLogEntry();

			if (old != val)
			{
				InvalidateAfter(frame);
				Changes = true;
				ChangeLog.AddBoolToggle(frame, buttonName, old, $"Set {buttonName}({(val ? "On" : "Off")}): {frame}");
			}
		}

		public void SetBoolStates(int frame, int count, string buttonName, bool val)
		{
			if (Log.Count < frame + count)
			{
				ExtendMovieForEdit(frame + count - Log.Count);
			}

			ChangeLog.AddGeneralUndo(frame, frame + count - 1, $"Set {buttonName}({(val ? "On" : "Off")}): {frame}-{frame + count - 1}");

			int changed = -1;
			for (int i = 0; i < count; i++)
			{
				var adapter = GetInputState(frame + i);
				bool old = adapter.IsPressed(buttonName);
				adapter.SetBool(buttonName, val);

				var lg = LogGeneratorInstance(adapter);
				Log[frame + i] = lg.GenerateLogEntry();

				if (changed == -1 && old != val)
				{
					changed = frame + i;
				}
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
			if (frame >= Log.Count) // Insert blank frames up to this point
			{
				ExtendMovieForEdit(frame - Log.Count + 1);
			}

			var adapter = GetInputState(frame);
			var old = adapter.AxisValue(buttonName);
			adapter.SetAxis(buttonName, val);

			var lg = LogGeneratorInstance(adapter);
			Log[frame] = lg.GenerateLogEntry();

			if (old != val)
			{
				InvalidateAfter(frame);
				Changes = true;
				ChangeLog.AddFloatChange(frame, buttonName, old, val, $"Set {buttonName}({val}): {frame}");
			}
		}

		public void SetFloatStates(int frame, int count, string buttonName, float val)
		{
			if (frame + count >= Log.Count) // Insert blank frames up to this point
			{
				ExtendMovieForEdit(frame - Log.Count + 1);
			}

			ChangeLog.AddGeneralUndo(frame, frame + count - 1, $"Set {buttonName}({val}): {frame}-{frame + count - 1}");

			int changed = -1;
			for (int i = 0; i < count; i++)
			{
				var adapter = GetInputState(frame + i);
				float old = adapter.AxisValue(buttonName);
				adapter.SetAxis(buttonName, val);

				var lg = LogGeneratorInstance(adapter);
				Log[frame + i] = lg.GenerateLogEntry();

				if (changed == -1 && old != val)
				{
					changed = frame + i;
				}
			}

			if (changed != -1)
			{
				InvalidateAfter(changed);
				Changes = true;
			}

			ChangeLog.SetGeneralRedo();
		}

		#region LagLog

		public void RemoveLagHistory(int frame)
		{
			TasLagLog.RemoveHistoryAt(frame);
		}

		public void InsertLagHistory(int frame, bool isLag)
		{
			TasLagLog.InsertHistoryAt(frame, isLag);
		}

		public void SetLag(int frame, bool? value)
		{
			TasLagLog[frame] = value;
		}

		#endregion
	}
}
