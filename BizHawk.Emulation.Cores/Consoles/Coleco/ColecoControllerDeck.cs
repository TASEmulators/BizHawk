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
					.Concat(new[]
					{
						"Power", "Reset"
					})
					.ToList()
			};

			Definition.AxisControls.AddRange(Port1.Definition.AxisControls);
			Definition.AxisControls.AddRange(Port2.Definition.AxisControls);

			Definition.AxisRanges.AddRange(Port1.Definition.AxisRanges);
			Definition.AxisRanges.AddRange(Port2.Definition.AxisRanges);
		}

		public float wheel1;
		public float wheel2;

		public float temp_wheel1;
		public float temp_wheel2;

		public byte ReadPort1(IController c, bool leftMode, bool updateWheel)
		{
			wheel1 = Port1.UpdateWheel(c);

			return Port1.Read(c, leftMode, updateWheel, temp_wheel1);
		}

		public byte ReadPort2(IController c, bool leftMode, bool updateWheel)
		{
			wheel2 = Port2.UpdateWheel(c);

			return Port2.Read(c, leftMode, updateWheel, temp_wheel2);
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(Port1));
			Port1.SyncState(ser);
			ser.Sync(nameof(temp_wheel1), ref temp_wheel1);
			ser.EndSection();

			ser.BeginSection(nameof(Port2));
			ser.Sync(nameof(temp_wheel2), ref temp_wheel2);
			Port2.SyncState(ser);
			ser.EndSection();
		}

		public IPort Port1 { get; }
		public IPort Port2 { get; }

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
