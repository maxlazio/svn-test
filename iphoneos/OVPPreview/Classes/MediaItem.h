//
// File: MediaItem.h 
//
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// Originally created by Petrovic, Tommy (tpetrovi@akamai.com)
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.

#import <Foundation/Foundation.h>

@interface MediaItem : NSObject 
{
@private

    NSString *title;
	NSString *name;
    // Holds the updated date for the media item
    NSDate *updated;
    NSString *mediaURL;
	NSString *thumbnailURL;
	NSString *description;
	UIImage *thumbImage;
}

@property (nonatomic, retain) NSString *title;
@property (nonatomic, retain) NSString *name;
@property (nonatomic, retain) NSDate *updated;
@property (nonatomic, retain) NSString *mediaURL;
@property (nonatomic, retain) NSString *thumbnailURL;
@property (nonatomic, retain) NSString *description;
@property (nonatomic, retain) UIImage *thumbImage;

-(void)loadThumbnailImage;

@end
