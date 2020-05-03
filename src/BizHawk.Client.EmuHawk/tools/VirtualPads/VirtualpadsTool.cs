using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualpadTool : ToolFormBase, IToolFormAutoConfig
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[ConfigPersist]
		public bool StickyPads { get; set; }

		[ConfigPersist]
		public bool ClearAlsoClearsAnalog { get; set; }
		
		private bool _readOnly;

		private List<VirtualPad> Pads =>
			ControllerPanel.Controls
				.OfType<VirtualPad>()
				.ToList();

		public bool Readonly
		{
			get => _readOnly;
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
					.OfType<SchemaAttribute>()
					.Any())
				.FirstOrDefault(t => t.GetCustomAttributes(false)
					.OfType<SchemaAttribute>()
					.First().SystemId == Emulator.SystemId);

			if (schemaType == null) return;

			var padSchemata = ((IVirtualPadSchema) Activator.CreateInstance(schemaType))
				.GetPadSchemas(Emulator)
				.ToList();

			if (VersionInfo.DeveloperBuild)
			{
				var buttonControls = Emulator.ControllerDefinition.BoolButtons;
				var axisControls = Emulator.ControllerDefinition.AxisControls;
				foreach (var schema in padSchemata) foreach (var controlSchema in schema.Buttons)
				{
					Predicate<string> searchSetContains = controlSchema switch
					{
						ButtonSchema _ => buttonControls.Contains,
						DiscManagerSchema _ => s => buttonControls.Contains(s) || axisControls.Contains(s),
						_ => axisControls.Contains
					};
					if (!searchSetContains(controlSchema.Name))
					{
						MessageBox.Show(this,
							$"Schema warning: Schema entry '{schema.DisplayName}':'{controlSchema.Name}' will not correspond to any control in definition '{Emulator.ControllerDefinition.Name}'",
							"Dev Warning");
					}
				}
			}

			ControllerPanel.Controls.AddRange(padSchemata.Select(s => (Control) new VirtualPad(s)).Reverse().ToArray());
		}

		public void ScrollToPadSchema(string padSchemaName)
		{
			foreach (var control in ControllerPanel.Controls)
			{
				var vp = control as VirtualPad;
				if (vp == null)
				{
					continue;
				}

				if (vp.PadSchemaDisplayName == padSchemaName)
				{
					ControllerPanel.ScrollControlIntoView(vp);
				}
			}
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			CreatePads();
		}

		protected override void UpdateValuesAfter()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			Pads.ForEach(p => p.SetPrevious(null)); // Not the cleanest way to clear this every frame

			if (MovieSession.Movie.Mode == MovieMode.Play)
			{
				Readonly = true;
				if (MovieSession.CurrentInput != null)
				{
					Pads.ForEach(p => p.Set(MovieSession.CurrentInput));
				}
			}
			else
			{
				if (MovieSession.Movie.IsRecording())
				{
					Pads.ForEach(p => p.SetPrevious(MovieSession.PreviousFrame));
				}

				Readonly = false;
			}

			if (!Readonly && !StickyPads && !MouseButtons.HasFlag(MouseButtons.Left))
			{
				Pads.ForEach(pad => pad.Clear());
			}

			Pads.ForEach(pad => pad.UpdateValues());
		}

		protected override void FastUpdate()
		{
			// TODO: SetPrevious logic should go here too or that will get out of whack

			if (!Readonly && !StickyPads)
			{
				Pads.ForEach(pad => pad.Clear());
			}
		}

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
