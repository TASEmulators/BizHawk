using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class SuperVision
	{
		private static readonly string[] _buttons =
		[
			"RIGHT", "LEFT", "DOWN", "UP", "B", "A", "SELECT", "START"
		];

		private static readonly Lazy<ControllerDefinition> _superVisionControllerDefinition = new(() =>
		{
			ControllerDefinition definition = new("SuperVision Controller");

			foreach (var b in _buttons)
			{
				definition.BoolButtons.Add(b);
			}

			return definition.MakeImmutable();
		});		

		private bool[] _buttonsState = new bool[8];

		/// <summary>
		/// 7       0
		/// ---------
		/// SLAB UDLR
		/// 
		/// S: Start button
		/// L: Select button
		/// A: A button
		/// B: B button
		/// U: Up on D-pad
		/// D: Down on D-pad
		/// L: Left on D-pad
		/// R: Right on D-pad
		/// </summary>
		private byte _buttonsData
		{
			get
			{
				// pins are active-low
				byte data = 0xFF;

				for (int i = 0; i < 8; i++)
				{
					if (_buttonsState[i])
					{
						data &= (byte) ~(1 << i);
					}
				}

				return data;
			}
		}

		public byte ReadControllerByte() => _buttonsData;
	}
}
