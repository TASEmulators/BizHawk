using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using System.Windows;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadAnalogStick : UserControl, IVirtualPadControl
	{
		private bool _programmaticallyUpdatingNumerics;
		private bool _readonly;

		public VirtualPadAnalogStick()
		{
			InitializeComponent();
			RangeX = 127;
			RangeY = 127;
		}

		public int RangeX { get; set; }
		public int RangeY { get; set; }

		private void VirtualPadAnalogStick_Load(object sender, EventArgs e)
		{
			this.Size = new System.Drawing.Size(204 + (RangeX - 127), 136 + (RangeY - 127));
			AnalogStick.Name = Name;
			AnalogStick.XName = Name;
			AnalogStick.YName = Name.Replace("X", "Y"); // TODO: allow schema to dictate this but this is a convenient default
			AnalogStick.MaxX = RangeX;
			AnalogStick.MaxY = RangeY;

			ManualX.Minimum = AnalogStick.MinX;
			ManualX.Maximum = AnalogStick.MaxX;

			ManualY.Minimum = AnalogStick.MinY;
			ManualY.Maximum = AnalogStick.MaxY;

			MaxXNumeric.Maximum = RangeX;
			MaxXNumeric.Value = RangeX;

			MaxYNumeric.Maximum = RangeY;
			MaxYNumeric.Value = RangeY; // Note: these trigger change events that change the analog stick too
		}

		#region IVirtualPadControl Implementation

		public void Set(IController controller)
		{
			AnalogStick.Set(controller);
			SetNumericsFromAnalog();
		}

		public void Clear()
		{
			AnalogStick.Clear();
			ManualX.Value = 0;
			ManualY.Value = 0;
		}

		public bool ReadOnly
		{
			get
			{
				return _readonly;
			}

			set
			{
				if (_readonly != value)
				{
					XLabel.Enabled =
						ManualX.Enabled =
						YLabel.Enabled =
						ManualY.Enabled =
						MaxLabel.Enabled =
						MaxXNumeric.Enabled =
						MaxYNumeric.Enabled =
						!value;

					AnalogStick.ReadOnly = 
						_readonly =
						value;
				
					Refresh();
				}
			}
		}

		#endregion

		public void Bump(int? x, int? y)
		{
			if (x.HasValue)
			{
				AnalogStick.HasValue = true;
				AnalogStick.X += x.Value;
			}

			if (y.HasValue)
			{
				AnalogStick.HasValue = true;
				AnalogStick.Y += y.Value;
			}

			SetNumericsFromAnalog();
		}

		public void SetPrevious(IController previous)
		{
			AnalogStick.SetPrevious(previous);
			Refresh();
		}

		private void ManualX_ValueChanged(object sender, EventArgs e)
		{
			SetAnalogControlFromNumerics();
		}

		private void ManualX_KeyUp(object sender, KeyEventArgs e)
		{
			SetAnalogControlFromNumerics();
		}

		private void ManualY_KeyUp(object sender, KeyEventArgs e)
		{
			SetAnalogControlFromNumerics();
		}

		private void ManualY_ValueChanged(object sender, EventArgs e)
		{
			SetAnalogControlFromNumerics();
		}

		private void SetAnalogControlFromNumerics()
		{
			if (!_programmaticallyUpdatingNumerics)
			{
				AnalogStick.X = (int)ManualX.Value;
				AnalogStick.Y = (int)ManualY.Value;
				AnalogStick.HasValue = true;
				AnalogStick.Refresh();
			}
		}

		private void SetNumericsFromAnalog()
		{
			if (AnalogStick.HasValue)
			{
				ManualX.Value = AnalogStick.X;
				ManualY.Value = AnalogStick.Y;
			}
		}

		private void AnalogStick_MouseDown(object sender, MouseEventArgs e)
		{
			if (!ReadOnly)
			{
				_programmaticallyUpdatingNumerics = true;
				SetNumericsFromAnalog();
				_programmaticallyUpdatingNumerics = false;
			}
		}

		private void AnalogStick_MouseMove(object sender, MouseEventArgs e)
		{
			if (!ReadOnly)
			{
				_programmaticallyUpdatingNumerics = true;
				SetNumericsFromAnalog();
				_programmaticallyUpdatingNumerics = false;
			}
		}

		private void MaxXNumeric_ValueChanged(object sender, EventArgs e)
		{
			SetAnalogMaxFromNumerics();
		}

		private void MaxXNumeric_KeyUp(object sender, KeyEventArgs e)
		{
			SetAnalogMaxFromNumerics();
		}

		private void MaxYNumeric_ValueChanged(object sender, EventArgs e)
		{
			SetAnalogMaxFromNumerics();
		}

		private void MaxYNumeric_KeyUp(object sender, KeyEventArgs e)
		{
			SetAnalogMaxFromNumerics();
		}

		private void SetAnalogMaxFromNumerics()
		{
			if (!_programmaticallyUpdatingNumerics)
			{
				AnalogStick.MaxX = (int)MaxXNumeric.Value;
				AnalogStick.MaxY = (int)MaxYNumeric.Value;
			}
		}
	}
}
