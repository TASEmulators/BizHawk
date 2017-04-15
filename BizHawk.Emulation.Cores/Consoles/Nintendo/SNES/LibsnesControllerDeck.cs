using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public class LibsnesControllerDeck
	{
		public enum ControllerType
		{
			Unplugged,
			Gamepad,
			Multitap,
			Payload
		}

		private static ILibsnesController Factory(ControllerType t)
		{
			switch (t)
			{
				case ControllerType.Unplugged: return new SnesUnpluggedController();
				case ControllerType.Gamepad: return new SnesController();
				case ControllerType.Multitap: return new SnesMultitapController();
				case ControllerType.Payload: return new SnesPayloadController();
				default: throw new InvalidOperationException();
			}
		}

		private readonly ILibsnesController[] _ports;
		private readonly ControlDefUnMerger[] _mergers;

		public ControllerDefinition Definition { get; private set; }

		public LibsnesControllerDeck(ControllerType left, ControllerType right)
		{
			_ports = new[] { Factory(left), Factory(right) };
			List<ControlDefUnMerger> tmp;
			Definition = ControllerDefinitionMerger.GetMerged(_ports.Select(p => p.Definition), out tmp);
			_mergers = tmp.ToArray();

			// add buttons that the core itself will handle
			Definition.BoolButtons.Add("Reset");
			Definition.BoolButtons.Add("Power");
			Definition.Name = "SNES Controller";
		}

		public void NativeInit(LibsnesApi api)
		{
			for (int i = 0; i < 2; i++)
			{
				api.SetInputPortBeforeInit(i, _ports[i].PortType);
			}
		}

		public ushort CoreInputState(IController controller, int port, int device, int index, int id)
		{
			return _ports[port].GetState(_mergers[port].UnMerge(controller), index, id);
		}
	}

	public interface ILibsnesController
	{
		/// <summary>
		/// the type to pass back to the native init
		/// </summary>
		LibsnesApi.SNES_INPUT_PORT PortType { get; }

		/// <summary>
		/// respond to a native core poll
		/// </summary>
		/// <param name="controller">controller input from user, remapped</param>
		/// <param name="index">libsnes specific value, sometimes multitap number</param>
		/// <param name="id">libsnes specific value, sometimes button number</param>
		/// <returns></returns>
		ushort GetState(IController controller, int index, int id);

		ControllerDefinition Definition { get; }

		// due to the way things are implemented, right now, all of the ILibsnesControllers are stateless
		// but if one needed state, that would be doable
		// void SyncState(Serializer ser);
	}

	public class SnesController : ILibsnesController
	{
		public LibsnesApi.SNES_INPUT_PORT PortType { get; } = LibsnesApi.SNES_INPUT_PORT.Joypad;

		private static readonly string[] Buttons =
		{
			"0B",
			"0Y",
			"0Select",
			"0Start",
			"0Up",
			"0Down",
			"0Left",
			"0Right",
			"0A",
			"0X",
			"0L",
			"0R"
		};

		private static readonly ControllerDefinition _definition = new ControllerDefinition
		{
			BoolButtons = Buttons.ToList()
		};

		public ControllerDefinition Definition { get; } = _definition;

		public ushort GetState(IController controller, int index, int id)
		{
			if (id >= 12)
			{
				return 0;
			}
			return (ushort)(controller.IsPressed(Buttons[id]) ? 1 : 0);
		}
	}

	public class SnesMultitapController : ILibsnesController
	{
		public LibsnesApi.SNES_INPUT_PORT PortType { get; } = LibsnesApi.SNES_INPUT_PORT.Multitap;

		private static readonly string[] Buttons =
		{
			"B",
			"Y",
			"Select",
			"Start",
			"Up",
			"Down",
			"Left",
			"Right",
			"A",
			"X",
			"L",
			"R"
		};

		private static readonly ControllerDefinition _definition = new ControllerDefinition
		{
			BoolButtons = Enumerable.Range(0, 4).SelectMany(i => Buttons.Select(b => i + b)).ToList()
		};

		public ControllerDefinition Definition { get; } = _definition;

		public ushort GetState(IController controller, int index, int id)
		{
			if (id >= 12)
			{
				return 0;
			}
			return (ushort)(controller.IsPressed(index + Buttons[id]) ? 1 : 0);
		}
	}

	public class SnesPayloadController : ILibsnesController
	{
		public LibsnesApi.SNES_INPUT_PORT PortType { get; } = LibsnesApi.SNES_INPUT_PORT.Multitap;

		private static readonly ControllerDefinition _definition = new ControllerDefinition
		{
			BoolButtons = Enumerable.Range(0, 32).Select(i => "0B" + i).ToList()
		};

		public ControllerDefinition Definition { get; } = _definition;

		public ushort GetState(IController controller, int index, int id)
		{
			return (ushort)(controller.IsPressed("0B" + (index << 4 & 16 | id)) ? 1 : 0);
		}
	}

	public class SnesUnpluggedController : ILibsnesController
	{
		public LibsnesApi.SNES_INPUT_PORT PortType { get; } = LibsnesApi.SNES_INPUT_PORT.None;

		private static readonly ControllerDefinition _definition = new ControllerDefinition();

		public ControllerDefinition Definition { get; } = _definition;

		public ushort GetState(IController controller, int index, int id)
		{
			return 0;
		}
	}
}
