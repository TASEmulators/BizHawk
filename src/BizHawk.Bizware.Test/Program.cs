using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.Graphics;
using BizHawk.Client.EmuHawk;

namespace BizHawk.Bizware.Test
{
	public static class Program
	{
		static Program()
		{
			AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
			{
				lock (AppDomain.CurrentDomain)
				{
					var firstAsm = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), asm => asm.FullName == args.Name);
					if (firstAsm is not null) return firstAsm;
					var guessFilename = Path.Combine(AppContext.BaseDirectory, "dll", $"{new AssemblyName(args.Name).Name}.dll");
					return File.Exists(guessFilename) ? Assembly.LoadFile(guessFilename) : null;
				}
			};
		}

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
			IGL igl = new IGL_OpenGL(2, 0, false);
			ArtManager am = new(igl);
			var testArts = typeof(Program).Assembly.GetManifestResourceNames().Where(s => s.Contains("flame"))
				.Select(s => am.LoadArt(ReflectionCache.EmbeddedResourceStream(s.Substring(21)))) // ReflectionCache adds back the prefix
				.ToList();
			var smile = am.LoadArt(ReflectionCache.EmbeddedResourceStream("TestImages.smile.png"));
			am.Close();
			StringRenderer sr;
			using (var xml = ReflectionCache.EmbeddedResourceStream("TestImages.courier16px.fnt"))
			using (var tex = ReflectionCache.EmbeddedResourceStream("TestImages.courier16px_0.png"))
			{
				sr = new StringRenderer(igl, xml, tex);
			}

			GuiRenderer gr = new(igl);

			RetainedGraphicsControl? c = new(igl) { Dock = DockStyle.Fill };
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
			igl.SetClearColor(Color.Blue);
			igl.Clear(ClearBufferMask.ColorBufferBit);
			gr.Begin(60, 60);
			gr.Draw(smile);
			gr.End();
			rt.Unbind();

			var rttex2d = igl.LoadTexture(rt.Texture2d.Resolve());

			// test retroarch shader
			var rt2 = igl.CreateRenderTarget(240, 240);
			rt2.Bind();
			igl.SetClearColor(Color.CornflowerBlue);
			igl.Clear(ClearBufferMask.ColorBufferBit);
			RetroShader shader;
			using (var stream = ReflectionCache.EmbeddedResourceStream("TestImages.4xSoft.glsl"))
			{
				shader = new(igl, new StreamReader(stream).ReadToEnd());
			}
			igl.SetBlendState(igl.BlendNoneCopy);
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

					igl.SetClearColor(Color.Red);
					igl.Clear(ClearBufferMask.ColorBufferBit);

					var frame = (int) (DateTime.Now - start).TotalSeconds % testArts.Count;

					gr.Begin(c.ClientSize.Width, c.ClientSize.Height);
					gr.SetBlendState(igl.BlendNormal);

					gr.SetModulateColor(Color.Green);
					gr.RectFill(250, 0, 16, 16);

					gr.SetBlendState(igl.BlendNoneCopy);
					gr.Draw(rttex2d, 0, 20);
					gr.SetBlendState(igl.BlendNormal);

					sr.RenderString(gr, 0, 0, "?? fps");
					gr.SetModulateColor(Color.FromArgb(255, 255, 255, 255));
					gr.SetCornerColor(0, new(1.0f, 0.0f, 0.0f, 1.0f));
					gr.Draw(rt2.Texture2d, 0, 0);
					gr.SetCornerColor(0, new(1.0f, 1.0f, 1.0f, 1.0f));
					gr.SetModulateColorWhite();
					gr.Modelview.Translate((float) Math.Sin(wobble / 360.0f) * 50, 0);
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

				Application.DoEvents();
				Thread.Sleep(0);
			}
		}
	}
}
