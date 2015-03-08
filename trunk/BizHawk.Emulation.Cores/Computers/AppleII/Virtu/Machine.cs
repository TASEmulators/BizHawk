using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Jellyfish.Virtu.Services;

namespace Jellyfish.Virtu
{
    public enum MachineState { Stopped = 0, Starting, Running, Pausing, Paused, Stopping }

    public enum Buttons : ulong
    {
        Up     = 0x0000000000001,
        Down   = 0x0000000000002,
        Left   = 0x0000000000004,
        Right  = 0x0000000000008,
        Tab    = 0x0000000000010,
        Enter  = 0x0000000000020,
        Escape = 0x0000000000040,
        Back   = 0x0000000000080,
        Space  = 0x0000000000100,
        Ctrl   = 0x0000000000200,
        Shift  = 0x0000000000400,
        Caps   = 0x0000000000800,
        Key1   = 0x0000000001000,
        Key2   = 0x0000000002000,
        Key3   = 0x0000000004000,
        Key4   = 0x0000000008000,
        Key5   = 0x0000000010000,
        Key6   = 0x0000000020000,
        Key7   = 0x0000000040000,
        Key8   = 0x0000000080000,
        Key9   = 0x0000000100000,
        Key0   = 0x0000000200000,
        KeyA   = 0x0000001000000,
        KeyB   = 0x0000002000000, 
        KeyC   = 0x0000004000000,
        KeyD   = 0x0000008000000,
        KeyE   = 0x0000010000000,
        KeyF   = 0x0000020000000,
        KeyG   = 0x0000040000000,
        KeyH   = 0x0000080000000,
        KeyI   = 0x0000100000000,
        KeyJ   = 0x0000200000000,
        KeyK   = 0x0000400000000,
        KeyL   = 0x0000800000000,
        KeyM   = 0x0001000000000,
        KeyN   = 0x0002000000000,
        KeyO   = 0x0004000000000,
        KeyP   = 0x0008000000000,
        KeyQ   = 0x0010000000000,
        KeyR   = 0x0020000000000,
        KeyS   = 0x0040000000000,
        KeyT   = 0x0080000000000,
        KeyU   = 0x0100000000000,
        KeyV   = 0x0200000000000,
        KeyW   = 0x0400000000000,
        KeyX   = 0x0800000000000,
        KeyY   = 0x1000000000000,
        KeyZ   = 0x2000000000000
    }

    public sealed class Machine : IDisposable
    {
        public Machine()
        {
            Events = new MachineEvents();
            Services = new MachineServices();

            Cpu = new Cpu(this);
            Memory = new Memory(this);
            Keyboard = new Keyboard(this);
            GamePort = new GamePort(this);
            Cassette = new Cassette(this);
            Speaker = new Speaker(this);
            Video = new Video(this);
            NoSlotClock = new NoSlotClock(this);

            var emptySlot = new PeripheralCard(this);
            Slot1 = emptySlot;
            Slot2 = emptySlot;
            Slot3 = emptySlot;
            Slot4 = emptySlot;
            Slot5 = emptySlot;
            Slot6 = new DiskIIController(this);
            Slot7 = emptySlot;

            Slots = new Collection<PeripheralCard> { null, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7 };
            Components = new Collection<MachineComponent> { Cpu, Memory, Keyboard, GamePort, Cassette, Speaker, Video, NoSlotClock, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7 };

            BootDiskII = Slots.OfType<DiskIIController>().Last();

            //Thread = new Thread(Run) { Name = "Machine" };
        }

        public void Dispose()
        {
            _pauseEvent.Close();
            _unpauseEvent.Close();
        }

        public void Reset()
        {
            foreach (var component in Components)
            {
                _debugService.WriteMessage("Resetting machine '{0}'", component.GetType().Name);
                component.Reset();
                //_debugService.WriteMessage("Reset machine '{0}'", component.GetType().Name);
            }
        }

				//[SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Jellyfish.Virtu.Services.DebugService.WriteMessage(System.String)")]
				//public void Start()
				//{
				//    _debugService = Services.GetService<DebugService>();
				//    _storageService = Services.GetService<StorageService>();

				//    _debugService.WriteMessage("Starting machine");
				//    State = MachineState.Starting;
				//    Thread.Start();
				//}

				//[SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Jellyfish.Virtu.Services.DebugService.WriteMessage(System.String)")]
				//public void Pause()
				//{
				//    _debugService.WriteMessage("Pausing machine");
				//    State = MachineState.Pausing;
				//    _pauseEvent.WaitOne();
				//    State = MachineState.Paused;
				//    _debugService.WriteMessage("Paused machine");
				//}

				//[SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Jellyfish.Virtu.Services.DebugService.WriteMessage(System.String)")]
				//public void Unpause()
				//{
				//    _debugService.WriteMessage("Running machine");
				//    State = MachineState.Running;
				//    _unpauseEvent.Set();
				//}

				//[SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Jellyfish.Virtu.Services.DebugService.WriteMessage(System.String)")]
				//public void Stop()
				//{
				//    _debugService.WriteMessage("Stopping machine");
				//    State = MachineState.Stopping;
				//    _unpauseEvent.Set();
				//    if (Thread.IsAlive)
				//    {
				//        Thread.Join();
				//    }
				//    State = MachineState.Stopped;
				//    _debugService.WriteMessage("Stopped machine");
				//}

