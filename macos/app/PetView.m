#import "PetView.h"
#import "PetArms.h"
#import <math.h>

static double Clamp(double x,double a,double b) { return fmax(a,fmin(b,x)); }
static double Smooth(double x) { x=Clamp(x,0,1);return x*x*(3-2*x); }
static NSPoint Mix(NSPoint a,NSPoint b,double t) { return NSMakePoint(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t); }
static NSPoint Pair(NSArray *xy,NSPoint fallback) {
    return xy.count==2 ? NSMakePoint([xy[0] doubleValue],[xy[1] doubleValue]) : fallback;
}
static double Beat(double age) {
    if(age<0||age>.24)return 0;
    return age<.065?1:1-Smooth((age-.065)/.175);
}
static double Pulse(double age,double duration) {
    return age<0||age>duration ? 0:fmin(1,sin(M_PI*age/duration)*1.4);
}

@interface KCCap : NSObject
@property(copy) NSString *label;
@property NSPoint start;
@property double time, angle;
@end
@implementation KCCap
@end

@implementation KCPetView {
    NSDictionary *_sprites, *_keys;
    NSURL *_assetDir;
    NSMutableDictionary<NSString *,NSImage *> *_images;
    NSMutableDictionary<NSNumber *,NSNumber *> *_keysDown;
    NSMutableArray<KCCap *> *_caps;
    NSDictionary *_cursorInfo;
    NSPoint _mousePaw, _pointer, _left, _freeLeft, _right, _leftKey, _rightKey;
    NSPoint _travel, _screenPosition, _offset;
    BOOL _pointerKnown, _headHovered, _leftDown, _rightDown;
    double _time,_lastKey,_leftAt,_rightAt,_lastMove,_leftClick,_rightClick,_lastPet,_nextBlink;
    double _grip,_gazeX,_gazeY,_blink,_leftActivity,_rightActivity;
}

