#nullable enable

using System;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadAnalogStick : UserControl, IVirtualPadControl
	{
		/// <summary>for coordinate transformation when non orthogonal (like PSX for example)</summary>
		private int rangeAverageX;

		/// <inheritdoc cref="rangeAverageX"/>
		private int rangeAverageY;

		private Range<int> _rangeX = AnalogStickPanel.DefaultRange;

		private Range<int> _rangeY = AnalogStickPanel.DefaultRange;

		private bool _readonly;

		private bool _reverseX;

		private bool _reverseY;

		private bool _updatingFromAnalog;

		private bool _updatingFromPolar;

		private bool _updatingFromXY;

		public VirtualPadAnalogStick()
		{
			InitializeComponent();
			AnalogStick.ClearCallback = ClearCallback;

			ManualX.ValueChanged += ManualXY_ValueChanged;
			ManualY.ValueChanged += ManualXY_ValueChanged;
			manualR.ValueChanged += PolarNumeric_Changed;
			manualTheta.ValueChanged += PolarNumeric_Changed;
		}

		public float[] RangeX
		{
			set
			{
				if (value.Length != 3) throw new ArgumentException("must be float[3]", nameof(value));
#if false
				_midX = (int) value[1];
#endif
				_reverseX = value[2] < value[0];
				_rangeX = _reverseX ? ((int) value[2]).RangeTo((int) value[0]) : ((int) value[0]).RangeTo((int) value[2]);
			}
		}

		public float[] RangeY
		{
			set
			{
				if (value.Length != 3) throw new ArgumentException("must be float[3]", nameof(value));
#if false
				_midY = (int) value[1];
#endif
				_reverseY = value[2] < value[0];
				_rangeY = _reverseY ? ((int) value[2]).RangeTo((int) value[0]) : ((int) value[0]).RangeTo((int) value[2]);
			}
		}

		public string? SecondaryName { get; set; }

		private void VirtualPadAnalogStick_Load(object sender, EventArgs e)
		{
			AnalogStick.XName = AnalogStick.Name = Name;
			AnalogStick.SetRangeX(_rangeX);
			AnalogStick.YName = !string.IsNullOrEmpty(SecondaryName) ? SecondaryName : Name.Replace("X", "Y");
			AnalogStick.SetRangeY(_rangeY);

			ManualX.Minimum = _rangeX.Start;
			ManualX.Maximum = _rangeX.EndInclusive;
			ManualY.Minimum = _rangeY.Start;
			ManualY.Maximum = _rangeY.EndInclusive;
			MaxXNumeric.Minimum = 1;
			MaxXNumeric.Maximum = 100;
			MaxYNumeric.Minimum = 1;
			MaxYNumeric.Maximum = 100;

			// these trigger Change events that set the analog stick's values too
			MaxXNumeric.Value = 100;
			MaxYNumeric.Value = 100;

			rangeAverageX = (_rangeX.Start + _rangeX.EndInclusive) / 2;
			rangeAverageY = (_rangeY.Start + _rangeY.EndInclusive) / 2;
		}

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

		public void Clear() => AnalogStick.Clear();

		public bool ReadOnly
		{
			get => _readonly;
			set
			{
				if (_readonly == value) return;
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

		public void SetPrevious(IController previous) => AnalogStick.SetPrevious(previous);

		private void ManualXY_ValueChanged(object sender, EventArgs e)
		{
			if (_updatingFromAnalog || _updatingFromPolar) return;
			_updatingFromXY = true;

			var x = (sbyte) ManualX.Value;
			var y = (sbyte) ManualY.Value;
			var (r, θ) = PolarRectConversion.RectToPolarLookup(x, y);
			SetAnalog(x, y);
			SetPolar(r, θ);

			_updatingFromXY = false;
		}

		private void MaxManualXY_ValueChanged(object sender, EventArgs e)
			=> AnalogStick.SetUserRange(_reverseX ? -MaxXNumeric.Value : MaxXNumeric.Value, _reverseY ? -MaxYNumeric.Value : MaxYNumeric.Value);

		private void PolarNumeric_Changed(object sender, EventArgs e)
		{
			if (_updatingFromAnalog || _updatingFromXY) return;
			_updatingFromPolar = true;

			var (x, y) = PolarRectConversion.PolarToRectLookup((ushort) manualR.Value, (ushort) manualTheta.Value);
			var x1 = (rangeAverageX + x).ConstrainWithin(_rangeX);
			var y1 = (rangeAverageY + y).ConstrainWithin(_rangeY);
			SetAnalog(x1, y1);
			SetXY(x1, y1);

			_updatingFromPolar = false;
		}

		private void SetAnalog(int x, int y)
		{
			AnalogStick.X = x;
			AnalogStick.Y = y;
			AnalogStick.HasValue = true;
			AnalogStick.Refresh();
		}

		/// <remarks>setting <see cref="NumericUpDown.Value"/> causes a draw, so we avoid it unless necessary</remarks>
		private void SetPolar(decimal r, decimal θ)
		{
			if (manualR.Value != r) manualR.Value = r;
			if (manualTheta.Value != θ) manualTheta.Value = θ;
		}

		/// <inheritdoc cref="SetPolar"/>
		private void SetXY(decimal x, decimal y)
		{
			if (ManualX.Value != x) ManualX.Value = x;
			if (ManualY.Value != y) ManualY.Value = y;
		}

		private void SetNumericsFromAnalog()
		{
			_updatingFromAnalog = true;

			if (AnalogStick.HasValue)
			{
				var x = AnalogStick.X - rangeAverageX;
				var y = AnalogStick.Y - rangeAverageY;
				var (r, θ) = PolarRectConversion.RectToPolarLookup((sbyte) x, (sbyte) y);
				SetPolar(r, θ);
				SetXY(x, y);
			}
			else
			{
				SetPolar(0, 0);
				SetXY(0, 0);
			}

			_updatingFromAnalog = false;
		}

		private void AnalogStick_MouseDown(object sender, MouseEventArgs e)
		{
			if (!ReadOnly) SetNumericsFromAnalog();
		}

		private void AnalogStick_MouseMove(object sender, MouseEventArgs e)
		{
			if (!ReadOnly) SetNumericsFromAnalog();
		}
	}
}
