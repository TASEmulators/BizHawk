using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualpadTool : ToolFormBase, IToolFormAutoConfig
	{
		public static Icon ToolIcon
			=> Properties.Resources.GameControllerIcon;

		[RequiredService]
		private IEmulator Emulator { get; set; }

		[ConfigPersist]
		public bool StickyPads { get; set; }

		[ConfigPersist]
		public bool ClearAlsoClearsAnalog { get; set; }
		
		private bool _readOnly;

		private Control _lastFocusedNUD = null;

		public override bool BlocksInputWhenFocused => _lastFocusedNUD is not null;

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

		protected override string WindowTitleStatic => "Virtual Pads";

		public VirtualpadTool()
		{
			StickyPads = true;
			InitializeComponent();
			Icon = ToolIcon;
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

		private void SetLastFocusedNUD(object sender, EventArgs args)
			=> _lastFocusedNUD = (Control) sender;

		private void CreatePads()
		{
			ControllerPanel.Controls.Clear();

			Type schemaType;
			try
			{
				schemaType = Emulation.Cores.ReflectionCache.Types.Where(typeof(IVirtualPadSchema).IsAssignableFrom)
					.Select(t => (SchemaType: t, Attr: t.GetCustomAttributes(false).OfType<SchemaAttribute>().FirstOrDefault()))
					.First(tuple => tuple.Attr?.SystemId == Emulator.SystemId)
					.SchemaType;
			}
			catch (Exception)
			{
				return;
			}

			var padSchemata = ((IVirtualPadSchema) Activator.CreateInstance(schemaType))
				.GetPadSchemas(Emulator, s => DialogController.ShowMessageBox(s))
				.ToList();

			if (VersionInfo.DeveloperBuild)
			{
				var buttonControls = Emulator.ControllerDefinition.BoolButtons;
				var axisControls = Emulator.ControllerDefinition.Axes;
				foreach (var schema in padSchemata) foreach (var controlSchema in schema.Buttons)
				{
					Predicate<string> searchSetContains = controlSchema switch
					{
						ButtonSchema => buttonControls.Contains,
						DiscManagerSchema => s => buttonControls.Contains(s) || axisControls.ContainsKey(s),
						_ => axisControls.ContainsKey
					};
					if (!searchSetContains(controlSchema.Name))
					{
						this.ModalMessageBox(
							$"Schema warning: Schema entry '{schema.DisplayName}':'{controlSchema.Name}' will not correspond to any control in definition '{Emulator.ControllerDefinition.Name}'",
							"Dev Warning");
					}
				}
			}

			ControllerPanel.Controls.AddRange(padSchemata.Select(Control (s) => new VirtualPad(s, InputManager, SetLastFocusedNUD)).Reverse().ToArray());
		}

		public void ScrollToPadSchema(string padSchemaName)
		{
			foreach (var control in ControllerPanel.Controls)
			{
				if (control is not VirtualPad vp)
				{
					continue;
				}

				if (vp.PadSchemaDisplayName == padSchemaName)
				{
					ControllerPanel.ScrollControlIntoView(vp);
				}
			}
		}

		public override void Restart()
		{
			if (!IsActive)
			{
				return;
			}

			CreatePads();
		}

		protected override void GeneralUpdate() => UpdateAfter();

		protected override void UpdateAfter()
		{
			if (!IsActive)
			{
				return;
			}

			Pads.ForEach(p => p.SetPrevious(null)); // Not the cleanest way to clear this every frame
			Readonly = MovieSession.Movie.IsPlaying();

			if (MovieSession.Movie.IsPlaying())
			{
				var currentInput = CurrentInput();
				if (currentInput != null)
				{
					Pads.ForEach(p => p.Set(currentInput));
				}
			}
			else if (MovieSession.Movie.IsRecording())
			{
				var previousFrame = PreviousFrame();
				Pads.ForEach(p => p.SetPrevious(previousFrame));
			}

			if (!Readonly && !StickyPads && !MouseButtons.HasFlag(MouseButtons.Left))
			{
				Pads.ForEach(pad => pad.Clear());
			}

			Pads.ForEach(pad => pad.UpdateValues());
		}

		private IController CurrentInput()
		{
			if (MovieSession.Movie.IsPlayingOrRecording() && Emulator.Frame > 0)
			{
				return MovieSession.Movie.GetInputState(Emulator.Frame - 1);
			}

			return null;
		}

		private IController PreviousFrame()
		{
			if (MovieSession.Movie.IsPlayingOrRecording() && Emulator.Frame > 1)
			{
				return MovieSession.Movie.GetInputState(Emulator.Frame - 2);
			}

			return null;
		}

		protected override void FastUpdateAfter()
		{
			// TODO: SetPrevious logic should go here too or that will get out of whack

			if (!Readonly && !StickyPads)
			{
				Pads.ForEach(pad => pad.Clear());
			}
		}

		private void PadsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			StickyMenuItem.Checked = StickyPads;
		}

		private void ClearAllMenuItem_Click(object sender, EventArgs e)
		{
			ClearVirtualPadHolds();
		}

		private void StickyMenuItem_Click(object sender, EventArgs e)
			=> StickyPads = !StickyPads;

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
			=> ClearAlsoClearsAnalog = !ClearAlsoClearsAnalog;
	}
}
