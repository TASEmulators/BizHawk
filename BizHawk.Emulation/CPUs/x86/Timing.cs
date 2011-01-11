namespace BizHawk.Emulation.CPUs.x86
{
    public partial class x86<CpuType> where CpuType : struct, x86CpuType
    {
        // TODO test if static on these is a performance boost
        // it would be appropriate if so because closed types have their own static variables
        private int timing_mov_ri8;
        private int timing_mov_ri16;

        private void InitTiming()
        {
            if (typeof(CpuType) == typeof(Intel8086))
            {
                timing_mov_ri8  = 4;
                timing_mov_ri16 = 4;
            }
        }
    }
}
