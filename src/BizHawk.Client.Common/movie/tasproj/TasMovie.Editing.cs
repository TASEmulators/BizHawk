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

			var lg = LogGeneratorInstance(source);
			SetFrameAt(frame, lg.GenerateLogEntry());

			Changes = true;

			LagLog.RemoveFrom(frame);
			LagLog[frame] = _inputPollable.IsLagFrame;

			if (this.IsRecording())
			{
				TasStateManager.Invalidate(frame + 1);
				GreenzoneInvalidated(frame + 1);
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

			LagLog.RemoveFrom(frame);
			TasStateManager.Invalidate(frame);
			GreenzoneInvalidated(frame);
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

		public void ClearFrame(int frame)
		{
			ChangeLog.AddGeneralUndo(frame, frame, $"Clear Frame: {frame}");

			var lg = LogGeneratorInstance(Session.MovieController);
			SetFrameAt(frame, lg.EmptyEntry);
			Changes = true;

			InvalidateAfter(frame);
			ChangeLog.SetGeneralRedo();
		}

		private void ShiftBindedMarkers(int frame, int offset)
		{
			if (BindMarkersToInput)
			{
				Markers.ShiftAt(frame, offset);
			}
		}

		public void RemoveFrame(int frame)
		{
			RemoveFrames(frame, frame + 1);
		}

		public void RemoveFrames(ICollection<int> frames)
		{
			if (frames.Any())
			{
				// Separate the given frames into contiguous blocks
				// and process each block independently
				List<int> framesToDelete = frames.OrderBy(f => f).ToList();
				// f is the current index for framesToDelete
				int startFrame, prevFrame, frame;
				int f = 0;
				int numDeleted = 0;
				while (numDeleted != framesToDelete.Count)
				{
					prevFrame = startFrame = framesToDelete[f];
					f++;
					for (; f < framesToDelete.Count; f++)
					{
						frame = framesToDelete[f];
						if (frame - 1 != prevFrame)
						{
							f--;
							break;
						}
						prevFrame = frame;
					}
					// Each block is logged as an individual ChangeLog entry
					RemoveFrames(startFrame - numDeleted, prevFrame + 1 - numDeleted);
					numDeleted += prevFrame + 1 - startFrame;
				}
			}
		}

		/// <summary>
		/// Remove all frames between removeStart and removeUpTo (excluding removeUpTo).
		/// </summary>
		/// <param name="removeStart">The first frame to remove.</param>
		/// <param name="removeUpTo">The frame after the last frame to remove.</param>
		public void RemoveFrames(int removeStart, int removeUpTo)
		{
			// Log.GetRange() might be preferrable, but Log's type complicates that.
			string[] removedInputs = new string[removeUpTo - removeStart];
			Log.CopyTo(removeStart, removedInputs, 0, removedInputs.Length);

			// Pre-process removed markers for the ChangeLog.
			List<TasMovieMarker> removedMarkers = new List<TasMovieMarker>();
			if (BindMarkersToInput)
			{
				bool wasRecording = ChangeLog.IsRecording;
				ChangeLog.IsRecording = false;

				// O(n^2) removal time, but removing many binded markers in a deleted section is probably rare.
				removedMarkers = Markers.Where(m => m.Frame >= removeStart && m.Frame < removeUpTo).ToList();
				foreach (var marker in removedMarkers)
				{
					Markers.Remove(marker);
				}

				ChangeLog.IsRecording = wasRecording;
			}

			Log.RemoveRange(removeStart, removeUpTo - removeStart);

			ShiftBindedMarkers(removeUpTo, removeStart - removeUpTo);

			Changes = true;
			InvalidateAfter(removeStart);

			ChangeLog.AddRemoveFrames(
				removeStart,
				removeUpTo,
				removedInputs.ToList(),
				removedMarkers,
				$"Remove frames {removeStart}-{removeUpTo - 1}"
			);
		}

		public void InsertInput(int frame, string inputState)
		{
			var inputLog = new List<string>();
			inputLog.Add(inputState);
			InsertInput(frame, inputLog); // ChangeLog handled within
		}

		public void InsertInput(int frame, IEnumerable<string> inputLog)
		{
			Log.InsertRange(frame, inputLog);

			ShiftBindedMarkers(frame, inputLog.Count());

			Changes = true;
			InvalidateAfter(frame);
			
			ChangeLog.AddInsertInput(frame, inputLog.ToList(), $"Insert {inputLog.Count()} frame(s) at {frame}");
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

		public void InsertEmptyFrame(int frame, int count = 1)
		{
			if (frame > Log.Count())
			{
				frame = Log.Count();
			}

			var lg = LogGeneratorInstance(Session.MovieController);
			Log.InsertRange(frame, Enumerable.Repeat(lg.EmptyEntry, count).ToList());

			ShiftBindedMarkers(frame, count);

			Changes = true;
			InvalidateAfter(frame);

			ChangeLog.AddInsertFrames(frame, count, $"Insert {count} empty frame(s) at {frame}");
		}

		private void ExtendMovieForEdit(int numFrames)
		{
			bool endBatch = ChangeLog.BeginNewBatch("Auto-Extend Movie", true);
			int oldLength = InputLogLength;
			ChangeLog.AddGeneralUndo(oldLength, oldLength + numFrames - 1);

			Session.MovieController.SetFromSticky(Session.StickySource);

			// account for autohold. needs autohold pattern to be already recorded in the current frame
			var lg = LogGeneratorInstance(Session.MovieController);

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

		public void SetAxisState(int frame, string buttonName, int val)
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
				ChangeLog.AddAxisChange(frame, buttonName, old, val, $"Set {buttonName}({val}): {frame}");
			}
		}

		public void SetAxisStates(int frame, int count, string buttonName, int val)
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
				var old = adapter.AxisValue(buttonName);
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
	}
}
