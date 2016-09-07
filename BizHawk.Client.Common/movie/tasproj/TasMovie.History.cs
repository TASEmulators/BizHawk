using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System;

namespace BizHawk.Client.Common
{
	public class TasMovieChangeLog
	{
		List<List<IMovieAction>> History;
		public List<string> Names;
		public int UndoIndex = -1;
		int _maxSteps = 100;

		public int MaxSteps
		{
			get { return _maxSteps; }
			set
			{
				_maxSteps = value;
				if (History.Count > value)
				{
					if (History.Count <= value)
						ClearLog();
					else
						ClearLog(History.Count - value);
				}
			}
		}

		private int _totalSteps = 0;
		private bool RecordingBatch = false;

		/// <summary>
		/// This is not intended to turn off the ChangeLog, but to disable the normal recording process.
		/// Use this to manually control the ChangeLog. (Useful for when you are making lots of 
		/// </summary>
		public bool IsRecording = true;

		public TasMovie Movie;

		public TasMovieChangeLog(TasMovie movie)
		{
			History = new List<List<IMovieAction>>();
			Names = new List<string>();
			Movie = movie;
		}

		public void ClearLog(int upTo = -1)
		{
			if (upTo == -1)
				upTo = History.Count;

			History.RemoveRange(0, upTo);
			Names.RemoveRange(0, upTo);
			UndoIndex -= upTo;
			if (UndoIndex < -1)
				UndoIndex = -1;

			if (History.Count == 0)
				RecordingBatch = false;
		}

		private void TruncateLog(int from)
		{
			History.RemoveRange(from, History.Count - from);
			Names.RemoveRange(from, Names.Count - from);

			if (UndoIndex < History.Count - 1)
				UndoIndex = History.Count - 1;

			if (RecordingBatch)
			{
				RecordingBatch = false;
				BeginNewBatch();
			}
		}

		/// <summary>
		/// All changes made between calling Begin and End will be one Undo.
		/// If already recording in a batch, calls EndBatch.
		/// </summary>
		/// <param name="keepOldBatch">If set and a batch is in progress, a new batch will not be created.</param>
		/// <returns>Returns true if a new batch was started; otherwise false.</returns>
		public bool BeginNewBatch(string name = "", bool keepOldBatch = false)
		{
			if (!IsRecording)
				return false;

			bool ret = true;
			if (RecordingBatch)
			{
				if (keepOldBatch)
					ret = false;
				else
					EndBatch();
			}

			if (ret)
			{
				ret = AddMovieAction(name);
			}
			RecordingBatch = true;

			return ret;
		}

		/// <summary>
		/// Ends the current undo batch. Future changes will be one undo each.
		/// If not already recording a batch, does nothing.
		/// </summary>
		public void EndBatch()
		{
			if (!IsRecording || !RecordingBatch)
				return;

			RecordingBatch = false;
			List<IMovieAction> last = History.Last();
			if (last.Count == 0) // Remove batch if it's empty.
			{
				History.RemoveAt(History.Count - 1);
				Names.RemoveAt(Names.Count - 1);
				UndoIndex--;
			}
			else
				last.Capacity = last.Count;
		}

		/// <summary>
		/// Undoes the most recent action batch, if any exist.
		/// </summary>
		/// <returns>Returns the frame which the movie needs to rewind to.</returns>
		public int Undo()
		{
			if (UndoIndex == -1)
				return Movie.InputLogLength;

			List<IMovieAction> batch = History[UndoIndex];
			for (int i = batch.Count - 1; i >= 0; i--)
				batch[i].Undo(Movie);
			UndoIndex--;

			RecordingBatch = false;

			if (!batch.Where(a => a.GetType() != typeof(MovieActionMarker)).Any())
				return Movie.InputLogLength;

			return PreviousUndoFrame;
		}

		/// <summary>
		/// Redoes the most recent undo, if any exist.
		/// </summary>
		/// <returns>Returns the frame which the movie needs to rewind to.</returns>
		public int Redo()
		{
			if (UndoIndex == History.Count - 1)
				return Movie.InputLogLength;

			UndoIndex++;
			List<IMovieAction> batch = History[UndoIndex];
			for (int i = 0; i < batch.Count; i++)
				batch[i].Redo(Movie);

			RecordingBatch = false;

			if (!batch.Where(a => a.GetType() != typeof(MovieActionMarker)).Any())
				return Movie.InputLogLength;

			return PreviousRedoFrame;
		}

