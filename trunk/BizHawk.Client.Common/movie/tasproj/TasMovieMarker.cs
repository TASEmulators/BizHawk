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
		private int _frame;

		public TasMovieMarker(int frame, string message = "")
		{
			_frame = frame;
			Message = message;
		}

		/// <summary>
		/// Parses a Marker from a line of text
		/// </summary>
		public TasMovieMarker(string line)
		{
			var split = line.Split('\t');
			_frame = int.Parse(split[0]);
			Message = split[1];
		}

		public virtual int Frame 
		{
			get { return _frame; }
		}

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

		public new void Add(TasMovieMarker item)
		{
			var existingItem = this.FirstOrDefault(m => m.Frame == item.Frame);
			if (existingItem != null)
			{
				if (existingItem.Message != item.Message)
				{
					existingItem.Message = item.Message;
					OnListChanged(NotifyCollectionChangedAction.Replace);
				}
			}
			else
			{
				base.Add(item);
				this.Sort((m1, m2) => m1.Frame.CompareTo(m2.Frame));
				OnListChanged(NotifyCollectionChangedAction.Add);
			}
		}

		public void Add(int frame, string message)
		{
			Add(new TasMovieMarker(frame, message));
		}

		public new void AddRange(IEnumerable<TasMovieMarker> collection)
		{
			foreach(TasMovieMarker m in collection){
				Add(m);
			}
		}

		public new void Insert(int index, TasMovieMarker item)
		{
			base.Insert(index, item);
			this.Sort((m1, m2) => m1.Frame.CompareTo(m2.Frame));
			OnListChanged(NotifyCollectionChangedAction.Add);
		}

		public new void InsertRange(int index, IEnumerable<TasMovieMarker> collection)
		{
			base.InsertRange(index, collection);
			this.Sort((m1, m2) => m1.Frame.CompareTo(m2.Frame));
			OnListChanged(NotifyCollectionChangedAction.Add);
		}

		public new void Remove(TasMovieMarker item)
		{
			base.Remove(item);
			OnListChanged(NotifyCollectionChangedAction.Remove);
		}

		public new int RemoveAll(Predicate<TasMovieMarker> match)
		{
			int removeCount = base.RemoveAll(match);
			if (removeCount > 0)
			{
				OnListChanged(NotifyCollectionChangedAction.Remove);
			}
			return removeCount;
		}

		/// <summary>
		/// Deletes all markers at or below the given start frame.
		/// </summary>
		/// <param name="startFrame">The first frame for markers to be deleted.</param>
		/// <returns>Number of markers deleted.</returns>
		public int TruncateAt(int startFrame)
		{
			int deletedCount = 0;
			for (int i = Count - 1; i > -1; i--)
			{
				if(this[i].Frame >= startFrame){
					RemoveAt(i);
					deletedCount++;
				}
			}
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
	}
}
