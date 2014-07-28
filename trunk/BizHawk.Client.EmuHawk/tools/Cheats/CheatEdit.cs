using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class CheatEdit : UserControl
	{
		public CheatEdit()
		{
			InitializeComponent();
			AddressBox.Nullable = false;
			ValueBox.Nullable = false;
		}

		#region Privates

		private const string HexInd = "0x";

		private Cheat _cheat;
		private bool _loading;
		private bool _editmode;

		private Action _addCallback;
		private Action _editCallback;

		private void CheatEdit_Load(object sender, EventArgs e)
		{
			if (Global.Emulator != null) // the designer needs this check
			{
				DomainDropDown.Items.Clear();
				DomainDropDown.Items.AddRange(Global.Emulator.MemoryDomains
					.Select(d => d.ToString())
					.ToArray());
				DomainDropDown.SelectedItem = Global.Emulator.MemoryDomains.MainMemory.ToString();
			}

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

			NameBox.Text = _cheat.Name;
			AddressBox.Text = _cheat.AddressStr;
			ValueBox.Text = _cheat.ValueStr;
			CompareBox.Text = _cheat.Compare.HasValue ? _cheat.CompareStr : String.Empty;

			ValueBox.ByteSize =
				CompareBox.ByteSize =
				_cheat.Size;

			ValueBox.Type =
				CompareBox.Type =
				_cheat.Type;

			ValueHexIndLabel.Text =
				CompareHexIndLabel.Text =
				_cheat.Type == Watch.DisplayType.Hex ? HexInd : String.Empty;

			BigEndianCheckBox.Checked = _cheat.BigEndian.Value;

			CheckFormState();
			if (!_cheat.Compare.HasValue)
			{
				CompareBox.Text = String.Empty; // Necessary hack until WatchValueBox.ToRawInt() becomes nullable
			}

			_loading = false;
		}

		private void SetFormToDefault()
		{
			_loading = true;
			SetSizeSelected(Watch.WatchSize.Byte);
			PopulateTypeDropdown();

			NameBox.Text = String.Empty;

			if (Global.Emulator != null)
			{
				AddressBox.SetHexProperties(Global.Emulator.MemoryDomains.MainMemory.Size);
			}

			ValueBox.ByteSize = 
				CompareBox.ByteSize =
				Watch.WatchSize.Byte;

			ValueBox.Type = 
				CompareBox.Type =
				Watch.DisplayType.Hex;

			ValueBox.ResetText();
			CompareBox.ResetText();

			ValueHexIndLabel.Text =
				CompareHexIndLabel.Text =
				HexInd;

			BigEndianCheckBox.Checked = false;

			SetTypeSelected(Watch.DisplayType.Hex);

			CheckFormState();
			CompareBox.Text = String.Empty; // TODO: A needed hack until WatchValueBox.ToRawInt() becomes nullable
			_loading = false;
		}

		private void SetSizeSelected(Watch.WatchSize size)
		{
			switch (size)
			{
				default:
				case Watch.WatchSize.Byte:
					SizeDropDown.SelectedIndex = 0;
					break;
				case Watch.WatchSize.Word:
					SizeDropDown.SelectedIndex = 1;
					break;
				case Watch.WatchSize.DWord:
					SizeDropDown.SelectedIndex = 2;
					break;
			}
		}

		private void SetTypeSelected(Watch.DisplayType type)
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

		private void SetDomainSelected(MemoryDomain domain)
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
			DisplayTypeDropDown.Items.Clear();
			switch (SizeDropDown.SelectedIndex)
			{
				default:
				case 0:
					DisplayTypeDropDown.Items.AddRange(ByteWatch.ValidTypes.ConvertAll(e => Watch.DisplayTypeToString(e)).ToArray());
					break;
				case 1:
					DisplayTypeDropDown.Items.AddRange(WordWatch.ValidTypes.ConvertAll(e => Watch.DisplayTypeToString(e)).ToArray());
					break;
				case 2:
					DisplayTypeDropDown.Items.AddRange(DWordWatch.ValidTypes.ConvertAll(e => Watch.DisplayTypeToString(e)).ToArray());
					break;
			}

			DisplayTypeDropDown.SelectedItem = DisplayTypeDropDown.Items[0];
		}

		private void CheckFormState()
		{
			var valid = !String.IsNullOrWhiteSpace(AddressBox.Text) && !String.IsNullOrWhiteSpace(ValueBox.Text);
			AddButton.Enabled = valid;
			EditButton.Enabled = _editmode && valid;
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
				var domain = Global.Emulator.MemoryDomains[DomainDropDown.SelectedItem.ToString()];
				AddressBox.SetHexProperties(domain.Size);
			}
		}

		private Watch.WatchSize GetCurrentSize()
		{
			switch (SizeDropDown.SelectedIndex)
			{
				default:
				case 0:
					return Watch.WatchSize.Byte;
				case 1:
					return Watch.WatchSize.Word;
				case 2:
					return Watch.WatchSize.DWord;
			}
		}

		private void DisplayTypeDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			ValueBox.Type =
				CompareBox.Type =
				Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString());
		}

		private void AddButton_Click(object sender, EventArgs e)
		{
			if (_addCallback != null)
			{
				_addCallback();
			}
		}

		private void EditButton_Click(object sender, EventArgs e)
		{
			if (_editCallback != null)
			{
				_editCallback();
			}
		}

		#endregion

		#region API

		public void SetCheat(Cheat cheat)
		{
			_editmode = true;
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
			_editmode = false;
			SetFormToDefault();
		}

		public Cheat OriginalCheat
		{
			get { return _cheat; }
		}

		public Cheat Cheat
		{
			get
			{
				var domain = Global.Emulator.MemoryDomains[DomainDropDown.SelectedItem.ToString()];
				var address = AddressBox.ToRawInt().Value;
				//var address = AddressBox.ToRawInt() ?? 0;
				if (address < domain.Size)
				{
					var watch = Watch.GenerateWatch(
						Global.Emulator.MemoryDomains[DomainDropDown.SelectedItem.ToString()],
						AddressBox.ToRawInt().Value,
						GetCurrentSize(),
						Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString()),
						NameBox.Text,
						BigEndianCheckBox.Checked);

					return new Cheat(
						watch,
						ValueBox.ToRawInt().Value,
						 CompareBox.ToRawInt()
					);
				}
				else
				{
					MessageBox.Show(address.ToString() + " is not a valid address for the domain " + domain.Name, "Index out of range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return Cheat.Separator;
				}
			}
		}

		public void SetAddEvent(Action addCallback)
		{
			_addCallback = addCallback;
		}

		public void SetEditEvent(Action editCallback)
		{
			_editCallback = editCallback;
		}

		#endregion
	}
}
