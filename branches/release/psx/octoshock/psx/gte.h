#pragma once

namespace MDFN_IEN_PSX
{

void GTE_Init(void) MDFN_COLD;
void GTE_Power(void) MDFN_COLD;

int32 GTE_Instruction(uint32 instr);

void GTE_WriteCR(unsigned int which, uint32 value);
void GTE_WriteDR(unsigned int which, uint32 value);

uint32 GTE_ReadCR(unsigned int which);
uint32 GTE_ReadDR(unsigned int which);


}

