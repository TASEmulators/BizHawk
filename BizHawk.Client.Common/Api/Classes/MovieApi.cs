using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class MovieApi : IInputMovie
	{
		public MovieApi(Action<string> logCallback)
		{
			LogCallback = logCallback;
		}

		public MovieApi() : this(Console.WriteLine) {}

		private readonly Action<string> LogCallback;

		public bool StartsFromSavestate() => Global.MovieSession.Movie.IsActive() && Global.MovieSession.Movie.StartsFromSavestate;

		public bool StartsFromSaveram() => Global.MovieSession.Movie.IsActive() && Global.MovieSession.Movie.StartsFromSaveRam;

		public IDictionary<string, dynamic> GetInput(int frame, int? controller = null)
		{
			if (Global.MovieSession.Movie.NotActive())
			{
				LogCallback("No movie loaded");
				return null;
			}
			var adapter = Global.MovieSession.Movie.GetInputState(frame);
			if (adapter == null)
			{
				LogCallback("Can't get input of the last frame of the movie. Use the previous frame");
				return null;
			}

			return adapter.ToDictionary(controller);
		}

		public string GetInputAsMnemonic(int frame)
		{
			if (Global.MovieSession.Movie.NotActive() || frame >= Global.MovieSession.Movie.InputLogLength)
			{
				return string.Empty;
			}
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.Movie.GetInputState(frame));
			return lg.GenerateLogEntry();
		}

		public void Save(string filename = null)
		{
			if (Global.MovieSession.Movie.NotActive())
			{
				return;
			}

			if (!string.IsNullOrEmpty(filename))
			{
				filename += $".{Global.MovieSession.Movie.PreferredExtension}";
				if (new FileInfo(filename).Exists)
				{
					LogCallback($"File {filename} already exists, will not overwrite");
					return;
				}
				Global.MovieSession.Movie.Filename = filename;
			}
			Global.MovieSession.Movie.Save();
		}

		public Dictionary<string, string> GetHeader()
		{
			var table = new Dictionary<string, string>();
			if (Global.MovieSession.Movie.NotActive())
			{
				return table;
			}
			foreach (var kvp in Global.MovieSession.Movie.HeaderEntries) table[kvp.Key] = kvp.Value;
			return table;
		}

		public List<string> GetComments()
		{
			var list = new List<string>(Global.MovieSession.Movie.Comments.Count);
			if (Global.MovieSession.Movie.NotActive())
			{
				return list;
			}

			for (var i = 0; i < Global.MovieSession.Movie.Comments.Count; i++) list[i] = Global.MovieSession.Movie.Comments[i];
			return list;
		}

		public List<string> GetSubtitles()
		{
			var list = new List<string>(Global.MovieSession.Movie.Subtitles.Count);
			if (Global.MovieSession.Movie.NotActive())
			{
				return list;
			}

			for (var i = 0; i < Global.MovieSession.Movie.Subtitles.Count; i++) list[i] = Global.MovieSession.Movie.Subtitles[i].ToString();
			return list;
		}

		public string Filename() => Global.MovieSession.Movie.Filename;

		public bool GetReadOnly() => Global.MovieSession.ReadOnly;

		public ulong GetRerecordCount() => Global.MovieSession.Movie.Rerecords;

		public bool GetRerecordCounting() => Global.MovieSession.Movie.IsCountingRerecords;

		public bool IsLoaded() => Global.MovieSession.Movie.IsActive();

		public double Length() => Global.MovieSession.Movie.FrameCount;

		public string Mode() => Global.MovieSession.Movie.Mode.ToString().ToUpper();

		public void SetReadOnly(bool readOnly) => Global.MovieSession.ReadOnly = readOnly;

		public void SetRerecordCount(ulong count) => Global.MovieSession.Movie.Rerecords = count;

		public void SetRerecordCounting(bool counting) => Global.MovieSession.Movie.IsCountingRerecords = counting;

		public void Stop() => Global.MovieSession.Movie.Stop();

		public double GetFps()
		{
			var movie = Global.MovieSession.Movie;
			if (movie.NotActive())
			{
				return default;
			}

			return new PlatformFrameRates()[
				movie.HeaderEntries[HeaderKeys.PLATFORM],
				movie.HeaderEntries.TryGetValue(HeaderKeys.PAL, out var isPal) && isPal == "1"
			];
		}
	}
}
