using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	// TODO: don't use textboxes as labels
	public partial class RamPoke : Form, IDialogParent
	{
		private readonly List<Watch> _watchList;
		private readonly CheatCollection _cheats;

		public IDialogController DialogController { get; }

		public Point InitialLocation { get; set; } = new Point(0, 0);

		public RamPoke(IDialogController dialogController, IEnumerable<Watch> watches, CheatCollection cheats)
		{
			_watchList = watches
				.Where(w => !w.IsSeparator) // Weed out separators just in case
				.ToList();
			_cheats = cheats;
			DialogController = dialogController;
			InitializeComponent();
			Icon = Properties.Resources.PokeIcon;
		}

		public IToolForm ParentTool { get; set; }

		private void UnSupportedConfiguration()
		{
			DialogController.ShowMessageBox("RAM Poke does not support mixed types", "Unsupported Options", EMsgBoxIcon.Error);
			Close();
		}

		private void RamPoke_Load(object sender, EventArgs e)
		{
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
				var first = _watchList[0];
				if (_watchList.Skip(1).Any(watch => watch.Size != first.Size || watch.Type != first.Type || watch.BigEndian != first.BigEndian))
				{
					UnSupportedConfiguration();
				}
			}

			AddressBox.SetHexProperties(_watchList[0].Domain.Size);
			AddressBox.Text = (_watchList.Count > 10 ? _watchList.Take(10) : _watchList) // Hack in case an absurd amount of addresses are picked, this can be slow and create too long of a string
				.Select(a => a.AddressString)
				.Distinct()
				.Aggregate((addrStr, nextStr) => $"{addrStr},{nextStr}");

			ValueBox.ByteSize = _watchList[0].Size;
			ValueBox.Type = _watchList[0].Type;

			ValueHexLabel.Text = _watchList[0].Type == WatchDisplayType.Hex ? "0x" : "";
			ValueBox.Text = _watchList[0].ValueString.Replace(" ", "");
			DomainLabel.Text = _watchList[0].Domain.Name;
			SizeLabel.Text = _watchList[0].Size.ToString();
			DisplayTypeLabel.Text = Watch.DisplayTypeToString(_watchList[0].Type);
			BigEndianLabel.Text = _watchList[0].BigEndian ? "Big Endian" : "Little Endian";
			SetTitle();
		}

		private void SetTitle()
		{
			Text = $"RAM Poke - {_watchList[0].Domain.Name}";
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			var success = true;
			foreach (var watch in _watchList)
			{
				var result = watch.Poke(ValueBox.Text);
				if (result)
				{
					var cheat = _cheats.FirstOrDefault(c => c.Address == watch.Address && c.Domain == watch.Domain);
					cheat?.PokeValue(watch.Value);
				}
				else
				{
					success = false;
				}
			}

			ParentTool?.UpdateValues(ToolFormUpdateType.General);

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
