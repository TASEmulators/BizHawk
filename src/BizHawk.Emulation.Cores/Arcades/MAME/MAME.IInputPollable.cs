using System;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : IInputPollable
	{
		public int LagCount { get; set; } = 0;
		public bool IsLagFrame { get; set; } = false;

		[FeatureNotImplemented]
		public IInputCallbackSystem InputCallbacks => throw new NotImplementedException();

		private readonly ControllerDefinition MAMEController = new("MAME Controller");

		private readonly SortedDictionary<string, string> _fieldsPorts = new();
		private readonly SortedDictionary<string, string> _fieldsAnalog = new();
		private readonly SortedDictionary<string, string> _romHashes = new();

		private void GetInputFields()
		{
			var buttonFields = MameGetString(MAMELuaCommand.GetButtonFields).Split(';');
			var analogFields = MameGetString(MAMELuaCommand.GetAnalogFields).Split(';');

			foreach (var buttonField in buttonFields)
			{
				if (buttonField != string.Empty)
				{
					var tag = buttonField.SubstringBefore(',');
					var field = buttonField.SubstringAfterLast(',');
					_fieldsPorts.Add(field, tag);
					MAMEController.BoolButtons.Add(field);
				}
			}

			foreach (var analogField in analogFields)
			{
				if (analogField != string.Empty)
				{
					var keys = analogField.Split(',');
					var tag = keys[0];
					var field = keys[1];
					_fieldsAnalog.Add(field, tag);
					var def = int.Parse(keys[2]);
					var min = int.Parse(keys[3]);
					var max = int.Parse(keys[4]);
					MAMEController.AddAxis(field, min.RangeTo(max), def);
				}
			}

			MAMEController.MakeImmutable();
		}

		private void SendInput(IController controller)
		{
			foreach (var fieldPort in _fieldsPorts)
			{
				_core.mame_lua_execute(
					"manager.machine.ioport" +
					$".ports  [\"{ fieldPort.Value }\"]" +
					$".fields [\"{ fieldPort.Key   }\"]" +
					$":set_value({ (controller.IsPressed(fieldPort.Key) ? 1 : 0) })");
			}

			foreach (var fieldAnalog in _fieldsAnalog)
			{
				_core.mame_lua_execute(
					"manager.machine.ioport" +
					$".ports  [\"{fieldAnalog.Value}\"]" +
					$".fields [\"{fieldAnalog.Key}\"]" +
					$":set_value({controller.AxisValue(fieldAnalog.Key)})");
			}
		}
	}
}