- (instancetype)initWithFrame:(NSRect)frame {
    self=[super initWithFrame:frame]; if(!self)return nil;
    _assetDir=[[[NSBundle mainBundle] resourceURL] URLByAppendingPathComponent:@"Art" isDirectory:YES];
    NSData *data=[NSData dataWithContentsOfURL:[_assetDir URLByAppendingPathComponent:@"atlas.json"]];
    NSDictionary *atlas=data?[NSJSONSerialization JSONObjectWithData:data options:0 error:NULL]:nil;
    if([atlas isKindOfClass:NSDictionary.class]) {
        _sprites=atlas[@"sprites"];_keys=atlas[@"keys"];_mousePaw=Pair(atlas[@"mousePaw"],NSMakePoint(104,264));
    }
    _images=[NSMutableDictionary dictionary];_keysDown=[NSMutableDictionary dictionary];_caps=[NSMutableArray array];
    NSData *cursorData=[NSData dataWithContentsOfURL:[_assetDir URLByAppendingPathComponent:@"cursor.json"]];
    _cursorInfo=cursorData?[NSJSONSerialization JSONObjectWithData:cursorData options:0 error:NULL]:nil;
    [self clearInput];_showsLabels=YES; return self;
}
- (BOOL)hasArt { return _sprites[@"body-1"]!=nil && _keys.count>0; }
- (BOOL)isFlipped { return YES; }
- (BOOL)isOpaque { return NO; }
- (BOOL)acceptsFirstMouse:(NSEvent *)event { return YES; }
- (void)mouseDown:(NSEvent *)event { [self.window performWindowDragWithEvent:event]; }
- (void)clearInput {
    [_keysDown removeAllObjects];[_caps removeAllObjects];
    _left=_freeLeft=NSMakePoint(192,233);_right=NSMakePoint(229,233);
    _travel=_offset=NSZeroPoint;_pointerKnown=NO;_leftDown=_rightDown=_headHovered=NO;
    _time=NSProcessInfo.processInfo.systemUptime;
    _lastMove=_lastKey=_leftAt=_rightAt=_leftClick=_rightClick=_lastPet=_time-10;
    _nextBlink=_time+2.7;_grip=_gazeX=_gazeY=_leftActivity=_rightActivity=_blink=0;self.needsDisplay=YES;
}
- (void)setShowsLabels:(BOOL)flag { _showsLabels=flag;if(!flag)[_caps removeAllObjects];self.needsDisplay=YES; }
- (BOOL)usingMouse:(double)now { return _leftDown || _rightDown || now-_lastMove<.83; }
- (void)markMouse:(double)now {
    if(![self usingMouse:now] && _leftAt>_rightAt && now-_leftAt<.24) {
        _rightKey=_leftKey;_rightAt=_leftAt;_leftAt=now-10;
    }
    _lastMove=now;
}
- (void)button:(NSInteger)side down:(BOOL)down at:(double)now {
    if(side!=0&&side!=1)return;
    BOOL old=side==0?_leftDown:_rightDown;if(old==down)return;
    [self markMouse:now];
    if(side==0){_leftDown=down;if(down)_leftClick=now;}
    else {_rightDown=down;if(down)_rightClick=now;}
    if(down&&_headHovered)_lastPet=now;
}
- (double)lean { return Smooth((157-_left.x)/50); }
- (NSPoint)headPosition {
    double lean=[self lean];
    return NSMakePoint(214-45*lean+3.2*_gazeX,86+sin(_time*1.7)*.72+_gazeY*2+lean*1.2);
}
- (void)samplePointer:(NSPoint)p onScreen:(NSPoint)normalized left:(BOOL)left right:(BOOL)right at:(double)now {
    if(_pointerKnown){
        double dx=p.x-_pointer.x,dy=p.y-_pointer.y;
        if(dx*dx+dy*dy>.000001){
            _travel=NSMakePoint(Clamp(_travel.x+dx*.13,-5.5,5.5),Clamp(_travel.y+dy*.13,-6,6));
            [self markMouse:now];
        }
    }
    _pointer=p;_pointerKnown=YES;_screenPosition=normalized;
    NSPoint c=[self headPosition];double x=(p.x-c.x)/56,y=(p.y-c.y+9)/52;
    BOOL hit=x*x+y*y<1;if(hit&&!_headHovered)_lastPet=now;_headHovered=hit;
    [self button:0 down:left at:now];[self button:1 down:right at:now];
}
- (void)strike:(NSInteger)vk label:(NSString *)label at:(double)now {
    if(!vk || !label)return;
    NSString *key=[@(vk) stringValue];NSPoint target=Pair(_keys[key],NSMakePoint(158,258));
    _lastKey=now;_keysDown[@(vk)]=@(now);
    if(![self usingMouse:now] && (target.x<218 || (vk==32 && _rightAt>_leftAt))) {_leftAt=now;_leftKey=target;}
    else {_rightAt=now;_rightKey=target;}
    if(!_showsLabels)return;
    if(_caps.count>=9)[_caps removeObjectAtIndex:0];
    KCCap *cap=[KCCap new];cap.label=label;cap.time=now;
    cap.angle=(int)arc4random_uniform(15)-7;cap.start=NSMakePoint(target.x+(int)arc4random_uniform(11)-5,target.y-14);
    [_caps addObject:cap];
}
- (void)advance:(double)now {
    double dt=Clamp(now-_time,.001,.1);_time=now;
    if(now>_nextBlink+.30)_nextBlink=now+2.6+arc4random_uniform(3700)/1000.;
    _blink=fmax(Pulse(now-_nextBlink,.23),Pulse(now-_lastPet,.59));
    double typing=1-Smooth((now-_lastKey-.95)/.48);
    double hold=(_leftDown||_rightDown)?1:1-Smooth((now-_lastMove-.55)/.28);
    _grip+=Clamp(hold-_grip,-dt*10,dt*12);
    if(hold==0)_travel=Mix(_travel,NSZeroPoint,1-exp(-dt*8));
    NSPoint target=NSMakePoint((_screenPosition.x*5+_travel.x)*_grip,(_screenPosition.y*5+_travel.y)*_grip);
    _offset=Mix(_offset,target,1-exp(-dt*32));
    NSPoint left=Mix(NSMakePoint(192,233),NSMakePoint(188,189),typing);
    NSPoint right=Mix(NSMakePoint(229,233),NSMakePoint(233,193),typing);
    _freeLeft=Mix(_freeLeft,Mix(left,_leftKey,Beat(now-_leftAt)),1-exp(-dt*46));
    _right=Mix(_right,Mix(right,_rightKey,Beat(now-_rightAt)),1-exp(-dt*46));
    double lg=_leftDown?1:1-Smooth((now-_leftClick-.10)/.14);
    double rg=_rightDown?1:1-Smooth((now-_rightClick-.10)/.14);
    NSPoint mouse=NSMakePoint(_mousePaw.x+_offset.x+(lg-rg)*1.6,_mousePaw.y+_offset.y+2.6*fmax(lg,rg));
    _left=Mix(_freeLeft,mouse,_grip);_leftActivity=fmax(typing,_grip);_rightActivity=typing;
    double tx=_pointerKnown?tanh((_pointer.x-214)/155.):0,ty=_pointerKnown?tanh((_pointer.y-85)/155.):0;
    double focus=typing*(1-_grip*.55), keyX=_leftAt>=_rightAt?_leftKey.x:_rightKey.x;
    tx+=(tanh((keyX-214)/130.)-tx)*focus;ty+=(1-ty)*focus;
    _gazeX+=(tx-_gazeX)*(1-exp(-dt*14));_gazeY+=(ty-_gazeY)*(1-exp(-dt*14));
    for(NSNumber *key in _keysDown.allKeys)if(now-_keysDown[key].doubleValue>.43)[_keysDown removeObjectForKey:key];
    for(NSInteger i=(NSInteger)_caps.count-1;i>=0;i--){
        double life=i==(NSInteger)_caps.count-1?5.8:.89;
        if(now-_caps[(NSUInteger)i].time>life)[_caps removeObjectAtIndex:(NSUInteger)i];
    }
    self.needsDisplay=YES;
}
- (NSImage *)imageOf:(NSString *)name {
    NSDictionary *entry=_sprites[name];NSString *file=entry[@"file"];if(!file)return nil;
    NSImage *image=_images[file];
    if(!image){image=[[NSImage alloc] initWithContentsOfURL:[_assetDir URLByAppendingPathComponent:file]];
        if(image)_images[file]=image;}
    return image;
}
- (NSImage *)thumbnail:(NSString *)name maxWidth:(CGFloat)w {
    NSImage *img=[[self imageOf:name] copy];if(!img)return nil;
    NSDictionary *entry=_sprites[name];double a=[entry[@"w"] doubleValue], b=[entry[@"h"] doubleValue];
    if(a<=0||b<=0)return nil;double scale=fmin(w/a,22/b);img.size=NSMakeSize(a*scale,b*scale);return img;
}
- (NSDictionary *)cursorEntry:(NSInteger)style {
    return _cursorInfo[@"styles"][[NSString stringWithFormat:@"%ld",(long)style]];
}
- (NSImage *)cursorImage:(NSInteger)style {
    return [self imageOf:[NSString stringWithFormat:@"cursor-%ld",(long)style]];
}
- (NSPoint)cursorHotspot:(NSInteger)style {
    NSDictionary *entry=[self cursorEntry:style];
    return NSMakePoint(entry?[[entry objectForKey:@"hotX"] doubleValue]:16,entry?[[entry objectForKey:@"hotY"] doubleValue]:16);
}
- (NSSize)cursorSize:(NSInteger)style {
    NSDictionary *r=_sprites[[NSString stringWithFormat:@"cursor-%ld",(long)style]];
    // Sprites are exported at 2x; the overlay draws in points.
    return NSMakeSize(r?[[r objectForKey:@"w"] doubleValue]/2:32,r?[[r objectForKey:@"h"] doubleValue]/2:32);
}
- (void)drawSprite:(NSString *)name alpha:(CGFloat)alpha {
    NSDictionary *r=_sprites[name];NSImage *img=[self imageOf:name];if(!r||!img)return;
    NSRect box=NSMakeRect([r[@"x"] doubleValue],[r[@"y"] doubleValue],[r[@"w"] doubleValue],[r[@"h"] doubleValue]);
    [img drawInRect:box fromRect:NSZeroRect operation:NSCompositingOperationSourceOver fraction:alpha respectFlipped:YES hints:nil];
}
- (void)drawCaps:(CGContextRef)ctx {
    NSDictionary *attrs=@{NSFontAttributeName:[NSFont boldSystemFontOfSize:13],NSForegroundColorAttributeName:[NSColor colorWithWhite:.11 alpha:1]};
    for(KCCap *cap in _caps){
        double age=fmax(0,_time-cap.time),progress=age/.89,travel=1-pow(1-Clamp(progress,0,1),2);
        BOOL last=cap==_caps.lastObject;double settle=last?Smooth((age-.6)/.42):0;
        NSPoint p=Mix(NSMakePoint(cap.start.x+cap.angle*travel,cap.start.y-53*travel),NSMakePoint(298,214),settle);
        double opacity=last?1-Smooth((age-5.4)/.4):1-Smooth((progress-.55)/.45);
        NSSize text=[cap.label sizeWithAttributes:attrs];double w=fmax(25,text.width+9);
        CGContextSaveGState(ctx);CGContextTranslateCTM(ctx,p.x,p.y);CGContextRotateCTM(ctx,cap.angle*travel*(1-settle)*M_PI/180);
        CGContextSetAlpha(ctx,opacity);
        NSBezierPath *shape=[NSBezierPath bezierPathWithRoundedRect:NSMakeRect(-w/2,-9.5,w,19) xRadius:4 yRadius:4];
        [[NSColor colorWithRed:.98 green:.97 blue:.94 alpha:1] setFill];[shape fill];
        [[NSColor colorWithWhite:.30 alpha:1] setStroke];shape.lineWidth=.8;[shape stroke];
        [cap.label drawAtPoint:NSMakePoint(-text.width/2,-text.height/2) withAttributes:attrs];CGContextRestoreGState(ctx);
    }
}
- (void)drawRect:(NSRect)dirty {
    CGContextRef ctx=NSGraphicsContext.currentContext.CGContext;
    CGContextClearRect(ctx,NSRectToCGRect(dirty));
    CGContextSaveGState(ctx);CGContextScaleCTM(ctx,self.bounds.size.width/306.,self.bounds.size.height/298.);
    CGContextTranslateCTM(ctx,-75,-9);NSGraphicsContext.currentContext.imageInterpolation=NSImageInterpolationHigh;
    [self drawSprite:[NSString stringWithFormat:@"keyboard-%ld",(long)_keyboardStyle] alpha:1];
    for(NSNumber *key in _keysDown){
        double alpha=1-Smooth((_time-_keysDown[key].doubleValue)/.43);
        [self drawSprite:[NSString stringWithFormat:@"key-%ld-%@",(long)_keyboardStyle,key] alpha:alpha];
    }
    BOOL l=_leftDown||_time-_leftClick<.18, r=_rightDown||_time-_rightClick<.18;
    NSString *state=l?(r?@"both":@"left"):(r?@"right":@"normal");
    CGContextSaveGState(ctx);CGContextTranslateCTM(ctx,_offset.x,_offset.y);
    [self drawSprite:[NSString stringWithFormat:@"mouse-%ld-%@",(long)_mouseStyle,state] alpha:1];CGContextRestoreGState(ctx);
    NSInteger phase=((NSInteger)floor(_time*1.9/(2*M_PI)*8))%8;
    [self drawSprite:[NSString stringWithFormat:@"tail-%ld-%ld",(long)_catStyle,(long)phase] alpha:1];
    double lean=[self lean],shear=.282*lean;
    CGAffineTransform body=CGAffineTransformMake(1,0,shear,1,-233*shear,0);
    CGContextSaveGState(ctx);CGContextConcatCTM(ctx,body);
    [self drawSprite:[NSString stringWithFormat:@"body-%ld",(long)_catStyle] alpha:1];CGContextRestoreGState(ctx);
    KCDrawArm(ctx,_catStyle,NO,_right,lean,_rightActivity);
    KCDrawArm(ctx,_catStyle,YES,_left,lean,_leftActivity);
    NSPoint head=[self headPosition];CGContextSaveGState(ctx);CGContextTranslateCTM(ctx,head.x,head.y);
    CGContextRotateCTM(ctx,(2.8*_gazeX-3.2*lean)*M_PI/180.);
    NSInteger ix=(NSInteger)lround(Clamp(_gazeX,-1,1))+1, iy=(NSInteger)lround(Clamp(_gazeY,-1,1))+1;
    NSInteger blink=_blink>.80?2:(_blink>.30?1:0);
    [self drawSprite:[NSString stringWithFormat:@"head-%ld-%ld-%ld-%ld",(long)_catStyle,(long)ix,(long)iy,(long)blink] alpha:1];
    CGContextRestoreGState(ctx);[self drawCaps:ctx];CGContextRestoreGState(ctx);
}
@end

