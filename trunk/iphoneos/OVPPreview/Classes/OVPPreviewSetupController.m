//
//  OVPPreviewSetupController.m
//  OVPPreview
//
//
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// Originally created by Petrovic, Tommy (tpetrovi@akamai.com)
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.

#import "OVPPreviewSetupController.h"
#import "OVPPreviewAppDelegate.h"

#include "Settings.h"
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED == 30200
#define kLeftMargin			80.0
#define kTopMargin			20.0
#define kRightMargin			20.0

#define kTextFieldHeight	30.0
#define kTextFieldWidth		560.0
#elif __IPHONE_OS_VERSION_MIN_REQUIRED > 30200
#define kLeftMargin			-20.0
#define kTopMargin			15.0
#define kRightMargin			10.0

#define kTextFieldHeight	30.0
#define kTextFieldWidth		260.0
#else
#define kLeftMargin			-18.0
#define kTopMargin			10.0
#define kRightMargin			20.0

#define kTextFieldHeight	30.0
#define kTextFieldWidth		260.0
#endif
static NSString *kSectionTitleKey = @"sectionTitleKey";
static NSString *kPickerKey = @"pickKey";
static NSString *kViewKey = @"viewKey";
const NSInteger kViewTag = 1;

#pragma mark -
@implementation PickerViewController
@synthesize pickerArrayRSS, pickerArrayMovieClipURL, currentPicker, RSSURLPickerView, movieURLPickerView;
@synthesize button;
@synthesize RSSURLSelected, movieURLSelected;

-(PickerViewController*)init
{
	 
	pickerArrayRSS = [[NSArray arrayWithObjects:
					   @"Publisher Test Feed", @"http://publisher.edgesuite.net/iphonedemo/feed2.xml",
					   @"BBC Planet Earth", @"http://www.smoothhd.com/media/earth/iphone/master.m3u8",
					   @"Big Buck Bunny", @"http://mirror.cessen.com/blender.org/peach/trailer/trailer_iphone.m4v",
					   nil] retain];
	pickerArrayMovieClipURL = [[NSArray arrayWithObjects:
								@"--no clips--", @"",
								@"10 second clip", @"http://publisher.edgesuite.net/iphonedemo/tensecondclip.mp4",
								nil] retain];
	return [super init];
	
}
-(void)willRotateToInterfaceOrientation:(UIInterfaceOrientation)toInterfaceOrientation duration:(NSTimeInterval)duration
{
	OVPPreviewAppDelegate *appDelegate = (OVPPreviewAppDelegate *)[[UIApplication sharedApplication] delegate];
	[appDelegate.progressView UIOrientationChanged:toInterfaceOrientation];
}

// Override to allow orientations other than the default portrait orientation.
- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation 
{
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200
	return YES; 
#else
	return (interfaceOrientation == UIInterfaceOrientationPortrait);
#endif
}


-(void) loadView
{
	self.view = [[UIView alloc] initWithFrame:[[UIScreen mainScreen] applicationFrame]];
	self.view.backgroundColor = [UIColor darkGrayColor];
}

-(void) viewDidLoad
{	
	[super viewDidLoad];
	[self createPickers];
//	[self addPickerDismissButton];

}

- (void)viewDidUnload {
	// Release any retained subviews of the main view.
	// e.g. self.myOutlet = nil;
	[super viewDidUnload];
	
	// release and set out IBOutlets to nil	
	self.pickerArrayRSS = nil;
	self.pickerArrayMovieClipURL = nil;
	self.currentPicker = nil;
	self.RSSURLPickerView = nil;
	self.movieURLPickerView = nil;
	self.button = nil;
}
-(void)dealloc
{
	[pickerArrayRSS release];
	[pickerArrayMovieClipURL release];
	[RSSURLPickerView release];
	[movieURLPickerView release];
	[currentPicker release];
	[button release];
	[super dealloc];
	
}

#pragma mark UIPickerView

