using System.Windows.Forms;

namespace BizHawk.MultiClient.tools
{
	enum BoxType { ALL, SIGNED, UNSIGNED, HEX };
	class LuaTextBox : TextBox
	{
		private BoxType boxType = BoxType.ALL;

		public void SetType(BoxType type)
		{
			boxType = type;
			if (type != BoxType.ALL)
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

			switch (boxType)
			{
				case BoxType.UNSIGNED:
					if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
						e.Handled = true;
					break;
				case BoxType.SIGNED:
					if (!InputValidate.IsValidSignedNumber(e.KeyChar))
						e.Handled = true;
					break;
				case BoxType.HEX:
					if (!InputValidate.IsValidHexNumber(e.KeyChar))
						e.Handled = true;
					break;
			}
		}
	}
}
