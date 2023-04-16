auto RDRAM::readWord(u32 address) -> u32 {
  u32 chipID = address >> 13 & 3;
  auto& chip = chips[chipID];
  address = (address & 0x3ff) >> 2;
  u32 data = 0;

  if(address == 0) {
    //RDRAM_DEVICE_TYPE
    data = chip.deviceType;
  }

  if(address == 1) {
    //RDRAM_DEVICE_ID
    data = chip.deviceID;
  }

  if(address == 2) {
    //RDRAM_DELAY
    data = chip.delay;
  }

  if(address == 3) {
    //RDRAM_MODE
    data = chip.mode ^ 0xc0c0c0c0;
  }

  if(address == 4) {
    //RDRAM_REF_INTERVAL
    data = chip.refreshInterval;
  }

  if(address == 5) {
    //RDRAM_REF_ROW
    data = chip.refreshRow;
  }

  if(address == 6) {
    //RDRAM_RAS_INTERVAL
    data = chip.rasInterval;
  }

  if(address == 7) {
    //RDRAM_MIN_INTERVAL
    data = chip.minInterval;
  }

  if(address == 8) {
    //RDRAM_ADDRESS_SELECT
    data = chip.addressSelect;
  }

  if(address == 9) {
    //RDRAM_DEVICE_MANUFACTURER
    data = chip.deviceManufacturer;
  }

  if(address == 10) {
    //RDRAM_CURRENT_CONTROL
    data = chip.currentControl;
  }

  debugger.io(Read, chipID, address, data);
  return data;
}

auto RDRAM::writeWord(u32 address, u32 data) -> void {
  u32 chipID = address >> 13 & 3;
  auto& chip = chips[chipID];
  address = (address & 0x3ff) >> 2;

  if(address == 0) {
    //RDRAM_DEVICE_TYPE
    chip.deviceType = data;
  }

  if(address == 1) {
    //RDRAM_DEVICE_ID
    chip.deviceID = data;
  }

  if(address == 2) {
    //RDRAM_DELAY
    chip.delay = data;
  }

  if(address == 3) {
    //RDRAM_MODE
    chip.mode = data;
  }

  if(address == 4) {
    //RDRAM_REF_INTERVAL
    chip.refreshInterval = data;
  }

  if(address == 5) {
    //RDRAM_REF_ROW
    chip.refreshRow = data;
  }

  if(address == 6) {
    //RDRAM_RAS_INTERVAL
    chip.rasInterval = data;
  }

  if(address == 7) {
    //RDRAM_MIN_INTERVAL
    chip.minInterval = data;
  }

  if(address == 8) {
    //RDRAM_ADDRESS_SELECT
    chip.addressSelect = data;
  }

  if(address == 9) {
    //RDRAM_DEVICE_MANUFACTURER
    chip.deviceManufacturer = data;
  }

  if(address == 10) {
    //RDRAM_CURRENT_CONTROL
    chip.currentControl = data;
  }

  debugger.io(Write, chipID, address, data);
}