- (CGRect)pickerFrameWithSize:(CGSize)size
{
	CGRect pickerRect = CGRectMake(	0.0,
								   0.0, 
								   size.width, 
								   size.height);
	return pickerRect;
}


- (void)createOnePicker:(UIPickerView**)hPicker
{
 	// note we are using CGRectZero for the dimensions of our picker view,
	// this is because picker views have a built in optimum size,
	// you just need to set the correct origin in your view.
	//
	// position the picker at the bottom
	//*hPicker = [[UIView alloc] initWithFrame:[[UIScreen mainScreen] applicationFrame]];
	//(*hPicker).backgroundColor = [UIColor lightGrayColor];
	*hPicker = [[UIPickerView alloc] initWithFrame:CGRectZero];
	CGSize pickerSize = [RSSURLPickerView sizeThatFits:CGSizeZero];
	(*hPicker).frame = [self pickerFrameWithSize:pickerSize];
	(*hPicker).backgroundColor = [UIColor lightGrayColor];
	
	(*hPicker).autoresizingMask = UIViewAutoresizingFlexibleWidth;
	(*hPicker).showsSelectionIndicator = YES;	// note this is default to NO
	
	// this view controller is the data source and delegate
	(*hPicker).delegate  = self;
	(*hPicker).dataSource = self;
	
	// add this picker to our view controller, initially hidden
	(*hPicker).hidden = YES;
	
}

// Here's where we initialize a basic set of feeds for the picker
- (void)createPickers
{  
	[self createOnePicker:&RSSURLPickerView];
	[self createOnePicker:&movieURLPickerView];
	[self.view addSubview:RSSURLPickerView];
	[self.view addSubview:movieURLPickerView];
}

-(void)addPickerDismissButton
{
	CGRect buttonFrame = CGRectMake(	kLeftMargin,
									RSSURLPickerView.frame.origin.y - 32 /*kTextFieldHeight*/,
									50.0f,
									30.0f);
	
	self.button = [[UIButton buttonWithType:UIButtonTypeCustom] initWithFrame:buttonFrame];
	[self.button setBackgroundImage:[UIImage imageNamed:@"done.png"] forState:UIControlStateNormal];
	[self.button addTarget:self action:@selector(dismissCurrentPicker) forControlEvents:UIControlEventTouchUpInside];
	self.button.hidden = YES;
	[self.view addSubview:self.button];
}

-(void)dismissCurrentPicker
{
	currentPicker.hidden = YES;
	[self dismissModalViewControllerAnimated: YES];

}

- (void)showPicker:(UIView *)picker
{
	// hide the current picker and show the new one
	if (currentPicker)
	{
		currentPicker.hidden = YES;
		//label.text = @""; // there will be label associated as well, but not for now. tp.
	}
	picker.hidden = NO;
	
	currentPicker = picker;	// remember the current picker so we can remove it later when another one is chosen
}


#pragma mark UIPickerViewDelegate

// When picker value is chosen
- (void)pickerView:(UIPickerView *)pickerView didSelectRow:(NSInteger)row inComponent:(NSInteger)component
{
	if (pickerView == RSSURLPickerView)	
	{
		self.RSSURLSelected = [pickerArrayRSS objectAtIndex:[pickerView selectedRowInComponent:0]*2+1];
	}
	else if (pickerView == movieURLPickerView)
	{
		self.movieURLSelected = [pickerArrayMovieClipURL objectAtIndex:[pickerView selectedRowInComponent:0]*2+1];
	}
}


#pragma mark -
#pragma mark UIPickerViewDataSource

- (NSString *)pickerView:(UIPickerView *)pickerView titleForRow:(NSInteger)row forComponent:(NSInteger)component
{
	NSString *returnStr = @"";
	
	if (pickerView == RSSURLPickerView)
	{
		if (component == 0)
		{
			returnStr = [pickerArrayRSS objectAtIndex:row*2];
		}
		else
		{
			
			//returnStr = [[NSNumber numberWithInt:row] stringValue];
		}
	}
	else if (pickerView == movieURLPickerView)
	{
		if (component == 0) // we're only using single 'component'
			returnStr = [pickerArrayMovieClipURL objectAtIndex:row*2];
	}
	
	return returnStr;
}

