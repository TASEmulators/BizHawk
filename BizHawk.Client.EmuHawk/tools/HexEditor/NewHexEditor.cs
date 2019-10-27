using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class NewHexEditor : Form, IToolFormAutoConfig
	{
		#region Initialize and Dependencies

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		[RequiredService]
		private IEmulator Emulator { get; set; }

		public NewHexEditor()
		{
			InitializeComponent();

			Closing += (o, e) => SaveConfigSettings();

			HexViewControl.QueryIndexValue += HexView_QueryIndexValue;
		}

		private void NewHexEditor_Load(object sender, EventArgs e)
		{
			HexViewControl.ArrayLength = MemoryDomains.MainMemory.Size;
		}

		[ConfigPersist]
		private int DataSize { get; set; } = 1;

		[ConfigPersist]
		private bool BigEndian { get; set; }

		private void SetDataSize(int value)
		{
			HexViewControl.DataSize = DataSize = value;
			HexViewControl.Refresh();
		}

		private void SaveConfigSettings()
		{

		}

		#endregion

		#region IToolForm implementation

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			HexViewControl.Refresh();
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		public void Restart()
		{
			// TODO
		}

		public bool AskSaveChanges()
		{
			return true; // TODO
		}

		public bool UpdateBefore => false;

		#endregion

		#region HexView Callbacks

		private void HexView_QueryIndexValue(long index, int dataSize, out long value)
		{
			switch (dataSize)
			{
				default:
					value = MemoryDomains.MainMemory.PeekByte(index);
					break;
				case 2:
					value = MemoryDomains.MainMemory.PeekUshort(index, BigEndian);
					break;
				case 4:
					value = MemoryDomains.MainMemory.PeekUint(index, BigEndian);
					break;
			}
			
		}

		#endregion

		#region File

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{

		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Options

		private void DataSizeMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			OneByteMenuItem.Checked = DataSize == 1;
			TwoByteMenuItem.Checked = DataSize == 2;
			FourByteMenuItem.Checked = DataSize == 4;
		}

		private void OneByteMenuItem_Click(object sender, EventArgs e)
		{
			SetDataSize(1);
		}

		private void TwoByteMenuItem_Click(object sender, EventArgs e)
		{
			SetDataSize(2);
		}

		private void FourByteMenuItem_Click(object sender, EventArgs e)
		{
			SetDataSize(4);
		}

		#endregion
	}
}
