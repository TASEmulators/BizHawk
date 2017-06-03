using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public class ColecoVisionControllerDeck
	{
		public ColecoVisionControllerDeck(string controller1Name, string controller2Name)
		{
			if (!ValidControllerTypes.ContainsKey(controller1Name))
			{
				throw new InvalidOperationException("Invalid controller type: " + controller1Name);
			}

			if (!ValidControllerTypes.ContainsKey(controller2Name))
			{
				throw new InvalidOperationException("Invalid controller type: " + controller2Name);
			}

			Port1 = (IPort)Activator.CreateInstance(ValidControllerTypes[controller1Name], 1);
			Port2 = (IPort)Activator.CreateInstance(ValidControllerTypes[controller2Name], 2);

			Definition = new ControllerDefinition
			{
				Name = "ColecoVision Basic Controller",
				BoolButtons = Port1.Definition.BoolButtons
					.Concat(Port2.Definition.BoolButtons)
					.ToList()
			};

			Definition.FloatControls.AddRange(Port1.Definition.FloatControls);
			Definition.FloatControls.AddRange(Port2.Definition.FloatControls);

			Definition.FloatRanges.AddRange(Port1.Definition.FloatRanges);
			Definition.FloatRanges.AddRange(Port2.Definition.FloatRanges);
		}

		private int wheel1;
		private int wheel2;

		public byte ReadPort1(IController c, bool leftMode, bool updateWheel)
		{
			if (updateWheel)
			{
				wheel1 = Port1.UpdateWheel(c, wheel1);
			}

			return Port1.Read(c, leftMode, wheel1);
		}

		public byte ReadPort2(IController c, bool leftMode, bool updateWheel)
		{
			if (updateWheel)
			{
				wheel2 = Port2.UpdateWheel(c, wheel2);
			}

			return Port2.Read(c, leftMode, wheel2);
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Port1");
			ser.Sync("Wheel 1", ref wheel1);
			Port1.SyncState(ser);
			ser.EndSection();

			ser.BeginSection("Port2");
			ser.Sync("Wheel 2", ref wheel2);
			Port2.SyncState(ser);
			ser.EndSection();
		}

		public IPort Port1 { get; private set; }
		public IPort Port2 { get; private set; }

		private static Dictionary<string, Type> _controllerTypes = null;

		public static Dictionary<string, Type> ValidControllerTypes
		{
			get
			{
				if (_controllerTypes == null)
				{
					_controllerTypes = typeof(ColecoVisionControllerDeck).Assembly
						.GetTypes()
						.Where(t => typeof(IPort).IsAssignableFrom(t))
						.Where(t => !t.IsAbstract && !t.IsInterface)
						.ToDictionary(tkey => tkey.DisplayName());
				}

				return _controllerTypes;
			}
		}

		public static string DefaultControllerName => typeof(StandardController).DisplayName();
	}

}
