//
// OVPPreviewAppDelegate.m
// Controls the download of the RSS data upon starting
//
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.


#import "OVPPreviewAppDelegate.h"
#import "MediaTableViewController.h"
#import "MediaItem.h"

#import <CFNetwork/CFNetwork.h>
#import <OVPMediaPlayerController.h>

#include "Settings.h"

// About will show App Version and name.
@implementation AboutView
@synthesize txtLabel;
- (void)awakeFromNib
{
	[super awakeFromNib];
	self.txtLabel.textAlignment = UITextAlignmentCenter;
	self.txtLabel.lineBreakMode = UILineBreakModeWordWrap;
	self.txtLabel.font = [UIFont fontWithName:@"Georgia" size:64.0f];
	self.autoresizesSubviews = YES;
	NSString *verString = [[[NSBundle mainBundle] infoDictionary] objectForKey:@"CFBundleVersion"];
	NSString *titleString = [NSString stringWithFormat:@"%@ Version:%@", @"OVP-Preview", verString];
	self.txtLabel.text = titleString; 
	
}
 
@end


@implementation AboutViewController

-(BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation
{
	return YES;
}
-(void)willRotateToInterfaceOrientation:(UIInterfaceOrientation)toInterfaceOrientation duration:(NSTimeInterval)duration
{
	OVPPreviewAppDelegate *appDelegate = (OVPPreviewAppDelegate *)[[UIApplication sharedApplication] delegate];
	[appDelegate.progressView UIOrientationChanged:toInterfaceOrientation];
}


@end


@implementation ProgressView

-(void)UIOrientationChanged:(UIInterfaceOrientation)orientation
{
	
	if (orientation == UIInterfaceOrientationLandscapeLeft)
	{
		message.transform = CGAffineTransformMakeRotation(-M_PI/2);
		// iPad version
		message.transform = CGAffineTransformTranslate(message.transform, -28, -50);
	}
	else if (orientation == UIInterfaceOrientationLandscapeRight)
	{
		message.transform = CGAffineTransformMakeRotation(M_PI/2);
		message.transform = CGAffineTransformTranslate(message.transform, 40, -50);
	}
	else if (orientation == UIInterfaceOrientationPortraitUpsideDown)
	{
		message.transform = CGAffineTransformMakeRotation(-M_PI);
		// iPad version
		message.transform = CGAffineTransformTranslate(message.transform, 0, -68);
	}
	else 
	{
		message.transform = CGAffineTransformMakeRotation(M_PI);
		message.transform = CGAffineTransformTranslate(message.transform, 0, -50);
	}

	
	return;
	CGRect newFrame = self.frame;
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED == 30200
	// iPad version
	newFrame.origin.x = 300;
#else
	// iPhone version
	newFrame.origin.x = 190;
#endif
	
	message.frame = newFrame;
	
}

-(ProgressView*)initWithFrame:(CGRect)frame
{
	self = [super initWithFrame:frame];
	[self setBackgroundColor:[UIColor blackColor]];
	self.autoresizingMask = UIViewAutoresizingFlexibleLeftMargin;
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED == 30200
	// iPad version
	message = [[UILabel alloc] initWithFrame:CGRectMake(330.0f, 440.0f, 120.0f, 40.0f)];
#else
	// iPhone version
	message = [[UILabel alloc] initWithFrame:CGRectMake(108.0f, 160.0f, 120.0f, 40.0f)];
#endif

	message.text = @"Please Wait";
	message.textColor = [UIColor whiteColor];
	message.backgroundColor = [UIColor clearColor];
	message.textAlignment = UITextAlignmentLeft;
	message.autoresizingMask = UIViewAutoresizingFlexibleLeftMargin;
	message.lineBreakMode = UILineBreakModeWordWrap;
	message.font = [UIFont boldSystemFontOfSize:20.0f];
	
	
	[self addSubview:message];
	[message release];
	return self;
}

-(void)presentView
{
	CGContextRef context = UIGraphicsGetCurrentContext();
	[UIView beginAnimations:nil context:context];
	[UIView setAnimationCurve:UIViewAnimationCurveEaseInOut];
	[UIView setAnimationDuration:0.7];
	CGRect rect=[self frame];
	rect.origin.y = 0.0f;
	[self setFrame:rect];
	[UIView commitAnimations];
}

-(void)removeView
{
	CGContextRef context = UIGraphicsGetCurrentContext();
	[UIView beginAnimations:nil context:context];
	[UIView setAnimationCurve:UIViewAnimationCurveEaseInOut];
	[UIView setAnimationDuration:0.8];
	CGRect rect=[self frame];
	rect.origin.y = -10.0f - rect.size.height;
	[self setFrame:rect];
	[UIView commitAnimations];
}

-(void)dealloc
{
	[message release];
	[super dealloc];
}

@end



@implementation OVPPreviewAppDelegate

@synthesize window;
@synthesize navigationController;
@synthesize rootViewController;
@synthesize mediaList;
@synthesize mediaFeedConnection;
@synthesize mediaData;
@synthesize currentMediaItem;
@synthesize currentParsedCharacterData;
@synthesize currentParseBatch;
@synthesize tabBarController;
@synthesize activityIndicator;
@synthesize aboutView;
@synthesize aboutController;
@synthesize progressView;


#pragma mark UIViewController methods

 
//
// This method is called when app starts and subsequently when url to the feed has been updated/changed 
// to reprocess the feed
//
 
- (void)initiateUpdate
{
	self.mediaList = [NSMutableArray array];
    rootViewController.mediaItems = mediaList;
	
	NSString *feedOrMediaURL = [[NSUserDefaults standardUserDefaults] stringForKey:kRSSURL];

	if ([feedOrMediaURL rangeOfString:@".m3u8" options:NSCaseInsensitiveSearch].length>0 ||
		[feedOrMediaURL rangeOfString:@".m4v" options:NSCaseInsensitiveSearch].length>0 ||
		[feedOrMediaURL rangeOfString:@".mp4" options:NSCaseInsensitiveSearch].length>0)
	{
		// This block of code is responsible for processing direct media urls as oppose to the RSS feeds
		// i.e. user can enter direct media urls to m3u8,mp4,m4v files only. We'll simply construct an ad-hoc RSS feed 
		// from that and process it just as if it was coming from another url. 
		// This is to simplify insertions of ad-hoc media items in the list and keep the seamless user experience
		NSString *rssTemplate = @"<?xml version=\"1.0\" encoding=\"UTF-8\"?>  \
		<rss xmlns:media=\"http://video.search.yahoo.com/mrss/\" version=\"2.0\"> \
		<channel> \
		<item> \
		<title>User Selected Content...</title> \
		<description>User selected content. No description.</description> \
		<media:content medium=\"video\" url=\"%@\" /> \
		</item> \
		</channel> \
		</rss>";
		
		NSString *constructedFeed = [NSString stringWithFormat:rssTemplate, feedOrMediaURL];
		NSData *feedData = [constructedFeed dataUsingEncoding:NSUTF8StringEncoding];
		// Get a new thread going to fetch the media list from RSS so that UI is not blocked 
		[NSThread detachNewThreadSelector:@selector(parseRSSData:) toTarget:self withObject:feedData];
	}
	else 
	{
		// Create an URL request to the 'standard' (not ad-hoc) feed data
		NSURL *feedURL = [NSURL URLWithString:feedOrMediaURL];
		if (feedURL)
		{
			NSURLRequest *urlRequest = [NSURLRequest requestWithURL:feedURL];
			// Use NSURLConnection asynchronously to avoid main thread blocking
			self.mediaFeedConnection = [[[NSURLConnection alloc] initWithRequest:urlRequest delegate:self] autorelease];
			
			//TODO: add handling of the failed connection 
			NSAssert(self.mediaFeedConnection != nil, @"Connection failed.");
		
			[progressView setAlpha:0.7f]; // we'll present the Progress View dimmed
			[progressView presentView];
		
			[UIApplication sharedApplication].networkActivityIndicatorVisible = YES;
			if (self.activityIndicator) [self.activityIndicator startAnimating];
		}
		else  NSLog(@"Unable to retrieve feedURL");
	}
}

 // Implement viewDidLoad to do additional setup after loading the view, typically from a nib.
 - (void)viewDidLoad 
{
	
	[super viewDidLoad];
	// Set up activity indicator and Progress View
	activityIndicator = [[UIActivityIndicatorView alloc] initWithFrame:CGRectMake(0, 0, 32.0f, 32.0f)];

#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED == 30200
	[activityIndicator setCenter:CGPointMake(382.0f, 500.0f)];
#else
	[activityIndicator setCenter:CGPointMake(160.0f, 208.0f)];
#endif
	progressView = [[ProgressView alloc] initWithFrame:[[UIScreen mainScreen] bounds]]; 
	[activityIndicator setActivityIndicatorViewStyle:UIActivityIndicatorViewStyleWhite];
	[[[UIApplication sharedApplication] keyWindow] addSubview:progressView]; 
	[progressView setAlpha:0.0f]; 
	[[[UIApplication sharedApplication] keyWindow] addSubview:activityIndicator];
#if 0/* !defined( __IPHONE_OS_VERSION_MIN_REQUIRED) || __IPHONE_OS_VERSION_MIN_REQUIRED < 30200 */
	// call this only on iPhone devices, because for iPad we'll manage this call differently tp.
	[self initiateUpdate];
#endif
	 
} 

-(void)willRotateToInterfaceOrientation:(UIInterfaceOrientation)toInterfaceOrientation duration:(NSTimeInterval)duration
{
	[progressView UIOrientationChanged:toInterfaceOrientation];
}
// Override to allow orientations other than the default portrait orientation.
- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation 
{
	// We want to enable orientations for iPad to allow/support inline player orentation
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200
	return YES; 
#else
	return (interfaceOrientation == UIInterfaceOrientationPortrait);
#endif
}
   

- (void)didReceiveMemoryWarning {
	// Releases the view if it doesn't have a superview.
    [super didReceiveMemoryWarning];
	
	// Release any cached data, images, etc that aren't in use.
}

- (void)viewDidUnload {
	// Release any retained subviews of the main view.
	// e.g. self.myOutlet = nil;
}
	
 
#pragma mark -

- (void)dealloc 
{
	[mediaFeedConnection release];
	[mediaData release];
	[navigationController release];
	[rootViewController release];
	[window release];
	[mediaList release];
	[currentMediaItem release];
	[currentParsedCharacterData release];
	[currentParseBatch release];
	[tabBarController release];
	[activityIndicator release];
	[progressView release];
	[super dealloc];
}

- (void)applicationDidFinishLaunching:(UIApplication *)application 
{
	[window addSubview:tabBarController.view];
	[window makeKeyAndVisible];
	[self appSettings];
	[self.view insertSubview:navigationController.view atIndex:0]; 	// Add the navigation view controller to the window.
}

- (void)applicationWillTerminate:(UIApplication *)application
{
	[OVPMoviePlayerController deinitializeOVP];  
}

// set up application settings here... 
-(void)appSettings
{ 
	 	 
	NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];	
	NSDictionary *appDefaults =  [NSDictionary dictionaryWithObjectsAndKeys:
								  @"http://publisher.edgesuite.net/iphonedemo/feed2.xml", kRSSURL,
								  @"http://79423.analytics.edgesuite.net/html5/config/iPhone_htmlConfiguration.xml", kMediaAnalyticsURL,
								  @"", kPrerollMovieClipURL,
								  nil]; 
	
	[defaults registerDefaults:appDefaults];
	[defaults synchronize];
	[OVPMoviePlayerController initializeOVP]; // initialized OVP subsistem
	
	
}

