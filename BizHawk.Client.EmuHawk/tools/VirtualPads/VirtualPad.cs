using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPad : UserControl
	{
		private PadSchema _schema;

		private List<IVirtualPadControl> Pads
		{
			get
			{
				return Controls
					.OfType<IVirtualPadControl>()
					.ToList();
			}
		}

		public VirtualPad(PadSchema schema)
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			BorderStyle = BorderStyle.Fixed3D;
			InitializeComponent();

			_schema = schema;
		}

		private void VirtualPadControl_Load(object sender, EventArgs e)
		{
			Size = _schema.DefaultSize;

			foreach (var button in _schema.Buttons)
			{
				switch (button.Type)
				{
					case PadSchema.PadInputType.Boolean:
						Controls.Add(new VirtualPadButton
						{
							Name = button.Name,
							Text = button.DisplayName,
							Location = button.Location,
							Image = button.Icon
						});
						break;
					case PadSchema.PadInputType.AnalogStick:
						Controls.Add(new VirtualPadAnalogStick
						{
							Name = button.Name,
							Location = button.Location
						});
						break;
					case PadSchema.PadInputType.TargetedPair:
						Controls.Add(new VirtualPadTargetScreen
						{
							Name = button.Name,
							Location = button.Location,
							XName = button.Name,
							YName = button.SecondaryNames[0],
							FireButton = button.SecondaryNames[1],
							Size = button.TargetSize
						});
						break;
					case PadSchema.PadInputType.FloatSingle:
						Controls.Add(new VirtualPadAnalogButton
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
			Pads.ForEach(c => c.Clear());
		}

		public void Set(IController controller)
		{
			Pads.ForEach(c => c.Set(controller));
		}
	}
}
