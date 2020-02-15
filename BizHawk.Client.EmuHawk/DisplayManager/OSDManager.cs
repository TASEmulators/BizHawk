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

		public Color FixedMessagesColor => Color.FromArgb(Global.Config.MessagesColor);
		public Color FixedAlertMessageColor => Color.FromArgb(Global.Config.AlertMessageColor);

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
			if (Global.MovieSession.Movie.IsFinished())
			{
				var sb = new StringBuilder();
				sb
					.Append(Global.Emulator.Frame)
					.Append('/')
					.Append(Global.MovieSession.Movie.FrameCount)
					.Append(" (Finished)");
				return sb.ToString();
			}

			if (Global.MovieSession.Movie.IsPlaying())
			{
				var sb = new StringBuilder();
				sb
					.Append(Global.Emulator.Frame)
					.Append('/')
					.Append(Global.MovieSession.Movie.FrameCount);

				return sb.ToString();
			}
			
			return Global.Emulator.Frame.ToString();
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
			var point = GetCoordinates(g, Global.Config.Messages, message.Message);
			var y = point.Y + yOffset; // TODO: clean me up
			g.DrawString(message.Message, MessageFont, FixedMessagesColor, point.X, y);
		}

		public void DrawMessages(IBlitter g)
		{
			if (!Global.Config.DisplayMessages)
			{
				return;
			}

			_messages.RemoveAll(m => DateTime.Now > m.ExpireAt);

			if (_messages.Any())
			{
				if (Global.Config.StackOSDMessages)
				{
					int line = 1;
					for (int i = _messages.Count - 1; i >= 0; i--, line++)
					{
						int yOffset = (line - 1) * 18;
						if (!Global.Config.Messages.Anchor.IsTop())
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
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);

			return lg.GenerateInputDisplay();
		}

		public string InputStrImmediate()
		{
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.AutofireStickyXORAdapter);

			return lg.GenerateInputDisplay();
		}

		public string InputPrevious()
		{
			if (Global.MovieSession.Movie.IsPlayingOrRecording())
			{
				var lg = Global.MovieSession.LogGeneratorInstance();
				var state = Global.MovieSession.Movie.GetInputState(Global.Emulator.Frame - 1);
				if (state != null)
				{
					lg.SetSource(state);
					return lg.GenerateInputDisplay();
				}
			}

			return "";
		}

		public string InputStrOrAll()
		{
			var m = Global.MovieSession.Movie.IsPlayingOrRecording() && Global.Emulator.Frame > 0
				? Global.MovieSession.Movie.GetInputState(Global.Emulator.Frame - 1)
				: Global.MovieSession.MovieControllerInstance();

			var lg = Global.MovieSession.LogGeneratorInstance();

			lg.SetSource(Global.AutofireStickyXORAdapter.Or(m));
			return lg.GenerateInputDisplay();
		}

		private string MakeStringFor(IController controller)
		{
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(controller);
			return lg.GenerateInputDisplay();
		}

		public string MakeIntersectImmediatePrevious()
		{
			if (Global.MovieSession.Movie.IsActive())
			{
				var m = Global.MovieSession.Movie.IsPlayingOrRecording()
					? Global.MovieSession.Movie.GetInputState(Global.Emulator.Frame - 1)
					: Global.MovieSession.MovieControllerInstance();

				var lg = Global.MovieSession.LogGeneratorInstance();
				lg.SetSource(Global.AutofireStickyXORAdapter.And(m));
				return lg.GenerateInputDisplay();
			}

			return "";
		}

		public string MakeRerecordCount()
		{
			return Global.MovieSession.Movie.IsActive()
				? Global.MovieSession.Movie.Rerecords.ToString()
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
			if (Global.Config.DisplayFrameCounter && !Global.Game.IsNullInstance())
			{
				string message = MakeFrameCounter();
				var point = GetCoordinates(g, Global.Config.FrameCounter, message);
				DrawOsdMessage(g, message, Color.FromArgb(Global.Config.MessagesColor), point.X, point.Y);

				if (GlobalWin.MainForm.IsLagFrame)
				{
					DrawOsdMessage(g, Global.Emulator.Frame.ToString(), FixedAlertMessageColor, point.X, point.Y);
				}
			}

			if (Global.Config.DisplayInput && !Global.Game.IsNullInstance())
			{
				if (Global.MovieSession.Movie.Mode == MovieMode.Play
					|| (Global.MovieSession.Movie.IsFinished() && Global.Emulator.Frame == Global.MovieSession.Movie.InputLogLength)) // Account for the last frame of the movie, the movie state is immediately "Finished" here but we still want to show the input
				{
					var input = InputStrMovie();
					var point = GetCoordinates(g, Global.Config.InputDisplay, input);
					Color c = Color.FromArgb(Global.Config.MovieInput);
					g.DrawString(input, MessageFont, c, point.X, point.Y);
				}

				else // TODO: message config -- allow setting of "previous", "mixed", and "auto"
				{
					var previousColor = Color.Orange;
					Color immediateColor = Color.FromArgb(Global.Config.MessagesColor);
					var autoColor = Color.Pink;
					var changedColor = Color.PeachPuff;

					//we need some kind of string for calculating position when right-anchoring, of something like that
					var bgStr = InputStrOrAll();
					var point = GetCoordinates(g, Global.Config.InputDisplay, bgStr);

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
					var autoString = MakeStringFor(Global.StickyXORAdapter.Source.Xor(Global.AutofireStickyXORAdapter).And(Global.AutofireStickyXORAdapter));
					g.DrawString(autoString, MessageFont, autoColor, point.X, point.Y);

					//recolor everything that's changed from the previous input
					var immediateOverlay = MakeIntersectImmediatePrevious();
					g.DrawString(immediateOverlay, MessageFont, changedColor, point.X, point.Y);
				}
			}

			if (Global.MovieSession.MultiTrack.IsActive)
			{
				var point = GetCoordinates(g, Global.Config.MultitrackRecorder, Global.MovieSession.MultiTrack.Status);
				DrawOsdMessage(g, Global.MovieSession.MultiTrack.Status, FixedMessagesColor, point.X, point.Y);
			}

			if (Global.Config.DisplayFps && Fps != null)
			{
				var point = GetCoordinates(g, Global.Config.Fps, Fps);
				DrawOsdMessage(g, Fps, FixedMessagesColor, point.X, point.Y);
			}

			if (Global.Config.DisplayLagCounter && Global.Emulator.CanPollInput())
			{
				var counter = Global.Emulator.AsInputPollable().LagCount.ToString();
				var point = GetCoordinates(g, Global.Config.LagCounter, counter);
				DrawOsdMessage(g, counter, FixedAlertMessageColor, point.X, point.Y);
			}

			if (Global.Config.DisplayRerecordCount)
			{
				string rerecordCount = MakeRerecordCount();
				var point = GetCoordinates(g, Global.Config.ReRecordCounter, rerecordCount);
				DrawOsdMessage(g, rerecordCount, FixedMessagesColor, point.X, point.Y);
			}

			if (Global.ClientControls["Autohold"] || Global.ClientControls["Autofire"])
			{
				var sb = new StringBuilder("Held: ");

				foreach (string sticky in Global.StickyXORAdapter.CurrentStickies)
				{
					sb.Append(sticky).Append(' ');
				}

				foreach (string autoSticky in Global.AutofireStickyXORAdapter.CurrentStickies)
				{
					sb
						.Append("Auto-")
						.Append(autoSticky)
						.Append(' ');
				}

				var message = sb.ToString();
				var point = GetCoordinates(g, Global.Config.Autohold, message);
				g.DrawString(message, MessageFont, Color.White, point.X, point.Y);
			}

			if (Global.MovieSession.Movie.IsActive() && Global.Config.DisplaySubtitles)
			{
				var subList = Global.MovieSession.Movie.Subtitles.GetSubtitles(Global.Emulator.Frame);

				foreach (var sub in subList)
				{
					DrawOsdMessage(g, sub.Message, Color.FromArgb((int)sub.Color), sub.X, sub.Y);
				}
			}
		}
	}
}