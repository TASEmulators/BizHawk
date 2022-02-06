auto System::serialize(bool synchronize) -> serializer {
  serializer s;

  u32  signature = SerializerSignature;
  char version[16] = {};
  char description[512] = {};
  memory::copy(&version, (const char*)SerializerVersion, SerializerVersion.size());

  s(signature);
  s(synchronize);
  s(version);
  s(description);

  serialize(s, synchronize);
  return s;
}

auto System::unserialize(serializer& s) -> bool {
  u32  signature = 0;
  bool synchronize = true;
  char version[16] = {};
  char description[512] = {};

  s(signature);
  s(synchronize);
  s(version);
  s(description);

  if(signature != SerializerSignature) return false;
  if(string{version} != SerializerVersion) return false;

  if(synchronize) power(/* reset = */ false);
  serialize(s, synchronize);
  return true;
}

auto System::serialize(serializer& s, bool synchronize) -> void {
  s(queue);
  s(cartridge);
  s(controllerPort1);
  s(controllerPort2);
  s(controllerPort3);
  s(controllerPort4);
  s(rdram);
  s(mi);
  s(vi);
  s(ai);
  s(pi);
  s(ri);
  s(si);
  s(cpu);
  s(rdp);
  s(rsp);
  s(dd);
}
