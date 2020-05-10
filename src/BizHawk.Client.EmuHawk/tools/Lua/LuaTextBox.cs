using System.Globalization;
using System.Windows.Forms;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.EmuHawk
{
	internal enum BoxType
	{
		All, Signed, Unsigned, Hex
	}

	internal class LuaTextBox : TextBox
	{
		private BoxType _boxType = BoxType.All;

		public void SetType(BoxType type)
		{
			_boxType = type;
			CharacterCasing = type != BoxType.All
				? CharacterCasing.Upper
				: CharacterCasing.Normal;
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			SpecificValueBox_KeyPress(this, e);
			base.OnKeyPress(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Up)
			{
				switch (_boxType)
				{
					default:
					case BoxType.All:
						base.OnKeyDown(e);
						break;
					case BoxType.Hex:
					case BoxType.Signed:
					case BoxType.Unsigned:
						Increment();
						break;
				}
			}
			else if (e.KeyCode == Keys.Down)
			{
				switch (_boxType)
				{
					default:
					case BoxType.All:
						base.OnKeyDown(e);
						break;
					case BoxType.Hex:
					case BoxType.Signed:
					case BoxType.Unsigned:
						Decrement();
						break;
				}
			}
			else if (e.KeyData == (Keys.Control | Keys.A))
			{
				SelectAll();
			}
			else
			{
				base.OnKeyDown(e);
			}
		}

		private void Increment()
		{
			string text = string.IsNullOrWhiteSpace(Text) ? "0" : Text;
			switch (_boxType)
			{
				case BoxType.Hex:
					var hVal = uint.Parse(text, NumberStyles.HexNumber);
					if (hVal < uint.MaxValue)
					{
						hVal++;
						Text = hVal.ToString("X");
					}
					else
					{
						Text = "0";
					}

					break;
				case BoxType.Signed:
					var sval = int.Parse(text);
					if (sval < int.MaxValue)
					{
						sval++;
						Text = sval.ToString();
					}
					else
					{
						Text = "0";
					}

					break;
				case BoxType.Unsigned:
					var uval = uint.Parse(text);
					if (uval < uint.MaxValue)
					{
						uval++;
						Text = uval.ToString();
					}
					else
					{
						Text = "0";
					}

					break;
			}
		}

		private void Decrement()
		{
			string text = string.IsNullOrWhiteSpace(Text) ? "0" : Text;
			switch (_boxType)
			{
				case BoxType.Hex:
					var hVal = uint.Parse(text, NumberStyles.HexNumber);
					if (hVal > 0)
					{
						hVal--;
						Text = hVal.ToString("X");
					}

					break;
				case BoxType.Signed:
					var sval = int.Parse(text);
					sval--;
					Text = sval.ToString();
					break;
				case BoxType.Unsigned:
					var uval = uint.Parse(text);
					if (uval > 0)
					{
						uval--;
						Text = uval.ToString();
					}

					break;
			}
		}

		private void SpecificValueBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b')
			{
				return;
			}

			switch (_boxType)
			{
				case BoxType.Unsigned:
					if (!e.KeyChar.IsUnsigned())
					{
						e.Handled = true;
					}

					break;
				case BoxType.Signed:
					if (!e.KeyChar.IsSigned())
					{
						e.Handled = true;
					}

					break;
				case BoxType.Hex:
					if (!e.KeyChar.IsHex())
					{
						e.Handled = true;
					}

					break;
			}
		}
	}
}
