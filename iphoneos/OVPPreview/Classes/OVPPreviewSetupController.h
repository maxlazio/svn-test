//
//  OVPPreviewSetupController.h
//  OVPPreview
//
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// Originally created by Petrovic, Tommy (tpetrovi@akamai.com)
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.

#import <UIKit/UIKit.h>

@interface PickerViewController : UIViewController < UIPickerViewDelegate, UIPickerViewDataSource>
{
	UIPickerView *RSSURLPickerView;
	UIPickerView *movieURLPickerView;
	UIView		 *currentPicker;
	UIButton	 *button; // release picker button
	
	NSArray		 *pickerArrayRSS; // rss url array
	NSArray		 *pickerArrayMovieClipURL; // movie picker url array
	NSString	*RSSURLSelected;
	NSString	*movieURLSelected;
	
}
@property (nonatomic, retain) NSArray *pickerArrayRSS;
@property (nonatomic, retain) NSArray *pickerArrayMovieClipURL;
@property (nonatomic, retain) UIView *currentPicker;
@property (nonatomic, retain) UIButton *button;
@property (nonatomic, retain) UIPickerView *RSSURLPickerView;
@property (nonatomic, retain) UIPickerView *movieURLPickerView;
@property (nonatomic, retain) NSString *RSSURLSelected;
@property (nonatomic, retain) NSString *movieURLSelected;

-(void)loadView;
-(void)viewDidLoad;
-(void)dealloc;
-(void)createPickers;
-(void)addPickerDismissButton;
-(void)dismissCurrentPicker;
-(void)showPicker:(UIView *)picker;
@end

// Combo class for managing display/presentation of the application setup data
@interface OVPPreviewSetupController : UITableViewController  <UITextFieldDelegate> 
{
	UITextField	*movieURLTextField;
	UITextField	*rssURLTextField;
	UITextField	*configMAURLTextField;
	NSArray		*dataSourceArray;
	
	PickerViewController *pickerViewController;
	NSIndexPath *selectedIndexPath;
	NSIndexPath *prerollClipIndexPath;
}
@property (nonatomic, retain) IBOutlet UITextField *movieURLTextField;
@property (nonatomic, retain) IBOutlet UITextField *rssURLTextField;
@property (nonatomic, retain) IBOutlet UITextField *configMAURLTextField;
@property (nonatomic, retain) NSArray *dataSourceArray;

@property (nonatomic, retain) IBOutlet PickerViewController *pickerViewController;
@property (nonatomic, retain) NSIndexPath *selectedIndexPath;
@property (nonatomic, retain) NSIndexPath *prerollClipIndexPath;

-(void)viewDidAppear:(BOOL)animated;
-(void)viewWillAppear:(BOOL)animated;

-(void)getUserSettings;
@end