#pragma mark NSURLConnection delegate methods


- (void)connection:(NSURLConnection *)connection didReceiveResponse:(NSURLResponse *)response 
{
    self.mediaData = [NSMutableData data];
}

- (void)connection:(NSURLConnection *)connection didReceiveData:(NSData *)data 
{
    [mediaData appendData:data];
}

- (void)connection:(NSURLConnection *)connection didFailWithError:(NSError *)error 
{
    [UIApplication sharedApplication].networkActivityIndicatorVisible = NO; 
	if (self.activityIndicator)
		[self.activityIndicator stopAnimating];
    if ([error code] == kCFURLErrorNotConnectedToInternet) {
        NSDictionary *userInfo = [NSDictionary dictionaryWithObject:NSLocalizedString(@"Error",
																					  @"Failure to connect.") forKey:NSLocalizedDescriptionKey];
        NSError *noConnectionError = [NSError errorWithDomain:NSCocoaErrorDomain code:kCFURLErrorNotConnectedToInternet userInfo:userInfo];
        [self handleError:noConnectionError];
    } else {
        [self handleError:error];
    }
    self.mediaFeedConnection = nil;
}

- (void)connectionDidFinishLoading:(NSURLConnection *)connection 
{
    self.mediaFeedConnection = nil;
    [UIApplication sharedApplication].networkActivityIndicatorVisible = NO;  
	[self.activityIndicator stopAnimating];
	[progressView removeView];
    // Detach a thread to fetch the media list from RSS to avoid blocking the main UI thread
    [NSThread detachNewThreadSelector:@selector(parseRSSData:) toTarget:self withObject:mediaData];
    // remove the reference to the mediaData; not needed after parsing is done
    self.mediaData = nil;
}

