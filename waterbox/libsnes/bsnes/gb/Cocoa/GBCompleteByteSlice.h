#import "Document.h"
#import "HexFiend/HexFiend.h"
#import "HexFiend/HFByteSlice.h"

@interface GBCompleteByteSlice : HFByteSlice
- (instancetype) initWithByteArray:(HFByteArray *)array;
@end
