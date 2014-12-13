using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BizHawk.Emulation.Common
{
	public class BasicServiceProvider : IEmulatorServiceProvider
	{
		private Dictionary<Type, IEmulatorService> Services = new Dictionary<Type, IEmulatorService>();

		public BasicServiceProvider(IEmulator core)
		{
			var services = Assembly
				.GetAssembly(typeof(IEmulator))
				.GetTypes()
				.Where(t => t.IsInterface)
				.Where(t => typeof(IEmulatorService).IsAssignableFrom(t))
				.Where(t => t != typeof(IEmulatorService))
				.ToList();

			var coreType = core.GetType();

			foreach (var service in services)
			{
				if (service.IsAssignableFrom(coreType))
				{
					Services.Add(service, core);
				}
			}

			// Add the core itself since we know a core implements IEmulatorService
			Services.Add(core.GetType(), core);

			// Any IEmulatorServices the core might have that are core specific (and therefore not in Emulation.Common)
			var coreSpecificServices = core
				.GetType()
				.GetInterfaces()
				.Where(i => !services.Contains(i))
				.Where(t => typeof(IEmulatorService).IsAssignableFrom(t))
				.Where(t => !t.FullName.Contains("ISettable")) // adelikat: TODO: Hack! but I need a way around this, every core implements their own specific ISettable
				.ToList();

			foreach (var service in coreSpecificServices)
			{
				Services.Add(service, core);
			}

			foreach (var service in core.GetType().GetNestedTypes(BindingFlags.Public)
				.Where(t => typeof(IEmulatorService).IsAssignableFrom(t))
				.Where(t => t.IsClass))
			{
				if (service.IsAssignableFrom(coreType))
				{
					// TODO: get the instance from the core
					//Services.Add(service, core);
				}
			}
		}

		/// <summary>
		/// the core can call this to register an additional service
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="provider"></param>
		public void Register<T>(T provider)
			where T : IEmulatorService
		{
			Services[typeof(T)] = provider;
		}

		public IEmulatorService GetService<T>()
			where T : IEmulatorService
		{
			IEmulatorService service;
			if (Services.TryGetValue(typeof(T), out service))
			{
				return (T)service;
			}
			else
			{
				return null;
			}
		}

		public IEmulatorService GetService(Type t)
		{
			if (typeof(IEmulatorService).IsAssignableFrom(t))
			{
				IEmulatorService service;

				if (Services.TryGetValue(t, out service))
					return service;
				else
					return null;
			}
			else
			{
				throw new Exception(String.Format("Type {0} does not implement IEmulatorService.", t.Name));
			}
		}

		public bool HasService<T>()
			where T : IEmulatorService
		{
			IEmulatorService service;
			return Services.TryGetValue(typeof(T), out service);
		}

		public bool HasService(Type t)
		{
			if (typeof(IEmulatorService).IsAssignableFrom(t))
			{
				IEmulatorService service;
				return Services.TryGetValue(t, out service);
			}
			else
			{
				throw new Exception(String.Format("Type {0} does not implement IEmulatorService.", t.Name));
			}
		}
	}
}
