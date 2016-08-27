using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Represents a TasMovie Marker
	/// A marker is a tagged frame with a message
	/// </summary>
	public class TasMovieMarker
	{
		public TasMovieMarker(int frame, string message = "")
		{
			Frame = frame;
			Message = message;
		}

		/// <summary>
		/// Parses a Marker from a line of text
		/// </summary>
		public TasMovieMarker(string line)
		{
			var split = line.Split('\t');
			Frame = int.Parse(split[0]);
			Message = split[1];
		}

		public virtual int Frame { get; private set; }

		public virtual string Message { get; set; }

		public override string ToString()
		{
			return Frame.ToString() + '\t' + Message;
		}

		public override int GetHashCode()
		{
			return this.Frame.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			else if (obj is TasMovieMarker)
			{
				return this.Frame == (obj as TasMovieMarker).Frame;
			}
			else
			{
				return false;
			}
		}

		public static bool operator ==(TasMovieMarker marker, int frame)
		{
			return marker.Frame == frame;
		}

		public static bool operator !=(TasMovieMarker marker, int frame)
		{
			return marker.Frame != frame;
		}
	}

	public class TasMovieMarkerList : List<TasMovieMarker>
	{
		private readonly TasMovie _movie;
		
		public TasMovieMarkerList(TasMovie movie)
		{
			_movie = movie;
		}

		public TasMovieMarkerList DeepClone()
		{
			TasMovieMarkerList ret = new TasMovieMarkerList(_movie);
			for (int i = 0; i < this.Count; i++)
				ret.Add(new TasMovieMarker(this[i].Frame, this[i].Message));

			return ret;
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private void OnListChanged(NotifyCollectionChangedAction action)
		{
			if (CollectionChanged != null)
			{
				//TODO Allow different types
				CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (var marker in this)
			{
				sb.AppendLine(marker.ToString());
			}

			return sb.ToString();
		}

		// the inherited one
		public new void Add(TasMovieMarker item)
		{
			Add(item, false);
		}

		public void Add(TasMovieMarker item, bool fromHistory)
		{
			var existingItem = this.FirstOrDefault(m => m.Frame == item.Frame);
			if (existingItem != null)
			{
				if (existingItem.Message != item.Message)
				{
					if (!fromHistory)
						_movie.ChangeLog.AddMarkerChange(item, item.Frame, existingItem.Message);
					existingItem.Message = item.Message;
					OnListChanged(NotifyCollectionChangedAction.Replace);
				}
			}
			else
			{
				if (!fromHistory)
					_movie.ChangeLog.AddMarkerChange(item);
				base.Add(item);
				this.Sort((m1, m2) => m1.Frame.CompareTo(m2.Frame));
				OnListChanged(NotifyCollectionChangedAction.Add);
			}
		}

		public void Add(int frame, string message, bool fromHistory = false)
		{
			Add(new TasMovieMarker(frame, message), fromHistory);
		}

		public new void AddRange(IEnumerable<TasMovieMarker> collection)
		{
			bool endBatch = _movie.ChangeLog.BeginNewBatch("Add Markers", true);
			foreach (TasMovieMarker m in collection)
			{
				Add(m);
			}
			if (endBatch)
				_movie.ChangeLog.EndBatch();
		}

		// the inherited one
		public new void Insert(int index, TasMovieMarker item)
		{
			Insert(index, item, false);
		}

		public void Insert(int index, TasMovieMarker item, bool fromHistory)
		{
			if (!fromHistory)
				_movie.ChangeLog.AddMarkerChange(item);
			base.Insert(index, item);
			this.Sort((m1, m2) => m1.Frame.CompareTo(m2.Frame));
			OnListChanged(NotifyCollectionChangedAction.Add);
		}

		public new void InsertRange(int index, IEnumerable<TasMovieMarker> collection)
		{
			bool endBatch = _movie.ChangeLog.BeginNewBatch("Add Markers", true);
			foreach (TasMovieMarker m in collection)
				_movie.ChangeLog.AddMarkerChange(m);
			if (endBatch)
				_movie.ChangeLog.EndBatch();

			base.InsertRange(index, collection);
			this.Sort((m1, m2) => m1.Frame.CompareTo(m2.Frame));
			OnListChanged(NotifyCollectionChangedAction.Add);
		}

		// the inherited one
		public new void Remove(TasMovieMarker item)
		{
			Remove(item, false);
		}

		public void Remove(TasMovieMarker item, bool fromHistory)
		{
			if (item == null || item.Frame == 0) // TODO: Don't do this.
				return;
			if (!fromHistory)
				_movie.ChangeLog.AddMarkerChange(null, item.Frame, item.Message);
			base.Remove(item);
			OnListChanged(NotifyCollectionChangedAction.Remove);
		}

		public new int RemoveAll(Predicate<TasMovieMarker> match)
		{
			bool endBatch = _movie.ChangeLog.BeginNewBatch("Remove All Markers", true);
			foreach (TasMovieMarker m in this)
			{
				if (match.Invoke(m))
					_movie.ChangeLog.AddMarkerChange(null, m.Frame, m.Message);
			}
			if (endBatch)
				_movie.ChangeLog.EndBatch();

			int removeCount = base.RemoveAll(match);
			if (removeCount > 0)
			{
				OnListChanged(NotifyCollectionChangedAction.Remove);
			}
			return removeCount;
		}

		public void Move(int fromFrame, int toFrame, bool fromHistory = false)
		{
			if (fromFrame == 0) // no thanks!
				return;
			TasMovieMarker m = Get(fromFrame);
			if (m == null) // TODO: Don't do this.
				return;
			_movie.ChangeLog.AddMarkerChange(m, m.Frame);
			Insert(0, new TasMovieMarker(toFrame, m.Message), fromHistory);
			Remove(m, fromHistory);
		}

		/// <summary>
		/// Deletes all markers at or below the given start frame.
		/// </summary>
		/// <param name="startFrame">The first frame for markers to be deleted.</param>
		/// <returns>Number of markers deleted.</returns>
		public int TruncateAt(int startFrame)
		{
			int deletedCount = 0;
			bool endBatch = _movie.ChangeLog.BeginNewBatch("Truncate Markers", true);
			for (int i = Count - 1; i > -1; i--)
			{
				if (this[i].Frame >= startFrame)
				{
					if (i == 0)
						continue;
					_movie.ChangeLog.AddMarkerChange(null, this[i].Frame, this[i].Message);
					RemoveAt(i);
					deletedCount++;
				}
			}
			if (endBatch)
				_movie.ChangeLog.EndBatch();
			if (deletedCount > 0)
			{
				OnListChanged(NotifyCollectionChangedAction.Remove);
			}
			return deletedCount;
		}

		public TasMovieMarker Previous(int currentFrame)
		{
			return this
				.Where(m => m.Frame < currentFrame)
				.OrderBy(m => m.Frame)
				.LastOrDefault();
		}

		public TasMovieMarker PreviousOrCurrent(int currentFrame)
		{
			return this
				.Where(m => m.Frame <= currentFrame)
				.OrderBy(m => m.Frame)
				.LastOrDefault();
		}

		public TasMovieMarker Next(int currentFrame)
		{
			return this
				.Where(m => m.Frame > currentFrame)
				.OrderBy(m => m.Frame)
				.FirstOrDefault();
		}

		public bool IsMarker(int frame)
		{
			return this.Any(m => m == frame);
		}

		public TasMovieMarker Get(int frame)
		{
			return this.FirstOrDefault(m => m == frame);
		}
	}
}
