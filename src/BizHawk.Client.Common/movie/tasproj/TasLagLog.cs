using System.Collections.Generic;
using System.IO;

using BizHawk.Common.CollectionExtensions;

using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	public class TasLagLog
	{
		private SortedList<int, bool> _lagLog = new();
		private Dictionary<int, bool> _wasLag = new();

		public bool? this[int frame]
		{
			get
			{
				var result = _lagLog.TryGetValue(frame, out var lag);
				return result ? lag : null;
			}

			set
			{
				if (!value.HasValue)
				{
					RemoveLagEntry(frame);
					return;
				}

				_lagLog[frame] = value.Value;
			}
		}

		public void Clear()
		{
			_wasLag.Clear();
			_lagLog.Clear();
		}

		public bool RemoveFrom(int frame)
		{
			// find the index of the first lag log entry with frame number > `frame`
			int startIndex = _lagLog.Keys.LowerBoundBinarySearch(static key => key, frame) + 1;
			if (startIndex >= _lagLog.Count) return false;

			// iterate in reverse to prevent array copies in RemoveAt
			for (int i = _lagLog.Count - 1; i >= startIndex; i--)
			{
				int frameNumber = _lagLog.Keys[i];
				_wasLag[frameNumber] = _lagLog.Values[i];
				_lagLog.RemoveAt(i);
			}

			return true;
		}

		public void RemoveHistoryAt(int frame)
		{
			_wasLag.Remove(frame);
		}

		public void InsertHistoryAt(int frame, bool isLag)
		{
			_lagLog[frame] = isLag;
			_wasLag[frame] = isLag;
		}

		public void Save(TextWriter tw)
		{
			tw.WriteLine(JsonConvert.SerializeObject(_lagLog));
			tw.WriteLine(JsonConvert.SerializeObject(_wasLag));
		}

		public void Load(TextReader tr)
		{
			_lagLog = JsonConvert.DeserializeObject<SortedList<int, bool>>(tr.ReadLine());
			_wasLag = JsonConvert.DeserializeObject<Dictionary<int, bool>>(tr.ReadLine());
		}

		public bool? History(int frame)
		{
			var result = _wasLag.TryGetValue(frame, out var wasLag);
			if (result)
			{
				return wasLag;
			}

			return null;
		}

		public void FromLagLog(TasLagLog log)
		{
			_lagLog = new SortedList<int, bool>(log._lagLog);
			_wasLag = log._wasLag.ToDictionary();
		}

		public void StartFromFrame(int index)
		{
			for (int i = 0; i < index; i++)
			{
				_lagLog.Remove(i);
				_wasLag.Remove(i);
			}
		}

		private void RemoveLagEntry(int frame)
		{
			int index = _lagLog.IndexOfKey(frame);
			if (index >= 0)
			{
				_wasLag[frame] = _lagLog.Values[index];
				_lagLog.RemoveAt(index);
			}
		}
	}
}
