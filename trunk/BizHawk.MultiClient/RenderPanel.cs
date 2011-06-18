using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

		public Direct3DRenderPanel(Direct3D direct3D, Control control)
		{
			d3d = direct3D;
			backingControl = control;
			control.DoubleClick += (o, e) => Global.MainForm.ToggleFullscreen();
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
			if (Device != null)
			{
				Device.Dispose();
				Device = null;
			}
		}

		public void CreateDevice()
		{
			DestroyDevice();
			var pp = new PresentParameters
				{
					BackBufferWidth = Math.Max(1, backingControl.ClientSize.Width),
					BackBufferHeight = Math.Max(1, backingControl.ClientSize.Height),
					DeviceWindowHandle = backingControl.Handle,
					PresentationInterval = Global.Config.DisplayVSync ? PresentInterval.One : PresentInterval.Immediate
				};
			Device = new Device(d3d, 0, DeviceType.Hardware, backingControl.Handle, CreateFlags.HardwareVertexProcessing, pp);
			Sprite = new Sprite(Device);
			Texture = new ImageTexture(Device);
			MessageFont = new Font(Device, 16, 0, FontWeight.Bold, 1, false, CharacterSet.Default, Precision.Default, FontQuality.Default, PitchAndFamily.Default | PitchAndFamily.DontCare, "Courier");
			AlertFont = new Font(Device, 16, 0, FontWeight.ExtraBold, 1, true, CharacterSet.Default, Precision.Default, FontQuality.Default, PitchAndFamily.Default | PitchAndFamily.DontCare, "Courier");
		}

		public void Render()
		{
			if (Device == null || Resized)
				CreateDevice();

			Resized = false;
			Device.Clear(ClearFlags.Target, BackgroundColor, 1.0f, 0);
			Device.Present(Present.DoNotWait);
		}

		public void Render(IVideoProvider video)
		{
			if (video == null)
			{
				Render();
				return;
			}

			if (Device == null || Resized)
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
			int x, y;
			//TODO: If movie loaded use that frame counter, and also display total movie frame count if read-only
			if (Global.Config.DisplayFrameCounter)
			{
				x = GetX(Global.Config.DispFrameCx, Global.Config.DispFrameanchor);
				y = GetY(Global.Config.DispFrameCy, Global.Config.DispFrameanchor);
				MessageFont.DrawString(null, MakeFrameCounter(), x + 1,
					y + 1, new Color4(Color.Black));
				MessageFont.DrawString(null, MakeFrameCounter(), x,
					y, Color.FromArgb(Global.Config.MessagesColor));
			}
			if (Global.Config.DisplayInput)
			{
				string input = MakeLastInputDisplay();
				Color c;
				x = GetX(Global.Config.DispInpx, Global.Config.DispInpanchor);
				y = GetY(Global.Config.DispInpy, Global.Config.DispInpanchor);
				if (Global.MainForm.UserMovie.GetMovieMode() == MOVIEMODE.PLAY)
				{
					c = Color.Gray;
				}
				else
					c = Color.FromArgb(Global.Config.MessagesColor);

				MessageFont.DrawString(null, input, x + 2, y + 2, new Color4(Color.Black));
				MessageFont.DrawString(null, input, x + 1, y + 1, Color.FromArgb(Global.Config.LastInputColor));
				input = MakeInputDisplay();
				MessageFont.DrawString(null, input, x, y, c);
			}
            if (Global.MainForm.UserMovie.MultiTrack.isActive)
            {
                 MessageFont.DrawString(null, MT, Global.Config.DispFPSx + 1, //TODO: Multitrack position variables
                    Global.Config.DispFPSy + 1, new Color4(Color.Black));
                MessageFont.DrawString(null, MT, Global.Config.DispFPSx,
                    Global.Config.DispFPSy, Color.FromArgb(Global.Config.MessagesColor));
            }
			if (Global.Config.DisplayFPS && FPS != null)
			{
				x = GetX(Global.Config.DispFPSx, Global.Config.DispFPSanchor);
				y = GetY(Global.Config.DispFPSy, Global.Config.DispFPSanchor);
				MessageFont.DrawString(null, FPS, x + 1,
					y + 1, new Color4(Color.Black));
				MessageFont.DrawString(null, FPS, x,
					y, Color.FromArgb(Global.Config.MessagesColor));
			}

			if (Global.Config.DisplayLagCounter)
			{
				x = GetX(Global.Config.DispLagx, Global.Config.DispLaganchor);
				y = GetY(Global.Config.DispLagy, Global.Config.DispLaganchor);
				if (Global.Emulator.IsLagFrame)
				{
					AlertFont.DrawString(null, MakeLagCounter(), Global.Config.DispLagx + 1,
						Global.Config.DispLagy + 1, new Color4(Color.Black));
					AlertFont.DrawString(null, MakeLagCounter(), Global.Config.DispLagx,
						Global.Config.DispLagy, Color.FromArgb(Global.Config.AlertMessageColor));
				}
				else
				{
					MessageFont.DrawString(null, MakeLagCounter(), x + 1,
						y + 1, new Color4(Color.Black));
					MessageFont.DrawString(null, MakeLagCounter(), x,
						y, Color.FromArgb(Global.Config.MessagesColor));
				}

			}
			if (Global.Config.DisplayRerecordCount)
			{
				x = GetX(Global.Config.DispRecx, Global.Config.DispRecanchor);
				y = GetY(Global.Config.DispRecy, Global.Config.DispRecanchor);
				MessageFont.DrawString(null, MakeRerecordCount(), x + 1,
					 y + 1, new Color4(Color.Black));
				MessageFont.DrawString(null, MakeRerecordCount(), x,
					y, Color.FromArgb(Global.Config.MessagesColor));
			}

			//TODO: clean this up or replace with simple draw symbols
			if (Global.MainForm.UserMovie.GetMovieMode() == MOVIEMODE.PLAY
				|| Global.MainForm.UserMovie.GetMovieMode() == MOVIEMODE.PLAY)
			{
				MessageFont.DrawString(null, "Playback", 208 + 1,
					 0 + 1, new Color4(Color.Black));
				MessageFont.DrawString(null, "Playback", 208,
					0, new Color4(Color.Red));
			}
			else if (Global.MainForm.UserMovie.GetMovieMode() == MOVIEMODE.RECORD)
			{
				AlertFont.DrawString(null, "Recording", 208 + 1,
						 0 + 1, new Color4(Color.Black));
				AlertFont.DrawString(null, "Recording", 208,
					0, new Color4(Color.Red));
			}
		}

		private string MakeFrameCounter()
		{
			//TODO: remove rerecord count code and make it its own display option
			if (Global.MainForm.UserMovie.GetMovieMode() == MOVIEMODE.FINISHED)
			{
				return Global.Emulator.Frame.ToString() + "/" + Global.MainForm.UserMovie.GetMovieLength().ToString() + " (Finished)";
			}
			else if (Global.MainForm.UserMovie.GetMovieMode() == MOVIEMODE.PLAY)
			{
				return "E" + Global.Emulator.Frame.ToString() + " " + Global.MainForm.UserMovie.lastLog.ToString()
					+ "/" + Global.MainForm.UserMovie.GetMovieLength().ToString();
			}
			else if (Global.MainForm.UserMovie.GetMovieMode() == MOVIEMODE.RECORD)
				return "E" + Global.Emulator.Frame.ToString() + " " + Global.MainForm.UserMovie.lastLog.ToString() +
					" length: " + Global.MainForm.UserMovie.GetMovieLength().ToString();
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

		public void AddMessage(string message)
		{
			messages.Add(new UIMessage { Message = message, ExpireAt = DateTime.Now + TimeSpan.FromSeconds(2) });
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
				MessageFont.DrawString(null, messages[i].Message, x + 2, y + 2, new Color4(Color.Black));
				MessageFont.DrawString(null, messages[i].Message, x, y, Color.FromArgb(Global.Config.MessagesColor));
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
			string tmp = Global.ActiveController.GetControllersAsMnemonic();
			tmp = tmp.Replace(".", " ");
			tmp = tmp.Replace("|", "");
			return tmp;
		}

		public string MakeLastInputDisplay()
		{
			string tmp = Global.MainForm.wasPressed;
			tmp = tmp.Replace(".", " ");
			tmp = tmp.Replace("|", "");
			return tmp;
		}

		public string MakeRerecordCount()
		{
			string tmp = "";
			if (Global.MainForm.UserMovie.GetMovieMode() != MOVIEMODE.INACTIVE)
			{
				tmp += "Rerecord Count: ";
				tmp += Global.MainForm.GetActiveMovie().GetRerecordCount().ToString();
			}
			return tmp;
		}
	}

	class UIMessage
	{
		public string Message;
		public DateTime ExpireAt;
	}
}