- (void)parseRSSData:(NSData *)data 
{
    // Must create an autorelease pool for all secondary threads
    NSAutoreleasePool *pool = [[NSAutoreleasePool alloc] init];

	accumulatingParsedCharacterData = NO;
	didAbortParsing = NO;
	processingMainMediaElement = NO;
	bParsingMediaRSS = NO;
    
    self.currentParseBatch = [NSMutableArray array];
    self.currentParsedCharacterData = [NSMutableString string];
    NSXMLParser *parser = [[NSXMLParser alloc] initWithData:data];
    [parser setDelegate:self];
    [parser parse];

 	// Typical processing in 'batches'; send to the main thread current batch for adding to the list
    if ([self.currentParseBatch count] > 0) 
	{
        [self performSelectorOnMainThread:@selector(addMediaToList:) withObject:self.currentParseBatch waitUntilDone:NO];
    }
    self.currentParseBatch = nil;
    self.currentMediaItem = nil;
    self.currentParsedCharacterData = nil;
    [parser release];        
    [pool release];
}


// This is called on the main thread with batches of parsed media item objects. 
- (void)addMediaToList:(NSArray *)mediaitems 
{
    [self.mediaList addObjectsFromArray:mediaitems];
    // The table needs to be reloaded to reflect the new content of the list.
    [rootViewController.tableView reloadData];
}

