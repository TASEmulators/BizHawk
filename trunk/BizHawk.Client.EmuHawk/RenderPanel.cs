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
	/// <summary>
	/// Handles the EmuHawk main presentation control - final display only.
	/// Compositing, filters, etc., should be handled by DisplayManager
	/// </summary>
	public class PresentationPanel : IBlitter
	{
		public PresentationPanel()
		{
			GL = GlobalWin.GL;

			GraphicsControl = GL.CreateGraphicsControl();
			GraphicsControl.Control.Dock = DockStyle.Fill;
			GraphicsControl.Control.BackColor = Color.Black;

			//prepare a renderer 
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

		public Control Control { get { return GraphicsControl.Control; } }
		public static implicit operator Control(PresentationPanel self) { return self.GraphicsControl.Control; }
		StringRenderer TheOneFont;
		public Bizware.BizwareGL.GraphicsControl GraphicsControl;
		static Bizware.BizwareGL.IGL GL;
		GuiRenderer Renderer;
		Texture2d LastSurfaceTexture;

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
			GraphicsControl.Begin();

			if (Resized || Vsync != VsyncRequested)
			{
				GraphicsControl.SetVsync(VsyncRequested);	
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
			}
			else
			{
				GL.LoadTextureData(LastSurfaceTexture, surface);
			}

			if(Global.Config.DispBlurry)
				LastSurfaceTexture.SetFilterLinear();
			else
				LastSurfaceTexture.SetFilterNearest();


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

			if (!overlay)
			{
				GL.ClearColor(Color.Black); //TODO GL - set from background color
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

}
