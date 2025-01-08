using System.Windows.Forms;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class AddBreakpointDialog : Form
	{
		public AddBreakpointDialog(BreakpointOperation op)
		{
			InitializeComponent();
			Operation = op;
			AddressMaskBox.SetHexProperties(0xFFFFFFFF);
			AddressMask = 0xFFFFFFFF;
		}

		public AddBreakpointDialog(BreakpointOperation op, uint address, uint mask, MemoryCallbackType type)
			: this(op)
		{
			AddressMaskBox.SetHexProperties(0xFFFFFFFF);
			Address = address;
			AddressMask = mask;
			BreakType = type;
		}

		private BreakpointOperation _operation;

		private BreakpointOperation Operation
		{
			get => _operation;

			set
			{
				switch (value)
				{
					case BreakpointOperation.Add:
						Text = "Add Breakpoint";
						break;
					case BreakpointOperation.Duplicate:
						Text = "Duplicate Breakpoint";
						break;
					case BreakpointOperation.Edit:
						Text = "Edit Breakpoint";
						break;
				}

				_operation = value;
			}
		}

		public void DisableExecuteOption()
		{
			if (ExecuteRadio.Checked)
			{
				ReadRadio.Checked = true;
			}

			ExecuteRadio.Enabled = false;
		}

		public MemoryCallbackType BreakType
		{
			get
			{
				if (ReadRadio.Checked)
				{
					return MemoryCallbackType.Read;
				}

				if (WriteRadio.Checked)
				{
					return MemoryCallbackType.Write;
				}

				if (ExecuteRadio.Checked)
				{
					return MemoryCallbackType.Execute;
				}

				return MemoryCallbackType.Read;
			}

			set
			{
				ReadRadio.Checked = WriteRadio.Checked = ExecuteRadio.Checked = false;
				switch (value)
				{
					case MemoryCallbackType.Read:
						ReadRadio.Checked = true;
						break;
					case MemoryCallbackType.Write:
						WriteRadio.Checked = true;
						break;
					case MemoryCallbackType.Execute:
						ExecuteRadio.Checked = true;
						break;
				}
			}
		}

		public uint Address
		{
			get => (uint)AddressBox.ToRawInt().Value & AddressMask;
			set => AddressBox.SetFromLong(value & AddressMask);
		}

		public uint AddressMask
		{
			get => (uint)AddressMaskBox.ToRawInt().Value;
			set => AddressMaskBox.SetFromLong(value);
		}

		public long MaxAddressSize
		{
			get => AddressBox.GetMax();

			set
			{
				AddressBox.SetHexProperties(value);
				AddressMaskBox.SetHexProperties(value);
			}
		}

		private void AddButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void AddBreakpointDialog_Load(object sender, EventArgs e)
		{
		}

		public enum BreakpointOperation
		{
			Add, Edit, Duplicate
		}
	}
}