- (CGFloat)pickerView:(UIPickerView *)pickerView widthForComponent:(NSInteger)component
{
	CGFloat componentWidth = 0.0;
	
	if (component == 0)
		componentWidth = 240.0;	// first column size is wider to hold names
	
	return componentWidth;
}

- (CGFloat)pickerView:(UIPickerView *)pickerView rowHeightForComponent:(NSInteger)component
{
	return 40.0;
}

- (NSInteger)pickerView:(UIPickerView *)pickerView numberOfRowsInComponent:(NSInteger)component
{
	if (pickerView == RSSURLPickerView)
		return ([pickerArrayRSS count]/2);
	return [pickerArrayMovieClipURL count]/2;
}

- (NSInteger)numberOfComponentsInPickerView:(UIPickerView *)pickerView
{
	return 1; // only use the titles for both pickers
}


@end
//
//-------------------------------------------------------------------------------------------------------
// 
#pragma mark -
@implementation OVPPreviewSetupController

@synthesize movieURLTextField, rssURLTextField, configMAURLTextField;
@synthesize dataSourceArray;

@synthesize pickerViewController;
@synthesize selectedIndexPath;
@synthesize prerollClipIndexPath;


#pragma mark TextField Delegates
- (BOOL)textFieldShouldReturn:(UITextField *)theTextField 
{
	// grab the NSUserDefaults db ref as we'll be updating them after text fields have been updated
	NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
	BOOL itemChanged = NO;
	// When the user presses return, take focus away from the text field so that the keyboard is dismissed.
	if (theTextField == self.movieURLTextField) 
	{
		[self.movieURLTextField resignFirstResponder];
		if (![[defaults stringForKey:kPrerollMovieClipURL] isEqualToString:self.movieURLTextField.text])
		{
			itemChanged = YES;
			[defaults setObject:self.movieURLTextField.text forKey:kPrerollMovieClipURL];
		}
	} 
	else if (theTextField == self.rssURLTextField)
	{
		[self.rssURLTextField resignFirstResponder];
		if (![[defaults stringForKey:kRSSURL] isEqualToString:self.rssURLTextField.text])
		{
			itemChanged = YES;		
			[defaults setObject:self.rssURLTextField.text forKey:kRSSURL];
#if 0 /*!defined( __IPHONE_OS_VERSION_MIN_REQUIRED) || __IPHONE_OS_VERSION_MIN_REQUIRED < 30200 */
			// For iPhone devices, initiate update to the table as soon as data has been entered.
			OVPPreviewAppDelegate *appDelegate = (OVPPreviewAppDelegate *)[[UIApplication sharedApplication] delegate];
			[appDelegate initiateUpdate]; 
#endif
		}
	}
	else if (theTextField == self.configMAURLTextField)
	{
		[self.configMAURLTextField resignFirstResponder];
		if (![[defaults stringForKey:kMediaAnalyticsURL] isEqualToString:self.configMAURLTextField.text])
		{
			itemChanged = YES;		
			[defaults setObject:self.configMAURLTextField.text forKey:kMediaAnalyticsURL];
		}
	}
		
	// inform any listeners of the settings changes...
	if (itemChanged)
	{
		// TODO: change this to async notification otherwise the UI element will block
		[[NSNotificationCenter defaultCenter] postNotificationName:SettingsChangedNotification object:nil];
	}

	return YES;
}

-(void)getUserSettings
{
	
	NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
	
	self.rssURLTextField.text = [defaults stringForKey:kRSSURL];
	self.movieURLTextField.text = [defaults stringForKey:kPrerollMovieClipURL];
	self.configMAURLTextField.text = [defaults stringForKey:kMediaAnalyticsURL];
 	
}
#pragma mark UIViewController methods


