using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Bizware.Graphics;
using BizHawk.Bizware.Graphics.Controls;

namespace BizHawk.Bizware.Test
{
	public static class Program
	{
		public static void Main() => RunTest();

		private sealed class TestForm : Form
		{
			public TestForm()
			{
				SuspendLayout();
				AutoScaleDimensions = new(6F, 13F);
				AutoScaleMode = AutoScaleMode.Font;
				ClientSize = new(292, 273);
				Name = "TestForm";
				Text = "TestForm";
				ResumeLayout();
			}
		}

		private static void RunTest()
		{
			IGL igl = new IGL_OpenGL();
			// graphics control must be made right away to create the OpenGL context
			RetainedGraphicsControl? c = new(igl) { Dock = DockStyle.Fill, BackColor = Color.Black };

			var testTexs = typeof(Program).Assembly.GetManifestResourceNames().Where(s => s.Contains("flame"))
				.Select(s => igl.LoadTexture(ReflectionCache.EmbeddedResourceStream(s[21..]))) // ReflectionCache adds back the prefix
				.ToList();
			var smile = igl.LoadTexture(ReflectionCache.EmbeddedResourceStream("TestImages.smile.png"))!;
			StringRenderer sr;
			using (var xml = ReflectionCache.EmbeddedResourceStream("TestImages.courier16px.fnt"))
			using (var tex = ReflectionCache.EmbeddedResourceStream("TestImages.courier16px_0.png"))
			{
				sr = new StringRenderer(igl, xml, tex);
			}

			GuiRenderer gr = new(igl);
			TestForm tf = new() { Controls = { c } };
			tf.FormClosing += (_, _) =>
			{
				tf.Controls.Remove(c);
				c.Dispose();
				c = null;
			};
			tf.Show();

#if false
			tf.Paint += (_, _) => c.Refresh();
#endif

			c.SetVsync(false);

			// create a render target
			var rt = igl.CreateRenderTarget(60, 60);
			rt.Bind();
			igl.ClearColor(Color.Blue);
			gr.Begin(60, 60);
			gr.Draw(smile);
			gr.End();
			igl.BindDefaultRenderTarget();

			var rttex2d = igl.LoadTexture(rt.Resolve())!;

			// test retroarch shader
			var rt2 = igl.CreateRenderTarget(240, 240);
			rt2.Bind();
			igl.ClearColor(Color.CornflowerBlue);
			RetroShader shader;
			using (var stream = ReflectionCache.EmbeddedResourceStream("TestImages.4xSoft.glsl"))
			{
				shader = new(igl, new StreamReader(stream).ReadToEnd());
			}
			igl.DisableBlending();
			shader.Run(rttex2d, new Size(60, 60), new Size(240, 240), true);

			var running = true;
			c.MouseClick += (_, args) =>
			{
				if (args.Button == MouseButtons.Left) running ^= true;
				else if (args.Button == MouseButtons.Right) c.Retain ^= true;
			};

			var start = DateTime.Now;
			var wobble = 0;
			while (c is not null)
			{
				if (running)
				{
					c.Begin();

					igl.ClearColor(Color.Red);

					var frame = (int) (DateTime.Now - start).TotalSeconds % testTexs.Count;

					gr.Begin(c.ClientSize.Width, c.ClientSize.Height);
					gr.EnableBlending();

					gr.SetModulateColor(Color.Green);
					gr.DrawSubrect(null, 250, 0, 16, 16, 0, 0, 1, 1);

					gr.DisableBlending();
					gr.Draw(rttex2d, 0, 20);
					gr.EnableBlending();

					sr.RenderString(gr, 0, 0, "?? fps");
					gr.SetModulateColor(Color.FromArgb(255, 255, 255, 255));
					gr.SetCornerColor(0, new(1.0f, 0.0f, 0.0f, 1.0f));
					gr.Draw(rt2, 0, 0);
					gr.SetCornerColor(0, new(1.0f, 1.0f, 1.0f, 1.0f));
					gr.SetModulateColorWhite();
					gr.ModelView.Translate((float) Math.Sin(wobble / 360.0f) * 50, 0);
					gr.ModelView.Translate(100, 100);
					gr.ModelView.Push();
					gr.ModelView.Translate(testTexs[frame].Width, 0);
					gr.ModelView.Scale(-1, 1);
					wobble++;
					gr.SetModulateColor(Color.Yellow);
					gr.DrawSubrect(testTexs[frame], 0, 0, testTexs[frame].Width, testTexs[frame].Height, 1, 0, 0, 1);
					gr.SetModulateColorWhite();
					gr.ModelView.Pop();
					gr.EnableBlending();
					gr.Draw(smile);

					gr.End();

					c.SwapBuffers();
					c.End();
				}

				Application.DoEvents();
				Thread.Sleep(0);
			}
		}
	}
}
