//
// File: OVPPreviewAppDelegate.h
// Main app delegate
// 
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.

#import <UIKit/UIKit.h>

@interface AboutView : UIView
{
	UILabel *txtLabel;
}

@property (nonatomic, retain) IBOutlet UILabel *txtLabel;
- (void)awakeFromNib;
@end

// Adding interface rotation handling to the About view, used for 3.2 implementation
@interface AboutViewController : UIViewController
-(BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation;
@end

// A simple view which will be overlaid with 70% transparency on top of current view in order to 'show' that
// processing is in progress and prevent accidental tapping into the areas which are in process of intializing/changing.
@interface ProgressView : UIView
{
	UILabel *message;
}

-(void)UIOrientationChanged:(UIInterfaceOrientation)orientation;

@end


@class MediaItem, MediaTableViewController;

@interface OVPPreviewAppDelegate : UIViewController <UIApplicationDelegate, UITabBarControllerDelegate> 
{
	UIWindow *window;
	AboutViewController *aboutController;
	UINavigationController *navigationController;
	MediaTableViewController *rootViewController; 
	NSMutableArray *mediaList;
    
	// Additional controllers/views
	UITabBarController *tabBarController;
	UIActivityIndicatorView *activityIndicator;
	AboutView *aboutView;
	ProgressView *progressView;
	
	// for downloading the xml data
	NSURLConnection *mediaFeedConnection;
	NSMutableData *mediaData;

	// parse variables 
	MediaItem *currentMediaItem;
	NSMutableArray *currentParseBatch;
	NSUInteger parsedMediaItemCounter;
	NSMutableString *currentParsedCharacterData;
	BOOL accumulatingParsedCharacterData;
	BOOL didAbortParsing;
	BOOL processingMainMediaElement;
	
	BOOL bParsingMediaRSS;
	
}

@property (nonatomic, retain) IBOutlet UIWindow *window;
@property (nonatomic, retain) IBOutlet UINavigationController *navigationController;
@property (nonatomic, retain) IBOutlet AboutViewController *aboutController;
@property (nonatomic, retain) IBOutlet MediaTableViewController *rootViewController;
@property (nonatomic, retain) IBOutlet UITabBarController *tabBarController;
@property (nonatomic, retain) IBOutlet AboutView *aboutView;
@property (nonatomic, retain) ProgressView *progressView;

@property (nonatomic, retain) NSMutableArray *mediaList;

@property (nonatomic, retain) NSURLConnection *mediaFeedConnection;
@property (nonatomic, retain) NSMutableData *mediaData;

@property (nonatomic, retain) MediaItem *currentMediaItem;
@property (nonatomic, retain) NSMutableString *currentParsedCharacterData;
@property (nonatomic, retain) NSMutableArray *currentParseBatch;
@property (nonatomic, retain) UIActivityIndicatorView *activityIndicator;

- (void)addMediaToList:(NSArray *)mediaitems;
- (void)handleError:(NSError *)error;
- (void)initiateUpdate;
- (void)appSettings;

@end
