using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Client.EmuHawk;
using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using System.IO;


namespace BizHawk.Client.MultiHawk
{
	// TODO: we can safely assume videoprovider cores are a requirement of multihawk,
	// but fail sooner and with a clear message instead of making misc calls to AsVideoProvider that will fail
	public partial class EmulatorWindow : Form
	{
		public EmulatorWindow(Mainform parent)
		{
			InitializeComponent();
			Closing += (o, e) =>
			{
				ShutDown();
			};

			MainForm = parent;
		}

		private void EmulatorWindow_Load(object sender, EventArgs e)
		{
			if (Game != null)
			{
				Text = Game.Name;
			}
		}

		public Mainform MainForm { get; private set; }
		public IEmulator Emulator { get; set; }
		public CoreComm CoreComm { get; set; }
		public GameInfo Game { get; set; }
		public string CurrentRomPath { get; set; }

		public IGL GL { get; set; }
		public GLManager.ContextRef CR_GL { get; set; }
		public GLManager GLManager { get; set; }

		public PresentationPanel PresentationPanel { get; set; }

		//public Sound Sound; // TODO
		public DisplayManager DisplayManager;
		

		public void Init()
		{
			PresentationPanel = new PresentationPanel(this, GL);
			CR_GL = GLManager.GetContextForIGL(GL);
			DisplayManager = new DisplayManager(PresentationPanel, GL ,GLManager);

			Controls.Add(PresentationPanel);
			Controls.SetChildIndex(PresentationPanel, 0);
		}

		public void ShutDown()
		{
			SaveRam();
			MainForm.EmulatorWindowClosed(this);
			Emulator.Dispose();
			GL.Dispose();
		}

		public void LoadQuickSave(string quickSlotName)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			var path = PathManager.SaveStatePrefix(Game) + "." + quickSlotName + ".State";

			if (LoadStateFile(path, quickSlotName))
			{
				// SetMainformMovieInfo(); // TODO
				MainForm.AddMessage("Loaded state: " + quickSlotName);
			}
			else
			{
				MainForm.AddMessage("Loadstate error!");
			}
		}

