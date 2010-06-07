//
// File: MediaTableViewController.m
// View controller for displaying the media list.
//
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.

#import "MediaTableViewController.h"
#import "MediaItem.h"
#import "OVPPreviewAppDelegate.h"

#include "Settings.h"
#include <Availability.h>

#import <MediaPlayer/MediaPlayer.h>


@implementation MediaTableViewController

@synthesize mediaItems;
@synthesize mpc;
@synthesize prerollMovieURL;


- (void)dealloc 
{
    [mediaItems release];
    [dateFormatter release];
	[mpc release];
	[prerollMovieURL release];
	[super dealloc];
}

-(void)willRotateToInterfaceOrientation:(UIInterfaceOrientation)toInterfaceOrientation duration:(NSTimeInterval)duration
{
	OVPPreviewAppDelegate *appDelegate = (OVPPreviewAppDelegate *)[[UIApplication sharedApplication] delegate];
	[appDelegate.progressView UIOrientationChanged:toInterfaceOrientation];
}

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation 
{
	
	return YES;
}

- (void)viewWillAppear:(BOOL)animated
{

	[super viewWillAppear:animated];
#if 1 /*defined(__IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200*/
	// For iPad device only, we initiate update on this screen
	static NSString *existingURL = @"";
	NSString *selectedURL = [[NSUserDefaults standardUserDefaults] stringForKey:kRSSURL];
	if (![selectedURL isEqualToString:existingURL])
	{
		existingURL = selectedURL;
		// initiate update to the table if url's been changed
		[(OVPPreviewAppDelegate *)[[UIApplication sharedApplication] delegate] initiateUpdate]; 
	}
#endif

}
 
- (void)viewDidLoad 
{
    [super viewDidLoad];

    // The table row height is not the standard value. Since all the rows have the same height, it is more efficient to
    // set this property on the table, rather than using the delegate method -tableView:heightForRowAtIndexPath:.
    self.tableView.rowHeight = 62.0;
}

// On-demand initializer for read-only property.
- (NSDateFormatter *)dateFormatter 
{
    if (dateFormatter == nil) {
        dateFormatter = [[NSDateFormatter alloc] init];
        [dateFormatter setDateStyle:NSDateFormatterMediumStyle];
        [dateFormatter setTimeStyle:NSDateFormatterMediumStyle];
    }
    return dateFormatter;
}


// The number of rows is equal to the number of media items in the array.
- (NSInteger)tableView:(UITableView *)tableView numberOfRowsInSection:(NSInteger)section 
{
    return [mediaItems count];
}

