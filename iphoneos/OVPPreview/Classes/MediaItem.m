//
//     File: MediaItem.m
//
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// Originally created by Petrovic, Tommy (tpetrovi@akamai.com)
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.


#import "MediaItem.h"

@implementation MediaItem

@synthesize title;
@synthesize name;
@synthesize updated;
@synthesize mediaURL;
@synthesize thumbnailURL;
@synthesize description;
@synthesize thumbImage;


-(void)loadThumbnailImage
{
	if (thumbnailURL && ![thumbnailURL isEqualToString:@""])
	{
		NSURL *urlToImage;
		if (urlToImage = [NSURL URLWithString:thumbnailURL])
		{
			NSData *imageData = [NSData dataWithContentsOfURL:urlToImage];
			if (imageData)
				thumbImage = [[UIImage imageWithData:imageData] retain];
		}
	}
}

- (void)dealloc 
{
    [title release];
	[name release];
    [updated release];
    [mediaURL release];
	[thumbnailURL release];
	[description release];
	[thumbImage release];
    [super dealloc];
}

@end
