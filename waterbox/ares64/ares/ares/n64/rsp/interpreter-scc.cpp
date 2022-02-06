auto RSP::MFC0(r32& rt, u8 rd) -> void {
  if((rd & 8) == 0) rt.u32 = Nintendo64::rsp.readWord((rd & 7) << 2);
  if((rd & 8) != 0) rt.u32 = Nintendo64::rdp.readWord((rd & 7) << 2);
}

auto RSP::MTC0(cr32& rt, u8 rd) -> void {
  if((rd & 8) == 0) Nintendo64::rsp.writeWord((rd & 7) << 2, rt.u32);
  if((rd & 8) != 0) Nintendo64::rdp.writeWord((rd & 7) << 2, rt.u32);
}
