using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPad : UserControl
	{
		private readonly PadSchema _schema;
		private bool _readOnly;

		public void UpdateValues()
		{
			PadControls.ForEach(c => c.UpdateValues());
		}

		private List<IVirtualPadControl> PadControls
		{
			get
			{
				return PadBox.Controls
					.OfType<IVirtualPadControl>()
					.ToList();
			}
		}

		public string PadSchemaDisplayName { get { return _schema.DisplayName; } }

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
			Size = UIHelper.Scale(_schema.DefaultSize);
			MaximumSize = UIHelper.Scale(_schema.MaxSize ?? _schema.DefaultSize);
			PadBox.Text = _schema.DisplayName;
			foreach (var button in _schema.Buttons)
			{
				switch (button.Type)
				{
					case PadSchema.PadInputType.Boolean:
						var buttonControl = new VirtualPadButton
						{
							Name = button.Name,
							Text = button.DisplayName,
							Location = UIHelper.Scale(button.Location),
							Image = button.Icon,
						};
						if (button.Icon != null && UIHelper.AutoScaleFactorX > 1F && UIHelper.AutoScaleFactorY > 1F)
						{
							// When scaling up, unfortunately the icon will look too small, but at least we can make the rest of the button bigger
							buttonControl.AutoSize = false;
							buttonControl.Size = UIHelper.Scale(button.Icon.Size) + new Size(6, 6);
						}
						PadBox.Controls.Add(buttonControl);
						break;
					case PadSchema.PadInputType.AnalogStick:
						PadBox.Controls.Add(new VirtualPadAnalogStick
						{
							Name = button.Name,
							SecondaryName = (button.SecondaryNames != null && button.SecondaryNames.Any()) ? button.SecondaryNames[0] : "",
							Location = UIHelper.Scale(button.Location),
							Size = UIHelper.Scale(new Size(180 + 79, 200 + 9)),
							RangeX = new float[] { button.MinValue, button.MidValue, button.MaxValue },
							RangeY = new float[] { button.MinValueSec, button.MidValueSec, button.MaxValueSec }, 
						});
						break;
					case PadSchema.PadInputType.TargetedPair:
						PadBox.Controls.Add(new VirtualPadTargetScreen
						{
							Name = button.Name,
							Location = UIHelper.Scale(button.Location),
							TargetSize = button.TargetSize,
							XName = button.Name,
							YName = button.SecondaryNames[0],
							RangeX = button.MaxValue,
							RangeY = button.MaxValue // TODO: ability to have a different Y than X
						});
						break;
					case PadSchema.PadInputType.FloatSingle:
						PadBox.Controls.Add(new VirtualPadAnalogButton
						{
							Name = button.Name,
							DisplayName = button.DisplayName,
							Location = UIHelper.Scale(button.Location),
							Size = UIHelper.Scale(button.TargetSize),
							MinValue = button.MinValue,
							MaxValue = button.MaxValue,
							Orientation = button.Orientation
						});
						break;
					case PadSchema.PadInputType.DiscManager:
						PadBox.Controls.Add(new VirtualPadDiscManager(button.SecondaryNames)
						{
							Name = button.Name,
							//DisplayName = button.DisplayName,
							Location = UIHelper.Scale(button.Location),
							Size = UIHelper.Scale(button.TargetSize),
							OwnerEmulator = button.OwnerEmulator
						});
						break;
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
