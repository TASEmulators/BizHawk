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

		public bool HasService<T>()
			where T : IEmulatorService
		{
			IEmulatorService service;
			if (Services.TryGetValue(typeof(T), out service))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
