using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Client.Common
{
	public sealed class InputMovieApi : IInputMovie
	{
		private readonly Action<string> _logCallback;

		public InputMovieApi(Action<string> logCallback)
		{
			_logCallback = logCallback;
		}

		public InputMovieApi() : this(Console.WriteLine) {}

		public IList<string> Comments => Global.MovieSession.Movie.NotActive()
			? new List<string>()
			: Global.MovieSession.Movie.Comments.SimpleCopy();

		public string Filename => Global.MovieSession.Movie.Filename;

		public double FramesPerSecond
		{
			get
			{
				var movie = Global.MovieSession.Movie;
				if (movie.NotActive()) return default;
				return new PlatformFrameRates()[
					movie.HeaderEntries[HeaderKeys.PLATFORM],
					movie.HeaderEntries.TryGetValue(HeaderKeys.PAL, out var isPal) && isPal == "1"
				];
			}
		}

		public IDictionary<string, string> Header => Global.MovieSession.Movie.NotActive()
			? new Dictionary<string, string>()
			: Global.MovieSession.Movie.HeaderEntries.SimpleCopy();

		public bool IsLoaded => Global.MovieSession.Movie.IsActive();

		public bool IsReadOnly
		{
			get => Global.MovieSession.ReadOnly;
			set => Global.MovieSession.ReadOnly = value;
		}

		public bool IsRerecordCounting
		{
			get => Global.MovieSession.Movie.IsCountingRerecords;
			set => Global.MovieSession.Movie.IsCountingRerecords = value;
		}

		public double Length => Global.MovieSession.Movie.FrameCount;

		public string Mode => Global.MovieSession.Movie.Mode.ToString().ToUpper();

		public ulong RerecordCount
		{
			get => Global.MovieSession.Movie.Rerecords;
			set => Global.MovieSession.Movie.Rerecords = value;
		}

		public bool StartsFromSaveram => Global.MovieSession.Movie.IsActive() && Global.MovieSession.Movie.StartsFromSaveRam;

		public bool StartsFromSavestate => Global.MovieSession.Movie.IsActive() && Global.MovieSession.Movie.StartsFromSavestate;

		public IList<string> Subtitles => Global.MovieSession.Movie.NotActive()
			? new List<string>()
			: Global.MovieSession.Movie.Subtitles.Select(subtitle => subtitle.ToString()).ToList();

		public IDictionary<string, dynamic> GetInput(int frame, int? controller)
		{
			if (Global.MovieSession.Movie.NotActive())
			{
				_logCallback("No movie loaded");
				return null;
			}
			var adapter = Global.MovieSession.Movie.GetInputState(frame);
			if (adapter == null)
			{
				_logCallback("Can't get input of the last frame of the movie. Use the previous frame");
				return null;
			}
			return adapter.ToDictionary(controller);
		}

		public string GetInputAsMnemonic(int frame)
		{
			if (Global.MovieSession.Movie.NotActive() || frame >= Global.MovieSession.Movie.InputLogLength) return string.Empty;
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.Movie.GetInputState(frame));
			return lg.GenerateLogEntry();
		}

		public void Save(string filename)
		{
			if (Global.MovieSession.Movie.NotActive()) return;
			if (!string.IsNullOrEmpty(filename))
			{
				filename += $".{Global.MovieSession.Movie.PreferredExtension}";
				if (new FileInfo(filename).Exists)
				{
					_logCallback($"File {filename} already exists, will not overwrite");
					return;
				}
				Global.MovieSession.Movie.Filename = filename;
			}
			Global.MovieSession.Movie.Save();
		}

		public void Stop() => Global.MovieSession.Movie.Stop();
	}
}