- (void)handleError:(NSError *)error 
{
    NSString *errorMessage = [error localizedDescription];
    UIAlertView *alertView = [[UIAlertView alloc] initWithTitle:NSLocalizedString(@"Error!", @"") 
														message:errorMessage 
													   delegate:nil 
											  cancelButtonTitle:@"OK" otherButtonTitles:nil];
    [alertView show];
    [alertView release];
	[UIApplication sharedApplication].networkActivityIndicatorVisible = NO;  
	[self.activityIndicator stopAnimating];
	[progressView removeView];
	
}

#pragma mark Parser constants

static const const NSUInteger kMaxNumOfMediaItemsToParse = 100;


static NSUInteger const kSizeOfParsingBatch = 10;

// Parsing elements for Atom feeds
static NSString * const kEntryElementName = @"entry";
static NSString * const kLinkElementName = @"link";
static NSString * const kTitleElementName = @"title";
static NSString * const kUpdatedElementName = @"updated";
static NSString * const kSummaryElementName = @"summary";
static NSString * const kTumbnailElementName = @"im:image";

// Parsing elements for Media-RSS feeds
static NSString * const kMRSSElementName = @"rss";
static NSString * const kMRSSChannelElementName = @"channel";
static NSString * const kMRSSItemElementName = @"item";
static NSString * const kMRSSDescriptionElementName = @"description";
static NSString * const kMRSSMediaContentElementName = @"media:content";
static NSString * const kMRSSTitleElementName = @"title";
static NSString * const kMRSSUpdatedElementName = @"pubDate";
static NSString * const kMRSSTumbnailElementName = @"media:thumbnail";


#pragma mark NSXMLParser delegate methods

