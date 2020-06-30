using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink
{
	public class GBHawkLinkControllerDeck
	{
		public GBHawkLinkControllerDeck(string controller1Name, string controller2Name)
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
				Name = Port1.Definition.Name,
				BoolButtons = Port1.Definition.BoolButtons
					.Concat(Port2.Definition.BoolButtons)
					.Concat(new[] { "Toggle Cable" } )
					.ToList()
			};
		}

		public byte ReadPort1(IController c)
		{
			return Port1.Read(c);
		}

		public byte ReadPort2(IController c)
		{
			return Port2.Read(c);
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(Port1));
			Port1.SyncState(ser);
			ser.EndSection();

			ser.BeginSection(nameof(Port2));
			Port2.SyncState(ser);
			ser.EndSection();
		}

		private readonly IPort Port1;
		private readonly IPort Port2;

		private static Dictionary<string, Type> _controllerTypes;

		public static Dictionary<string, Type> ValidControllerTypes
		{
			get
			{
				if (_controllerTypes == null)
				{
					_controllerTypes = typeof(GBHawkLinkControllerDeck).Assembly
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
