#nullable disable

using System.Collections.Generic;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Can freeze a copy of a controller input set and serialize\deserialize it
	/// </summary>
	public class SaveController : IController
	{
		private readonly Dictionary<string, int> _buttons = new();

		public SaveController()
		{
			Definition = null;
		}

		public SaveController(ControllerDefinition def)
		{
			Definition = def;
		}

		/// <summary>
		/// Gets the current definition.
		/// Invalid until CopyFrom has been called
		/// </summary>
		public ControllerDefinition Definition { get; private set; }

		public void Serialize(BinaryWriter b)
		{
			b.Write(_buttons.Keys.Count);
			foreach (var (k, v) in _buttons)
			{
				b.Write(k);
				b.Write(v);
			}
		}

		/// <summary>
		/// No checking to see if the deserialized controls match any definition
		/// </summary>
		public void DeSerialize(BinaryReader b)
		{
			_buttons.Clear();
			int numButtons = b.ReadInt32();
			for (int i = 0; i < numButtons; i++)
			{
				string k = b.ReadString();
				float v = b.ReadSingle();
				_buttons.Add(k, (int) v);
			}
		}

		/// <summary>replace this controller's definition with that of <paramref name="source"/></summary>
		/// <exception cref="Exception">definition of <paramref name="source"/> has a button and an analog control with the same name</exception>
		public void CopyFrom(IController source)
		{
			Definition = source.Definition;
			_buttons.Clear();
			foreach (var k in Definition.BoolButtons)
			{
				_buttons.Add(k, source.IsPressed(k) ? 1 : 0);
			}

			foreach (var k in Definition.Axes.Keys)
			{
				if (_buttons.ContainsKey(k))
				{
					throw new Exception("name collision between bool and float lists!");
				}

				_buttons.Add(k, source.AxisValue(k));
			}
		}

		public void Clear()
		{
			_buttons.Clear();
		}

		public void Set(string button)
		{
			_buttons[button] = 1;
		}

		public bool IsPressed(string button)
			=> _buttons.GetValueOrDefault(button) is not 0;

		public int AxisValue(string name)
			=> _buttons.GetValueOrDefault(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Array.Empty<(string, int)>();

		public void SetHapticChannelStrength(string name, int strength) {}
	}
}
