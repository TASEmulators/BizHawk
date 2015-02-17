using System.Diagnostics.CodeAnalysis;

namespace Jellyfish.Virtu
{
    public sealed class Cassette : MachineComponent
    {
        public Cassette(Machine machine) :
            base(machine)
        {
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public bool ReadInput()
        {
            return false;
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public void ToggleOutput()
        {
        }
    }
}