		public bool LoadStateFile(string path, string name)
		{
			var core = Emulator.AsStatable();

			// try to detect binary first
			var bl = BinaryStateLoader.LoadAndDetect(path);
			if (bl != null)
			{
				try
				{
					var succeed = false;

					// TODO
					if (IAmMaster)
					{
						if (Global.MovieSession.Movie.IsActive)
						{
							bl.GetLump(BinaryStateLump.Input, true, tr => succeed = Global.MovieSession.HandleMovieLoadState_HackyStep1(tr));
							if (!succeed)
							{
								return false;
							}

							bl.GetLump(BinaryStateLump.Input, true, tr => succeed = Global.MovieSession.HandleMovieLoadState_HackyStep2(tr));
							if (!succeed)
							{
								return false;
							}
						}
					}

					using (new SimpleTime("Load Core"))
						bl.GetCoreState(br => core.LoadStateBinary(br), tr => core.LoadStateText(tr));

					bl.GetLump(BinaryStateLump.Framebuffer, false, PopulateFramebuffer);
				}
				catch
				{
					return false;
				}
				finally
				{
					bl.Dispose();
				}

				return true;
			}
			else // text mode
			{
				if (Global.MovieSession.HandleMovieLoadState(path))
				{
					using (var reader = new StreamReader(path))
					{
						core.LoadStateText(reader);

						while (true)
						{
							var str = reader.ReadLine();
							if (str == null)
							{
								break;
							}

							if (str.Trim() == string.Empty)
							{
								continue;
							}

							var args = str.Split(' ');
							if (args[0] == "Framebuffer")
							{
								Emulator.AsVideoProvider().GetVideoBuffer().ReadFromHex(args[1]);
							}
						}
					}

					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public void PopulateFramebuffer(BinaryReader br)
		{
			try
			{
				using (new SimpleTime("Load Framebuffer"))
					QuickBmpFile.Load(Emulator.AsVideoProvider(), br.BaseStream);
			}
			catch
			{
				var buff = Emulator.AsVideoProvider().GetVideoBuffer();
				try
				{
					for (int i = 0; i < buff.Length; i++)
					{
						int j = br.ReadInt32();
						buff[i] = j;
					}
				}
				catch (EndOfStreamException) { }
			}
		}

		public void SaveQuickSave(string quickSlotName)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			var path = PathManager.SaveStatePrefix(Game) + "." + quickSlotName + ".State";

			var file = new FileInfo(path);
			if (file.Directory != null && file.Directory.Exists == false)
			{
				file.Directory.Create();
			}

			// TODO
			// Make backup first
			//if (Global.Config.BackupSavestates && file.Exists)
			//{
			//	var backup = path + ".bak";
			//	var backupFile = new FileInfo(backup);
			//	if (backupFile.Exists)
			//	{
			//		backupFile.Delete();
			//	}

			//	File.Move(path, backup);
			//}

			try
			{
				SaveStateFile(path, quickSlotName);

				MainForm.AddMessage("Saved state: " + quickSlotName);
			}
			catch (IOException)
			{
				MainForm.AddMessage("Unable to save state " + path);
			}

			// TODO
		}

		private void SaveStateFile(string filename, string name)
		{
			var core = Emulator.AsStatable();

			using (var bs = new BinaryStateSaver(filename))
			{
				if (Global.Config.SaveStateType == Config.SaveStateTypeE.Text ||
					(Global.Config.SaveStateType == Config.SaveStateTypeE.Default && !core.BinarySaveStatesPreferred))
				{
					// text savestate format
					using (new SimpleTime("Save Core"))
						bs.PutLump(BinaryStateLump.CorestateText, (tw) => core.SaveStateText(tw));
				}
				else
				{
					// binary core lump format
					using (new SimpleTime("Save Core"))
						bs.PutLump(BinaryStateLump.Corestate, bw => core.SaveStateBinary(bw));
				}

				if (true) //TODO: Global.Config.SaveScreenshotWithStates)
				{
					var vp = Emulator.AsVideoProvider();
					var buff = vp.GetVideoBuffer();

					int out_w = vp.BufferWidth;
					int out_h = vp.BufferHeight;

					// if buffer is too big, scale down screenshot
					if (true /* !Global.Config.NoLowResLargeScreenshotWithStates*/ && buff.Length >= Global.Config.BigScreenshotSize)
					{
						out_w /= 2;
						out_h /= 2;
					}
					using (new SimpleTime("Save Framebuffer"))
						bs.PutLump(BinaryStateLump.Framebuffer, (s) => QuickBmpFile.Save(Emulator.AsVideoProvider(), s, out_w, out_h));
				}

				if (IAmMaster)
				{
					if (Global.MovieSession.Movie.IsActive)
					{
						bs.PutLump(BinaryStateLump.Input,
							delegate(TextWriter tw)
							{
								// this never should have been a core's responsibility
								tw.WriteLine("Frame {0}", Emulator.Frame);
								Global.MovieSession.HandleMovieSaveState(tw);
							});
					}
				}
			}
		}

		public bool IAmMaster
		{
			get
			{
				return MainForm.EmulatorWindows.First() == this;
			}
		}

		public void FrameBufferResized()
		{
			// run this entire thing exactly twice, since the first resize may adjust the menu stacking
			for (int i = 0; i < 2; i++)
			{
				var video = Emulator.AsVideoProvider();
				int zoom = Global.Config.TargetZoomFactors[Global.Emulator.SystemId];
				var area = Screen.FromControl(this).WorkingArea;

				int borderWidth = Size.Width - PresentationPanel.Control.Size.Width;
				int borderHeight = Size.Height - PresentationPanel.Control.Size.Height;

				// start at target zoom and work way down until we find acceptable zoom
				Size lastComputedSize = new Size(1, 1);
				for (; zoom >= 1; zoom--)
				{
					lastComputedSize = DisplayManager.CalculateClientSize(video, zoom);
					if ((((lastComputedSize.Width) + borderWidth) < area.Width)
						&& (((lastComputedSize.Height) + borderHeight) < area.Height))
					{
						break;
					}
				}
				Console.WriteLine("Selecting display size " + lastComputedSize.ToString());

				// Change size
				Size = new Size((lastComputedSize.Width) + borderWidth, ((lastComputedSize.Height) + borderHeight));
				PerformLayout();
				PresentationPanel.Resized = true;

				// Is window off the screen at this size?
				if (area.Contains(Bounds) == false)
				{
					if (Bounds.Right > area.Right) // Window is off the right edge
					{
						Location = new Point(area.Right - Size.Width, Location.Y);
					}

					if (Bounds.Bottom > area.Bottom) // Window is off the bottom edge
					{
						Location = new Point(Location.X, area.Bottom - Size.Height);
					}
				}
			}
		}

		private Size _lastVideoSize = new Size(-1, -1), _lastVirtualSize = new Size(-1, -1);
		public void Render()
		{
			var video = Emulator.AsVideoProvider();

			Size currVideoSize = new Size(video.BufferWidth, video.BufferHeight);
			Size currVirtualSize = new Size(video.VirtualWidth, video.VirtualWidth);
			if (currVideoSize != _lastVideoSize || currVirtualSize != _lastVirtualSize)
			{
				_lastVideoSize = currVideoSize;
				_lastVirtualSize = currVirtualSize;
				FrameBufferResized();
			}

			DisplayManager.UpdateSource(video);
		}

		public void FrameAdvance()
		{
			Emulator.FrameAdvance(true);
		}

		public void SaveRam()
		{
			if (Emulator.HasSaveRam() && Emulator.AsSaveRam().SaveRamModified)
			{
				var path = PathManager.SaveRamPath(Global.Game);
				var f = new FileInfo(path);
				if (f.Directory != null && f.Directory.Exists == false)
				{
					f.Directory.Create();
				}

				// Make backup first
				if (Global.Config.BackupSaveram && f.Exists)
				{
					var backup = path + ".bak";
					var backupFile = new FileInfo(backup);
					if (backupFile.Exists)
					{
						backupFile.Delete();
					}

					f.CopyTo(backup);
				}

				var writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write));
				var saveram = Emulator.AsSaveRam().CloneSaveRam();

				writer.Write(saveram, 0, saveram.Length);
				writer.Close();
			}
		}
	}
}
