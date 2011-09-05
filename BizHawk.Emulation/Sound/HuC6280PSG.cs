using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace BizHawk.Emulation.Sound
{
    // Emulates PSG audio unit of a PC Engine / Turbografx-16 / SuperGrafx.
    // It is embedded on the CPU and doesn't have its own part number. None the less, it is emulated separately from the 6280 CPU.

    public sealed class HuC6280PSG : ISoundProvider
    {
        public class PSGChannel
        {
            public ushort Frequency;
            public byte Panning;
            public byte Volume;
            public bool Enabled;
            public bool NoiseChannel;
            public bool DDA;
            public ushort NoiseFreq;
            public short DDAValue;
            public short[] Wave = new short[32];
            public float SampleOffset;
        }

        public PSGChannel[] Channels = new PSGChannel[8];
        
        public byte VoiceLatch;
        byte WaveTableWriteOffset;

        Queue<QueuedCommand> commands = new Queue<QueuedCommand>(256);
        long frameStartTime, frameStopTime;

        const int SampleRate = 44100;
        const int PsgBase = 3580000;
        static byte[] LogScale = { 0, 0, 10, 10, 13, 13, 16, 16, 20, 20, 26, 26, 32, 32, 40, 40, 51, 51, 64, 64, 81, 81, 102, 102, 128, 128, 161, 161, 203, 203, 255, 255 };
        static byte[] VolumeReductionTable = { 0x1F, 0x1D, 0x1B, 0x19, 0x17, 0x15, 0x13, 0x10, 0x0F, 0x0D, 0x0B, 0x09, 0x07, 0x05, 0x03, 0x00 };

        public byte MainVolumeLeft;
        public byte MainVolumeRight;
        public int MaxVolume { get; set; }

        public HuC6280PSG()
        {
            MaxVolume = short.MaxValue;
            Waves.InitWaves();
            for (int i=0; i<8; i++)
                Channels[i] = new PSGChannel();
        }

        public void BeginFrame(long cycles)
        {
            while (commands.Count > 0)
            {
                var cmd = commands.Dequeue();
                WritePSGImmediate(cmd.Register, cmd.Value);
            }
            frameStartTime = cycles;
        }

        public void EndFrame(long cycles)
        {
            frameStopTime = cycles;
        }

        public void WritePSG(byte register, byte value, long cycles)
        {
            commands.Enqueue(new QueuedCommand { Register = register, Value = value, Time = cycles-frameStartTime });
        }

        public void WritePSGImmediate(int register, byte value)
        {
            register &= 0x0F;
            switch (register)
            {
                case 0: // Set Voice Latch
                    VoiceLatch = (byte) (value & 7);
                    break;
                case 1: // Global Volume select;
                    MainVolumeLeft  = (byte) ((value >> 4) & 0x0F);
                    MainVolumeRight = (byte) (value & 0x0F);
                    break;
                case 2: // Frequency LSB
                    Channels[VoiceLatch].Frequency &= 0xFF00;
                    Channels[VoiceLatch].Frequency |= value;
                    break;
                case 3: // Frequency MSB
                    Channels[VoiceLatch].Frequency &= 0x00FF;
                    Channels[VoiceLatch].Frequency |= (ushort)(value << 8);
                    Channels[VoiceLatch].Frequency &= 0x0FFF;
                    break;
                case 4: // Voice Volume
                    Channels[VoiceLatch].Volume = (byte) (value & 0x1F);
                    Channels[VoiceLatch].Enabled = (value & 0x80) != 0;
                    Channels[VoiceLatch].DDA = (value & 0x40) != 0;
                    if (Channels[VoiceLatch].Enabled == false && Channels[VoiceLatch].DDA)
                        WaveTableWriteOffset = 0;
                    break;
                case 5: // Panning
                    Channels[VoiceLatch].Panning = value;
                    break;
                case 6: // Wave data
                    if (Channels[VoiceLatch].DDA == false)
                    {
                        Channels[VoiceLatch].Wave[WaveTableWriteOffset++] = (short) ((value*2047) - 32767);
                        WaveTableWriteOffset &= 31;
                    } else {
                        Channels[VoiceLatch].DDAValue = (short)((value * 2047) - 32767);
                    }
                    break;
                case 7: // Noise
                    Channels[VoiceLatch].NoiseChannel = ((value & 0x80) != 0) && VoiceLatch >= 4;
                    if ((value & 0x1F) == 0x1F)
                        value &= 0xFE;
                    Channels[VoiceLatch].NoiseFreq = (ushort) (PsgBase/(64*(0x1F - (value & 0x1F))));
                    break;
                case 8: // LFO
                    // TODO: implement LFO
                    break;
                case 9: // LFO Control
                    if ((value & 0x80) == 0 && (value & 3) != 0)
                    {
                        Console.WriteLine("****************      LFO ON !!!!!!!!!!       *****************");
                        Channels[1].Enabled = false;
                    } else
                    {
                        Channels[1].Enabled = true;
                    }
                    break;
            }
        }

		public void DiscardSamples() { }
        public void GetSamples(short[] samples)
        {
            int elapsedCycles = (int) (frameStopTime - frameStartTime);
            int start = 0;
            while (commands.Count > 0)
            {
                var cmd = commands.Dequeue();
                int pos = (int) ((cmd.Time * samples.Length) / elapsedCycles) & ~1;
                MixSamples(samples, start, pos - start);
                start = pos;
                WritePSGImmediate(cmd.Register, cmd.Value);
            }
            MixSamples(samples, start, samples.Length - start);
        }

        void MixSamples(short[] samples, int start, int len)
        {
            for (int i = 0; i < 6; i++)
                MixChannel(samples, start, len, Channels[i]);
        }

        void MixChannel(short[] samples, int start, int len, PSGChannel channel)
        {
            if (channel.Enabled == false) return;
            if (channel.DDA == false && channel.Volume == 0) return;

            short[] wave = channel.Wave;
            int freq;

            if (channel.NoiseChannel)
            {
                wave = Waves.NoiseWave;
                freq = channel.NoiseFreq;
            } else if (channel.DDA) {
                freq = 0;
            } else {
                if (channel.Frequency <= 1) return;
                freq = PsgBase / (32 * ((int)channel.Frequency));
            }

            int globalPanFactorLeft = VolumeReductionTable[MainVolumeLeft];
            int globalPanFactorRight = VolumeReductionTable[MainVolumeRight];          
            int channelPanFactorLeft = VolumeReductionTable[channel.Panning >> 4];
            int channelPanFactorRight = VolumeReductionTable[channel.Panning & 0xF];
            int channelVolumeFactor = 0x1f - channel.Volume;

            int volumeLeft = 0x1F - globalPanFactorLeft - channelPanFactorLeft - channelVolumeFactor;
            if (volumeLeft < 0) 
                volumeLeft = 0;

            int volumeRight = 0x1F - globalPanFactorRight - channelPanFactorRight - channelVolumeFactor;
            if (volumeRight < 0)
                volumeRight = 0;

            float adjustedWaveLengthInSamples = SampleRate / (channel.NoiseChannel ? freq/(float)(channel.Wave.Length*128) : freq);
            float moveThroughWaveRate = wave.Length / adjustedWaveLengthInSamples;

            int end = start + len;
            for (int i=start; i<end;)
            {
                channel.SampleOffset %= wave.Length;
                short value = channel.DDA ? channel.DDAValue : wave[(int) channel.SampleOffset];

                samples[i++] += (short)(value * LogScale[volumeLeft] / 255f / 6f * MaxVolume / short.MaxValue);
                samples[i++] += (short)(value * LogScale[volumeRight] / 255f / 6f * MaxVolume / short.MaxValue);

                channel.SampleOffset += moveThroughWaveRate;
                channel.SampleOffset %= wave.Length;
            }
        }

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[PSG]");

            writer.WriteLine("MainVolumeLeft {0:X2}", MainVolumeLeft);
            writer.WriteLine("MainVolumeRight {0:X2}", MainVolumeRight);
            writer.WriteLine("VoiceLatch {0}", VoiceLatch);
            writer.WriteLine("WaveTableWriteOffset {0:X2}", WaveTableWriteOffset);
            writer.WriteLine();

            for (int i = 0; i<6; i++)
            {
                writer.WriteLine("[Channel{0}]",i+1);
                writer.WriteLine("Frequency {0:X4}", Channels[i].Frequency);
                writer.WriteLine("Panning {0:X2}", Channels[i].Panning);
                writer.WriteLine("Volume {0:X2}", Channels[i].Volume);
                writer.WriteLine("Enabled {0}", Channels[i].Enabled);
                if (i.In(4,5))
                {
                    writer.WriteLine("NoiseChannel {0}", Channels[i].NoiseChannel);
                    writer.WriteLine("NoiseFreq {0:X4}", Channels[i].NoiseFreq);
                }
                writer.WriteLine("DDA {0}",Channels[i].DDA);
                writer.WriteLine("DDAValue {0:X4}", Channels[i].DDAValue);
                writer.WriteLine("SampleOffset {0}", Channels[i].SampleOffset);
                writer.Write("Wave ");
                Channels[i].Wave.SaveAsHex(writer);
                writer.WriteLine("[/Channel{0}]\n",i+1);
            }

            writer.WriteLine("[/PSG]\n");
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/PSG]") break;
                if (args[0] == "MainVolumeLeft")
                    MainVolumeLeft = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "MainVolumeRight")
                    MainVolumeRight = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "VoiceLatch")
                    VoiceLatch = byte.Parse(args[1]);
                else if (args[0] == "WaveTableWriteOffset")
                    WaveTableWriteOffset = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "[Channel1]")
                    LoadChannelStateText(reader, 0);
                else if (args[0] == "[Channel2]")
                    LoadChannelStateText(reader, 1);
                else if (args[0] == "[Channel3]")
                    LoadChannelStateText(reader, 2);
                else if (args[0] == "[Channel4]")
                    LoadChannelStateText(reader, 3);
                else if (args[0] == "[Channel5]")
                    LoadChannelStateText(reader, 4);
                else if (args[0] == "[Channel6]")
                    LoadChannelStateText(reader, 5);
                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }
        }

        void LoadChannelStateText(TextReader reader, int channel)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/Channel"+(channel+1)+"]") break;
                if (args[0] == "Frequency")
                    Channels[channel].Frequency = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Panning")
                    Channels[channel].Panning = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Volume")
                    Channels[channel].Volume = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Enabled")
                    Channels[channel].Enabled = bool.Parse(args[1]);
                else if (args[0] == "NoiseChannel")
                    Channels[channel].NoiseChannel = bool.Parse(args[1]);
                else if (args[0] == "NoiseFreq")
                    Channels[channel].NoiseFreq = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "DDA")
                    Channels[channel].DDA = bool.Parse(args[1]);
                else if (args[0] == "DDAValue")
                    Channels[channel].DDAValue = short.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "SampleOffset")
                    Channels[channel].SampleOffset = float.Parse(args[1]);
                else if (args[0] == "Wave")
                    Channels[channel].Wave.ReadFromHex(args[1]);
                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            writer.Write(MainVolumeLeft);
            writer.Write(MainVolumeRight);
            writer.Write(VoiceLatch);
            writer.Write(WaveTableWriteOffset);

            for (int i = 0; i < 6; i++)
            {
                writer.Write(Channels[i].Frequency);
                writer.Write(Channels[i].Panning);
                writer.Write(Channels[i].Volume);
                writer.Write(Channels[i].Enabled);
                writer.Write(Channels[i].NoiseChannel);
                writer.Write(Channels[i].NoiseFreq);
                writer.Write(Channels[i].DDA);
                writer.Write(Channels[i].DDAValue);
                writer.Write(Channels[i].SampleOffset);
                for (int j = 0; j < 32; j++)
                    writer.Write(Channels[i].Wave[j]);
            }
        }

        public void LoadStateBinary(BinaryReader reader)
        {
            MainVolumeLeft = reader.ReadByte();
            MainVolumeRight = reader.ReadByte();
            VoiceLatch = reader.ReadByte();
            WaveTableWriteOffset = reader.ReadByte();

            for (int i=0; i<6; i++)
            {
                Channels[i].Frequency = reader.ReadUInt16();
                Channels[i].Panning = reader.ReadByte();
                Channels[i].Volume = reader.ReadByte();
                Channels[i].Enabled = reader.ReadBoolean();
                Channels[i].NoiseChannel = reader.ReadBoolean();
                Channels[i].NoiseFreq = reader.ReadUInt16();
                Channels[i].DDA = reader.ReadBoolean();
                Channels[i].DDAValue = reader.ReadInt16();
                Channels[i].SampleOffset = reader.ReadSingle();
                for (int j = 0; j < 32; j++)
                    Channels[i].Wave[j] = reader.ReadInt16();
            }
        }

        class QueuedCommand
        {
            public byte Register;
            public byte Value;
            public long Time;
        }
    }
}