using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadAnalogStick : UserControl, IVirtualPadControl
	{
		private bool _programmaticallyUpdatingNumerics = false;

		public VirtualPadAnalogStick()
		{
			InitializeComponent();
		}

		private void VirtualPadAnalogStick_Load(object sender, EventArgs e)
		{
			AnalogStick.Name = Name;
			AnalogStick.Name = Name.Replace("X", "Y"); // TODO: allow schema to dictate this but this is a convenient default
			MaxXNumeric.Value = 127;
			MaxYNumeric.Value = 127; // Note: these trigger change events that change the analog stick too
		}

		public void Set(IController controller)
		{
		}

		public void Clear()
		{
			AnalogStick.Clear();
			ManualX.Value = 0;
			ManualY.Value = 0;
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
			_programmaticallyUpdatingNumerics = true;
			SetNumericsFromAnalog();
			_programmaticallyUpdatingNumerics = false;
		}

		private void AnalogStick_MouseMove(object sender, MouseEventArgs e)
		{
			_programmaticallyUpdatingNumerics = true;
			SetNumericsFromAnalog();
			_programmaticallyUpdatingNumerics = false;
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
