using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

// http://www.codeproject.com/Articles/154680/A-customizable-NET-WinForms-Message-Box
namespace BizHawk.Client.EmuHawk.CustomControls
{
	/// <summary>
	/// A customizable Dialog box with 3 buttons, custom icon, and checkbox.
	/// </summary>
	partial class MsgBox : Form
	{
		/// <summary>
		/// Create a new instance of the dialog box with a message and title.
		/// </summary>
		/// <param name="message">Message text.</param>
		/// <param name="title">Dialog Box title.</param>
		public MsgBox(string message, string title)
			: this(message, title, MessageBoxIcon.None)
		{

		}

		/// <summary>
		/// Create a new instance of the dialog box with a message and title and a standard windows messagebox icon.
		/// </summary>
		/// <param name="message">Message text.</param>
		/// <param name="title">Dialog Box title.</param>
		/// <param name="icon">Standard system messagebox icon.</param>
		public MsgBox(string message, string title, MessageBoxIcon icon)
			: this(message, title, getMessageBoxIcon(icon))
		{

		}

		/// <summary>
		/// Create a new instance of the dialog box with a message and title and a custom windows icon.
		/// </summary>
		/// <param name="message">Message text.</param>
		/// <param name="title">Dialog Box title.</param>
		/// <param name="icon">Custom icon.</param>
		public MsgBox(string message, string title, Icon icon)
		{
			InitializeComponent();

			this.messageLbl.Text = message;
			this.Text = title;
			this.m_sysIcon = icon;

			if (this.m_sysIcon == null)
				this.messageLbl.Location = new Point(FORM_X_MARGIN, FORM_Y_MARGIN);
		}

		public void SetMessageToAutoSize()
		{
			this.messageLbl.AutoSize = true;
			this.messageLbl.MaximumSize = new Size(this.MaximumSize.Width - this.m_sysIcon.Width - 25, this.MaximumSize.Height);
		}

		/// <summary>
		/// Get system icon for MessageBoxIcon.
		/// </summary>
		/// <param name="icon">The MessageBoxIcon value.</param>
		/// <returns>SystemIcon type Icon.</returns>
		static Icon getMessageBoxIcon(MessageBoxIcon icon)
		{
			switch (icon)
			{
				case MessageBoxIcon.Asterisk:
					return SystemIcons.Asterisk;
				case MessageBoxIcon.Error:
					return SystemIcons.Error;
				case MessageBoxIcon.Exclamation:
					return SystemIcons.Exclamation;
				case MessageBoxIcon.Question:
					return SystemIcons.Question;
				default:
					return null;
			}
		}

		#region Setup API

		/// <summary>
		/// Min set width.
		/// </summary>
		int m_minWidth;
		/// <summary>
		/// Min set height.
		/// </summary>
		int m_minHeight;

		/// <summary>
		/// Sets the min size of the dialog box. If the text or button row needs more size then the dialog box will size to fit the text.
		/// </summary>
		/// <param name="width">Min width value.</param>
		/// <param name="height">Min height value.</param>
		public void SetMinSize(int width, int height)
		{
			m_minWidth = width;
			m_minHeight = height;
		}

		/// <summary>
		/// Create up to 3 buttons with no DialogResult values.
		/// </summary>
		/// <param name="names">Array of button names. Must of length 1-3.</param>
		public void SetButtons(params string[] names)
		{
			DialogResult[] drs = new DialogResult[names.Length];
			for (int i = 0; i < names.Length; i++)
				drs[i] = DialogResult.None;
			this.SetButtons(names, drs);
		}

		/// <summary>
		/// Create up to 3 buttons with given DialogResult values.
		/// </summary>
		/// <param name="names">Array of button names. Must of length 1-3.</param>
		/// <param name="results">Array of DialogResult values. Must be same length as names.</param>
		public void SetButtons(string[] names, DialogResult[] results)
		{
			this.SetButtons(names, results, 1);
		}

		/// <summary>
		/// Create up to 3 buttons with given DialogResult values.
		/// </summary>
		/// <param name="names">Array of button names. Must of length 1-3.</param>
		/// <param name="results">Array of DialogResult values. Must be same length as names.</param>
		/// <param name="def">Default Button number. Must be 1-3.</param>
		public void SetButtons(string[] names, DialogResult[] results, int def)
		{
			if (names == null)
				throw new ArgumentNullException("names", "Button Text is null");

			int count = names.Length;

			if (count < 1 || count > 3)
				throw new ArgumentException("Invalid number of buttons. Must be between 1 and 3.");

			//---- Set Button 1
			m_minButtonRowWidth += setButtonParams(btn1, names[0], def == 1 ? 1 : 2, results[0]);

			//---- Set Button 2
			if (count > 1)
			{
				m_minButtonRowWidth += setButtonParams(btn2, names[1], def == 2 ? 1 : 3, results[1]) + BUTTON_SPACE;
			}

			//---- Set Button 3
			if (count > 2)
			{
				m_minButtonRowWidth += setButtonParams(btn3, names[2], def == 3 ? 1 : 4, results[2]) + BUTTON_SPACE;
			}

		}

		/// <summary>
		/// The min required width of the button and checkbox row. Sum of button widths + checkbox width + margins.
		/// </summary>
		int m_minButtonRowWidth;

