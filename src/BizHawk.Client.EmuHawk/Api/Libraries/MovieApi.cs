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
		private readonly IMovieSession _movieSession;

		private readonly Action<string> LogCallback;

		public MovieApi(Action<string> logCallback, InputManager inputManager, IMovieSession movieSession)
		{
			LogCallback = logCallback;
			_movieSession = movieSession;
		}

		public bool StartsFromSavestate() => _movieSession.Movie.IsActive() && _movieSession.Movie.StartsFromSavestate;

		public bool StartsFromSaveram() => _movieSession.Movie.IsActive() && _movieSession.Movie.StartsFromSaveRam;

		public IDictionary<string, object> GetInput(int frame, int? controller = null)
		{
			if (_movieSession.Movie.NotActive())
			{
				LogCallback("No movie loaded");
				return null;
			}
			var adapter = _movieSession.Movie.GetInputState(frame);
			if (adapter == null)
			{
				LogCallback("Can't get input of the last frame of the movie. Use the previous frame");
				return null;
			}

			return adapter.ToDictionary(controller);
		}

		public string GetInputAsMnemonic(int frame)
		{
			if (_movieSession.Movie.NotActive() || frame >= _movieSession.Movie.InputLogLength)
			{
				return string.Empty;
			}

			var lg = _movieSession.Movie.LogGeneratorInstance(
				_movieSession.Movie.GetInputState(frame));
			return lg.GenerateLogEntry();
		}

		public void Save(string filename = null)
		{
			if (_movieSession.Movie.NotActive())
			{
				return;
			}

			if (!string.IsNullOrEmpty(filename))
			{
				filename += $".{_movieSession.Movie.PreferredExtension}";
				if (new FileInfo(filename).Exists)
				{
					LogCallback($"File {filename} already exists, will not overwrite");
					return;
				}
				_movieSession.Movie.Filename = filename;
			}
			_movieSession.Movie.Save();
		}

		public Dictionary<string, string> GetHeader()
		{
			var table = new Dictionary<string, string>();
			if (_movieSession.Movie.NotActive())
			{
				return table;
			}
			foreach (var kvp in _movieSession.Movie.HeaderEntries) table[kvp.Key] = kvp.Value;
			return table;
		}

		public List<string> GetComments() => _movieSession.Movie.Comments.ToList();

		public List<string> GetSubtitles() =>
			_movieSession.Movie.Subtitles
				.Select(s => s.ToString())
				.ToList();

		public string Filename() => _movieSession.Movie.Filename;

		public bool GetReadOnly() => _movieSession.ReadOnly;

		public ulong GetRerecordCount() => _movieSession.Movie.Rerecords;

		public bool GetRerecordCounting() => _movieSession.Movie.IsCountingRerecords;

		public bool IsLoaded() => _movieSession.Movie.IsActive();

		public int Length() => _movieSession.Movie.FrameCount;

		public string Mode() => (_movieSession.Movie?.Mode ?? MovieMode.Inactive).ToString().ToUpper();

		public void SetReadOnly(bool readOnly) => _movieSession.ReadOnly = readOnly;

		public void SetRerecordCount(ulong count) => _movieSession.Movie.Rerecords = count;

		public void SetRerecordCounting(bool counting) => _movieSession.Movie.IsCountingRerecords = counting;

		public void Stop() => _movieSession.StopMovie();

		public double GetFps()
		{
			var movie = _movieSession.Movie;
			// Why does it need the movie to be active to know the frame rate?
			if (movie.NotActive())
			{
				return default;
			}

			return movie.FrameRate;
		}
	}
}
