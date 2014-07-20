using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPad : UserControl
	{
		private readonly PadSchema _schema;
		private bool _readOnly;

		private List<IVirtualPadControl> PadControls
		{
			get
			{
				return PadBox.Controls
					.OfType<IVirtualPadControl>()
					.ToList();
			}
		}

		public bool ReadOnly
		{
			get
			{
				return _readOnly;
			}

			set
			{
				_readOnly = value;
				PadControls.ForEach(c => c.ReadOnly = value);
			}
		}

		public VirtualPad(PadSchema schema)
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			InitializeComponent();
			Dock = DockStyle.Top | DockStyle.Left;
			_schema = schema;
		}

		private void VirtualPadControl_Load(object sender, EventArgs e)
		{
			Size = _schema.DefaultSize;
			MaximumSize = _schema.MaxSize ?? _schema.DefaultSize;
			PadBox.Text = _schema.DisplayName;
			foreach (var button in _schema.Buttons)
			{
				switch (button.Type)
				{
					case PadSchema.PadInputType.Boolean:
						PadBox.Controls.Add(new VirtualPadButton
						{
							Name = button.Name,
							Text = button.DisplayName,
							Location = button.Location,
							Image = button.Icon,
						});
						break;
					case PadSchema.PadInputType.AnalogStick:
						PadBox.Controls.Add(new VirtualPadAnalogStick
						{
							Name = button.Name,
							Location = button.Location,
							Size = new Size(button.MaxValue + 79, button.MaxValue + 9), // TODO: don't use hardcoded values here, at least make them defaults in the AnalogStick object itself
							RangeX = button.MaxValue,
							RangeY = button.MaxValue // TODO ability to pass in a different Y max
						});
						break;
					case PadSchema.PadInputType.TargetedPair:
						PadBox.Controls.Add(new VirtualPadTargetScreen
						{
							Name = button.Name,
							Location = button.Location,
							XName = button.Name,
							YName = button.SecondaryNames[0],
							Size = button.TargetSize,
							RangeX = button.MaxValue,
							RangeY = button.MaxValue // TODO: ability to have a different Y than X
						});
						break;
					case PadSchema.PadInputType.FloatSingle:
						PadBox.Controls.Add(new VirtualPadAnalogButton
						{
							Name = button.Name,
							DisplayName = button.DisplayName,
							Location = button.Location,
							Size = button.TargetSize,
							MaxValue = button.MaxValue
						});
						break;
				}
			}
		}

		public void Clear()
		{
			PadControls.ForEach(p => p.Clear());
		}

		public void Set(IController controller)
		{
			PadControls.ForEach(c => c.Set(controller));
		}

		public void SetPrevious(IController previous)
		{
			PadControls
				.OfType<VirtualPadAnalogStick>()
				.ToList()
				.ForEach(c => c.SetPrevious(previous));
		}

		public void BumpAnalog(int? x, int? y)
		{
			PadControls
				.OfType<VirtualPadAnalogStick>()
				.ToList()
				.ForEach(a => a.Bump(x, y));

			PadControls
				.OfType<VirtualPadAnalogButton>()
				.ToList()
				.ForEach(a => a.Bump(x));

			PadControls
				.OfType<VirtualPadTargetScreen>()
				.ToList()
				.ForEach(a => a.Bump(x, y));
		}
	}
}
