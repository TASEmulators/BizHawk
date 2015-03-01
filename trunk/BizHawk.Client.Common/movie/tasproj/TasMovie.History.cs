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
		int UndoIndex = -1;

		private bool RecordingBatch = false;
		public bool AutoRecord = true;

		public TasMovie Movie;
		public TasMovieChangeLog(TasMovie movie)
		{
			History = new List<List<IMovieAction>>();
			Movie = movie;
		}

		public void ClearLog(int upTo = -1)
		{
			if (upTo == -1)
				upTo = History.Count;

			History.RemoveRange(0, upTo);
			UndoIndex -= upTo;
			if (UndoIndex < -1)
				UndoIndex = -1;

			if (History.Count == 0)
				RecordingBatch = false;
		}
		private void TruncateLog(int from)
		{
			UndoIndex -= History.Count - from;
			if (UndoIndex < -1)
				UndoIndex = -1;

			History.RemoveRange(from, History.Count - from);
			RecordingBatch = false;
		}

		/// <summary>
		/// All changes made between calling Begin and End will be one Undo.
		/// If already recording in a batch, calls EndBatch.
		/// </summary>
		public void BeginNewBatch()
		{
			if (RecordingBatch)
				EndBatch();

			RecordingBatch = true;
			History.Add(new List<IMovieAction>());
		}
		/// <summary>
		/// Ends the current undo batch. Future changes will be one undo each.
		/// If not already recording a batch, does (essentially) nothing.
		/// </summary>
		public void EndBatch()
		{
			RecordingBatch = false;
			List<IMovieAction> last = History.Last();
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

			return PreviousUndoFrame;
		}

		public bool CanUndo	{ get { return UndoIndex > -1; } }
		public bool CanRedo	{ get { return UndoIndex < History.Count - 1; } }

		public int PreviousUndoFrame
		{
			get
			{
				if (UndoIndex == History.Count - 1)
					return Movie.InputLogLength;

				if (History[UndoIndex + 1].Count == 0)
					return Movie.InputLogLength;

				return History[UndoIndex + 1].Max(a => a.FirstFrame);
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

				return History[UndoIndex].Max(a => a.FirstFrame);
			}
		}

		#region "Change History"
		private bool AddMovieAction()
		{
			if (!AutoRecord)
				return false;

			if (UndoIndex + 1 != History.Count)
				TruncateLog(UndoIndex + 1);

			if (!RecordingBatch)
			{
				History.Add(new List<IMovieAction>(1));
				UndoIndex += 1;
			}

			return true;
		}

		// TODO: These probably aren't the best way to handle undo/redo.
		public void AddGeneralUndo(int first, int last)
		{
			if (AutoRecord)
			{
				AddMovieAction();
				History.Last().Add(new MovieAction(first, last, Movie));
			}
		}
		public void SetGeneralRedo()
		{
			if (AutoRecord)
			{
				(History.Last().Last() as MovieAction).SetRedoLog(Movie);
			}
		}

		public void AddBoolToggle(int frame, string button, bool oldState)
		{
			if (AutoRecord)
			{
				AddMovieAction();
				History.Last().Add(new MovieActionFrameEdit(frame, button, oldState, !oldState));
			}
		}

		public void AddFloatChange(int frame, string button, float oldState, float newState)
		{
			if (AutoRecord)
			{
				AddMovieAction();
				History.Last().Add(new MovieActionFrameEdit(frame, button, oldState, newState));
			}
		}

		public void AddMarkerChange(TasMovieMarker newMarker, int oldPosition = -1, string old_message = "")
		{
			if (AutoRecord)
			{
				AddMovieAction();
				History.Last().Add(new MovieActionMarker(newMarker, oldPosition, old_message));
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

		public MovieAction(int firstFrame, int lastFrame, TasMovie movie)
		{
			FirstFrame = firstFrame;
			LastFrame = lastFrame;
			oldLog = new List<string>(length);

			undoLength = Math.Min(lastFrame + 1, movie.InputLogLength) - firstFrame;
			for (int i = 0; i < undoLength; i++)
				oldLog.Add(movie.GetLogEntries()[FirstFrame + i]);
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
			bool wasRecording = movie.ChangeLog.AutoRecord;
			movie.ChangeLog.AutoRecord = false;

			if (redoLength != length)
				movie.InsertEmptyFrame(movie.InputLogLength, length - redoLength);

			for (int i = 0; i < undoLength; i++)
				movie.SetFrame(FirstFrame + i, oldLog[i]);

			if (undoLength != length)
				movie.RemoveFrames(FirstFrame + undoLength, movie.InputLogLength);

			movie.ChangeLog.AutoRecord = wasRecording;
		}
		public void Redo(TasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.AutoRecord;
			movie.ChangeLog.AutoRecord = false;

			if (undoLength != length)
				movie.InsertEmptyFrame(movie.InputLogLength, length - undoLength);

			for (int i = 0; i < redoLength; i++)
				movie.SetFrame(FirstFrame + i, newLog[i]);

			if (redoLength != length)
				movie.RemoveFrames(FirstFrame + redoLength, movie.InputLogLength);

			movie.ChangeLog.AutoRecord = wasRecording;
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
				movie.Markers.Remove(movie.Markers.Get(LastFrame));
			else if (LastFrame == -1) // Action: Remove marker
				movie.Markers.Add(FirstFrame, oldMessage);
			else // Action: Move/rename marker
			{
				movie.Markers.Move(LastFrame, FirstFrame);
				movie.Markers.Get(LastFrame).Message = oldMessage;
			}
		}
		public void Redo(TasMovie movie)
		{
			if (FirstFrame == -1) // Action: Place marker
				movie.Markers.Add(LastFrame, oldMessage);
			else if (LastFrame == -1) // Action: Remove marker
				movie.Markers.Remove(movie.Markers.Get(FirstFrame));
			else // Action: Move/rename marker
			{
				movie.Markers.Move(FirstFrame, LastFrame);
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
			FirstFrame = 0;
			buttonName = button;
			isFloat = true;
		}

		public void Undo(TasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.AutoRecord;
			movie.ChangeLog.AutoRecord = false;

			if (isFloat)
				movie.SetFloatState(FirstFrame, buttonName, oldState);
			else
				movie.SetBoolState(FirstFrame, buttonName, oldState == 1);

			movie.ChangeLog.AutoRecord = wasRecording;
		}
		public void Redo(TasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.AutoRecord;
			movie.ChangeLog.AutoRecord = false;

			if (isFloat)
				movie.SetFloatState(FirstFrame, buttonName, newState);
			else
				movie.SetBoolState(FirstFrame, buttonName, newState == 1);

			movie.ChangeLog.AutoRecord = wasRecording;
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
			bool wasRecording = movie.ChangeLog.AutoRecord;
			movie.ChangeLog.AutoRecord = false;

			//if (isFloat)
			//	movie.SetFloatStates(FirstFrame, LastFrame - FirstFrame + 1, buttonName, oldState);
			//else
			//	movie.SetBoolState(FirstFrame, buttonName, oldState == 1);

			movie.ChangeLog.AutoRecord = wasRecording;
		}
		public void Redo(TasMovie movie)
		{
			bool wasRecording = movie.ChangeLog.AutoRecord;
			movie.ChangeLog.AutoRecord = false;

			if (isFloat)
				movie.SetFloatStates(FirstFrame, LastFrame - FirstFrame + 1, buttonName, newState);
			else
				movie.SetBoolStates(FirstFrame, LastFrame - FirstFrame + 1, buttonName, newState == 1);

			movie.ChangeLog.AutoRecord = wasRecording;
		}
	}
	#endregion
}