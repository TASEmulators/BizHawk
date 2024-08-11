using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class OSDManager
	{
		private Config _config;

		private IEmulator _emulator;

		private readonly InputManager _inputManager;

		private readonly IMovieSession _movieSession;

		public OSDManager(Config config, IEmulator emulator, InputManager inputManager, IMovieSession movieSession)
		{
			_config = config;
			_emulator = emulator;
			_inputManager = inputManager;
			_movieSession = movieSession;
		}

		public void UpdateGlobals(Config config, IEmulator emulator)
		{
			_config = config;
			_emulator = emulator;
		}

		public string Fps { get; set; }

		public Color FixedMessagesColor => Color.FromArgb(_config.MessagesColor);
		public Color FixedAlertMessageColor => Color.FromArgb(_config.AlertMessageColor);



		private static PointF GetCoordinates(IBlitter g, MessagePosition position, string message)
		{
			var size = g.MeasureString(message);
			var x = position.Anchor.IsLeft()
				? position.X * g.Scale
				: g.ClipBounds.Width - position.X * g.Scale - size.Width;

			var y = position.Anchor.IsTop()
				? position.Y * g.Scale
				: g.ClipBounds.Height - position.Y * g.Scale - size.Height;

			return new PointF(x, y);
		}

		private string MakeFrameCounter()
		{
			if (_movieSession.Movie.IsFinished())
			{
				var sb = new StringBuilder();
				sb
					.Append(_emulator.Frame)
					.Append('/')
					.Append(_movieSession.Movie.FrameCount)
					.Append(" (Finished)");
				return sb.ToString();
			}

			if (_movieSession.Movie.IsPlayingOrFinished())
			{
				var sb = new StringBuilder();
				sb
					.Append(_emulator.Frame)
					.Append('/')
					.Append(_movieSession.Movie.FrameCount);

				return sb.ToString();
			}
			
			return _emulator.Frame.ToString();
		}

		private readonly List<UIMessage> _messages = new(5);
		private readonly List<UIDisplay> _guiTextList = [ ];
		private readonly List<UIDisplay> _ramWatchList = [ ];

		public void AddMessage(string message, int? duration = null)
			=> _messages.Add(new() {
				Message = message,
				ExpireAt = DateTime.Now + TimeSpan.FromSeconds(duration ?? _config.OSDMessageDuration),
			});

		public void ClearRamWatches()
			=> _ramWatchList.Clear();

		public void AddRamWatch(string message, MessagePosition pos, Color backGround, Color foreColor)
		{
			_ramWatchList.Add(new UIDisplay
			{
				Message = message,
				Position = pos,
				BackGround = backGround,
				ForeColor = foreColor
			});
		}

		public void AddGuiText(string message, MessagePosition pos, Color backGround, Color foreColor)
		{
			_guiTextList.Add(new UIDisplay
			{
				Message = message,
				Position = pos,
				BackGround = backGround,
				ForeColor = foreColor
			});
		}

		public void ClearGuiText()
			=> _guiTextList.Clear();

		private void DrawMessage(IBlitter g, UIMessage message, int yOffset)
		{
			var point = GetCoordinates(g, _config.Messages, message.Message);
			var y = point.Y + yOffset; // TODO: clean me up
			g.DrawString(message.Message, FixedMessagesColor, point.X, y);
		}

		public void DrawMessages(IBlitter g)
		{
			if (!_config.DisplayMessages)
			{
				return;
			}

			_messages.RemoveAll(m => DateTime.Now > m.ExpireAt);

			if (_messages.Any())
			{
				if (_config.StackOSDMessages)
				{
					var line = 1;
					for (var i = _messages.Count - 1; i >= 0; i--, line++)
					{
						var yOffset = (int)Math.Round((line - 1) * 18 * g.Scale);
						if (!_config.Messages.Anchor.IsTop())
						{
							yOffset = 0 - yOffset;
						}

						DrawMessage(g, _messages[i], yOffset);
					}
				}
				else
				{
					var message = _messages[^1];
					DrawMessage(g, message, 0);
				}
			}

			foreach (var text in _guiTextList.Concat(_ramWatchList))
			{
				try
				{
					var point = GetCoordinates(g, text.Position, text.Message);
					if (point.Y >= g.ClipBounds.Height) continue; // simple optimisation; don't bother drawing off-screen
					g.DrawString(text.Message, text.ForeColor, point.X, point.Y);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public string InputStrMovie()
			=> MakeStringFor(_movieSession.MovieController, cache: true);

		public string InputStrImmediate()
			=> MakeStringFor(_inputManager.AutofireStickyXorAdapter, cache: true);

		public string InputPrevious()
		{
			if (_movieSession.Movie.IsPlayingOrRecording())
			{
				var state = _movieSession.Movie.GetInputState(_emulator.Frame - 1);
				if (state != null)
				{
					return MakeStringFor(state);
				}
			}

			return "";
		}

		public string InputStrOrAll()
			=> _movieSession.Movie.IsPlayingOrRecording() && _emulator.Frame > 0
				? MakeStringFor(_inputManager.AutofireStickyXorAdapter.Or(_movieSession.Movie.GetInputState(_emulator.Frame - 1)))
				: InputStrImmediate();

		private string MakeStringFor(IController controller, bool cache = false)
		{
			var idg = controller.InputDisplayGenerator;
			if (idg is null)
			{
				idg = new Bk2InputDisplayGenerator(_emulator.SystemId, controller);
				if (cache) controller.InputDisplayGenerator = idg;
			}
			return idg.Generate();
		}

		public string MakeIntersectImmediatePrevious()
		{
			if (_movieSession.Movie.IsActive())
			{
				var m = _movieSession.Movie.IsPlayingOrRecording()
					? _movieSession.Movie.GetInputState(_emulator.Frame - 1)
					: _movieSession.MovieController;

				return MakeStringFor(_inputManager.AutofireStickyXorAdapter.And(m));
			}

			return "";
		}

		public string MakeRerecordCount()
		{
			return _movieSession.Movie.IsActive()
				? _movieSession.Movie.Rerecords.ToString()
				: "";
		}

		private static void DrawOsdMessage(IBlitter g, string message, Color color, float x, float y)
			=> g.DrawString(message, color, x, y);

		/// <summary>
		/// Display all screen info objects like fps, frame counter, lag counter, and input display
		/// </summary>
		public void DrawScreenInfo(IBlitter g)
		{
			if (_config.DisplayFrameCounter && !_emulator.IsNull())
			{
				var message = MakeFrameCounter();
				var point = GetCoordinates(g, _config.FrameCounter, message);
				DrawOsdMessage(g, message, Color.FromArgb(_config.MessagesColor), point.X, point.Y);

				if (_emulator.CanPollInput() && _emulator.AsInputPollable().IsLagFrame)
				{
					DrawOsdMessage(g, _emulator.Frame.ToString(), FixedAlertMessageColor, point.X, point.Y);
				}
			}

			if (_config.DisplayInput)
			{
				var moviePlaying = _movieSession.Movie.IsPlaying();
				// After the last frame of the movie, we want both the last movie input and the current inputs.
				var atMovieEnd = _movieSession.Movie.IsFinished() && _movieSession.Movie.IsAtEnd();
				if (moviePlaying || atMovieEnd)
				{
					var input = InputStrMovie();
					var point = GetCoordinates(g, _config.InputDisplay, input);
					var c = Color.FromArgb(_config.MovieInput);
					g.DrawString(input, c, point.X, point.Y);
				}

				if (!moviePlaying) // TODO: message config -- allow setting of "mixed", and "auto"
				{
					var previousColor = Color.FromArgb(_config.LastInputColor);
					var immediateColor = Color.FromArgb(_config.MessagesColor);
					var autoColor = Color.Pink;
					var changedColor = Color.PeachPuff;

					//we need some kind of string for calculating position when right-anchoring, of something like that
					var bgStr = InputStrOrAll();
					var point = GetCoordinates(g, _config.InputDisplay, bgStr);

					// now, we're going to render these repeatedly, with higher-priority things overriding

					// first display previous frame's input.
					// note: that's only available in case we're working on a movie
					var previousStr = InputPrevious();
					g.DrawString(previousStr, previousColor, point.X, point.Y);

					// next, draw the immediate input.
					// that is, whatever is being held down interactively right this moment even if the game is paused
					// this includes things held down due to autohold or autofire
					// I know, this is all really confusing
					var immediate = InputStrImmediate();
					g.DrawString(immediate, immediateColor, point.X, point.Y);

					// next draw anything that's pressed because it's sticky.
					// this applies to autofire and autohold both. somehow. I don't understand it.
					// basically we're tinting whatever is pressed because it's sticky specially
					// in order to achieve this we want to avoid drawing anything pink that isn't actually held down right now
					// so we make an AND adapter and combine it using immediate & sticky
					// (adapter creation moved to InputManager)
					var autoString = MakeStringFor(_inputManager.WeirdStickyControllerForInputDisplay, cache: true);
					g.DrawString(autoString, autoColor, point.X, point.Y);

					//recolor everything that's changed from the previous input
					var immediateOverlay = MakeIntersectImmediatePrevious();
					g.DrawString(immediateOverlay, changedColor, point.X, point.Y);
				}
			}

			if (_config.DisplayFps && Fps != null)
			{
				var point = GetCoordinates(g, _config.Fps, Fps);
				DrawOsdMessage(g, Fps, FixedMessagesColor, point.X, point.Y);
			}

			if (_config.DisplayLagCounter && _emulator.CanPollInput())
			{
				var counter = _emulator.AsInputPollable().LagCount.ToString();
				var point = GetCoordinates(g, _config.LagCounter, counter);
				DrawOsdMessage(g, counter, FixedAlertMessageColor, point.X, point.Y);
			}

			if (_config.DisplayRerecordCount)
			{
				var rerecordCount = MakeRerecordCount();
				var point = GetCoordinates(g, _config.ReRecordCounter, rerecordCount);
				DrawOsdMessage(g, rerecordCount, FixedMessagesColor, point.X, point.Y);
			}

			if (_inputManager.ClientControls["Autohold"] || _inputManager.ClientControls["Autofire"])
			{
				var sb = new StringBuilder("Held: ");

				foreach (var sticky in _inputManager.StickyXorAdapter.CurrentStickies)
				{
					sb.Append(sticky).Append(' ');
				}

				foreach (var autoSticky in _inputManager.AutofireStickyXorAdapter.CurrentStickies)
				{
					sb
						.Append("Auto-")
						.Append(autoSticky)
						.Append(' ');
				}

				var message = sb.ToString();
				var point = GetCoordinates(g, _config.Autohold, message);
				g.DrawString(message, Color.White, point.X, point.Y);
			}

			if (_movieSession.Movie.IsActive() && _config.DisplaySubtitles)
			{
				var subList = _movieSession.Movie.Subtitles.GetSubtitles(_emulator.Frame);

				foreach (var sub in subList)
				{
					DrawOsdMessage(g, sub.Message, Color.FromArgb((int)sub.Color), sub.X, sub.Y);
				}
			}
		}
	}
}
