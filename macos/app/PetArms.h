#ifndef KC_PET_ARMS_H
#define KC_PET_ARMS_H

#import <Cocoa/Cocoa.h>
#include <math.h>

// Scene coordinates are logical points in a flipped 480 x 320 view.
// All coats share the black-cat pose rig; gradients and socks follow the
// Windows per-style coat definitions in CatArt.cs / SiameseArt.cs / GoldenArt.cs.
typedef struct {
    CGPoint shoulder, nearControl, farControl, paw;
    int side;
} KCArmCurve;

static inline CGFloat KCArmSmooth(CGFloat x) {
    x=fmax(0,fmin(1,x));
    return x*x*(3-2*x);
}
static inline CGPoint KCArmCenter(KCArmCurve curve,CGFloat t) {
    CGFloat s=1-t;
    return CGPointMake(s*s*s*curve.shoulder.x+3*s*s*t*curve.nearControl.x+3*s*t*t*curve.farControl.x+t*t*t*curve.paw.x,
                       s*s*s*curve.shoulder.y+3*s*s*t*curve.nearControl.y+3*s*t*t*curve.farControl.y+t*t*t*curve.paw.y);
}
static inline CGPoint KCArmNormal(KCArmCurve curve,CGFloat t) {
    CGPoint a=KCArmCenter(curve,fmax(0,t-.01)),b=KCArmCenter(curve,fmin(1,t+.01));
    CGFloat dx=b.x-a.x,dy=b.y-a.y,length=hypot(dx,dy);
    if(length*length<.000001)return CGPointMake(-1,0);
    return CGPointMake(-dy/length,dx/length);
}
static inline CGPoint KCArmOffset(CGPoint center,CGPoint n,CGFloat width) {
    return CGPointMake(center.x+n.x*width,center.y+n.y*width);
}
static KCArmCurve KCMakeArmCurve(BOOL left,CGPoint paw,CGFloat lean) {
    KCArmCurve c={0};c.side=left?-1:1;c.paw=paw;
    CGFloat raised=KCArmSmooth((221-paw.y)/32);
    c.shoulder=CGPointMake(214+c.side*26-29*lean,128);
    CGFloat dx=paw.x-c.shoulder.x,dy=paw.y-c.shoulder.y;
    c.nearControl=CGPointMake(c.shoulder.x+dx*.25+c.side*(4+8*raised),c.shoulder.y+dy*.37);
    c.farControl=CGPointMake(c.shoulder.x+dx*.79+c.side*(4+20*raised),paw.y-15+raised*41);
    return c;
}
static CGPathRef KCArmOutline(KCArmCurve c,CGFloat pawRadius) {
    const int n=18;CGPoint back[19];
    CGMutablePathRef p=CGPathCreateMutable();
    for(int i=0;i<=n;i++){
        CGFloat t=i/(CGFloat)n,width;
        width=(12.8+(pawRadius-12.8)*KCArmSmooth(t))*(.08+.92*KCArmSmooth(t/.19));
        CGPoint center=KCArmCenter(c,t),normal=KCArmNormal(c,t);
        CGPoint first=KCArmOffset(center,normal,width);
        back[i]=KCArmOffset(center,normal,-width);
        if(!i)CGPathMoveToPoint(p,NULL,first.x,first.y);
        else CGPathAddLineToPoint(p,NULL,first.x,first.y);
    }
    for(int i=n;i>=0;i--) CGPathAddLineToPoint(p,NULL,back[i].x,back[i].y);
    CGPathCloseSubpath(p);return p;
}
static inline void KCArmStrokeColor(CGContextRef ctx,unsigned color,CGFloat alpha) {
    CGContextSetRGBStrokeColor(ctx,((color>>16)&255)/255.,((color>>8)&255)/255.,(color&255)/255.,alpha);
}
// Gradient swatch slots: 0 black limb, 1 tuxedo white sock, 2 siamese ivory limb,
// 3 siamese seal sock, 4 golden limb.
enum { LimbBlack=0, SoxWhite=1, LimbIvory=2, SoxSeal=3, LimbGold=4 };
static CGGradientRef KCArmGradient(int index) {
    // These immutable swatches live as long as the main-thread scene.
    static CGGradientRef palette[5];
    static const unsigned colors[5][2]={{0x1e1c1f,0x18171a},{0xfff9ee,0xe2d9cb},
                                        {0xf0e8da,0xe2d8c8},{0x3e302c,0x2d221f},
                                        {0xeccc94,0xb88c52}};
    if(!palette[index]){
        CGFloat rgba[8];for(int i=0;i<2;i++){
            unsigned rgb=colors[index][i];
            rgba[i*4]=((rgb>>16)&255)/255.;rgba[i*4+1]=((rgb>>8)&255)/255.;rgba[i*4+2]=(rgb&255)/255.;rgba[i*4+3]=1;
        }
        CGColorSpaceRef space=CGColorSpaceCreateWithName(kCGColorSpaceSRGB);
        palette[index]=CGGradientCreateWithColorComponents(space,rgba,NULL,2);CGColorSpaceRelease(space);
    }
    return palette[index];
}
static void KCArmPaintPath(CGContextRef ctx,CGPathRef p,int index) {
    CGRect b=CGPathGetPathBoundingBox(p);if(b.size.width<=0 || b.size.height<=0)return;
    // Anchor points mirror the WPF relative gradients: limb fur runs (.2,0)→(.9,1),
    // socks run (.2,0)→(.8,1).
    CGPoint z=(index==1||index==3)?CGPointMake(.8,1):CGPointMake(.9,1);
    CGContextSaveGState(ctx);CGContextAddPath(ctx,p);CGContextClip(ctx);
    CGContextTranslateCTM(ctx,b.origin.x,b.origin.y);CGContextScaleCTM(ctx,b.size.width,b.size.height);
    CGContextDrawLinearGradient(ctx,KCArmGradient(index),CGPointMake(.2,0),z,kCGGradientDrawsBeforeStartLocation|kCGGradientDrawsAfterEndLocation);
    CGContextRestoreGState(ctx);
}
static void KCPointSock(CGContextRef ctx,KCArmCurve c,CGPathRef limb,CGPoint paw,CGFloat radius,
                        CGFloat activity,int sox,unsigned rim) {
    CGFloat dx=paw.x-c.farControl.x,dy=paw.y-c.farControl.y,length=hypot(dx,dy);
    if(length*length<.001){dx=0;dy=1;}else{dx/=length;dy/=length;}
    CGContextSaveGState(ctx);CGContextAddPath(ctx,limb);CGContextClip(ctx);
    CGContextConcatCTM(ctx,CGAffineTransformMake(dy,-dx,dx,dy,paw.x,paw.y));
    CGMutablePathRef sock=CGPathCreateMutable();
    CGPathMoveToPoint(sock,NULL,-9.6,.5);
    CGPathAddCurveToPoint(sock,NULL,-10.4,-4.5,-10.2,-9.5,-7.6,-11.2);
    CGPathAddQuadCurveToPoint(sock,NULL,-5.2,-12.6,-2.2,-11.3);
    CGPathAddQuadCurveToPoint(sock,NULL,.8,-9.8,3,-11.2);
    CGPathAddQuadCurveToPoint(sock,NULL,5.9,-13,8.1,-10.9);
    CGPathAddQuadCurveToPoint(sock,NULL,10.1,-7.5,9,-.5);
    CGPathAddQuadCurveToPoint(sock,NULL,7,7.4,0,9.4);
    CGPathAddQuadCurveToPoint(sock,NULL,-8.1,8.3,-9.6,.5);
    CGPathCloseSubpath(sock);
    KCArmPaintPath(ctx,sock,sox);CGPathRelease(sock);
    CGContextRestoreGState(ctx);
    CGFloat rx=radius+.35,ry=radius*(.94-.06*activity);
    CGPathRef cap=CGPathCreateWithEllipseInRect(CGRectMake(paw.x-rx,paw.y-ry,rx*2,ry*2),NULL);
    KCArmPaintPath(ctx,cap,sox);
    KCArmStrokeColor(ctx,rim,1);CGContextSetLineWidth(ctx,.9);
    CGContextAddPath(ctx,cap);CGContextStrokePath(ctx);CGPathRelease(cap);
}
static void KCDrawArm(CGContextRef ctx,NSInteger cat,BOOL left,CGPoint paw,double lean,double activity) {
    CGFloat radius=8.4+fmax(0,fmin(1,activity));
    KCArmCurve c=KCMakeArmCurve(left,paw,lean);
    CGPathRef limb=KCArmOutline(c,radius);
    CGContextSaveGState(ctx);CGContextSetLineCap(ctx,kCGLineCapRound);
    int limbGradient=cat==2?LimbIvory:(cat==3?LimbGold:LimbBlack);
    KCArmPaintPath(ctx,limb,limbGradient);
    if(cat==1){
        KCPointSock(ctx,c,limb,paw,radius,activity,SoxWhite,0x968d82);
    }else if(cat==2){
        KCPointSock(ctx,c,limb,paw,radius,activity,SoxSeal,0x8a7a70);
    }else{
        CGFloat rx=radius+.35,ry=radius*(.94-.06*activity);
        CGRect box=CGRectMake(paw.x-rx,paw.y-ry,rx*2,ry*2);
        CGPathRef cap=CGPathCreateWithEllipseInRect(box,NULL);KCArmPaintPath(ctx,cap,limbGradient);CGPathRelease(cap);
        CGFloat raised=KCArmSmooth((221-paw.y)/32);
        if(raised>.1){
            KCArmStrokeColor(ctx,cat==3?0x96744a:0x5c595b,raised*.85);CGContextSetLineWidth(ctx,.95);CGContextBeginPath(ctx);
            CGContextMoveToPoint(ctx,paw.x+c.side*7.5,paw.y+.7);
            CGContextAddCurveToPoint(ctx,paw.x+c.side*7.1,paw.y+6.2,paw.x+c.side*2.7,paw.y+8.2,paw.x-c.side*2.3,paw.y+7.7);
            CGContextStrokePath(ctx);
        }
    }
    CGContextRestoreGState(ctx);CGPathRelease(limb);
}
#endif
