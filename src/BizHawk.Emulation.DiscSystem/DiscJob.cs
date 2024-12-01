using System.IO;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Returns ERRORS for things which will can't be processed any further. Processing of this job will continue though to try to collect the maximum amount of feedback.
	/// Returns WARNINGS for things which will are irregular or erroneous but later jobs might be able to handle, or which can be worked around by configuration assumptions.
	/// TODO - make IDisposable so I don't have to remember to Finish() it?
	/// </summary>
	public abstract class DiscJob
	{
		internal int CurrentLine = -1;

		internal StringWriter swLog = new StringWriter();
		internal void Warn(string format, params object[] args) { Log("WARN ", format, args); }
		internal void Error(string format, params object[] args) { OUT_ErrorLevel = true; Log("ERROR", format, args); }
		internal void Log(string level, string format, params object[] args)
		{
			var msg = string.Format(format, args);
			if (CurrentLine == -1)
				swLog.WriteLine("{0}: {1}", level, msg);
			else
				swLog.WriteLine("[{0,3}] {1}: {2}", CurrentLine, level, msg);
		}

		/// <summary>
		/// Whether there were any errors
		/// </summary>
		public bool OUT_ErrorLevel { get; private set; } = false;

		/// <summary>
		/// output: log transcript of the job
		/// </summary>
		public string OUT_Log { get; private set; }

		/// <summary>
		/// Finishes logging. Flushes the output and closes the logging mechanism
		/// </summary>
		internal void FinishLog()
		{
			OUT_Log = swLog.ToString();
			swLog.Close();
		}

		/// <summary>
		/// Concatenates the log results from the provided job into this log
		/// </summary>
		public void ConcatenateJobLog(DiscJob job)
		{
			OUT_ErrorLevel |= job.OUT_ErrorLevel;
			swLog.Write(job.OUT_Log);
		}

		public abstract void Run();
	}

	public sealed class DiscJobAbortException : Exception
	{
	}
}