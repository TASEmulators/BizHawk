using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using System.Windows;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadAnalogStick : UserControl, IVirtualPadControl
	{
		#region Fields

		private bool _programmaticallyUpdatingNumerics;
		private bool _readonly;
		private int rangeAverageX; //for coordinate transformation when non orthogonal (like PSX for example)
		private int rangeAverageY;

		private EventHandler manualXYValueChangedEventHandler;
		private EventHandler polarNumericChangedEventHandler;

		#endregion

		public VirtualPadAnalogStick()
		{
			InitializeComponent();
			AnalogStick.ClearCallback = ClearCallback;

			manualXYValueChangedEventHandler = new EventHandler(ManualXY_ValueChanged);
			polarNumericChangedEventHandler = new EventHandler(PolarNumeric_Changed);

			ManualX.ValueChanged += manualXYValueChangedEventHandler;
			ManualY.ValueChanged += manualXYValueChangedEventHandler;
			manualR.ValueChanged += polarNumericChangedEventHandler;
			manualTheta.ValueChanged += polarNumericChangedEventHandler;
		}

		public float[] RangeX = new float[] { -128f, 0.0f, 127f };
		public float[] RangeY = new float[] { -128f, 0.0f, 127f };


		private void VirtualPadAnalogStick_Load(object sender, EventArgs e)
		{
			AnalogStick.Name = Name;
			AnalogStick.XName = Name;
			AnalogStick.YName = Name.Replace("X", "Y"); // TODO: allow schema to dictate this but this is a convenient default
			AnalogStick.SetRangeX(RangeX);
			AnalogStick.SetRangeY(RangeY);

			ManualX.Minimum = (decimal)RangeX[0];
			ManualX.Maximum = (decimal)RangeX[2];

			ManualY.Minimum = (decimal)RangeX[0];
			ManualY.Maximum = (decimal)RangeX[2];

			MaxXNumeric.Minimum = 1;
			MaxXNumeric.Maximum = 100;
			MaxXNumeric.Value = 100;

			MaxYNumeric.Minimum = 1;
			MaxYNumeric.Maximum = 100;
			MaxYNumeric.Value = 100; // Note: these trigger change events that change the analog stick too

			rangeAverageX = (int)((RangeX[0] + RangeX[2]) / 2);
			rangeAverageY = (int)((RangeY[0] + RangeY[2]) / 2);
		}

		#region IVirtualPadControl Implementation

		public void UpdateValues()
		{
			// Nothing to do
			// This tool already draws as necessary
		}

		public void Set(IController controller)
		{
			AnalogStick.Set(controller);
			SetNumericsFromAnalog();
		}

		public void ClearCallback()
		{
			ManualX.Value = 0;
			ManualY.Value = 0;
			manualR.Value = 0;
			manualTheta.Value = 0;
			//see HOOMOO
			Global.AutofireStickyXORAdapter.SetSticky(AnalogStick.XName, false);
			Global.StickyXORAdapter.Unset(AnalogStick.XName);
			Global.AutofireStickyXORAdapter.SetSticky(AnalogStick.YName, false);
			Global.StickyXORAdapter.Unset(AnalogStick.YName);
			AnalogStick.HasValue = false;
		}

		public void Clear()
		{
			AnalogStick.Clear();
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
						manualR.Enabled =
						rLabel.Enabled =
						manualTheta.Enabled =
						thetaLabel.Enabled =
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
		}

		private void ManualXY_ValueChanged(object sender, EventArgs e)
		{
			SetAnalogControlFromNumerics();
		}
		private void MaxManualXY_ValueChanged(object sender, EventArgs e)
		{
			SetAnalogMaxFromNumerics();
		}

		private void PolarNumeric_Changed(object sender, EventArgs e)
		{
			ManualX.ValueChanged -= manualXYValueChangedEventHandler;
			ManualY.ValueChanged -= manualXYValueChangedEventHandler;

			ManualX.Value = Math.Ceiling(manualR.Value * (decimal)Math.Cos(Math.PI * (double)manualTheta.Value / 180)).Clamp(-127, 127) + rangeAverageX;
			ManualY.Value = Math.Ceiling(manualR.Value * (decimal)Math.Sin(Math.PI * (double)manualTheta.Value / 180)).Clamp(-127, 127) + rangeAverageY;

			AnalogStick.X = (int)ManualX.Value;
			AnalogStick.Y = (int)ManualY.Value;

			AnalogStick.HasValue = true;
			AnalogStick.Refresh();

			ManualX.ValueChanged += manualXYValueChangedEventHandler;
			ManualY.ValueChanged += manualXYValueChangedEventHandler;
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
			_programmaticallyUpdatingNumerics = true;

			if (AnalogStick.HasValue)
			{
				// Setting .Value of a numeric causes a draw, so avoid it unless necessary
				if (ManualX.Value != AnalogStick.X)
				{
					ManualX.Value = AnalogStick.X;
				}

				if (ManualY.Value != AnalogStick.Y)
				{
					ManualY.Value = AnalogStick.Y;
				}
			}
			else
			{
				if (ManualX.Value != 0)
				{
					ManualX.Value = 0;
				}

				if (ManualY.Value != 0)
				{
					ManualY.Value = 0;
				}
			}

			manualR.ValueChanged -= polarNumericChangedEventHandler;
			manualTheta.ValueChanged -= polarNumericChangedEventHandler;

			manualR.Value = (decimal)Math.Sqrt(Math.Pow(AnalogStick.X - rangeAverageX, 2) + Math.Pow(AnalogStick.Y - rangeAverageY, 2));
			manualTheta.Value = (decimal)(Math.Atan2(AnalogStick.Y - rangeAverageY, AnalogStick.X - rangeAverageX) * (180 / Math.PI));

			manualR.ValueChanged += polarNumericChangedEventHandler;
			manualTheta.ValueChanged += polarNumericChangedEventHandler;

			_programmaticallyUpdatingNumerics = false;
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

		private void SetAnalogMaxFromNumerics()
		{
			if (!_programmaticallyUpdatingNumerics)
			{
				//blehh,... this damn feature
				AnalogStick.SetUserRange((float)MaxXNumeric.Value, (float)MaxYNumeric.Value);
			}
		}
	}
}
