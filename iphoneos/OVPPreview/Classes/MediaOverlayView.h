//
//  MediaOverlayView.h
//  MoviePlayer
//
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// Originally created by Petrovic, Tommy (tpetrovi@akamai.com)
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.

#import <UIKit/UIKit.h>
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200

// Uncomment following .h file reference to enable Media Anayltics along with linking with the appropriate Libraries.
// Contact Akamai for further information.
//#import <AKAMMediaAnalytics.h>
#endif

#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200
@interface MediaOverlayView : UIView /*<AKAMMediaExtensionsCallback> */ /* --> AKAMMediaExtensionsCallback is used when MediaAnalytics is enabled in SDK 3.2 and later */
#else
@interface MediaOverlayView : UIView 
#endif
{
	UILabel *metaDisplayLabel;
	UIScrollView *displayDataFromMediaExtensions;
	UITextView *textView;
	UIButton *infoButton;
	UIButton *metaControlButton;
	UIButton *textViewFrame;

	UITextView *metadataView;
	UIButton *metadataViewFrame;
	UIButton *metadataThumbnail;

}
@property (nonatomic, retain) IBOutlet UILabel *metaDisplayLabel;
@property (nonatomic, retain) IBOutlet UIScrollView *displayDataFromMediaExtensions;
@property (nonatomic, retain) IBOutlet UITextView *textView;
@property (nonatomic, retain) IBOutlet UIButton *infoButton;
@property (nonatomic, retain) IBOutlet UIButton *textViewFrame;
@property (nonatomic, retain) IBOutlet UIButton *metaControlButton;

@property (nonatomic, retain) IBOutlet UITextView *metadataView;
@property (nonatomic, retain) IBOutlet UIButton *metadataViewFrame;
@property (nonatomic, retain) IBOutlet UIButton *metadataThumbnail;


- (void)awakeFromNib;
- (void)dealloc;
- (void)touchesBegan:(NSSet *)touches withEvent:(UIEvent *)event;


@end
