using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    public class MultitrackRecording
    {
        public bool isActive;        
        public int CurrentPlayer;
        public bool RecordAll;
        public MultitrackRecording()
        {
            isActive = false;
            CurrentPlayer = 0;
            RecordAll = false;
        }
    }    
}
