using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class RamPoke : Form
	{
		//TODO: don't use textboxes as labels

		private List<Watch> _watchList = new List<Watch>();

		public Point InitialLocation = new Point(0, 0);

		public RamPoke()
		{
			InitializeComponent();
		}

		public void SetWatch(List<Watch> watches)
		{
			_watchList = watches;
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
			ValueBox.Text = _watchList[0].ValueString.Replace(" ", "");
			DomainLabel.Text = _watchList[0].Domain.Name;
			SizeLabel.Text = _watchList[0].Size.ToString();
			DisplayTypeLabel.Text = Watch.DisplayTypeToString(_watchList[0].Type);
			BigEndianLabel.Text = _watchList[0].BigEndian ? "Big Endian" : "Little Endian";
			SetTitle();

			ValueBox.ByteSize = _watchList[0].Size;
			ValueBox.Type = _watchList[0].Type;
		}

		private void SetTitle()
		{
			Text = "Ram Poke - " + _watchList[0].Domain.Name;
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			bool success = true;
			foreach (var watch in _watchList)
			{
				if (!watch.Poke(ValueBox.Text))
				{
					success = false;
				}
			}

			if (success)
			{
				OutputLabel.Text = "Value successfully written.";
			}
			else
			{
				OutputLabel.Text = "An error occured when writing Value.";
			}
		}
	}
}
