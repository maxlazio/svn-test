//
//  MoviePlayerController.h
//  OVPPreview
//
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// Originally created by Petrovic, Tommy (tpetrovi@akamai.com)
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <OVPMediaPlayerController.h>
#import "MediaOverlayView.h" 
#import "MediaItem.h" 

#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200
 
 
// This is an 'internal' UIViewController supporting new 3.2 video player
@interface moviePlayerViewController : UIViewController
{
	OVPMoviePlayerController *moviePlayerRef;
}
@property (nonatomic, assign) OVPMoviePlayerController *moviePlayerRef;
-(void)loadView;
-(void)viewDidLoad;
-(BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation;

@end
#endif
  

@class AKAMMediaExtensionsController;
@interface MoviePlayerController : UIViewController <OVPMediaPlayback>
{
	OVPMoviePlayerController *moviePlayer;
	AKAMMediaExtensionsController *mediaExtension;
	MediaOverlayView *overlayView;
	MediaItem *mediaItem;
	BOOL playingPreroll;
	NSURL *prMovieURL;
	NSURL *mediaURL;
	UIImageView *medItemImage;
	int overlayButtonState;
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200
	moviePlayerViewController *moviePlayer32ViewController;
#endif	
}

@property (nonatomic, retain) OVPMoviePlayerController *moviePlayer;
@property (nonatomic, retain) AKAMMediaExtensionsController *mediaExtension;
@property (nonatomic, retain) NSURL *prMovieURL;
@property (nonatomic, retain) NSURL *mediaURL;
@property (nonatomic, assign) MediaItem *mediaItem;
@property (nonatomic, retain) IBOutlet MediaOverlayView *overlayView;
@property (nonatomic, retain) UIImageView *medItemImage;
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200
@property (nonatomic, retain) moviePlayerViewController *moviePlayer32ViewController;
#endif



-(MoviePlayerController*)init;


-(void)playMovie:(NSURL *)movieURL;
-(IBAction)overlayViewButtonPress:(id)sender;
-(IBAction)metaControlButtonPress:(id)sender;

@end
