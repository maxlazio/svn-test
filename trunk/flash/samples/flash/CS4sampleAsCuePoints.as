//
// Copyright (c) 2009, the Open Video Player authors. All rights reserved.
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
	import flash.text.TextFormat;
	import flash.media.Video;
	import flash.events.*;
	import fl.events.SliderEvent;
	import fl.data.DataProvider;
	import fl.controls.dataGridClasses.*;
	
	
	import org.openvideoplayer.net.*;
	import org.openvideoplayer.events.*;
	
	public class CS4sampleAsCuePoints extends MovieClip {
		private var _nc:OvpConnection;
		private var _ns:OvpNetStream;
		private var _scrubberDragging:Boolean;
		private var _playBtnStatePlaying:Boolean;
		private var _url:String;
		private var _waitForSeek:Boolean;
		private var _streamLength:Number;
		private var _filename:String;
		private var _cuePointMgr:OvpCuePointManager;
		private var _cuePoints:Array;
		private var _cuePointDisplayX:int = 0;
		private var _cuePointDisplayY:int = 0;
		private var _cuePointsReadOnlyArray:Array;
		private var pdlUrlList:Array = [
			"http://products.edgeboss.net/download/products/content/demo/video/oomt/elephants_dream_700k.flv",
			"http://products.edgeboss.net/download/products/content/demo/video/oomt/big_buck_bunny_700k.flv" ];
		private var hostNameList:Array = [ 
			"cp27886.edgefcs.net/ondemand" ];
		private var streamNameList:Array = [ 
			"14808/nocc_small307K" ];		
		private var _rbTextFormat:TextFormat;
		private var _timeDisplayTextFormat:TextFormat;
		private var _cuePointTextFormat:TextFormat;
		private var _currentState:uint;
		private var _currentCuePointIndex:int;
		private var _currentCuePointText:String;

		private const _STATE_PDL_:uint = 1;
		private const _STATE_STREAM_:uint = 2;
		private const _NUM_CUEPOINTS:uint = 600;
		private const _DEFAULT_FONT_:String = "Verdana";
		private const _DEFAULT_FONT_SIZE_:uint = 11;
		private const _DEFAULT_FONT_COLOR_:Number = 0xffffff;
		
		// Constructor
		public function CS4sampleAsCuePoints() {
			// Initialize private vars
			_scrubberDragging = false;
			_playBtnStatePlaying = false;
			_waitForSeek = false;
			_streamLength = 0;
			_currentState = 0;
			_currentCuePointIndex = 0;
			_currentCuePointText = "";
			
			// Create text format objects for styling
			_rbTextFormat = new TextFormat();
			_rbTextFormat.font = _DEFAULT_FONT_;
			_rbTextFormat.color = _DEFAULT_FONT_COLOR_;
			_rbTextFormat.size = _DEFAULT_FONT_SIZE_;
			
			_timeDisplayTextFormat = new TextFormat();
			_timeDisplayTextFormat.font = _DEFAULT_FONT_;
			_timeDisplayTextFormat.color = _DEFAULT_FONT_COLOR_;
			_timeDisplayTextFormat.size = _DEFAULT_FONT_SIZE_;
			
			_cuePointTextFormat = new TextFormat();
			_cuePointTextFormat.font = _DEFAULT_FONT_;
			_cuePointTextFormat.color = _DEFAULT_FONT_COLOR_;
			_cuePointTextFormat.size = _DEFAULT_FONT_SIZE_;
			_cuePointTextFormat.align = "right";
			
			// Init other UI controls
			_videoMC._progressBar.minimum = 0;
			_videoMC._progressBar.maximum = 100;

			// Set control styles
			_rbPDL.setStyle("textFormat", _rbTextFormat);
			_rbStreaming.setStyle("textFormat", _rbTextFormat);
			_videoMC._timeDisplay.setStyle("textFormat", _timeDisplayTextFormat);
			_cuePointMC._lblCuePoint.setStyle("textFormat", _timeDisplayTextFormat);
			
			// Set values in the combo boxes
			_pdlMC._cbPdlUrl.dataProvider = new DataProvider(pdlUrlList);
			_pdlMC._cbPdlUrl.selectedIndex = 0;
			_streamingMC._cbHostName.dataProvider = new DataProvider(hostNameList);
			_streamingMC._cbHostName.selectedIndex = 0;
			_streamingMC._cbStreamName.dataProvider = new DataProvider(streamNameList);
			_streamingMC._cbStreamName.selectedIndex = 0;
			
			// Hide combo boxes initially
			_pdlMC.visible = _streamingMC.visible = false;
			
			// Hide the video movie clip initially
			_videoMC.visible = false;
			
			// Add UI control event listeners
			_rbPDL.addEventListener("click", onClickRbPDL);			// Progressive download radio button
			_rbStreaming.addEventListener("click", onClickRbStreaming);	// Streaming radio button
			_pdlMC._btnLoadPDL.addEventListener("click", onClickBtnLoadPDL);	// Progressive Load button
			_streamingMC._btnLoadStream.addEventListener("click", onClickBtnLoadStreaming);
			_videoMC._btnPlay.addEventListener("click", onClickBtnPlay);	// Play button
			_videoMC._scrubber.addEventListener("thumbPress", onScrubberThumbPress);
			_videoMC._scrubber.addEventListener("thumbRelease", onScrubberThumbRelease);
			
			// Create the connection object and add the necessary event listeners
			_nc = new OvpConnection()
			_nc.addEventListener(OvpEvent.ERROR, errorHandler);
			_nc.addEventListener(OvpEvent.STREAM_LENGTH, streamLengthHandler);
			_nc.addEventListener(NetStatusEvent.NET_STATUS, netStatusHandler);
			
			// Create some ActionScript cue points
			_cuePoints = new Array();
			for (var i:uint = 0; i < _NUM_CUEPOINTS; i++) {
				var cuePoint:Object = new Object();
				cuePoint.name = "cue point number " + i;
				//cuePoint.time = 5 + (5 * i);	// every 5 seconds
				cuePoint.time = Math.round(Math.random() * 1000) + 1;	// random number 
				_cuePoints.push(cuePoint);
			}
			
			// Create the cue point manager object and add the ActionScript cue points
			_cuePointMgr = new OvpCuePointManager();
			_cuePointMgr.addEventListener(OvpEvent.ERROR, errorHandler);
			_cuePointMgr.addCuePoints(_cuePoints);
			
			// Get a "read-only" copy of the ActionScript cue points for displaying 
			_cuePointsReadOnlyArray = _cuePointMgr.cuePoints;
			
			// Show the cue points in the UI
			_videoMC._dataGrid.columns = ["time", "name"];
			for each(var cp:Object in _cuePointsReadOnlyArray) {
				//trace("----- Adding to grid, item.time="+cp.time);
				_videoMC._dataGrid.addItem(cp);
			}
			
			var timeCol:DataGridColumn = _videoMC._dataGrid.getColumnAt(0);
			timeCol.width = 40;
			timeCol.sortable = false;
			var nameCol:DataGridColumn = _videoMC._dataGrid.getColumnAt(1);
			nameCol.sortable = false;	
		}
		
		private function showCuePoint(_show:Boolean=true):void {
			_cuePointMC.visible = _show;
			_cuePointMC._lblCuePoint.text = _currentCuePointText; 
		}
		
		private function showVideo(_show:Boolean=true):void {
			_videoMC.visible = _show;
			_cuePointMC.visible = _show;
		}
		
		// Starts the video playing when everything is ready
   		private function playVideo(name:String):void {
   			_playBtnStatePlaying = true;
			_videoMC._btnPlay.label = "pause";
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
			_ns.addEventListener(OvpEvent.NETSTREAM_CUEPOINT, cuepointHandler);
			_ns.addEventListener(OvpEvent.PROGRESS, update);
			_ns.addEventListener(OvpEvent.STREAM_LENGTH, streamLengthHandler); 

			// Tell the OvpNetStream object we want it to generate NetStream.Pause.Notify 
			// for progressive download files because the Flash Player will not
			_ns.createProgressivePauseEvents = true;
			
			// Give the video object our net stream object
			_videoMC._video.attachNetStream(_ns);
			
			// Give the cue point manager the OvpNetStream object so it can start monitoring
			_cuePointMgr.netStream = _ns;
			
			if (_currentState == _STATE_PDL_) {
				// Progressive download
				_videoMC._progressBar.setProgress(0, 100);
				_videoMC._progressBar.visible = true;				
				playVideo(_filename);
			} else {
				// Streaming
				_videoMC._progressBar.visible = false;
				trace("Port: " + _nc.actualPort);
				trace("Protocol: " + _nc.actualProtocol);
				trace("IP address: " + _nc.serverIPaddress);
				// Start the asynchronous process of requesting the stream length
				_nc.requestStreamLength(_filename);
			}
		}		
		
		private function toggleDragging(_state:Boolean):void {
			_scrubberDragging = _state;
			if (!_state) {
				_waitForSeek = true;
				doSeek();
			}
		}
		
		private function doSeek() : void {
			showCuePoint(false);
			_currentCuePointIndex = 0;
			_videoMC._dataGrid.selectedIndex = _currentCuePointIndex;
			_videoMC._dataGrid.scrollToSelected();
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
		
		private function onClickRbPDL(event:MouseEvent):void {
			if (_currentState == _STATE_PDL_) {
				return;
			}
			
			if (_ns) {
				if (_playBtnStatePlaying) {
					onClickBtnPlay(null);
				}
				if (_nc.isProgressive) {
					showVideo(true);
				}
				else {
					showVideo(false);
				}
			}
			
			_pdlMC.visible = true;
			_streamingMC.visible = false;
			_currentState = _STATE_PDL_;
		}
		
		private function onClickRbStreaming(event:MouseEvent):void {
			if (_currentState == _STATE_STREAM_) {
				return;
			}
			
			if (_ns) {
				if (_playBtnStatePlaying) {
					onClickBtnPlay(null);
				}
				if (_nc.isProgressive) {
					showVideo(false);
				}
				else {
					showVideo(true);
				}
			}
			_streamingMC.visible = true;
			_pdlMC.visible = false;
			_currentState = _STATE_STREAM_;
		}
		
		private function onClickBtnLoadPDL(event:MouseEvent):void {
			showCuePoint(false);
			showVideo(false);
			_currentCuePointIndex = 0;
			
			_filename = _pdlMC._cbPdlUrl.text;
			
			if (_nc.netConnection is NetConnection) {
				_nc.close();
			}
			
			_nc.connect(null);
		}
		
		private function onClickBtnLoadStreaming(event:MouseEvent):void {
			showCuePoint(false);
			showVideo(false);
			_currentCuePointIndex = 0;
			
			_filename = _streamingMC._cbStreamName.text;
			var hostname:String = _streamingMC._cbHostName.text;
			
			if (_nc.netConnection is NetConnection) {
				_nc.close();
			}
			
			_nc.connect(hostname);
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
			
			if (_currentState == _STATE_STREAM_)
				playVideo(_filename);
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
		// Receives all cuepoint events dispatched by the active NetStream object
		// Note this is the same handler for both embedded and ActionScript cue points
		private function cuepointHandler(e:OvpEvent):void {
			if (e && e.data && e.data.name && e.data.time) {
				showCuePoint(false);
				_currentCuePointText = e.data.name + " at (" + e.data.time + ") secs";
				showCuePoint();

				for (var i:int = _currentCuePointIndex; i < _cuePointsReadOnlyArray.length; i++) {
					if (_cuePointsReadOnlyArray[i].time == e.data.time) {
						_currentCuePointIndex = i;
						_videoMC._dataGrid.selectedIndex = _currentCuePointIndex;
						_videoMC._dataGrid.scrollToSelected();
						break;
					}
				}				
			}
		}
				
		// Handles the OvpEvent.PROGRESS event fired by the OvpNetStream class
   		private function update(e:OvpEvent):void {
   			_videoMC._timeDisplay.text =  _ns.timeAsTimeCode + "|"+ _nc.streamLengthAsTimeCode(_streamLength);
   			if (!_scrubberDragging && !_waitForSeek) {
   				_videoMC._scrubber.value = _ns.time;
   			}
   			if (_currentState == _STATE_PDL_) {
   				_videoMC._progressBar.setProgress(_ns.bytesLoaded, _ns.bytesTotal);
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
			trace(e.data.code);
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
		}
		
	}
}
