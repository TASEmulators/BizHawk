using System;
using System.Linq;
using System.Reflection;
using BizHawk.Common.ReflectionExtensions;
using System.Runtime.CompilerServices;

namespace BizHawk.Emulation.Common.IEmulatorExtensions
{
	public static class Extensions
	{
		public static CoreAttributes Attributes(this IEmulator core)
		{
			return (CoreAttributes)Attribute.GetCustomAttribute(core.GetType(), typeof(CoreAttributes));
		}

		// todo: most of the special cases involving the NullEmulator should probably go away
		public static bool IsNull(this IEmulator core)
		{
			return core == null || core is NullEmulator;
		}

		public static bool HasVideoProvider(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IVideoProvider>();
		}

		public static IVideoProvider AsVideoProvider(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IVideoProvider>();
		}

		/// <summary>
		/// Returns the core's VideoProvider, or a suitable dummy provider
		/// </summary>
		/// <param name="core"></param>
		/// <returns></returns>
		public static IVideoProvider AsVideoProviderOrDefault(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IVideoProvider>()
				?? NullVideo.Instance;
		}

		public static bool HasSoundProvider(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<ISoundProvider>();
		}

		public static ISoundProvider AsSoundProvider(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ISoundProvider>();
		}

		private static readonly ConditionalWeakTable<IEmulator, ISoundProvider> CachedNullSoundProviders = new ConditionalWeakTable<IEmulator, ISoundProvider>();

		/// <summary>
		/// returns the core's SoundProvider, or a suitable dummy provider
		/// </summary>
		public static ISoundProvider AsSoundProviderOrDefault(this IEmulator core)
		{
			var ret = core.ServiceProvider.GetService<ISoundProvider>();
			if (ret == null)
				ret = CachedNullSoundProviders.GetValue(core, e => new NullSound(e.CoreComm.VsyncNum, e.CoreComm.VsyncDen));
			return ret;
		}

		public static bool HasMemoryDomains(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IMemoryDomains>();
		}

		public static IMemoryDomains AsMemoryDomains(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IMemoryDomains>();
		}

		public static bool HasSaveRam(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<ISaveRam>();
		}

		public static ISaveRam AsSaveRam(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ISaveRam>();
		}

		public static bool HasSavestates(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IStatable>();
		}

		public static IStatable AsStatable(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IStatable>();
		}

		public static bool CanPollInput(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IInputPollable>();
		}

		public static IInputPollable AsInputPollable(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IInputPollable>();
		}

		public static bool InputCallbacksAvailable(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			// TODO: this is a pretty ugly way to handle this
			var pollable = core.ServiceProvider.GetService<IInputPollable>();
			if (pollable != null)
			{
				try
				{
					var callbacks = pollable.InputCallbacks;
					return true;
				}
				catch (NotImplementedException)
				{
					return false;
				}
			}

			return false;
		}

		public static bool HasDriveLight(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IDriveLight>();
		}

		public static IDriveLight AsDriveLight(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IDriveLight>();
		}

		public static bool CanDebug(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IDebuggable>();
		}

		public static IDebuggable AsDebuggable(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IDebuggable>();
		}

		public static bool CpuTraceAvailable(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<ITraceable>();
		}

		public static ITraceable AsTracer(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ITraceable>();
		}

		public static bool MemoryCallbacksAvailable(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			// TODO: this is a pretty ugly way to handle this
			var debuggable = (IDebuggable)core.ServiceProvider.GetService<IDebuggable>();
			if (debuggable != null)
			{
				try
				{
					var callbacks = debuggable.MemoryCallbacks;
					return true;
				}
				catch (NotImplementedException)
				{
					return false;
				}
			}

			return false;
		}

		public static bool MemoryCallbacksAvailable(this IDebuggable core)
		{
			if (core == null)
			{
				return false;
			}

			try
			{
				var callbacks = core.MemoryCallbacks;
				return true;
			}
			catch (NotImplementedException)
			{
				return false;
			}
		}

		public static bool CanDisassemble(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IDisassemblable>();
		}

		public static IDisassemblable AsDissassembler(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IDisassemblable>();
		}

		public static bool CanPoke(this MemoryDomain d)
		{
			if (!d.Writable)
			{
				return false;
			}
			
			try
			{
				d.PokeByte(0, d.PeekByte(0));
			}
			catch (NotImplementedException)
			{
				return false;
			}

			return true;
		}

		public static bool HasRegions(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IRegionable>();
		}

		public static IRegionable AsRegionable(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IRegionable>();
		}

		public static bool CanCDLog(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<ICodeDataLogger>();
		}

		public static ICodeDataLogger AsCodeDataLogger(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ICodeDataLogger>();
		}

		public static ILinkable AsLinkable(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ILinkable>();
		}

		public static bool UsesLinkCable(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<ILinkable>();
		}

		public static bool CanGenerateGameDBEntries(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<ICreateGameDBEntries>();
		}

		public static ICreateGameDBEntries AsGameDBEntryGenerator(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ICreateGameDBEntries>();
		}

		// TODO: a better place for these
		public static bool IsImplemented(this MethodInfo info)
		{
			// If a method is marked as Not implemented, it is not implemented no matter what the body is
			if (info.GetCustomAttributes(false).Any(a => a is FeatureNotImplemented))
			{
				return false;
			}

			// If a method is not marked but all it does is throw an exception, consider it not implemented
			return !info.ThrowsError();
		}

		public static bool IsImplemented(this PropertyInfo info)
		{
			return !info.GetCustomAttributes(false).Any(a => a is FeatureNotImplemented);
		}
	}
}
