using System.Collections.Generic;

using Silk.NET.OpenGL;

using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Wraps checking OpenGL versions
	/// </summary>
	public static class OpenGLVersion
	{
		private static readonly IDictionary<int, bool> _glSupport = new Dictionary<int, bool>();

		private static int PackGLVersion(int major, int minor)
			=> major * 10 + minor;

		private static bool CheckVersion(int requestedMajor, int requestedMinor)
		{
			using (new SavedOpenGLContext())
			{
				try
				{
					using (new SDL2OpenGLContext(requestedMajor, requestedMinor, true))
					{
						using var gl = GL.GetApi(SDL2OpenGLContext.GetGLProcAddress);
						var versionString = gl.GetStringS(StringName.Version);
						var versionParts = versionString!.Split('.');
						var major = int.Parse(versionParts[0]);
						var minor = int.Parse(versionParts[1][0].ToString());
						return PackGLVersion(major, minor) >= PackGLVersion(requestedMajor, requestedMinor);
					}
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"OpenGL check for version {requestedMajor}.{requestedMinor} failed, underlying exception: {ex}");
					return false;
				}
			}
		}

		public static bool SupportsVersion(int major, int minor)
			=> _glSupport.GetValueOrPut(PackGLVersion(major, minor),
				static version => CheckVersion(version / 10, version % 10));
	}
}
