#nullable enable

using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk.ForDebugging
{
	internal sealed class N64RomByteswapToolForm : ToolFormBase
	{
		public const string TOOL_NAME = "Manual N64 ROM Byteswapping Tool";

		protected override string WindowTitleStatic
			=> TOOL_NAME;

		public N64RomByteswapToolForm()
		{
			SzTextBoxEx txtBaseFile = new() { Size = new(224, 23) };
			void ChooseBaseFile()
			{
				var filename = this.ShowFileOpenDialog(initDir: Config!.PathEntries.RomAbsolutePath());
				if (filename is not null) txtBaseFile.Text = filename;
			}
			ComboBox comboFormats = new()
			{
				Items = { "rare little-endian (.n64)", "byte-swapped (.v64)", "native (.z64)" },
				SelectedIndex = 2,
				Size = new(160, 23),
			};
			SzTextBoxEx txtTargetFile = new() { Size = new(224, 23) };
			void ChooseTargetFile()
			{
				var filename = this.ShowFileSaveDialog(
					fileExt: comboFormats.SelectedIndex switch
					{
						0 => "n64",
						1 => "v64",
						2 => "z64",
						_ => null
					},
					initDir: Config!.PathEntries.RomAbsolutePath());
				if (filename is not null) txtTargetFile.Text = filename;
			}
			void DoConvert()
			{
				try
				{
					var rom = File.ReadAllBytes(txtBaseFile.Text);
					_ = comboFormats.SelectedIndex switch
					{
						0 => N64RomByteswapper.ToN64LittleEndian(rom),
						1 => N64RomByteswapper.ToV64ByteSwapped(rom),
						_ => N64RomByteswapper.ToZ64Native(rom)
					};
					File.WriteAllBytes(txtTargetFile.Text, rom);
					this.ModalMessageBox($"wrote {txtTargetFile.Text}\n{SHA1Checksum.ComputePrefixedHex(rom)}");
				}
				catch (Exception e)
				{
					this.ModalMessageBox($"caught {e.GetType().Name}:\n{e}");
				}
			}
			ClientSize = new(320, 200);
			SuspendLayout();
			Controls.Add(new SingleColumnFLP
			{
				Controls =
				{
					new LabelEx { Text = "convert" },
					new SingleRowFLP { Controls = { txtBaseFile, GenControl.Button("(browse)", width: 75, ChooseBaseFile) } },
					new LabelEx { Text = "from <autodetected> (assumes .z64 on fail)" },
					new SingleRowFLP { Controls = { new LabelEx { Text = "to" }, comboFormats } },
					new SingleRowFLP { Controls = { txtTargetFile, GenControl.Button("(browse)", width: 75, ChooseTargetFile) } },
					GenControl.Button("--> convert", width: 75, DoConvert),
				}
			});
			ResumeLayout();
		}
	}
}
