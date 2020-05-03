using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.BizwareGL.Drivers.OpenTK;

using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.Test
{
	class Program
	{

		static unsafe void Main(string[] args)
		{
			BizHawk.Bizware.BizwareGL.IGL igl = new BizHawk.Bizware.BizwareGL.Drivers.OpenTK.IGL_TK();

			List<Art> testArts = new List<Art>();
			ArtManager am = new ArtManager(igl);
			foreach (var name in typeof(Program).Assembly.GetManifestResourceNames())
				if (name.Contains("flame"))
					testArts.Add(am.LoadArt(typeof(Program).Assembly.GetManifestResourceStream(name)));
			var smile = am.LoadArt(typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Bizware.Test.TestImages.smile.png"));
			am.Close(true);
			StringRenderer sr;
			using (var xml = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Bizware.Test.TestImages.courier16px.fnt"))
			using (var tex = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Bizware.Test.TestImages.courier16px_0.png"))
				sr = new StringRenderer(igl, xml, tex);

			GuiRenderer gr = new GuiRenderer(igl);

			TestForm tf = new TestForm();
			RetainedGraphicsControl c = new RetainedGraphicsControl(igl);
			tf.Controls.Add(c);
			c.Dock = System.Windows.Forms.DockStyle.Fill;
			tf.FormClosing += (object sender, System.Windows.Forms.FormClosingEventArgs e) =>
				{
					tf.Controls.Remove(c);
					c.Dispose();
					c = null;
				};
			tf.Show();

			//tf.Paint += (object sender, PaintEventArgs e) => c.Refresh();

			c.SetVsync(false);

			//create a render target
			RenderTarget rt = igl.CreateRenderTarget(60, 60);
			rt.Bind();
			igl.SetClearColor(Color.Blue);
			igl.Clear(ClearBufferMask.ColorBufferBit);
			gr.Begin(60, 60, true);
			gr.Draw(smile);
			gr.End();
			rt.Unbind();

			Texture2d rttex2d = igl.LoadTexture(rt.Texture2d.Resolve());

			//test retroarch shader
			RenderTarget rt2 = igl.CreateRenderTarget(240, 240);
			rt2.Bind();
			igl.SetClearColor(Color.CornflowerBlue);
			igl.Clear(ClearBufferMask.ColorBufferBit);
			RetroShader shader;
			using (var stream = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Bizware.Test.TestImages.4xSoft.glsl"))
				shader = new RetroShader(igl, new System.IO.StreamReader(stream).ReadToEnd());
			igl.SetBlendState(igl.BlendNone);
			shader.Run(rttex2d, new Size(60, 60), new Size(240, 240), true);


			bool running = true;
			c.MouseClick += (object sender, MouseEventArgs e) =>
			{
				if(e.Button == MouseButtons.Left)
					running ^= true;
				if (e.Button == MouseButtons.Right)
					c.Retain ^= true;
			};

			DateTime start = DateTime.Now;
			int wobble = 0;
			for (; ; )
			{
				if (c == null) break;

				if (running)
				{
					c.Begin();

					igl.SetClearColor(Color.Red);
					igl.Clear(ClearBufferMask.ColorBufferBit);

					int frame = (int)((DateTime.Now - start).TotalSeconds) % testArts.Count;

					gr.Begin(c.ClientSize.Width, c.ClientSize.Height);
					gr.SetBlendState(igl.BlendNormal);

					gr.SetModulateColor(Color.Green);
					gr.RectFill(250, 0, 16, 16);

					gr.SetBlendState(igl.BlendNone);
					gr.Draw(rttex2d, 0, 20);
					gr.SetBlendState(igl.BlendNormal);

					sr.RenderString(gr, 0, 0, "?? fps");
					gr.SetModulateColor(Color.FromArgb(255, 255, 255, 255));
					gr.SetCornerColor(0, OpenTK.Graphics.Color4.Red);
					gr.Draw(rt2.Texture2d, 0, 0);
					gr.SetCornerColor(0, OpenTK.Graphics.Color4.White);
					gr.SetModulateColorWhite();
					gr.Modelview.Translate((float)Math.Sin(wobble / 360.0f) * 50, 0);
					gr.Modelview.Translate(100, 100);
					gr.Modelview.Push();
					gr.Modelview.Translate(testArts[frame].Width, 0);
					gr.Modelview.Scale(-1, 1);
					wobble++;
					gr.SetModulateColor(Color.Yellow);
					gr.DrawFlipped(testArts[frame], true, false);
					gr.SetModulateColorWhite();
					gr.Modelview.Pop();
					gr.SetBlendState(igl.BlendNormal);
					gr.Draw(smile);

					gr.End();


					c.SwapBuffers();
					c.End();
				}

				System.Windows.Forms.Application.DoEvents();
				System.Threading.Thread.Sleep(0);
			}
		}
	}
}
