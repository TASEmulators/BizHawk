using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class NewRamPoke : Form
	{
		//TODO: don't use textboxes as labels

		private List<Watch> _watchList = new List<Watch>();

		public Point InitialLocation = new Point(0, 0);

		public NewRamPoke()
		{
			InitializeComponent();
		}

		public void SetWatch(List<Watch> watches)
		{
			if (watches != null)
			{
				_watchList = watches;
			}

			SetTitle();
		}

		private void UnSupportedConfiguration()
		{
			MessageBox.Show("Ram Poke does not support mixed types", "Unsupported Options", MessageBoxButtons.OK, MessageBoxIcon.Error);
			Close();
		}

		private void RamPoke_Load(object sender, EventArgs e)
		{
			_watchList = _watchList.Where(x => !x.IsSeparator).ToList(); //Weed out separators just in case

			if (_watchList.Count == 0)
			{
				ValueBox.Enabled = false;
				return;
			}

			if (InitialLocation.X > 0 || InitialLocation.Y > 0)
			{
				Location = InitialLocation;
			}

			if (_watchList.Count > 1)
			{
				bool hasMixedSizes = _watchList.Select(x => x.Size).Distinct().Count() > 1;
				bool hasMixedTypes = _watchList.Select(x => x.Type).Distinct().Count() > 1;
				bool hasMixedEndian = _watchList.Select(x => x.BigEndian).Distinct().Count() > 1;

				if (hasMixedSizes || hasMixedTypes || hasMixedEndian)
				{
					UnSupportedConfiguration();
				}
			}

			AddressBox.Text = _watchList.Select(a => a.AddressString).Distinct().Aggregate((addrStr, nextStr) => addrStr + ("," + nextStr));
			ValueHexLabel.Text = _watchList[0].Type == Watch.DisplayType.Hex ? "0x" : String.Empty;
			ValueBox.Text = _watchList[0].ValueString;
			SizeLabel.Text = _watchList[0].Size.ToString();
			DisplayTypeLabel.Text = Watch.DisplayTypeToString(_watchList[0].Type);
			BigEndianLabel.Text = _watchList[0].BigEndian ? "Big Endian" : "Little Endian";
		}

		private void SetValueBoxProperties()
		{
			switch(_watchList[0].Type)
			{
				default:
					ValueBox.MaxLength = 8;
					break;
				case Watch.DisplayType.Binary:
					switch (_watchList[0].Size)
					{
						default:
						case Watch.WatchSize.Byte:
							ValueBox.MaxLength = 8;
							break;
						case Watch.WatchSize.Word:
							ValueBox.MaxLength = 16;
							break;
					}
					break;
				case Watch.DisplayType.Hex:
					switch (_watchList[0].Size)
					{
						default:
						case Watch.WatchSize.Byte:
							ValueBox.MaxLength = 2;
							break;
						case Watch.WatchSize.Word:
							ValueBox.MaxLength = 4;
							break;
						case Watch.WatchSize.DWord:
							ValueBox.MaxLength = 8;
							break;
					}
					break;
				case Watch.DisplayType.Signed:
					switch (_watchList[0].Size)
					{
						default:
						case Watch.WatchSize.Byte:
							ValueBox.MaxLength = 4;
							break;
						case Watch.WatchSize.Word:
							ValueBox.MaxLength = 6;
							break;
						case Watch.WatchSize.DWord:
							ValueBox.MaxLength = 11;
							break;
					}
					break;
				case Watch.DisplayType.Unsigned:
					switch (_watchList[0].Size)
					{
						default:
						case Watch.WatchSize.Byte:
							ValueBox.MaxLength = 3;
							break;
						case Watch.WatchSize.Word:
							ValueBox.MaxLength = 5;
							break;
						case Watch.WatchSize.DWord:
							ValueBox.MaxLength = 10;
							break;
					}
					break;
				case Watch.DisplayType.Float:
				case Watch.DisplayType.FixedPoint_12_4:
				case Watch.DisplayType.FixedPoint_20_12:
					ValueBox.MaxLength = 32;
					break;
			}
		}

		private void SetTitle()
		{
			Text = "Ram Poke - " + _watchList[0].Domain;
		}

		#region Events

		private void Cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			//TODO

			OutputLabel.Text = ValueBox.Text + " written to " + AddressBox.Text;
		}

		private void ValueBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
			{
				return;
			}
			
			switch(_watchList[0].Type)
			{
				case Watch.DisplayType.Signed:
					if (!InputValidate.IsValidSignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch.DisplayType.Unsigned:
					if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch.DisplayType.Hex:
					if (!InputValidate.IsValidHexNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch.DisplayType.Binary:
					if (!InputValidate.IsValidBinaryNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch.DisplayType.FixedPoint_12_4:
				case Watch.DisplayType.FixedPoint_20_12:
					if (!InputValidate.IsValidFixedPointNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch.DisplayType.Float:
					if (!InputValidate.IsValidDecimalNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
					
			}
		}

		#endregion
	}
}
