using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RamPoke : Form
	{
		// TODO: don't use textboxes as labels
		private List<Watch> _watchList = new List<Watch>();

		public Point InitialLocation = new Point(0, 0);

		public RamPoke()
		{
			InitializeComponent();
		}

		public IToolForm ParentTool { get; set; }

		public void SetWatch(IEnumerable<Watch> watches)
		{
			_watchList = watches.ToList();
		}

		private void UnSupportedConfiguration()
		{
			MessageBox.Show("RAM Poke does not support mixed types", "Unsupported Options", MessageBoxButtons.OK, MessageBoxIcon.Error);
			Close();
		}

		private void RamPoke_Load(object sender, EventArgs e)
		{
			_watchList = _watchList.Where(x => !x.IsSeparator).ToList(); // Weed out separators just in case

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

			AddressBox.SetHexProperties(_watchList[0].Domain.Size);
			if (_watchList.Count < 10) // Hack in case an asburd amount of addresses is picked, this can get slow and create a too long string
			{
				AddressBox.Text = _watchList
					.Select(a => a.AddressString)
					.Distinct()
					.Aggregate((addrStr, nextStr) => addrStr + ("," + nextStr));
			}
			else
			{
				AddressBox.Text = _watchList
					.Take(10)
					.Select(a => a.AddressString)
					.Distinct()
					.Aggregate((addrStr, nextStr) => addrStr + ("," + nextStr));
			}

			ValueBox.ByteSize = _watchList[0].Size;
			ValueBox.Type = _watchList[0].Type;

			ValueHexLabel.Text = _watchList[0].Type == DisplayType.Hex ? "0x" : string.Empty;
			ValueBox.Text = _watchList[0].ValueString.Replace(" ", string.Empty);
			DomainLabel.Text = _watchList[0].Domain.Name;
			SizeLabel.Text = _watchList[0].Size.ToString();
			DisplayTypeLabel.Text = Watch.DisplayTypeToString(_watchList[0].Type);
			BigEndianLabel.Text = _watchList[0].BigEndian ? "Big Endian" : "Little Endian";
			SetTitle();
		}

		private void SetTitle()
		{
			Text = "RAM Poke - " + _watchList[0].Domain.Name;
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			var success = _watchList.All(watch => watch.Poke(ValueBox.Text));

			if (ParentTool != null)
			{
				ParentTool.UpdateValues();
			}

			if (success)
			{
				DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				OutputLabel.Text = "An error occured when writing Value.";
			}
		}
	}
}
