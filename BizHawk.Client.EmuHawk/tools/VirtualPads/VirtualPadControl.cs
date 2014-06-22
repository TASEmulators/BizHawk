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
	public partial class VirtualPadControl : UserControl, IVirtualPad
	{
		private PadSchema _schema;

		public VirtualPadControl(PadSchema schema)
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
						var checkbox = new CheckBox
						{
							Appearance = Appearance.Button,
							AutoSize = true,
							Location = button.Location,
							ForeColor = _schema.IsConsole ? Color.Red : SystemColors.ControlText,
							Name = button.Name,
							Text = button.DisplayName,
							Image = button.Icon
						};

						checkbox.CheckedChanged += Boolean_CheckedChanged;

						Controls.Add(checkbox);
						break;
					case PadSchema.PadInputType.FloatPair:
						break;
				}
			}
		}

		public void Clear()
		{
			Controls
				.OfType<CheckBox>()
				.ToList()
				.ForEach(c => {
					c.Checked = false;
					Global.StickyXORAdapter.SetSticky(c.Name, false);
				});
		}

		public IController Get()
		{
			return Global.MovieSession.MovieControllerInstance();
		}

		public void Set(IController controller)
		{
			
		}

		private void Boolean_CheckedChanged(object sender, EventArgs e)
		{
			var cbox = sender as CheckBox;
			Global.StickyXORAdapter.SetSticky(cbox.Name, cbox.Checked);
		}
	}
}