		public bool CanUndo { get { return UndoIndex > -1; } }
		public bool CanRedo { get { return UndoIndex < History.Count - 1; } }

		public string NextUndoStepName
		{
			get
			{
				if (Names.Count == 0 || UndoIndex < 0)
					return null;
				else
					return Names[UndoIndex];
			}
		}

		public int PreviousUndoFrame
		{
			get
			{
				if (UndoIndex == History.Count - 1)
					return Movie.InputLogLength;

				if (History[UndoIndex + 1].Count == 0)
					return Movie.InputLogLength;

				return History[UndoIndex + 1].Min(a => a.FirstFrame);
			}
		}

		public int PreviousRedoFrame
		{
			get
			{
				if (UndoIndex == -1)
					return Movie.InputLogLength;

				if (History[UndoIndex].Count == 0)
					return Movie.InputLogLength;

				return History[UndoIndex].Min(a => a.FirstFrame);
			}
		}

		#region "Change History"

		private bool AddMovieAction(string name)
		{
			if (UndoIndex + 1 != History.Count)
				TruncateLog(UndoIndex + 1);

			if (name == "")
				name = "Undo step " + _totalSteps;

			bool ret = false;
			if (!RecordingBatch)
			{
				ret = true;
				History.Add(new List<IMovieAction>(1));
				Names.Add(name);
				_totalSteps += 1;

				if (History.Count <= MaxSteps)
					UndoIndex += 1;
				else
				{
					History.RemoveAt(0);
					Names.RemoveAt(0);
					ret = false;
				}
			}

			return ret;
		}

		public void SetName(string name)
		{
			Names[Names.Count - 1] = name;
		}

		// TODO: These probably aren't the best way to handle undo/redo.
		private int lastGeneral;

		public void AddGeneralUndo(int first, int last, string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				AddMovieAction(name);
				History.Last().Add(new MovieAction(first, last, Movie));
				lastGeneral = History.Last().Count - 1;
			}
		}

		public void SetGeneralRedo(bool force = false)
		{
			if (IsRecording || force)
			{
				(History.Last()[lastGeneral] as MovieAction).SetRedoLog(Movie);
			}
		}

