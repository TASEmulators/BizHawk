using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.Common.InputAdapterExtensions;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// This is an old abstracted rendering class that the OSD system is using to get its work done.
	/// We should probably just use a GuiRenderer (it was designed to do that) although wrapping it with
	/// more information for OSDRendering could be helpful I suppose
	/// </summary>
	public interface IBlitter
	{
		IBlitterFont GetFontType(string fontType);
		void DrawString(string s, IBlitterFont font, Color color, float x, float y);
		SizeF MeasureString(string s, IBlitterFont font);
		Rectangle ClipBounds { get; set; }
	}

	public class UIMessage
	{
		public string Message { get; set; }
		public DateTime ExpireAt { get; set; }
	}

	public class UIDisplay
	{
		public string Message { get; set; }
		public MessagePosition Position { get; set; }
		public Color ForeColor { get; set; }
		public Color BackGround { get; set; }
	}

	public class OSDManager
	{
		public string Fps { get; set; }
		public IBlitterFont MessageFont;

		public void Begin(IBlitter blitter)
		{
			MessageFont = blitter.GetFontType(nameof(MessageFont));
		}

		public Color FixedMessagesColor => Color.FromArgb(GlobalWin.Config.MessagesColor);
		public Color FixedAlertMessageColor => Color.FromArgb(GlobalWin.Config.AlertMessageColor);

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
			if (GlobalWin.MovieSession.Movie.IsFinished())
			{
				var sb = new StringBuilder();
				sb
					.Append(GlobalWin.Emulator.Frame)
					.Append('/')
					.Append(GlobalWin.MovieSession.Movie.FrameCount)
					.Append(" (Finished)");
				return sb.ToString();
			}

			if (GlobalWin.MovieSession.Movie.IsPlayingOrFinished())
			{
				var sb = new StringBuilder();
				sb
					.Append(GlobalWin.Emulator.Frame)
					.Append('/')
					.Append(GlobalWin.MovieSession.Movie.FrameCount);

				return sb.ToString();
			}
			
			return GlobalWin.Emulator.Frame.ToString();
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
			var point = GetCoordinates(g, GlobalWin.Config.Messages, message.Message);
			var y = point.Y + yOffset; // TODO: clean me up
			g.DrawString(message.Message, MessageFont, FixedMessagesColor, point.X, y);
		}

		public void DrawMessages(IBlitter g)
		{
			if (!GlobalWin.Config.DisplayMessages)
			{
				return;
			}

			_messages.RemoveAll(m => DateTime.Now > m.ExpireAt);

			if (_messages.Any())
			{
				if (GlobalWin.Config.StackOSDMessages)
				{
					int line = 1;
					for (int i = _messages.Count - 1; i >= 0; i--, line++)
					{
						int yOffset = (line - 1) * 18;
						if (!GlobalWin.Config.Messages.Anchor.IsTop())
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
			return MakeStringFor(GlobalWin.MovieSession.MovieController);
		}

		public string InputStrImmediate()
		{
			return MakeStringFor(GlobalWin.InputManager.AutofireStickyXorAdapter);
		}

		public string InputPrevious()
		{
			if (GlobalWin.MovieSession.Movie.IsPlayingOrRecording())
			{
				var state = GlobalWin.MovieSession.Movie.GetInputState(GlobalWin.Emulator.Frame - 1);
				if (state != null)
				{
					return MakeStringFor(state);
				}
			}

			return "";
		}

		public string InputStrOrAll()
		{
			IController m = GlobalWin.InputManager.AutofireStickyXorAdapter;

			if (GlobalWin.MovieSession.Movie.IsPlayingOrRecording() && GlobalWin.Emulator.Frame > 0)
			{
				m = m.Or(GlobalWin.MovieSession.Movie.GetInputState(GlobalWin.Emulator.Frame - 1));
			}

			return MakeStringFor(m);
		}

		private string MakeStringFor(IController controller)
		{
			return new Bk2InputDisplayGenerator(GlobalWin.Emulator.SystemId, controller).Generate();
		}

		public string MakeIntersectImmediatePrevious()
		{
			if (GlobalWin.MovieSession.Movie.IsActive())
			{
				var m = GlobalWin.MovieSession.Movie.IsPlayingOrRecording()
					? GlobalWin.MovieSession.Movie.GetInputState(GlobalWin.Emulator.Frame - 1)
					: GlobalWin.MovieSession.MovieController;

				return MakeStringFor(GlobalWin.InputManager.AutofireStickyXorAdapter.And(m));
			}

			return "";
		}

		public string MakeRerecordCount()
		{
			return GlobalWin.MovieSession.Movie.IsActive()
				? GlobalWin.MovieSession.Movie.Rerecords.ToString()
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
			if (GlobalWin.Config.DisplayFrameCounter && !GlobalWin.Game.IsNullInstance())
			{
				string message = MakeFrameCounter();
				var point = GetCoordinates(g, GlobalWin.Config.FrameCounter, message);
				DrawOsdMessage(g, message, Color.FromArgb(GlobalWin.Config.MessagesColor), point.X, point.Y);

				if (GlobalWin.Emulator.CanPollInput() && GlobalWin.Emulator.AsInputPollable().IsLagFrame)
				{
					DrawOsdMessage(g, GlobalWin.Emulator.Frame.ToString(), FixedAlertMessageColor, point.X, point.Y);
				}
			}

			if (GlobalWin.Config.DisplayInput && !GlobalWin.Game.IsNullInstance())
			{
				if (GlobalWin.MovieSession.Movie.IsPlaying()
					|| (GlobalWin.MovieSession.Movie.IsFinished() && GlobalWin.Emulator.Frame == GlobalWin.MovieSession.Movie.InputLogLength)) // Account for the last frame of the movie, the movie state is immediately "Finished" here but we still want to show the input
				{
					var input = InputStrMovie();
					var point = GetCoordinates(g, GlobalWin.Config.InputDisplay, input);
					Color c = Color.FromArgb(GlobalWin.Config.MovieInput);
					g.DrawString(input, MessageFont, c, point.X, point.Y);
				}

				else // TODO: message config -- allow setting of "previous", "mixed", and "auto"
				{
					var previousColor = Color.Orange;
					Color immediateColor = Color.FromArgb(GlobalWin.Config.MessagesColor);
					var autoColor = Color.Pink;
					var changedColor = Color.PeachPuff;

					//we need some kind of string for calculating position when right-anchoring, of something like that
					var bgStr = InputStrOrAll();
					var point = GetCoordinates(g, GlobalWin.Config.InputDisplay, bgStr);

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
					var autoString = MakeStringFor(GlobalWin.InputManager.StickyXorAdapter.Source.Xor(GlobalWin.InputManager.AutofireStickyXorAdapter).And(GlobalWin.InputManager.AutofireStickyXorAdapter));
					g.DrawString(autoString, MessageFont, autoColor, point.X, point.Y);

					//recolor everything that's changed from the previous input
					var immediateOverlay = MakeIntersectImmediatePrevious();
					g.DrawString(immediateOverlay, MessageFont, changedColor, point.X, point.Y);
				}
			}

			if (GlobalWin.MovieSession.MultiTrack.IsActive)
			{
				var point = GetCoordinates(g, GlobalWin.Config.MultitrackRecorder, GlobalWin.MovieSession.MultiTrack.Status);
				DrawOsdMessage(g, GlobalWin.MovieSession.MultiTrack.Status, FixedMessagesColor, point.X, point.Y);
			}

			if (GlobalWin.Config.DisplayFps && Fps != null)
			{
				var point = GetCoordinates(g, GlobalWin.Config.Fps, Fps);
				DrawOsdMessage(g, Fps, FixedMessagesColor, point.X, point.Y);
			}

			if (GlobalWin.Config.DisplayLagCounter && GlobalWin.Emulator.CanPollInput())
			{
				var counter = GlobalWin.Emulator.AsInputPollable().LagCount.ToString();
				var point = GetCoordinates(g, GlobalWin.Config.LagCounter, counter);
				DrawOsdMessage(g, counter, FixedAlertMessageColor, point.X, point.Y);
			}

			if (GlobalWin.Config.DisplayRerecordCount)
			{
				string rerecordCount = MakeRerecordCount();
				var point = GetCoordinates(g, GlobalWin.Config.ReRecordCounter, rerecordCount);
				DrawOsdMessage(g, rerecordCount, FixedMessagesColor, point.X, point.Y);
			}

			if (GlobalWin.InputManager.ClientControls["Autohold"] || GlobalWin.InputManager.ClientControls["Autofire"])
			{
				var sb = new StringBuilder("Held: ");

				foreach (string sticky in GlobalWin.InputManager.StickyXorAdapter.CurrentStickies)
				{
					sb.Append(sticky).Append(' ');
				}

				foreach (string autoSticky in GlobalWin.InputManager.AutofireStickyXorAdapter.CurrentStickies)
				{
					sb
						.Append("Auto-")
						.Append(autoSticky)
						.Append(' ');
				}

				var message = sb.ToString();
				var point = GetCoordinates(g, GlobalWin.Config.Autohold, message);
				g.DrawString(message, MessageFont, Color.White, point.X, point.Y);
			}

			if (GlobalWin.MovieSession.Movie.IsActive() && GlobalWin.Config.DisplaySubtitles)
			{
				var subList = GlobalWin.MovieSession.Movie.Subtitles.GetSubtitles(GlobalWin.Emulator.Frame);

				foreach (var sub in subList)
				{
					DrawOsdMessage(g, sub.Message, Color.FromArgb((int)sub.Color), sub.X, sub.Y);
				}
			}
		}
	}
}