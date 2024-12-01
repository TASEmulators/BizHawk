using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public interface IMovieChangeLog
	{
		List<string> Names { get; }
		int UndoIndex { get; }
		string NextUndoStepName { get; }
		bool IsRecording { get; set; }
		void Clear(int upTo = -1);
		bool BeginNewBatch(string name = "", bool keepOldBatch = false);
		void EndBatch();
		int Undo();
		int Redo();
		bool CanUndo { get; }
		bool CanRedo { get; }
		int PreviousUndoFrame { get; }
		int PreviousRedoFrame { get; }
		int MaxSteps { get; set; }

		void AddGeneralUndo(int first, int last, string name = "", bool force = false);
		void SetGeneralRedo(bool force = false);
		void AddBoolToggle(int frame, string button, bool oldState, string name = "", bool force = false);
		void AddAxisChange(int frame, string button, int oldState, int newState, string name = "", bool force = false);
		void AddMarkerChange(TasMovieMarker newMarker, int oldPosition = -1, string oldMessage = "", string name = "", bool force = false);
		void AddInputBind(int frame, bool isDelete, string name = "", bool force = false);
		void AddInsertFrames(int frame, int count, string name = "", bool force = false);
		void AddInsertInput(int frame, List<string> newInputs, string name = "", bool force = false);
		void AddRemoveFrames(int removeStart, int removeUpTo, List<string> oldInputs, List<TasMovieMarker> removedMarkers, string name = "", bool force = false);
	}

	public class TasMovieChangeLog : IMovieChangeLog
	{
		public TasMovieChangeLog(ITasMovie movie)
		{
			_movie = movie;
		}

		private readonly List<List<IMovieAction>> _history = new List<List<IMovieAction>>();
		private readonly ITasMovie _movie;

		private int _maxSteps = 1000;
		private int _totalSteps;
		private bool _recordingBatch;

		private List<IMovieAction> LatestBatch
			=> _history[_history.Count - 1];

		public List<string> Names { get; } = new List<string>();
		public int UndoIndex { get; private set; } = -1;

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

		/// <summary>
		/// Gets or sets a value indicating whether the movie is recording action history.
		/// This is not intended to turn off the ChangeLog, but to disable the normal recording process.
		/// Use this to manually control the ChangeLog. (Useful for disabling the ChangeLog during undo/redo).
		/// </summary>
		public bool IsRecording { get; set; } = true;

		public void Clear(int upTo = -1)
		{
			if (upTo == -1)
			{
				upTo = _history.Count;
			}

			_history.RemoveRange(0, upTo);
			Names.RemoveRange(0, upTo);
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
			Names.RemoveRange(from, Names.Count - from);

			if (UndoIndex < _history.Count - 1)
			{
				UndoIndex = _history.Count - 1;
			}

			if (_recordingBatch)
			{
				_recordingBatch = false;
				BeginNewBatch();
			}
		}

		/// <summary>
		/// All changes made between calling Begin and End will be one Undo.
		/// If already recording in a batch, calls EndBatch.
		/// </summary>
		/// <param name="name">The name of the batch</param>
		/// <param name="keepOldBatch">If set and a batch is in progress, a new batch will not be created.</param>
		/// <returns>Returns true if a new batch was started; otherwise false.</returns>
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
				ret = AddMovieAction(name);
			}

			_recordingBatch = true;

			return ret;
		}

		/// <summary>
		/// Ends the current undo batch. Future changes will be one undo each.
		/// If not already recording a batch, does nothing.
		/// </summary>
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
				Names.RemoveAt(Names.Count - 1);
				UndoIndex--;
			}
			else
			{
				last.Capacity = last.Count;
			}
		}

		/// <summary>
		/// Undoes the most recent action batch, if any exist.
		/// </summary>
		/// <returns>Returns the frame which the movie needs to rewind to.</returns>
		public int Undo()
		{
			if (UndoIndex == -1)
			{
				return _movie.InputLogLength;
			}

			List<IMovieAction> batch = _history[UndoIndex];
			for (int i = batch.Count - 1; i >= 0; i--)
			{
				batch[i].Undo(_movie);
			}

			UndoIndex--;

			_recordingBatch = false;
			return batch.TrueForAll(static a => a is MovieActionMarker) ? _movie.InputLogLength : PreviousUndoFrame;
		}

		/// <summary>
		/// Redoes the most recent undo, if any exist.
		/// </summary>
		/// <returns>Returns the frame which the movie needs to rewind to.</returns>
		public int Redo()
		{
			if (UndoIndex == _history.Count - 1)
			{
				return _movie.InputLogLength;
			}

			UndoIndex++;
			List<IMovieAction> batch = _history[UndoIndex];
			foreach (IMovieAction b in batch)
			{
				b.Redo(_movie);
			}

			_recordingBatch = false;
			return batch.TrueForAll(static a => a is MovieActionMarker) ? _movie.InputLogLength : PreviousRedoFrame;
		}

		public bool CanUndo => UndoIndex > -1;
		public bool CanRedo => UndoIndex < _history.Count - 1;

		public string NextUndoStepName
		{
			get
			{
				if (Names.Count == 0 || UndoIndex < 0)
				{
					return null;
				}

				return Names[UndoIndex];
			}
		}

		public int PreviousUndoFrame
		{
			get
			{
				if (UndoIndex == _history.Count - 1)
				{
					return _movie.InputLogLength;
				}

				if (_history[UndoIndex + 1].Count == 0)
				{
					return _movie.InputLogLength;
				}

				return _history[UndoIndex + 1].Min(a => a.FirstFrame);
			}
		}

		public int PreviousRedoFrame
		{
			get
			{
				if (UndoIndex == -1)
				{
					return _movie.InputLogLength;
				}

				if (_history[UndoIndex].Count == 0)
				{
					return _movie.InputLogLength;
				}

				return _history[UndoIndex].Min(a => a.FirstFrame);
			}
		}

		private bool AddMovieAction(string name)
		{
			if (UndoIndex + 1 != _history.Count)
			{
				TruncateLog(UndoIndex + 1);
			}

			if (name == "")
			{
				name = $"Undo step {_totalSteps}";
			}

			bool ret = false;
			if (!_recordingBatch)
			{
				ret = true;
				_history.Add(new List<IMovieAction>(1));
				Names.Add(name);
				_totalSteps += 1;

				if (_history.Count <= MaxSteps)
				{
					UndoIndex += 1;
				}
				else
				{
					_history.RemoveAt(0);
					Names.RemoveAt(0);
					ret = false;
				}
			}

			return ret;
		}

		// TODO: These probably aren't the best way to handle undo/redo.
		private int _lastGeneral;

		public void AddGeneralUndo(int first, int last, string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieAction(first, last, _movie));
				_lastGeneral = LatestBatch.Count - 1;
			}
		}

		public void SetGeneralRedo(bool force = false)
		{
			if (IsRecording || force)
			{
				((MovieAction) LatestBatch[_lastGeneral]).SetRedoLog(_movie);
			}
		}

		public void AddBoolToggle(int frame, string button, bool oldState, string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieActionFrameEdit(frame, button, oldState, !oldState));
			}
		}

		public void AddAxisChange(int frame, string button, int oldState, int newState, string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieActionFrameEdit(frame, button, oldState, newState));
			}
		}

		public void AddMarkerChange(TasMovieMarker newMarker, int oldPosition = -1, string oldMessage = "", string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				name = oldPosition == -1
					? $"Set Marker at frame {newMarker.Frame}"
					: $"Remove Marker at frame {oldPosition}";

				AddMovieAction(name);
				LatestBatch.Add(new MovieActionMarker(newMarker, oldPosition, oldMessage));
			}
		}

		public void AddInputBind(int frame, bool isDelete, string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieActionBindInput(_movie, frame, isDelete));
			}
		}

		public void AddInsertFrames(int frame, int count, string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieActionInsertFrames(frame, count));
			}
		}

		public void AddInsertInput(int frame, List<string> newInputs, string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieActionInsertFrames(frame, newInputs));
			}
		}

		public void AddRemoveFrames(int removeStart, int removeUpTo, List<string> oldInputs, List<TasMovieMarker> removedMarkers, string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				AddMovieAction(name);
				LatestBatch.Add(new MovieActionRemoveFrames(removeStart, removeUpTo, oldInputs, removedMarkers));
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

		public void SetRedoLog(ITasMovie movie)
		{
			_redoLength = Math.Min(LastFrame + 1, movie.InputLogLength) - FirstFrame;
			_newLog = new List<string>();
			for (int i = 0; i < _redoLength; i++)
			{
				_newLog.Add(movie.GetInputLogEntry(FirstFrame + i));
			}
		}

		public void Undo(ITasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;
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

			movie.ChangeLog.IsRecording = wasRecording;
			movie.BindMarkersToInput = _bindMarkers;
		}

		public void Redo(ITasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;
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

			movie.ChangeLog.IsRecording = wasRecording;
			movie.BindMarkersToInput = _bindMarkers;
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
				_oldMessage = oldMessage == "" ? marker.Message : oldMessage;
				_newMessage = marker.Message;
			}
		}

		public void Undo(ITasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

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

			movie.ChangeLog.IsRecording = wasRecording;
		}

		public void Redo(ITasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

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

			movie.ChangeLog.IsRecording = wasRecording;
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
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

			if (_isAxis)
			{
				movie.SetAxisState(FirstFrame, _buttonName, _oldState);
			}
			else
			{
				movie.SetBoolState(FirstFrame, _buttonName, _oldState == 1);
			}

			movie.ChangeLog.IsRecording = wasRecording;
		}

		public void Redo(ITasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

			if (_isAxis)
			{
				movie.SetAxisState(FirstFrame, _buttonName, _newState);
			}
			else
			{
				movie.SetBoolState(FirstFrame, _buttonName, _newState == 1);
			}

			movie.ChangeLog.IsRecording = wasRecording;
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
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

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

			movie.ChangeLog.IsRecording = wasRecording;
		}

		public void Redo(ITasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

			if (_isAxis)
			{
				movie.SetAxisStates(FirstFrame, LastFrame - FirstFrame + 1, _buttonName, _newState);
			}
			else
			{
				movie.SetBoolStates(FirstFrame, LastFrame - FirstFrame + 1, _buttonName, _newState == 1);
			}

			movie.ChangeLog.IsRecording = wasRecording;
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
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;
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

			movie.ChangeLog.IsRecording = wasRecording;
			movie.BindMarkersToInput = _bindMarkers;
		}

		public void Redo(ITasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;
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

			movie.ChangeLog.IsRecording = wasRecording;
			movie.BindMarkersToInput = _bindMarkers;
		}
	}

	public class MovieActionInsertFrames : IMovieAction
	{
		public int FirstFrame { get; }
		public int LastFrame { get; }
		private readonly int _count;
		private readonly bool _onlyEmpty;

		private readonly List<string> _newInputs;

		public MovieActionInsertFrames(int frame, int count)
		{
			FirstFrame = frame;
			LastFrame = frame + count;
			_count = count;
			_onlyEmpty = true;
		}

		public MovieActionInsertFrames(int frame, List<string> newInputs)
		{
			FirstFrame = frame;
			LastFrame = frame + newInputs.Count;
			_onlyEmpty = false;
			_newInputs = newInputs;
		}
		
		public void Undo(ITasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

			movie.RemoveFrames(FirstFrame, LastFrame);

			movie.ChangeLog.IsRecording = wasRecording;
		}

		public void Redo(ITasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

			if (_onlyEmpty)
			{
				movie.InsertEmptyFrame(FirstFrame, _count);
			}
			else
			{
				movie.InsertInput(FirstFrame, _newInputs);
			}

			movie.ChangeLog.IsRecording = wasRecording;
		}
	}

	public class MovieActionRemoveFrames : IMovieAction
	{
		public int FirstFrame { get; }
		public int LastFrame { get; }

		private readonly List<string> _oldInputs;
		private readonly List<TasMovieMarker> _removedMarkers;

		public MovieActionRemoveFrames(int removeStart, int removeUpTo, List<string> oldInputs, List<TasMovieMarker> removedMarkers)
		{
			FirstFrame = removeStart;
			LastFrame = removeUpTo;
			_oldInputs = oldInputs;
			_removedMarkers = removedMarkers;
		}
		
		public void Undo(ITasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

			movie.InsertInput(FirstFrame, _oldInputs);

			movie.Markers.AddRange(_removedMarkers);

			movie.ChangeLog.IsRecording = wasRecording;
		}

		public void Redo(ITasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

			movie.RemoveFrames(FirstFrame, LastFrame);

			movie.ChangeLog.IsRecording = wasRecording;
		}
	}
}