- (void)viewDidAppear:(BOOL)animated
{
	
}

- (void)viewWillAppear:(BOOL)animated
{
	if (self.pickerViewController)
	{
		NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
		if (self.pickerViewController.RSSURLSelected)
		{
			if (![self.pickerViewController.RSSURLSelected isEqualToString:self.rssURLTextField.text])
			{
				self.rssURLTextField.text = self.pickerViewController.RSSURLSelected;		
				[defaults setObject:self.rssURLTextField.text forKey:kRSSURL];
				OVPPreviewAppDelegate *appDelegate = (OVPPreviewAppDelegate *)[[UIApplication sharedApplication] delegate];
				[appDelegate initiateUpdate]; 
			}
			
			
		}
		if (self.pickerViewController.movieURLSelected)
		{
			if (![self.pickerViewController.movieURLSelected isEqualToString:self.movieURLTextField.text])
			{
				self.movieURLTextField.text = self.pickerViewController.movieURLSelected;
				[defaults setObject:self.movieURLTextField.text forKey:kPrerollMovieClipURL];
			}
			
		}
	}
}

// Implement viewDidLoad to do additional setup after loading the view, typically from a nib.
- (void)viewDidLoad 
{
	
    [super viewDidLoad];
	
	self.pickerViewController = [[PickerViewController alloc] init];
	
	[self getUserSettings];
	self.dataSourceArray = [NSArray arrayWithObjects:
							[NSDictionary dictionaryWithObjectsAndKeys:
							 @"Content/Feed URL", kSectionTitleKey,
							 @"Click here to make a selection...", kPickerKey,
							 self.rssURLTextField, kViewKey,
							 nil],
  
							[NSDictionary dictionaryWithObjectsAndKeys:
							 @"Pre-Roll Movie URL", kSectionTitleKey,
							 @"Click here to make a selection...", kPickerKey,
							 self.movieURLTextField, kViewKey,
							 nil],
														
							[NSDictionary dictionaryWithObjectsAndKeys:
							 @"Media Analytics Config URL", kSectionTitleKey,
							 @"", kPickerKey,
							 self.configMAURLTextField, kViewKey,
							 nil],
							
							nil];
	
#if 1 /* defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200 */
	self.selectedIndexPath = nil;
	self.prerollClipIndexPath = nil;
	if (rssURLTextField != nil)
	{
		CGRect frame = CGRectMake(-kLeftMargin, 8.0, kTextFieldWidth, kTextFieldHeight);
		rssURLTextField.frame = frame;
	}

	if (configMAURLTextField != nil)
	{
		CGRect frame = CGRectMake(-kLeftMargin, 8.0, kTextFieldWidth, kTextFieldHeight);
		configMAURLTextField.frame = frame;
	}
	if (movieURLTextField != nil)
	{
		CGRect frame = CGRectMake(-kLeftMargin, 8.0, kTextFieldWidth, kTextFieldHeight);
		movieURLTextField.frame = frame;
	}		
#endif	
} 

-(void)willRotateToInterfaceOrientation:(UIInterfaceOrientation)toInterfaceOrientation duration:(NSTimeInterval)duration
{
	OVPPreviewAppDelegate *appDelegate = (OVPPreviewAppDelegate *)[[UIApplication sharedApplication] delegate];
	[appDelegate.progressView UIOrientationChanged:toInterfaceOrientation];
}

// Override to allow orientations other than the default portrait orientation.
- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation 
{
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200
	return YES; 
#else
	return (interfaceOrientation == UIInterfaceOrientationPortrait);
#endif
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
	// e.g. self.myOutlet = nil;
	[super viewDidUnload];
	
	// release and set out IBOutlets to nil
	self.movieURLTextField = nil;
	self.rssURLTextField = nil;
	self.configMAURLTextField = nil;	
	
}


- (void)dealloc 
{
	[dataSourceArray release];
    [super dealloc];
}


#pragma mark UITableViewDataSource

