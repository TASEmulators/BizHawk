using System;
using Jellyfish.Library;

namespace Jellyfish.Virtu.Services
{
    public abstract class MachineService : DisposableBase
    {
        protected MachineService(Machine machine)
        {
            Machine = machine;

            _debugService = new Lazy<DebugService>(() => Machine.Services.GetService<DebugService>());
        }

        protected Machine Machine { get; private set; }
        protected DebugService DebugService { get { return _debugService.Value; } }

        private Lazy<DebugService> _debugService;
    }
}