// Parsing both types of feeds, Media RSS and Atom. 
// We choose to parse both types of feeds in this funciton instead of devising a more elaborate but more flexible scheme. We choose simplicity
// mostly due to a small number of parsing elements/attributes we have to handle for each 'type' RSS. 
- (void)parser:(NSXMLParser *)parser didStartElement:(NSString *)elementName namespaceURI:(NSString *)namespaceURI qualifiedName:(NSString *)qName attributes:(NSDictionary *)attributeDict 
{
    // If the number of parsed items is greater than kMaxNumOfMediaItemsToParse, abort the parse.
    if (parsedMediaItemCounter >= kMaxNumOfMediaItemsToParse) 
	{
		didAbortParsing = YES; // use this flag to separate deliberate stop from parse errors
		[parser abortParsing];
    }
	// Here we test for Media RSS vs. Atom feeds
	if ([elementName isEqualToString:kMRSSElementName])
	{
		bParsingMediaRSS = YES;
	}
	if (bParsingMediaRSS)
	{
		if ([elementName isEqualToString:kMRSSItemElementName]) 
		{
			MediaItem *mi = [[MediaItem alloc] init];
			self.currentMediaItem = mi;
			self.currentMediaItem.mediaURL = nil;
			self.currentMediaItem.thumbnailURL = nil;
			processingMainMediaElement = YES; // this flag is there to avoid processing of similarly named tags from other tree hierarchy
			[mi release];
		}
		else if (processingMainMediaElement)
		{
			if ([elementName isEqualToString:kMRSSMediaContentElementName])
			{
				// look for 'video' type
				if (!self.currentMediaItem.mediaURL)
				{
					// dealing with a video link, store it
					self.currentMediaItem.mediaURL = [attributeDict valueForKey:@"url"];
				}
			} 
			else if ([elementName isEqualToString:kMRSSTumbnailElementName])
			{
				self.currentMediaItem.thumbnailURL = [attributeDict valueForKey:@"url"];
				[self.currentMediaItem loadThumbnailImage];

			}
			else if ([elementName isEqualToString:kMRSSTitleElementName] || [elementName isEqualToString:kMRSSUpdatedElementName] || [elementName isEqualToString:kMRSSDescriptionElementName]) 
			{
				// For the 'title', 'updated' and 'description', begin accumulating character data.
				accumulatingParsedCharacterData = YES;
				// The mutable string needs to be reset to empty.
				[currentParsedCharacterData setString:@""];
			}
			
		}
		
	}
	else // parsing Atom feeds
	{
		if ([elementName isEqualToString:kEntryElementName]) 
		{
			MediaItem *mi = [[MediaItem alloc] init];
			self.currentMediaItem = mi;
			self.currentMediaItem.mediaURL = nil;
			self.currentMediaItem.thumbnailURL = nil;
			processingMainMediaElement = YES; // this flag is there to avoid processing of similarly named tags from other tree hierarchy
			[mi release];
		}
		else if (processingMainMediaElement && [elementName isEqualToString:kLinkElementName])
		{
			NSString *relAttribute = [attributeDict valueForKey:@"type"];
			// look for 'video' type
			NSRange foundVideoTag = [relAttribute rangeOfString:@"video"];
			if (!self.currentMediaItem.mediaURL && foundVideoTag.length>0)
			{
				// dealing with a video link, store it
				self.currentMediaItem.mediaURL = [attributeDict valueForKey:@"href"];
			}
		} 
		else if (processingMainMediaElement && [elementName isEqualToString:kTitleElementName] || [elementName isEqualToString:kUpdatedElementName] || [elementName isEqualToString:kTumbnailElementName] || [elementName isEqualToString:kSummaryElementName]) 
		{
			// For the 'title', 'updated' and image/thumbnail elements, begin accumulating character data.
			// The contents are collected in parser:foundCharacters:.
			accumulatingParsedCharacterData = YES;
			// The mutable string needs to be reset to empty.
			[currentParsedCharacterData setString:@""];
		}
	}
}

