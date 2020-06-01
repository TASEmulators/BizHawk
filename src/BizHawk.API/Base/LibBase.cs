using System;

namespace BizHawk.API.Base
{
	public abstract class LibBase<T> where T : APIEnvironment
	{
		private T? _env;

		protected T Env => _env ?? throw new NullReferenceException();

		protected LibBase(out Action<T> updateEnv)
			=> updateEnv = newEnv =>
			{
				_env = newEnv;
				PostEnvUpdate();
			};

		protected virtual void PostEnvUpdate() {}
	}
}
