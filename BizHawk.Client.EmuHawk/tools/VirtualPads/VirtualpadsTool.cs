using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualpadTool : Form, IToolForm
	{
		private int _defaultWidth;
		private int _defaultHeight;
		private bool _readOnly;

		private List<VirtualPad> Pads
		{
			get
			{
				return ControllerBox.Controls
					.OfType<VirtualPad>()
					.ToList();
			}
		}

		
		public bool Readonly
		{
			get
			{
				return _readOnly;
			}

			set
			{
				_readOnly = value;
				Pads.ForEach(p => p.ReadOnly = value);
			}
		}

		public VirtualpadTool()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
			TopMost = Global.Config.VirtualPadSettings.TopMost;
		}

		private void VirtualpadTool_Load(object sender, EventArgs e)
		{
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			if (Global.Config.VirtualPadSettings.UseWindowPosition)
			{
				Location = Global.Config.VirtualPadSettings.WindowPosition;
			}

			if (Global.Config.VirtualPadSettings.UseWindowPosition)
			{
				Size = Global.Config.VirtualPadSettings.WindowSize;
			}

			CreatePads();
		}

		public void ClearVirtualPadHolds()
		{
			Pads.ForEach(pad => pad.Clear());
		}

		public void BumpAnalogValue(int? x, int? y) // TODO: multi-player
		{
			Pads.ForEach(pad => pad.BumpAnalog(x, y));
		}

		private void CreatePads()
		{
			ControllerBox.Controls.Clear();

			var schemaType = Assembly
				.GetExecutingAssembly()
				.GetTypes()
				.Where(t => typeof(IVirtualPadSchema)
					.IsAssignableFrom(t) && t.GetCustomAttributes(false)
					.OfType<SchemaAttributes>()
					.Any())
				.FirstOrDefault(t => t.GetCustomAttributes(false)
					.OfType<SchemaAttributes>()
					.First().SystemId == Global.Emulator.SystemId);
			
			if (schemaType != null)
			{
				var padschemas = (Activator.CreateInstance(schemaType) as IVirtualPadSchema).GetPadSchemas();
				if (VersionInfo.DeveloperBuild)
				{
					CheckPads(padschemas, Global.Emulator.ControllerDefinition);
				}
				var pads = padschemas.Select(s => new VirtualPad(s));

				if (pads.Any())
				{
					ControllerBox.Controls.AddRange(pads.Reverse().ToArray());
				}
			}
		}

		private void CheckPads(IEnumerable<PadSchema> schemas, BizHawk.Emulation.Common.ControllerDefinition def)
		{
			HashSet<string> analogs = new HashSet<string>(def.FloatControls);
			HashSet<string> bools = new HashSet<string>(def.BoolButtons);

			foreach (var schema in schemas)
			{
				foreach (var button in schema.Buttons)
				{
					HashSet<string> searchset = null;
					switch (button.Type)
					{
						case PadSchema.PadInputType.AnalogStick:
						case PadSchema.PadInputType.FloatSingle:
						case PadSchema.PadInputType.TargetedPair:
							// analog
							searchset = analogs;
							break;
						case PadSchema.PadInputType.Boolean:
							searchset = bools;
							break;
					}
					if (!searchset.Contains(button.Name))
					{
						MessageBox.Show(this,
							string.Format("Schema warning: Schema entry '{0}':'{1}' will not correspond to any control in definition '{2}'", schema.DisplayName, button.Name, def.Name),
							"Dev Warning");
					}
				}
			}
		}

		private void SaveConfigSettings()
		{
			Global.Config.VirtualPadSettings.Wndx = Location.X;
			Global.Config.VirtualPadSettings.Wndy = Location.Y;
			Global.Config.VirtualPadSettings.Width = Right - Left;
			Global.Config.VirtualPadSettings.Height = Bottom - Top;
		}

		private void RefreshFloatingWindowControl()
		{
			Owner = Global.Config.VirtualPadSettings.FloatingWindow ? null : GlobalWin.MainForm;
		}

		#region IToolForm Implementation

		public bool AskSave() { return true; }
		public bool UpdateBefore { get { return false; } }

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			CreatePads();
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			Pads.ForEach(p => p.SetPrevious(null)); // Not the cleanest way to clear this every frame

			if (Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsFinished)
			{
				Readonly = true;
				Pads.ForEach(p => p.Set(Global.MovieSession.CurrentInput));
			}
			else
			{
				if (Global.MovieSession.Movie.IsRecording)
				{
					Pads.ForEach(p => p.SetPrevious(Global.MovieSession.PreviousFrame));
				}

				Readonly = false;
			}

			if (!Readonly && !Global.Config.VirtualPadSticky)
			{
				Pads.ForEach(pad => pad.Clear());
			}

			Pads.ForEach(pad => pad.UpdateValues());
		}

		public void FastUpdate()
		{
			// TODO: SetPrevious logic should go here too or that will get out of whack

			if (!Readonly && !Global.Config.VirtualPadSticky)
			{
				Pads.ForEach(pad => pad.Clear());
			}
		}

		#endregion

		#region Events

		#region Menu

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadMenuItem.Checked = Global.Config.AutoloadVirtualPad;
			SaveWindowPositionMenuItem.Checked = Global.Config.VirtualPadSettings.SaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Global.Config.VirtualPadSettings.TopMost;
			FloatingWindowMenuItem.Checked = Global.Config.VirtualPadSettings.FloatingWindow;
		}

		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoloadVirtualPad ^= true;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VirtualPadSettings.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VirtualPadSettings.TopMost ^= true;
			TopMost = Global.Config.VirtualPadSettings.TopMost;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VirtualPadSettings.FloatingWindow ^= true;
			RefreshFloatingWindowControl();
		}

		private void RestoreDefaultSettingsMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);

			Global.Config.VirtualPadSettings.SaveWindowPosition = true;
			Global.Config.VirtualPadSettings.TopMost = TopMost = false;
			Global.Config.VirtualPadSettings.FloatingWindow = false;
			Global.Config.VirtualPadMultiplayerMode = false;
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void PadsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			StickyMenuItem.Checked = Global.Config.VirtualPadSticky;
		}

		private void ClearAllMenuItem_Click(object sender, EventArgs e)
		{
			ClearVirtualPadHolds();
		}

		private void StickyMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VirtualPadSticky ^= true;
		}

		private void PadBoxContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			StickyContextMenuItem.Checked = Global.Config.VirtualPadSticky;
		}

		#endregion

		#endregion
	}
}
