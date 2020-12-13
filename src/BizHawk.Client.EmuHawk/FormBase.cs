#nullable enable

using System;
using System.ComponentModel;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class FormBase : Form
	{
		private string? _windowTitleStatic;

		public virtual bool BlocksInputWhenFocused { get; } = true;

		public Config? Config { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override string Text
		{
			get => base.Text;
			set => throw new InvalidOperationException("window title can only be changed by calling " + nameof(UpdateWindowTitle) + " (which calls " + nameof(WindowTitle) + " getter)");
		}

		protected virtual string WindowTitle => WindowTitleStatic;

		/// <remarks>To enforce the "static title" semantics for implementations, this getter will be called once and cached.</remarks>
		protected virtual string WindowTitleStatic => DesignMode
			? "(will take value from WindowTitle/WindowTitleStatic)"
			: throw new NotImplementedException("you have to implement this; the Designer prevents this from being an abstract method");

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			UpdateWindowTitle();
		}

		public void UpdateWindowTitle()
			=> base.Text = Config?.UseStaticWindowTitles == true
				? (_windowTitleStatic ??= WindowTitleStatic)
				: WindowTitle;
	}
}
