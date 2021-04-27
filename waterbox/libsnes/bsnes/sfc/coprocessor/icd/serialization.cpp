auto ICD::serialize(serializer& s) -> void {
  Thread::serialize(s);

  auto size = GB_get_save_state_size(&sameboy);
  auto data = new uint8_t[size];
  if(s.mode() == serializer::Save) {
    GB_save_state_to_buffer(&sameboy, data);
  }
  s.array(data, size);
  if(s.mode() == serializer::Load) {
    GB_load_state_from_buffer(&sameboy, data, size);
  }
  delete[] data;

  for(auto n : range(64)) s.array(packet[n].data);
  s.integer(packetSize);

  s.integer(joypID);
  s.integer(joypLock);
  s.integer(pulseLock);
  s.integer(strobeLock);
  s.integer(packetLock);
  s.array(joypPacket.data);
  s.integer(packetOffset);
  s.integer(bitData);
  s.integer(bitOffset);

  s.array(output);
  s.integer(readBank);
  s.integer(readAddress);
  s.integer(writeBank);

  s.integer(r6003);
  s.integer(r6004);
  s.integer(r6005);
  s.integer(r6006);
  s.integer(r6007);
  s.array(r7000);
  s.integer(mltReq);

  s.integer(hcounter);
  s.integer(vcounter);
}
