using System.Drawing;
using System.Windows.Forms;

namespace BizHawk
{
	public class TextDebugView : Control
	{
		public TextDebugView()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.Clear(SystemColors.Control);
			Font font = new Font(new FontFamily("Courier New"), 8);
			e.Graphics.DrawString(Text, font, Brushes.Black,0,0);
			font.Dispose();
		}

		public override string Text
		{
			get
			{
				return base.Text;
			}
			set
			{
				base.Text = value;
				Invalidate();
			}
		}
	}

	public class TextDebugForm : Form
	{
		public TextDebugView view = new TextDebugView();
		public TextDebugForm()
		{
			view.Dock = DockStyle.Fill;
			Controls.Add(view);
		}

		public override string Text
		{
			get
			{
				return view.Text;
			}
			set
			{
				view.Text = value;
			}
		}
	}
}