#define GB_INTERNAL // Todo: Some memory accesses are being done using the struct directly
#import "GBMemoryByteArray.h"
#import "GBCompleteByteSlice.h"


@implementation GBMemoryByteArray
{
    Document *_document;
}

- (instancetype) initWithDocument:(Document *)document
{
    if ((self = [super init])) {
        _document = document;
    }
    return self;
}

- (unsigned long long)length
{
    switch (_mode) {
        case GBMemoryEntireSpace:
            return 0x10000;
        case GBMemoryROM:
            return 0x8000;
        case GBMemoryVRAM:
            return 0x2000;
        case GBMemoryExternalRAM:
            return 0x2000;
        case GBMemoryRAM:
            return 0x2000;
    }
}

- (void)copyBytes:(unsigned char *)dst range:(HFRange)range
{
    __block uint16_t addr = (uint16_t) range.location;
    __block unsigned long long length = range.length;
    if (_mode == GBMemoryEntireSpace) {
        while (length) {
            *(dst++) = [_document readMemory:addr++];
            length--;
        }
    }
    else {
        [_document performAtomicBlock:^{
            unsigned char *_dst = dst;
            uint16_t bank_backup = 0;
            GB_gameboy_t *gb = _document.gameboy;
            switch (_mode) {
                case GBMemoryROM:
                    bank_backup = gb->mbc_rom_bank;
                    gb->mbc_rom_bank = self.selectedBank;
                    break;
                case GBMemoryVRAM:
                    bank_backup = gb->cgb_vram_bank;
                    if (GB_is_cgb(gb)) {
                        gb->cgb_vram_bank = self.selectedBank;
                    }
                    addr += 0x8000;
                    break;
                case GBMemoryExternalRAM:
                    bank_backup = gb->mbc_ram_bank;
                    gb->mbc_ram_bank = self.selectedBank;
                    addr += 0xA000;
                    break;
                case GBMemoryRAM:
                    bank_backup = gb->cgb_ram_bank;
                    if (GB_is_cgb(gb)) {
                        gb->cgb_ram_bank = self.selectedBank;
                    }
                    addr += 0xC000;
                    break;
                default:
                    assert(false);
            }
            while (length) {
                *(_dst++) = [_document readMemory:addr++];
                length--;
            }
            switch (_mode) {
                case GBMemoryROM:
                    gb->mbc_rom_bank = bank_backup;
                    break;
                case GBMemoryVRAM:
                    gb->cgb_vram_bank = bank_backup;
                    break;
                case GBMemoryExternalRAM:
                    gb->mbc_ram_bank = bank_backup;
                    break;
                case GBMemoryRAM:
                    gb->cgb_ram_bank = bank_backup;
                    break;
                default:
                    assert(false);
            }
        }];
    }
}

- (NSArray *)byteSlices
{
    return @[[[GBCompleteByteSlice alloc] initWithByteArray:self]];
}

- (HFByteArray *)subarrayWithRange:(HFRange)range
{
    unsigned char arr[range.length];
    [self copyBytes:arr range:range];
    HFByteArray *ret = [[HFBTreeByteArray alloc] init];
    HFFullMemoryByteSlice *slice = [[HFFullMemoryByteSlice alloc] initWithData:[NSData dataWithBytes:arr length:range.length]];
    [ret insertByteSlice:slice inRange:HFRangeMake(0, 0)];
    return ret;
}

- (void)insertByteSlice:(HFByteSlice *)slice inRange:(HFRange)lrange
{
    if (slice.length != lrange.length) return; /* Insertion is not allowed, only overwriting. */
    [_document performAtomicBlock:^{
        uint16_t addr = (uint16_t) lrange.location;
        uint16_t bank_backup = 0;
        GB_gameboy_t *gb = _document.gameboy;
        switch (_mode) {
            case GBMemoryROM:
                bank_backup = gb->mbc_rom_bank;
                gb->mbc_rom_bank = self.selectedBank;
                break;
            case GBMemoryVRAM:
                bank_backup = gb->cgb_vram_bank;
                if (GB_is_cgb(gb)) {
                    gb->cgb_vram_bank = self.selectedBank;
                }
                addr += 0x8000;
                break;
            case GBMemoryExternalRAM:
                bank_backup = gb->mbc_ram_bank;
                gb->mbc_ram_bank = self.selectedBank;
                addr += 0xA000;
                break;
            case GBMemoryRAM:
                bank_backup = gb->cgb_ram_bank;
                if (GB_is_cgb(gb)) {
                    gb->cgb_ram_bank = self.selectedBank;
                }
                addr += 0xC000;
                break;
            default:
                break;
        }
        uint8_t values[lrange.length];
        [slice copyBytes:values range:HFRangeMake(0, lrange.length)];
        uint8_t *src = values;
        unsigned long long length = lrange.length;
        while (length) {
            [_document writeMemory:addr++ value:*(src++)];
            length--;
        }
        switch (_mode) {
            case GBMemoryROM:
                gb->mbc_rom_bank = bank_backup;
                break;
            case GBMemoryVRAM:
                gb->cgb_vram_bank = bank_backup;
                break;
            case GBMemoryExternalRAM:
                gb->mbc_ram_bank = bank_backup;
                break;
            case GBMemoryRAM:
                gb->cgb_ram_bank = bank_backup;
                break;
            default:
                break;
        }
    }];
}

@end