- (NSInteger)numberOfSectionsInTableView:(UITableView *)tableView
{
	return [self.dataSourceArray count];

}

- (NSString *)tableView:(UITableView *)tableView titleForHeaderInSection:(NSInteger)section
{
	return [[self.dataSourceArray objectAtIndex: section] valueForKey:kSectionTitleKey];
}

- (NSInteger)tableView:(UITableView *)tableView numberOfRowsInSection:(NSInteger)section
{
#if 1 /* defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200 */
	// the iPad version will be slightly different, as the Picker interface will not be invokable, we'll 
	// 'flatten' the list by showing the picker items inside this table view
	// For that to happen, we need to change the responses of the delegate methods slightly. We will change
	// the returned number of rows based on the section. 
	switch (section) 
	{
		case 0:
			return [self.pickerViewController.pickerArrayRSS count]/2+1;
		case 1:
			return [self.pickerViewController.pickerArrayMovieClipURL count]/2+1;
		default:
			return 2;
	}
		
#endif
	// This is 'regular' iPhone implementation...
	return 2;
}

// to determine specific row height for each cell, override this.
// In this example, each row is determined by its subviews that are embedded.
//
- (CGFloat)tableView:(UITableView *)tableView heightForRowAtIndexPath:(NSIndexPath *)indexPath
{
	// first row is always input row, others are text rows
	return ([indexPath row] == 0) ? 52.0 : 36.0;
}

