using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPad : UserControl
	{
		private static readonly IReadOnlyDictionary<VGamepadButtonImage, Bitmap> _buttonImages = new Dictionary<VGamepadButtonImage, Bitmap>
		{
			[VGamepadButtonImage.BlueArrE] = Properties.Resources.Forward,
			[VGamepadButtonImage.BlueArrENE] = Properties.Resources.ENE,
			[VGamepadButtonImage.BlueArrESE] = Properties.Resources.ESE,
			[VGamepadButtonImage.BlueArrN] = Properties.Resources.BlueUp,
			[VGamepadButtonImage.BlueArrNE] = Properties.Resources.NE,
			[VGamepadButtonImage.BlueArrNNE] = Properties.Resources.NNE,
			[VGamepadButtonImage.BlueArrNNW] = Properties.Resources.NNW,
			[VGamepadButtonImage.BlueArrNW] = Properties.Resources.NW,
			[VGamepadButtonImage.BlueArrS] = Properties.Resources.BlueDown,
			[VGamepadButtonImage.BlueArrSE] = Properties.Resources.SE,
			[VGamepadButtonImage.BlueArrSSE] = Properties.Resources.SSE,
			[VGamepadButtonImage.BlueArrSSW] = Properties.Resources.SSW,
			[VGamepadButtonImage.BlueArrSW] = Properties.Resources.SW,
			[VGamepadButtonImage.BlueArrW] = Properties.Resources.Back,
			[VGamepadButtonImage.BlueArrWNW] = Properties.Resources.WNW,
			[VGamepadButtonImage.BlueArrWSW] = Properties.Resources.WSW,
			[VGamepadButtonImage.C64Symbol] = Properties.Resources.C64Symbol,
			[VGamepadButtonImage.Circle] = Properties.Resources.Circle,
			[VGamepadButtonImage.Cross] = Properties.Resources.Cross,
			[VGamepadButtonImage.Play] = Properties.Resources.Play,
			[VGamepadButtonImage.SkipBack] = Properties.Resources.BackMore,
			[VGamepadButtonImage.Square] = Properties.Resources.Square,
			[VGamepadButtonImage.Stop] = Properties.Resources.Stop,
			[VGamepadButtonImage.Triangle] = Properties.Resources.Triangle,
			[VGamepadButtonImage.YellowArrE] = Properties.Resources.YellowRight,
			[VGamepadButtonImage.YellowArrN] = Properties.Resources.YellowUp,
			[VGamepadButtonImage.YellowArrS] = Properties.Resources.YellowDown,
			[VGamepadButtonImage.YellowArrW] = Properties.Resources.YellowLeft,
		};

		private readonly PadSchema _schema;
		private readonly InputManager _inputManager;
		private readonly EventHandler _setLastFocusedNUD;
		private bool _readOnly;

		public void UpdateValues()
		{
			PadControls.ForEach(c => c.UpdateValues());
		}

		private List<IVirtualPadControl> PadControls =>
			PadBox.Controls
				.OfType<IVirtualPadControl>()
				.ToList();

		public string PadSchemaDisplayName => _schema.DisplayName;

		public bool ReadOnly
		{
			get => _readOnly;

			set
			{
				_readOnly = value;
				PadControls.ForEach(c => c.ReadOnly = value);
			}
		}

		public VirtualPad(PadSchema schema, InputManager inputManager, EventHandler setLastFocusedNUD)
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			InitializeComponent();
			Dock = DockStyle.Top | DockStyle.Left;
			_schema = schema;
			_inputManager = inputManager;
			_setLastFocusedNUD = setLastFocusedNUD;
		}

		private void VirtualPadControl_Load(object sender, EventArgs e)
		{
			static VirtualPadButton GenVirtualPadButton(InputManager inputManager, ButtonSchema button)
			{
				var icon = button.Icon == null ? null : _buttonImages[button.Icon.Value];
				var buttonControl = new VirtualPadButton
				{
					InputManager = inputManager,
					Name = button.Name,
					Text = icon != null ? null : button.DisplayName,
					Location = UIHelper.Scale(button.Location),
					Image = icon
				};
				if (icon != null && UIHelper.AutoScaleFactorX > 1F && UIHelper.AutoScaleFactorY > 1F)
				{
					// When scaling up, unfortunately the icon will look too small, but at least we can make the rest of the button bigger
					buttonControl.AutoSize = false;
					buttonControl.Size = UIHelper.Scale(icon.Size) + new Size(6, 6);
				}
				return buttonControl;
			}

			Size = UIHelper.Scale(_schema.Size);
			MaximumSize = UIHelper.Scale(_schema.Size);
			PadBox.Text = _schema.DisplayName;

			if (_schema.IsConsole)
			{
				PadBox.ForeColor = SystemColors.HotTrack;
			}

			foreach (var controlSchema in _schema.Buttons)
			{
				PadBox.Controls.Add(controlSchema switch
				{
					ButtonSchema button => GenVirtualPadButton(_inputManager, button),
					SingleAxisSchema singleAxis => new VirtualPadAnalogButton(
						_inputManager.StickyHoldController,
						singleAxis.Name,
						singleAxis.DisplayName,
						singleAxis.MinValue,
						singleAxis.MaxValue,
						singleAxis.IsVertical ? Orientation.Vertical : Orientation.Horizontal
					)
					{
						Location = UIHelper.Scale(singleAxis.Location),
						Size = UIHelper.Scale(singleAxis.TargetSize)
					},
					AnalogSchema analog => new VirtualPadAnalogStick(
						_inputManager.StickyHoldController,
						_setLastFocusedNUD,
						analog.Name,
						analog.SecondaryName,
						analog.Spec,
						analog.SecondarySpec
					)
					{
						Location = UIHelper.Scale(analog.Location),
						Size = UIHelper.Scale(new Size(180 + 79, 200 + 9))
					},
					TargetedPairSchema targetedPair => new VirtualPadTargetScreen(
						_inputManager.StickyHoldController,
						_setLastFocusedNUD,
						targetedPair.Name,
						targetedPair.SecondaryName,
						targetedPair.MaxX,
						targetedPair.MaxY
					)
					{
						Location = UIHelper.Scale(targetedPair.Location),
						TargetSize = targetedPair.TargetSize,
					},
					DiscManagerSchema discManager => new VirtualPadDiscManager(
						_inputManager,
						discManager.OwnerEmulator,
						discManager.Name,
						discManager.SecondaryNames
					)
					{
						Location = UIHelper.Scale(discManager.Location),
						Size = UIHelper.Scale(discManager.TargetSize)
					},
					_ => throw new InvalidOperationException()
				});
			}
			if (OSTailoredCode.IsUnixHost)
			{
				PadBox.PerformLayout();
				foreach (var c in PadBox.Controls().Where(static c => c is CheckBox { Image: null }))
				{
					var size = c.Size;
					size.Width -= UIHelper.ScaleX(8);
					c.AutoSize = false;
					c.Size = size;
				}
			}
		}

		public void Clear()
		{
			PadControls.ForEach(p => p.Clear());
		}

		public void ClearBoolean()
		{
			foreach (var p in PadControls.OfType<VirtualPadButton>())
			{
				p.Clear();
			}
		}

		public void Set(IController controller)
		{
			PadControls.ForEach(c => c.Set(controller));
		}

		public void SetPrevious(IController previous)
		{
			foreach (var c in PadControls.OfType<VirtualPadAnalogStick>())
			{
				c.SetPrevious(previous);
			}
		}

		public void BumpAnalog(int? x, int? y)
		{
			foreach (var a in PadControls.OfType<VirtualPadAnalogStick>())
			{
				a.Bump(x, y);
			}

			foreach (var a in PadControls.OfType<VirtualPadAnalogButton>())
			{
				a.Bump(x);
			}

			foreach (var a in PadControls.OfType<VirtualPadTargetScreen>())
			{
				a.Bump(x, y);
			}
		}
	}
}
