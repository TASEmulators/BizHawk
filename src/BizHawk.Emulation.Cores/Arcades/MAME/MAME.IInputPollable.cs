using System;
using System.Collections.Generic;
using System.Linq;

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

		private string[] _buttonFields;
		private string[] _analogFields;
		private IntPtr[] _fieldPtrs;
		private int[] _fieldInputs;

		private void GetInputFields()
		{
			string[] buttonFields = MameGetString(MAMELuaCommand.GetButtonFields).Split(';');
			string[] analogFields = MameGetString(MAMELuaCommand.GetAnalogFields).Split(';');

			List<string> buttonFieldList = new List<string>();
			List<string> analogFieldList = new List<string>();
			List<IntPtr> fieldPtrList = new List<IntPtr>();

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

			foreach (string buttonField in buttonFields)
			{
				if (buttonField != string.Empty && !buttonField.Contains('%'))
				{
					string tag = buttonField.SubstringBefore(',');
					string field = buttonField.SubstringAfterLast(',');
					buttonFieldList.Add(field);
					AddFieldPtr(tag, field);
					ControllerDefinition.BoolButtons.Add(field);
				}
			}

			foreach (string analogField in analogFields)
			{
				if (analogField != string.Empty && !analogField.Contains('%'))
				{
					string[] keys = analogField.Split(',');
					string tag = keys[0];
					string field = keys[1];
					analogFieldList.Add(field);
					AddFieldPtr(tag, field);
					int def = int.Parse(keys[2]);
					int min = int.Parse(keys[3]);
					int max = int.Parse(keys[4]);
					ControllerDefinition.AddAxis(field, min.RangeTo(max), def);
				}
			}

			_buttonFields = buttonFieldList.ToArray();
			_analogFields = analogFieldList.ToArray();
			_fieldPtrs = fieldPtrList.ToArray();
			_fieldInputs = new int[_fieldPtrs.Length];

			ControllerDefinition.MakeImmutable();
		}

		private void SendInput(IController controller)
		{
			for (int i = 0; i < _buttonFields.Length; i++)
			{
				_fieldInputs[i] = controller.IsPressed(_buttonFields[i]) ? 1 : 0;
			}

			for (int i = 0; i < _analogFields.Length; i++)
			{
				_fieldInputs[_buttonFields.Length + i] = controller.AxisValue(_analogFields[i]);
			}

			_core.mame_input_set_fields(_fieldPtrs, _fieldInputs, _fieldInputs.Length);

			_core.mame_set_input_poll_callback(InputCallbacks.Count > 0 ? _inputPollCallback : null);
		}
	}
}