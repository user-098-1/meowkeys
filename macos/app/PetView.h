#import <Cocoa/Cocoa.h>

@interface KCPetView : NSView
@property(nonatomic) NSInteger catStyle;
@property(nonatomic) NSInteger keyboardStyle;
@property(nonatomic) NSInteger mouseStyle;
@property(nonatomic) BOOL showsLabels;
@property (nonatomic,readonly) BOOL hasArt;
- (void)samplePointer:(NSPoint)point onScreen:(NSPoint)normalized left:(BOOL)left right:(BOOL)right at:(double)now;
- (void)button:(NSInteger)side down:(BOOL)down at:(double)now;
- (void)strike:(NSInteger)vk label:(NSString *)label at:(double)now;
- (void)advance:(double)now;
- (void)clearInput;
- (NSImage *)thumbnail:(NSString *)name maxWidth:(CGFloat)width;
- (NSImage *)cursorImage:(NSInteger)style;
- (NSPoint)cursorHotspot:(NSInteger)style;
- (NSSize)cursorSize:(NSInteger)style;
@end


