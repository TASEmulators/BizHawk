using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A generic implementation of IEmulatorService provider that provides
	/// this functionality to any core.
	/// The provider will scan an IEmulator and register all IEmulatorServices
	/// that the core object itself implements.  In addition it provides
	/// a Register() method to allow the core to pass in any additional services
	/// </summary>
	/// <seealso cref="IEmulatorServiceProvider"/> 
	public class BasicServiceProvider : IEmulatorServiceProvider
	{
		private Dictionary<Type, object> Services = new Dictionary<Type, object>();

		public BasicServiceProvider(IEmulator core)
		{
			// simplified logic here doesn't scan for possible services; just adds what it knows is implemented by the core
			// this removes the possibility of automagically picking up a service in a nested class, (find the type, then
			// find the field), but we're going to keep such logic out of the basic provider.  anything the passed
			// core doesn't implement directly needs to be added with Register()

			// this also fully allows services that are not IEmulatorService

			Type coreType = core.GetType();

			var services = coreType.GetInterfaces()
				.Where(t => typeof(IEmulatorService).IsAssignableFrom(t))
				.Where(t => t != typeof(IEmulatorService));

			foreach (Type service in services)
			{
				Services.Add(service, core);
			}

			// add the actual instantiated type and any types in the hierarchy
			// except for object because that would be dumb (or would it?)
			while (coreType != typeof(object))
			{
				Services.Add(coreType, core);
				coreType = coreType.BaseType;
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
			if (provider == null)
			{
				throw new ArgumentNullException("provider");
			}

			Services[typeof(T)] = provider;
		}

		public T GetService<T>()
			where T : IEmulatorService
		{
			return (T)GetService(typeof(T));
		}

		public object GetService(Type t)
		{
			object service;
			if (Services.TryGetValue(t, out service))
			{
				return service;
			}

			return null;
		}

		public bool HasService<T>()
			where T : IEmulatorService
		{
			return HasService(typeof(T));
		}

		public bool HasService(Type t)
		{
			return Services.ContainsKey(t);
		}

		public IEnumerable<Type> AvailableServices
		{
			get
			{
				return Services.Select(d => d.Key);
			}
		}
	}
}
