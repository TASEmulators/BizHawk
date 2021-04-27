#import <Foundation/Foundation.h>
#import <Core/gb.h>

@interface GBAudioClient : NSObject
@property (strong) void (^renderBlock)(UInt32 sampleRate, UInt32 nFrames, GB_sample_t *buffer);
@property (readonly) UInt32 rate;
@property (readonly, getter=isPlaying) bool playing;
-(void) start;
-(void) stop;
-(id) initWithRendererBlock:(void (^)(UInt32 sampleRate, UInt32 nFrames, GB_sample_t *buffer)) block
              andSampleRate:(UInt32) rate;
@end
