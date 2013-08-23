using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

// Emulates a Texas Instruments SN76489
// TODO the freq->note translation should be moved to a separate utility class.

namespace BizHawk.Emulation.Sound
{
    public sealed class SN76489 : ISoundProvider
    {
        public sealed class Channel
        {
            public ushort Frequency;
            public byte Volume;
            public short[] Wave;
            public bool Noise;
            public byte NoiseType;
            public float WaveOffset;
            public bool Left = true;
            public bool Right = true;

            const int SampleRate = 44100;
            static byte[] LogScale = { 0, 10, 13, 16, 20, 26, 32, 40, 51, 64, 81, 102, 128, 161, 203, 255 };

            public void Mix(short[] samples, int start, int len, int maxVolume)
            {
                if (Volume == 0) return;

                float adjustedWaveLengthInSamples = SampleRate / (Noise ? (Frequency / (float)Wave.Length) : Frequency);
                float moveThroughWaveRate = Wave.Length / adjustedWaveLengthInSamples;

                int end = start + len;
                for (int i = start; i < end; )
                {
                    short value = Wave[(int)WaveOffset];

                    samples[i++] += (short)(Left ? (value / 4 * LogScale[Volume] / 0xFF * maxVolume / short.MaxValue) : 0);
                    samples[i++] += (short)(Right ? (value / 4 * LogScale[Volume] / 0xFF * maxVolume / short.MaxValue) : 0);
                    WaveOffset += moveThroughWaveRate;
                    if (WaveOffset >= Wave.Length)
                        WaveOffset %= Wave.Length;
                }
            }
        }
        
        public Channel[] Channels = new Channel[4];
        public byte PsgLatch;

        Queue<QueuedCommand> commands = new Queue<QueuedCommand>(256);
        int frameStartTime, frameStopTime;

        const int PsgBase = 111861;

        public SN76489()
        {
            MaxVolume = short.MaxValue * 2 / 3;
            Waves.InitWaves();
            for (int i=0; i<4; i++)
            {
                Channels[i] = new Channel();
                switch (i)
                {
                    case 0:
                    case 1:
                    case 2:
                        Channels[i].Wave = Waves.ImperfectSquareWave;
                        break;
                    case 3:
                        Channels[i].Wave = Waves.NoiseWave;
                        Channels[i].Noise = true;
                        break;
                }
            }
        }

        public void Reset()
        {
            PsgLatch = 0;
            foreach (var channel in Channels)
            {
                channel.Frequency = 0;
                channel.Volume = 0;
                channel.NoiseType = 0;
                channel.WaveOffset = 0f;
            }
        }

        public void BeginFrame(int cycles)
        {
            while (commands.Count > 0)
            {
                var cmd = commands.Dequeue();
                WritePsgDataImmediate(cmd.Value);
            }
            frameStartTime = cycles;
        }

        public void EndFrame(int cycles)
        {
            frameStopTime = cycles;
        }

        public void WritePsgData(byte value, int cycles)
        {
            commands.Enqueue(new QueuedCommand {Value = value, Time = cycles-frameStartTime});
        }

        void UpdateNoiseType(int value)
        {
            Channels[3].NoiseType = (byte)(value & 0x07);
            switch (Channels[3].NoiseType & 3)
            {
                case 0: Channels[3].Frequency = PsgBase / 16; break;
                case 1: Channels[3].Frequency = PsgBase / 32; break;
                case 2: Channels[3].Frequency = PsgBase / 64; break;
                case 3: Channels[3].Frequency = Channels[2].Frequency; break;
            }
            var newWave = (value & 4) == 0 ? Waves.PeriodicWave16 : Waves.NoiseWave;
            if (newWave != Channels[3].Wave)
            {
                Channels[3].Wave = newWave;
                Channels[3].WaveOffset = 0f;
            }
        }

        void WritePsgDataImmediate(byte value)
        {
            switch (value & 0xF0)
            {
                case 0x80:
                case 0xA0:
                case 0xC0:
                    PsgLatch = value;
                    break;
                case 0xE0:
                    PsgLatch = value;
                    UpdateNoiseType(value);
                    break;
                case 0x90:
                    Channels[0].Volume = (byte)(~value & 15);
                    PsgLatch = value;
                    break;
                case 0xB0:
                    Channels[1].Volume = (byte)(~value & 15);
                    PsgLatch = value;
                    break;
                case 0xD0:
                    Channels[2].Volume = (byte)(~value & 15);
                    PsgLatch = value;
                    break;
                case 0xF0:
                    Channels[3].Volume = (byte)(~value & 15);
                    PsgLatch = value;
                    break;
                default:
                    byte channel = (byte) ((PsgLatch & 0x60) >> 5);
                    if ((PsgLatch & 16) == 0) // Tone latched
                    {
                        int f = PsgBase/(((value & 0x03F)*16) + (PsgLatch & 0x0F) + 1);
                        if (f > 15000)
                            f = 0; // upper bound of playable frequency
                        Channels[channel].Frequency = (ushort) f;
                        if ((Channels[3].NoiseType & 3) == 3 && channel == 2)
                            Channels[3].Frequency = (ushort) f;
                    } else { // volume latched
                        Channels[channel].Volume = (byte)(~value & 15);
                    }
                    break;
            }
        }

