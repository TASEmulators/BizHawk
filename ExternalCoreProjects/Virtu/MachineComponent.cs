using Newtonsoft.Json;

namespace Jellyfish.Virtu
{
	public abstract class MachineComponent
	{
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

		internal virtual void Uninitialize()
		{
		}

		[field: JsonIgnore]
		protected Machine Machine { get; set; }
	}
}
