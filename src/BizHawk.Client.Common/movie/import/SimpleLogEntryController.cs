using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.movie.import
{
	public class SimpleLogEntryController : SimpleController, ILogEntryController
	{
		public SimpleLogEntryController(ControllerDefinition definition, string systemId) : base(definition)
		{
			LogEntryGenerator = new Bk2LogEntryGenerator(systemId, this);
		}

		public Bk2LogEntryGenerator LogEntryGenerator { get; }
	}
}
