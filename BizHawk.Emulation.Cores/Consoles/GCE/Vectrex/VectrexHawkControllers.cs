using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	/// <summary>
	/// Represents a Vectrex add on
	/// </summary>
	public interface IPort
	{
		byte Read(IController c);

		ControllerDefinition Definition { get; }

		void SyncState(Serializer ser);

		int PortNum { get; }
	}

	[DisplayName("Vectrex Digital Controller")]
	public class StandardControls : IPort
	{
		public StandardControls(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				Name = "Vectrex Digital Controller",
				BoolButtons = BaseDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList(),
			};
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		public byte Read(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed($"P{PortNum} Button 1")) { result &= 0xFE; }
			if (c.IsPressed($"P{PortNum} Button 2")) { result &= 0xFD; }
			if (c.IsPressed($"P{PortNum} Button 3")) { result &= 0xFB; }
			if (c.IsPressed($"P{PortNum} Button 4")) { result &= 0xF7; }

			return result;
		}

		private static readonly string[] BaseDefinition =
		{
			"Up",
			"Down",
			"Left",
			"Right",
			"Button 1",
			"Button 2",
			"Button 3",
			"Button 4"
		};

		public void SyncState(Serializer ser)
		{
			//nothing
		}
	}

	[DisplayName("Vectrex Analog Controller")]
	public class AnalogControls : IPort
	{
		public AnalogControls(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				Name = "Vectrex Analog Controller",
				BoolButtons = BaseDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList(),
				FloatControls = { "P" + PortNum + " Stick X", "P" + PortNum + " Stick Y" },
				FloatRanges = { new[] { -128.0f, 0, 127.0f }, new[] { -128.0f, 0, 127.0f } }
			};
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		public byte Read(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed($"P{PortNum} Button 1")) { result &= 0xFE; }
			if (c.IsPressed($"P{PortNum} Button 2")) { result &= 0xFD; }
			if (c.IsPressed($"P{PortNum} Button 3")) { result &= 0xFB; }
			if (c.IsPressed($"P{PortNum} Button 4")) { result &= 0xF7; }

			return result;
		}

		private static readonly string[] BaseDefinition =
		{
			"Button 1",
			"Button 2",
			"Button 3",
			"Button 4"
		};

		public void SyncState(Serializer ser)
		{
			//nothing
		}
	}
}