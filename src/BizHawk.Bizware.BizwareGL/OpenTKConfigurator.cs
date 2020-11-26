using System;

using OpenTK;

namespace BizHawk.Bizware.BizwareGL
{
	public static class OpenTKConfigurator
	{
		static OpenTKConfigurator()
		{
			// make sure OpenTK initializes without getting wrecked on the SDL check and throwing an exception to annoy our MDA's
			var toolkitOptions = ToolkitOptions.Default;
			toolkitOptions.Backend = PlatformBackend.PreferNative;
			Toolkit.Init(toolkitOptions);
			// NOTE: this throws EGL exceptions anyway. I'm going to ignore it and whine about it later
			// still seeing the exception in VS as of 2.5.3 dev... --yoshi
		}

		/// <summary>no-op; this class' static ctor is guaranteed to be called exactly once if this is called at least once</summary>
		public static void EnsureConfigurated() {}
	}
}
