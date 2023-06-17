using System;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public class ClipboardEventTextBox : TextBox
	{
		protected override void WndProc(ref Message m)
		{
			const int WM_PASTE = 0x302;

			if (m.Msg == WM_PASTE)
			{
				bool containsText;
				string text;

				try
				{
					containsText = Clipboard.ContainsText();
					text = containsText ? Clipboard.GetText() : "";
				}
				catch (Exception)
				{
					// Clipboard is busy? No idea if this ever happens in practice
					return;
				}

				var args = new PasteEventArgs(containsText, text);
				OnPaste(args);

				if (args.Handled)
					return;
			}

			base.WndProc(ref m);
		}

		protected virtual void OnPaste(PasteEventArgs e)
		{ }

		/// <summary>
		/// Paste <paramref name="text"/> at selected position without exceeding the <see cref="TextBoxBase.MaxLength"/> limit.
		/// The pasted string will be truncated if necessary.
		/// </summary>
		/// <remarks>
		/// Does not raise <see cref="OnPaste"/>.
		/// </remarks>
		public void PasteWithMaxLength(string text)
		{
			if (MaxLength > 0)
			{
				int availableLength = MaxLength - TextLength + SelectionLength;
				if (text.Length > availableLength)
				{
					text = text.Substring(0, availableLength);
				}
			}
			Paste(text);
		}

		protected class PasteEventArgs : EventArgs
		{
			public bool ContainsText { get; }
			public string Text { get; }
			/// <summary>Prevents regular paste handling if set to <see langword="true"/>.</summary>
			public bool Handled { get; set; }

			public PasteEventArgs(bool containsText, string text)
			{
				ContainsText = containsText;
				Text = text;
			}
		}
	}
}
