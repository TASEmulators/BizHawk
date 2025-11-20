using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BizHawk.Client.Common
{
	public interface IMovieChangeLog
	{
		/// <summary>
		/// The index of the action that would be undone, in the undo history list. If there is nothing to undo, -1.
		/// </summary>
		int UndoIndex { get; }

		/// <summary>
		/// The Id of the most recent recorded action.
		/// <br/>This is different from index. IDs don't change and are never reused.
		/// </summary>
		int MostRecentId { get; }

		/// <summary>
		/// The total number of actions currently recorded.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the movie is recording action history.
		/// This is not intended to turn off the ChangeLog, but to disable the normal recording process.
		/// Use this to manually control the ChangeLog. (Useful for disabling the ChangeLog during undo/redo).
		/// </summary>
		bool IsRecording { get; set; }
		void Clear(int upTo = -1);

		/// <summary>
		/// All changes made between calling Begin and End will be one Undo.
		/// If already recording in a batch, calls EndBatch.
		/// </summary>
		/// <param name="name">The name of the batch</param>
		/// <param name="keepOldBatch">If set and a batch is in progress, a new batch will not be created.</param>
		/// <returns>Returns true if a new batch was started; otherwise false.</returns>
		bool BeginNewBatch(string name = "", bool keepOldBatch = false);

		/// <summary>
		/// Ends the current undo batch. Future changes will be one undo each.
		/// If not already recording a batch, does nothing.
		/// </summary>
		void EndBatch();

		string GetActionName(int index);

		/// <summary>
		/// Combine two undo actions, making them part of one batch action.
		/// The actions will be merged into the first one, which will keep its original name and id.
		/// Use action IDs, not indexes.
		/// </summary>
		/// <param name="action1_id">The first action. Must have happened before action2.</param>
		/// <param name="action2_id">The second action. Must have happened after action1.</param>
		/// <returns>True if the actions were merged, or the second action doesn't exist. False otherwise.</returns>
		bool MergeActions(int action1_id, int action2_id);

		/// <summary>
		/// Undoes the most recent action batch, if any exist.
		/// </summary>
		void Undo();

		/// <summary>
		/// Redoes the most recent undo, if any exist.
		/// </summary>
		void Redo();
		bool CanUndo { get; }
		bool CanRedo { get; }
		int MaxSteps { get; set; }

		/// <summary>
		/// Creates a full copy of the change log.
		/// </summary>
		IMovieChangeLog Clone();

		void AddGeneralUndo(int first, int last, string name = "");
		int SetGeneralRedo();
		void AddBoolToggle(int frame, string button, bool oldState, string name = "");
		void AddAxisChange(int frame, string button, int oldState, int newState, string name = "");
		void AddMarkerChange(TasMovieMarker newMarker, int oldPosition = -1, string oldMessage = "", string name = "");
		void AddInputBind(int frame, bool isDelete, string name = "");
		void AddInsertFrames(int frame, int count, bool bindMarkers, string name = "");
		void AddInsertInput(int frame, List<string> newInputs, bool bindMarkers, string name = "");
		void AddRemoveFrames(int removeStart, int removeUpTo, List<string> oldInputs, bool bindMarkers, string name = "");
		void AddExtend(int originalLength, int count, string inputs);
	}

	public class TasMovieChangeLog : IMovieChangeLog
	{
		public TasMovieChangeLog(ITasMovie movie)
		{
			_movie = movie;
		}

		private class UndoItem
		{
			public List<IMovieAction> actions;
			public string name;
			public int id;

			public UndoItem Clone()
			{
				return new()
				{
					actions = new(this.actions),
					name = this.name,
					id = this.id,
				};
			}
		}

		private List<UndoItem> _history = new();
		private readonly ITasMovie _movie;

		private int _maxSteps = 1000;
		private int _totalSteps;
		private bool _recordingBatch;

		private List<IMovieAction> LatestBatch
			=> _history[_history.Count - 1].actions;

		public int UndoIndex { get; private set; } = -1;
		public int MostRecentId => _totalSteps;
		public int Count => _history.Count;

		public int MaxSteps
		{
			get => _maxSteps;
			set
			{
				_maxSteps = value;
				if (_history.Count > value)
				{
					Clear(_history.Count - value);
				}
			}
		}

		public bool IsRecording { get; set; } = true;

		public void Clear(int upTo = -1)
		{
			if (upTo == -1)
			{
				upTo = _history.Count;
			}

			_history.RemoveRange(0, upTo);
			UndoIndex -= upTo;
			if (UndoIndex < -1)
			{
				UndoIndex = -1;
			}

			if (_history.Count == 0)
			{
				_recordingBatch = false;
			}
		}

		private void TruncateLog(int from)
		{
			_history.RemoveRange(from, _history.Count - from);

			if (UndoIndex < _history.Count - 1)
			{
				UndoIndex = _history.Count - 1;
			}

			if (_recordingBatch)
			{
				// This means we are adding new actions to a batch that was undone while still in progress.
				// So start a new one.
				_recordingBatch = false;
				BeginNewBatch();
			}
		}

		public bool BeginNewBatch(string name = "", bool keepOldBatch = false)
		{
			if (!IsRecording)
			{
				return false;
			}

			bool ret = true;
			if (_recordingBatch)
			{
				if (keepOldBatch)
				{
					ret = false;
				}
				else
				{
					EndBatch();
				}
			}

			if (ret)
			{
				AddMovieAction(name);
			}

			_recordingBatch = true;

			return ret;
		}

		public void EndBatch()
		{
			if (!IsRecording || !_recordingBatch)
			{
				return;
			}

			_recordingBatch = false;
			var last = LatestBatch;
			if (last.Count == 0) // Remove batch if it's empty.
			{
				_history.RemoveAt(_history.Count - 1);
				UndoIndex--;
			}
			else
			{
				last.Capacity = last.Count;
			}
		}

		public string GetActionName(int index)
		{
			return _history[index].name;
		}

		public bool MergeActions(int action1, int action2)
		{
			int index2 = _history.FindLastIndex(item => item.id == action2);
			if (index2 == -1) return true; // second action doesn't exist (perhaps it was already merged, or deleted for being a no-op)
			if (index2 == 0) return false;
			int index1 = index2 - 1;
			if (_history[index1].id != action1) return false;

			_history[index1].actions.AddRange(_history[index2].actions);
			_history.RemoveAt(_history.Count - 1);
			UndoIndex--;

			return true;
		}

		public void Undo()
		{
			if (UndoIndex == -1)
			{
				return;
			}

			List<IMovieAction> batch = _history[UndoIndex].actions;
			UndoIndex--;

			bool wasRecording = IsRecording;
			IsRecording = false;
			_movie.SingleInvalidation(() =>
			{
				for (int i = batch.Count - 1; i >= 0; i--)
				{
					batch[i].Undo(_movie);
				}
			});
			IsRecording = wasRecording;
		}

		public void Redo()
		{
			if (UndoIndex == _history.Count - 1)
			{
				return;
			}

			UndoIndex++;
			List<IMovieAction> batch = _history[UndoIndex].actions;

			bool wasRecording = IsRecording;
			IsRecording = false;
			_movie.SingleInvalidation(() =>
			{
				foreach (IMovieAction b in batch)
				{
					b.Redo(_movie);
				}
			});
			IsRecording = wasRecording;
		}

		public bool CanUndo => UndoIndex > -1;
		public bool CanRedo => UndoIndex < _history.Count - 1;

		private void AddMovieAction(string name)
		{
			if (UndoIndex + 1 != _history.Count)
			{
				TruncateLog(UndoIndex + 1);
			}
			if (name.Length is 0) name = $"Undo step {_totalSteps}";
			if (!_recordingBatch)
			{
				_totalSteps++;
				UndoItem item = new();
				item.actions = new List<IMovieAction>(1);
				item.name = name;
				item.id = _totalSteps;
				_history.Add(item);

				if (_history.Count <= MaxSteps)
				{
					UndoIndex += 1;
				}
				else
				{
					_history.RemoveAt(0);
				}
			}
		}

		public IMovieChangeLog Clone()
		{
			TasMovieChangeLog newLog = (TasMovieChangeLog)this.MemberwiseClone();
			// The history list is the only thing that needs more than a shallow copy from MemberwiseClone.
			newLog._history = new();
			// Clone each UndoItem because they can be mutated by MergeActions.
			foreach (UndoItem item in this._history)
			{
				newLog._history.Add(item.Clone());
			}
			return newLog;
		}

		// TODO: These probably aren't the best way to handle undo/redo.
		private int _lastGeneral;

		public void AddGeneralUndo(int first, int last, string name = "")
		{
			if (IsRecording)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieAction(first, last, _movie));
				_lastGeneral = LatestBatch.Count - 1;
			}
		}

		public int SetGeneralRedo()
		{
			if (IsRecording)
			{
				Debug.Assert(_lastGeneral == LatestBatch.Count - 1, "GeneralRedo should not see changes from other undo actions.");
				int changed = ((MovieAction) LatestBatch[_lastGeneral]).SetRedoLog(_movie);
				if (changed == -1)
				{
					LatestBatch.RemoveAt(_lastGeneral);
					if (LatestBatch.Count == 0 && !_recordingBatch)
					{
						// Remove this undo item
						_recordingBatch = true;
						EndBatch();
					}
				}
				return changed;
			}
			return -1;
		}

		public void AddBoolToggle(int frame, string button, bool oldState, string name = "")
		{
			if (IsRecording)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieActionFrameEdit(frame, button, oldState, !oldState));
			}
		}

		public void AddAxisChange(int frame, string button, int oldState, int newState, string name = "")
		{
			if (IsRecording)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieActionFrameEdit(frame, button, oldState, newState));
			}
		}

		public void AddMarkerChange(TasMovieMarker newMarker, int oldPosition = -1, string oldMessage = "", string name = "")
		{
			if (IsRecording)
			{
				name = oldPosition == -1
					? $"Set Marker at frame {newMarker.Frame}"
					: $"Remove Marker at frame {oldPosition}";

				AddMovieAction(name);
				LatestBatch.Add(new MovieActionMarker(newMarker, oldPosition, oldMessage));
			}
		}

		public void AddInputBind(int frame, bool isDelete, string name = "")
		{
			if (IsRecording)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieActionBindInput(_movie, frame, isDelete));
			}
		}

		public void AddInsertFrames(int frame, int count, bool bindMarkers, string name = "")
		{
			if (IsRecording)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieActionInsertFrames(frame, bindMarkers, count));
			}
		}

		public void AddInsertInput(int frame, List<string> newInputs, bool bindMarkers, string name = "")
		{
			if (IsRecording)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieActionInsertFrames(frame, bindMarkers, newInputs));
			}
		}

		public void AddRemoveFrames(int removeStart, int removeUpTo, List<string> oldInputs, bool bindMarkers, string name = "")
		{
			if (IsRecording)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieActionRemoveFrames(removeStart, removeUpTo, oldInputs, bindMarkers));
			}
		}

		public void AddExtend(int originalLength, int count, string inputs)
		{
			if (IsRecording)
			{
				AddMovieAction("extend movie");
				LatestBatch.Add(new MovieActionExtend(originalLength, count, inputs));
			}
		}
	}

	public interface IMovieAction
	{
		void Undo(ITasMovie movie);
		void Redo(ITasMovie movie);

		int FirstFrame { get; }
		int LastFrame { get; }
	}

	public class MovieAction : IMovieAction
	{
		public int FirstFrame { get; }
		public int LastFrame { get; }

		private readonly int _undoLength;
		private int _redoLength;

		private int Length => LastFrame - FirstFrame + 1;

		private readonly List<string> _oldLog;
		private List<string> _newLog;
		private readonly bool _bindMarkers;

		public MovieAction(int firstFrame, int lastFrame, ITasMovie movie)
		{
			FirstFrame = firstFrame;
			LastFrame = lastFrame;
			_oldLog = new List<string>(Length);
			_undoLength = Math.Min(LastFrame + 1, movie.InputLogLength) - FirstFrame;

			for (int i = 0; i < _undoLength; i++)
			{
				_oldLog.Add(movie.GetInputLogEntry(FirstFrame + i));
			}

			_bindMarkers = movie.BindMarkersToInput;
		}

		/// <returns>Returns the first frame that has changed, or -1 if no changes.</returns>
		public int SetRedoLog(ITasMovie movie)
		{
			_redoLength = Math.Min(LastFrame + 1, movie.InputLogLength) - FirstFrame;
			_newLog = new List<string>(_redoLength);
			int changed = Math.Min(_redoLength, _undoLength);
			for (int i = 0; i < _redoLength; i++)
			{
				string newEntry = movie.GetInputLogEntry(FirstFrame + i);
				_newLog.Add(newEntry);
				if (i < changed && newEntry != _oldLog[i]) changed = i;
			}

			if (changed == _redoLength && changed == _undoLength) return -1;
			else return changed + FirstFrame;
		}

		public void Undo(ITasMovie movie)
		{
			bool wasBinding = movie.BindMarkersToInput;
			movie.BindMarkersToInput = _bindMarkers;

			if (_redoLength != Length)
			{
				movie.InsertEmptyFrame(FirstFrame, Length - _redoLength);
			}

			if (_undoLength != Length)
			{
				movie.RemoveFrames(FirstFrame, movie.InputLogLength - _undoLength);
			}

			for (int i = 0; i < _undoLength; i++)
			{
				movie.SetFrame(FirstFrame + i, _oldLog[i]);
			}
			movie.BindMarkersToInput = wasBinding;
		}

		public void Redo(ITasMovie movie)
		{
			bool wasBinding = movie.BindMarkersToInput;
			movie.BindMarkersToInput = _bindMarkers;

			if (_undoLength != Length)
			{
				movie.InsertEmptyFrame(FirstFrame, Length - _undoLength);
			}

			if (_redoLength != Length)
			{
				movie.RemoveFrames(FirstFrame, movie.InputLogLength - _redoLength);
			}

			for (int i = 0; i < _redoLength; i++)
			{
				movie.SetFrame(FirstFrame + i, _newLog[i]);
			}
			movie.BindMarkersToInput = wasBinding;
		}
	}

	public sealed class MovieActionMarker : IMovieAction
	{
		public int FirstFrame { get; }
		public int LastFrame { get; }

		private readonly string _oldMessage;
		private readonly string _newMessage;

		public MovieActionMarker(TasMovieMarker marker, int oldPosition = -1, string oldMessage = "")
		{
			FirstFrame = oldPosition;
			if (marker == null)
			{
				LastFrame = -1;
				_oldMessage = oldMessage;
			}
			else
			{
				LastFrame = marker.Frame;
				_oldMessage = oldMessage.Length is 0 ? marker.Message : oldMessage;
				_newMessage = marker.Message;
			}
		}

		public void Undo(ITasMovie movie)
		{
			if (FirstFrame == -1) // Action: Place marker
			{
				movie.Markers.Remove(movie.Markers.Get(LastFrame));
			}
			else if (LastFrame == -1) // Action: Remove marker
			{
				movie.Markers.Add(FirstFrame, _oldMessage);
			}
			else // Action: Move/rename marker
			{
				movie.Markers.Move(LastFrame, FirstFrame);
				movie.Markers.Get(LastFrame).Message = _oldMessage;
			}
		}

		public void Redo(ITasMovie movie)
		{
			if (FirstFrame == -1) // Action: Place marker
			{
				movie.Markers.Add(LastFrame, _oldMessage);
			}
			else if (LastFrame == -1) // Action: Remove marker
			{
				movie.Markers.Remove(movie.Markers.Get(FirstFrame));
			}
			else // Action: Move/rename marker
			{
				movie.Markers.Move(FirstFrame, LastFrame);
				movie.Markers.Get(LastFrame).Message = _newMessage;
			}
		}
	}

	public class MovieActionFrameEdit : IMovieAction
	{
		public int FirstFrame { get; }
		public int LastFrame => FirstFrame;

		private readonly int _oldState;
		private readonly int _newState;

		private readonly string _buttonName;
		private readonly bool _isAxis;

		public MovieActionFrameEdit(int frame, string button, bool oldS, bool newS)
		{
			_oldState = oldS ? 1 : 0;
			_newState = newS ? 1 : 0;
			FirstFrame = frame;
			_buttonName = button;
		}

		public MovieActionFrameEdit(int frame, string button, int oldS, int newS)
		{
			_oldState = oldS;
			_newState = newS;
			FirstFrame = frame;
			_buttonName = button;
			_isAxis = true;
		}

		public void Undo(ITasMovie movie)
		{
			if (_isAxis)
			{
				movie.SetAxisState(FirstFrame, _buttonName, _oldState);
			}
			else
			{
				movie.SetBoolState(FirstFrame, _buttonName, _oldState == 1);
			}
		}

		public void Redo(ITasMovie movie)
		{
			if (_isAxis)
			{
				movie.SetAxisState(FirstFrame, _buttonName, _newState);
			}
			else
			{
				movie.SetBoolState(FirstFrame, _buttonName, _newState == 1);
			}
		}
	}

	public class MovieActionPaint : IMovieAction
	{
		public int FirstFrame { get; }
		public int LastFrame { get; }
		private readonly List<int> _oldState;
		private readonly int _newState;
		private readonly string _buttonName;
		private readonly bool _isAxis = false;

		public MovieActionPaint(int startFrame, int endFrame, string button, bool newS, IMovie movie)
		{
			_newState = newS ? 1 : 0;
			FirstFrame = startFrame;
			LastFrame = endFrame;
			_buttonName = button;
			_oldState = new List<int>(endFrame - startFrame + 1);

			for (int i = 0; i < endFrame - startFrame + 1; i++)
			{
				_oldState.Add(movie.BoolIsPressed(startFrame + i, button) ? 1 : 0);
			}
		}

		public MovieActionPaint(int startFrame, int endFrame, string button, int newS, IMovie movie)
		{
			_newState = newS;
			FirstFrame = startFrame;
			LastFrame = endFrame;
			_buttonName = button;
			_isAxis = true;
			_oldState = new List<int>(endFrame - startFrame + 1);

			for (int i = 0; i < endFrame - startFrame + 1; i++)
			{
				_oldState.Add(movie.BoolIsPressed(startFrame + i, button) ? 1 : 0);
			}
		}

		public void Undo(ITasMovie movie)
		{
			if (_isAxis)
			{
				for (int i = 0; i < _oldState.Count; i++)
				{
					movie.SetAxisState(FirstFrame + i, _buttonName, _oldState[i]);
				}
			}
			else
			{
				for (int i = 0; i < _oldState.Count; i++)
				{
					movie.SetBoolState(FirstFrame + i, _buttonName, _oldState[i] == 1);
				}
			}
		}

		public void Redo(ITasMovie movie)
		{
			if (_isAxis)
			{
				movie.SetAxisStates(FirstFrame, LastFrame - FirstFrame + 1, _buttonName, _newState);
			}
			else
			{
				movie.SetBoolStates(FirstFrame, LastFrame - FirstFrame + 1, _buttonName, _newState == 1);
			}
		}
	}

	public class MovieActionBindInput : IMovieAction
	{
		public int FirstFrame { get; }
		public int LastFrame { get; }

		private readonly string _log;
		private readonly bool _delete;

		private readonly bool _bindMarkers;

		public MovieActionBindInput(ITasMovie movie, int frame, bool isDelete)
		{
			FirstFrame = LastFrame = frame;
			_log = movie.GetInputLogEntry(frame);
			_delete = isDelete;
			_bindMarkers = movie.BindMarkersToInput;
		}

		public void Undo(ITasMovie movie)
		{
			movie.BindMarkersToInput = _bindMarkers;

			if (_delete) // Insert
			{
				movie.InsertInput(FirstFrame, _log);
				movie.LagLog.InsertHistoryAt(FirstFrame + 1, true);
			}
			else // Delete
			{
				movie.RemoveFrame(FirstFrame);
				movie.LagLog.RemoveHistoryAt(FirstFrame + 1);
			}
			movie.BindMarkersToInput = _bindMarkers;
		}

		public void Redo(ITasMovie movie)
		{
			movie.BindMarkersToInput = _bindMarkers;

			if (_delete)
			{
				movie.RemoveFrame(FirstFrame);
				movie.LagLog.RemoveHistoryAt(FirstFrame + 1);
			}
			else
			{
				movie.InsertInput(FirstFrame, _log);
				movie.LagLog.InsertHistoryAt(FirstFrame + 1, true);
			}
			movie.BindMarkersToInput = _bindMarkers;
		}
	}

	public class MovieActionInsertFrames : IMovieAction
	{
		public int FirstFrame { get; }
		public int LastFrame { get; }
		private readonly int _count;
		private readonly bool _onlyEmpty;
		private readonly bool _bindMarkers;

		private readonly List<string> _newInputs;

		public MovieActionInsertFrames(int frame, bool bindMarkers, int count)
		{
			FirstFrame = frame;
			LastFrame = frame + count;
			_count = count;
			_onlyEmpty = true;
			_bindMarkers = bindMarkers;
		}

		public MovieActionInsertFrames(int frame, bool bindMarkers, List<string> newInputs)
		{
			FirstFrame = frame;
			LastFrame = frame + newInputs.Count;
			_onlyEmpty = false;
			_newInputs = newInputs;
			_bindMarkers = bindMarkers;
		}

		public void Undo(ITasMovie movie)
		{
			bool wasBinding = movie.BindMarkersToInput;
			movie.BindMarkersToInput = _bindMarkers;

			movie.RemoveFrames(FirstFrame, LastFrame);

			movie.BindMarkersToInput = wasBinding;
		}

		public void Redo(ITasMovie movie)
		{
			bool wasBinding = movie.BindMarkersToInput;
			movie.BindMarkersToInput = _bindMarkers;

			if (_onlyEmpty)
			{
				movie.InsertEmptyFrame(FirstFrame, _count);
			}
			else
			{
				movie.InsertInput(FirstFrame, _newInputs);
			}

			movie.BindMarkersToInput = wasBinding;
		}
	}

	public class MovieActionRemoveFrames : IMovieAction
	{
		public int FirstFrame { get; }
		public int LastFrame { get; }

		private readonly List<string> _oldInputs;
		private readonly bool _bindMarkers;

		public MovieActionRemoveFrames(int removeStart, int removeUpTo, List<string> oldInputs, bool bindMarkers)
		{
			FirstFrame = removeStart;
			LastFrame = removeUpTo;
			_oldInputs = oldInputs;
			_bindMarkers = bindMarkers;
		}

		public void Undo(ITasMovie movie)
		{
			bool wasBinding = movie.BindMarkersToInput;
			movie.BindMarkersToInput = _bindMarkers;

			movie.InsertInput(FirstFrame, _oldInputs);

			movie.BindMarkersToInput = wasBinding;
		}

		public void Redo(ITasMovie movie)
		{
			bool wasBinding = movie.BindMarkersToInput;
			movie.BindMarkersToInput = _bindMarkers;

			movie.RemoveFrames(FirstFrame, LastFrame);

			movie.BindMarkersToInput = wasBinding;
		}
	}


	public class MovieActionExtend : IMovieAction
	{
		public int FirstFrame { get; }
		public int LastFrame => FirstFrame + _count - 1;

		private int _count;
		private string _inputs;

		public MovieActionExtend(int currentEndOfMovie, int count, string inputs)
		{
			FirstFrame = currentEndOfMovie;
			_count = count;
			_inputs = inputs;
		}

		public void Undo(ITasMovie movie)
		{
			bool wasMarkerBound = movie.BindMarkersToInput;
			movie.BindMarkersToInput = false;

			movie.RemoveFrames(FirstFrame, LastFrame + 1);
			movie.BindMarkersToInput = wasMarkerBound;
		}

		public void Redo(ITasMovie movie)
		{
			bool wasMarkerBound = movie.BindMarkersToInput;
			movie.BindMarkersToInput = false;

			movie.InsertInput(FirstFrame, Enumerable.Repeat(_inputs, _count));
			movie.BindMarkersToInput = wasMarkerBound;
		}
	}
}
