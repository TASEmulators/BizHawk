using System;
using System.Collections.Generic;

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
		private readonly SortedDictionary<string, string> _romHashes = new();

		private void GetInputFields()
		{
			string inputFields = MameGetString(MAMELuaCommand.GetInputFields);
			string[] portFields = inputFields.Split(';');
			MAMEController.BoolButtons.Clear();
			_fieldsPorts.Clear();

			foreach (string portField in portFields)
			{
				if (portField != string.Empty)
				{
					var tag = portField.SubstringBefore(',');
					var field = portField.SubstringAfterLast(',');
					_fieldsPorts.Add(field, tag);
					MAMEController.BoolButtons.Add(field);
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
		}
	}
}