namespace BizHawk.Emulation.Sound 
{
    public static class Waves
    {
        public static short[] SquareWave;
        public static short[] ImperfectSquareWave;
        public static short[] NoiseWave;
        public static short[] PeriodicWave16;

        public static void InitWaves()
        {
            SquareWave = new short[] 
            {
                -32768,-32768,-32768,-32768,-32768,-32768,-32768,-32768,-32768,-32768,-32768,-32768,-32768,-32768,-32768,-32768,
                 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767
            };

            ImperfectSquareWave = new short[]
            {
                -32768,-30145,-27852,-26213,-24902,-23592,-22282,-20971,-19988,-19005,-18350,-17694,-17366,-17039,-16711,-16711,
                 32767, 30145, 27852, 26213, 24902, 23592, 22282, 20971, 19988, 19005, 18350, 17694, 17366, 17039, 16711, 16711
            };

            PeriodicWave16 = new short[] { 32767, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            NoiseWave = new short[0x1000];
            var rnd = new System.Random(unchecked((int)0xDEADBEEF));
            for (int i = 0; i < NoiseWave.Length; i++)
            {
                int r = rnd.Next();
                if ((r & 1) > 0)
                    NoiseWave[i] = short.MaxValue;
            }

            /*TriangleWave = new short[512];
            for (int i = 0; i < 256; i++)
                TriangleWave[i] = (short)((ushort.MaxValue*i/256)-short.MinValue);
            for (int i = 0; i < 256; i++)
                TriangleWave[256+i] = TriangleWave[256-i];
            TriangleWave[256] = short.MaxValue;

            SawWave = new short[512];
            for (int i = 0; i < 512; i++)
                SawWave[i] = (short)((ushort.MaxValue * i / 512) - short.MinValue);

            SineWave = new short[1024];
            for (int i=0; i<1024; i++)
            {
                SineWave[i] = (short) (Math.Sin(i*Math.PI*2/1024d)*32767);
            }*/
        }
    }
}