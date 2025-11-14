using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
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
		private Dictionary<string, int> _analogMasks;
		private IntPtr[] _fieldPtrs;
		private int[] _fieldInputs;

		private void GetInputFields()
		{
			var buttonFields = MameGetString(MAMELuaCommand.GetButtonFields).Split(';');
			var analogFields = MameGetString(MAMELuaCommand.GetAnalogFields).Split(';');

			var buttonFieldList = new List<string>();
			var analogFieldList = new List<string>();
			var analogMaskDict = new Dictionary<string, int>();
			var fieldPtrList = new List<IntPtr>();
			var fieldsTagsList = new List<Tuple<string, string, AxisSpec?, int>>();

			void AddFieldPtr(
				string tag,
				string @field,
				AxisSpec? axis = null,
				int mask = 0
			)
			{
				var ptr = _core.mame_input_get_field_ptr(tag, @field);

				if (ptr == IntPtr.Zero)
				{
					Dispose();
					throw new Exception($"Fatal error: {nameof(LibMAME.mame_input_get_field_ptr)} returned NULL!");
				}

				fieldPtrList.Add(ptr);
				fieldsTagsList.Add(new Tuple<string, string, AxisSpec?, int>(@field, tag, axis, mask));
			}

			MAMEController.BoolButtons.Add("Reset");

			foreach (var buttonField in buttonFields)
			{
				if (buttonField.Length is not 0 && !buttonField.ContainsOrdinal('%'))
				{
					var tag = buttonField.SubstringBefore(',');
					var @field = buttonField.SubstringAfterLast(',');
					AddFieldPtr(tag, @field);
				}
			}

			foreach (var analogField in analogFields)
			{
				if (analogField.Length is not 0 && !analogField.ContainsOrdinal('%'))
				{
					var keys = analogField.Split(',');
					var tag = keys[0];
					var @field = keys[1];
					var def = int.Parse(keys[2]);
					var min = int.Parse(keys[3]);
					var max = int.Parse(keys[4]);
					var mask = int.Parse(keys[5]);

					// we expect the minimum to be actually be smaller than the maximum
					// however, mame sometimes has the minimum be "larger" as a way to indicate signed logic
					if (min > max)
					{
						if (def == 0)
						{
							min = -min;
						}
						else
						{
							// not sure what to actually do in this case
							// throw until we have an example of games having non-0 def with min > max
							throw new Exception("Fatal error: Minimum is greater than maximum with non-0 default for analog field, please report");
						}
					}

					AddFieldPtr(tag, @field, new AxisSpec(min.RangeTo(max), def), mask);
				}
			}

			foreach (var entry in fieldsTagsList)
			{
				var @field = entry.Item1;
				var tag = entry.Item2;
				var axis = entry.Item3;
				var mask = entry.Item4;
				var combined = $"{@field} [{tag}]";

				if (fieldsTagsList.Where(e => e.Item1 == @field).Skip(1).Any())
				{
					@field = combined;
				}

				if (axis == null)
				{
					buttonFieldList.Add(@field);
					MAMEController.BoolButtons.Add(@field);
				}
				else
				{
					analogFieldList.Add(@field);
					analogMaskDict.Add(@field, mask);
					MAMEController.AddAxis(@field, axis.Value.Range, axis.Value.Neutral);
				}
			}

			_buttonFields = buttonFieldList.ToArray();
			_analogFields = analogFieldList.ToArray();
			_analogMasks = analogMaskDict;
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

			var analogs = "";

			for (var i = 0; i < _analogFields.Length; i++)
			{
				var analogField = _analogFields[i];
				var value = controller.AxisValue(analogField);
				value &= _analogMasks[analogField];
				_fieldInputs[_buttonFields.Length + i] = value;
				analogs += value.ToHexString(4) + " ";
			}

			Console.WriteLine(analogs);

			_core.mame_input_set_fields(_fieldPtrs, _fieldInputs, _fieldInputs.Length);

			_core.mame_set_input_poll_callback(InputCallbacks.Count > 0 ? _inputPollCallback : null);
		}
	}
}