using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESNameTableViewer : ToolFormBase, IToolFormAutoConfig
	{
		// TODO:
		// Show Scroll Lines + UI Toggle

		public static Icon ToolIcon
			=> Properties.Resources.NesControllerIcon;

		[RequiredService]
		public INESPPUViewable _nesCore { get; set; }

		private INESPPUViewable _ppu
			=> _nesCore!;

		[RequiredService]
		public IEmulator _core { get; set; }

		private IEmulator _emu
			=> _core!;

		[ConfigPersist]
		private int RefreshRateConfig
		{
			get => RefreshRate.Value;
			set => RefreshRate.Value = value;
		}

		private int _scanline;

		protected override string WindowTitleStatic => "Nametable Viewer";

		public NESNameTableViewer()
		{
			InitializeComponent();
			Icon = ToolIcon;
		}

		private void NESNameTableViewer_Load(object sender, EventArgs e)
		{
			Generate(true);
		}

		public override void Restart()
		{
			Generate(true);
		}

		protected override void UpdateBefore()
		{
			_ppu.InstallCallback1(() => Generate(), _scanline);
		}

		protected override void GeneralUpdate() => UpdateBefore();

		private unsafe void DrawTile(int* dst, int pitch, byte* pal, byte* tile, int* finalPal)
		{
			dst += 7;
			int verticalInc = pitch + 8;
			for (int j = 0; j < 8; j++)
			{
				int lo = tile[0];
				int hi = tile[8] << 1;
				for (int i = 0; i < 8; i++)
				{
					*dst-- = finalPal[pal[lo & 1 | hi & 2]];
					lo >>= 1;
					hi >>= 1;
				}
				dst += verticalInc;
				tile++;
			}
		}

		private unsafe void GenerateExAttr(int* dst, int pitch, byte[] palRam, byte[] ppuMem, byte[] exRam)
		{
			byte[] chr = _ppu.GetExTiles();
			int chrMask = chr.Length - 1;

			fixed (byte* chrPtr = chr, palPtr = palRam, ppuPtr = ppuMem, exPtr = exRam)
			fixed (int* finalPal = _ppu.GetPalette())
			{
				DrawExNT(dst, pitch, palPtr, ppuPtr + 0x2000, exPtr, chrPtr, chrMask, finalPal);
				DrawExNT(dst + 256, pitch, palPtr, ppuPtr + 0x2400, exPtr, chrPtr, chrMask, finalPal);
				dst += pitch * 240;
				DrawExNT(dst, pitch, palPtr, ppuPtr + 0x2800, exPtr, chrPtr, chrMask, finalPal);
				DrawExNT(dst + 256, pitch, palPtr, ppuPtr + 0x2c00, exPtr, chrPtr, chrMask, finalPal);
			}
		}

		private unsafe void GenerateAttr(int* dst, int pitch, byte[] palRam, byte[] ppuMem)
		{
			fixed (byte* palPtr = palRam, ppuPtr = ppuMem)
			fixed (int* finalPal = _ppu.GetPalette())
			{
				byte* chrPtr = ppuPtr + (_ppu.BGBaseHigh ? 0x1000 : 0);
				DrawNT(dst, pitch, palPtr, ppuPtr + 0x2000, chrPtr, finalPal);
				DrawNT(dst + 256, pitch, palPtr, ppuPtr + 0x2400, chrPtr, finalPal);
				dst += pitch * 240;
				DrawNT(dst, pitch, palPtr, ppuPtr + 0x2800, chrPtr, finalPal);
				DrawNT(dst + 256, pitch, palPtr, ppuPtr + 0x2c00, chrPtr, finalPal);
			}
		}

		private unsafe void DrawNT(int* dst, int pitch, byte* palRam, byte* nt, byte* chr, int* finalPal)
		{
			byte* at = nt + 0x3c0;

			for (int ty = 0; ty < 30; ty++)
			{
				for (int tx = 0; tx < 32; tx++)
				{
					byte t = *nt++;
					byte a = at[ty >> 2 << 3 | tx >> 2];
					a >>= tx & 2;
					a >>= (ty & 2) << 1;
					int palNum = a & 3;

					int tileAddr = t << 4;
					DrawTile(dst, pitch, palRam + palNum * 4, chr + tileAddr, finalPal);
					dst += 8;
				}
				dst -= 256;
				dst += pitch * 8;
			}
		}

		private unsafe void DrawExNT(int* dst, int pitch, byte* palRam, byte* nt, byte* exNt, byte* chr, int chrMask, int* finalPal)
		{
			for (int ty = 0; ty < 30; ty++)
			{
				for (int tx = 0; tx < 32; tx++)
				{
					byte t = *nt++;
					byte ex = *exNt++;

					int tileNum = t | (ex & 0x3f) << 8;
					int palNum = ex >> 6;

					int tileAddr = tileNum << 4 & chrMask;
					DrawTile(dst, pitch, palRam + palNum * 4, chr + tileAddr, finalPal);
					dst += 8;
				}
				dst -= 256;
				dst += pitch * 8;
			}
		}

		private unsafe void Generate(bool now = false)
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			if (!now && _emu.Frame % RefreshRate.Value != 0)
			{
				return;
			}

			var bmpData = NameTableView.Nametables.LockBits(
				new Rectangle(0, 0, 512, 480),
				ImageLockMode.WriteOnly,
				PixelFormat.Format32bppArgb);

			var dPtr = (int*)bmpData.Scan0.ToPointer();
			var pitch = bmpData.Stride / 4;

			// Buffer all the data from the ppu, because it will be read multiple times and that is slow
			var ppuBuffer = _ppu.GetPPUBus();

			var palRam = _ppu.GetPalRam();

			if (_ppu.ExActive)
			{
				byte[] exRam = _ppu.GetExRam();
				GenerateExAttr(dPtr, pitch, palRam, ppuBuffer, exRam);
			}
			else
			{
				GenerateAttr(dPtr, pitch, palRam, ppuBuffer);
			}

			NameTableView.Nametables.UnlockBits(bmpData);
			NameTableView.Refresh();
		}

		private void ScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			NameTableView
				.ToBitMap()
				.SaveAsFile(Game, "Nametables", VSystemID.Raw.NES, Config.PathEntries, this);
		}

		private void ScreenshotToClipboardMenuItem_Click(object sender, EventArgs e)
		{
			NameTableView.ToBitMap().ToClipBoard();
		}

		private void RefreshImageContextMenuItem_Click(object sender, EventArgs e)
		{
			GeneralUpdate();
			NameTableView.Refresh();
		}

		private void NesNameTableViewer_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.C:
					if (e.Modifiers == Keys.Control)
					{
						NameTableView.ToBitMap().ToClipBoard();
					}

					break;
			}
		}

		private void NESNameTableViewer_FormClosed(object sender, FormClosedEventArgs e)
			=> _ppu.RemoveCallback1();

		private void ScanlineTextBox_TextChanged(object sender, EventArgs e)
		{
			if (int.TryParse(txtScanline.Text, out _scanline))
			{
				_ppu.InstallCallback1(() => Generate(), _scanline);
			}
		}

		private void NametableRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (rbNametableNW.Checked)
			{
				NameTableView.Which = NameTableViewer.WhichNametable.NT_2000;
			}

			if (rbNametableNE.Checked)
			{
				NameTableView.Which = NameTableViewer.WhichNametable.NT_2400;
			}

			if (rbNametableSW.Checked)
			{
				NameTableView.Which = NameTableViewer.WhichNametable.NT_2800;
			}

			if (rbNametableSE.Checked)
			{
				NameTableView.Which = NameTableViewer.WhichNametable.NT_2C00;
			}

			if (rbNametableAll.Checked)
			{
				NameTableView.Which = NameTableViewer.WhichNametable.NT_ALL;
			}
		}

		private void NameTableView_MouseMove(object sender, MouseEventArgs e)
		{
			int tileX, tileY, nameTable;
			if (NameTableView.Which == NameTableViewer.WhichNametable.NT_ALL)
			{
				tileX = e.X / 8;
				tileY = e.Y / 8;
				nameTable = (tileX / 32) + ((tileY / 30) * 2);
			}
			else
			{
				nameTable = NameTableView.Which switch
				{
					NameTableViewer.WhichNametable.NT_2000 => 0,
					NameTableViewer.WhichNametable.NT_2400 => 1,
					NameTableViewer.WhichNametable.NT_2800 => 2,
					NameTableViewer.WhichNametable.NT_2C00 => 3,
					_ => 0
				};

				tileX = e.X / 16;
				tileY = e.Y / 16;
			}

			XYLabel.Text = $"{tileX} : {tileY}";
			int ppuAddress = 0x2000 + (nameTable * 0x400) + ((tileY % 30) * 32) + (tileX % 32);
			PPUAddressLabel.Text = $"{ppuAddress:X4}";
			int tileID = _ppu.PeekPPU(ppuAddress);
			TileIDLabel.Text = $"{tileID:X2}";
			TableLabel.Text = nameTable.ToString();

			int yTable = 0, yLine = 0;
			if (e.Y >= 240)
			{
				yTable += 2;
				yLine = 240;
			}
			int table = (e.X >> 8) + yTable;
			int ntAddr = (table << 10);
			int px = e.X & 255;
			int py = e.Y - yLine;
			int tx = px >> 3;
			int ty = py >> 3;
			int atBytePtr = ntAddr + 0x3C0 + ((ty >> 2) << 3) + (tx >> 2);
			int at = _ppu.PeekPPU(atBytePtr + 0x2000);
			if ((ty & 2) != 0) at >>= 4;
			if ((tx & 2) != 0) at >>= 2;
			at &= 0x03;
			PaletteLabel.Text = at.ToString();
		}

		private void NameTableView_MouseLeave(object sender, EventArgs e)
		{
			XYLabel.Text = "";
			PPUAddressLabel.Text = "";
			TileIDLabel.Text = "";
			TableLabel.Text = "";
			PaletteLabel.Text = "";
		}
	}
}