        private void Initialize()
        {
            foreach (var component in Components)
            {
                _debugService.WriteMessage("Initializing machine '{0}'", component.GetType().Name);
                component.Initialize();
                //_debugService.WriteMessage("Initialized machine '{0}'", component.GetType().Name);
            }
        }

        private void LoadState()
        {
#if WINDOWS
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                string name = args[1];
                Func<string, Action<Stream>, bool> loader = StorageService.LoadFile;

                if (name.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(6);
                    loader = StorageService.LoadResource;
                }

                if (name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                {
                    loader(name, stream => LoadState(stream));
                }
                else if (name.EndsWith(".prg", StringComparison.OrdinalIgnoreCase))
                {
                    loader(name, stream => Memory.LoadPrg(stream));
                }
                else if (name.EndsWith(".xex", StringComparison.OrdinalIgnoreCase))
                {
                    loader(name, stream => Memory.LoadXex(stream));
                }
                else
                {
                    loader(name, stream => BootDiskII.BootDrive.InsertDisk(name, stream, false));
                }
            }
            else
#endif
            if (!_storageService.Load(Machine.StateFileName, stream => LoadState(stream)))
            {
                StorageService.LoadResource("Disks/Default.dsk", stream => BootDiskII.BootDrive.InsertDisk("Default.dsk", stream, false));
            }
        }

        private void LoadState(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                string signature = reader.ReadString();
                var version = new Version(reader.ReadString());
                if ((signature != StateSignature) || (version != new Version(Machine.Version))) // avoid state version mismatch (for now)
                {
                    throw new InvalidOperationException();
                }
                foreach (var component in Components)
                {
                    _debugService.WriteMessage("Loading machine '{0}'", component.GetType().Name);
                    component.LoadState(reader, version);
                    //_debugService.WriteMessage("Loaded machine '{0}'", component.GetType().Name);
                }
            }
        }

        private void SaveState()
        {
            _storageService.Save(Machine.StateFileName, stream => SaveState(stream));
        }

        private void SaveState(Stream stream)
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(StateSignature);
                writer.Write(Machine.Version);
                foreach (var component in Components)
                {
                    _debugService.WriteMessage("Saving machine '{0}'", component.GetType().Name);
                    component.SaveState(writer);
                    //_debugService.WriteMessage("Saved machine '{0}'", component.GetType().Name);
                }
            }
        }

        private void Uninitialize()
        {
            foreach (var component in Components)
            {
                _debugService.WriteMessage("Uninitializing machine '{0}'", component.GetType().Name);
                component.Uninitialize();
                //_debugService.WriteMessage("Uninitialized machine '{0}'", component.GetType().Name);
            }
        }

				//[SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Jellyfish.Virtu.Services.DebugService.WriteMessage(System.String)")]
				//private void Run() // machine thread
				//{
				//    Initialize();
				//    Reset();
				//    LoadState();
                //
				//    _debugService.WriteMessage("Running machine");
				//    State = MachineState.Running;
				//    do
				//    {
				//        do
				//        {
				//            Events.HandleEvents(Cpu.Execute());
				//        }
				//        while (State == MachineState.Running);
                //
				//        if (State == MachineState.Pausing)
				//        {
				//            _pauseEvent.Set();
				//            _unpauseEvent.WaitOne();
				//        }
				//    }
				//    while (State != MachineState.Stopping);
                //
				//    SaveState();
				//    Uninitialize();
				//}

				public void BizInitialize()
				{
					_debugService = Services.GetService<DebugService>();
					_storageService = Services.GetService<StorageService>();

					Initialize();
					Reset();
				}

				public void BizFrameAdvance()
				{
                    Services.GetService<KeyboardService>().Update();
                    Services.GetService<GamePortService>().Update();
					//frame begins at vsync.. beginning of vblank
					while (Video.IsVBlank)
						Events.HandleEvents(Cpu.Execute());
					//now, while not vblank, we're in a frame
					while (!Video.IsVBlank)
						Events.HandleEvents(Cpu.Execute());
                    
				}

				public void BizShutdown()
				{
					Uninitialize();
				}

        public const string Version = "0.9.4.0";

        public MachineEvents Events { get; private set; }
        public MachineServices Services { get; private set; }
        public MachineState State { get { return _state; } private set { _state = value; } }

        public Cpu Cpu { get; private set; }
        public Memory Memory { get; private set; }
        public Keyboard Keyboard { get; private set; }
        public GamePort GamePort { get; private set; }
        public Cassette Cassette { get; private set; }
        public Speaker Speaker { get; private set; }
        public Video Video { get; private set; }
        public NoSlotClock NoSlotClock { get; private set; }

        public PeripheralCard Slot1 { get; private set; }
        public PeripheralCard Slot2 { get; private set; }
        public PeripheralCard Slot3 { get; private set; }
        public PeripheralCard Slot4 { get; private set; }
        public PeripheralCard Slot5 { get; private set; }
        public PeripheralCard Slot6 { get; private set; }
        public PeripheralCard Slot7 { get; private set; }

        public Collection<PeripheralCard> Slots { get; private set; }
        public Collection<MachineComponent> Components { get; private set; }

        public DiskIIController BootDiskII { get; private set; }

        public Thread Thread { get; private set; }

        public Buttons Buttons;

        private const string StateFileName = "State.bin";
        private const string StateSignature = "Virtu";

        private DebugService _debugService;
        private StorageService _storageService;
        private volatile MachineState _state;

        private AutoResetEvent _pauseEvent = new AutoResetEvent(false);
        private AutoResetEvent _unpauseEvent = new AutoResetEvent(false);
    }
}
