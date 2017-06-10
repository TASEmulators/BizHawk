void op_io();
uint8 op_read(uint32 addr, eCDLog_Flags flags = eCDLog_Flags_CPUData);
void op_write(uint32 addr, uint8 data);
alwaysinline unsigned speed(unsigned addr) const;
