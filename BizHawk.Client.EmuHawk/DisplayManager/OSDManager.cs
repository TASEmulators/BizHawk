using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
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

	class UIMessage
	{
		public string Message;
		public DateTime ExpireAt;
	}

	class UIDisplay
	{
		public string Message;
		public int X;
		public int Y;
		public MessageOption.AnchorType Anchor;
		public Color ForeColor;
		public Color BackGround;
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

		private float GetX(IBlitter g, int x, MessageOption.AnchorType anchor, string message)
		{
			var size = g.MeasureString(message, MessageFont);

			switch (anchor)
			{
				default:
				case MessageOption.AnchorType.TopLeft:
				case MessageOption.AnchorType.BottomLeft:
					return x;
				case MessageOption.AnchorType.TopRight:
				case MessageOption.AnchorType.BottomRight:
					return g.ClipBounds.Width - x - size.Width;
			}
		}

		private float GetY(IBlitter g, int y, MessageOption.AnchorType anchor, string message)
		{
			var size = g.MeasureString(message, MessageFont);

			switch (anchor)
			{
				default:
				case MessageOption.AnchorType.TopLeft:
				case MessageOption.AnchorType.TopRight:
					return y;
				case MessageOption.AnchorType.BottomLeft:
				case MessageOption.AnchorType.BottomRight:
					return g.ClipBounds.Height - y - size.Height;
			}
		}

		private string MakeFrameCounter()
		{
			if (Global.MovieSession.Movie.IsFinished)
			{
				var sb = new StringBuilder();
				sb
					.Append(Global.Emulator.Frame)
					.Append('/')
					.Append(Global.MovieSession.Movie.FrameCount)
					.Append(" (Finished)");
				return sb.ToString();
			}

			if (Global.MovieSession.Movie.IsPlaying)
			{
				var sb = new StringBuilder();
				sb
					.Append(Global.Emulator.Frame)
					.Append('/')
					.Append(Global.MovieSession.Movie.FrameCount);

				return sb.ToString();
			}
			
			if (Global.MovieSession.Movie.IsRecording)
			{
				return Global.Emulator.Frame.ToString();
			}
			
			return Global.Emulator.Frame.ToString();
		}

		private readonly List<UIMessage> _messages = new List<UIMessage>(5);
		private readonly List<UIDisplay> _guiTextList = new List<UIDisplay>();

		public void AddMessage(string message)
		{
			_messages.Add(new UIMessage { Message = message, ExpireAt = DateTime.Now + TimeSpan.FromSeconds(2) });
		}

		public void AddGuiText(string message, int x, int y, Color backGround, Color foreColor, MessageOption.AnchorType anchor)
		{
			_guiTextList.Add(new UIDisplay
			{
				Message = message,
				X = x,
				Y = y,
				BackGround = backGround,
				ForeColor = foreColor,
				Anchor = anchor
			});
		}

		public void ClearGuiText()
		{
			_guiTextList.Clear();
		}

		public void DrawMessages(IBlitter g)
		{
			if (!Global.Config.DisplayMessages)
			{
				return;
			}

			_messages.RemoveAll(m => DateTime.Now > m.ExpireAt);
			int line = 1;
			if (Global.Config.StackOSDMessages)
			{
				for (int i = _messages.Count - 1; i >= 0; i--, line++)
				{
					float x = GetX(g, Global.Config.Messages.X, Global.Config.Messages.Anchor, _messages[i].Message);
					float y = GetY(g, Global.Config.Messages.X, Global.Config.Messages.Anchor, _messages[i].Message);
					if (Global.Config.Messages.Anchor.IsTop())
					{
						y += (line - 1) * 18;
					}
					else
					{
						y -= (line - 1) * 18;
					}

					g.DrawString(_messages[i].Message, MessageFont, FixedMessagesColor, x, y);
				}
			}
			else
			{
				if (_messages.Any())
				{
					int i = _messages.Count - 1;

					float x = GetX(g, Global.Config.Messages.X, Global.Config.Messages.Anchor, _messages[i].Message);
					float y = GetY(g, Global.Config.Messages.Y, Global.Config.Messages.Anchor, _messages[i].Message);
					if (Global.Config.Messages.Anchor.IsTop())
					{
						y += (line - 1) * 18;
					}
					else
					{
						y -= (line - 1) * 18;
					}

					g.DrawString(_messages[i].Message, MessageFont, FixedMessagesColor, x, y);
				}
			}

			foreach (var text in _guiTextList)
			{
				try
				{
					float posX = GetX(g, text.X, text.Anchor, text.Message);
					float posY = GetY(g, text.Y, text.Anchor, text.Message);

					g.DrawString(text.Message, MessageFont, text.ForeColor, posX, posY);
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
			if (Global.MovieSession.Movie.IsActive && !Global.MovieSession.Movie.IsFinished)
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
			var m = (Global.MovieSession.Movie.IsActive && 
				!Global.MovieSession.Movie.IsFinished &&
				Global.Emulator.Frame > 0) ?
				Global.MovieSession.Movie.GetInputState(Global.Emulator.Frame - 1) :
				Global.MovieSession.MovieControllerInstance();

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
			if (Global.MovieSession.Movie.IsActive)
			{
				var m = Global.MovieSession.Movie.IsActive && !Global.MovieSession.Movie.IsFinished ?
					Global.MovieSession.Movie.GetInputState(Global.Emulator.Frame - 1) :
					Global.MovieSession.MovieControllerInstance();

				var lg = Global.MovieSession.LogGeneratorInstance();
				lg.SetSource(Global.AutofireStickyXORAdapter.And(m));
				return lg.GenerateInputDisplay();
			}

			return "";
		}

		public string MakeRerecordCount()
		{
			return Global.MovieSession.Movie.IsActive
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
				float x = GetX(g, Global.Config.FrameCounter.X, Global.Config.FrameCounter.Anchor, message);
				float y = GetY(g, Global.Config.FrameCounter.Y, Global.Config.FrameCounter.Anchor, message);

				DrawOsdMessage(g, message, Color.FromArgb(Global.Config.MessagesColor), x, y);

				if (GlobalWin.MainForm.IsLagFrame)
				{
					DrawOsdMessage(g, Global.Emulator.Frame.ToString(), FixedAlertMessageColor, x, y);
				}
			}

			if (Global.Config.DisplayInput && !Global.Game.IsNullInstance())
			{
				if ((Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsFinished)
					|| (Global.MovieSession.Movie.IsFinished && Global.Emulator.Frame == Global.MovieSession.Movie.InputLogLength)) // Account for the last frame of the movie, the movie state is immediately "Finished" here but we still want to show the input
				{
					var input = InputStrMovie();
					var x = GetX(g, Global.Config.InputDisplay.X, Global.Config.InputDisplay.Anchor, input);
					var y = GetY(g, Global.Config.InputDisplay.Y, Global.Config.InputDisplay.Anchor, input);
					Color c = Color.FromArgb(Global.Config.MovieInput);
					g.DrawString(input, MessageFont, c, x, y);
				}

				else // TODO: message config -- allow setting of "previous", "mixed", and "auto"
				{
					var previousColor = Color.Orange;
					Color immediateColor = Color.FromArgb(Global.Config.MessagesColor);
					var autoColor = Color.Pink;
					var changedColor = Color.PeachPuff;

					//we need some kind of string for calculating position when right-anchoring, of something like that
					var bgStr = InputStrOrAll();
					var x = GetX(g, Global.Config.InputDisplay.X, Global.Config.InputDisplay.Anchor, bgStr);
					var y = GetY(g, Global.Config.InputDisplay.Y, Global.Config.InputDisplay.Anchor, bgStr);

					// now, we're going to render these repeatedly, with higher-priority things overriding

					// first display previous frame's input.
					// note: that's only available in case we're working on a movie
					var previousStr = InputPrevious();
					g.DrawString(previousStr, MessageFont, previousColor, x, y);

					// next, draw the immediate input.
					// that is, whatever is being held down interactively right this moment even if the game is paused
					// this includes things held down due to autohold or autofire
					// I know, this is all really confusing
					var immediate = InputStrImmediate();
					g.DrawString(immediate, MessageFont, immediateColor, x, y);

					// next draw anything that's pressed because it's sticky.
					// this applies to autofire and autohold both. somehow. I don't understand it.
					// basically we're tinting whatever is pressed because it's sticky specially
					// in order to achieve this we want to avoid drawing anything pink that isn't actually held down right now
					// so we make an AND adapter and combine it using immediate & sticky
					var autoString = MakeStringFor(Global.StickyXORAdapter.Source.Xor(Global.AutofireStickyXORAdapter).And(Global.AutofireStickyXORAdapter));
					g.DrawString(autoString, MessageFont, autoColor, x, y);

					//recolor everything that's changed from the previous input
					var immediateOverlay = MakeIntersectImmediatePrevious();
					g.DrawString(immediateOverlay, MessageFont, changedColor, x, y);
				}
			}

			if (Global.MovieSession.MultiTrack.IsActive)
			{
				float x = GetX(g, Global.Config.MultitrackRecorder.X, Global.Config.MultitrackRecorder.Anchor, Global.MovieSession.MultiTrack.Status);
				float y = GetY(g, Global.Config.MultitrackRecorder.Y, Global.Config.MultitrackRecorder.Anchor, Global.MovieSession.MultiTrack.Status);

				DrawOsdMessage(g, Global.MovieSession.MultiTrack.Status, FixedMessagesColor, x, y);
			}

			if (Global.Config.DisplayFPS && Fps != null)
			{
				float x = GetX(g, Global.Config.Fps.X, Global.Config.Fps.Anchor, Fps);
				float y = GetY(g, Global.Config.Fps.Y, Global.Config.Fps.Anchor, Fps);

				DrawOsdMessage(g, Fps, FixedMessagesColor, x, y);
			}

			if (Global.Config.DisplayLagCounter && Global.Emulator.CanPollInput())
			{
				var counter = Global.Emulator.AsInputPollable().LagCount.ToString();
				var x = GetX(g, Global.Config.LagCounter.X, Global.Config.LagCounter.Anchor, counter);
				var y = GetY(g, Global.Config.LagCounter.Y, Global.Config.LagCounter.Anchor, counter);

				DrawOsdMessage(g, counter, FixedAlertMessageColor, x, y);
			}

			if (Global.Config.DisplayRerecordCount)
			{
				string rerecordCount = MakeRerecordCount();
				float x = GetX(g, Global.Config.ReRecordCounter.X, Global.Config.ReRecordCounter.Anchor, rerecordCount);
				float y = GetY(g, Global.Config.ReRecordCounter.Y, Global.Config.ReRecordCounter.Anchor, rerecordCount);

				DrawOsdMessage(g, rerecordCount, FixedMessagesColor, x, y);
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

				g.DrawString(
					message,
					MessageFont,
					Color.White,
					GetX(g, Global.Config.Autohold.X, Global.Config.Autohold.Anchor, message),
					GetY(g, Global.Config.Autohold.Y, Global.Config.Autohold.Anchor, message));
			}

			if (Global.MovieSession.Movie.IsActive && Global.Config.DisplaySubtitles)
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