using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BizHawk.MultiClient
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

		private NewCheat _cheat;
		private const string HexInd = "0x";
		private bool _loading = false;
		private bool _editmode = false;

		private Action _addCallback = null;
		private Action _editCallback = null;

		private void CheatEdit_Load(object sender, EventArgs e)
		{
			if (Global.Emulator != null)
			{
				ToolHelpers.PopulateMemoryDomainDropdown(ref DomainDropDown, Global.Emulator.MainMemory);
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
				AddressBox.SetHexProperties(Global.Emulator.MainMemory.Size);
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
					DisplayTypeDropDown.Items.AddRange(ByteWatch.ValidTypes.ConvertAll<string>(e => Watch.DisplayTypeToString(e)).ToArray());
					break;
				case 1:
					DisplayTypeDropDown.Items.AddRange(WordWatch.ValidTypes.ConvertAll<string>(e => Watch.DisplayTypeToString(e)).ToArray());
					break;
				case 2:
					DisplayTypeDropDown.Items.AddRange(DWordWatch.ValidTypes.ConvertAll<string>(e => Watch.DisplayTypeToString(e)).ToArray());
					break;
			}
			DisplayTypeDropDown.SelectedItem = DisplayTypeDropDown.Items[0];
		}

		private void CheckFormState()
		{
			bool valid = (!String.IsNullOrWhiteSpace(AddressBox.Text) && !String.IsNullOrWhiteSpace(ValueBox.Text));
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
				var domain = ToolHelpers.DomainByName(DomainDropDown.SelectedItem.ToString());
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

		public void SetCheat(NewCheat cheat)
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
			SetFormToDefault();
			_cheat = NewCheat.Separator;
		}

		public NewCheat Cheat
		{
			get
			{
				Watch w = Watch.GenerateWatch(
					ToolHelpers.DomainByName(DomainDropDown.SelectedItem.ToString()),
					AddressBox.ToRawInt(),
					GetCurrentSize(),
					Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString()),
					NameBox.Text,
					BigEndianCheckBox.Checked);

				return new NewCheat(w, CompareBox.ToRawInt(), enabled: true);
			}
		}

		public void SetAddEvent(Action AddCallback)
		{
			_addCallback = AddCallback;
		}

		public void SetEditEvent(Action EditCallback)
		{
			_editCallback = EditCallback;
		}

		#endregion
	}
}
