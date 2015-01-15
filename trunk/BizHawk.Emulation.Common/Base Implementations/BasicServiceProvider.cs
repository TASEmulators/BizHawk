using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BizHawk.Emulation.Common
{
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

			foreach (Type service in coreType.GetInterfaces().Where(t => t != typeof(IEmulatorService)))
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
		{
			if (provider == null)
				throw new ArgumentNullException("provider");
			Services[typeof(T)] = provider;
		}

		public T GetService<T>()
		{
			return (T)GetService(typeof(T));
		}

		public object GetService(Type t)
		{
			object service;
			if (Services.TryGetValue(t, out service))
				return service;
			else
				return null;
		}

		public bool HasService<T>()
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
