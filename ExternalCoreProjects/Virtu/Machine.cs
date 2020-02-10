using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfish.Virtu.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Virtu.Library;

namespace Jellyfish.Virtu
{
	public sealed class Machine : IDisposable
	{
		/// <summary>
		/// for deserialization only!!
		/// </summary>
		public Machine() { }

		public Machine(byte[] appleIIe, byte[] diskIIRom)
		{
			Events = new MachineEvents();

			Cpu = new Cpu(this);
			Memory = new Memory(this, appleIIe);
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
			Slot6 = new DiskIIController(this, diskIIRom);
			Slot7 = emptySlot;

			Slots = new List<PeripheralCard> { null, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7 };
			Components = new List<MachineComponent> { Cpu, Memory, Keyboard, GamePort, Cassette, Speaker, Video, NoSlotClock, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7 };

			BootDiskII = Slots.OfType<DiskIIController>().Last();
		}

		#region API

		public void Dispose()
		{
		}

		public void BizInitialize()
		{
			Initialize();
			Reset();
		}

		public void BizFrameAdvance(IEnumerable<string> buttons)
		{
			Lagged = true;
			DriveLight = false;

			Keyboard.SetKeys(buttons);

			// frame begins at vsync.. beginning of vblank
			while (Video.IsVBlank)
			{
				Events.HandleEvents(Cpu.Execute());
			}

			// now, while not vblank, we're in a frame
			while (!Video.IsVBlank)
			{
				Events.HandleEvents(Cpu.Execute());
			}
		}

		public void Serialize(JsonWriter w)
		{
			CreateSerializer().Serialize(w, this);
		}

		public static Machine Deserialize(JsonReader r)
		{
			return CreateSerializer().Deserialize<Machine>(r);
		}

		public void CpuExecute()
		{
			Events.HandleEvents(Cpu.Execute());
		}

		public IDictionary<string, int> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, int>
			{
				["A"] = Cpu.RA,
				["X"] = Cpu.RX,
				["Y"] = Cpu.RY,
				["S"] = Cpu.RS,
				["PC"] = Cpu.RPC,
				["Flag C"] = Cpu.FlagC ? 1 : 0,
				["Flag Z"] = Cpu.FlagZ ? 1 : 0,
				["Flag I"] = Cpu.FlagI ? 1 : 0,
				["Flag D"] = Cpu.FlagD ? 1 : 0,
				["Flag B"] = Cpu.FlagB ? 1 : 0,
				["Flag V"] = Cpu.FlagV ? 1 : 0,
				["Flag N"] = Cpu.FlagN ? 1 : 0,
				["Flag T"] = Cpu.FlagT ? 1 : 0
			};
		}

		public Cpu Cpu { get; private set; }
		public Memory Memory { get; private set; }
		public Speaker Speaker { get; private set; }
		public Video Video { get; private set; }
		public DiskIIController BootDiskII { get; private set; }
		public bool Lagged { get; set; }
		public bool DriveLight { get; set; }

		#endregion

		private void Reset()
		{
			foreach (var component in Components)
			{
				TraceWriter.Write("Resetting machine '{0}'", component.GetType().Name);
				component.Reset();
			}
		}

		private void Initialize()
		{
			foreach (var component in Components)
			{
				TraceWriter.Write("Initializing machine '{0}'", component.GetType().Name);
				component.Initialize();
			}
		}

		private static JsonSerializer CreateSerializer()
		{
			// TODO: converters could be cached for speedup

			var ser = new JsonSerializer
			{
				TypeNameHandling = TypeNameHandling.Auto,
				PreserveReferencesHandling = PreserveReferencesHandling.All, // leaving out Array is a very important problem, and means that we can't rely on a directly shared array to work.
				ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
			};

			ser.Converters.Add(new TypeTypeConverter(new[]
			{
				// all expected Types to convert are either in this assembly or mscorlib
				typeof(Machine).Assembly,
				typeof(object).Assembly
			}));

			ser.Converters.Add(new DelegateConverter());
			ser.Converters.Add(new ArrayConverter());

			var cr = new DefaultContractResolver();
			cr.DefaultMembersSearchFlags |= System.Reflection.BindingFlags.NonPublic;
			ser.ContractResolver = cr;

			return ser;
		}

		private const string Version = "0.9.4.0";

		internal MachineEvents Events { get; set; }

		internal Keyboard Keyboard { get; private set; }
		internal GamePort GamePort { get; private set; }
		internal Cassette Cassette { get; private set; }
		
		internal NoSlotClock NoSlotClock { get; private set; }

		internal PeripheralCard Slot1 { get; private set; }
		internal PeripheralCard Slot2 { get; private set; }
		internal PeripheralCard Slot3 { get; private set; }
		internal PeripheralCard Slot4 { get; private set; }
		internal PeripheralCard Slot5 { get; private set; }
		internal PeripheralCard Slot6 { get; private set; }
		internal PeripheralCard Slot7 { get; private set; }

		internal IList<PeripheralCard> Slots { get; private set; }
		internal IList<MachineComponent> Components { get; private set; }
		
	}
}