- (void)parser:(NSXMLParser *)parser didEndElement:(NSString *)elementName namespaceURI:(NSString *)namespaceURI qualifiedName:(NSString *)qName 
{     
    
	if (bParsingMediaRSS)
	{
		// media RSS has only a couple of elements which will collect/parse on
		if ([elementName isEqualToString:kMRSSItemElementName]) 
		{
			[self.currentParseBatch addObject:self.currentMediaItem];
			parsedMediaItemCounter++;
			if (parsedMediaItemCounter % kSizeOfParsingBatch == 0) 
			{
				[self performSelectorOnMainThread:@selector(addMediaToList:) withObject:self.currentParseBatch waitUntilDone:NO];
				self.currentParseBatch = [NSMutableArray array];
			}
		} 
		else if ([elementName isEqualToString:kMRSSTitleElementName]) 
		{ 
			// Title element is sampled verbatim
			NSScanner *scanner = [NSScanner scannerWithString:self.currentParsedCharacterData];
			NSString *title = nil;
			// Scan the string.
			[scanner scanUpToCharactersFromSet:[NSCharacterSet illegalCharacterSet]  intoString:&title];
			self.currentMediaItem.title = title;
		} 
		else if ([elementName isEqualToString:kMRSSUpdatedElementName]) 
		{
			// Enable the following to format the 'updated' date, for now,
			// the update date will not be displayed. 
			//NSDateFormatter *dateFormatter = [[[NSDateFormatter alloc] init] autorelease];
			//[dateFormatter setDateFormat:@"yyyy-MM-dd'T'HH:mm:ss"];
			//self.currentMediaItem.updated = [dateFormatter dateFromString:self.currentParsedCharacterData];
		} 
		else if ([elementName isEqualToString:kMRSSDescriptionElementName]) 
		{
			NSScanner *scanner = [NSScanner scannerWithString:self.currentParsedCharacterData];
			NSString *description = nil;
			[scanner scanUpToCharactersFromSet:[NSCharacterSet illegalCharacterSet]  intoString:&description];
			self.currentMediaItem.description = description;			
		} 
		
	}
	else // parsing Atom 1.0 feeds
	{
		if ([elementName isEqualToString:kEntryElementName]) 
		{
			[self.currentParseBatch addObject:self.currentMediaItem];
			parsedMediaItemCounter++;
			if (parsedMediaItemCounter % kSizeOfParsingBatch == 0) 
			{
				[self performSelectorOnMainThread:@selector(addMediaToList:) withObject:self.currentParseBatch waitUntilDone:NO];
				self.currentParseBatch = [NSMutableArray array];
			}
		} 
		else if ([elementName isEqualToString:kTitleElementName]) 
		{
			// Title element is sampled verbatim
			NSScanner *scanner = [NSScanner scannerWithString:self.currentParsedCharacterData];
			NSString *title = nil;
			// Scan the string.
			[scanner scanUpToCharactersFromSet:[NSCharacterSet illegalCharacterSet]  intoString:&title];
			self.currentMediaItem.title = title;
		} 
		else if ([elementName isEqualToString:kSummaryElementName]) 
		{
			// 'summary' element is collected as is
			NSScanner *scanner = [NSScanner scannerWithString:self.currentParsedCharacterData];
			NSString *summary = nil;
			// Scan the string to the end.
			[scanner scanUpToCharactersFromSet:[NSCharacterSet illegalCharacterSet]  intoString:&summary];
			self.currentMediaItem.description = summary;
		} 
		else if ([elementName isEqualToString:kUpdatedElementName]) 
		{
			NSDateFormatter *dateFormatter = [[[NSDateFormatter alloc] init] autorelease];
			[dateFormatter setDateFormat:@"yyyy-MM-dd'T'HH:mm:ss"];
			self.currentMediaItem.updated = [dateFormatter dateFromString:self.currentParsedCharacterData];
		} 
		else if ([elementName isEqualToString:kTumbnailElementName]) 
		{
			if (!self.currentMediaItem.thumbnailURL)
			{	
				NSScanner *scanner = [NSScanner scannerWithString:self.currentParsedCharacterData];
				NSString *urlToThumbnail = nil;
				[scanner scanUpToString:@"\"" intoString:&urlToThumbnail];
				self.currentMediaItem.thumbnailURL = urlToThumbnail;
				[self.currentMediaItem loadThumbnailImage];
			}
			
		}
	}
    // Stop accumulating character data. Start again when begining of these elements are found again in the document
    accumulatingParsedCharacterData = NO;
}

// Following methods were adapted from Apple's parser code

// Called by the parser when it find parsed character data in an element. 
// Accumulate data until the end of the element is reached.
- (void)parser:(NSXMLParser *)parser foundCharacters:(NSString *)string 
{
    if (accumulatingParsedCharacterData) 
	{
        // If the current element is one whose content we care about, append 'string'
        // to the property that holds the content of the current element.
        [self.currentParsedCharacterData appendString:string];
    }
}

- (void)parser:(NSXMLParser *)parser parseErrorOccurred:(NSError *)parseError 
{
	// If there's a record mismatch between 
    // If the number of records received is greater than max number allowed - abort parsing.
    // The parser will report this as an error, but it won't be treated as error due to the flag didAbortParsing. 
    if (didAbortParsing == NO) 
	{
        // Pass the error to the main thread for handling.
        [self performSelectorOnMainThread:@selector(handleError:) withObject:parseError waitUntilDone:NO];
    }
}

@end