		/// <summary>
		/// Sets button text and returns the width.
		/// </summary>
		/// <param name="btn">Button object.</param>
		/// <param name="text">Text of the button.</param>
		/// <param name="tab">TabIndex of the button.</param>
		/// <param name="dr">DialogResult of the button.</param>
		/// <returns>Width of the button.</returns>
		static int setButtonParams(Button btn, string text, int tab, DialogResult dr)
		{
			btn.Text = text;
			btn.Visible = true;
			btn.DialogResult = dr;
			btn.TabIndex = tab;
			return btn.Size.Width;
		}

		/// <summary>
		/// Enables the checkbox. By default the checkbox is unchecked.
		/// </summary>
		/// <param name="text">Text of the checkbox.</param>
		public void SetCheckbox(string text)
		{
			this.SetCheckbox(text, false);
		}

		/// <summary>
		/// Enables the checkbox and the default checked state.
		/// </summary>
		/// <param name="text">Text of the checkbox.</param>
		/// <param name="chcked">Default checked state of the box.</param>
		public void SetCheckbox(string text, bool chcked)
		{
			this.chkBx.Visible = true;
			this.chkBx.Text = text;
			this.chkBx.Checked = chcked;
			this.m_minButtonRowWidth += this.chkBx.Size.Width + CHECKBOX_SPACE;
		}

		#endregion

		#region Sizes and Locations
		private void DialogBox_Load(object sender, EventArgs e)
		{
			if (!btn1.Visible)
				this.SetButtons(new string[] { "OK" }, new DialogResult[] { DialogResult.OK });

			m_minButtonRowWidth += 2 * FORM_X_MARGIN; //add margin to the ends

			this.setDialogSize();

			//this.setButtonRowLocations();

		}

		const int FORM_Y_MARGIN = 10;
		const int FORM_X_MARGIN = 16;
		const int BUTTON_SPACE = 5;
		const int CHECKBOX_SPACE = 15;
		const int TEXT_Y_MARGIN = 30;

		/// <summary>
		/// Auto fits the dialog box to fit the text and the buttons.
		/// </summary>
		void setDialogSize()
		{
			int requiredWidth = this.messageLbl.Location.X + this.messageLbl.Size.Width + FORM_X_MARGIN;
			requiredWidth = requiredWidth > m_minButtonRowWidth ? requiredWidth : m_minButtonRowWidth;

			int requiredHeight = this.messageLbl.Location.Y + this.messageLbl.Size.Height - this.btn2.Location.Y + this.ClientSize.Height + TEXT_Y_MARGIN;

			int minSetWidth = this.ClientSize.Width > this.m_minWidth ? this.ClientSize.Width : this.m_minWidth;
			int minSetHeight = this.ClientSize.Height > this.m_minHeight ? this.ClientSize.Height : this.m_minHeight;

			Size s = new Size();
			s.Width = requiredWidth > minSetWidth ? requiredWidth : minSetWidth;
			s.Height = requiredHeight > minSetHeight ? requiredHeight : minSetHeight;
			this.ClientSize = s;
		}

		/// <summary>
		/// Sets the buttons and checkboxe location.
		/// </summary>
		void setButtonRowLocations()
		{
			int formWidth = this.ClientRectangle.Width;

			int x = formWidth - FORM_X_MARGIN;
			int y = btn1.Location.Y;

			if (btn3.Visible)
			{
				x -= btn3.Size.Width;
				btn3.Location = new Point(x, y);
				x -= BUTTON_SPACE;
			}

			if (btn2.Visible)
			{
				x -= btn2.Size.Width;
				btn2.Location = new Point(x, y);
				x -= BUTTON_SPACE;
			}

			x -= btn1.Size.Width;
			btn1.Location = new Point(x, y);

			if (this.chkBx.Visible)
				this.chkBx.Location = new Point(FORM_X_MARGIN, this.chkBx.Location.Y);

		}

		#endregion

		#region Icon Pain
		/// <summary>
		/// The icon to paint.
		/// </summary>
		Icon m_sysIcon;

		/// <summary>
		/// Paint the System Icon in the top left corner.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (m_sysIcon != null)
			{
				Graphics g = e.Graphics;
				g.DrawIconUnstretched(m_sysIcon, new Rectangle(FORM_X_MARGIN, FORM_Y_MARGIN, m_sysIcon.Width, m_sysIcon.Height));
			}

			base.OnPaint(e);
		}
		#endregion

		#region Result API

		/// <summary>
		/// If visible checkbox was checked.
		/// </summary>
		public bool CheckboxChecked
		{
			get
			{
				return this.chkBx.Checked;
			}
		}

		DialogBoxResult m_result;
		/// <summary>
		/// Gets the button that was pressed.
		/// </summary>
		public DialogBoxResult DialogBoxResult
		{
			get
			{
				return m_result;
			}
		}

		private void btn_Click(object sender, EventArgs e)
		{
			if (sender == btn1)
				this.m_result = DialogBoxResult.Button1;
			else if (sender == btn2)
				this.m_result = DialogBoxResult.Button2;
			else if (sender == btn3)
				this.m_result = DialogBoxResult.Button3;

			if (((Button)sender).DialogResult == DialogResult.None)
				this.Close();
		}

		#endregion
	}

	public enum DialogBoxResult
	{
		Button1,
		Button2,
		Button3
	}
}
