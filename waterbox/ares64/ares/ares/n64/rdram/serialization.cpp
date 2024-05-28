auto RDRAM::serialize(serializer& s) -> void {
  s(ram);
  for(auto& chip : chips) {
    s(chip.deviceType);
    s(chip.deviceID);
    s(chip.delay);
    s(chip.mode);
    s(chip.refreshInterval);
    s(chip.refreshRow);
    s(chip.rasInterval);
    s(chip.minInterval);
    s(chip.addressSelect);
    s(chip.deviceManufacturer);
    s(chip.currentControl);
  }
}
