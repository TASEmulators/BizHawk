using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	enum BoxType { All, Signed, Unsigned, Hex };
	class LuaTextBox : TextBox
	{
		private BoxType _boxType = BoxType.All;

		public void SetType(BoxType type)
		{
			_boxType = type;
			if (type != BoxType.All)
			{
				CharacterCasing = CharacterCasing.Upper;
			}
			else
			{
				CharacterCasing = CharacterCasing.Normal;
			}
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			SpecificValueBox_KeyPress(this, e);
			base.OnKeyPress(e);
		}

		private void SpecificValueBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b') return;

			switch (_boxType)
			{
				case BoxType.Unsigned:
					if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
						e.Handled = true;
					break;
				case BoxType.Signed:
					if (!InputValidate.IsValidSignedNumber(e.KeyChar))
						e.Handled = true;
					break;
				case BoxType.Hex:
					if (!InputValidate.IsValidHexNumber(e.KeyChar))
						e.Handled = true;
					break;
			}
		}
	}
}
