/*
/*
 * MachineBase.cs
 * 
 * Abstraction of an emulated machine.
 * 
 * Copyright © 2003, 2004 Mike Murphy
 * 
 */
using System;
using System.IO;
using System.Reflection;

namespace EMU7800.Core
{
    public abstract class MachineBase
    {
        #region Fields

        ILogger _Logger;
        FrameBuffer _FrameBuffer;

        bool _MachineHalt;
        int _FrameHZ;
        readonly int _VisiblePitch, _Scanlines;

        protected Cart Cart { get; set; }

        #endregion

        #region Internal Properties

        internal FrameBuffer FrameBuffer
        {
            get
            {
                AssertDebug(_FrameBuffer != null);
                return _FrameBuffer;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The machine's Central Processing Unit.
        /// </summary>
        public M6502 CPU { get; protected set; }

        /// <summary>
        /// The machine's Address Space.
        /// </summary>
        public AddressSpace Mem { get; protected set; }

        /// <summary>
        /// The machine's Peripheral Interface Adaptor device.
        /// </summary>
        public PIA PIA { get; protected set; }

        /// <summary>
        /// Reports whether the machine has been halted due to an internal condition or error.
        /// </summary>
        public bool MachineHalt
        {
            get { return _MachineHalt; }
            internal set { if (value) _MachineHalt = true; }
        }

        /// <summary>
        /// The machine input state.
        /// </summary>
        public InputState InputState { get; private set; }

        /// <summary>
        /// The current frame number.
        /// </summary>
        public long FrameNumber { get; private set; }

        /// <summary>
        /// The first scanline that is visible.
        /// </summary>
        public int FirstScanline { get; private set; }

        /// <summary>
        /// Frame rate.
        /// </summary>
        public int FrameHZ
        {
            get { return _FrameHZ < 1 ? 1 : _FrameHZ; }
            set { _FrameHZ = value < 1 ? 1 : value; }
        }

        /// <summary>
        /// Number of sound samples per second.
        /// </summary>
        public int SoundSampleFrequency { get; private set; }

        /// <summary>
        /// The color palette for the configured machine.
        /// </summary>
        public int[] Palette { get; internal set; }

        /// <summary>
        /// Dumps CPU registers to the log when NOP instructions are encountered.
        /// </summary>
        public bool NOPRegisterDumping { get; set; }

        /// <summary>
        /// The configured logger sink.
        /// </summary>
        public ILogger Logger
        {
            get { return _Logger ?? (_Logger = new NullLogger()); }
            set { _Logger = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates an instance of the specified machine.
        /// </summary>
        /// <param name="machineType"></param>
        /// <param name="cart"></param>
        /// <param name="bios">7800 BIOS, optional.</param>
        /// <param name="hsc">7800 High Score cart, optional.</param>
        /// <param name="p1">Left controller, optional.</param>
        /// <param name="p2">Right controller, optional.</param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException">Cart must not be null.</exception>
        /// <exception cref="Emu7800Exception">Specified MachineType is unexpected.</exception>
        public static MachineBase Create(MachineType machineType, Cart cart, Bios7800 bios, HSC7800 hsc, Controller p1, Controller p2, ILogger logger)
        {
            if (cart == null)
                throw new ArgumentNullException("cart");

            MachineBase m;
            switch (machineType)
            {
                case MachineType.A2600NTSC:
                    m = new Machine2600NTSC(cart, logger);
                    break;
                case MachineType.A2600PAL:
                    m = new Machine2600PAL(cart, logger);
                    break;
                case MachineType.A7800NTSC:
                    m = new Machine7800NTSC(cart, bios, hsc, logger);
                    break;
                case MachineType.A7800PAL:
                    m = new Machine7800PAL(cart, bios, hsc, logger);
                    break;
                default:
                    throw new Emu7800Exception("Unexpected MachineType: " + machineType);
            }

            m.InputState.LeftControllerJack = p1;
            m.InputState.RightControllerJack = p2;

            m.Reset();

            return m;
        }

        /// <summary>
        /// Deserialize a <see cref="MachineBase"/> from the specified stream.
        /// </summary>
        /// <param name="binaryReader"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="Emu7800SerializationException"/>
        public static MachineBase Deserialize(BinaryReader binaryReader)
        {
            var context = new DeserializationContext(binaryReader);
            MachineBase m;
            try
            {
                m = context.ReadMachine();
            }
            catch (Emu7800SerializationException)
            {
                throw;
            }
            catch (TargetInvocationException ex)
            {
                // TargetInvocationException wraps exceptions that unwind an Activator.CreateInstance() frame.
                throw new Emu7800SerializationException("Serialization stream does not describe a valid machine.", ex.InnerException);
            }
            catch (Exception ex)
            {
                throw new Emu7800SerializationException("Serialization stream does not describe a valid machine.", ex);
            }
            return m;
        }

        /// <summary>
        /// Resets the state of the machine.
        /// </summary>
        public virtual void Reset()
        {
            Logger.WriteLine("Machine {0}  reset ({1} HZ  {2} scanlines)", this, FrameHZ, _Scanlines);
            FrameNumber = 0;
            _MachineHalt = false;
            InputState.ClearAllInput();
        }

        /// <summary>
        /// Computes the next machine frame, updating contents of the provided <see cref="FrameBuffer"/>.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer to contain the computed output.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException">frameBuffer is incompatible with machine.</exception>
        public virtual void ComputeNextFrame(FrameBuffer frameBuffer)
        {
            if (MachineHalt)
                return;

            InputState.CaptureInputState();

            _FrameBuffer = frameBuffer;
            FrameNumber++;

            for (var i = 0; i < _FrameBuffer.SoundBufferElementLength; i++)
                _FrameBuffer.SoundBuffer[i].ClearAll();
        }

        /// <summary>
        /// Create a <see cref="FrameBuffer"/> with compatible dimensions for this machine.
        /// </summary>
        public FrameBuffer CreateFrameBuffer()
        {
            var fb = new FrameBuffer(_VisiblePitch, _Scanlines);
            return fb;
        }

        /// <summary>
        /// Serialize the state of the machine to the specified stream.
        /// </summary>
        /// <param name="binaryWriter"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="Emu7800SerializationException"/>
        public void Serialize(BinaryWriter binaryWriter)
        {
            var context = new SerializationContext(binaryWriter);
            try
            {
                context.Write(this);
            }
            catch (Emu7800SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Emu7800SerializationException("Problem serializing specified machine.", ex);
            }
        }

        #endregion

        #region Constructors

        private MachineBase()
        {
        }

        protected MachineBase(ILogger logger, int scanLines, int firstScanline, int fHZ, int soundSampleFreq, int[] palette, int vPitch) : this()
        {
            InputState = new InputState();
            Logger = logger;
            _Scanlines = scanLines;
            FirstScanline = firstScanline;
            FrameHZ = fHZ;
            SoundSampleFrequency = soundSampleFreq;
            Palette = palette;
            _VisiblePitch = vPitch;
        }

        #endregion

        #region Serialization Members

        protected MachineBase(DeserializationContext input, int[] palette)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (palette == null)
                throw new ArgumentNullException("palette");
            if (palette.Length != 0x100)
                throw new ArgumentException("palette incorrect size, must be 256.");

            input.CheckVersion(1);
            _MachineHalt = input.ReadBoolean();
            _FrameHZ = input.ReadInt32();
            _VisiblePitch = input.ReadInt32();
            _Scanlines = input.ReadInt32();
            FirstScanline = input.ReadInt32();
            SoundSampleFrequency = input.ReadInt32();
            NOPRegisterDumping = input.ReadBoolean();
            InputState = input.ReadInputState();

            Palette = palette;
            Logger = null;
        }

        public virtual void GetObjectData(SerializationContext output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            output.WriteVersion(1);
            output.Write(_MachineHalt);
            output.Write(_FrameHZ);
            output.Write(_VisiblePitch);
            output.Write(_Scanlines);
            output.Write(FirstScanline);
            output.Write(SoundSampleFrequency);
            output.Write(NOPRegisterDumping);
            output.Write(InputState);
        }

        #endregion

        [System.Diagnostics.Conditional("DEBUG")]
        void AssertDebug(bool cond)
        {
            if (!cond)
                System.Diagnostics.Debugger.Break();
        }
    }
}
