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

		/// <summary>Clears the queue used by <see cref="AddMessage"/>. You probably don't want to do this.</summary>
		public void ClearRegularMessages()
			=> _messages.Clear();

		public void AddMessage(string message, int? duration = null)
			=> _messages.Add(new() {
				Message = message,
				ExpireAt = DateTime.Now + TimeSpan.FromSeconds(Math.Max(_config.OSDMessageDuration, duration ?? 0)),
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

		private string InputStrMovie()
		{
			var state = _movieSession.Movie?.GetInputState(_emulator.Frame - 1);
			return state is not null ? MakeStringFor(state) : "";
		}

		private string InputStrCurrent()
			=> MakeStringFor(_movieSession.MovieIn);

		// returns an input string for inputs pressed solely by the sticky controller
		private string InputStrSticky()
			=> MakeStringFor(_movieSession.MovieIn.And(_inputManager.StickyController));

		private static string MakeStringFor(IController controller)
		{
			return Bk2InputDisplayGenerator.Generate(controller);
		}

		private string MakeIntersectImmediatePrevious()
		{
			if (_movieSession.Movie.IsRecording())
			{
				var movieInput = _movieSession.Movie.GetInputState(_emulator.Frame - 1);
				return MakeStringFor(_movieSession.MovieIn.And(movieInput));
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
				if (_movieSession.Movie.IsPlaying())
				{
					var input = InputStrMovie();
					var point = GetCoordinates(g, _config.InputDisplay, input);
					var c = Color.FromArgb(_config.MovieInputColor);
					g.DrawString(input, c, point.X, point.Y);
				}
				else // TODO: message config -- allow setting of "mixed", and "auto"
				{
					var previousColor = _movieSession.Movie.IsRecording() ? Color.FromArgb(_config.LastInputColor) : Color.FromArgb(_config.MovieInputColor);
					var currentColor = Color.FromArgb(_config.MessagesColor);
					var stickyColor = Color.Pink;
					var currentAndPreviousColor = Color.PeachPuff;

					// now, we're going to render these repeatedly, with higher priority draws overwriting all lower priority draws
					// in order of highest priority to lowest, we are effectively displaying (in different colors):
					// 1. currently pressed input that was also pressed on the previous frame (movie active + recording mode only)
					// 2. currently pressed input that is being pressed by sticky autohold or sticky autofire
					// 3. currently pressed input by the user (non-sticky)
					// 4. input that was pressed on the previous frame (movie active only)

					var previousInput = InputStrMovie();
					var currentInput = InputStrCurrent();
					var stickyInput = InputStrSticky();
					var currentAndPreviousInput = MakeIntersectImmediatePrevious();

					// calculate origin for drawing all strings. Mainly relevant when right-anchoring
					var point = GetCoordinates(g, _config.InputDisplay, currentInput);

					// draw previous input first. Currently pressed input will overwrite this
					g.DrawString(previousInput, previousColor, point.X, point.Y);
					// draw all currently pressed input with the current color (including sticky input)
					g.DrawString(currentInput, currentColor, point.X, point.Y);
					// re-draw all currently pressed sticky input with the sticky color
					g.DrawString(stickyInput, stickyColor, point.X, point.Y);
					// re-draw all currently pressed inputs that were also pressed on the previous frame in their own color
					g.DrawString(currentAndPreviousInput, currentAndPreviousColor, point.X, point.Y);
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

				foreach (var sticky in _inputManager.StickyHoldController.CurrentHolds)
				{
					sb.Append(sticky).Append(' ');
				}

				foreach (var autoSticky in _inputManager.StickyAutofireController.CurrentAutofires)
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
