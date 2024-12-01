using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

namespace BizHawk.Client.EmuHawk
{
	public partial class ColorChooserForm : Form, IDialogParent
	{
		private readonly Config _config;
		private readonly IGameInfo _game;

		public IDialogController DialogController { get; }

		private ColorChooserForm(IDialogController dialogController, Config config, IGameInfo game)
		{
			_config = config;
			_game = game;
			DialogController = dialogController;
			InitializeComponent();
			Icon = Properties.Resources.GambatteIcon;
		}

		private readonly Color[] _colors = new Color[12];

		// gambatte's default dmg colors
		private static readonly int[] DefaultCGBColors =
		{
			0x00ffffff, 0x00aaaaaa, 0x00555555, 0x00000000,
			0x00ffffff, 0x00aaaaaa, 0x00555555, 0x00000000,
			0x00ffffff, 0x00aaaaaa, 0x00555555, 0x00000000
		};

		// bsnes's default dmg colors with slight tweaking
		private static readonly int[] DefaultDMGColors =
		{
			10798341, 8956165, 1922333, 337157,
			10798341, 8956165, 1922333, 337157,
			10798341, 8956165, 1922333, 337157
		};

		private void RefreshAllBackdrops()
		{
			panel1.BackColor = _colors[0];
			panel2.BackColor = _colors[1];
			panel3.BackColor = _colors[2];
			panel4.BackColor = _colors[3];
			panel5.BackColor = _colors[4];
			panel6.BackColor = _colors[5];
			panel7.BackColor = _colors[6];
			panel8.BackColor = _colors[7];
			panel9.BackColor = _colors[8];
			panel10.BackColor = _colors[9];
			panel11.BackColor = _colors[10];
			panel12.BackColor = _colors[11];
		}

		private Color BetweenColor(Color left, Color right, double pos)
		{
			int r = (int)(right.R * pos + left.R * (1.0 - pos) + 0.5);
			int g = (int)(right.G * pos + left.G * (1.0 - pos) + 0.5);
			int b = (int)(right.B * pos + left.B * (1.0 - pos) + 0.5);
			int a = (int)(right.A * pos + left.A * (1.0 - pos) + 0.5);

			return Color.FromArgb(a, r, g, b);
		}

		private void InterpolateColors(int firstIndex, int lastIndex)
		{
			for (int i = firstIndex + 1; i < lastIndex; i++)
			{
				double pos = (i - firstIndex) / (double)(lastIndex - firstIndex);
				_colors[i] = BetweenColor(_colors[firstIndex], _colors[lastIndex], pos);
			}

			RefreshAllBackdrops();
		}

		private void Button3_Click(object sender, EventArgs e)
		{
			InterpolateColors(0, 3);
		}

		private void Button4_Click(object sender, EventArgs e)
		{
			InterpolateColors(4, 7);
		}

		private void Button5_Click(object sender, EventArgs e)
		{
			InterpolateColors(8, 11);
		}

		private void Panel12_DoubleClick(object sender, EventArgs e)
		{
			Panel panel = (Panel)sender;

			int i;
			if (panel == panel1)
				i = 0;
			else if (panel == panel2)
				i = 1;
			else if (panel == panel3)
				i = 2;
			else if (panel == panel4)
				i = 3;
			else if (panel == panel5)
				i = 4;
			else if (panel == panel6)
				i = 5;
			else if (panel == panel7)
				i = 6;
			else if (panel == panel8)
				i = 7;
			else if (panel == panel9)
				i = 8;
			else if (panel == panel10)
				i = 9;
			else if (panel == panel11)
				i = 10;
			else if (panel == panel12)
				i = 11;
			else
				return; // i = -1;

			using var dlg = new ColorDialog
			{
				AllowFullOpen = true,
				AnyColor = true,
				Color = _colors[i]
			};

			// custom colors are ints, not Color structs?
			// and they don't work right unless the alpha bits are set to 0
			// and the rgb order is switched
			int[] customs = new int[12];
			for (int j = 0; j < customs.Length; j++)
			{
				customs[j] = _colors[j].R | _colors[j].G << 8 | _colors[j].B << 16;
			}

			dlg.CustomColors = customs;
			dlg.FullOpen = true;

			if (!this.ShowDialogAsChild(dlg).IsOk()) return;

			if (_colors[i] != dlg.Color)
			{
				_colors[i] = dlg.Color;
				panel.BackColor = _colors[i];
			}
		}

