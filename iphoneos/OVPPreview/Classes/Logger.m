//
//  Logger.m
//  OVPPreview
//
// This file is part of the OpenVideoPlayer project, http://openvideoplayer.com
// Copyright © 2008-2010, Akamai Technologies.  All rights reserved.
// OpenVideoPlayer is free software, you may use, modify, and/or redistribute under the terms of the license:
// http://openvideoplayer.com/license
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
// This is adapted from Erica Sandun's code...

#import "Logger.h"

// Basic logger to do special application events loggin
void doLog(id formatting,...)
{
	va_list arglist;
	if (formatting)
	{
		va_start(arglist, formatting);
		id outstring = [[NSString alloc] initWithFormat:formatting arguments:arglist];
		fprintf(stderr, "%s\n", [outstring UTF8String]);
		[outstring release];
		va_end (arglist);
	}
}
