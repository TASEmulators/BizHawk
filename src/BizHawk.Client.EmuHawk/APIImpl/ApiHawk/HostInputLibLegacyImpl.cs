#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.API.ApiHawk;
using BizHawk.API.Base;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk.APIImpl.ApiHawk
{
	internal sealed class HostInputLibLegacyImpl : LibBase<GlobalsAccessAPIEnvironment>, IHostInputLib, IInput
	{
		public HostInputLibLegacyImpl(out Action<GlobalsAccessAPIEnvironment> updateEnv) : base(out updateEnv) {}

		public IReadOnlyDictionary<string, bool> Get() => ((IInput) this).Get();

		[LegacyApiHawk]
		Dictionary<string, bool> IInput.Get() => Env.GlobalInputManager.ControllerInputCoalescer.BoolButtons()
			.Where(kvp => kvp.Value)
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

		public IReadOnlyDictionary<string, object> GetMouse() => ((IInput) this).GetMouse();

		[LegacyApiHawk]
		Dictionary<string, object> IInput.GetMouse()
		{
			var buttons = new Dictionary<string, object>();
			// TODO - need to specify whether in "emu" or "native" coordinate space.
			var p = Env.GlobalDisplayManager.UntransformPoint(Control.MousePosition);
			buttons["X"] = p.X;
			buttons["Y"] = p.Y;
			buttons[MouseButtons.Left.ToString()] = (Control.MouseButtons & MouseButtons.Left) != 0;
			buttons[MouseButtons.Middle.ToString()] = (Control.MouseButtons & MouseButtons.Middle) != 0;
			buttons[MouseButtons.Right.ToString()] = (Control.MouseButtons & MouseButtons.Right) != 0;
			buttons[MouseButtons.XButton1.ToString()] = (Control.MouseButtons & MouseButtons.XButton1) != 0;
			buttons[MouseButtons.XButton2.ToString()] = (Control.MouseButtons & MouseButtons.XButton2) != 0;
			buttons["Wheel"] = Env.GlobalMainForm.MouseWheelTracker;
			return buttons;
		}
	}
}
