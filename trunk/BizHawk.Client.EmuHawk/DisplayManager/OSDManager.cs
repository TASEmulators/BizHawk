using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using BizHawk.Client.Common;

using BizHawk.Bizware.BizwareGL;

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
		public int Anchor;
		public Color ForeColor;
		public Color BackGround;
	}

	public class OSDManager
	{
		public string FPS { get; set; }
		public string MT { get; set; }
		public IBlitterFont MessageFont;

		public void Dispose()
		{

		}

		public void Begin(IBlitter blitter)
		{
			MessageFont = blitter.GetFontType("MessageFont");
		}

		public System.Drawing.Color FixedMessagesColor { get { return System.Drawing.Color.FromArgb(Global.Config.MessagesColor); } }
		public System.Drawing.Color FixedAlertMessageColor { get { return System.Drawing.Color.FromArgb(Global.Config.AlertMessageColor); } }

		public OSDManager()
		{
		}

		private float GetX(IBlitter g, int x, int anchor, IBlitterFont font, string message)
		{
			var size = g.MeasureString(message, font);
			//Rectangle rect = g.MeasureString(Sprite, message, new DrawTextFormat());
			switch (anchor)
			{
				default:
				case 0: //Top Left
				case 2: //Bottom Left
					return x;
				case 1: //Top Right
				case 3: //Bottom Right
					return g.ClipBounds.Width - x - size.Width;
			}
		}

		private float GetY(IBlitter g, int y, int anchor, IBlitterFont font, string message)
		{
			var size = g.MeasureString(message, font);
			switch (anchor)
			{
				default:
				case 0: //Top Left
				case 1: //Top Right
					return y;
				case 2: //Bottom Left
				case 3: //Bottom Right
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
			else if (Global.MovieSession.Movie.IsPlaying)
			{
				var sb = new StringBuilder();
				sb
					.Append(Global.Emulator.Frame)
					.Append('/')
					.Append(Global.MovieSession.Movie.FrameCount);

				return sb.ToString();
			}
			else if (Global.MovieSession.Movie.IsRecording)
			{
				return Global.Emulator.Frame.ToString();
			}
			else
			{
				return Global.Emulator.Frame.ToString();
			}
		}

		private string MakeLagCounter()
		{
			return Global.Emulator.LagCount.ToString();
		}

		private List<UIMessage> messages = new List<UIMessage>(5);
		private List<UIDisplay> GUITextList = new List<UIDisplay>();

		public void AddMessage(string message)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			messages.Add(new UIMessage { Message = message, ExpireAt = DateTime.Now + TimeSpan.FromSeconds(2) });
		}

		public void AddGUIText(string message, int x, int y, Color backGround, Color foreColor, int anchor)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			GUITextList.Add(new UIDisplay
			{
				Message = message,
				X = x,
				Y = y,
				BackGround = backGround,
				ForeColor = foreColor,
				Anchor = anchor
			});
		}

		public void ClearGUIText()
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			GUITextList.Clear();
		}


		public void DrawMessages(IBlitter g)
		{
			if (!Global.ClientControls["MaxTurbo"])
			{
				messages.RemoveAll(m => DateTime.Now > m.ExpireAt);
				int line = 1;
				if (Global.Config.StackOSDMessages)
				{
					for (int i = messages.Count - 1; i >= 0; i--, line++)
					{
						float x = GetX(g, Global.Config.DispMessagex, Global.Config.DispMessageanchor, MessageFont, messages[i].Message);
						float y = GetY(g, Global.Config.DispMessagey, Global.Config.DispMessageanchor, MessageFont, messages[i].Message);
						if (Global.Config.DispMessageanchor < 2)
						{
							y += ((line - 1) * 18);
						}
						else
						{
							y -= ((line - 1) * 18);
						}
						g.DrawString(messages[i].Message, MessageFont, Color.Black, x + 2, y + 2);
						g.DrawString(messages[i].Message, MessageFont, FixedMessagesColor, x, y);
					}
				}
				else
				{
					if (messages.Count > 0)
					{
						int i = messages.Count - 1;

						float x = GetX(g, Global.Config.DispMessagex, Global.Config.DispMessageanchor, MessageFont, messages[i].Message);
						float y = GetY(g, Global.Config.DispMessagey, Global.Config.DispMessageanchor, MessageFont, messages[i].Message);
						if (Global.Config.DispMessageanchor < 2)
						{
							y += ((line - 1) * 18);
						}
						else
						{
							y -= ((line - 1) * 18);
						}

						g.DrawString(messages[i].Message, MessageFont, Color.Black, x + 2, y + 2);
						g.DrawString(messages[i].Message, MessageFont, FixedMessagesColor, x, y);
					}
				}

				for (int x = 0; x < GUITextList.Count; x++)
				{
					try
					{
						float posx = GetX(g, GUITextList[x].X, GUITextList[x].Anchor, MessageFont, GUITextList[x].Message);
						float posy = GetY(g, GUITextList[x].Y, GUITextList[x].Anchor, MessageFont, GUITextList[x].Message);

						g.DrawString(GUITextList[x].Message, MessageFont, GUITextList[x].BackGround, posx + 2, posy + 2);
						g.DrawString(GUITextList[x].Message, MessageFont, GUITextList[x].ForeColor, posx, posy);
					}
					catch (Exception)
					{
						return;
					}
				}
			}
		}


		public string MakeInputDisplay()
		{
			StringBuilder s;
			if (!Global.MovieSession.Movie.IsActive || Global.MovieSession.Movie.IsFinished)
			{
				s = new StringBuilder(Global.GetOutputControllersAsMnemonic());
			}
			else
			{
				s = new StringBuilder(Global.MovieSession.Movie.GetInput(Global.Emulator.Frame - 1));
			}

			s.Replace(".", " ").Replace("|", "").Replace(" 000, 000", "         ");

			return s.ToString();
		}

		public string MakeRerecordCount()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				return "Rerecord Count: " + Global.MovieSession.Movie.Header.Rerecords;
			}
			else
			{
				return string.Empty;
			}
		}

		private void DrawOsdMessage(IBlitter g, string message, Color color, float x, float y)
		{
			g.DrawString(message, MessageFont, Color.Black, x + 1, y + 1);
			g.DrawString(message, MessageFont, color, x, y);
		}

		/// <summary>
		/// Display all screen info objects like fps, frame counter, lag counter, and input display
		/// </summary>
		public void DrawScreenInfo(IBlitter g)
		{
			if (Global.Config.DisplayFrameCounter)
			{
				string message = MakeFrameCounter();
				float x = GetX(g, Global.Config.DispFrameCx, Global.Config.DispFrameanchor, MessageFont, message);
				float y = GetY(g, Global.Config.DispFrameCy, Global.Config.DispFrameanchor, MessageFont, message);

				DrawOsdMessage(g, message, Color.FromArgb(Global.Config.MessagesColor), x, y);
			}

			if (Global.Config.DisplayInput)
			{
				string input = MakeInputDisplay();
				Color c;
				float x = GetX(g, Global.Config.DispInpx, Global.Config.DispInpanchor, MessageFont, input);
				float y = GetY(g, Global.Config.DispInpy, Global.Config.DispInpanchor, MessageFont, input);
				if (Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsRecording)
				{
					c = Color.FromArgb(Global.Config.MovieInput);
				}
				else
				{
					c = Color.FromArgb(Global.Config.MessagesColor);
				}

				// TODO: this needs to be multi-colored and more intelligent: https://code.google.com/p/bizhawk/issues/detail?id=52
				g.DrawString(input, MessageFont, Color.Black, x + 1, y + 1);
				g.DrawString(input, MessageFont, c, x, y);
			}

			if (Global.MovieSession.MultiTrack.IsActive)
			{
				float x = GetX(g, Global.Config.DispMultix, Global.Config.DispMultianchor, MessageFont, MT);
				float y = GetY(g, Global.Config.DispMultiy, Global.Config.DispMultianchor, MessageFont, MT);

				DrawOsdMessage(g, MT, FixedMessagesColor, x, y);
			}

			if (Global.Config.DisplayFPS && FPS != null)
			{
				float x = GetX(g, Global.Config.DispFPSx, Global.Config.DispFPSanchor, MessageFont, FPS);
				float y = GetY(g, Global.Config.DispFPSy, Global.Config.DispFPSanchor, MessageFont, FPS);

				DrawOsdMessage(g, FPS, FixedMessagesColor, x, y);
			}

			if (Global.Config.DisplayLagCounter)
			{
				string counter = MakeLagCounter();

				float x = GetX(g, Global.Config.DispLagx, Global.Config.DispLaganchor, MessageFont, counter);
				float y = GetY(g, Global.Config.DispLagy, Global.Config.DispLaganchor, MessageFont, counter);

				DrawOsdMessage(g, counter, (Global.Emulator.IsLagFrame ? FixedAlertMessageColor : FixedAlertMessageColor), x, y);
			}

			if (Global.Config.DisplayRerecordCount)
			{
				string rerec = MakeRerecordCount();
				float x = GetX(g, Global.Config.DispRecx, Global.Config.DispRecanchor, MessageFont, rerec);
				float y = GetY(g, Global.Config.DispRecy, Global.Config.DispRecanchor, MessageFont, rerec);

				DrawOsdMessage(g, rerec, FixedMessagesColor, x, y);
			}

			if (Global.ClientControls["Autohold"] || Global.ClientControls["Autofire"])
			{
				StringBuilder disp = new StringBuilder("Held: ");

				foreach (string s in Global.StickyXORAdapter.CurrentStickies)
				{
					disp.Append(s);
					disp.Append(' ');
				}

				foreach (string s in Global.AutofireStickyXORAdapter.CurrentStickies)
				{
					disp.Append("Auto-");
					disp.Append(s);
					disp.Append(' ');
				}

				g.DrawString(disp.ToString(), MessageFont, Color.White, GetX(g, Global.Config.DispAutoholdx, Global.Config.DispAutoholdanchor, MessageFont,
					disp.ToString()), GetY(g, Global.Config.DispAutoholdy, Global.Config.DispAutoholdanchor, MessageFont, disp.ToString()));
			}

			if (Global.MovieSession.Movie.IsActive && Global.Config.DisplaySubtitles)
			{
				var subList = Global.MovieSession.Movie.Header.Subtitles.GetSubtitles(Global.Emulator.Frame).ToList();

				foreach (var sub in subList)
				{
					DrawOsdMessage(g, sub.Message, Color.FromArgb((int)sub.Color), sub.X, sub.Y);
				}
			}
		}
	}

}