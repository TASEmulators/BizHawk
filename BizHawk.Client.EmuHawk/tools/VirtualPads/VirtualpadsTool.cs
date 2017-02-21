using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualpadTool : Form, IToolFormAutoConfig
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[ConfigPersist]
		public bool StickyPads { get; set; }

		[ConfigPersist]
		public bool ClearAlsoClearsAnalog { get; set; }
		
		private bool _readOnly;

		private List<VirtualPad> Pads
		{
			get
			{
				return ControllerPanel.Controls
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
			StickyPads = true;
			InitializeComponent();
		}

		private void VirtualpadTool_Load(object sender, EventArgs e)
		{
			CreatePads();
		}

		public void ClearVirtualPadHolds()
		{
			if (ClearAlsoClearsAnalog)
			{
				Pads.ForEach(pad => pad.Clear());
			}
			else
			{
				Pads.ForEach(pad => pad.ClearBoolean());
			}
		}

		public void BumpAnalogValue(int? x, int? y) // TODO: multi-player
		{
			Pads.ForEach(pad => pad.BumpAnalog(x, y));
		}

		private void CreatePads()
		{
			ControllerPanel.Controls.Clear();

			var schemaType = Assembly
				.GetExecutingAssembly()
				.GetTypes()
				.Where(t => typeof(IVirtualPadSchema)
					.IsAssignableFrom(t) && t.GetCustomAttributes(false)
					.OfType<SchemaAttributes>()
					.Any())
				.FirstOrDefault(t => t.GetCustomAttributes(false)
					.OfType<SchemaAttributes>()
					.First().SystemId == Emulator.SystemId);
			
			if (schemaType != null)
			{
				var padschemas = (Activator.CreateInstance(schemaType) as IVirtualPadSchema).GetPadSchemas(Emulator);
				if (VersionInfo.DeveloperBuild)
				{
					CheckPads(padschemas, Emulator.ControllerDefinition);
				}
				var pads = padschemas.Select(s => new VirtualPad(s));

				if (pads.Any())
				{
					ControllerPanel.Controls.AddRange(pads.Reverse().ToArray());
				}
			}
		}

		public void ScrollToPadSchema(string padSchemaName)
		{
			foreach (var control in ControllerPanel.Controls)
			{
				VirtualPad vp = control as VirtualPad;
				if (vp == null) continue;
				if (vp.PadSchemaDisplayName == padSchemaName)
					ControllerPanel.ScrollControlIntoView(vp);
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
						case PadSchema.PadInputType.DiscManager:
							searchset = bools;
							searchset.UnionWith(analogs);
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

		#region IToolForm Implementation

		public bool AskSaveChanges() { return true; }
		public bool UpdateBefore { get { return false; } }

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			CreatePads();
		}

		public void NewUpdate(ToolFormUpdateType type) { }

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

			if (!Readonly && !StickyPads && !Control.MouseButtons.HasFlag(MouseButtons.Left))
			{
				Pads.ForEach(pad => pad.Clear());
			}

			Pads.ForEach(pad => pad.UpdateValues());
		}

		public void FastUpdate()
		{
			// TODO: SetPrevious logic should go here too or that will get out of whack

			if (!Readonly && !StickyPads)
			{
				Pads.ForEach(pad => pad.Clear());
			}
		}

		#endregion

		#region Menu

		private void PadsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			StickyMenuItem.Checked = StickyPads;
		}

		private void ClearAllMenuItem_Click(object sender, EventArgs e)
		{
			ClearVirtualPadHolds();
		}

		private void StickyMenuItem_Click(object sender, EventArgs e)
		{
			StickyPads ^= true;
		}

		private void PadBoxContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			StickyContextMenuItem.Checked = StickyPads;
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ClearClearsAnalogInputMenuItem.Checked = ClearAlsoClearsAnalog;
		}

		private void ClearClearsAnalogInputMenuItem_Click(object sender, EventArgs e)
		{
			ClearAlsoClearsAnalog ^= true;
		}

		#endregion
	}
}
