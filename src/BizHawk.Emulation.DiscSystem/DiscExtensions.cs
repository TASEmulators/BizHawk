using System;

namespace BizHawk.Emulation.DiscSystem
{
	public static class DiscExtensions
	{
		public static Disc Create(this DiscType type, string path, Action<string> errorCallback)
		{
			//--- load the disc in a context which will let us abort if it's going to take too long
			var discMountJob = new DiscMountJob { IN_FromPath = path, IN_SlowLoadAbortThreshold = 8 };
			discMountJob.Run();
			var disc = discMountJob.OUT_Disc ?? throw new InvalidOperationException($"Can't find the file specified: {path}");

			if (discMountJob.OUT_SlowLoadAborted)
			{
				errorCallback("This disc would take too long to load. Run it through DiscoHawk first, or find a new rip because this one is probably junk");
				return null;
			}

			if (discMountJob.OUT_ErrorLevel)
			{
				throw new InvalidOperationException($"\r\n{discMountJob.OUT_Log}");
			}

			var discType = new DiscIdentifier(disc).DetectDiscType();

			if (discType != type)
			{
				errorCallback($"Not a {type} disc");
				return null;
			}

			return disc;
		}
	}
}
