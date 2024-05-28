auto PIF::serialize(serializer& s) -> void {
  s(ram);
  s(state);
  s(intram);
  s(io.romLockout);
  s(io.resetEnabled);
}

auto PIF::Intram::serialize(serializer& s) -> void {
  s(osInfo);
  s(cpuChecksum);
  s(cicChecksum);
  s(bootTimeout);
  s(joyAddress);
  for(auto i: range(5)) s(joyStatus[i].skip), s(joyStatus[i].reset);
}