		public void AddBoolToggle(int frame, string button, bool oldState, string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				AddMovieAction(name);
				History.Last().Add(new MovieActionFrameEdit(frame, button, oldState, !oldState));
			}
		}

		public void AddFloatChange(int frame, string button, float oldState, float newState, string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				AddMovieAction(name);
				History.Last().Add(new MovieActionFrameEdit(frame, button, oldState, newState));
			}
		}

		public void AddMarkerChange(TasMovieMarker newMarker, int oldPosition = -1, string old_message = "", string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				if (oldPosition == -1)
					name = "Set Marker at frame " + newMarker.Frame;
				else
					name = "Remove Marker at frame " + oldPosition;

				AddMovieAction(name);
				History.Last().Add(new MovieActionMarker(newMarker, oldPosition, old_message));
			}
		}

		public void AddInputBind(int frame, bool isDelete, string name = "", bool force = false)
		{
			if (IsRecording || force)
			{
				AddMovieAction(name);
				History.Last().Add(new MovieActionBindInput(Movie, frame, isDelete));
			}
		}
		#endregion
	}

	#region "Classes"

	public interface IMovieAction
	{
		void Undo(TasMovie movie);
		void Redo(TasMovie movie);

		int FirstFrame { get; }
		int LastFrame { get; }
	}

	public class MovieAction : IMovieAction
	{
		public int FirstFrame { get; private set; }
		public int LastFrame { get; private set; }
		private int undoLength;
		private int redoLength;
		private int length
		{ get { return LastFrame - FirstFrame + 1; } }
		private List<string> oldLog;
		private List<string> newLog;
		private bool bindMarkers;

		public MovieAction(int firstFrame, int lastFrame, TasMovie movie)
		{
			FirstFrame = firstFrame;
			LastFrame = lastFrame;
			oldLog = new List<string>(length);
			undoLength = Math.Min(LastFrame + 1, movie.InputLogLength) - FirstFrame;

			for (int i = 0; i < undoLength; i++)
				oldLog.Add(movie.GetLogEntries()[FirstFrame + i]);

			bindMarkers = movie.BindMarkersToInput;
		}

		public void SetRedoLog(TasMovie movie)
		{
			redoLength = Math.Min(LastFrame + 1, movie.InputLogLength) - FirstFrame;
			newLog = new List<string>();
			for (int i = 0; i < redoLength; i++)
				newLog.Add(movie.GetLogEntries()[FirstFrame + i]);
		}

		public void Undo(TasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			bool wasBinding = movie.BindMarkersToInput;
			movie.ChangeLog.IsRecording = false;
			movie.BindMarkersToInput = bindMarkers;

			if (redoLength != length)
				movie.InsertEmptyFrame(FirstFrame, length - redoLength, true);
			if (undoLength != length)
				movie.RemoveFrames(FirstFrame, movie.InputLogLength - undoLength, true);

			for (int i = 0; i < undoLength; i++)
				movie.SetFrame(FirstFrame + i, oldLog[i]);

			movie.ChangeLog.IsRecording = wasRecording;
			movie.BindMarkersToInput = bindMarkers;
		}

		public void Redo(TasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			bool wasBinding = movie.BindMarkersToInput;
			movie.ChangeLog.IsRecording = false;
			movie.BindMarkersToInput = bindMarkers;

			if (undoLength != length)
				movie.InsertEmptyFrame(FirstFrame, length - undoLength);
			if (redoLength != length)
				movie.RemoveFrames(FirstFrame, movie.InputLogLength - redoLength);

			for (int i = 0; i < redoLength; i++)
				movie.SetFrame(FirstFrame + i, newLog[i]);

			movie.ChangeLog.IsRecording = wasRecording;
			movie.BindMarkersToInput = bindMarkers;
		}
	}

	public class MovieActionMarker : IMovieAction
	{
		public int FirstFrame { get; private set; }
		public int LastFrame { get; private set; }

		private string oldMessage;
		private string newMessage;

		public MovieActionMarker(TasMovieMarker marker, int oldPosition = -1, string old_message = "")
		{
			FirstFrame = oldPosition;
			if (marker == null)
			{
				LastFrame = -1;
				oldMessage = old_message;
			}
			else
			{
				LastFrame = marker.Frame;
				if (old_message == "")
					oldMessage = marker.Message;
				else
					oldMessage = old_message;
				newMessage = marker.Message;
			}
		}

		public void Undo(TasMovie movie)
		{
			if (FirstFrame == -1) // Action: Place marker
				movie.Markers.Remove(movie.Markers.Get(LastFrame), true);
			else if (LastFrame == -1) // Action: Remove marker
				movie.Markers.Add(FirstFrame, oldMessage, true);
			else // Action: Move/rename marker
			{
				movie.Markers.Move(LastFrame, FirstFrame, true);
				movie.Markers.Get(LastFrame).Message = oldMessage;
			}
		}

		public void Redo(TasMovie movie)
		{
			if (FirstFrame == -1) // Action: Place marker
				movie.Markers.Add(LastFrame, oldMessage, true);
			else if (LastFrame == -1) // Action: Remove marker
				movie.Markers.Remove(movie.Markers.Get(FirstFrame), true);
			else // Action: Move/rename marker
			{
				movie.Markers.Move(FirstFrame, LastFrame, true);
				movie.Markers.Get(LastFrame).Message = newMessage;
			}
		}
	}

	public class MovieActionFrameEdit : IMovieAction
	{
		public int FirstFrame { get; private set; }
		public int LastFrame { get { return FirstFrame; } }
		private float oldState, newState;
		private string buttonName;
		private bool isFloat = false;

		public MovieActionFrameEdit(int frame, string button, bool oldS, bool newS)
		{
			oldState = oldS ? 1 : 0;
			newState = newS ? 1 : 0;
			FirstFrame = frame;
			buttonName = button;
		}

		public MovieActionFrameEdit(int frame, string button, float oldS, float newS)
		{
			oldState = oldS;
			newState = newS;
			FirstFrame = frame;
			buttonName = button;
			isFloat = true;
		}

		public void Undo(TasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

			if (isFloat)
				movie.SetFloatState(FirstFrame, buttonName, oldState);
			else
				movie.SetBoolState(FirstFrame, buttonName, oldState == 1);

			movie.ChangeLog.IsRecording = wasRecording;
		}

		public void Redo(TasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

			if (isFloat)
				movie.SetFloatState(FirstFrame, buttonName, newState);
			else
				movie.SetBoolState(FirstFrame, buttonName, newState == 1);

			movie.ChangeLog.IsRecording = wasRecording;
		}
	}

	public class MovieActionPaint : IMovieAction
	{
		public int FirstFrame { get; private set; }
		public int LastFrame { get; private set; }
		private List<float> oldState;
		private float newState;
		private string buttonName;
		private bool isFloat = false;

		public MovieActionPaint(int startFrame, int endFrame, string button, bool newS, TasMovie movie)
		{
			newState = newS ? 1 : 0;
			FirstFrame = startFrame;
			LastFrame = endFrame;
			buttonName = button;
			oldState = new List<float>(endFrame - startFrame + 1);

			for (int i = 0; i < endFrame - startFrame + 1; i++)
				oldState.Add(movie.BoolIsPressed(startFrame + i, button) ? 1 : 0);
		}

		public MovieActionPaint(int startFrame, int endFrame, string button, float newS, TasMovie movie)
		{
			newState = newS;
			FirstFrame = startFrame;
			LastFrame = endFrame;
			buttonName = button;
			isFloat = true;
			oldState = new List<float>(endFrame - startFrame + 1);

			for (int i = 0; i < endFrame - startFrame + 1; i++)
				oldState.Add(movie.BoolIsPressed(startFrame + i, button) ? 1 : 0);
		}

		public void Undo(TasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

			if (isFloat)
			{
				for (int i = 0; i < oldState.Count; i++)
					movie.SetFloatState(FirstFrame + i, buttonName, oldState[i]);
			}
			else
			{
				for (int i = 0; i < oldState.Count; i++)
					movie.SetBoolState(FirstFrame + i, buttonName, oldState[i] == 1);
			}

			movie.ChangeLog.IsRecording = wasRecording;
		}

		public void Redo(TasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			movie.ChangeLog.IsRecording = false;

			if (isFloat)
				movie.SetFloatStates(FirstFrame, LastFrame - FirstFrame + 1, buttonName, newState);
			else
				movie.SetBoolStates(FirstFrame, LastFrame - FirstFrame + 1, buttonName, newState == 1);

			movie.ChangeLog.IsRecording = wasRecording;
		}
	}

	public class MovieActionBindInput : IMovieAction
	{
		public int FirstFrame { get; private set; }
		public int LastFrame { get; private set; }

		private string log;
		private bool delete;

		private bool bindMarkers;

		public MovieActionBindInput(TasMovie movie, int frame, bool isDelete)
		{
			FirstFrame = LastFrame = frame;
			log = movie.GetInputLogEntry(frame);
			delete = isDelete;
			bindMarkers = movie.BindMarkersToInput;
		}

		public void Undo(TasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			bool wasBinding = movie.BindMarkersToInput;
			movie.ChangeLog.IsRecording = false;
			movie.BindMarkersToInput = bindMarkers;

			if (delete) // Insert
			{
				movie.InsertInput(FirstFrame, log);
				movie.InsertLagHistory(FirstFrame + 1, true);
			}
			else // Delete
			{
				movie.RemoveFrame(FirstFrame);
				movie.RemoveLagHistory(FirstFrame + 1);
			}

			movie.ChangeLog.IsRecording = wasRecording;
			movie.BindMarkersToInput = bindMarkers;
		}

		public void Redo(TasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.IsRecording;
			bool wasBinding = movie.BindMarkersToInput;
			movie.ChangeLog.IsRecording = false;
			movie.BindMarkersToInput = bindMarkers;

			if (delete)
			{
				movie.RemoveFrame(FirstFrame);
				movie.RemoveLagHistory(FirstFrame + 1);
			}
			else
			{
				movie.InsertInput(FirstFrame, log);
				movie.InsertLagHistory(FirstFrame + 1, true);
			}

			movie.ChangeLog.IsRecording = wasRecording;
			movie.BindMarkersToInput = bindMarkers;
		}
	}

	#endregion
}