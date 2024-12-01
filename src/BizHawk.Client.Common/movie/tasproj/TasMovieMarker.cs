using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using BizHawk.Common.CollectionExtensions;

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
		/// Initializes a new instance of the <see cref="TasMovieMarker"/> class from a line of text
		/// </summary>
		public TasMovieMarker(string line)
		{
			var split = line.Split('\t');
			Frame = int.Parse(split[0]);
			Message = split[1];
		}

		public int Frame { get; private set; }

		public string Message { get; set; }

		public override string ToString() => Frame.ToString() + '\t' + Message;

		public override int GetHashCode() => Frame.GetHashCode();

		public override bool Equals(object obj)
		{
			return obj switch
			{
				null => false,
				TasMovieMarker marker => Frame == marker.Frame,
				_ => false
			};
		}

		public static bool operator ==(TasMovieMarker marker, int frame)
		{
			if (marker == null)
			{
				return false;
			}

			return marker.Frame == frame;
		}

		public static bool operator !=(TasMovieMarker marker, int frame)
		{
			if (marker == null)
			{
				return false;
			}

			return marker.Frame != frame;
		}

		/// <summary>
		/// Shifts the marker's position directly.
		/// Should be used sparingly and only while considering the surrounding frames.
		/// Intended for moving binded markers during frame inserts/deletions.
		/// </summary>
		/// <param name="offset">Amount to shift marker by.</param>
		public void ShiftTo(int offset)
		{
			Frame += offset;
		}
	}

	public class TasMovieMarkerList : List<TasMovieMarker>
	{
		private readonly ITasMovie _movie;
		
		public TasMovieMarkerList(ITasMovie movie)
		{
			_movie = movie;
		}

		public TasMovieMarkerList DeepClone()
		{
			var ret = new TasMovieMarkerList(_movie);
			for (int i = 0; i < Count; i++)
			{
				// used to copy markers between branches
				ret.Add(new TasMovieMarker(this[i].Frame, this[i].Message), skipHistory: true);
			}

			return ret;
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private void OnListChanged(NotifyCollectionChangedAction action)
		{
			// TODO Allow different types
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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

		public void Add(TasMovieMarker item, bool skipHistory)
		{
			var existingItem = Find(m => m.Frame == item.Frame);
			if (existingItem != null)
			{
				if (existingItem.Message != item.Message)
				{
					if (!skipHistory)
					{
						_movie.ChangeLog.AddMarkerChange(item, item.Frame, existingItem.Message);
					}

					existingItem.Message = item.Message;
					OnListChanged(NotifyCollectionChangedAction.Replace);
				}
			}
			else
			{
				if (!skipHistory)
				{
					_movie.ChangeLog.AddMarkerChange(item);
				}

				base.Add(item);
				Sort((m1, m2) => m1.Frame.CompareTo(m2.Frame));
				OnListChanged(NotifyCollectionChangedAction.Add);
			}
		}

		public void Add(int frame, string message)
		{
			Add(new TasMovieMarker(frame, message));
		}

		public new void AddRange(IEnumerable<TasMovieMarker> collection)
		{
			bool endBatch = _movie.ChangeLog.BeginNewBatch("Add Markers", true);
			foreach (TasMovieMarker m in collection)
			{
				Add(m);
			}

			if (endBatch)
			{
				_movie.ChangeLog.EndBatch();
			}
		}

		public new void Insert(int index, TasMovieMarker item)
		{
			_movie.ChangeLog.AddMarkerChange(item);

			base.Insert(index, item);
			Sort((m1, m2) => m1.Frame.CompareTo(m2.Frame));
			OnListChanged(NotifyCollectionChangedAction.Add);
		}

		public new void InsertRange(int index, IEnumerable<TasMovieMarker> collection)
		{
			bool endBatch = _movie.ChangeLog.BeginNewBatch("Add Markers", true);
			foreach (TasMovieMarker m in collection)
			{
				_movie.ChangeLog.AddMarkerChange(m);
			}

			if (endBatch)
			{
				_movie.ChangeLog.EndBatch();
			}

			base.InsertRange(index, collection);
			Sort((m1, m2) => m1.Frame.CompareTo(m2.Frame));
			OnListChanged(NotifyCollectionChangedAction.Add);
		}

		public new void Remove(TasMovieMarker item)
		{
			if (item == null || item.Frame == 0) // TODO: Don't do this.
			{
				return;
			}

			_movie.TasStateManager.EvictReserved(item.Frame);
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
				{
					_movie.ChangeLog.AddMarkerChange(null, m.Frame, m.Message);
					_movie.TasStateManager.EvictReserved(m.Frame);
				}
			}

			if (endBatch)
			{
				_movie.ChangeLog.EndBatch();
			}

			int removeCount = base.RemoveAll(match);
			if (removeCount > 0)
			{
				OnListChanged(NotifyCollectionChangedAction.Remove);
			}

			return removeCount;
		}

		public void Move(int fromFrame, int toFrame)
		{
			if (fromFrame == 0) // no thanks!
			{
				return;
			}

			TasMovieMarker m = Get(fromFrame);
			if (m == null) // TODO: Don't do this.
			{
				return;
			}

			_movie.ChangeLog.AddMarkerChange(m, m.Frame);
			Insert(0, new TasMovieMarker(toFrame, m.Message));
			Remove(m);
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
					{
						continue;
					}

					_movie.ChangeLog.AddMarkerChange(null, this[i].Frame, this[i].Message);
					RemoveAt(i);
					deletedCount++;
				}
			}

			if (endBatch)
			{
				_movie.ChangeLog.EndBatch();
			}

			if (deletedCount > 0)
			{
				OnListChanged(NotifyCollectionChangedAction.Remove);
			}

			return deletedCount;
		}

		public TasMovieMarker Previous(int currentFrame)
		{
			return PreviousOrCurrent(currentFrame - 1);
		}

		public TasMovieMarker PreviousOrCurrent(int currentFrame)
		{
			int lowerBoundIndex = this.LowerBoundBinarySearch(static m => m.Frame, currentFrame);

			return lowerBoundIndex < 0 ? null : this[lowerBoundIndex];
		}

		public TasMovieMarker Next(int currentFrame)
		{
			return this
				.Where(m => m.Frame > currentFrame)
				.OrderBy(m => m.Frame)
				.FirstOrDefault();
		}

		public int FindIndex(string markerName)
		{
			return FindIndex(m => m.Message == markerName);
		}

		public bool IsMarker(int frame)
		{
			// TODO: could use a BinarySearch here, but CollectionExtensions.BinarySearch currently throws
			// an exception on failure, which is probably so expensive it nullifies any performance benefits
			foreach (var marker in this)
			{
				if (marker.Frame > frame) return false;
				if (marker.Frame == frame) return true;
			}

			return false;
		}

		public TasMovieMarker Get(int frame)
		{
			return Find(m => m == frame);
		}
		
		public void ShiftAt(int frame, int offset)
		{
			foreach (var marker in this.Where(m => m.Frame >= frame).ToList())
			{
				marker.ShiftTo(offset);
			}
		}
	}
}
