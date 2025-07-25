using System.Collections.Generic;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	internal class FakeMovieSession : IMovieSession
	{
		public IMovieConfig Settings { get; set; }

		public IMovie? Movie { get; set; }

		public bool ReadOnly { get => false; set { } }

		public bool NewMovieQueued => throw new NotImplementedException();

		public string QueuedSyncSettings => throw new NotImplementedException();

		public string QueuedCoreName => throw new NotImplementedException();

		public string QueuedSysID => throw new NotImplementedException();

		public IDictionary<string, object> UserBag { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }


		public IMovieController MovieController { get; }

		public IController StickySource { get; set; }
		public IController MovieIn { get; set; } = NullController.Instance;

		private IInputAdapter _out = new CopyControllerAdapter();
		public IInputAdapter MovieOut
		{
			get
			{
				if (Movie?.IsActive() == true && !Movie.IsRecording())
				{
					_out.Source = MovieController;
				}
				else
				{
					_out.Source = MovieIn;
				}
				return _out;
			}
		}

		public string BackupDirectory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public FakeMovieSession(IEmulator emulator)
		{
			Settings = new MovieConfig()
			{
				DefaultTasStateManagerSettings = new FakeStateManagerSettings(),
			};
			StickySource = new Bk2Controller(emulator.ControllerDefinition);
			MovieController = new Bk2Controller(emulator.ControllerDefinition);
		}

		public void AbortQueuedMovie() => throw new NotImplementedException();
		public bool CheckSavestateTimeline(TextReader reader) => throw new NotImplementedException();
		public IMovieController GenerateMovieController(ControllerDefinition? definition = null, string? logKey = null) => throw new NotImplementedException();
		public IMovie Get(string path, bool loadMovie = false) => throw new NotImplementedException();
		public void HandleFrameAfter(bool ignoreMovieEndAction) => throw new NotImplementedException();
		public void HandleFrameBefore() => throw new NotImplementedException();
		public bool HandleLoadState(TextReader reader) => throw new NotImplementedException();
		public void HandleSaveState(TextWriter writer) => throw new NotImplementedException();
		public void PopupMessage(string message) => throw new NotImplementedException();
		public void QueueNewMovie(IMovie movie, string systemId, string loadedRomHash, PathEntryCollection pathEntries, IDictionary<string, string> preferredCores) => throw new NotImplementedException();
		public void RunQueuedMovie(bool recordMode, IEmulator emulator) => throw new NotImplementedException();
		public void StopMovie(bool saveChanges = true) => Movie?.Stop();
	}
}
