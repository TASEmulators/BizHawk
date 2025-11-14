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
		private IntPtr[] _fieldPtrs;
		private int[] _fieldInputs;

		// negative min value seems to be represented as positive in mame
		// which needs to be used in combination with the mask.
		// for example, Top Landing has ranges like 0x800 - 0 - 0x7ff and mask 0xfff
		// all positive internally but the internal negative value appears as (mask - value).
		// when sending those back to the core we have to convert them back
		private Dictionary<string, int> _wildAxes = [ ];

		private void GetInputFields()
		{
			var buttonFields = MameGetString(MAMELuaCommand.GetButtonFields).Split(';');
			var analogFields = MameGetString(MAMELuaCommand.GetAnalogFields).Split(';');

			var buttonFieldList = new List<string>();
			var analogFieldList = new List<string>();
			var fieldPtrList = new List<IntPtr>();
			var axes = new Dictionary<string, AxisSpec>();
			var fieldsTagsList = new List<Tuple<string, string, AxisSpec?, bool, int>>();

			void AddFieldPtr(
				string tag,
				string @field,
				AxisSpec? axis = null,
				bool isWild = false,
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
				fieldsTagsList.Add(new Tuple<string, string, AxisSpec?, bool, int>(@field, tag, axis, isWild, mask));
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
					var isWild = min > def;

					if (isWild)
					{
						min -= mask;
					}

					AddFieldPtr(tag, @field, new AxisSpec(min.RangeTo(max), def), isWild, mask);
				}
			}

			foreach (var entry in fieldsTagsList)
			{
				var @field = entry.Item1;
				var tag = entry.Item2;
				var axis = entry.Item3;
				var isWild = entry.Item4;
				var mask = entry.Item5;
				var combined = $"{@field} [{tag}]";

				if (fieldsTagsList.Where(e => e.Item1 == @field).Skip(1).Any())
				{
					@field = combined;
				}

				if (isWild)
				{
					_wildAxes.Add(@field, mask);
				}

				if (axis == null)
				{
					buttonFieldList.Add(@field);
					MAMEController.BoolButtons.Add(@field);
				}
				else
				{
					analogFieldList.Add(@field);
					MAMEController.AddAxis(@field, axis.Value.Range, axis.Value.Neutral);
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

			var analogs = "";

			for (var i = 0; i < _analogFields.Length; i++)
			{
				var value = controller.AxisValue(_analogFields[i]);
				var axis = controller.Definition.Axes.First(a => a.Key == _analogFields[i]).Value;

				if (value < axis.Neutral && _wildAxes.TryGetValue(_analogFields[i], out int mask))
				{
					value += mask;
				}

				_fieldInputs[_buttonFields.Length + i] = value;

				analogs += value.ToHexString(4) + " ";
			}

			Console.WriteLine(analogs);

			_core.mame_input_set_fields(_fieldPtrs, _fieldInputs, _fieldInputs.Length);

			_core.mame_set_input_poll_callback(InputCallbacks.Count > 0 ? _inputPollCallback : null);
		}
	}
}