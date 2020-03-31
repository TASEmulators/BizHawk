using System;
using System.Collections.Generic;
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
			
			if (schemaType != null)
			{
				var padSchemas = ((IVirtualPadSchema)Activator.CreateInstance(schemaType))
					.GetPadSchemas(Emulator)
					.ToList();

				if (VersionInfo.DeveloperBuild)
				{
					CheckPads(padSchemas, Emulator.ControllerDefinition);
				}

				var pads = padSchemas.Select(s => new VirtualPad(s));

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

		private void CheckPads(IEnumerable<PadSchema> schemas, ControllerDefinition def)
		{
			var analogs = new HashSet<string>(def.AxisControls);
			var bools = new HashSet<string>(def.BoolButtons);

			foreach (var schema in schemas)
			{
				foreach (var button in schema.Buttons)
				{
					var searchSet = new HashSet<string>();
					switch (button.Type)
					{
						case PadInputType.AnalogStick:
						case PadInputType.SingleAxis:
						case PadInputType.TargetedPair:
							// analog
							searchSet = analogs;
							break;
						case PadInputType.Boolean:
							searchSet = bools;
							break;
						case PadInputType.DiscManager:
							searchSet = bools;
							searchSet.UnionWith(analogs);
							break;
					}
					if (!searchSet.Contains(button.Name))
					{
						MessageBox.Show(this,
							$"Schema warning: Schema entry '{schema.DisplayName}':'{button.Name}' will not correspond to any control in definition '{def.Name}'",
							"Dev Warning");
					}
				}
			}
		}

		#region IToolForm Implementation

		public bool AskSaveChanges() => true;
		public bool UpdateBefore => false;

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

			if (Global.MovieSession.Movie.Mode == MovieMode.Play)
			{
				Readonly = true;
				if (Global.MovieSession.CurrentInput != null)
				{
					Pads.ForEach(p => p.Set(Global.MovieSession.CurrentInput));
				}
			}
			else
			{
				if (Global.MovieSession.Movie.IsRecording())
				{
					Pads.ForEach(p => p.SetPrevious(Global.MovieSession.PreviousFrame));
				}

				Readonly = false;
			}

			if (!Readonly && !StickyPads && !MouseButtons.HasFlag(MouseButtons.Left))
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
