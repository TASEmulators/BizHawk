//Memory

unsigned Memory::size() const { return 0; }

//StaticRAM

uint8* StaticRAM::data() { return data_; }
unsigned StaticRAM::size() const { return size_; }

uint8 StaticRAM::read(unsigned addr) { return data_[addr]; }
void StaticRAM::write(unsigned addr, uint8 n) { data_[addr] = n; }
uint8& StaticRAM::operator[](unsigned addr) { return data_[addr]; }
const uint8& StaticRAM::operator[](unsigned addr) const { return data_[addr]; }

StaticRAM::StaticRAM(unsigned n, const char* name) : size_(n), name_(name) {
  if(name_) data_ = nullptr; // data_ alloc must be deferred (static ctor is not in a cothread!)
  else data_ = new uint8[size_]();
}
StaticRAM::~StaticRAM() { abort(); }

void StaticRAM::init() { if(name_ && !data_) data_ = (uint8*)interface()->allocSharedMemory(name_, size_); }

//MappedRAM

void MappedRAM::reset() {
  if(data_) {
		/*if(name_) interface()->freeSharedMemory(data_);
		else free(data_);
    data_ = 0;*/
    abort();
  }
  size_ = 0;
  write_protect_ = false;
}

void MappedRAM::map(uint8 *source, unsigned length) {
  reset();
  data_ = source;
  size_ = data_ ? length : 0;
}

void MappedRAM::copy(const uint8 *data, unsigned size) {
  if(!data_) {
    size_ = (size & ~255) + ((bool)(size & 255) << 8);
		if(name_) data_ = (uint8*)interface()->allocSharedMemory(name_, size_);
    else data_ = new uint8[size_]();
  }
  memcpy(data_, data, min(size_, size));
}

void MappedRAM::write_protect(bool status) { write_protect_ = status; }
uint8* MappedRAM::data() { return data_; }
unsigned MappedRAM::size() const { return size_; }

uint8 MappedRAM::read(unsigned addr) { return data_[addr]; }
void MappedRAM::write(unsigned addr, uint8 n) { if(!write_protect_) data_[addr] = n; }
const uint8& MappedRAM::operator[](unsigned addr) const { return data_[addr]; }
MappedRAM::MappedRAM(const char* name) : data_(0), size_(0), write_protect_(false), name_(name) {}

//Bus

uint8 Bus::read(unsigned addr) {
  return reader[lookup[addr]](target[addr]);
}

void Bus::write(unsigned addr, uint8 data) {
  return writer[lookup[addr]](target[addr], data);
}
