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

		public virtual void Initialize()
		{
		}

		public virtual void Reset()
		{
		}

		public virtual void Uninitialize()
		{
		}

		[field: JsonIgnore]
		protected Machine Machine { get; set; }
	}
}
