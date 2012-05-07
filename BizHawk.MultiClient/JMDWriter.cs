using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace BizHawk.MultiClient
{
    class JMDWriter : IVideoWriter
    {
        /// <summary>
        /// carries private compression information data
        /// NYI
        /// </summary>
        class CodecToken : IDisposable
        {
            public void Dispose()
            {
            }

            // get constants from Deflater
            public int compressionlevel
            {
                get;
                private set;
            }

            public int numthreads
            {
                get;
                private set;
            }

            public CodecToken()
            {
                compressionlevel = Deflater.DEFAULT_COMPRESSION;
                numthreads = 3;
            }
        }

        CodecToken token;
        /// <summary>
        /// fps numerator, constant
        /// </summary>
        int fpsnum;
        /// <summary>
        /// fps denominator, constant
        /// </summary>
        int fpsden;

        /// <summary>
        /// audio samplerate, constant
        /// </summary>
        int audiosamplerate;
        /// <summary>
        /// audio number of channels, constant; 1 or 2 only
        /// </summary>
        int audiochannels;
        /// <summary>
        /// audio bits per sample, constant; only 16 supported
        /// </summary>
        int audiobits;

        /// <summary>
        /// actual disk file being written
        /// </summary>
        FileStream JMDfile;

        /// <summary>
        /// current timestamp offset in JMD
        /// ie, (number of ffffffffff appearances) * (ffffffff)
        /// </summary>
        UInt64 timestampoff;
        /// <summary>
        /// total number of video frames, used to calculate timestamps
        /// </summary>
        UInt64 totalframes;

        /// <summary>
        /// total number of audio samples, used to calculate timestamps
        /// </summary>
        UInt64 totalsamples;

        // movie metadata

        string gamename;
        string authors;
        UInt64 lengthms;
        UInt64 rerecords;



        /// <summary>
        /// sets default (probably wrong) parameters
        /// </summary>
        public JMDWriter()
        {
            fpsnum = 25;
            fpsden = 1;
            audiosamplerate = 22050;
            audiochannels = 1;
            audiobits = 8;
            token = null;

            gamename = "";
            authors = "";
            lengthms = 0;
            rerecords = 0;
        }

        public void Dispose()
        {
        }


        /// <summary>
        /// sets the codec token to be used for video compression
        /// </summary>
        public void SetVideoCodecToken(IDisposable token)
        {
            if (token is CodecToken)
                this.token = (CodecToken)token;
            else
                throw new ArgumentException("codec token must be of right type");
        }

        /// <summary>
        /// obtain a set of recording compression parameters
        /// </summary>
        /// <param name="hwnd">hwnd to attach to if the user is shown config dialog</param>
        /// <returns>codec token, dispose of it when you're done with it</returns>
        public IDisposable AcquireVideoCodecToken(IntPtr hwnd)
        {
            // no user interaction for now
            return new CodecToken();
        }

        /// <summary>
        /// set framerate to fpsnum/fpsden (assumed to be unchanging over the life of the stream)
        /// </summary>
        public void SetMovieParameters(int fpsnum, int fpsden)
        {
            this.fpsnum = fpsnum;
            this.fpsden = fpsden;
        }

        /// <summary>
        /// set resolution parameters (width x height)
        /// must be set before file is opened
        /// can be changed in future
        /// should always match IVideoProvider
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetVideoParameters(int width, int height)
        {
            // each frame is dumped with its resolution, so we don't care to store this or monitor it
        }

        /// <summary>
        /// set audio parameters.  cannot change later
        /// </summary>
        public void SetAudioParameters(int sampleRate, int channels, int bits)
        {
            // these are pretty arbitrary
            if (sampleRate < 8000 || sampleRate > 96000 || channels < 1 || channels > 2 || bits != 16)
                throw new ArgumentException("Audio parameters out of range!");
            audiosamplerate = sampleRate;
            audiochannels = channels;
            audiobits = bits;
        }


        /// <summary>
        /// opens a recording stream
        /// set a video codec token first.
        /// </summary>
        public void OpenFile(string baseName)
        {
            string ext = Path.GetExtension(baseName);
            if (ext == null || ext.ToLower() != "jmd")
                baseName = baseName + ".jmd";

            JMDfile = File.Open(baseName, FileMode.OpenOrCreate);
            timestampoff = 0;
            totalframes = 0;
            totalsamples = 0;

            // write JPC MAGIC
            writeBE16(0xffff);
            JMDfile.Write(Encoding.ASCII.GetBytes("JPCRRMULTIDUMP"), 0, 14);

            // write channel table
            writeBE16(3); // number of streams

            // for each stream
            writeBE16(0); // channel 0
            writeBE16(0); // video
            writeBE16(0); // no name

            writeBE16(1); // channel 1
            writeBE16(1); // pcm audio
            writeBE16(0); // no name

            writeBE16(2); // channel 2
            writeBE16(5); // metadata
            writeBE16(0); // no name

            if (gamename != null && gamename != String.Empty)
            {
                byte[] temp;
                // write metadatas
                writeBE16(2); // data channel
                writeBE32(0); // timestamp 0;
                JMDfile.WriteByte(71); // gamename
                temp = System.Text.Encoding.UTF8.GetBytes(gamename);
                writeVar(temp.Length);
                JMDfile.Write(temp, 0, temp.Length);

                writeBE16(2);
                writeBE32(0);
                JMDfile.WriteByte(65); // authors
                temp = System.Text.Encoding.UTF8.GetBytes(authors);
                writeVar(temp.Length);
                JMDfile.Write(temp, 0, temp.Length);

                writeBE16(2);
                writeBE32(0);
                JMDfile.WriteByte(76); // length
                writeVar(8);
                writeBE64(lengthms * 1000000);

                writeBE16(2);
                writeBE32(0);
                JMDfile.WriteByte(82); // rerecords
                writeVar(8);
                writeBE64(rerecords);
            }

            // start up thread
            // problem: since audio chunks and video frames both go through here, exactly how many worker gzips this
            // gives is not known without knowing how the emulator will chunk audio packets
            // this shouldn't affect results though, just performance
            threadQ = new System.Collections.Concurrent.BlockingCollection<Object>(token.numthreads * 2);
            workerT = new System.Threading.Thread(new System.Threading.ThreadStart(threadproc));
            workerT.Start();
            GzipFrameDelegate = new GzipFrameD(GzipFrame);
            astorage = new Queue<JMDPacket>();
            vstorage = new Queue<JMDPacket>();
        }

        // some of this code is copied from AviWriter... not sure how if at all it should be abstracted
        /// <summary>
        /// blocking threadsafe queue, used for communication between main program and file writing thread
        /// </summary>
        System.Collections.Concurrent.BlockingCollection<Object> threadQ;
        /// <summary>
        /// file writing thread; most of the work happens here
        /// </summary>
        System.Threading.Thread workerT;

        /// <summary>
        /// filewriting thread's loop
        /// </summary>
        void threadproc()
        {
            try
            {
                while (true)
                {
                    Object o = threadQ.Take();
                    if (o is IAsyncResult)
                        AddFrameEx(GzipFrameDelegate.EndInvoke((IAsyncResult)o));
                    else if (o is short[])
                        AddSamplesEx((short[])o);
                    else
                        // anything else is assumed to be quit time
                        return;
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("JMD Worker Thread died:\n\n" + e.ToString());
                return;

            }
        }

        /// <summary>
        /// write big endian 16 bit unsigned to JMDfile
        /// </summary>
        /// <param name="v"></param>
        void writeBE16(UInt16 v)
        {
            byte[] b = new byte[2];
            b[0] = (byte)(v >> 8);
            b[1] = (byte)(v & 255);
            JMDfile.Write(b, 0, 2);
        }

        /// <summary>
        /// write big endian 32 bit unsigned to JMDfile
        /// </summary>
        /// <param name="v"></param>
        void writeBE32(UInt32 v)
        {
            byte[] b = new byte[4];
            b[0] = (byte)(v >> 24);
            b[1] = (byte)(v >> 16);
            b[2] = (byte)(v >> 8);
            b[3] = (byte)(v & 255);
            JMDfile.Write(b, 0, 4);
        }

        /// <summary>
        /// write big endian 64 bit unsigned to JMDfile
        /// </summary>
        /// <param name="v"></param>
        void writeBE64(UInt64 v)
        {
            byte[] b = new byte[8];
            for (int i = 7; i >= 0; i--)
            {
                b[i] = (byte)(v & 255);
                v >>= 8;
            }
            JMDfile.Write(b, 0, 8);
        }

        /// <summary>
        /// write variable length number to file
        /// encoding is similar to MIDI
        /// </summary>
        /// <param name="v"></param>
        void writeVar(UInt64 v)
        {
            byte[] b = new byte[10];
            int i = 0;
            while (v > 0)
            {
                if (i > 0)
                    b[i++] = (byte)((v & 127) | 128);
                else
                    b[i++] = (byte)(v & 127);
                v /= 128;
            }
            if (i == 0)
                JMDfile.WriteByte(0);
            else
                for (; i > 0; i--)
                    JMDfile.WriteByte(b[i - 1]);
        }

        /// <summary>
        /// write variable length number to file
        /// encoding is similar to MIDI
        /// </summary>
        /// <param name="v"></param>
        void writeVar(int v)
        {
            if (v < 0)
                throw new ArgumentException("length cannot be less than 0!");
            writeVar((UInt64)v);
        }

        /// <summary>
        /// write packet, but they have to be in order!
        /// </summary>
        /// <param name="j"></param>
        void writeActual(JMDPacket j)
        {
            if (j.timestamp < timestampoff)
                throw new ArithmeticException("JMD Timestamp problem?");
            UInt64 timestampout = j.timestamp - timestampoff;
            while (timestampout > 0xffffffff)
            {
                timestampout -= 0xffffffff;
                // write timestamp skipper
                for (int i = 0; i < 6; i++)
                    JMDfile.WriteByte(0xff);
            }
            timestampoff = j.timestamp;
            writeBE16(j.stream);
            writeBE32((UInt32)timestampout);
            JMDfile.WriteByte(j.subtype);
            writeVar((UInt64)j.data.LongLength);
            JMDfile.Write(j.data, 0, j.data.Length);
        }


        // ensure outputs are in order
        // JMD packets must be in nondecreasing timestamp order, but there's no obligation
        // for us to get handed that.  this code is a bit overcomplex to handle edge cases
        // that may not be a problem with the current system?

        /// <summary>
        /// collection of JMDpackets yet to be written (audio)
        /// assumed to be in order
        /// </summary>
        Queue<JMDPacket> astorage;
        /// <summary>
        /// collection of JMDpackets yet to be written (video)
        /// assumed to be in order
        /// </summary>
        Queue<JMDPacket> vstorage;

        /// <summary>
        /// add a sound packet to the file write queue
        /// will be written when order-appropriate wrt video
        /// </summary>
        /// <param name="j"></param>
        void writesound(JMDPacket j)
        {
            while (vstorage.Count > 0)
            {
                var p = vstorage.Peek();
                if (p.timestamp <= j.timestamp)
                    writeActual(vstorage.Dequeue());
                else
                    break;
            }
            astorage.Enqueue(j);
        }

        /// <summary>
        /// add a video packet to the file write queue
        /// will be written when order-appropriate wrt audio
        /// </summary>
        /// <param name="j"></param>
        void writevideo(JMDPacket j)
        {
            while (astorage.Count > 0)
            {
                var p = astorage.Peek();
                if (p.timestamp <= j.timestamp)
                    writeActual(astorage.Dequeue());
                else
                    break;
            }
            vstorage.Enqueue(j);
        }
        /// <summary>
        /// flush all remaining JMDPackets to file
        /// call before closing the file
        /// </summary>
        void flushpackets()
        {
            while (astorage.Count > 0 && vstorage.Count > 0)
            {
                var ap = astorage.Peek();
                var av = vstorage.Peek();
                if (ap.timestamp <= av.timestamp)
                    writeActual(astorage.Dequeue());
                else
                    writeActual(vstorage.Dequeue());
            }
            while (astorage.Count > 0)
                writeActual(astorage.Dequeue());
            while (vstorage.Count > 0)
                writeActual(vstorage.Dequeue());
        }

        /// <summary>
        /// close recording stream
        /// </summary>
        public void CloseFile()
        {
            threadQ.Add(new Object()); // acts as stop message
            workerT.Join();

            flushpackets();

            JMDfile.Close();
        }

        /// <summary>
        /// makes a copy of an IVideoProvider
        /// handles conversion to a byte array suitable for compression by zlib
        /// </summary>
        class VideoCopy
        {
            public byte[] VideoBuffer;

            public int BufferWidth;
            public int BufferHeight;
            public int BackgroundColor;
            public VideoCopy(IVideoProvider c)
            {
                int[] vb = c.GetVideoBuffer();
                VideoBuffer = new byte[vb.Length * sizeof(int)];
                // we have to switch RGB ordering here
                for (int i = 0; i < vb.Length; i++)
                {
                    VideoBuffer[i * 4 + 0] = (byte)(vb[i] >> 16);
                    VideoBuffer[i * 4 + 1] = (byte)(vb[i] >> 8);
                    VideoBuffer[i * 4 + 2] = (byte)(vb[i] & 255);
                    VideoBuffer[i * 4 + 3] = 0;
                }
                //Buffer.BlockCopy(vb, 0, VideoBuffer, 0, VideoBuffer.Length);               
                BufferWidth = c.BufferWidth;
                BufferHeight = c.BufferHeight;
                BackgroundColor = c.BackgroundColor;
            }
        }

        /// <summary>
        /// deflates (zlib) a VideoCopy, returning a byte array suitable for insertion into a JMD file
        /// the byte array includes width and height dimensions at the beginning
        /// this is run asynchronously for speedup, as compressing can be slow
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        byte[] GzipFrame(VideoCopy v)
        {
            MemoryStream m = new MemoryStream();
            // write frame height and width first
            m.WriteByte((byte)(v.BufferWidth >> 8));
            m.WriteByte((byte)(v.BufferWidth & 255));
            m.WriteByte((byte)(v.BufferHeight >> 8));
            m.WriteByte((byte)(v.BufferHeight & 255));
            // NET 4.5 is needed for CompressionLevel?  what a pile of balls
            //var g = new DeflateStream(m, CompressionMode.Compress, true);
            var g = new DeflaterOutputStream(m, new ICSharpCode.SharpZipLib.Zip.Compression.Deflater(token.compressionlevel));
            g.IsStreamOwner = false; // leave memory stream open so we can pick its contents
            g.Write(v.VideoBuffer, 0, v.VideoBuffer.Length);
            g.Flush();
            g.Close();
            byte[] ret = m.GetBuffer();
            Array.Resize(ref ret, (int)m.Length);
            m.Close();
            return ret;
        }

        /// <summary>
        /// delegate for GzipFrame
        /// </summary>
        /// <param name="v">VideoCopy to compress</param>
        /// <returns>gzipped stream with width and height prepended</returns>
        delegate byte[] GzipFrameD(VideoCopy v);
        GzipFrameD GzipFrameDelegate;

        /// <summary>
        /// adds a frame to the stream
        /// </summary>
        public void AddFrame(IVideoProvider source)
        {
            if (!workerT.IsAlive)
                // signal some sort of error?
                return;
            threadQ.Add(GzipFrameDelegate.BeginInvoke(new VideoCopy(source), null, null));
        }

        /// <summary>
        /// adds audio samples to the stream
        /// no attempt is made to sync this to the video
        /// </summary>
        public void AddSamples(short[] samples)
        {
            if (!workerT.IsAlive)
                // signal some sort of error?
                return;
            threadQ.Add((short[])samples.Clone());
        }

        /// <summary>
        /// assemble JMDPacket and send to packetqueue
        /// </summary>
        /// <param name="source"></param>
        void AddFrameEx(byte[] source)
        {
            // at this point, VideoCopy contains a gzipped bytestream
            var j = new JMDPacket();
            j.stream = 0;
            j.subtype = 1; // zlib compressed
            j.data = source;
            j.timestamp = timestampcalc(fpsnum, fpsden, (UInt64)totalframes);
            totalframes++;
            writevideo(j);
        }







        /// <summary>
        /// assemble JMDPacket and send to packetqueue
        /// </summary>
        /// <param name="samples"></param>
        void AddSamplesEx(short[] samples)
        {
            if (audiochannels == 1)
                for (int i = 0; i < samples.Length; i++)
                    doaudiopacket(samples[i], samples[i]);
            else
                for (int i = 0; i < samples.Length / 2; i++)
                    doaudiopacket(samples[2 * i], samples[2 * i + 1]);
        }
        void doaudiopacket(short l, short r)
        {
            var j = new JMDPacket();
            j.stream = 1;
            j.subtype = 1; // raw PCM audio
            j.data = new byte[4];
            j.data[0] = (byte)(l >> 8);
            j.data[1] = (byte)(l & 255);
            j.data[2] = (byte)(r >> 8);
            j.data[3] = (byte)(r & 255);

            j.timestamp = timestampcalc(audiosamplerate, 1, totalsamples);
            totalsamples++;
            writesound(j);
        }

        /// <summary>
        /// represents a JMD file packet ready to be written except for sorting and timestamp offset
        /// </summary>
        class JMDPacket
        {
            public UInt16 stream;
            public UInt64 timestamp; // haven't subtracted timestampoffs yet
            public byte subtype;
            public byte[] data;
        }

        /// <summary>
        /// creates a timestamp out of fps value
        /// </summary>
        /// <param name="rate">fpsnum</param>
        /// <param name="scale">fpsden</param>
        /// <param name="pos">frame position</param>
        /// <returns></returns>
        static UInt64 timestampcalc(int rate, int scale, UInt64 pos)
        {
            // rate/scale events per second
            // timestamp is in nanoseconds
            // round down, consistent with JPC-rr apparently?
            var b = new System.Numerics.BigInteger(pos) * scale * 1000000000 / rate;

            return (UInt64)b;
        }
        /// <summary>
        /// set metadata parameters; should be called before opening file
        /// NYI
        /// </summary>
        public void SetMetaData(string gameName, string authors, UInt64 lengthMS, UInt64 rerecords)
        {
            this.gamename = gameName;
            this.authors = authors;
            this.lengthms = lengthMS;
            this.rerecords = rerecords;
        }
    }
}
