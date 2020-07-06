using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class MovieApi : IMovieApi
	{
		public MovieApi(Action<string> logCallback)
		{
			LogCallback = logCallback;
		}

		public MovieApi() : this(Console.WriteLine) {}

		private readonly Action<string> LogCallback;

		public bool StartsFromSavestate() => GlobalWin.MovieSession.Movie.IsActive() && GlobalWin.MovieSession.Movie.StartsFromSavestate;

		public bool StartsFromSaveram() => GlobalWin.MovieSession.Movie.IsActive() && GlobalWin.MovieSession.Movie.StartsFromSaveRam;

		public IDictionary<string, object> GetInput(int frame, int? controller = null)
		{
			if (GlobalWin.MovieSession.Movie.NotActive())
			{
				LogCallback("No movie loaded");
				return null;
			}
			var adapter = GlobalWin.MovieSession.Movie.GetInputState(frame);
			if (adapter == null)
			{
				LogCallback("Can't get input of the last frame of the movie. Use the previous frame");
				return null;
			}

			return adapter.ToDictionary(controller);
		}

		public string GetInputAsMnemonic(int frame)
		{
			if (GlobalWin.MovieSession.Movie.NotActive() || frame >= GlobalWin.MovieSession.Movie.InputLogLength)
			{
				return string.Empty;
			}

			var lg = GlobalWin.MovieSession.Movie.LogGeneratorInstance(
				GlobalWin.MovieSession.Movie.GetInputState(frame));
			return lg.GenerateLogEntry();
		}

		public void Save(string filename = null)
		{
			if (GlobalWin.MovieSession.Movie.NotActive())
			{
				return;
			}

			if (!string.IsNullOrEmpty(filename))
			{
				filename += $".{GlobalWin.MovieSession.Movie.PreferredExtension}";
				if (new FileInfo(filename).Exists)
				{
					LogCallback($"File {filename} already exists, will not overwrite");
					return;
				}
				GlobalWin.MovieSession.Movie.Filename = filename;
			}
			GlobalWin.MovieSession.Movie.Save();
		}

		public Dictionary<string, string> GetHeader()
		{
			var table = new Dictionary<string, string>();
			if (GlobalWin.MovieSession.Movie.NotActive())
			{
				return table;
			}
			foreach (var kvp in GlobalWin.MovieSession.Movie.HeaderEntries) table[kvp.Key] = kvp.Value;
			return table;
		}

		public List<string> GetComments() => GlobalWin.MovieSession.Movie.Comments.ToList();

		public List<string> GetSubtitles() =>
			GlobalWin.MovieSession.Movie.Subtitles
				.Select(s => s.ToString())
				.ToList();

		public string Filename() => GlobalWin.MovieSession.Movie.Filename;

		public bool GetReadOnly() => GlobalWin.MovieSession.ReadOnly;

		public ulong GetRerecordCount() => GlobalWin.MovieSession.Movie.Rerecords;

		public bool GetRerecordCounting() => GlobalWin.MovieSession.Movie.IsCountingRerecords;

		public bool IsLoaded() => GlobalWin.MovieSession.Movie.IsActive();

		public int Length() => GlobalWin.MovieSession.Movie.FrameCount;

		public string Mode() => GlobalWin.MovieSession.Movie.Mode.ToString().ToUpper();

		public void SetReadOnly(bool readOnly) => GlobalWin.MovieSession.ReadOnly = readOnly;

		public void SetRerecordCount(ulong count) => GlobalWin.MovieSession.Movie.Rerecords = count;

		public void SetRerecordCounting(bool counting) => GlobalWin.MovieSession.Movie.IsCountingRerecords = counting;

		public void Stop() => GlobalWin.MovieSession.StopMovie();

		public double GetFps()
		{
			var movie = GlobalWin.MovieSession.Movie;
			// Why does it need the movie to be active to know the frame rate?
			if (movie.NotActive())
			{
				return default;
			}

			return movie.FrameRate;
		}
	}
}
