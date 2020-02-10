using Newtonsoft.Json;

namespace Jellyfish.Virtu
{
	public abstract class MachineComponent
	{
		// ReSharper disable once UnusedMember.Global
		protected MachineComponent() { }

		protected MachineComponent(Machine machine)
		{
			Machine = machine;
		}

		internal virtual void Initialize()
		{
		}

		internal virtual void Reset()
		{
		}

		[field: JsonIgnore]
		protected Machine Machine { get; set; }
	}
}
