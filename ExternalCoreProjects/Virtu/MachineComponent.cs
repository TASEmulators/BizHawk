namespace Jellyfish.Virtu
{
	public abstract class MachineComponent
	{
		public MachineComponent() { }
		protected MachineComponent(Machine machine)
		{
			_machine = machine;
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

		[Newtonsoft.Json.JsonIgnore]
		private Machine _machine;

		public Machine Machine
		{
			get { return _machine; }
			set { _machine = value; }
		}
	}
}
