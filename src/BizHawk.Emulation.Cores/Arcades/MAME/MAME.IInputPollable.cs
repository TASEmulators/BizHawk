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
		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		private readonly ControllerDefinition MAMEController = new("MAME Controller");

		private string[] _buttonFields;
		private string[] _analogFields;
		private IntPtr[] _fieldPtrs;
		private int[] _fieldInputs;

		private void GetInputFields()
		{
			var buttonFields = MameGetString(MAMELuaCommand.GetButtonFields).Split(';');
			var analogFields = MameGetString(MAMELuaCommand.GetAnalogFields).Split(';');

			var buttonFieldList = new List<string>();
			var analogFieldList = new List<string>();
			var fieldPtrList = new List<IntPtr>();

			void AddFieldPtr(string tag, string field)
			{
				var ptr = _core.mame_input_get_field_ptr(tag, field);

				if (ptr == IntPtr.Zero)
				{
					Dispose();
					throw new Exception($"Fatal error: {nameof(LibMAME.mame_input_get_field_ptr)} returned NULL!");
				}

				fieldPtrList.Add(ptr);
			}

			MAMEController.BoolButtons.Add("Reset");

			foreach (var buttonField in buttonFields)
			{
				if (buttonField.Length is not 0 && !buttonField.ContainsOrdinal('%'))
				{
					var tag = buttonField.SubstringBefore(',');
					var field = buttonField.SubstringAfterLast(',');
					var dupName = "";
					buttonFieldList.Add(field);
					AddFieldPtr(tag, field);

					if (MAMEController.BoolButtons.Contains(field))
					{
						dupName = $" [{ tag }]"; 
					}

					MAMEController.BoolButtons.Add(field + dupName);
				}
			}

			foreach (var analogField in analogFields)
			{
				if (analogField.Length is not 0 && !analogField.ContainsOrdinal('%'))
				{
					var keys = analogField.Split(',');
					var tag = keys[0];
					var field = keys[1];
					analogFieldList.Add(field);
					AddFieldPtr(tag, field);
					var def = int.Parse(keys[2]);
					var min = int.Parse(keys[3]);
					var max = int.Parse(keys[4]);
					MAMEController.AddAxis(field, min.RangeTo(max), def);
				}
			}

			_buttonFields = buttonFieldList.ToArray();
			_analogFields = analogFieldList.ToArray();
			_fieldPtrs = fieldPtrList.ToArray();
			_fieldInputs = new int[_fieldPtrs.Length];

			MAMEController.MakeImmutable();
		}

		private void SendInput(IController controller)
		{
			if (controller.IsPressed("Reset"))
			{
				_core.mame_lua_execute(MAMELuaCommand.Reset);
			}

			for (var i = 0; i < _buttonFields.Length; i++)
			{
				_fieldInputs[i] = controller.IsPressed(_buttonFields[i]) ? 1 : 0;
			}

			for (var i = 0; i < _analogFields.Length; i++)
			{
				_fieldInputs[_buttonFields.Length + i] = controller.AxisValue(_analogFields[i]);
			}

			_core.mame_input_set_fields(_fieldPtrs, _fieldInputs, _fieldInputs.Length);

			_core.mame_set_input_poll_callback(InputCallbacks.Count > 0 ? _inputPollCallback : null);
		}
	}
}