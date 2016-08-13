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
			HexViewControl.QueryIndexForeColor += HexView_QueryIndexForeColor;
			HexViewControl.QueryIndexBgColor += HexView_QueryIndexForeColor;
		}

		private void NewHexEditor_Load(object sender, EventArgs e)
		{
			HexViewControl.ArrayLength = MemoryDomains.MainMemory.Size;
		}

		private void SaveConfigSettings()
		{

		}

		#endregion

		#region IToolForm implementation

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			// TODO
		}

		public void FastUpdate()
		{
			// TODO
		}

		public void Restart()
		{
			// TODO
		}

		public bool AskSaveChanges()
		{
			return true; // TODO
		}

		public bool UpdateBefore {  get { return false; } }

		#endregion

		#region HexView Callbacks

		private void HexView_QueryIndexValue(int index, out long value)
		{
			value = MemoryDomains.MainMemory.PeekByte(index);
		}

		private void HexView_QueryIndexBgColor(int index, ref Color color)
		{
			color = Color.White;
		}

		private void HexView_QueryIndexForeColor(int index, ref Color color)
		{
			color = Color.Black;
		}

		#endregion

		#region Menu Items

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{

		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion
	}
}
