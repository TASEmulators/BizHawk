using System;
using System.IO;
using Jellyfish.Library;
using Jellyfish.Virtu.Services;

namespace Jellyfish.Virtu
{
    public abstract class MachineComponent
    {
        protected MachineComponent(Machine machine)
        {
            Machine = machine;

            _debugService = new Lazy<DebugService>(() => Machine.Services.GetService<DebugService>());
        }

        public virtual void Initialize()
        {
        }

        public virtual void Reset()
        {
        }

        public virtual void LoadState(BinaryReader reader, Version version)
        {
        }

        public virtual void Uninitialize()
        {
        }

        public virtual void SaveState(BinaryWriter writer)
        {
        }

        protected Machine Machine { get; private set; }
        protected DebugService DebugService { get { return _debugService.Value; } }

        private Lazy<DebugService> _debugService;
    }
}
