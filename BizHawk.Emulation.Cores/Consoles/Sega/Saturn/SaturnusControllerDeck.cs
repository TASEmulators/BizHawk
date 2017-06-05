using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Sega.Saturn
{
	public class SaturnusControllerDeck
	{
		private const int DataSize = 32;

		private static readonly Type[] Implementors =
		{
			typeof(None),
			typeof(Gamepad),
			typeof(None),
			typeof(None),
			typeof(None),
			typeof(None),
			typeof(None),
			typeof(None)
		};

		private readonly IDevice[] _devices;
		private readonly ControlDefUnMerger[] _unmerger;
		private readonly byte[] _data;

		public ControllerDefinition Definition { get; }

		public SaturnusControllerDeck(bool[] multitap, Device[] devices, LibSaturnus core)
		{
			int count = 2 + multitap.Count(b => b) * 5;

			int[] dev = new int[12];
			int[] mt = new int[2];

			for (int i = 0; i < 12; i++)
				dev[i] = (int)(i < count ? devices[i] : Device.None);
			for (int i = 0; i < 2; i++)
				mt[i] = multitap[i] ? 1 : 0;

			core.SetupInput(dev, mt);

			_devices = dev.Take(count)
				.Select(i => Activator.CreateInstance(Implementors[i]))
				.Cast<IDevice>()
				.ToArray();
			_data = new byte[count * DataSize];

			List<ControlDefUnMerger> cdum;
			Definition = ControllerDefinitionMerger.GetMerged(_devices.Select(d => d.Definition),
				out cdum);
			_unmerger = cdum.ToArray();
		}

		public byte[] Poll(IController controller)
		{
			for (int i = 0, offset = 0; i < _devices.Length; i++, offset += DataSize)
				_devices[i].Update(_unmerger[i].UnMerge(controller), _data, offset);
			return _data;
		}

		public enum Device
		{
			None,
			Gamepad,
			ThreeDeePad,
			Mouse,
			Wheel,
			Mission,
			DualMission,
			Keyboard
		}

		private interface IDevice
		{
			void Update(IController controller, byte[] dest, int offset);
			ControllerDefinition Definition { get; }
		}

		private class None : IDevice
		{
			private static readonly ControllerDefinition NoneDefition = new ControllerDefinition();
			public ControllerDefinition Definition => NoneDefition;
			public void Update(IController controller, byte[] dest, int offset)
			{
			}

		}

		private abstract class ButtonedDevice : IDevice
		{
			protected ButtonedDevice()
			{
				Definition = new ControllerDefinition
				{
					BoolButtons = ButtonNames.Where(s => s != null).ToList()
				};
			}

			protected abstract string[] ButtonNames { get; }
			public ControllerDefinition Definition { get; }

			public virtual void Update(IController controller, byte[] dest, int offset)
			{
				byte data = 0;
				int pos = 0;
				int bit = 0;
				for (int i = 0; i < ButtonNames.Length; i++)
				{
					if (ButtonNames[i] != null && controller.IsPressed(ButtonNames[i]))
						data |= (byte)(1 << bit);
					if (++bit == 8)
					{
						bit = 0;
						dest[offset + pos++] = data;
						data = 0;
					}
				}
				if (bit != 0)
					dest[offset] = data;
			}
		}

		private class Gamepad : ButtonedDevice
		{
			private static readonly string[] _buttonNames =
			{
				"0Z", "0Y", "0X", "0R",
				"0Up", "0Down", "0Left", "0Right",
				"0B", "0C", "0A", "0Start",
				null,null, null, "0L"
			};

			protected override string[] ButtonNames => _buttonNames;
		}
	}
}
