using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace BizHawk.MultiClient.AVOut
{
	// http://msdn.microsoft.com/en-us/library/ms229644%28v=vs.80%29.aspx
	public class NumericTextBox : TextBox
	{
		// Restricts the entry of characters to digits (including hex), the negative sign,
		// the decimal point, and editing keystrokes (backspace).
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			base.OnKeyPress(e);

			NumberFormatInfo numberFormatInfo = System.Globalization.CultureInfo.CurrentCulture.NumberFormat;
			string decimalSeparator = numberFormatInfo.NumberDecimalSeparator;
			string groupSeparator = numberFormatInfo.NumberGroupSeparator;
			string negativeSign = numberFormatInfo.NegativeSign;

			string keyInput = e.KeyChar.ToString();

			if (Char.IsDigit(e.KeyChar))
			{
				// Digits are OK
			}
			else if (keyInput.Equals(decimalSeparator) && AllowDecimal)
			{
				// Decimal separator is OK
			}
			else if (keyInput.Equals(negativeSign) && AllowNegative)
			{
				// Negative is OK
			}
			else if (keyInput.Equals(groupSeparator))
			{
				// group seperator is ok
			}
			else if (e.KeyChar == '\b')
			{
				// Backspace key is OK
			}
			//    else if ((ModifierKeys & (Keys.Control | Keys.Alt)) != 0)
			//    {
			//     // Let the edit control handle control and alt key combinations
			//    }
			else if (AllowSpace && e.KeyChar == ' ')
			{

			}
			else
			{
				// Swallow this invalid key and beep
				e.Handled = true;
				//    MessageBeep();
			}
		}

		public int IntValue { get { return Int32.Parse(this.Text); } }
		public decimal DecimalValue { get { return Decimal.Parse(this.Text); } }
		public bool AllowSpace { set; get; }
		public bool AllowDecimal { set; get; }
		public bool AllowNegative { set; get; }
	}
}
