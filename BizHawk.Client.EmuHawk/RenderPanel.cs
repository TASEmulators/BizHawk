using System;
using System.Drawing;
using sd=System.Drawing;
using sysdrawingfont=System.Drawing.Font;
using sysdrawing2d=System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;
#if WINDOWS
using SlimDX;
#endif

using BizHawk.Client.Common;
using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{
	public interface IRenderer : IDisposable
	{
		void RenderOverlay(DisplaySurface surface);
		void Render(BitmapBuffer surface);
		void Clear(Color color);
		void Present();
		bool Resized { get; set; }
		Size NativeSize { get; }
		
		/// <summary>
		/// convert coordinates. this is a dumb name
		/// </summary>
		/// <param name="p">desktop coordinates</param>
		/// <returns>ivideoprovider coordinates</returns>
		sd.Point ScreenToScreen(sd.Point p);
	}

	//you might think it's cool to make this reusable for other windows, but it's probably a pipe dream. dont even try it. at least, refactor it into a simple render panel and some kind of NicePresentationWindow class
	public class BizwareGLRenderPanel : IRenderer, IBlitter
	{

		public BizwareGLRenderPanel()
		{
			GL = GlobalWin.GL;

			GraphicsControl = GL.CreateGraphicsControl();
			Renderer = new GuiRenderer(GL);

			//pass through these events to the form. we might need a more scalable solution for mousedown etc. for zapper and whatnot.
			//http://stackoverflow.com/questions/547172/pass-through-mouse-events-to-parent-control (HTTRANSPARENT)
			GraphicsControl.Control.MouseDoubleClick += (o, e) => HandleFullscreenToggle(o, e);
			GraphicsControl.Control.MouseClick += (o, e) => GlobalWin.MainForm.MainForm_MouseClick(o, e);

			using (var xml = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Client.EmuHawk.Resources.courier16px.fnt"))
			using (var tex = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Client.EmuHawk.Resources.courier16px_0.png"))
				TheOneFont = new StringRenderer(GL, xml, tex);
		}

		public void Dispose()
		{
			//this hasnt been analyzed real well yet
			Renderer.Dispose();
			GraphicsControl.Dispose();
			TheOneFont.Dispose();
			if (LastSurfaceTexture != null)
				LastSurfaceTexture.Dispose();
			//gl shouldnt be disposed! it should be a global resource! probably managed elsewhere
		}

		private void HandleFullscreenToggle(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				GlobalWin.MainForm.ToggleFullscreen();
		}

		class FontWrapper : IBlitterFont
		{
			public FontWrapper(StringRenderer font)
			{
				this.font = font;
			}

			public readonly StringRenderer font;
		}


		void IBlitter.Open()
		{
			Renderer.Begin(GraphicsControl.Control.ClientSize.Width, GraphicsControl.Control.ClientSize.Height);
			Renderer.SetBlendState(GL.BlendNormal);
			ClipBounds = new sd.Rectangle(0, 0, NativeSize.Width, NativeSize.Height);
		}
		void IBlitter.Close() {
			Renderer.End();
		}
		IBlitterFont IBlitter.GetFontType(string fontType) { return new FontWrapper(TheOneFont); }
		void IBlitter.DrawString(string s, IBlitterFont font, Color color, float x, float y)
		{
			var stringRenderer = ((FontWrapper)font).font;
			Renderer.SetModulateColor(color);
			stringRenderer.RenderString(Renderer, x, y, s);
			Renderer.SetModulateColorWhite();
		}
		SizeF IBlitter.MeasureString(string s, IBlitterFont font)
		{
			var stringRenderer = ((FontWrapper)font).font;
			return stringRenderer.Measure(s);
		}
		public sd.Rectangle ClipBounds { get; set; }

		StringRenderer TheOneFont;
		public Bizware.BizwareGL.GraphicsControl GraphicsControl;
		static Bizware.BizwareGL.IGL GL;
		GuiRenderer Renderer;
		Texture2d LastSurfaceTexture;

		public bool Resized { get; set; }

		public void Clear(Color color)
		{
			GraphicsControl.Begin();
		}

		public sd.Point ScreenToScreen(sd.Point p)
		{
			p = GraphicsControl.Control.PointToClient(p);
			sd.Point ret = new sd.Point(p.X * sw / GraphicsControl.Control.Width,
				p.Y * sh / GraphicsControl.Control.Height);
			return ret;
		}

		public Size NativeSize { get { return GraphicsControl.Control.ClientSize; } }

		//the hell is this doing here? horrible
		private bool VsyncRequested
		{
			get
			{
				if (Global.ForceNoThrottle)
					return false;
				return Global.Config.VSyncThrottle || Global.Config.VSync;
			}
		}

		public void RenderOverlay(DisplaySurface surface)
		{
			RenderExec(null, surface);
		}

		public void Render(BitmapBuffer surface)
		{
			RenderExec(surface,null);
		}

		private bool Vsync;
		int sw=1, sh=1;
		public void RenderExec(BitmapBuffer surface, DisplaySurface displaySurface)
		{
			if (Resized || Vsync != VsyncRequested)
			{
				//recreate device
			}

			bool overlay = false;
			if(displaySurface != null)
			{
				overlay = true;
				displaySurface.ToBitmap(false);
				surface = new BitmapBuffer(displaySurface.PeekBitmap(), new BitmapLoadOptions());
			}

			if (sw != surface.Width || sh != surface.Height || LastSurfaceTexture == null)
			{
				LastSurfaceTexture = GL.LoadTexture(surface);
				LastSurfaceTexture.SetFilterNearest();
			}
			else
			{
				GL.LoadTextureData(LastSurfaceTexture, surface);
			}

			sw = surface.Width;
			sh = surface.Height;

			Resized = false;

			// figure out scaling factor
			float vw = (float)GraphicsControl.Control.Width;
			float vh = (float)GraphicsControl.Control.Height;
			float widthScale =  vw / surface.Width;
			float heightScale = vh / surface.Height;
			float finalScale = Math.Min(widthScale, heightScale);
			float dx = (int)((vw - finalScale * surface.Width)/2);
			float dy = (int)((vh - finalScale * surface.Height)/2);

			GraphicsControl.Begin();
			if (!overlay)
			{
				GL.ClearColor(Color.Black); //TODO set from background color
				GL.Clear(ClearBufferMask.ColorBufferBit);
			}
			
			Renderer.Begin(GraphicsControl.Control.ClientSize.Width, GraphicsControl.Control.ClientSize.Height);
			if (overlay)
				Renderer.SetBlendState(GL.BlendNormal);
			else Renderer.SetBlendState(GL.BlendNone);
			Renderer.Modelview.Translate(dx, dy);
			Renderer.Modelview.Scale(finalScale);
			Renderer.Draw(LastSurfaceTexture);
			Renderer.End();
			GraphicsControl.End();
		}

		public void Present()
		{
			GraphicsControl.Begin();
			GraphicsControl.SwapBuffers();
			GraphicsControl.End();
		}

		public void RenderOverlay(BitmapBuffer surface)
		{
			//TODO GL - draw transparent overlay
		}
	}

	

	public interface IBlitter
	{
		void Open();
		void Close();
		IBlitterFont GetFontType(string fontType);
		void DrawString(string s, IBlitterFont font, Color color, float x, float y);
		SizeF MeasureString(string s, IBlitterFont font);
		sd.Rectangle ClipBounds { get; set; }
	}

	public interface IBlitterFont { }



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
