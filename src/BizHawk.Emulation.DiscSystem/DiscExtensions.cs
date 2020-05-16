using System;

namespace BizHawk.Emulation.DiscSystem
{
	public static class DiscExtensions
	{
		public static Disc Create(this DiscType diskType, string discPath, Action<string> errorCallback)
		{
			//--- load the disc in a context which will let us abort if it's going to take too long
			var discMountJob = new DiscMountJob { IN_FromPath = discPath, IN_SlowLoadAbortThreshold = 8 };
			discMountJob.Run();
			var disc = discMountJob.OUT_Disc;
			if (disc == null)
			{
				throw new InvalidOperationException($"Can't find the file specified: {discPath}");
			}

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

			if (discType != diskType)
			{
				errorCallback($"Not a {diskType} disc");
				return null;
			}

			return disc;
		}
	}
}
