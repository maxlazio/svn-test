//
//  MoviePlayerController.m
//  OVPPreview
//
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// Originally created by Petrovic, Tommy (tpetrovi@akamai.com)
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.

#import "MoviePlayerController.h"
#import "MediaOverlayView.h"


#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200
// This is an 'internal' UIViewController supporting new 3.2 video player
@implementation moviePlayerViewController
@synthesize moviePlayerRef;
-(void)loadView
{
	self.view = [[UIView alloc] initWithFrame:[[UIScreen mainScreen] bounds]]; 
	self.view.backgroundColor = [UIColor blackColor];
	
} 
-(void)viewDidLoad
{
	[super viewDidLoad];
	
}

- (void)viewDidDisappear:(BOOL)animated
{
	[moviePlayerRef pause];
	[moviePlayerRef stop];
}

// place movie control here after a view has been re-entered in inline mode, it needs to be
// restarted
- (void)viewDidAppear:(BOOL)animated
{
	
}

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation 
{
	return YES;
}



@end

#endif

@implementation MoviePlayerController
@synthesize mediaExtension;
@synthesize moviePlayer;
@synthesize prMovieURL;
@synthesize mediaURL;
@synthesize overlayView;
@synthesize mediaItem;
@synthesize medItemImage;
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200
@synthesize moviePlayer32ViewController;
#endif

 

-(MoviePlayerController*)init
{
	if (self=[super init])
	{
		moviePlayer = nil;
		playingPreroll = NO;
		self.prMovieURL = nil;
		self.mediaURL = nil;
		overlayButtonState = 0;
	}
	return self;
}  
   
-(void)showAlert:(NSString *)message
{
	UIAlertView *alert = [[UIAlertView alloc] initWithTitle:@"Error" message:message delegate:nil cancelButtonTitle:@"OK" otherButtonTitles:nil];
	[alert show];
	[alert release];
}

-(void)playMovie:(NSURL*)movieURL 
{ 
	// Main function which deals with playing of video using OVPMoviePlayerController
	// 
	// Demonstration of queue playing:
	// To use multi-clip/queue playing capability, use initQueueWithArray: call instantiating the player with an array
	// of NSURL's or objects which adopt <OVPMediaInitialize> protocol. Additionally, a queue can be started by 
	// simply calling enqueueFirstItem:/enqueueLastItem: messages.
	// This second option is to initialize the queue using calls to 
	// enqueueFirstItem/enqueueLastItem on instance of OVPMoviePlayerController class.
	
	// Two examples:
	// Using the array
	// NSArray *movieArray = [NSArray arrayWithObjects:<firstURL>, <secondURL>, .. nil];
	// Example: OVPMoviePlayerController *mpc = [[OVPMoviePlayerController alloc] initQueueWithArray:arrayOfURls];
	// Using the instance of an existing player...
	// Example: OVPStatus status=[player enqueueFirstItem:<url>]; 
	// Make sure the player has been initialized with an url if using enqueuFirst/Last Item calls. These calls are typically used
	// to add new media items during play.
	// Please reference OVPMediaPlayerController project documentation for more info...
	
	NSMutableArray *playItems = [[NSMutableArray alloc] init];
	if (prMovieURL && [prMovieURL scheme])
		[playItems addObject:prMovieURL];
	if (movieURL && [movieURL scheme])
		[playItems addObject:movieURL];
	if ([playItems count]==0) // nothing was added, an error condition
		[self showAlert:@"No media available for play"];
	
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200
	// 3.2+ SDK Implementation slightly different than it's 3.0/3.1 counterpart...

	 
	if (self.moviePlayer)
	{
		[self.moviePlayer.view removeFromSuperview];
		[self.moviePlayer pause];
		[self.moviePlayer stop];
		self.moviePlayer = nil;
	}
	
	self.moviePlayer = [[OVPMoviePlayerController alloc] initPlayerQueueWithArray:playItems];
	self.moviePlayer.scalingMode = MPMovieScalingModeAspectFit;
	self.moviePlayer.controlStyle = MPMovieControlStyleDefault;
	self.moviePlayer.view.frame = moviePlayer32ViewController.view.frame;
	
	self.moviePlayer.view.autoresizingMask = UIViewAutoresizingFlexibleWidth|UIViewAutoresizingFlexibleHeight;
	
	// Adding subviews to the player view controller
	[moviePlayer32ViewController.view addSubview:moviePlayer.view];
	[moviePlayer32ViewController.view addSubview:overlayView];
	[moviePlayer32ViewController.view bringSubviewToFront:overlayView];
	moviePlayer32ViewController.moviePlayerRef = self.moviePlayer;
	
	[self.moviePlayer play];
	 
	 
#else 
	// 3.0/3.1.X implementation
	self.moviePlayer = [[OVPMoviePlayerController alloc] initPlayerQueueWithArray:playItems];
	[self.moviePlayer addSubview:self.overlayView];
	[self.moviePlayer play];
	
#endif 
	
}


-(void)dealloc
{
	[moviePlayer release];
#if defined(__IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200	
	[moviePlayer32ViewController release];
#endif
	[super dealloc];
} 

//
// Various button handlers for processing overlaid UI elements.
//

-(IBAction)overlayViewButtonPress:(id)sender
{
	self.overlayView.textView.hidden = !self.overlayView.textView.hidden;
	if (self.overlayView.textViewFrame)
		self.overlayView.textViewFrame.hidden = !self.overlayView.textViewFrame.hidden;
}

-(IBAction)metaControlButtonPress:(id)sender
{

	overlayButtonState=++overlayButtonState%3;
	self.overlayView.textView.hidden = (overlayButtonState!=2); 
	if (self.overlayView.textViewFrame)
		self.overlayView.textViewFrame.hidden = self.overlayView.textView.hidden;

	
	self.overlayView.metadataView.text = mediaItem.description;

	BOOL isoverlayOn = (overlayButtonState==1||overlayButtonState==2);
	self.overlayView.metadataView.hidden = !isoverlayOn;
	if (self.overlayView.metadataViewFrame)
		self.overlayView.metadataViewFrame.hidden = self.overlayView.metadataView.hidden;
	if (self.overlayView.metadataThumbnail)
		self.overlayView.metadataThumbnail.hidden = self.overlayView.metadataView.hidden;


}

#pragma mark -
#pragma mark UIViewController methods
 
// Implement viewDidLoad to do additional setup after loading the view, typically from a nib.
 
 - (void)viewDidLoad 
 { 
	 [super viewDidLoad];
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200
	 moviePlayer32ViewController = [[moviePlayerViewController alloc] init];
	 moviePlayer32ViewController.view.autoresizesSubviews = YES;
#endif	
	 
 }
  

// Override to allow orientations other than the default portrait orientation.
- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation 
{
    // OK to return YES for all orentations here...
	return YES; 

}

 
- (void)didReceiveMemoryWarning 
{
	// Releases the view if it doesn't have a superview.
    [super didReceiveMemoryWarning];
	
	// Release any cached data, images, etc that aren't in use.
}

- (void)viewDidUnload 
{
	// Release any retained subviews of the main view.
	medItemImage = nil;
}

#pragma mark -
#pragma mark OVPMediaPlayback methods


-(void)movieWillPlay:(OVPMoviePlayerController*)moviePlayer
{
	
}


@end
