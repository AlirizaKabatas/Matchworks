#import <UIKit/UIKit.h>
extern "C" void WMChainHaptic()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        static UIImpactFeedbackGenerator *generator = nil;
        if (!generator) generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        [generator prepare];
        if (@available(iOS 13.0, *)) [generator impactOccurredWithIntensity:0.35];
        else [generator impactOccurred];
    });
}
