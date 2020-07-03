using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using System.Drawing;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPad : UserControl
	{
		private readonly PadSchema _schema;
		private readonly InputManager _inputManager;
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

		public VirtualPad(PadSchema schema, InputManager inputManager)
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			InitializeComponent();
			Dock = DockStyle.Top | DockStyle.Left;
			_schema = schema;
			_inputManager = inputManager;
		}

		private void VirtualPadControl_Load(object sender, EventArgs e)
		{
			static VirtualPadButton GenVirtualPadButton(ButtonSchema button)
			{
				var buttonControl = new VirtualPadButton
				{
					Name = button.Name,
					Text = button.Icon != null ? null : button.DisplayName,
					Location = UIHelper.Scale(button.Location),
					Image = button.Icon
				};
				if (button.Icon != null && UIHelper.AutoScaleFactorX > 1F && UIHelper.AutoScaleFactorY > 1F)
				{
					// When scaling up, unfortunately the icon will look too small, but at least we can make the rest of the button bigger
					buttonControl.AutoSize = false;
					buttonControl.Size = UIHelper.Scale(button.Icon.Size) + new Size(6, 6);
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
					ButtonSchema button => GenVirtualPadButton(button),
					SingleAxisSchema singleAxis => new VirtualPadAnalogButton(_inputManager.StickyXorAdapter)
					{
						Name = singleAxis.Name,
						DisplayName = singleAxis.DisplayName,
						Location = UIHelper.Scale(singleAxis.Location),
						Size = UIHelper.Scale(singleAxis.TargetSize),
						MinValue = singleAxis.MinValue,
						MaxValue = singleAxis.MaxValue,
						Orientation = singleAxis.Orientation
					},
					AnalogSchema analog => new VirtualPadAnalogStick(_inputManager)
					{
						Name = analog.Name,
						SecondaryName = analog.SecondaryName,
						Location = UIHelper.Scale(analog.Location),
						Size = UIHelper.Scale(new Size(180 + 79, 200 + 9)),
						RangeX = analog.Spec,
						RangeY = analog.SecondarySpec
					},
					TargetedPairSchema targetedPair => new VirtualPadTargetScreen(_inputManager.StickyXorAdapter)
					{
						Name = targetedPair.Name,
						Location = UIHelper.Scale(targetedPair.Location),
						TargetSize = targetedPair.TargetSize,
						XName = targetedPair.Name,
						YName = targetedPair.SecondaryName,
						RangeX = targetedPair.MaxValue,
						RangeY = targetedPair.MaxValue // TODO split into MaxX and MaxY, and rename VirtualPadTargetScreen.RangeX/RangeY
					},
					DiscManagerSchema discManager => new VirtualPadDiscManager(_inputManager.StickyXorAdapter, discManager.SecondaryNames) {
						Name = discManager.Name,
						Location = UIHelper.Scale(discManager.Location),
						Size = UIHelper.Scale(discManager.TargetSize),
						OwnerEmulator = discManager.OwnerEmulator
					},
					_ => throw new InvalidOperationException()
				});
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
