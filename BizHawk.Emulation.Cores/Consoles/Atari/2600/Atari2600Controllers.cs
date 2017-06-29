using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public enum Atari2600ControllerTypes
	{
		Unplugged,
		Joystick,
		Paddle
	}

	/// <summary>
	/// Represents a controller plugged into a controller port on the Colecovision
	/// </summary>
	public interface IPort
	{
		byte Read(IController c);

		int Read_Pot(IController c, int pot);

		ControllerDefinition Definition { get; }

		void SyncState(Serializer ser);

		int PortNum { get; }
	}

	public class UnpluggedController : IPort
	{
		public UnpluggedController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				BoolButtons = new List<string>()
			};
		}

		public byte Read(IController c)
		{
			return 0xFF;
		}

		public int Read_Pot(IController c, int pot)
		{
			return -1; // indicates not applicable
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			// Do nothing
		}

		public int PortNum { get; }
	}

	public class StandardController : IPort
	{
		public StandardController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				BoolButtons = BaseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList()
			};
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			// Nothing todo, I think
		}

		public int PortNum { get; }

		public byte Read(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed($"P{PortNum} Up")) { result &= 0xEF; }
			if (c.IsPressed($"P{PortNum} Down")) { result &= 0xDF; }
			if (c.IsPressed($"P{PortNum} Left")) { result &= 0xBF; }
			if (c.IsPressed($"P{PortNum} Right")) { result &= 0x7F; }
			if (c.IsPressed($"P{PortNum} Button")) { result &= 0xF7; }

			return result;
		}

		public int Read_Pot(IController c, int pot)
		{
			return -1; // indicates not applicable
		}

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "Button"
		};
	}

	public class PaddleController : IPort
	{
		public PaddleController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				BoolButtons = BaseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList(),
				FloatControls = { "P" + PortNum + " Paddle X 1" , "P" + PortNum + " Paddle X 2" },
				FloatRanges = { new[] { -127.0f, 0, 127.0f }, new[] { -127.0f, 0, 127.0f } }
			};
		}

		public int PortNum { get; }

		public void SyncState(Serializer ser)
		{
			// Nothing todo, I think
		}

		public ControllerDefinition Definition { get; }

		private static readonly string[] BaseDefinition =
		{
			"Button 1",
			"Button 2"
		};

		public byte Read(IController c)
		{
			byte result = 0xF0;

			if (c.IsPressed($"P{PortNum} Button 1")) { result &= 0x70; }
			if (c.IsPressed($"P{PortNum} Button 2")) { result &= 0xB0; }

			return result;
		}

		public int Read_Pot(IController c, int pot)
		{
			int x = (int)c.GetFloat(Definition.FloatControls[pot]);			
			
			x = -x;
			x += 127;

			x = x * 64 + 10;

			return x;
		}
	}
}
