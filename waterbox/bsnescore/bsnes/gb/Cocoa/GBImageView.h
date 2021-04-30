#import <Cocoa/Cocoa.h>

@protocol GBImageViewDelegate;

@interface GBImageViewGridConfiguration : NSObject
@property NSColor *color;
@property NSUInteger size;
- (instancetype) initWithColor: (NSColor *) color size: (NSUInteger) size;
@end

@interface GBImageView : NSImageView
@property (nonatomic) NSArray *horizontalGrids;
@property (nonatomic) NSArray *verticalGrids;
@property (nonatomic) bool displayScrollRect;
@property NSRect scrollRect;
@property (weak) IBOutlet id<GBImageViewDelegate> delegate;
@end

@protocol GBImageViewDelegate <NSObject>
@optional
- (void) mouseDidLeaveImageView: (GBImageView *)view;
- (void) imageView: (GBImageView *)view mouseMovedToX:(NSUInteger) x Y:(NSUInteger) y;
@end
