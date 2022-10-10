#nullable enable

using System;
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
				using OpenFileDialog ofd = new() { InitialDirectory = Config!.PathEntries.RomAbsolutePath() };
				this.ShowDialogAsChild(ofd);
				txtBaseFile.Text = ofd.FileName;
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
				using SaveFileDialog sfd = new()
				{
					DefaultExt = comboFormats.SelectedIndex switch
					{
						0 => "n64",
						1 => "v64",
						2 => "z64",
						_ => string.Empty
					},
					InitialDirectory = Config!.PathEntries.RomAbsolutePath(),
				};
				this.ShowDialogAsChild(sfd);
				txtTargetFile.Text = sfd.FileName;
			}
			void DoConvert()
			{
				try
				{
					var rom = File.ReadAllBytes(txtBaseFile.Text);
					switch (comboFormats.SelectedIndex) // can't have Action<Span<byte>> (System.Buffers.SpanAction isn't suitable) or I'd be able to have a tiny switch expr >:( --yoshi
					{
						case 0:
							N64RomByteswapper.ToN64LittleEndian(rom);
							break;
						case 1:
							N64RomByteswapper.ToV64ByteSwapped(rom);
							break;
						case 2:
						default:
							N64RomByteswapper.ToZ64Native(rom);
							break;
					}
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
