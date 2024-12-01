using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

using Emu = BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class CheatEdit : UserControl
	{
		public Emu.IMemoryDomains MemoryDomains { get; set; }

		public CheatEdit()
		{
			InitializeComponent();
			AddressBox.Nullable = false;
			ValueBox.Nullable = false;
		}

		private const string HexInd = "0x";

		private Cheat _cheat;
		private bool _loading;
		private bool _editMode;

		private Action _addCallback;
		private Action _editCallback;

		private void CheatEdit_Load(object sender, EventArgs e)
		{
			Restart();
		}

		public void Restart()
		{
			if (DesignMode)
			{
				return;
			}

			DomainDropDown.Items.Clear();
			DomainDropDown.Items.AddRange(MemoryDomains
				.Where(d => d.Writable)
				.Select(d => (object) d.ToString())
				.ToArray());

			DomainDropDown.SelectedItem = MemoryDomains.HasSystemBus
				? MemoryDomains.SystemBus.ToString()
				: MemoryDomains.MainMemory.ToString();

			SetFormToDefault();
		}

		private void SetFormToCheat()
		{
			_loading = true;
			SetSizeSelected(_cheat.Size);
			PopulateTypeDropdown();
			SetTypeSelected(_cheat.Type);
			SetDomainSelected(_cheat.Domain);

			AddressBox.SetHexProperties(_cheat.Domain.Size);

			ValueBox.ByteSize =
				CompareBox.ByteSize =
				_cheat.Size;

			ValueBox.Type =
				CompareBox.Type =
				_cheat.Type;

			ValueHexIndLabel.Text =
				CompareHexIndLabel.Text =
				_cheat.Type == WatchDisplayType.Hex ? HexInd : "";

			BigEndianCheckBox.Checked = _cheat.BigEndian ?? false;

			NameBox.Text = _cheat.Name;
			AddressBox.Text = _cheat.AddressStr;
			ValueBox.Text = _cheat.ValueStr;
			CompareBox.Text = _cheat.Compare.HasValue ? _cheat.CompareStr : "";

			if (_cheat.ComparisonType.Equals(Cheat.CompareType.None))
			{
				CompareTypeDropDown.SelectedIndex = 0;
			}
			else
			{
				CompareTypeDropDown.SelectedIndex = (int)_cheat.ComparisonType - 1;
			}

			CheckFormState();
			if (!_cheat.Compare.HasValue)
			{
				CompareBox.Text = ""; // Necessary hack until WatchValueBox.ToRawInt() becomes nullable
			}

			_loading = false;
		}

		private void SetFormToDefault()
		{
			_loading = true;
			SetSizeSelected(WatchSize.Byte);
			PopulateTypeDropdown();

			NameBox.Text = "";

			if (MemoryDomains != null)
			{
				AddressBox.SetHexProperties(MemoryDomains.SystemBus.Size);
			}

			ValueBox.ByteSize =
				CompareBox.ByteSize =
				WatchSize.Byte;

			ValueBox.Type =
				CompareBox.Type =
				WatchDisplayType.Hex;

			ValueBox.ResetText();
			CompareBox.ResetText();

			ValueHexIndLabel.Text =
				CompareHexIndLabel.Text =
				HexInd;

			BigEndianCheckBox.Checked = false;

			SetTypeSelected(WatchDisplayType.Hex);

			CheckFormState();
			CompareBox.Text = ""; // TODO: A needed hack until WatchValueBox.ToRawInt() becomes nullable
			_loading = false;
		}

		private void SetSizeSelected(WatchSize size)
		{
			switch (size)
			{
				default:
				case WatchSize.Byte:
					SizeDropDown.SelectedIndex = 0;
					break;
				case WatchSize.Word:
					SizeDropDown.SelectedIndex = 1;
					break;
				case WatchSize.DWord:
					SizeDropDown.SelectedIndex = 2;
					break;
			}
		}

		private void SetTypeSelected(WatchDisplayType type)
		{
			foreach (var item in DisplayTypeDropDown.Items)
			{
				if (item.ToString() == Watch.DisplayTypeToString(type))
				{
					DisplayTypeDropDown.SelectedItem = item;
					return;
				}
			}
		}

		private void SetDomainSelected(Emu.MemoryDomain domain)
		{
			foreach (var item in DomainDropDown.Items)
			{
				if (item.ToString() == domain.Name)
				{
					DomainDropDown.SelectedItem = item;
					return;
				}
			}
		}

		private void PopulateTypeDropdown()
		{
			var wasSelected = DisplayTypeDropDown.SelectedItem?.ToString() ?? string.Empty;
			bool Reselect()
			{
				for (var i = 0; i < DisplayTypeDropDown.Items.Count; i++)
				{
					if (DisplayTypeDropDown.Items[i].ToString() == wasSelected)
					{
						DisplayTypeDropDown.SelectedIndex = i;
						return true;
					}
				}
				return false;
			}
			DisplayTypeDropDown.Items.Clear();
			switch (SizeDropDown.SelectedIndex)
			{
				default:
				case 0:
					foreach (WatchDisplayType t in ByteWatch.ValidTypes)
					{
						DisplayTypeDropDown.Items.Add(Watch.DisplayTypeToString(t));
					}

					break;
				case 1:
					foreach (WatchDisplayType t in WordWatch.ValidTypes)
					{
						DisplayTypeDropDown.Items.Add(Watch.DisplayTypeToString(t));
					}

					break;
				case 2:
					foreach (WatchDisplayType t in DWordWatch.ValidTypes)
					{
						DisplayTypeDropDown.Items.Add(Watch.DisplayTypeToString(t));
					}

					break;
			}
			DisplayTypeDropDown.SelectedIndex = 0;
			if (Reselect()) return;
			wasSelected = Watch.DisplayTypeToString(WatchDisplayType.Hex);
			_ = Reselect();
		}

		private void CheckFormState()
		{
			var valid = !string.IsNullOrWhiteSpace(AddressBox.Text) && !string.IsNullOrWhiteSpace(ValueBox.Text);
			AddButton.Enabled = valid;
			EditButton.Enabled = _editMode && valid;
		}

		private void SizeDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_loading)
			{
				PopulateTypeDropdown();

				ValueBox.ByteSize =
					CompareBox.ByteSize =
					GetCurrentSize();
			}
		}

		private void DomainDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_loading)
			{
				var domain = MemoryDomains[DomainDropDown.SelectedItem.ToString()]!;
				AddressBox.SetHexProperties(domain.Size);
			}
		}

		private WatchSize GetCurrentSize()
		{
			switch (SizeDropDown.SelectedIndex)
			{
				default:
				case 0:
					return WatchSize.Byte;
				case 1:
					return WatchSize.Word;
				case 2:
					return WatchSize.DWord;
			}
		}

		private void DisplayTypeDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			var newDisp = Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString()); //TODO use Tag or Index
			ValueBox.Type = CompareBox.Type = newDisp;
			ValueHexIndLabel.Text = CompareHexIndLabel.Text = newDisp is WatchDisplayType.Hex ? HexInd : string.Empty; //TODO "0b" for binary
			// NOT writing to `_cheat`, the "Override" button handles that
		}

		private void AddButton_Click(object sender, EventArgs e)
		{
			_addCallback?.Invoke();
		}

		private void EditButton_Click(object sender, EventArgs e)
		{
			_editCallback?.Invoke();
		}

		public void SetCheat(Cheat cheat)
		{
			_editMode = true;
			_cheat = cheat;
			if (cheat.IsSeparator)
			{
				SetFormToDefault();
			}
			else
			{
				SetFormToCheat();
			}
		}

		public void ClearForm()
		{
			_cheat = Cheat.Separator;
			_editMode = false;
			SetFormToDefault();
		}

		public Cheat OriginalCheat => _cheat;

		public Cheat GetCheat()
		{
			var domain = MemoryDomains[DomainDropDown.SelectedItem.ToString()]!;
			var address = AddressBox.ToRawInt().Value;
			if (address < domain.Size)
			{
				var watch = Watch.GenerateWatch(
					domain,
					address: address,
					GetCurrentSize(),
					Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString()),
					bigEndian: BigEndianCheckBox.Checked,
					NameBox.Text);

				var comparisonType = CompareTypeDropDown.SelectedItem.ToString() switch
				{
					"" => Cheat.CompareType.None,
					"=" => Cheat.CompareType.Equal,
					">" => Cheat.CompareType.GreaterThan,
					">=" => Cheat.CompareType.GreaterThanOrEqual,
					"<" => Cheat.CompareType.LessThan,
					"<=" => Cheat.CompareType.LessThanOrEqual,
					"!=" => Cheat.CompareType.NotEqual,
					_ => Cheat.CompareType.None
				};

				var compare = CompareBox.ToRawInt();
				return new Cheat(
					watch,
					value: ValueBox.ToRawInt().Value,
					compare: compare,
					enabled: true,
					comparisonType);
			}

			MessageBox.Show($"{address} is not a valid address for the domain {domain.Name}", "Index out of range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return Cheat.Separator;
		}

		public void SetAddEvent(Action addCallback)
		{
			_addCallback = addCallback;
		}

		public void SetEditEvent(Action editCallback)
		{
			_editCallback = editCallback;
		}

		private void CompareBox_TextChanged(object sender, EventArgs e)
		{
			var compareBox = (WatchValueBox)sender;
			PopulateComparisonTypeBox(string.IsNullOrWhiteSpace(compareBox.Text));
		}

		/// <summary>
		/// Populates the comparison type drop down
		/// </summary>
		/// <param name="empty">True if drop down should be left empty</param>
		private void PopulateComparisonTypeBox(bool empty = false)
		{
			// Don't need to do anything in this case
			if (empty && CompareTypeDropDown.Items.Count == 1)
			{
				return;
			}
			
			// Don't need to do anything in this case
			if (!empty && CompareTypeDropDown.Items.Count == 6)
			{
				return;
			}

			CompareTypeDropDown.Items.Clear();

			if (empty)
			{
				CompareTypeDropDown.Items.AddRange(new object[]
				{
					""
				});
			}
			else
			{
				CompareTypeDropDown.Items.AddRange(new object[]
				{
					"=",
					">",
					">=",
					"<",
					"<=",
					"!="
				});
			}

			CompareTypeDropDown.SelectedIndex = 0;
		}
	}
}
