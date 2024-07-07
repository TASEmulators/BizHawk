using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class ExceptionBox : Form
	{
		public ExceptionBox(string str)
		{
			InitializeComponent();
			txtException.Text = str;
			timer1.Start();
		}

		public ExceptionBox(Exception ex): this(ex.ToString()) {}

		private void btnCopy_Click(object sender, EventArgs e)
		{
			DoCopy();
		}

		private void DoCopy()
		{
			string txt = txtException.Text;
			Clipboard.SetText(txt);
			try
			{
				if (Clipboard.GetText() == txt)
				{
					lblDone.Text = "Done!";
					lblDone.ForeColor = SystemColors.ControlText;
					return;
				}
			}
			catch
			{
			}

			lblDone.Text = "ERROR!";
			lblDone.ForeColor = SystemColors.ControlText;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == (Keys.C | Keys.Control))
			{
				if (txtException.SelectionLength > 0)
					return false;
				DoCopy();
				return true;
			}

			return false;
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			int a = lblDone.ForeColor.A - 16;
			if (a < 0) a = 0;
			lblDone.ForeColor = Color.FromArgb(a, lblDone.ForeColor);
		}

		//http://stackoverflow.com/questions/2636065/alpha-in-forecolor
		private class MyLabel : Label
		{
			protected override void OnPaint(PaintEventArgs e)
			{
				Rectangle rc = ClientRectangle;
				StringFormat fmt = new StringFormat(StringFormat.GenericTypographic);
				using var br = new SolidBrush(ForeColor);
				e.Graphics.DrawString(this.Text, this.Font, br, rc, fmt);
			}
		}

	}
}
