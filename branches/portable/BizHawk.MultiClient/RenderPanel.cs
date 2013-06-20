using System;
using System.Drawing;
using sysdrawingfont=System.Drawing.Font;
using sysdrawing2d=System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;
#if WINDOWS
using SlimDX;
using SlimDX.Direct3D9;
using d3d9font=SlimDX.Direct3D9.Font;
#else
using OpenTK;
using OpenTK.Graphics.OpenGL;
#endif
using BizHawk.Core;

namespace BizHawk.MultiClient
{
#if WINDOWS
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

		public unsafe void SetImage(DisplaySurface surface, int width, int height)
		{
			//this function fails if the width and height are zero
			if (width == 0 || height == 0) return;

			bool needsRecreating = false;

			//experiment: 
			//needsRecreating = true;

			if (Texture == null)
			{
				needsRecreating = true;
			}
			else
			{
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
				Texture = new Texture(GraphicsDevice, textureWidth, textureHeight, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
			}

			surface.FromBitmap();

			// Copy the image data to the texture.
			using (var Data = Texture.LockRectangle(0, LockFlags.None).Data)
			{
				if (imageWidth == textureWidth)
				{
					// Widths are the same, just dump the data across (easy!)
					Data.WriteRange(surface.PixelIntPtr, imageWidth * imageHeight << 2);
				}
				else
				{
					// Widths are different, need a bit of additional magic here to make them fit:
					long RowSeekOffset = (textureWidth - imageWidth) << 2;
					for (int r = 0, s = 0; r < imageHeight; ++r, s += imageWidth)
					{
						IntPtr src = new IntPtr(((byte*)surface.PixelPtr + r*surface.Stride));
						Data.WriteRange(src,imageWidth << 2);
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
#endif

	public interface IRenderer : IDisposable
	{
		void FastRenderAndPresent(DisplaySurface surface);
		void Render(DisplaySurface surface);
		void RenderOverlay(DisplaySurface surface);
		void Clear(Color color);
		void Present();
		bool Resized { get; set; }
		Size NativeSize { get; }
		/// <summary>
		/// convert coordinates
		/// </summary>
		/// <param name="p">desktop coordinates</param>
		/// <returns>ivideoprovider coordinates</returns>
		Point ScreenToScreen(Point p);
	}

	public class SysdrawingRenderPanel : IRenderer, IBlitter
	{
		private readonly sysdrawingfont MessageFont;
		private readonly sysdrawingfont AlertFont;
		private DisplaySurface tempBuffer;
		private Graphics g;
		private readonly SwappableDisplaySurfaceSet surfaceSet = new SwappableDisplaySurfaceSet();

		public bool Resized { get; set; }
		public void Dispose() { }
		public void Render(DisplaySurface surface)
		{
			backingControl.ReleaseCallback = RetainedViewportPanelDisposeCallback;

			lock (this)
				tempBuffer = surfaceSet.AllocateSurface(backingControl.Width, backingControl.Height, false);

			RenderInternal(surface);
		}

		class FontWrapper : IBlitterFont
		{
			public FontWrapper(sysdrawingfont font)
			{
				this.font = font;
			}
			
			public readonly sysdrawingfont font;
		}

		public Size NativeSize { get { return backingControl.ClientSize; } }

		IBlitterFont IBlitter.GetFontType(string fontType)
		{
			if (fontType == "MessageFont") return new FontWrapper(MessageFont);
			if (fontType == "AlertFont") return new FontWrapper(AlertFont);
			return null;
		}

		void IBlitter.Open()
		{
			g = Graphics.FromImage(tempBuffer.PeekBitmap());
			ClipBounds = new Rectangle(0, 0, NativeSize.Width, NativeSize.Height);
		}

		void IBlitter.Close()
		{
			g.Dispose();
		}

		void IBlitter.DrawString(string s, IBlitterFont font, Color color, float x, float y)
		{
			using (var brush = new SolidBrush(color))
				g.DrawString(s, ((FontWrapper)font).font, brush, x, y);
		}

		SizeF IBlitter.MeasureString(string s, IBlitterFont _font)
		{
			var font = ((FontWrapper)_font).font;
			return g.MeasureString(s, font);
		}

		public void Clear(Color color)
		{
			//todo
		}

		public Rectangle ClipBounds { get; set; }

		bool RetainedViewportPanelDisposeCallback(Bitmap bmp)
		{
			lock (this)
			{
				DisplaySurface tempSurface = DisplaySurface.DisplaySurfaceWrappingBitmap(bmp);
				surfaceSet.ReleaseSurface(tempSurface);
			}
			return false;
		}

		void RenderInternal(DisplaySurface surface, bool transparent = false)
		{
			using (var g = Graphics.FromImage(tempBuffer.PeekBitmap()))
			{
				g.PixelOffsetMode = sysdrawing2d.PixelOffsetMode.HighSpeed;
				g.InterpolationMode = Global.Config.DispBlurry ? sysdrawing2d.InterpolationMode.Bilinear : sysdrawing2d.InterpolationMode.NearestNeighbor;
				if (transparent) g.CompositingMode = sysdrawing2d.CompositingMode.SourceOver;
				else g.CompositingMode = sysdrawing2d.CompositingMode.SourceCopy;
				g.CompositingQuality = sysdrawing2d.CompositingQuality.HighSpeed;
				if (backingControl.Width == surface.Width && backingControl.Height == surface.Height)
					g.DrawImageUnscaled(surface.PeekBitmap(), 0, 0);
				else
					g.DrawImage(surface.PeekBitmap(), 0, 0, backingControl.Width, backingControl.Height);
			}
			if (!transparent)
			{
				lastsize = new Size(surface.Width, surface.Height);
			}
		}

		public void FastRenderAndPresent(DisplaySurface surface)
		{
			backingControl.SetBitmap((Bitmap)surface.PeekBitmap().Clone());
		}

		public void RenderOverlay(DisplaySurface surface)
		{
			RenderInternal(surface, true);
		}

		public void Present()
		{
			backingControl.SetBitmap(tempBuffer.PeekBitmap());
			tempBuffer = null;
		}

		public SysdrawingRenderPanel(RetainedViewportPanel control)
		{
			backingControl = control;
			MessageFont = new sysdrawingfont("Courier", 14, FontStyle.Bold, GraphicsUnit.Pixel);
			AlertFont = new sysdrawingfont("Courier", 14, FontStyle.Bold, GraphicsUnit.Pixel);
		}
		RetainedViewportPanel backingControl;

		Size lastsize = new Size(256, 192);
		public Point ScreenToScreen(Point p)
		{
			p = backingControl.PointToClient(p);
			Point ret = new Point(p.X * lastsize.Width / backingControl.Width,
				p.Y * lastsize.Height / backingControl.Height);
			return ret;
		}
	}

	public interface IBlitter
	{
		void Open();
		void Close();
		IBlitterFont GetFontType(string fontType);
		void DrawString(string s, IBlitterFont font, Color color, float x, float y);
		SizeF MeasureString(string s, IBlitterFont font);
		Rectangle ClipBounds { get; set; }
	}

	public interface IBlitterFont { }


#if WINDOWS

	public class Direct3DRenderPanel : IRenderer, IBlitter
	{
		public Color BackgroundColor { get; set; }
		public bool Resized { get; set; }
		public string FPS { get; set; }
		public string MT { get; set; }
		private readonly Direct3D d3d;
		private Device _device;
		private readonly Control backingControl;
		public ImageTexture Texture;
		private Sprite Sprite;
		private d3d9font MessageFont;
		private d3d9font AlertFont;

		class FontWrapper : IBlitterFont
		{
			public FontWrapper(d3d9font font)
			{
				this.font = font;
			}

			public readonly d3d9font font;
		}

		void IBlitter.Open()
		{
			ClipBounds = new Rectangle(0, 0, NativeSize.Width, NativeSize.Height);
		}
		void IBlitter.Close() {}

		private bool Vsync;

		public Size NativeSize { get { return backingControl.ClientSize; } }

		IBlitterFont IBlitter.GetFontType(string fontType)
		{
			if (fontType == "MessageFont") return new FontWrapper(MessageFont);
			if (fontType == "AlertFont") return new FontWrapper(AlertFont);
			return null;
		}

		Color4 col(Color c) { return new Color4(c.ToArgb()); }

		void IBlitter.DrawString(string s, IBlitterFont font, Color color, float x, float y)
		{
			((FontWrapper)font).font.DrawString(null, s, (int)x + 1, (int)y + 1, col(color));
		}

		SizeF IBlitter.MeasureString(string s, IBlitterFont _font)
		{
			var font = ((FontWrapper)_font).font;
			Rectangle r = font.MeasureString(null, s, DrawTextFormat.Left);
			return new SizeF(r.Width, r.Height);
		}

		public Rectangle ClipBounds { get; set; }

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

			if (_device != null)
			{
				_device.Dispose();
				_device = null;
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
		}

		private bool VsyncRequested
		{
			get
			{
				if (Global.ForceNoThrottle)
					return false;
				return Global.Config.VSyncThrottle || Global.Config.VSync;
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
			_device = new Device(d3d, 0, DeviceType.Hardware, backingControl.Handle, flags, pp);
			Sprite = new Sprite(_device);
			Texture = new ImageTexture(_device);

			MessageFont = new d3d9font(_device, 16, 0, FontWeight.Bold, 1, false, CharacterSet.Default, Precision.Default, FontQuality.Default, PitchAndFamily.Default | PitchAndFamily.DontCare, "Courier");
			AlertFont = new d3d9font(_device, 16, 0, FontWeight.ExtraBold, 1, true, CharacterSet.Default, Precision.Default, FontQuality.Default, PitchAndFamily.Default | PitchAndFamily.DontCare, "Courier");
			// NOTE: if you add ANY objects, like new fonts, textures, etc, to this method
			// ALSO add dispose code in DestroyDevice() or you will be responsible for VRAM memory leaks.

		}

		public void Render()
		{
			if (_device == null || Resized || Vsync != VsyncRequested)
				backingControl.Invoke(() => CreateDevice());

			Resized = false;
			_device.Clear(ClearFlags.Target, BackgroundColor, 1.0f, 0);
			Present();
		}

		public void FastRenderAndPresent(DisplaySurface surface)
		{
			Render(surface);
			Present();
		}

		public void RenderOverlay(DisplaySurface surface)
		{
			RenderWrapper(() => RenderExec(surface, true));
		}

		public void Render(DisplaySurface surface)
		{
			RenderWrapper(() => RenderExec(surface, false));
		}

		public void RenderWrapper(Action todo)
		{
			try
			{
				todo();
			}
			catch (Direct3D9Exception)
			{
				// Wait until device is available or user gets annoyed and closes app
				Result r;
				// it can take a while for the device to be ready again, so avoid sound looping during the wait
				if (Global.Sound != null) Global.Sound.StopSound();
				do
				{
					r = _device.TestCooperativeLevel();
					Thread.Sleep(100);
				} while (r == ResultCode.DeviceLost);
				if (Global.Sound != null) Global.Sound.StartSound();

				// lets try recovery!
				DestroyDevice();
				backingControl.Invoke(() => CreateDevice());
				todo();
			}
		}

		void RenderPrep()
		{
			if (_device == null || Resized || Vsync != VsyncRequested)
				backingControl.Invoke(() => CreateDevice());
			Resized = false;
		}

		public void Clear(Color color)
		{
			_device.Clear(ClearFlags.Target, col(color), 0.0f, 0);
		}

		private void RenderExec(DisplaySurface surface, bool overlay)
		{
			RenderPrep();
			if (surface == null)
			{
				Render();
				return;
			}

			Texture.SetImage(surface, surface.Width, surface.Height);

			if(!overlay) _device.Clear(ClearFlags.Target, BackgroundColor, 0.0f, 0);
			// figure out scaling factor
			float widthScale = (float)backingControl.Size.Width / surface.Width;
			float heightScale = (float)backingControl.Size.Height / surface.Height;
			float finalScale = Math.Min(widthScale, heightScale);

			_device.BeginScene();

			SpriteFlags flags = SpriteFlags.None;
			if (overlay) flags |= SpriteFlags.AlphaBlend;
			Sprite.Begin(flags);
			if (Global.Config.DispBlurry)
			{
				_device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Linear);
				_device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Linear);
			}
			else
			{
				_device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Point);
				_device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Point);
			}
			Sprite.Transform = Matrix.Scaling(finalScale, finalScale, 0f);
			Sprite.Draw(Texture.Texture, new Rectangle(0, 0, surface.Width, surface.Height), new Vector3(surface.Width / 2f, surface.Height / 2f, 0), new Vector3(backingControl.Size.Width / 2f / finalScale, backingControl.Size.Height / 2f / finalScale, 0), Color.White);
			Sprite.End();

			_device.EndScene();
		}

		public void Present()
		{
			// Present() is the most likely place to get DeviceLost, so we need to wrap it
			RenderWrapper(_Present);
		}

		private readonly System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
		private readonly long stopwatchthrottle = System.Diagnostics.Stopwatch.Frequency / 50;
		private long stopwatchtmr;
		private void _Present()
		{
			// according to the internet, D3DPRESENT_DONOTWAIT is not terribly reliable
			// so instead we measure the time the present takes, and drop the next present call if it was too long
			// this code isn't really very good
			if (Global.Config.VSync && !Global.Config.VSyncThrottle)
			{
				if (stopwatchtmr > stopwatchthrottle)
				{
					stopwatchtmr = 0;
					//Console.WriteLine('s');
				}
				else
				{
					stopwatch.Restart();
					//Device.GetSwapChain(0).Present(SlimDX.Direct3D9.Present.DoNotWait);
					_device.Present(SlimDX.Direct3D9.Present.None);
					stopwatch.Stop();
					stopwatchtmr += stopwatch.ElapsedTicks;
					//Console.WriteLine('.');
					stopwatchtmr -= stopwatchthrottle / 4;
				}
			}
			else
				_device.Present(SlimDX.Direct3D9.Present.None);
		}

		// not used anywhere?
		//public static EventWaitHandle vsyncEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

		private bool disposed;

		public void Dispose()
		{
			if (disposed == false)
			{
				disposed = true;
				DestroyDevice();
			}
		}

		public Point ScreenToScreen(Point p)
		{
			p = backingControl.PointToClient(p);
			Point ret = new Point(p.X * Texture.ImageWidth / backingControl.Width,
				p.Y * Texture.ImageHeight / backingControl.Height);
			return ret;
		}

	}
#endif
	/*
	public class OpenGLRenderPanel : OpenTK.GLControl, IRenderer
	{
		public bool Resized { get; set; }
		public string FPS { get; set; }
		public string MT { get; set; }
		public void Render(DisplaySurface surface)
		{
			MakeCurrent();
            int[] data = surface.ToIntArray();
            int[] flipped = new int[data.Length]; //Cheap trick to avoid using a texture... for now.
            int width = surface.Width, height = surface.Height;
            for (int i = 0; i < height; i++)
            {
                Array.Copy(data, i*width, flipped, (width*(height-1-i)),width);
            }
            GL.PixelZoom(Width*1.0f/width, Height*1.0f/height);
            GL.DrawPixels(surface.Width, surface.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, flipped);
            SwapBuffers();
		}
		public OpenGLRenderPanel() : base(OpenTK.Graphics.GraphicsMode.Default,2,1,OpenTK.Graphics.GraphicsContextFlags.Default)
		{
			
		}
		public void AddMessage(string msg) { }
		public void AddGUIText(string msg, int x, int y, bool alert, int anchor) { }
		public void ClearGUIText() { }
		public Size NativeSize { get { return this.Size; } }
	}*/

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
		public bool Alert;
		public int Anchor;
		public Color ForeColor;
		public Color BackGround;
	}
}
