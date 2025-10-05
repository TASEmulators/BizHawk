using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal partial class TasMovie
	{
		public IMovieChangeLog ChangeLog { get; set; }

		// In each editing method we do things in this order:
		// 1) Any special logic (such as short-circuit)
		// 2) Begin an undo batch, if needed.
		// 3) Edit the movie.
		// 4) End the undo batch, if needed.
		// 5) Call InvalidateAfter.

		// InvalidateAfter being last ensures that the GreenzoneInvalidated callback sees all edits.

		public override void RecordFrame(int targetFrame, int currentFrame, IController source)
		{
			ChangeLog.AddGeneralUndo(targetFrame, targetFrame, $"Record Frame: {targetFrame}");
			SetFrameAt(targetFrame, Bk2LogEntryGenerator.GenerateLogEntry(source));
			ChangeLog.SetGeneralRedo();

			LagLog[targetFrame] = _inputPollable.IsLagFrame;
			LastEditWasRecording = true;

			InvalidateAfter(targetFrame);
		}

		public override void Truncate(int frame)
		{
			if (frame >= Log.Count - 1) return;

			bool endBatch = ChangeLog.BeginNewBatch($"Truncate Movie: {frame}", true);

			ChangeLog.AddGeneralUndo(frame, InputLogLength - 1);
			base.Truncate(frame);
			ChangeLog.SetGeneralRedo();

			Markers.TruncateAt(frame);

			if (endBatch) ChangeLog.EndBatch();

			InvalidateAfter(frame);
		}

		public override void PokeFrame(int frame, IController source)
		{
			ChangeLog.AddGeneralUndo(frame, frame, $"Set Frame At: {frame}");
			base.PokeFrame(frame, source);
			ChangeLog.SetGeneralRedo();

			InvalidateAfter(frame);
		}

		public void SetFrame(int frame, string source)
		{
			ChangeLog.AddGeneralUndo(frame, frame, $"Set Frame At: {frame}");
			SetFrameAt(frame, source);
			ChangeLog.SetGeneralRedo();

			InvalidateAfter(frame);
		}

		public void ClearFrame(int frame)
		{
			string empty = Bk2LogEntryGenerator.EmptyEntry(Session.MovieController);
			if (GetInputLogEntry(frame) == empty) return;

			ChangeLog.AddGeneralUndo(frame, frame, $"Clear Frame: {frame}");
			SetFrameAt(frame, empty);
			ChangeLog.SetGeneralRedo();

			InvalidateAfter(frame);
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
			if (frames.Count is 0) return;

			// Separate the given frames into contiguous blocks
			// and process each block independently
			List<int> framesToDelete = frames
				.Where(fr => fr >= 0 && fr < InputLogLength)
				.Order().ToList();

			SingleInvalidation(() =>
			{
				int alreadyDeleted = 0;
				bool endBatch = ChangeLog.BeginNewBatch($"Delete {framesToDelete.Count} frames from {framesToDelete[0]}-{framesToDelete[framesToDelete.Count - 1]}", true);
				for (int i = 1; i <= framesToDelete.Count; i++)
				{
					if (i == framesToDelete.Count || framesToDelete[i] - framesToDelete[i - 1] != 1)
					{
						RemoveFrames(framesToDelete[alreadyDeleted] - alreadyDeleted, framesToDelete[i - 1] + 1 - alreadyDeleted);
						alreadyDeleted = i;
					}
				}
				if (endBatch) ChangeLog.EndBatch();
			});
		}

		public void RemoveFrames(int removeStart, int removeUpTo)
		{
			bool endBatch = ChangeLog.BeginNewBatch($"Remove frames {removeStart}-{removeUpTo - 1}", true);
			if (BindMarkersToInput)
			{
				// O(n^2) removal time, but removing many binded markers in a deleted section is probably rare.
				List<TasMovieMarker> markersToRemove = Markers.Where(m => m.Frame >= removeStart && m.Frame < removeUpTo).ToList();
				foreach (var marker in markersToRemove)
				{
					Markers.Remove(marker);
				}
			}
			ShiftBindedMarkers(removeUpTo, removeStart - removeUpTo);

			// Log.GetRange() might be preferrable, but Log's type complicates that.
			string[] removedInputs = new string[removeUpTo - removeStart];
			Log.CopyTo(removeStart, removedInputs, 0, removedInputs.Length);
			Log.RemoveRange(removeStart, removeUpTo - removeStart);

			ChangeLog.AddRemoveFrames(
				removeStart,
				removeUpTo,
				removedInputs.ToList(),
				BindMarkersToInput
			);
			if (endBatch) ChangeLog.EndBatch();

			InvalidateAfter(removeStart);
		}

		public void InsertInput(int frame, string inputState)
		{
			var inputLog = new List<string> { inputState };
			InsertInput(frame, inputLog); // ChangeLog handled within
		}

		public void InsertInput(int frame, IEnumerable<string> inputLog)
		{
			Log.InsertRange(frame, inputLog);
			ShiftBindedMarkers(frame, inputLog.Count());
			ChangeLog.AddInsertInput(frame, inputLog.ToList(), BindMarkersToInput, $"Insert {inputLog.Count()} frame(s) at {frame}");

			InvalidateAfter(frame);
		}

		public void InsertInput(int frame, IEnumerable<IController> inputStates)
		{
			var inputLog = new List<string>();

			foreach (var input in inputStates)
			{
				inputLog.Add(Bk2LogEntryGenerator.GenerateLogEntry(input));
			}

			InsertInput(frame, inputLog); // Sets the ChangeLog
		}

		public void CopyOverInput(int frame, IEnumerable<IController> inputStates)
		{
			var states = inputStates.ToList();

			bool endBatch = ChangeLog.BeginNewBatch($"Copy Over Input: {frame}", true);

			if (Log.Count < states.Count + frame)
			{
				ExtendMovieForEdit(states.Count + frame - Log.Count);
			}

			ChangeLog.AddGeneralUndo(frame, frame + states.Count - 1, $"Copy Over Input: {frame}");
			for (int i = 0; i < states.Count; i++)
			{
				Log[frame + i] = Bk2LogEntryGenerator.GenerateLogEntry(states[i]);
			}
			int firstChangedFrame = ChangeLog.SetGeneralRedo();

			if (endBatch) ChangeLog.EndBatch();

			if (firstChangedFrame != -1)
			{
				InvalidateAfter(firstChangedFrame);
			}
		}

		public void InsertEmptyFrame(int frame, int count = 1)
		{
			frame = Math.Min(frame, Log.Count);

			Log.InsertRange(frame, Enumerable.Repeat(Bk2LogEntryGenerator.EmptyEntry(Session.MovieController), count));
			ShiftBindedMarkers(frame, count);
			ChangeLog.AddInsertFrames(frame, count, BindMarkersToInput, $"Insert {count} empty frame(s) at {frame}");

			InvalidateAfter(frame);
		}

		private void ExtendMovieForEdit(int numFrames)
		{
			int oldLength = InputLogLength;

			// account for autohold TODO: What about auto-fire?
			string inputs = Bk2LogEntryGenerator.GenerateLogEntry(Session.StickySource);
			for (int i = 0; i < numFrames; i++)
			{
				Log.Add(inputs);
			}

			ChangeLog.AddExtend(oldLength, numFrames, inputs);
		}

		public void ToggleBoolState(int frame, string buttonName)
		{
			bool endBatch = ChangeLog.BeginNewBatch($"Toggle {buttonName}: {frame}", true);

			if (frame >= Log.Count) // Insert blank frames up to this point
			{
				ExtendMovieForEdit(frame - Log.Count + 1);
			}

			var adapter = GetInputState(frame);
			adapter.SetBool(buttonName, !adapter.IsPressed(buttonName));

			Log[frame] = Bk2LogEntryGenerator.GenerateLogEntry(adapter);
			ChangeLog.AddBoolToggle(frame, buttonName, !adapter.IsPressed(buttonName));

			if (endBatch) ChangeLog.EndBatch();

			InvalidateAfter(frame);
		}

		public void SetBoolState(int frame, string buttonName, bool val)
		{
			bool endBatch = ChangeLog.BeginNewBatch($"Set {buttonName}({(val ? "On" : "Off")}): {frame}", true);

			bool extended = false;
			if (frame >= Log.Count) // Insert blank frames up to this point
			{
				ExtendMovieForEdit(frame - Log.Count + 1);
				extended = true;
			}

			var adapter = GetInputState(frame);
			var old = adapter.IsPressed(buttonName);

			if (old != val)
			{
				adapter.SetBool(buttonName, val);
				Log[frame] = Bk2LogEntryGenerator.GenerateLogEntry(adapter);
				ChangeLog.AddBoolToggle(frame, buttonName, old);
			}

			if (endBatch) ChangeLog.EndBatch();

			if (old != val || extended) InvalidateAfter(frame);
		}

		public void SetBoolStates(int frame, int count, string buttonName, bool val)
		{
			bool endBatch = ChangeLog.BeginNewBatch($"Set {buttonName}({(val ? "On" : "Off")}): {frame}-{frame + count - 1}", true);

			int firstChangedFrame = -1;
			if (Log.Count < frame + count)
			{
				firstChangedFrame = Log.Count;
				ExtendMovieForEdit(frame + count - Log.Count);
			}

			ChangeLog.AddGeneralUndo(frame, frame + count - 1);
			for (int i = 0; i < count; i++)
			{
				var adapter = GetInputState(frame + i);
				bool old = adapter.IsPressed(buttonName);
				adapter.SetBool(buttonName, val);

				Log[frame + i] = Bk2LogEntryGenerator.GenerateLogEntry(adapter);

				if (firstChangedFrame == -1 && old != val)
				{
					firstChangedFrame = frame + i;
				}
			}
			ChangeLog.SetGeneralRedo();

			if (endBatch) ChangeLog.EndBatch();

			if (firstChangedFrame != -1) InvalidateAfter(firstChangedFrame);
		}

		public void SetAxisState(int frame, string buttonName, int val)
		{
			bool endBatch = ChangeLog.BeginNewBatch($"Set {buttonName}({val}): {frame}", true);

			bool extended = false;
			if (frame >= Log.Count) // Insert blank frames up to this point
			{
				ExtendMovieForEdit(frame - Log.Count + 1);
				extended = true;
			}

			var adapter = GetInputState(frame);
			var old = adapter.AxisValue(buttonName);

			if (old != val)
			{
				adapter.SetAxis(buttonName, val);
				Log[frame] = Bk2LogEntryGenerator.GenerateLogEntry(adapter);
				ChangeLog.AddAxisChange(frame, buttonName, old, val);
			}

			if (endBatch) ChangeLog.EndBatch();

			if (old != val || extended) InvalidateAfter(frame);
		}

		public void SetAxisStates(int frame, int count, string buttonName, int val)
		{
			bool endBatch = ChangeLog.BeginNewBatch($"Set {buttonName}({val}): {frame}-{frame + count - 1}", true);

			int firstChangedFrame = -1;
			if (frame + count >= Log.Count) // Insert blank frames up to this point
			{
				firstChangedFrame = Log.Count;
				ExtendMovieForEdit(frame + count - Log.Count);
			}

			ChangeLog.AddGeneralUndo(frame, frame + count - 1);
			for (int i = 0; i < count; i++)
			{
				var adapter = GetInputState(frame + i);
				var old = adapter.AxisValue(buttonName);
				adapter.SetAxis(buttonName, val);

				Log[frame + i] = Bk2LogEntryGenerator.GenerateLogEntry(adapter);

				if (firstChangedFrame == -1 && old != val)
				{
					firstChangedFrame = frame + i;
				}
			}
			ChangeLog.SetGeneralRedo();

			if (endBatch) ChangeLog.EndBatch();

			if (firstChangedFrame != -1) InvalidateAfter(firstChangedFrame);
		}

		private bool _suspendInvalidation;
		private int _minInvalidationFrame;
		public void SingleInvalidation(Action action)
		{
			bool wasSuspending = _suspendInvalidation;
			if (!wasSuspending) _minInvalidationFrame = int.MaxValue;
			_suspendInvalidation = true;
			try { action(); }
			finally { _suspendInvalidation = wasSuspending; }

			if (!wasSuspending && _minInvalidationFrame != int.MaxValue)
			{
				InvalidateAfter(_minInvalidationFrame);
			}
		}
	}
}
