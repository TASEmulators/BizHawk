using System.Drawing;
using System.Windows.Forms;

// http://www.codeproject.com/Articles/154680/A-customizable-NET-WinForms-Message-Box
namespace BizHawk.Client.EmuHawk.CustomControls
{
	/// <summary>
	/// A customizable Dialog box with 3 buttons, custom icon, and checkbox.
	/// </summary>
	internal partial class MsgBox : Form
	{
		private readonly Icon _msgIcon;
		private static readonly int FormYMargin = UIHelper.ScaleY(10);
		private static readonly int FormXMargin = UIHelper.ScaleX(16);
		private static readonly int ButtonSpace = UIHelper.ScaleX(5);
		private static readonly int TextYMargin = UIHelper.ScaleY(30);

		// The min required width of the button and checkbox row. Sum of button widths + checkbox width + margins.
		private int _minButtonRowWidth;

		/// <summary>
		/// Create a new instance of the dialog box with a message and title and a standard windows MessageBox icon.
		/// </summary>
		/// <param name="message">Message text.</param>
		/// <param name="title">Dialog Box title.</param>
		/// <param name="boxIcon">Standard system MessageBox icon.</param>
		public MsgBox(string message, string title, MessageBoxIcon boxIcon)
		{
			var icon = GetMessageBoxIcon(boxIcon);
			InitializeComponent();
			Icon = Properties.Resources.MsgBoxIcon;

			ControlBox = false; // Do not set in designer (causes problems with auto scaling)
			messageLbl.Text = message;
			Text = title;
			_msgIcon = icon;

			if (_msgIcon == null)
			{
				messageLbl.Location = new Point(FormXMargin, FormYMargin);
			}
		}

		/// <summary>
		/// Gets the button that was pressed.
		/// </summary>
		public DialogBoxResult DialogBoxResult { get; private set; }

		public void SetMessageToAutoSize()
		{
			messageLbl.MaximumSize = new Size(MaximumSize.Width - _msgIcon.Width - UIHelper.ScaleX(25), MaximumSize.Height);
		}

		/// <summary>
		/// Create up to 3 buttons with given DialogResult values.
		/// </summary>
		/// <param name="names">Array of button names. Must of length 1-3.</param>
		/// <param name="results">Array of DialogResult values. Must be same length as names.</param>
		/// <param name="def">Default Button number. Must be 1-3.</param>
		/// <exception cref="ArgumentException">length of <paramref name="names"/> is not in range <c>1..3</c></exception>
		/// <exception cref="ArgumentNullException"><paramref name="names"/> is null</exception>
		public void SetButtons(string[] names, DialogResult[] results, int def = 1)
		{
			if (names == null)
			{
				throw new ArgumentNullException(nameof(names), "Button Text is null");
			}

			int count = names.Length;
			if (count is < 1 or > 3) throw new ArgumentException(message: "Invalid number of buttons. Must be between 1 and 3.", paramName: nameof(names));

			//---- Set Button 1
			_minButtonRowWidth += SetButtonParams(btn1, names[0], def == 1 ? 1 : 2, results[0]);

			//---- Set Button 2
			if (count > 1)
			{
				_minButtonRowWidth += SetButtonParams(btn2, names[1], def == 2 ? 1 : 3, results[1]) + ButtonSpace;
			}

			//---- Set Button 3
			if (count > 2)
			{
				_minButtonRowWidth += SetButtonParams(btn3, names[2], def == 3 ? 1 : 4, results[2]) + ButtonSpace;
			}

		}

		/// <summary>
		/// Paint the System Icon in the top left corner.
		/// </summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (_msgIcon != null)
			{
				e.Graphics.DrawIconUnstretched(_msgIcon, new Rectangle(FormXMargin, FormYMargin, _msgIcon.Width, _msgIcon.Height));
			}

			base.OnPaint(e);
		}

		// Get system icon for MessageBoxIcon.
		private static Icon GetMessageBoxIcon(MessageBoxIcon icon)
		{
			return icon switch
			{
				MessageBoxIcon.Asterisk => SystemIcons.Asterisk,
				MessageBoxIcon.Error => SystemIcons.Error,
				MessageBoxIcon.Exclamation => SystemIcons.Exclamation,
				MessageBoxIcon.Question => SystemIcons.Question,
				_ => null
			};
		}

		// Sets button text and returns the width.
		private static int SetButtonParams(Button btn, string text, int tab, DialogResult dr)
		{
			btn.Text = text;
			btn.Visible = true;
			btn.DialogResult = dr;
			btn.TabIndex = tab;
			return btn.Size.Width;
		}

		private void DialogBox_Load(object sender, EventArgs e)
		{
			if (!btn1.Visible)
			{
				SetButtons(new[] { "OK" }, new[] { DialogResult.OK });
			}

			_minButtonRowWidth += 2 * FormXMargin; //add margin to the ends

			SetDialogSize();
		}

		// Auto fits the dialog box to fit the text and the buttons.
		private void SetDialogSize()
		{
			int requiredWidth = messageLbl.Location.X + messageLbl.Size.Width + FormXMargin;
			requiredWidth = requiredWidth > _minButtonRowWidth ? requiredWidth : _minButtonRowWidth;

			int requiredHeight = messageLbl.Location.Y + messageLbl.Size.Height - btn2.Location.Y + ClientSize.Height + TextYMargin;

			int minSetWidth = ClientSize.Width;
			int minSetHeight = ClientSize.Height;

			ClientSize = new Size
			{
				Width = requiredWidth > minSetWidth
					? requiredWidth
					: minSetWidth,
				Height = requiredHeight > minSetHeight
					? requiredHeight
					: minSetHeight
			};
		}

		private void ButtonClick(object sender, EventArgs e)
		{
			if (sender == btn1)
			{
				DialogBoxResult = DialogBoxResult.Button1;
			}
			else if (sender == btn2)
			{
				DialogBoxResult = DialogBoxResult.Button2;
			}
			else if (sender == btn3)
			{
				DialogBoxResult = DialogBoxResult.Button3;
			}

			if (((Button) sender).DialogResult == DialogResult.None)
			{
				Close();
			}
		}
	}

	public enum DialogBoxResult
	{
		Button1,
		Button2,
		Button3
	}
}
