using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using BizHawk.Bizware.BizwareGL;
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
		public StringRenderer MessageFont;

		public void Begin(IBlitter blitter)
		{
			MessageFont = blitter.GetFontType(nameof(MessageFont));
		}

		public Color FixedMessagesColor => Color.FromArgb(_config.MessagesColor);
		public Color FixedAlertMessageColor => Color.FromArgb(_config.AlertMessageColor);

		private PointF GetCoordinates(IBlitter g, MessagePosition position, string message)
		{
			var size = g.MeasureString(message, MessageFont);
			float x = position.Anchor.IsLeft()
				? position.X
				: g.ClipBounds.Width - position.X - size.Width;

			float y = position.Anchor.IsTop()
				? position.Y
				: g.ClipBounds.Height - position.Y - size.Height;
			

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

		private readonly List<UIMessage> _messages = new List<UIMessage>(5);
		private readonly List<UIDisplay> _guiTextList = new List<UIDisplay>();
		private readonly List<UIDisplay> _ramWatchList = new List<UIDisplay>();

		public void AddMessage(string message)
		{
			_messages.Add(new UIMessage { Message = message, ExpireAt = DateTime.Now + TimeSpan.FromSeconds(2) });
		}

		public void ClearRamWatches()
		{
			_ramWatchList.Clear();
		}

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
		{
			_guiTextList.Clear();
		}

		private void DrawMessage(IBlitter g, UIMessage message, int yOffset)
		{
			var point = GetCoordinates(g, _config.Messages, message.Message);
			var y = point.Y + yOffset; // TODO: clean me up
			g.DrawString(message.Message, MessageFont, FixedMessagesColor, point.X, y);
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
					int line = 1;
					for (int i = _messages.Count - 1; i >= 0; i--, line++)
					{
						int yOffset = (line - 1) * 18;
						if (!_config.Messages.Anchor.IsTop())
						{
							yOffset = 0 - yOffset;
						}

						DrawMessage(g, _messages[i], yOffset);
					}
				}
				else
				{
					var message = _messages.Last();
					DrawMessage(g, message, 0);
				}
			}

			foreach (var text in _guiTextList.Concat(_ramWatchList))
			{
				try
				{
					var point = GetCoordinates(g, text.Position, text.Message);
					g.DrawString(text.Message, MessageFont, text.ForeColor, point.X, point.Y);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public string InputStrMovie()
		{
			return MakeStringFor(_movieSession.MovieController);
		}

		public string InputStrImmediate()
		{
			return MakeStringFor(_inputManager.AutofireStickyXorAdapter);
		}

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
		{
			IController m = _inputManager.AutofireStickyXorAdapter;

			if (_movieSession.Movie.IsPlayingOrRecording() && _emulator.Frame > 0)
			{
				m = m.Or(_movieSession.Movie.GetInputState(_emulator.Frame - 1));
			}

			return MakeStringFor(m);
		}

		private string MakeStringFor(IController controller)
		{
			return new Bk2InputDisplayGenerator(_emulator.SystemId, controller).Generate();
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

		private void DrawOsdMessage(IBlitter g, string message, Color color, float x, float y)
		{
			g.DrawString(message, MessageFont, color, x, y);
		}

		/// <summary>
		/// Display all screen info objects like fps, frame counter, lag counter, and input display
		/// </summary>
		public void DrawScreenInfo(IBlitter g)
		{
			if (_config.DisplayFrameCounter && !_emulator.IsNull())
			{
				string message = MakeFrameCounter();
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
					Color c = Color.FromArgb(_config.MovieInput);
					g.DrawString(input, MessageFont, c, point.X, point.Y);
				}

				if (!moviePlaying) // TODO: message config -- allow setting of "mixed", and "auto"
				{
					var previousColor = Color.FromArgb(_config.LastInputColor);
					Color immediateColor = Color.FromArgb(_config.MessagesColor);
					var autoColor = Color.Pink;
					var changedColor = Color.PeachPuff;

					//we need some kind of string for calculating position when right-anchoring, of something like that
					var bgStr = InputStrOrAll();
					var point = GetCoordinates(g, _config.InputDisplay, bgStr);

					// now, we're going to render these repeatedly, with higher-priority things overriding

					// first display previous frame's input.
					// note: that's only available in case we're working on a movie
					var previousStr = InputPrevious();
					g.DrawString(previousStr, MessageFont, previousColor, point.X, point.Y);

					// next, draw the immediate input.
					// that is, whatever is being held down interactively right this moment even if the game is paused
					// this includes things held down due to autohold or autofire
					// I know, this is all really confusing
					var immediate = InputStrImmediate();
					g.DrawString(immediate, MessageFont, immediateColor, point.X, point.Y);

					// next draw anything that's pressed because it's sticky.
					// this applies to autofire and autohold both. somehow. I don't understand it.
					// basically we're tinting whatever is pressed because it's sticky specially
					// in order to achieve this we want to avoid drawing anything pink that isn't actually held down right now
					// so we make an AND adapter and combine it using immediate & sticky
					var autoString = MakeStringFor(_inputManager.StickyXorAdapter.Source.Xor(_inputManager.AutofireStickyXorAdapter).And(_inputManager.AutofireStickyXorAdapter));
					g.DrawString(autoString, MessageFont, autoColor, point.X, point.Y);

					//recolor everything that's changed from the previous input
					var immediateOverlay = MakeIntersectImmediatePrevious();
					g.DrawString(immediateOverlay, MessageFont, changedColor, point.X, point.Y);
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
				string rerecordCount = MakeRerecordCount();
				var point = GetCoordinates(g, _config.ReRecordCounter, rerecordCount);
				DrawOsdMessage(g, rerecordCount, FixedMessagesColor, point.X, point.Y);
			}

			if (_inputManager.ClientControls["Autohold"] || _inputManager.ClientControls["Autofire"])
			{
				var sb = new StringBuilder("Held: ");

				foreach (string sticky in _inputManager.StickyXorAdapter.CurrentStickies)
				{
					sb.Append(sticky).Append(' ');
				}

				foreach (string autoSticky in _inputManager.AutofireStickyXorAdapter.CurrentStickies)
				{
					sb
						.Append("Auto-")
						.Append(autoSticky)
						.Append(' ');
				}

				var message = sb.ToString();
				var point = GetCoordinates(g, _config.Autohold, message);
				g.DrawString(message, MessageFont, Color.White, point.X, point.Y);
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