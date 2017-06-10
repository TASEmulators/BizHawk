#ifdef CARTRIDGE_CPP

void Cartridge::serialize(serializer &s) {
  for(auto &ram : nvram) {
    if(ram.size) s.array(ram.data, ram.size);
  }
}

#endif
