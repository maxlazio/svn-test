//
// File: MediaTableViewController.h
// View controller for displaying the list of media items from the feed
//  
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.

#import <UIKit/UIKit.h>
#import "MoviePlayerController.h"


@interface MediaTableViewController : UITableViewController <UIActionSheetDelegate> 
{
    // This array is passed to the controller by the application delegate.
    NSArray *mediaItems;
    // This date formatter is used to convert NSDate objects to NSString objects, using the user's preferred formats.
    NSDateFormatter *dateFormatter;
	MoviePlayerController *mpc;
	
	NSString *prerollMovieURL; 
	
}

@property (nonatomic, retain) NSArray *mediaItems;
@property (nonatomic, retain, readonly) NSDateFormatter *dateFormatter;
@property (nonatomic, retain) IBOutlet MoviePlayerController *mpc; 

@property (nonatomic, retain) NSString *prerollMovieURL;


- (void)viewWillAppear:(BOOL)animated;
- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation;

@end
