#nullable disable

using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A empty implementation of IController that represents the lack of
	/// a controller interface
	/// </summary>
	/// <seealso cref="IController" />
	public class NullController : IController
	{
		public ControllerDefinition Definition { get; } = new ControllerDefinition("Null Controller").MakeImmutable();

		public bool IsPressed(string button) => false;

		public int AxisValue(string name) => 0;

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Array.Empty<(string, int)>();

		public void SetHapticChannelStrength(string name, int strength) {}

		public static readonly NullController Instance = new();
		private NullController() {}
	}
}
