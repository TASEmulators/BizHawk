using System.Globalization;
using System.Windows.Forms;

namespace BizHawk
{
	public class HexTextBox : TextBox
	{
		public HexTextBox()
		{
			CharacterCasing = CharacterCasing.Upper;
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
			{
				return;
			}
			else if (!InputValidate.IsValidHexNumber(e.KeyChar))
			{
				e.Handled = true;
			}
		}

		public int ToInt()
		{
			return int.Parse(Text, NumberStyles.HexNumber);
		}
	}
}
