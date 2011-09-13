using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Text;
using System.Windows.Forms;
using SlimDX;
using SlimDX.Direct3D9;
using Font = SlimDX.Direct3D9.Font;
using BizHawk.Core;

namespace BizHawk.MultiClient
{
	public class ImageTexture : IDisposable
	{
		public Device GraphicsDevice;
		public Texture Texture;

		private int imageWidth;
		public int ImageWidth { get { return imageWidth; } }

		private int imageHeight;
		public int ImageHeight { get { return imageHeight; } }

		private int textureWidth;
		public int TextureWidth { get { return textureWidth; } }

		private int textureHeight;
		public int TextureHeight { get { return textureHeight; } }

		public ImageTexture(Device graphicsDevice)
		{
			GraphicsDevice = graphicsDevice;
		}

		public void SetImage(int[] data, int width, int height)
		{
			bool needsRecreating = false;
			if (Texture == null)
			{
				needsRecreating = true;
			}
			else
			{
				var currentTextureSize = Texture.GetLevelDescription(0);
				if (imageWidth != width || imageHeight != height)
				{
					needsRecreating = true;
				}
			}
			// If we need to recreate the texture, do so.
			if (needsRecreating)
			{
				if (Texture != null)
				{
					Texture.Dispose();
					Texture = null;
				}
				// Copy the width/height to member fields.
				imageWidth = width;
				imageHeight = height;
				// Round up the width/height to the nearest power of two.
				textureWidth = 32; textureHeight = 32;
				while (textureWidth < imageWidth) textureWidth <<= 1;
				while (textureHeight < imageHeight) textureHeight <<= 1;
				// Create a new texture instance.
				Texture = new Texture(GraphicsDevice, textureWidth, textureHeight, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
			}
			// Copy the image data to the texture.
			using (var Data = Texture.LockRectangle(0, new Rectangle(0, 0, imageWidth, imageHeight), LockFlags.None).Data)
			{
				if (imageWidth == textureWidth)
				{
					// Widths are the same, just dump the data across (easy!)
					Data.WriteRange(data, 0, imageWidth * imageHeight);
				}
				else
				{
					// Widths are different, need a bit of additional magic here to make them fit:
					long RowSeekOffset = 4 * (textureWidth - imageWidth);
					for (int r = 0, s = 0; r < imageHeight; ++r, s += imageWidth)
					{
						Data.WriteRange(data, s, imageWidth);
						Data.Seek(RowSeekOffset, SeekOrigin.Current);
					}
				}
				Texture.UnlockRectangle(0);
			}
		}

		private bool disposed;

		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				if (Texture != null)
					Texture.Dispose();
				Texture = null;
				GC.SuppressFinalize(this);
			}
		}
	}

	public interface IRenderer : IDisposable
	{
		void Render(IVideoProvider video);
		bool Resized { get; set; }
		void AddMessage(string msg);
		void AddGUIText(string msg, int x, int y);
		void ClearGUIText();
		string FPS { get; set; }
		string MT { get; set; }
	}

	public class SysdrawingRenderPanel : IRenderer
	{
		public bool Resized { get; set; }
		public void Dispose() { }
		public string FPS { get; set; }
		public string MT { get; set; }
		public void Render(IVideoProvider video)
		{
			Color BackgroundColor = Color.FromArgb(video.BackgroundColor);
			int[] data = video.GetVideoBuffer();

			Bitmap bmp = new Bitmap(video.BufferWidth, video.BufferHeight, PixelFormat.Format32bppArgb);
			BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			//TODO - this is not very intelligent. no handling of pitch, for instance
			Marshal.Copy(data, 0, bmpdata.Scan0, bmp.Width * bmp.Height);

			bmp.UnlockBits(bmpdata);

			backingControl.SetBitmap(bmp);
		}
		public SysdrawingRenderPanel(RetainedViewportPanel control)
		{
			backingControl = control;
		}
		RetainedViewportPanel backingControl;
		public void AddMessage(string msg) { }
		public void AddGUIText(string msg, int x, int y) { }
		public void ClearGUIText() { }
	}


	public class Direct3DRenderPanel : IRenderer
	{
		public Color BackgroundColor { get; set; }
		public bool Resized { get; set; }
		public string FPS { get; set; }
		public string MT { get; set; }
		private Direct3D d3d;
		private Device Device;
		private Control backingControl;
		public ImageTexture Texture;
		private Sprite Sprite;
		private Font MessageFont;
		private Font AlertFont;
		private bool Vsync;


		public Direct3DRenderPanel(Direct3D direct3D, Control control)
		{
			d3d = direct3D;
			backingControl = control;
			control.MouseDoubleClick += (o, e) => HandleFullscreenToggle(o, e);
			control.MouseClick += (o, e) => Global.MainForm.MainForm_MouseClick(o, e);
		}

		private void HandleFullscreenToggle(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				Global.MainForm.ToggleFullscreen();
		}

		private void DestroyDevice()
		{
			if (Texture != null)
			{
				Texture.Dispose();
				Texture = null;
			}
			if (Sprite != null)
			{
				Sprite.Dispose();
				Sprite = null;
			}
			if (MessageFont != null)
			{
				MessageFont.Dispose();
				MessageFont = null;
			}
            if (AlertFont != null)
            {
                AlertFont.Dispose();
                AlertFont = null;
            }
			if (Device != null)
			{
				Device.Dispose();
				Device = null;
			}
		}

		private bool VsyncRequested
		{
			get
			{
				if (Global.ForceNoVsync) return false;
				return Global.Config.DisplayVSync;
			}
		}

		public void CreateDevice()
		{
			DestroyDevice();
			Vsync = VsyncRequested;
			var pp = new PresentParameters
				{
					BackBufferWidth = Math.Max(1, backingControl.ClientSize.Width),
					BackBufferHeight = Math.Max(1, backingControl.ClientSize.Height),
					DeviceWindowHandle = backingControl.Handle,
					PresentationInterval = Vsync ? PresentInterval.One : PresentInterval.Immediate
				};

			var flags = CreateFlags.SoftwareVertexProcessing;
			if ((d3d.GetDeviceCaps(0, DeviceType.Hardware).DeviceCaps & DeviceCaps.HWTransformAndLight) != 0)
			{
				flags = CreateFlags.HardwareVertexProcessing;
			}
			Device = new Device(d3d, 0, DeviceType.Hardware, backingControl.Handle, flags, pp);
			Sprite = new Sprite(Device);
			Texture = new ImageTexture(Device);
			MessageFont = new Font(Device, 16, 0, FontWeight.Bold, 1, false, CharacterSet.Default, Precision.Default, FontQuality.Default, PitchAndFamily.Default | PitchAndFamily.DontCare, "Courier");
			AlertFont = new Font(Device, 16, 0, FontWeight.ExtraBold, 1, true, CharacterSet.Default, Precision.Default, FontQuality.Default, PitchAndFamily.Default | PitchAndFamily.DontCare, "Courier");
            // NOTE: if you add ANY objects, like new fonts, textures, etc, to this method
            // ALSO add dispose code in DestroyDevice() or you will be responsible for VRAM memory leaks.
		}

		public void Render()
		{
			if (Device == null || Resized || Vsync != VsyncRequested)
				CreateDevice();

			Resized = false;
			Device.Clear(ClearFlags.Target, BackgroundColor, 1.0f, 0);
			Device.Present(Present.DoNotWait);
		}

		public void Render(IVideoProvider video)
		{
			try
			{
				RenderExec(video);
			}
			catch (Direct3D9Exception)
			{
				// Wait until device is available or user gets annoyed and closes app
				Result r;
				do
				{
					r = Device.TestCooperativeLevel();
					Thread.Sleep(100);
				} while (r == ResultCode.DeviceLost);

				// lets try recovery!
				DestroyDevice();
				CreateDevice();
				RenderExec(video);
			}
		}

		private void RenderExec(IVideoProvider video)
		{
			if (video == null)
			{
				Render();
				return;
			}

			if (Device == null || Resized || Vsync != VsyncRequested)
				CreateDevice();
			Resized = false;

			BackgroundColor = Color.FromArgb(video.BackgroundColor);

			int[] data = video.GetVideoBuffer();
			Texture.SetImage(data, video.BufferWidth, video.BufferHeight);

			Device.Clear(ClearFlags.Target, BackgroundColor, 1.0f, 0);

			// figure out scaling factor
			float widthScale = (float)backingControl.Size.Width / video.BufferWidth;
			float heightScale = (float)backingControl.Size.Height / video.BufferHeight;
			float finalScale = Math.Min(widthScale, heightScale);

			Device.BeginScene();

			Sprite.Begin(SpriteFlags.None);
			Device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Point);
			Device.SetSamplerState(1, SamplerState.MagFilter, TextureFilter.Point);
			Sprite.Transform = Matrix.Scaling(finalScale, finalScale, 0f);
			Sprite.Draw(Texture.Texture, new Rectangle(0, 0, video.BufferWidth, video.BufferHeight), new Vector3(video.BufferWidth / 2f, video.BufferHeight / 2f, 0), new Vector3(backingControl.Size.Width / 2f / finalScale, backingControl.Size.Height / 2f / finalScale, 0), Color.White);
			Sprite.End();

			DrawMessages();

			Device.EndScene();
			Device.Present(Present.DoNotWait);
		}

		private int GetX(int x, int anchor)
		{
			switch (anchor)
			{
				default:
				case 0:
				case 2:
					return x;
				case 1:
				case 3:
					return backingControl.Size.Width - Global.Emulator.VideoProvider.BufferWidth + x;
			}
		}

		private int GetY(int y, int anchor)
		{
			switch (anchor)
			{
				default:
				case 0:
				case 1:
					return y;
				case 2:
				case 3:
					return backingControl.Size.Height - Global.Emulator.VideoProvider.BufferHeight + y;
			}
		}

		/// <summary>
		/// Display all screen info objects like fps, frame counter, lag counter, and input display
		/// </summary>
		public void DrawScreenInfo()
		{
			if (Global.Config.DisplayFrameCounter)
			{
				int x = GetX(Global.Config.DispFrameCx, Global.Config.DispFrameanchor);
				int y = GetY(Global.Config.DispFrameCy, Global.Config.DispFrameanchor);
				MessageFont.DrawString(null, MakeFrameCounter(), x + 1,
					y + 1, Color.Black);
				MessageFont.DrawString(null, MakeFrameCounter(), x,
					y, Color.FromArgb(Global.Config.MessagesColor));
			}
			if (Global.Config.DisplayInput)
			{
				Color c;
				int x = GetX(Global.Config.DispInpx, Global.Config.DispInpanchor);
				int y = GetY(Global.Config.DispInpy, Global.Config.DispInpanchor);
				if (Global.MovieSession.Movie.Mode == MOVIEMODE.PLAY)
				{
					c = Color.FromArgb(Global.Config.MovieInput);
				}
				else
					c = Color.FromArgb(Global.Config.MessagesColor);

				string input = MakeInputDisplay();
				MessageFont.DrawString(null, input, x + 1, y + 1, Color.Black);
				MessageFont.DrawString(null, input, x, y, c);
			}
			if (Global.MovieSession.MultiTrack.IsActive)
			{
				MessageFont.DrawString(null, MT, Global.Config.DispFPSx + 1, //TODO: Multitrack position variables
					Global.Config.DispFPSy + 1, Color.Black);
				MessageFont.DrawString(null, MT, Global.Config.DispFPSx,
					Global.Config.DispFPSy, Color.FromArgb(Global.Config.MessagesColor));
			}
			if (Global.Config.DisplayFPS && FPS != null)
			{
				int x = GetX(Global.Config.DispFPSx, Global.Config.DispFPSanchor);
				int y = GetY(Global.Config.DispFPSy, Global.Config.DispFPSanchor);
				MessageFont.DrawString(null, FPS, x + 1,
					y + 1, Color.Black);
				MessageFont.DrawString(null, FPS, x,
					y, Color.FromArgb(Global.Config.MessagesColor));
			}

			if (Global.Config.DisplayLagCounter)
			{
				int x = GetX(Global.Config.DispLagx, Global.Config.DispLaganchor);
				int y = GetY(Global.Config.DispLagy, Global.Config.DispLaganchor);
				if (Global.Emulator.IsLagFrame)
				{
					AlertFont.DrawString(null, MakeLagCounter(), Global.Config.DispLagx + 1,
						Global.Config.DispLagy + 1, Color.Black);
					AlertFont.DrawString(null, MakeLagCounter(), Global.Config.DispLagx,
						Global.Config.DispLagy, Color.FromArgb(Global.Config.AlertMessageColor));
				}
				else
				{
					MessageFont.DrawString(null, MakeLagCounter(), x + 1,
						y + 1, Color.Black);
					MessageFont.DrawString(null, MakeLagCounter(), x,
						y, Color.FromArgb(Global.Config.MessagesColor));
				}

			}
			if (Global.Config.DisplayRerecordCount)
			{
				int x = GetX(Global.Config.DispRecx, Global.Config.DispRecanchor);
				int y = GetY(Global.Config.DispRecy, Global.Config.DispRecanchor);
				MessageFont.DrawString(null, MakeRerecordCount(), x + 1,
					 y + 1, Color.Black);
				MessageFont.DrawString(null, MakeRerecordCount(), x,
					y, Color.FromArgb(Global.Config.MessagesColor));
			}

			if (Global.MovieSession.Movie.Mode == MOVIEMODE.PLAY)
			{
				MessageFont.DrawString(null, "Play", backingControl.Size.Width - 47,
					 0 + 1, Color.Black);
				MessageFont.DrawString(null, "Play", backingControl.Size.Width - 48,
					0, Color.FromArgb(Global.Config.MovieColor));
			}
			else if (Global.MovieSession.Movie.Mode == MOVIEMODE.RECORD)
			{
				AlertFont.DrawString(null, "Record", backingControl.Size.Width - 65,
						 0 + 1, Color.Black);
				AlertFont.DrawString(null, "Record", backingControl.Size.Width - 64,
					0, Color.FromArgb(Global.Config.MovieColor));
			}

			if (Global.MovieSession.Movie.Mode != MOVIEMODE.INACTIVE && Global.Config.DisplaySubtitles)
			{
				//TODO: implement multiple subtitles at once feature
				Subtitle s = Global.MovieSession.Movie.Subtitles.GetSubtitle(Global.Emulator.Frame);
				MessageFont.DrawString(null, s.Message, s.X + 1,
							s.Y + 1, new Color4(Color.Black));
				MessageFont.DrawString(null, s.Message, s.X,
							s.Y, Color.FromArgb((int)s.Color));
			}
		}

		private string MakeFrameCounter()
		{
			if (Global.MovieSession.Movie.Mode == MOVIEMODE.FINISHED)
			{
				return Global.Emulator.Frame.ToString() + "/" + Global.MovieSession.Movie.Length().ToString() + " (Finished)";
			}
			else if (Global.MovieSession.Movie.Mode == MOVIEMODE.PLAY)
			{
				return Global.Emulator.Frame.ToString() + "/" + Global.MovieSession.Movie.Length().ToString();
			}
			else if (Global.MovieSession.Movie.Mode == MOVIEMODE.RECORD)
				return Global.Emulator.Frame.ToString();
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
			messages.Add(new UIMessage { Message = message, ExpireAt = DateTime.Now + TimeSpan.FromSeconds(2) });
		}

		public void AddGUIText(string message, int x, int y)
		{
			GUITextList.Add(new UIDisplay { Message = message, X = x, Y = y });
		}

		public void ClearGUIText()
		{
			GUITextList.Clear();
		}

		private void DrawMessages()
		{
			messages.RemoveAll(m => DateTime.Now > m.ExpireAt);
			DrawScreenInfo();
			int line = 1;
			for (int i = messages.Count - 1; i >= 0; i--, line++)
			{
				int x = 3;
				int y = backingControl.Size.Height - (line * 18);
				MessageFont.DrawString(null, messages[i].Message, x + 2, y + 2, Color.Black);
				MessageFont.DrawString(null, messages[i].Message, x, y, Color.FromArgb(Global.Config.MessagesColor));
			}
			for (int x = 0; x < GUITextList.Count; x++)
			{
				MessageFont.DrawString(null, GUITextList[x].Message,
					GUITextList[x].X + 2, GUITextList[x].Y + 2, Color.Black);
				MessageFont.DrawString(null, GUITextList[x].Message,
					GUITextList[x].X + 1, GUITextList[x].Y + 1, Color.Gray);
				MessageFont.DrawString(null, GUITextList[x].Message,
					GUITextList[x].X, GUITextList[x].Y, Color.FromArgb(Global.Config.MessagesColor));
			}
		}

		private bool disposed;

		public void Dispose()
		{
			if (disposed == false)
			{
				disposed = true;
				DestroyDevice();
			}
		}

		public string MakeInputDisplay()
		{
			StringBuilder s;
			if (Global.MovieSession.Movie.Mode == MOVIEMODE.INACTIVE || Global.MovieSession.Movie.Mode == MOVIEMODE.FINISHED)
				s = new StringBuilder(Global.GetOutputControllersAsMnemonic());
			else
				s = new StringBuilder(Global.MovieSession.Movie.GetInputFrame(Global.Emulator.Frame - 1));
			s.Replace(".", " ");
			s.Replace("|", "");
			return s.ToString();
		}

		public string MakeRerecordCount()
		{
			if (Global.MovieSession.Movie.Mode != MOVIEMODE.INACTIVE)
				return "Rerecord Count: " + Global.MovieSession.Movie.Rerecords.ToString();
			else
				return "";
		}
	}

	class UIMessage
	{
		public string Message;
		public DateTime ExpireAt;
	}

	class UIDisplay
	{
		public string Message;
		public DateTime ExpireAt;
		public int X;
		public int Y;
	}
}