// We split the table view into sections with larger headings
// The following is a definition of sections in the table, i.e. url-section, prerol-clip or movie preroll section etc.
// It's just for the convenience/readablity instead of using indexes
enum UIActionSections
{
	kUIAction_URL_Section = 0,
	kUIAction_PreRollClipURL_Section,
	kUIAction_MAURL_Section
};
-(NSInteger)getFontSizeDependingOnPlatform
{
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED == 30200
	return 16;
#else
	return 12;
#endif
}
-(CGRect)getCellFrameSizeDependingOnPlatform
{
#if defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED == 30200
	// iPad only setup	
	return CGRectMake(kLeftMargin-8, 6, 360, 18);
#else
	return CGRectMake(20, 6, 280, 14);
#endif
}
// to determine which UITableViewCell to be used on a given row.
//
- (UITableViewCell *)tableView:(UITableView *)tableView cellForRowAtIndexPath:(NSIndexPath *)indexPath
{
	UITableViewCell *cell = nil;
#if 1 /* defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200 */
	// iPad specific impl.
	NSInteger row = [indexPath row];
	if (row == 0)
	{
		static NSString *kCellTextField_ID = @"TextField_cellID";
		cell = [tableView dequeueReusableCellWithIdentifier:kCellTextField_ID];
		if (cell == nil)
		{
			// a new cell needs to be created
			cell = [[[UITableViewCell alloc] initWithStyle:UITableViewCellStyleDefault reuseIdentifier:kCellTextField_ID] autorelease];
			cell.selectionStyle = UITableViewCellSelectionStyleNone;
		}
		else
		{
			// a cell is being recycled, remove the old edit field (if it contains one of our tagged edit fields)
			UIView *viewToCheck = nil;
			viewToCheck = [cell.contentView viewWithTag:kViewTag];
			if (!viewToCheck)
				[viewToCheck removeFromSuperview];
		}
		
		UITextField *textField = [[self.dataSourceArray objectAtIndex: indexPath.section] valueForKey:kViewKey];
		[cell.contentView addSubview:textField];
	}
	else 
	{	
		static NSUInteger const kTextLabelTag = 2;
		static NSString *kSourceCell_ID = @"URLElements_cellID";
		UILabel *cellLabel = nil;
		
		NSInteger section = [indexPath section];
		cell = [tableView dequeueReusableCellWithIdentifier:kSourceCell_ID];
		if (cell == nil)
		{
			cell = [[[UITableViewCell alloc] initWithStyle:UITableViewCellStyleDefault reuseIdentifier:kSourceCell_ID] autorelease];
			cell.selectionStyle =  UITableViewCellSelectionStyleGray;

			cellLabel = [[[UILabel alloc] initWithFrame:[self getCellFrameSizeDependingOnPlatform]] autorelease];
			cellLabel.tag = kTextLabelTag;
			cellLabel.font = [UIFont boldSystemFontOfSize:[self getFontSizeDependingOnPlatform]];
			cellLabel.textAlignment  = UITextAlignmentLeft;

			cellLabel.lineBreakMode = UILineBreakModeTailTruncation;
			[cell.contentView addSubview:cellLabel];
			
			cell.accessoryType = UITableViewCellAccessoryNone;
			
		}
		NSString *url = nil;
		switch (section) 
		{
			case kUIAction_URL_Section:
				cellLabel = (UILabel *)[cell.contentView viewWithTag:kTextLabelTag];
				if (cellLabel)
					cellLabel.text = [self.pickerViewController.pickerArrayRSS objectAtIndex:(row-1)*2];
				// pre-select the cell (mark it with a checkmark) which corresponds to the url in the settings
				url = [self.pickerViewController.pickerArrayRSS objectAtIndex:(indexPath.row-1)*2+1];
				if ([[[NSUserDefaults standardUserDefaults] stringForKey:kRSSURL] isEqualToString:url])
				{
					cell.accessoryType = UITableViewCellAccessoryCheckmark;
					self.selectedIndexPath = indexPath;
				}								
				break;
			case kUIAction_PreRollClipURL_Section:
				cellLabel = (UILabel *)[cell.contentView viewWithTag:kTextLabelTag];
				if (cellLabel)
					cellLabel.text = [self.pickerViewController.pickerArrayMovieClipURL objectAtIndex:(row-1)*2];	
				// pre-select the cell (mark it with a checkmark) which corresponds to value of the setting
				url = [self.pickerViewController.pickerArrayMovieClipURL objectAtIndex:(indexPath.row-1)*2+1];
				if ([[[NSUserDefaults standardUserDefaults] stringForKey:kPrerollMovieClipURL] isEqualToString:url])
				{
					cell.accessoryType = UITableViewCellAccessoryCheckmark;
					self.prerollClipIndexPath = indexPath;
				}								
				break;
			case kUIAction_MAURL_Section:
				
				break;
			default:
				break;
		}
	}
#else // iPhone implementation

	if ([indexPath row] == 0)
	{
		static NSString *kCellTextField_ID = @"TextField_cellID";
		cell = [tableView dequeueReusableCellWithIdentifier:kCellTextField_ID];
		if (cell == nil)
		{
			// a new cell needs to be created
			cell = [[[UITableViewCell alloc] initWithStyle:UITableViewCellStyleDefault reuseIdentifier:kCellTextField_ID] autorelease];
			cell.selectionStyle = UITableViewCellSelectionStyleNone;
		}
		else
		{
			// a cell is being recycled, remove the old edit field (if it contains one of our tagged edit fields)
			UIView *viewToCheck = nil;
			viewToCheck = [cell.contentView viewWithTag:kViewTag];
			if (!viewToCheck)
				[viewToCheck removeFromSuperview];
		}
		
		UITextField *textField = [[self.dataSourceArray objectAtIndex: indexPath.section] valueForKey:kViewKey];
		[cell.contentView addSubview:textField];
	}
	else /* (row == 1) */ /* This row is used as a selector for custom Picker */
	{
		static NSString *kSourceCell_ID = @"LaunchPicker_cellID";
		cell = [tableView dequeueReusableCellWithIdentifier:kSourceCell_ID];
		if (cell == nil)
		{
			cell = [[[UITableViewCell alloc] initWithStyle:UITableViewCellStyleDefault reuseIdentifier:kSourceCell_ID] autorelease];
			cell.selectionStyle = (indexPath.section==kUIAction_MAURL_Section ? UITableViewCellSelectionStyleNone:UITableViewCellSelectionStyleBlue); //allow input for 2 of 3 elements
			
            cell.textLabel.textAlignment = UITextAlignmentRight;
            cell.textLabel.textColor = [UIColor blueColor];
			cell.textLabel.highlightedTextColor = [UIColor redColor];
            cell.textLabel.font = [UIFont systemFontOfSize:[self getFontSizeDependingOnPlatform]];
			// display the right arrow next to the text
			//if (indexPath.section!=kUIAction_MAURL_Section)
			//	[cell.imageView initWithImage:[UIImage imageNamed:@"rarrow.png"]];
			
		}
		
		cell.textLabel.text = [[self.dataSourceArray objectAtIndex: indexPath.section] valueForKey:kPickerKey];
	}
#endif
    return cell;
}


