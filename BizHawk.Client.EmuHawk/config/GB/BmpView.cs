using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public class BmpView : Control
	{
		[Browsable(false)]
		public Bitmap BMP { get; private set; }

		private bool _scaled;

		public BmpView()
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv")
			{
				// in the designer
				SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			}
			else
			{
				SetStyle(ControlStyles.AllPaintingInWmPaint, true);
				SetStyle(ControlStyles.UserPaint, true);
				SetStyle(ControlStyles.DoubleBuffer, true);
				SetStyle(ControlStyles.SupportsTransparentBackColor, true);
				SetStyle(ControlStyles.Opaque, true);
				BackColor = Color.Transparent;
				Paint += BmpView_Paint;
				SizeChanged += BmpView_SizeChanged;
				ChangeBitmapSize(1, 1);
			}
		}

		private void BmpView_SizeChanged(object sender, EventArgs e)
		{
			_scaled = !(BMP.Width == Width && BMP.Height == Height);
		}

		private void BmpView_Paint(object sender, PaintEventArgs e)
		{
			if (_scaled)
			{
				e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
				e.Graphics.DrawImage(BMP, 0, 0, Width, Height);
			}
			else
			{
				e.Graphics.DrawImageUnscaled(BMP, 0, 0);
			}
		}

		public void ChangeBitmapSize(Size s)
		{
			ChangeBitmapSize(s.Width, s.Height);
		}

		public void ChangeBitmapSize(int w, int h)
		{
			if (BMP != null)
			{
				if (w == BMP.Width && h == BMP.Height)
				{
					return;
				}

				BMP.Dispose();
			}


			BMP = new Bitmap(w, h, PixelFormat.Format32bppArgb);
			BmpView_SizeChanged(null, null);
			Refresh();
		}

		public void Clear()
		{
			var lockBits = BMP.LockBits(new Rectangle(0, 0, BMP.Width, BMP.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			Win32Imports.MemSet(lockBits.Scan0, 0xff, (uint)(lockBits.Height * lockBits.Stride));
			BMP.UnlockBits(lockBits);
			Refresh();
		}

		public void SaveFile()
		{
			string path = PathManager.MakeAbsolutePath(
				Global.Config.PathEntries[Global.Emulator.SystemId, "Screenshots"].Path,
				Global.Emulator.SystemId);

			var di = new DirectoryInfo(path);

			if (!di.Exists)
			{
				di.Create();
			}

			using var sfd = new SaveFileDialog
			{
				FileName = $"{PathManager.FilesystemSafeName(Global.Game)}-Palettes",
				InitialDirectory = path,
				Filter = "PNG (*.png)|*.png|Bitmap (*.bmp)|*.bmp|All Files|*.*",
				RestoreDirectory = true
			};

			var result = sfd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return;
			}

			var file = new FileInfo(sfd.FileName);
			var b = BMP;

			ImageFormat i;
			string extension = file.Extension.ToUpper();

			switch (extension)
			{
				default:
				case ".PNG":
					i = ImageFormat.Png;
					break;
				case ".BMP":
					i = ImageFormat.Bmp;
					break;
			}

			b.Save(file.FullName, i);
		}
	}
}
