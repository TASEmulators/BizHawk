using System;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
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

		#endregion

		public VirtualPadAnalogStick()
		{
			InitializeComponent();
			AnalogStick.ClearCallback = ClearCallback;

			ManualX.ValueChanged += ManualXY_ValueChanged;
			ManualY.ValueChanged += ManualXY_ValueChanged;
			manualR.ValueChanged += PolarNumeric_Changed;
			manualTheta.ValueChanged += PolarNumeric_Changed;
		}

		public float[] RangeX = { -128f, 0.0f, 127f };
		public float[] RangeY = { -128f, 0.0f, 127f };
		private bool ReverseX;
		private bool ReverseY;

		public string SecondaryName { get; set; }


		private void VirtualPadAnalogStick_Load(object sender, EventArgs e)
		{
			AnalogStick.Name = Name;
			AnalogStick.XName = Name;
			AnalogStick.YName = !string.IsNullOrEmpty(SecondaryName)
				? SecondaryName
				: Name.Replace("X", "Y"); // Fallback
			if (RangeX[0] > RangeX[2])
			{
				RangeX = new[] { RangeX[2], RangeX[1], RangeX[0] };
				ReverseX = true;
			}
			AnalogStick.SetRangeX(RangeX);
			if (RangeY[0] > RangeY[2])
			{
				RangeY = new[] { RangeY[2], RangeY[1], RangeY[0] };
				ReverseY = true;
			}
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
			get => _readonly;

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
			ManualX.ValueChanged -= ManualXY_ValueChanged; //TODO is setting and checking a bool faster than subscription?
			ManualY.ValueChanged -= ManualXY_ValueChanged;

			var rect = PolarRectConversion.PolarDegToRect((double) manualR.Value, (double) manualTheta.Value);
			rect = new Tuple<double, double>(
				rangeAverageX + Math.Ceiling(rect.Item1).Clamp(-127, 127),
				rangeAverageY + Math.Ceiling(rect.Item2).Clamp(-127, 127));
			ManualX.Value = (decimal) rect.Item1;
			ManualY.Value = (decimal) rect.Item2;
			AnalogStick.X = (int) rect.Item1;
			AnalogStick.Y = (int) rect.Item2;
			AnalogStick.HasValue = true;
			AnalogStick.Refresh();

			ManualX.ValueChanged += ManualXY_ValueChanged;
			ManualY.ValueChanged += ManualXY_ValueChanged;
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

			manualR.ValueChanged -= PolarNumeric_Changed;
			manualTheta.ValueChanged -= PolarNumeric_Changed;

			var polar = PolarRectConversion.RectToPolarDeg(AnalogStick.X - rangeAverageX, AnalogStick.Y - rangeAverageY);
			manualR.Value = (decimal) polar.Item1;
			manualTheta.Value = (decimal) polar.Item2;

			manualR.ValueChanged += PolarNumeric_Changed;
			manualTheta.ValueChanged += PolarNumeric_Changed;

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
				AnalogStick.SetUserRange(ReverseX ? -MaxXNumeric.Value : MaxXNumeric.Value, ReverseY ? -MaxYNumeric.Value : MaxYNumeric.Value);
		}
	}
}
