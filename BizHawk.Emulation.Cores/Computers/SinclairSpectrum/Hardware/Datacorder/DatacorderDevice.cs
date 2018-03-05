using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Represents the tape device (or build-in datacorder as it was called +2 and above)
    /// </summary>
    public class DatacorderDevice
    {
        #region Construction

        private SpectrumBase _machine { get; set; }
        private Z80A _cpu { get; set; }
        private Buzzer _buzzer { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public DatacorderDevice()
        {
            
        }

        /// <summary>
        /// Initializes the datacorder device
        /// </summary>
        /// <param name="machine"></param>
        public void Init(SpectrumBase machine)
        {
            _machine = machine;
            _cpu = _machine.CPU;
            _buzzer = machine.BuzzerDevice;
        }

        #endregion

        #region State Information

        /// <summary>
        /// The index of the current tape data block that is loaded
        /// </summary>
        private int _currentDataBlockIndex = 0;
        public int CurrentDataBlockIndex
        {
            get
            {
                if (_dataBlocks.Count() > 0) { return _currentDataBlockIndex; }
                else { return -1; }
            }
            set
            {
                if (value == _currentDataBlockIndex) { return; }
                if (value < _dataBlocks.Count() && value >= 0)
                {
                    _currentDataBlockIndex = value;
                    _position = 0;
                }
            }
        }

        /// <summary>
        /// The current position within the current data block
        /// </summary>
        private int _position = 0;
        public int Position
        {
            get
            {
                if (_position >= _dataBlocks[_currentDataBlockIndex].DataPeriods.Count) { return 0; }
                else { return _position; }
            }
        }

        /// <summary>
        /// Signs whether the tape is currently playing or not
        /// </summary>
        private bool _tapeIsPlaying = false;
        public bool TapeIsPlaying
        {
            get { return _tapeIsPlaying; }
        }

        /// <summary>
        /// A list of the currently loaded data blocks
        /// </summary>
        private List<TapeDataBlock> _dataBlocks = new List<TapeDataBlock>();
        public List<TapeDataBlock> DataBlocks
        {
            get { return _dataBlocks; }
            set { _dataBlocks = value; }
        }

        /// <summary>
        /// Stores the last CPU t-state value
        /// </summary>
        private long _lastCycle = 0;

        /// <summary>
        /// 
        /// </summary>
        private int _waitEdge = 0;

        /// <summary>
        /// 
        /// </summary>
        private bool currentState = false;

        #endregion

        #region Datacorder Device Settings

        /// <summary>
        /// Signs whether the device should autodetect when the Z80 has entered into
        /// 'load' mode and auto-play the tape if neccesary
        /// </summary>
        public bool AutoPlay { get; set; }

        #endregion

        #region Tape Controls

        /// <summary>
        /// Starts the tape playing from the beginning of the current block
        /// </summary>
        public void Play()
        {
            if (_tapeIsPlaying)
                return;

            // update the lastCycle
            _lastCycle = _cpu.TotalExecutedCycles;

            // reset waitEdge and position
            _waitEdge = 0;
            _position = 0;

            if (
                _dataBlocks.Count > 0 &&        // data blocks are present &&
                _currentDataBlockIndex >= 0     // the current data block index is 1 or greater
                )
            {
                while (_position >= _dataBlocks[_currentDataBlockIndex].DataPeriods.Count)
                {
                    // we are at the end of a data block - move to the next
                    _position = 0;
                    _currentDataBlockIndex++;

                    // are we at the end of the tape?
                    if (_currentDataBlockIndex >= _dataBlocks.Count)
                    {
                        break;
                    }
                }

                // check for end of tape
                if (_currentDataBlockIndex >= _dataBlocks.Count)
                {
                    // end of tape reached. Rewind to beginning
                    RTZ();
                    return;
                }

                // update waitEdge with the current position in the current block
                _waitEdge = _dataBlocks[_currentDataBlockIndex].DataPeriods[_position];

                // sign that the tape is now playing
                _tapeIsPlaying = true;
            }            
        }

        /// <summary>
        /// Stops the tape
        /// (should move to the beginning of the next block)
        /// </summary>
        public void Stop()
        {
            if (!_tapeIsPlaying)
                return;

            // sign that the tape is no longer playing
            _tapeIsPlaying = false;

            if (
                _currentDataBlockIndex >= 0 &&                                              // we are at datablock 1 or above
                _position >= _dataBlocks[_currentDataBlockIndex].DataPeriods.Count - 1      // the block is still playing back
                )
            {
                // move to the next block
                _currentDataBlockIndex++;

                if (_currentDataBlockIndex >= _dataBlocks.Count())
                {
                    _currentDataBlockIndex = -1;
                }

                // reset waitEdge and position
                _waitEdge = 0;
                _position = 0;

                if (
                    _currentDataBlockIndex < 0 &&       // block index is -1
                    _dataBlocks.Count() > 0             // number of blocks is greater than 0
                    )
                {
                    // move the index on to 0
                    _currentDataBlockIndex = 0;
                }
            }

            // update the lastCycle
            _lastCycle = _cpu.TotalExecutedCycles;

        }

        /// <summary>
        /// Rewinds the tape to it's beginning (return to zero)
        /// </summary>
        public void RTZ()
        {            
            Stop();
            _currentDataBlockIndex = 0;
        }

        /// <summary>
        /// Inserts a new tape and sets up the tape device accordingly
        /// </summary>
        /// <param name="tapeData"></param>
        public void LoadTape(byte[] tapeData)
        {
            // attempt TZX deserialization
            TzxSerializer tzxSer = new TzxSerializer(this);
            try
            {
                tzxSer.DeSerialize(tapeData);
                return;
            }
            catch (Exception ex)
            {
                // TAP format not detected
                var e = ex;
            }

            // attempt TAP deserialization
            TapSerializer tapSer = new TapSerializer(this);
            try
            {
                tapSer.DeSerialize(tapeData);
                return;
            }
            catch (Exception ex)
            {
                // TAP format not detected
                var e = ex;
            }
        }

        /// <summary>
        /// Resets the tape
        /// </summary>
        public void Reset()
        {
            RTZ();
        }

        #endregion

        #region Tape Device Methods

        /// <summary>
        /// Simulates the spectrum 'EAR' input reading data from the tape
        /// </summary>
        /// <param name="cpuCycles"></param>
        /// <returns></returns>
        public bool GetEarBit(long cpuCycle)
        {
            // decide how many cycles worth of data we are capturing
            long cycles = cpuCycle - _lastCycle;

            // check whether tape is actually playing
            if (_tapeIsPlaying == false)
            {
                // it's not playing. Update lastCycle and return
                _lastCycle = cpuCycle;
                return false;
            }

            // check for end of tape
            if (_currentDataBlockIndex < 0)
            {
                // end of tape reached - RTZ (and stop)
                RTZ();
                return currentState;
            }

            // process the cycles based on the waitEdge
            while (cycles >= _waitEdge)
            {
                // decrement cycles
                cycles -= _waitEdge;

                // flip the current state
                currentState = !currentState;

                // increment the current period position
                _position++;
                
                if (_position >= _dataBlocks[_currentDataBlockIndex].DataPeriods.Count())
                {
                    // we have reached the end of the current block

                    // check for any commands
                    var command = _dataBlocks[_currentDataBlockIndex].Command;
                    var block = _dataBlocks[_currentDataBlockIndex];
                    switch (command)
                    {
                        // Stop the tape command found - if this is the end of the tape RTZ
                        // otherwise just STOP and move to the next block
                        case TapeCommand.STOP_THE_TAPE:
                            if (_currentDataBlockIndex >= _dataBlocks.Count())
                                RTZ();
                            else
                            {
                                Stop();
                            }
                            break;
                        case TapeCommand.STOP_THE_TAPE_48K:
                            if (_currentDataBlockIndex >= _dataBlocks.Count())
                                RTZ();
                            else
                            {
                                Stop();
                            }
                            break;
                    }

                    // skip any empty blocks
                    while (_position >= _dataBlocks[_currentDataBlockIndex].DataPeriods.Count())
                    {
                        _position = 0;
                        _currentDataBlockIndex++;
                        if (_currentDataBlockIndex >= _dataBlocks.Count())
                        {
                            break;
                        }
                    }

                    // check for end of tape
                    if (_currentDataBlockIndex >= _dataBlocks.Count())
                    {
                        _currentDataBlockIndex = -1;
                        RTZ();
                        return currentState;
                    }
                }

                // update waitEdge with current position within the current block
                _waitEdge = _dataBlocks[_currentDataBlockIndex].DataPeriods[_position];                

            }

            // update lastCycle and return currentstate
            _lastCycle = cpuCycle - (long)cycles;

            // play the buzzer
            _buzzer.ProcessPulseValue(true, currentState);

            return currentState;
        }

        #endregion

        #region Media Serialization



        #endregion

        #region State Serialization

        private int _tempBlockCount;

        /// <summary>
        /// Bizhawk state serialization
        /// </summary>
        /// <param name="ser"></param>
        public void SyncState(Serializer ser)
        {
            ser.BeginSection("DatacorderDevice");

            ser.Sync("_currentDataBlockIndex", ref _currentDataBlockIndex);
            ser.Sync("_position", ref _position);
            ser.Sync("_tapeIsPlaying", ref _tapeIsPlaying);
            ser.Sync("_lastCycle", ref _lastCycle);
            ser.Sync("_waitEdge", ref _waitEdge);
            ser.Sync("currentState", ref currentState);

            //_dataBlocks
            /*
            ser.BeginSection("Datablocks");

            if (ser.IsWriter)
            {
                _tempBlockCount = _dataBlocks.Count();
                ser.Sync("_tempBlockCount", ref _tempBlockCount);

                for (int i = 0; i < _tempBlockCount; i++)
                {
                    _dataBlocks[i].SyncState(ser, i);
                }
            }
            else
            {
                ser.Sync("_tempBlockCount", ref _tempBlockCount);
            }
           
            

            ser.EndSection();          
             */

            ser.EndSection();
        }

        #endregion
    }
}
