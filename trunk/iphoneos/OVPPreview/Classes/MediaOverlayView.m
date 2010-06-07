//
//  MediaOverlayView.m
//  MoviePlayer
//
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// Originally created by Petrovic, Tommy (tpetrovi@akamai.com)
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.

#import "MediaOverlayView.h"


NSString *AKAMMediaExtensionsNotification = @""; // remove this line after enabling Media Analytics component


@implementation MediaOverlayView
 
@synthesize metaDisplayLabel, displayDataFromMediaExtensions, textView, infoButton, textViewFrame, metaControlButton;
@synthesize metadataView, metadataViewFrame, metadataThumbnail;
- (void)awakeFromNib
{
  
	// Initialization code for Media Analytics. 
	// For more information contact Akamai.

	[[NSNotificationCenter defaultCenter] 
	 addObserver:self 
	 selector:@selector(processNotificationFromMediaExtensions:) 
	 name:AKAMMediaExtensionsNotification 
	 object:nil];
	
	self.metadataView.hidden = YES;
	self.metadataViewFrame.hidden = YES;
	self.metadataThumbnail.hidden = YES;
	self.textView.hidden = YES;
	self.textViewFrame.hidden = YES;
	// Uncomment the following line to enable Media Analytics app reporting. Contact Akamai for further information.
	//[AKAMMediaAnalytics registerMediaExtCallback:self];

	self.metaDisplayLabel.hidden = YES;
	self.textView.text = @"-Module Not Available!-";
		
}



// Handle touches to the overlay view
- (void)touchesBegan:(NSSet *)touches withEvent:(UIEvent *)event 
{
    UITouch* touch = [touches anyObject];
	
    if (touch.phase == UITouchPhaseBegan)
    {
		

    }    
}

- (BOOL)pointInside:(CGPoint)point withEvent:(UIEvent *)event
{
	// Here we'll test if touches occurred only for the button and the textView. Pass along the touches for all other areas
	// 

	if (!self.textView.hidden && CGRectContainsPoint(self.textView.frame, point))
	{
		return YES;
	}
	else if (CGRectContainsPoint(self.infoButton.frame, point))
	{
		return YES;
	}
	else if (CGRectContainsPoint(self.metaControlButton.frame, point))
	{
		return YES;
	}
	else if (!self.metadataView.hidden &&  CGRectContainsPoint(self.metadataView.frame, point))
	{
		return YES;
	}

	return NO;
}
 
- (id)initWithFrame:(CGRect)frame
{

    if (self = [super initWithFrame:frame]) 
	{
		// Initialization code for accepting notifications from the Media Analytics module.
		// To enable, please contact Akamai.
		[[NSNotificationCenter defaultCenter] 
		 addObserver:self 
		 selector:@selector(processNotificationFromMediaExtensions:) 
		 name:AKAMMediaExtensionsNotification 
		 object:nil];		
    }
    return self;     
} 

#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200
// iPad version of Media Analytics displays data from the module using somewhat different interface.
-(void)setData:(NSString*)data
{
	self.textView.text = data; 
}
-(void)displayableDataDictionary:(NSString*)displayableData
{ 
	[self performSelectorOnMainThread:@selector(setData:) withObject:displayableData waitUntilDone:NO];
	
	return;
	 
}
#endif
 
// This method responds to the Media Analytics notifications, the content of which shows in the overlay view
-(void)processNotificationFromMediaExtensions:(NSNotification *)notification
{ 
	static const CFStringRef newline = CFSTR("\n");
	NSDictionary *returnKVP = [notification userInfo];
	NSMutableString *concatenatedStr = [NSMutableString stringWithCapacity:320]; 

	NSEnumerator *keyEnumerator = [returnKVP keyEnumerator];
	NSEnumerator *objectEnumerator = [returnKVP objectEnumerator];
	id key;
	id value;
	while ((key = [keyEnumerator nextObject])) 
	{
		value = [objectEnumerator nextObject];
		[concatenatedStr appendString:[NSString stringWithFormat:@"%@=%@", key, value]];
		[concatenatedStr appendString:(NSString*)newline];

	}	
	self.textView.text = concatenatedStr;
	[concatenatedStr release];
}


- (void)dealloc 
{
	[[NSNotificationCenter defaultCenter] removeObserver:self
											      name:AKAMMediaExtensionsNotification
                                                  object:nil];
	
    [super dealloc];
}


@end
