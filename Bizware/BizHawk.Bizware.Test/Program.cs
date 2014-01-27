using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Bizware.BizwareGL;

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
			GraphicsControl c = igl.CreateGraphicsControl();
			tf.Controls.Add(c);
			c.Control.Dock = System.Windows.Forms.DockStyle.Fill;
			tf.FormClosing += (object sender, System.Windows.Forms.FormClosingEventArgs e) =>
				{
					tf.Controls.Remove(c);
					c.Dispose();
					c = null;
				};
			tf.Show();

			c.SetVsync(false);

			DateTime start = DateTime.Now;
			int wobble = 0;
			for (; ; )
			{
				if (c == null) break;

				c.Begin();
				
				igl.ClearColor(Color.Red);
				igl.Clear(BizwareGL.ClearBufferMask.ColorBufferBit);

				int frame = (int)((DateTime.Now - start).TotalSeconds) % testArts.Count;

				gr.Begin(c.Control.ClientSize.Width, c.Control.ClientSize.Height);
				sr.RenderString(gr, 0, 0, "60 fps");
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

				System.Windows.Forms.Application.DoEvents();
			}
		}
	}
}