        public byte StereoPanning
        {
            get
            {
                byte value = 0;
                if (Channels[0].Left)  value |= 0x10;
                if (Channels[0].Right) value |= 0x01;
                if (Channels[1].Left)  value |= 0x20;
                if (Channels[1].Right) value |= 0x02;
                if (Channels[2].Left)  value |= 0x40;
                if (Channels[2].Right) value |= 0x04;
                if (Channels[3].Left)  value |= 0x80;
                if (Channels[3].Right) value |= 0x08;
                return value;
            }
            set
            {
                Channels[0].Left  = (value & 0x10) != 0;
                Channels[0].Right = (value & 0x01) != 0;
                Channels[1].Left  = (value & 0x20) != 0;
                Channels[1].Right = (value & 0x02) != 0;
                Channels[2].Left  = (value & 0x40) != 0;
                Channels[2].Right = (value & 0x04) != 0;
                Channels[3].Left  = (value & 0x80) != 0;
                Channels[3].Right = (value & 0x08) != 0;    
            }
        }

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[PSG]");
            writer.WriteLine("Volume0 {0:X2}", Channels[0].Volume);
            writer.WriteLine("Volume1 {0:X2}", Channels[1].Volume);
            writer.WriteLine("Volume2 {0:X2}", Channels[2].Volume);
            writer.WriteLine("Volume3 {0:X2}", Channels[3].Volume);
            writer.WriteLine("Freq0 {0:X4}", Channels[0].Frequency);
            writer.WriteLine("Freq1 {0:X4}", Channels[1].Frequency);
            writer.WriteLine("Freq2 {0:X4}", Channels[2].Frequency);
            writer.WriteLine("Freq3 {0:X4}", Channels[3].Frequency);
            writer.WriteLine("NoiseType {0:X}", Channels[3].NoiseType);
            writer.WriteLine("PsgLatch {0:X2}", PsgLatch);
            writer.WriteLine("Panning {0:X2}", StereoPanning);
            writer.WriteLine("[/PSG]");
            writer.WriteLine();
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/PSG]") break;
                if (args[0] == "Volume0")
                    Channels[0].Volume = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Volume1")
                    Channels[1].Volume = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Volume2")
                    Channels[2].Volume = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Volume3")
                    Channels[3].Volume = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Freq0")
                    Channels[0].Frequency = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Freq1")
                    Channels[1].Frequency = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Freq2")
                    Channels[2].Frequency = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Freq3")
                    Channels[3].Frequency = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "NoiseType")
                    Channels[3].NoiseType = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "PsgLatch")
                    PsgLatch = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Panning")
                    StereoPanning = byte.Parse(args[1], NumberStyles.HexNumber);

                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }
            UpdateNoiseType(Channels[3].NoiseType);
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            writer.Write(Channels[0].Volume);
            writer.Write(Channels[1].Volume);
            writer.Write(Channels[2].Volume);
            writer.Write(Channels[3].Volume);
            writer.Write(Channels[0].Frequency);
            writer.Write(Channels[1].Frequency);
            writer.Write(Channels[2].Frequency);
            writer.Write(Channels[3].Frequency);
            writer.Write(Channels[3].NoiseType);
            writer.Write(PsgLatch);
            writer.Write(StereoPanning);
        }

        public void LoadStateBinary(BinaryReader reader)
        {
            Channels[0].Volume = reader.ReadByte();
            Channels[1].Volume = reader.ReadByte();
            Channels[2].Volume = reader.ReadByte();
            Channels[3].Volume = reader.ReadByte();
            Channels[0].Frequency = reader.ReadUInt16();
            Channels[1].Frequency = reader.ReadUInt16();
            Channels[2].Frequency = reader.ReadUInt16();
            Channels[3].Frequency = reader.ReadUInt16();
            UpdateNoiseType(reader.ReadByte());
            PsgLatch = reader.ReadByte();
            StereoPanning = reader.ReadByte();
        }

        #region Frequency -> Note Conversion (for interested humans)

        public static string GetNote(int freq)
        {
            if (freq < 26) return "LOW";
            if (freq > 4435) return "HIGH";

            for (int i = 0; i < frequencies.Length - 1; i++)
            {
                if (freq >= frequencies[i + 1]) continue;
                int nextNoteDistance = frequencies[i + 1] - frequencies[i];
                int distance = freq - frequencies[i];
                if (distance < nextNoteDistance / 2)
                {
                    // note identified
                    return notes[i];
                }
            }
            return "?";
        }

        // For the curious, A4 = 440hz. Every octave is a doubling, so A5=880, A3=220
        // Each next step is a factor of the 12-root of 2. So to go up a step you multiply by 1.0594630943592952645618252949463
        // Next step from A4 is A#4. A#4 = (440.00 * 1.05946...) = 466.163...
        // Note that because frequencies must be integers, SMS games will be slightly out of pitch to a normally tuned instrument, especially at the low end.

        static readonly int[] frequencies =
            {
                27,   // A0
                29,   // A#0
                31,   // B0
                33,   // C1
                35,   // C#1
                37,   // D1
                39,   // D#1
                41,   // E1
                44,   // F1
                46,   // F#1
                49,   // G1
                52,   // G#1
                55,   // A1
                58,   // A#1
                62,   // B1
                65,   // C2
                69,   // C#2
                73,   // D2
                78,   // D#2
                82,   // E2
                87,   // F2
                92,   // F#2
                98,   // G2
                104,  // G#2
                110,  // A2
                117,  // A#2
                123,  // B2
                131,  // C3
                139,  // C#3
                147,  // D3
                156,  // D#3
                165,  // E3
                175,  // F3
                185,  // F#3
                196,  // G3
                208,  // G#3
                220,  // A3
                233,  // A#3
                247,  // B3
                262,  // C4
                277,  // C#4
                294,  // D4
                311,  // D#4
                330,  // E4
                349,  // F4
                370,  // F#4
                392,  // G4
                415,  // G#4
                440,  // A4
                466,  // A#4
                494,  // B4
                523,  // C5
                554,  // C#5
                587,  // D5
                622,  // D#5
                659,  // E5
                698,  // F5
                740,  // F#5
                784,  // G5
                831,  // G#5
                880,  // A5
                932,  // A#5
                988,  // B5
                1046, // C6
                1109, // C#6
                1175, // D6
                1245, // D#6
                1319, // E6
                1397, // F6
                1480, // F#6
                1568, // G6
                1661, // G#6
                1760, // A6
                1865, // A#6
                1976, // B6
                2093, // C7
                2217, // C#7
                2349, // D7
                2489, // D#7
                2637, // E7
                2794, // F7
                2960, // F#7
                3136, // G7
                3322, // G#7
                3520, // A7
                3729, // A#7
                3951, // B7
                4186, // C8
                4435  // C#8
            };

        static readonly string[] notes =
            {
                                                                 "A0","A#0","B0",
                "C1","C#1","D1","D#1","E1","F1","F#1","G1","G#1","A1","A#1","B1",
                "C2","C#2","D2","D#2","E2","F2","F#2","G2","G#2","A2","A#2","B2",
                "C3","C#3","D3","D#3","E3","F3","F#3","G3","G#3","A3","A#3","B3",
                "C4","C#4","D4","D#4","E4","F4","F#4","G4","G#4","A4","A#4","B4",
                "C5","C#5","D5","D#5","E5","F5","F#5","G5","G#5","A5","A#5","B5",
                "C6","C#6","D6","D#6","E6","F6","F#6","G6","G#6","A6","A#6","B6",
                "C7","C#7","D7","D#7","E7","F7","F#7","G7","G#7","A7","A#7","B7",
                "C8","HIGH"
            };

        #endregion

        public int MaxVolume { get; set; }
        public void DiscardSamples() { commands.Clear(); }
        public void GetSamples(short[] samples)
        {
            int elapsedCycles = frameStopTime - frameStartTime;
            if (elapsedCycles == 0) 
                elapsedCycles = 1; // hey it's better than diving by zero

            int start = 0;
            while (commands.Count > 0)
            {
                var cmd = commands.Dequeue();
                int pos = ((cmd.Time*samples.Length)/elapsedCycles) & ~1;
                GetSamplesImmediate(samples, start, pos-start);
                start = pos;
                WritePsgDataImmediate(cmd.Value);
            }
            GetSamplesImmediate(samples, start, samples.Length - start);
        }

        public void GetSamplesImmediate(short[] samples, int start, int len)
        {
            for (int i = 0; i < 4; i++)
                Channels[i].Mix(samples, start, len, MaxVolume);
        }

        class QueuedCommand
        {
            public byte Value;
            public int Time;
        }
    }
}