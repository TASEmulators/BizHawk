using System;
using System.Collections.Generic;
using System.ComponentModel;
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
			Port2 = (IPort)Activator.CreateInstance(ValidControllerTypes[controller2Name], 2); ;

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

		public byte ReadPort1(IController c, bool left_mode)
		{
			return Port1.Read(c, left_mode);
		}

		public byte ReadPort2(IController c, bool left_mode)
		{
			return Port2.Read(c, left_mode);
		}

		public ControllerDefinition Definition { get; private set; }

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Port1");
			Port1.SyncState(ser);
			ser.EndSection();

			ser.BeginSection("Port2");
			Port2.SyncState(ser);
			ser.EndSection();
		}

		private readonly IPort Port1;
		private readonly IPort Port2;

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

		public static string DefaultControllerName
		{
			get { return typeof(StandardController).DisplayName(); }
		}
	}

}
