using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	public class TasLagLog
	{
		private Dictionary<int, bool> _lagLog = new();
		private Dictionary<int, bool> _wasLag = new();

		public bool? this[int frame]
		{
			get
			{
				bool result = _lagLog.TryGetValue(frame, out bool lag);
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
			List<int> frames = _lagLog.Keys.Where(k => k > frame).ToList();
			foreach (int f in frames)
			{
				RemoveLagEntry(f);
			}

			return frames.Any();
		}

		public void RemoveHistoryAt(int frame) => _wasLag.Remove(frame);

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
			_lagLog = JsonConvert.DeserializeObject<Dictionary<int, bool>>(tr.ReadLine());
			_wasLag = JsonConvert.DeserializeObject<Dictionary<int, bool>>(tr.ReadLine());
		}

		public bool? History(int frame)
		{
			bool result = _wasLag.TryGetValue(frame, out bool wasLag);
			if (result)
			{
				return wasLag;
			}

			return null;
		}

		public void FromLagLog(TasLagLog log)
		{
			_lagLog = log._lagLog.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			_wasLag = log._wasLag.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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
			bool result = _lagLog.TryGetValue(frame, out bool lag);
			if (result)
			{
				_wasLag[frame] = lag;
			}

			_lagLog.Remove(frame);
		}
	}
}