// This UITableViewCell has standard behavior. 'tags' are used to identify specific control, i.e. label, imageview etc.
- (UITableViewCell *)tableView:(UITableView *)tableView cellForRowAtIndexPath:(NSIndexPath *)indexPath 
{
    // Each subview in the cell will be identified by a unique tag.
    static NSUInteger const kTitleLabelTag = 2;
    static NSUInteger const kDateLabelTag = 3;
    static NSUInteger const kThumbnailLabelTag = 4;
    static NSUInteger const kThumbnailImageTag = 5;
    @try 
	{
    
		// Declare references to the subviews 
		UILabel *locationLabel = nil;
		UILabel *dateLabel = nil;
		UILabel *thumbnailLabel = nil;
		UIImageView *thumbnailImage = nil;
		
		static NSString *kMediaItemCellID = @"MICellID";    
		UITableViewCell *cell = [tableView dequeueReusableCellWithIdentifier:kMediaItemCellID];
		if (cell == nil) {
			// No reusable cell was available, so we create a new cell and configure its subviews.
			cell = [[[UITableViewCell alloc] initWithStyle:UITableViewCellStyleDefault reuseIdentifier:kMediaItemCellID] autorelease];
			
			locationLabel = [[[UILabel alloc] initWithFrame:CGRectMake(8, 13, 258, 12)] autorelease];
			locationLabel.tag = kTitleLabelTag;
			locationLabel.font = [UIFont boldSystemFontOfSize:12];
			locationLabel.textAlignment  = UITextAlignmentLeft;
			locationLabel.lineBreakMode = UILineBreakModeTailTruncation;
			[cell.contentView addSubview:locationLabel];
			
			dateLabel = [[[UILabel alloc] initWithFrame:CGRectMake(10, 38, 80, 14)] autorelease];
			dateLabel.tag = kDateLabelTag;
			dateLabel.font = [UIFont systemFontOfSize:10];
			[cell.contentView addSubview:dateLabel];

			thumbnailLabel = [[[UILabel alloc] initWithFrame:CGRectMake(263, 2, 50, 58)] autorelease];
			thumbnailLabel.tag = kThumbnailLabelTag;

			thumbnailImage = [[[UIImageView alloc] initWithImage:[UIImage imageNamed:@"empty.png"]] autorelease];
			CGRect imageFrame = thumbnailLabel.frame;
			imageFrame.origin = CGPointMake(264, 2);
			thumbnailImage.frame = imageFrame;
			thumbnailImage.tag = kThumbnailImageTag;
			thumbnailImage.autoresizingMask = UIViewAutoresizingFlexibleLeftMargin;
			[cell.contentView addSubview:thumbnailImage];
			
		
		} else {
			// A reusable cell was available, so we just need to get a reference to the subviews using their tags.
			locationLabel = (UILabel *)[cell.contentView viewWithTag:kTitleLabelTag];
			dateLabel = (UILabel *)[cell.contentView viewWithTag:kDateLabelTag];
			thumbnailLabel = (UILabel *)[cell.contentView viewWithTag:kThumbnailLabelTag];
			thumbnailImage = (UIImageView *)[cell.contentView viewWithTag:kThumbnailImageTag];
		}
 
		// Get the specific media item for this row
		MediaItem *medItem = [mediaItems objectAtIndex:indexPath.row];
		
		// Set the relevant data for each subview in the cell.
		locationLabel.text = medItem.title;
		dateLabel.text = [self.dateFormatter stringFromDate:medItem.updated];
		// set the thumbnail
		[thumbnailImage setImage:medItem.thumbImage];
		return cell;
	}
	@catch (NSException * e) 
	{
		NSLog(@"Exception occured:%@",[e description]);
	} 
	return nil; 
}
 
    
 
 
// When the user taps a row in the table, play media associated with that cell/row
- (void)tableView:(UITableView *)tableView didSelectRowAtIndexPath:(NSIndexPath *)indexPath 
{	   
    NSIndexPath *selectedIndexPath = [self.tableView indexPathForSelectedRow];
    MediaItem *medItem = (MediaItem *)[mediaItems objectAtIndex:selectedIndexPath.row];
	NSURL *movieURL = [NSURL URLWithString:[medItem mediaURL]];
	
	self.prerollMovieURL = [[NSUserDefaults standardUserDefaults] stringForKey:kPrerollMovieClipURL];
	[self.tableView deselectRowAtIndexPath:selectedIndexPath animated:YES];
	if (!self.mpc) 
		self.mpc = [[MoviePlayerController alloc] init]; 
	
	self.mpc.mediaItem = medItem;
	// Set the thumbnail image for displaying as overlay
 	
	self.mpc.prMovieURL = [NSURL URLWithString:self.prerollMovieURL]; 
#if defined(__IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200	
	// Set the thumbnail image for displaying as overlay
	if (self.mpc.overlayView.metadataThumbnail)
	{
		[self.mpc.overlayView.metadataThumbnail setBackgroundImage:medItem.thumbImage forState:UIControlStateNormal];
		
	}   
	[[self navigationController] pushViewController:self.mpc.moviePlayer32ViewController animated:YES];
#else // 3.0/3.1 implementation
	if (self.mpc.overlayView.metaControlButton)
	{
		[self.mpc.overlayView.metaControlButton setBackgroundImage:medItem.thumbImage forState:UIControlStateNormal];
		
	} 		
#endif
	[self.mpc playMovie:movieURL]; // delegate the call to start the playback including this view controller needed for 3.2 in-line video playback
	

}
 
// Called when the user selects an option in the sheet. The sheet will be automatically dismissed.
- (void)actionSheet:(UIActionSheet *)actionSheet willDismissWithButtonIndex:(NSInteger)buttonIndex 
{
}

@end

