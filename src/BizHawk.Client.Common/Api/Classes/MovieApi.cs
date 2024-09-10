using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class MovieApi : IMovieApi
	{
		private readonly IMainFormForApi _mainForm;

		private readonly IMovieSession _movieSession;

		private readonly Action<string> LogCallback;

		public MovieApi(Action<string> logCallback, IMainFormForApi mainForm, IMovieSession movieSession)
		{
			LogCallback = logCallback;
			_mainForm = mainForm;
			_movieSession = movieSession;
		}

		public bool StartsFromSavestate() => _movieSession.Movie.IsActive() && _movieSession.Movie.StartsFromSavestate;

		public bool StartsFromSaveram() => _movieSession.Movie.IsActive() && _movieSession.Movie.StartsFromSaveRam;

		public IReadOnlyDictionary<string, object> GetInput(int frame, int? controller = null)
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

			return Bk2LogEntryGenerator.GenerateLogEntry(_movieSession.Movie.GetInputState(frame));
		}

		public void Save(string filename)
		{
			if (_movieSession.Movie.NotActive())
			{
				return;
			}

			if (!string.IsNullOrEmpty(filename))
			{
				filename += $".{_movieSession.Movie.PreferredExtension}";
				if (File.Exists(filename))
				{
					LogCallback($"File {filename} already exists, will not overwrite");
					return;
				}
				_movieSession.Movie.Filename = filename;
			}
			_movieSession.Movie.Save();
		}

		public IReadOnlyDictionary<string, string> GetHeader()
			=> _movieSession.Movie.NotActive()
				? new Dictionary<string, string>()
				: _movieSession.Movie.HeaderEntries.ToDictionary();

		public IReadOnlyList<string> GetComments()
			=> _movieSession.Movie.Comments.ToList();

		public IReadOnlyList<string> GetSubtitles()
			=> _movieSession.Movie.Subtitles.Select(static s => s.ToString()).ToList();

		public string Filename() => _movieSession.Movie.Filename;

		public bool GetReadOnly() => _movieSession.ReadOnly;

		public ulong GetRerecordCount() => _movieSession.Movie.Rerecords;

		public bool GetRerecordCounting() => _movieSession.Movie.IsCountingRerecords;

		public bool IsLoaded() => _movieSession.Movie.IsActive();

		public int Length() => _movieSession.Movie.FrameCount;

		public string Mode() => (_movieSession.Movie?.Mode ?? MovieMode.Inactive).ToString().ToUpperInvariant();

		public bool PlayFromStart(string path = "")
		{
			if (string.IsNullOrEmpty(path))
			{
				try
				{
					return _mainForm.RestartMovie();
				}
				catch (Exception e)
				{
					LogCallback($"caught {e.GetType().Name} while trying to restart movie: {e.Message}");
					return false;
				}
			}
			try
			{
				return _mainForm.LoadMovie(filename: path);
			}
			catch (Exception e)
			{
				LogCallback($"caught {e.GetType().Name} while trying to load movie: {e.Message}");
				return false;
			}
		}

		public void SetReadOnly(bool readOnly) => _movieSession.ReadOnly = readOnly;

		public void SetRerecordCount(ulong count) => _movieSession.Movie.Rerecords = count;

		public void SetRerecordCounting(bool counting) => _movieSession.Movie.IsCountingRerecords = counting;

		public void Stop(bool saveChanges = true)
			=> _mainForm.StopMovie(saveChanges: saveChanges);

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
