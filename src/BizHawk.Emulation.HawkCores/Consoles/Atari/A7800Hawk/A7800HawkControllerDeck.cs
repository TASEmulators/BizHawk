using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public class A7800HawkControllerDeck
	{
		public A7800HawkControllerDeck(string controller1Name, string controller2Name)
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
					.Concat(new[]
					{
						"Power",
						"Reset",
						"Select",
						"BW",
						"Toggle Left Difficulty", // better not put P# on these as they might not correspond to player numbers
						"Toggle Right Difficulty",
						"Pause"
					})
					.ToList()
			};

			foreach (var kvp in Port1.Definition.Axes) Definition.Axes.Add(kvp);
			foreach (var kvp in Port2.Definition.Axes) Definition.Axes.Add(kvp);
		}

		public byte ReadPort1(IController c)
		{
			return Port1.Read(c);
		}

		public byte ReadPort2(IController c)
		{
			return Port2.Read(c);
		}

		public byte ReadFire1(IController c)
		{
			return Port1.ReadFire(c);
		}

		public byte ReadFire2(IController c)
		{
			return Port2.ReadFire(c);
		}

		public byte ReadFire1_2x(IController c)
		{
			return Port1.ReadFire2x(c);
		}

		public byte ReadFire2_2x(IController c)
		{
			return Port2.ReadFire2x(c);
		}

		public bool Is_2_button1(IController c)
		{
			return Port1.Is_2_button(c);
		}

		public bool Is_2_button2(IController c)
		{
			return Port2.Is_2_button(c);
		}

		public bool Is_LightGun1(IController c, out float lightgun_x, out float lightgun_y)
		{
			return Port1.Is_LightGun(c, out lightgun_x, out lightgun_y);
		}

		public bool Is_LightGun2(IController c, out float lightgun_x, out float lightgun_y)
		{
			return Port2.Is_LightGun(c, out lightgun_x, out lightgun_y);
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
					_controllerTypes = typeof(A7800HawkControllerDeck).Assembly
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