		// ini keys for gambatte palette file
		private static readonly string[] PaletteIniKeys =
		{
			"Background0",
			"Background1",
			"Background2",
			"Background3",
			"Sprite%2010",
			"Sprite%2011",
			"Sprite%2012",
			"Sprite%2013",
			"Sprite%2020",
			"Sprite%2021",
			"Sprite%2022",
			"Sprite%2023"
		};

		/// <summary>
		/// load gambatte-style .pal file
		/// </summary>
		/// <returns>null on failure</returns>
		public static int[] LoadPalFile(TextReader f)
		{
			var lines = new Dictionary<string, int>();

			string line;
			while ((line = f.ReadLine()) != null)
			{
				int i = line.IndexOf('=');
				if (i < 0)
				{
					continue;
				}

				try
				{
					lines.Add(
						line.Substring(startIndex: 0, length: i),
						int.Parse(line.Substring(startIndex: i + 1)));
				}
				catch (FormatException)
				{
				}
			}

			int[] ret = new int[12];
			try
			{
				for (int i = 0; i < 12; i++)
				{
					ret[i] = lines[PaletteIniKeys[i]];
				}
			}
			catch (KeyNotFoundException)
			{
				return null;
			}

			return ret;
		}

		// save gambatte-style palette file
		private static void SavePalFile(TextWriter f, int[] colors)
		{
			f.WriteLine("[General]");
			for (int i = 0; i < 12; i++)
			{
				f.WriteLine($"{PaletteIniKeys[i]}={colors[i]}");
			}
		}

		private void SetAllColors(int[] colors)
		{
			// fix alpha to 255 in created color objects, else problems
			for (int i = 0; i < _colors.Length; i++)
			{
				_colors[i] = Color.FromArgb(255, Color.FromArgb(colors[i]));
			}

			RefreshAllBackdrops();
		}

		public static void DoColorChooserFormDialog(IDialogParent parent, Config config, IGameInfo game, Gameboy.GambatteSettings s)
		{
			using var dlg = new ColorChooserForm(parent.DialogController, config, game);

			dlg.SetAllColors(s.GBPalette);

			var result = parent.ShowDialogAsChild(dlg);
			if (result.IsOk())
			{
				int[] colors = new int[12];
				for (int i = 0; i < 12; i++)
				{
					colors[i] = dlg._colors[i].ToArgb();
				}

				s.GBPalette = colors;
			}
		}

		private void LoadColorFile(string filename, bool alert)
		{
			try
			{
				using var f = new StreamReader(filename);
				int[] newColors = LoadPalFile(f);
				if (newColors == null)
				{
					throw new Exception();
				}

				SetAllColors(newColors);
			}
			catch
			{
				if (alert)
				{
					this.ModalMessageBox("Error loading .pal file!");
				}
			}
		}

		private void SaveColorFile(string filename)
		{
			try
			{
				using var f = new StreamWriter(filename);
				int[] saveColors = new int[12];
				for (int i = 0; i < 12; i++)
				{
					// clear alpha because gambatte color files don't usually contain it
					saveColors[i] = _colors[i].ToArgb() & 0xffffff;
				}

				SavePalFile(f, saveColors);
			}
			catch
			{
				this.ModalMessageBox("Error saving .pal file!");
			}
		}

		private void Button6_Click(object sender, EventArgs e)
		{
			var result = this.ShowFileOpenDialog(
				discardCWDChange: true,
				filter: FilesystemFilterSet.Palettes,
				initDir: _config.PathEntries.PalettesAbsolutePathFor(VSystemID.Raw.GB));
			if (result is not null) LoadColorFile(result, alert: true);
		}

		private void ColorChooserForm_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				var files = (string[])e.Data.GetData(DataFormats.FileDrop);

				if (files.Length > 1)
				{
					return;
				}

				LoadColorFile(files[0], true);
			}
		}

		private void ColorChooserForm_DragEnter(object sender, DragEventArgs e)
		{
			e.Set(DragDropEffects.Move);
		}

		private void Button7_Click(object sender, EventArgs e)
		{
			var result = this.ShowFileSaveDialog(
				discardCWDChange: true,
				filter: FilesystemFilterSet.Palettes,
				initDir: _config.PathEntries.PalettesAbsolutePathFor(VSystemID.Raw.GB),
				initFileName: $"{_game.Name}.pal");
			if (result is not null) SaveColorFile(result);
		}

		private void DefaultButton_Click(object sender, EventArgs e)
		{
			SetAllColors(DefaultDMGColors);
		}

		private void DefaultButtonCGB_Click(object sender, EventArgs e)
		{
			SetAllColors(DefaultCGBColors);
		}
	}
}
