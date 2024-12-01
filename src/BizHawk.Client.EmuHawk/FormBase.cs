#nullable enable

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public class FormBase : Form
	{
		private const string PLACEHOLDER_TITLE = "(will take value from WindowTitle/WindowTitleStatic)";

		/// <summary>
		/// Under Mono, <see cref="SystemColors.Control">SystemColors.Control</see> returns an ugly beige.<br/>
		/// This method recursively replaces the <see cref="Control.BackColor"/> of the given <paramref name="control"/> (can be a <see cref="Form"/>) with <see cref="Color.WhiteSmoke"/>
		/// iff they have the default of <see cref="SystemColors.Control">SystemColors.Control</see>.<br/>
		/// (Also adds a custom <see cref="ToolStrip.Renderer"/> to <see cref="ToolStrip">ToolStrips</see> to change their colors.)
		/// </summary>
		public static void FixBackColorOnControls(Control control)
		{
			if (control.BackColor == SystemColors.Control) control.BackColor = Color.WhiteSmoke;
			foreach (Control c1 in control.Controls)
			{
				if (c1 is ToolStrip ts) ts.Renderer = GlobalToolStripRenderer;
				else FixBackColorOnControls(c1);
			}
		}

		public static readonly ToolStripSystemRenderer GlobalToolStripRenderer = new();

		private string? _windowTitleStatic;

		public virtual bool BlocksInputWhenFocused
			=> true;

		public Config? Config { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override string Text
		{
			get => base.Text;
			set
			{
				if (DesignMode) return;
				throw new InvalidOperationException("window title can only be changed by calling " + nameof(UpdateWindowTitle) + " (which calls " + nameof(WindowTitle) + " getter)");
			}
		}

		protected virtual string WindowTitle => WindowTitleStatic;

		/// <remarks>To enforce the "static title" semantics for implementations, this getter will be called once and cached.</remarks>
		protected virtual string WindowTitleStatic
		{
			get
			{
				if (DesignMode) return PLACEHOLDER_TITLE;
				throw new NotImplementedException("you have to implement this; the Designer prevents this from being an abstract method");
			}
		}

		public FormBase() => base.Text = PLACEHOLDER_TITLE;

		protected override void OnLoad(EventArgs e)
		{
			try
			{
				base.OnLoad(e);
			}
			catch (Exception ex)
			{
				using ExceptionBox box = new(ex);
				box.ShowDialog(owner: this);
				Close();
				return;
			}
			if (OSTailoredCode.IsUnixHost) FixBackColorOnControls(this);
			UpdateWindowTitle();
		}

		public void UpdateWindowTitle()
			=> base.Text = Config?.UseStaticWindowTitles == true
				? (_windowTitleStatic ??= WindowTitleStatic)
				: WindowTitle;
	}
}
