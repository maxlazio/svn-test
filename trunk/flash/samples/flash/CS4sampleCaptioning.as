//
// Copyright (c) 2009-2010, the Open Video Player authors. All rights reserved.
//
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions are 
// met:
//
//    * Redistributions of source code must retain the above copyright 
//		notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above 
//		copyright notice, this list of conditions and the following 
//		disclaimer in the documentation and/or other materials provided 
//		with the distribution.
//    * Neither the name of the openvideoplayer.org nor the names of its 
//		contributors may be used to endorse or promote products derived 
//		from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR 
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

package {
	
	import flash.net.NetConnection;
	import flash.display.MovieClip;
	import flash.text.*;
	import flash.media.Video;
	import flash.events.*;
	import flash.utils.Timer;
	import fl.events.SliderEvent;
	import fl.data.DataProvider;
	import fl.controls.dataGridClasses.*;
	
	
	import org.openvideoplayer.net.*;
	import org.openvideoplayer.events.*;
	import org.openvideoplayer.cc.*;
	
	public class CS4sampleCaptioning extends MovieClip {
		private var _nc:OvpConnection;
		private var _ns:OvpNetStream;
		private var _scrubberDragging:Boolean;
		private var _playBtnStatePlaying:Boolean;
		private var _waitForSeek:Boolean;
		private var _streamLength:Number;
		private var _ccMgr:OvpCCManager;
		private var _timeDisplayTextFormat:TextFormat;
		private var _captionTextFormat:TextFormat;
		private var _captionTimer:Timer;
		private var _ccOn:Boolean;

		private const _DEFAULT_FONT_:String = "Verdana";
		private const _DEFAULT_FONT_SIZE_:uint = 11;
		private const _DEFAULT_FONT_COLOR_:Number = 0xffffff;
		
		private const _HOSTNAME_:String = "cp67126.edgefcs.net/ondemand";
		private const _STREAMNAME_:String = "mediapm/ovp/content/test/video/Akamai_10_Year_F8_512K";
		private const _CAPTION_URL_:String = "../data/caption/sample_caption.xml";		
		
		// Constructor
		public function CS4sampleCaptioning() {
			// Initialize private vars
			_scrubberDragging = false;
			_playBtnStatePlaying = false;
			_waitForSeek = false;
			_streamLength = 0;
			_ccOn = true;
			
			// Create text format objects for styling		
			_timeDisplayTextFormat = new TextFormat();
			_timeDisplayTextFormat.font = _DEFAULT_FONT_;
			_timeDisplayTextFormat.color = _DEFAULT_FONT_COLOR_;
			_timeDisplayTextFormat.size = _DEFAULT_FONT_SIZE_;
			
			_captionTextFormat = new TextFormat();
			_captionTextFormat.font = _DEFAULT_FONT_;
			_captionTextFormat.color = _DEFAULT_FONT_COLOR_;
			_captionTextFormat.size = 14;
			_captionTextFormat.align = TextFormatAlign.CENTER;
			_captionTextFormat.leftMargin = 5;
			_captionTextFormat.rightMargin = 5;
								
			// Set control styles
			_videoMC._timeDisplay.setStyle("textFormat", _timeDisplayTextFormat);
			_videoMC._ccBtn.setStyle("textFormat", _timeDisplayTextFormat);
			_captionMC._lblCaption.setStyle("textFormat", _captionTextFormat);
			_captionMC._lblCaption.wordWrap = true;
						
			// Hide the video movie clip initially
			_videoMC.visible = false;
			
			// Add UI control event listeners
			_videoMC._btnPlay.addEventListener("click", onClickBtnPlay);	// Play button
			_videoMC._ccBtn.addEventListener("click", onClickBtnCC);	// CC button
			
			_videoMC._scrubber.addEventListener("thumbPress", onScrubberThumbPress);
			_videoMC._scrubber.addEventListener("thumbRelease", onScrubberThumbRelease);
			
			// Initial control states
			_videoMC._ccBtn.setMouseState("down");
			
			// Create the connection object and add the necessary event listeners
			_nc = new OvpConnection()
			_nc.addEventListener(OvpEvent.ERROR, errorHandler);
			_nc.addEventListener(OvpEvent.STREAM_LENGTH, streamLengthHandler);
			_nc.addEventListener(NetStatusEvent.NET_STATUS, netStatusHandler);			
			
			_captionTimer = new Timer(10000);
			_captionTimer.addEventListener(TimerEvent.TIMER, onCaptionTimer);
			
			_nc.connect(_HOSTNAME_);
		}
		
		private function onCaptionTimer(e:TimerEvent):void {
			hideCaption();
		}
		
		private function showCaption(ccObj:Caption):void {
			_captionMC._lblCaption.htmlText = ccObj.text;

			// Formatting within the caption string			
			for (var i:uint = 0; i < ccObj.captionFormatCount(); i++) {
				var ccFormatObj:CaptionFormat = ccObj.getCaptionFormatAt(i);
				var txtFormat:TextFormat = new TextFormat();
				var styleObj:Style = ccFormatObj.styleObj;
				
				if (styleObj) {				
					if (styleObj.color != "") {
						txtFormat.color = styleObj.color;
					}
					if (styleObj.fontFamily != "") {
						txtFormat.font = styleObj.fontFamily;
					}
					if (styleObj.fontSize > 0) {
						txtFormat.size = styleObj.fontSize;
					}
					if (styleObj.fontStyle != "") {
						txtFormat.italic = (styleObj.fontStyle == "italic") ? true : false;
					}
					if (styleObj.fontWeight != "") {
						txtFormat.bold = (styleObj.fontWeight == "bold") ? true : false;
					}
					if (styleObj.textAlign != "") {
						txtFormat.align = styleObj.textAlign;
					}			

					_captionMC._lblCaption.textField.setTextFormat(txtFormat, ccFormatObj.startIndex, ccFormatObj.endIndex);
					
					if (_captionMC._lblCaption.wordWrap != styleObj.wrapOption) {
						_captionMC._lblCaption.wordWrap = styleObj.wrapOption;
					}
				}
			}
				
			_captionMC.visible = true;
			
			if (ccObj.endTime > 0) {
				_captionTimer.delay = (ccObj.endTime - ccObj.startTime)*1000;
				_captionTimer.start();
			}
			
		}
		
		private function hideCaption():void {
			_captionTimer.stop();
			_captionMC.visible = false;
		}
		
		private function showVideo(_show:Boolean=true):void {
			_videoMC.visible = _show;
		}
		
		// Starts the video playing when everything is ready
   		private function playVideo(name:String):void {
   			_playBtnStatePlaying = true;
			_videoMC._btnPlay.label = "pause";
			_ccMgr.enableCuePoints(_ccOn);
   			_ns.play(name);
   			showVideo();
   		}
		
		// This method is called from the netStatusHandler below when we receive a good connection		
		private function connectedHandler():void {
			trace("Successfully connected to: " + _nc.netConnection.uri);
			_videoMC._btnPlay.enabled = true;

			// Instantiate an OvpNetStream object
			_ns = new OvpNetStream(_nc);
			
			// Add the necessary listeners
			_ns.addEventListener(NetStatusEvent.NET_STATUS, streamStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_PLAYSTATUS, streamPlayStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_METADATA, metadataHandler);
			_ns.addEventListener(OvpEvent.PROGRESS, update);
			
			// Give the video object our net stream object
			_videoMC._video.attachNetStream(_ns);
			
			// Create the closed captioning manager and give it the net stream object
			_ccMgr = new OvpCCManager(_ns);
			_ccMgr.addEventListener(OvpEvent.ERROR, errorHandler);		
			_ccMgr.addEventListener(OvpEvent.CAPTION, captionHandler);
			_ccMgr.parse(_CAPTION_URL_);			

			trace("Port: " + _nc.actualPort);
			trace("Protocol: " + _nc.actualProtocol);
			trace("IP address: " + _nc.serverIPaddress);
			
			// Start the asynchronous process of requesting the stream length
			_nc.requestStreamLength(_STREAMNAME_);
		}		
		
		private function toggleDragging(_state:Boolean):void {
			_scrubberDragging = _state;
			if (!_state) {
				_waitForSeek = true;
				doSeek();
			}
		}
		
		private function doSeek() : void {
			hideCaption();
			_ns.seek(_videoMC._scrubber.value);
		}
		
		//-------------------------------------------------------------------
		//
		// Event Handlers
		//
		//-------------------------------------------------------------------

		private function onScrubberThumbPress(event:SliderEvent):void {
			toggleDragging(true);
		}
		
		private function onScrubberThumbRelease(event:SliderEvent):void {
			toggleDragging(false);
		}
		
		private function onClickBtnCC(event:MouseEvent):void {
			_ccOn = _videoMC._ccBtn.selected;
			_captionMC.visible = _ccOn;
			_ccMgr.enableCuePoints(_ccOn);
			if (!_ccOn) {
				_captionMC._lblCaption.htmlText = "";
			}		
		}
		
		private function onClickBtnPlay(event:MouseEvent):void {
			if (_playBtnStatePlaying) {
				_ns.pause();
				_videoMC._btnPlay.label = "play";
			}
			else {
				_ns.resume();
				_videoMC._btnPlay.label = "pause";
			}
			_playBtnStatePlaying = !_playBtnStatePlaying;
		}
			
		// Handles all OvpEvent.ERROR events
		private function errorHandler(e:OvpEvent):void {
			trace("Error #" + e.data.errorNumber+": " + e.data.errorDescription, "ERROR");
		}
		
		// Handles the stream length response after a call to requestStreamLength
		private function streamLengthHandler(e:OvpEvent):void {
			trace("Stream length is " + e.data.streamLength);
			_videoMC._scrubber.maximum = e.data.streamLength;
			_videoMC._scrubber.enabled = true;
			_streamLength = e.data.streamLength;
		
			playVideo(_STREAMNAME_);
		}
		
		// Handles NetStatusEvent.NET_STATUS events fired by the OvpConnection class
		private function netStatusHandler(e:NetStatusEvent):void {
			trace(e.info.code);
			switch (e.info.code) {
				case "NetConnection.Connect.Rejected":
					trace("Rejected by server. Reason is "+e.info.description);
					break;
				case "NetConnection.Connect.Success":
					connectedHandler();
					break;
			}
		}
		// Receives caption events dispatched by OvpCCManager class
		private function captionHandler(e:OvpEvent):void {
			if (e && e.data && (e.data is Caption)) {
				hideCaption();
				showCaption(Caption(e.data));
			}
		}
				
		// Handles the OvpEvent.PROGRESS event fired by the OvpNetStream class
   		private function update(e:OvpEvent):void {
   			_videoMC._timeDisplay.text =  _ns.timeAsTimeCode + "|"+ _nc.streamLengthAsTimeCode(_streamLength);
   			if (!_scrubberDragging && !_waitForSeek) {
   				_videoMC._scrubber.value = _ns.time;
   			}
   		}
		
		// Handles the NetStatusEvent.NET_STATUS events fired by the OvpNetStream class			
		private function streamStatusHandler(e:NetStatusEvent):void {
			trace("streamStatusHandler() - event.info.code="+e.info.code);
			switch(e.info.code) {
				case "NetStream.Buffer.Full":
					// _waitForSeek is used to stop the scrubber from updating
					// while the stream transtions after a seek
					_waitForSeek = false;
					break;
			}
		}
		// Handles the OvpEvent.NETSTREAM_PLAYSTATUS events fired by the OvpNetStream class
		private function streamPlayStatusHandler(e:OvpEvent):void {				
			trace("streamPlayStatusHandler() - e.data.code="+e.data.code);
			switch(e.data.code) {
				case "NetStream.Play.Complete":
					playVideo(_STREAMNAME_);
					break;
			}
		}
			
		// Handles the OvpEvent.NETSTREAM_METADATA events fired by the OvpNetStream class	
		private function metadataHandler(e:OvpEvent):void {
			for (var propName:String in e.data) {
				trace("metadata: "+propName+" = "+e.data[propName]);
			}
			_videoMC._video.x = 0;
			_videoMC._video.y = 0;
			_videoMC._video.width = e.data.width;
			_videoMC._video.height = e.data.height;

			_videoMC._videoBgMC.x = _videoMC._video.x - 4;
			_videoMC._videoBgMC.y = _videoMC._video.y - 4;
			_videoMC._videoBgMC.width = _videoMC._video.width + 8;
			_videoMC._videoBgMC.height = _videoMC._video.height + 8;
			

		}
		
	}
}

