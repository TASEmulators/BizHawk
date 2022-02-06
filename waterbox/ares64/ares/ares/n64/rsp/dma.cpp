auto RSP::dmaTransfer() -> void {
  if(dma.requests.empty()) return;
  auto request = *dma.requests.read();
  auto region = !request.pbusRegion ? 0x0400'0000 : 0x0400'1000;

  if(request.type == DMA::Request::Type::Read) {
    for(u32 block : range(request.count)) {
      for(u32 offset = 0; offset < request.length; offset += 4) {
        u32 data = bus.read<Word>(request.dramAddress + offset);
        bus.write<Word>(region + request.pbusAddress + offset, data);
      }
      request.pbusAddress += request.length;
      request.dramAddress += request.length + request.skip;
    }
  }

  if(request.type == DMA::Request::Type::Write) {
    for(u32 block : range(request.count)) {
      for(u32 offset = 0; offset < request.length; offset += 4) {
        u32 data = bus.read<Word>(region + request.pbusAddress + offset);
        bus.write<Word>(request.dramAddress + offset, data);
      }
      request.pbusAddress += request.length;
      request.dramAddress += request.length + request.skip;
    }
  }
}
