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

		/// <summary>removes transparency from an image by combining it with a solid background</summary>
		public static Image FillImageBackground(Image img, Color c)
		{
			Bitmap result = new(width: img.Width, height: img.Height);
			using var g = Graphics.FromImage(result);
			g.Clear(c);
			g.DrawImage(img, x: 0, y: 0, width: img.Width, height: img.Height);
			return result;
		}

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

		public bool MenuIsOpen { get; private set; }

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
#pragma warning disable CA1065 // yes, really throw
				throw new NotImplementedException("you have to implement this; the Designer prevents this from being an abstract method");
#pragma warning restore CA1065
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

			if (MainMenuStrip != null)
			{
				MainMenuStrip.MenuActivate += (_, _) => MenuIsOpen = true;
				MainMenuStrip.MenuDeactivate += (_, _) => MenuIsOpen = false;
			}
		}

		public void UpdateWindowTitle()
			=> base.Text = Config?.UseStaticWindowTitles == true
				? (_windowTitleStatic ??= WindowTitleStatic)
				: WindowTitle;

		// Alt key hacks. We need this in order for hotkey bindings with alt to work.
		private const int WM_SYSCOMMAND = 0x0112;
		private const int SC_KEYMENU = 0xF100;
		internal void SendAltCombination(char character)
		{
			var m = new Message { WParam = new IntPtr(SC_KEYMENU), LParam = new IntPtr(character), Msg = WM_SYSCOMMAND, HWnd = Handle };
			if (character == ' ') base.WndProc(ref m);
			else if (character >= 'a' && character <= 'z') base.ProcessDialogChar(character);
		}

		internal void FocusToolStipMenu()
		{
			var m = new Message { WParam = new IntPtr(SC_KEYMENU), LParam = new IntPtr(0), Msg = WM_SYSCOMMAND, HWnd = Handle };
			base.WndProc(ref m);
		}

		protected override void WndProc(ref Message m)
		{
			if (!BlocksInputWhenFocused)
			{
				// this is necessary to trap plain alt keypresses so that only our hotkey system gets them
				if (m.Msg == WM_SYSCOMMAND)
				{
					if (m.WParam.ToInt32() == SC_KEYMENU && m.LParam == IntPtr.Zero)
					{
						return;
					}
				}
			}

			base.WndProc(ref m);
		}

		protected override bool ProcessDialogChar(char charCode)
		{
			if (BlocksInputWhenFocused) return base.ProcessDialogChar(charCode);
			// this is necessary to trap alt+char combinations so that only our hotkey system gets them
			return (ModifierKeys & Keys.Alt) != 0 || base.ProcessDialogChar(charCode);
		}
	}
}
