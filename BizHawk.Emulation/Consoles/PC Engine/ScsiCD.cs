using System;

namespace BizHawk.Emulation
{
    public sealed class ScsiCD
    {
        private bool bsy, sel, cd, io, msg, req, ack, atn, rst;
        private bool lagBSY, lagSEL, lagCD, lagIO, lagMSG, lagREQ, lagACK, lagATN, lagRST;

        public bool BSY { 
            get { return bsy; } 
            set { 
                if (value != lagBSY) signalsChanged = true;
                lagBSY = bsy;
                bsy = value;
            } 
        }
        public bool SEL
        {
            get { return sel; }
            set
            {
                if (value != lagSEL) signalsChanged = true;
                lagSEL = sel;
                sel = value;
            }
        }
        public bool CD // false=data, true=control
        {
            get { return cd; }
            set
            {
                if (value != lagCD) signalsChanged = true;
                lagCD = cd;
                cd = value;
            }
        }
        public bool IO
        {
            get { return io; }
            set
            {
                if (value != lagIO) signalsChanged = true;
                lagIO = io;
                io = value;
            }
        }
        public bool MSG
        {
            get { return msg; }
            set
            {
                if (value != lagMSG) signalsChanged = true;
                lagMSG = msg;
                msg = value;
            }
        }
        public bool REQ
        {
            get { return req; }
            set
            {
                if (value != lagREQ) signalsChanged = true;
                lagREQ = req;
                req = value;
            }
        }
        public bool ACK
        {
            get { return ack; }
            set
            {
                if (value != lagACK) signalsChanged = true;
                lagACK = ack;
                ack = value;
            }
        }
        public bool ATN
        {
            get { return atn; }
            set
            {
                if (value != lagATN) signalsChanged = true;
                lagATN = atn;
                atn = value;
            }
        }
        public bool RST
        {
            get { return rst; }
            set
            {
                if (value != lagRST) signalsChanged = true;
                lagRST = rst;
                rst = value;
            }
        }
        public byte DB  { get; set; } // data bits

        private bool signalsChanged;
        private bool driveSelected;

        public void Think()
        {
            if (BSY == false && SEL == false)
            {
                Console.WriteLine("BUS FREE!"); 
                // zap the rest of the signals
                CD = false;
                IO = false;
                MSG = false;
                REQ = false;
                ACK = false;
                ATN = false;
                DB = 0;
            }

            if (SEL && lagSEL == false)
            {
                driveSelected = true;
                CD = true;
                BSY = true;
                MSG = false;
                IO = false;
                REQ = true;
            }

            if (RST && lagRST == false)
            {
                // reset buffers and CDDA and stuff.
                CD = false;
                IO = false;
                MSG = false;
                REQ = false;
                ACK = false;
                ATN = false;
                DB = 0;
            }
        }

    /*  SCSI BUS SIGNALS 
           
    BSY (BUSY).  An "OR-tied" signal that indicates that the bus is being used.

    SEL (SELECT).  A signal used by an initiator to select a target or by a target 
    to reselect an initiator.

    C/D  (CONTROL/DATA).  A signal driven by a target that indicates whether 
    CONTROL or DATA information is on the DATA BUS.  True indicates CONTROL.

    I/O (INPUT/OUTPUT).  A signal driven by a target that controls the direction 
    of data movement on the DATA BUS with respect to an initiator. True indicates 
    input to the initiator.  This signal is also used to distinguish between 
    SELECTION and RESELECTION phases.

    MSG (MESSAGE).  A signal driven by a target during the MESSAGE phase.

    REQ (REQUEST).  A signal driven by a target to indicate a request for a 
    REQ/ACK data transfer handshake.

    ACK (ACKNOWLEDGE).  A signal driven by an initiator to indicate an 
    acknowledgment for a REQ/ACK data transfer handshake.

    ATN (ATTENTION).  A signal driven by an initiator to indicate the ATTENTION 
    condition.

    RST (RESET).  An "OR-tied" signal that indicates the RESET condition.

    Plus 8 data bits (DB7-0) and parity (P).

    ==============================================================================
                                                 Signals
                        ----------------------------------------------------------
                                              C/D, I/O,
    Bus Phase           BSY       SEL         MSG, REQ     ACK/ATN       DB(7-0,P)
    ------------------------------------------------------------------------------
    BUS FREE            None      None        None         None          None
    ARBITRATION         All       Winner      None         None          SCSI ID
    SELECTION           I&T       Initiator   None         Initiator     Initiator
    RESELECTION         I&T       Target      Target       Initiator     Target
    COMMAND             Target    None        Target       Initiator     Initiator
    DATA IN             Target    None        Target       Initiator     Target
    DATA OUT            Target    None        Target       Initiator     Initiator
    STATUS              Target    None        Target       Initiator     Target
    MESSAGE IN          Target    None        Target       Initiator     Target
    MESSAGE OUT         Target    None        Target       Initiator     Initiator
    ==============================================================================
     
     */

    }
}
