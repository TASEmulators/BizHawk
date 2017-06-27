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
		Joystick
	}

	/// <summary>
	/// Represents a controller plugged into a controller port on the Colecovision
	/// </summary>
	public interface IPort
	{
		byte Read(IController c);

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

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "Button"
		};
	}
}