// the table's selection has changed, show the alert or action sheet
- (void)tableView:(UITableView *)tableView didSelectRowAtIndexPath:(NSIndexPath *)indexPath
{
	// deselect the current row (don't keep the table selection persistent)
	[tableView deselectRowAtIndexPath:[tableView indexPathForSelectedRow] animated:YES];

#if 1 /*defined( __IPHONE_OS_VERSION_MIN_REQUIRED) && __IPHONE_OS_VERSION_MIN_REQUIRED >= 30200 */
	int section = [indexPath section];
	int row = [indexPath row];
	UITableViewCell *cell = [tableView cellForRowAtIndexPath:indexPath];
	if (row > 0 && cell)
	{
		switch (section)
		{
			case kUIAction_URL_Section:
			{
				UITableViewCell *selectedCell = nil;
				if (self.selectedIndexPath)
					selectedCell = [tableView cellForRowAtIndexPath:self.selectedIndexPath];
				if (selectedCell)
					selectedCell.accessoryType = UITableViewCellAccessoryNone;
				if (cell.accessoryType == UITableViewCellAccessoryNone)
				{
					cell.accessoryType = UITableViewCellAccessoryCheckmark;
					self.selectedIndexPath = indexPath;
					// set the RSS url field to this url and initiate update...
					
					self.rssURLTextField.text = [self.pickerViewController.pickerArrayRSS objectAtIndex:(indexPath.row-1)*2+1];
					[[NSUserDefaults standardUserDefaults] setObject:self.rssURLTextField.text forKey:kRSSURL];
				}
				
				
				break;
			}
				
			case kUIAction_PreRollClipURL_Section:
			{
				UITableViewCell *selectedCell = nil;
				if (self.prerollClipIndexPath)
					selectedCell = [tableView cellForRowAtIndexPath:self.prerollClipIndexPath];
				if (selectedCell)
					selectedCell.accessoryType = UITableViewCellAccessoryNone;
				if (cell.accessoryType == UITableViewCellAccessoryNone)
				{
					cell.accessoryType = UITableViewCellAccessoryCheckmark;
					self.prerollClipIndexPath = indexPath;
					// set the pre-roll url field to the selected url and initiate update...
					
					self.movieURLTextField.text = [self.pickerViewController.pickerArrayMovieClipURL objectAtIndex:(indexPath.row-1)*2+1];
					[[NSUserDefaults standardUserDefaults] setObject:self.movieURLTextField.text forKey:kPrerollMovieClipURL];
				}
				
				
				break;
			}
				
		}
	}
#else
	if (indexPath.row == 1)
	{
		switch (indexPath.section)
		{
			case kUIAction_URL_Section:
			{
				//[self presentModalViewController:self.pickerViewController animated: YES]; // another method
				[[self navigationController] pushViewController:self.pickerViewController animated:YES];
				[self.pickerViewController showPicker:self.pickerViewController.RSSURLPickerView];
				break;
			}
				
			case kUIAction_PreRollClipURL_Section:
			{
				
				[[self navigationController] pushViewController:self.pickerViewController animated:YES];
				[self.pickerViewController showPicker:self.pickerViewController.movieURLPickerView];
				break;
			}
								
		}
	}
#endif
}


@end
