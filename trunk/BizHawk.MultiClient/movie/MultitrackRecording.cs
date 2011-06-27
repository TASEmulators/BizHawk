using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    public class MultitrackRecording
    {
        public bool IsActive;        
        public int CurrentPlayer;
        public bool RecordAll;
        public MultitrackRecording()
        {
            IsActive = false;
            CurrentPlayer = 0;
            RecordAll = false;
        }
    }    
}
