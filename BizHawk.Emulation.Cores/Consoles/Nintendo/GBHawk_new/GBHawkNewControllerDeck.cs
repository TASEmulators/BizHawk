using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkNew
{
	public class GBHawkNewControllerDeck
	{
		public GBHawkNewControllerDeck(string controller1Name)
		{
			if (!ValidControllerTypes.ContainsKey(controller1Name))
			{
				throw new InvalidOperationException("Invalid controller type: " + controller1Name);
			}

			Port1 = (IPort)Activator.CreateInstance(ValidControllerTypes[controller1Name], 1);

			Definition = new ControllerDefinition
			{
				Name = Port1.Definition.Name,
				BoolButtons = Port1.Definition.BoolButtons
					.ToList()
			};

			Definition.FloatControls.AddRange(Port1.Definition.FloatControls);

			Definition.FloatRanges.AddRange(Port1.Definition.FloatRanges);
		}

		public byte ReadPort1(IController c)
		{
			return Port1.Read(c);
		}

		public ushort ReadAccX1(IController c)
		{
			return Port1.ReadAccX(c);
		}

		public ushort ReadAccY1(IController c)
		{
			return Port1.ReadAccY(c);
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(Port1));
			Port1.SyncState(ser);
			ser.EndSection();
		}

		private readonly IPort Port1;

		private static Dictionary<string, Type> _controllerTypes;

		public static Dictionary<string, Type> ValidControllerTypes
		{
			get
			{
				if (_controllerTypes == null)
				{
					_controllerTypes = typeof(GBHawkNewControllerDeck).Assembly
						.GetTypes()
						.Where(t => typeof(IPort).IsAssignableFrom(t))
						.Where(t => !t.IsAbstract && !t.IsInterface)
						.ToDictionary(tkey => tkey.DisplayName());
				}

				return _controllerTypes;
			}
		}

		public static string DefaultControllerName => typeof(StandardControls).DisplayName();
	}
}
