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

// RandomSeek Flash CS4 AS3 Reference Player. This player demonstrates integration between the
// Akamai RandomSeek(tm) service for progressive Flash video delivery and the AkamaiEnhancedNetStream
// class. The UI is kept intentionally simple in order to illustrate how the class
// and service work together to deliver the instant-progressive seek capabilities of RandomSeek(tm).
// 
// Disclaimer - this code is provided for reference only. It is not supported by
// Akamai Customer Care and should be fully QA'd before being released into a production environment.
//
//

package {

	import flash.display.MovieClip;
	import flash.events.MouseEvent;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.media.Video;
	import flash.utils.getTimer;
	import flash.utils.Timer;
	import flash.net.NetConnection;
	import flash.events.NetStatusEvent;
	import flash.events.TimerEvent;
	import flash.events.IOErrorEvent;
	import flash.events.Event;
	import flash.events.AsyncErrorEvent;
	import fl.controls.Button;
	import fl.controls.Slider;
	import fl.controls.ProgressBar;
	import fl.events.SliderEvent;
	import fl.data.DataProvider;
	
	import com.akamai.net.AkamaiEnhancedNetStream;

	public class CS4sampleRandomSeek extends MovieClip {
		
		//Declare vars
		private var _ns:AkamaiEnhancedNetStream;
		private var _metadata:DataProvider;
		private var _timer:Timer;
		private var _dragging:Boolean;


		public function CS4sampleRandomSeek():void {
			cbList.addItem({label:"http://products.edgeboss.net/download/products/media_framework/flash/content/test/jp/tahiti_512K_CBR_2sec.flv"});
			cbList.addItem({label:"http://products.edgeboss.net/download/products/media_framework/flash/content/test/jp/nocc_320x180_548kbps.flv"});
			cbList.addItem({label:"http://products.edgeboss.net/download/products/mediaframework/fms/asp_final_700k.flv"});
			cbList.selectedIndex = 0;
			cbList.addEventListener(Event.CHANGE ,updateAddress);
			address.text = cbList.selectedItem.label;
			// Establish the NetConnection
			var nc:NetConnection = new NetConnection();
			nc.connect(null);
			// Establish the NetStream
			_ns = new AkamaiEnhancedNetStream(nc);
			_ns.addEventListener(NetStatusEvent.NET_STATUS,handleNetStatus);
			_ns.addEventListener(AsyncErrorEvent.ASYNC_ERROR,handleAsyncError);
			_ns.addEventListener(IOErrorEvent.IO_ERROR,handleIOError);
			// This is the only new event which the class introduces
			_ns.addEventListener("stateChange",handleStateChange);
			_ns.bufferTime = 1;
			_ns.client = this;
			video.attachNetStream(_ns);
			_dragging = false;
			bPlayPause.enabled = false;
			scrubber.enabled = false;
			_metadata = new DataProvider();
			// Set listeners on stage components
			scrubber.addEventListener(SliderEvent.THUMB_PRESS,startDragging);
			scrubber.addEventListener(SliderEvent.THUMB_RELEASE,doScrub);
			bLoad.addEventListener(MouseEvent.CLICK, doLoad);
			bPlayPause.addEventListener(MouseEvent.CLICK, doPlayPause);
			cbStopDownload.addEventListener(MouseEvent.CLICK,changePauseMode)
			// Init update timer
			_timer = new Timer(250);
			_timer.addEventListener(TimerEvent.TIMER,update);
		}
		// Handle state changes in the EnhancedNetStream
		private function handleStateChange(e:NetStatusEvent):void {
			statusDisplay.text = e.info.toString();
			switch (e.info) {
				case _ns.STATE_ENDED :
					bPlayPause.label = "PLAY";
					break;
				case _ns.STATE_PLAYING :
					_dragging = false;
					break;
				case _ns.STATE_PAUSED :
					_dragging = false;
					break;
				case _ns.STATE_BUFFERING :
					loadProgress.visible = true;
					break;
				case _ns.STATE_STOPPED :
					loadProgress.visible = false;
					break;
			}
		}
		// Updates UI as video plays
		private function update(e:TimerEvent):void {
			if (!isNaN(_ns.time) && !isNaN(_ns.duration)) {
				if (!_dragging) {
					scrubber.value = _ns.time;
					timeDisplay.text= _ns.timeAsTimeCode + "|"+_ns.durationAsTimeCode;
				} else {
					timeDisplay.text= _ns.convertToTimeCode(scrubber.value) + "|"+_ns.durationAsTimeCode;
				}
			}
			if (!isNaN(_ns.segmentLoadRatio)) {
				loadProgress.width = _ns.segmentLoadRatio*scrubber.width;
			}
		}
		// Initiates playback of a new file
		private function doLoad(e:MouseEvent):void {
			_ns.play(address.text);
			if (!_timer.running) {
				_timer.start();
			}
		}
		// Handle the start of drag for the drag handle
		private function startDragging(e:SliderEvent):void {
			_dragging = true;
		}
		// Handle IO Errors thrown by the AkamaiEnhancedNetStream class
		private function handleIOError(e:IOErrorEvent):void {
			statusDisplay.text = "Error: " + e.text;
		}
		// Handle AsyncError Errors thrown by the AkamaiEnhancedNetStream class
		private function handleAsyncError(e:AsyncErrorEvent):void {
			statusDisplay.text = "Error: " + e.text;
		}
		// Handle NetStatus events thrown by the AkamaiEnhancedNetStream class
		private function handleNetStatus(e:NetStatusEvent):void {
			switch (e.info.code) {
				case "NetStream.Play.StreamNotFound" :
					statusDisplay.text = "Stream not found";
					break;
				case "NetStream.Play.Start":
					bPlayPause.enabled  = true;
					scrubber.enabled = true;
					break;
			}
		}
		// Catch any cue points that may be embedded in the video
		public function onCuePoint(e:Object):void {
		}
		// Catch the metadata that is contained within the video
		public function onMetaData(... args):void {
			// Update datagrid with latest metadata
			var e:Object = args[0];
			_metadata.removeAll();
			for (var propName:String in e) {
				_metadata.addItem({name:propName,value:e[propName]});
			}
			metaDataGrid.dataProvider = _metadata;
			scrubber.maximum = _ns.duration;
			// Position the load progress bar;
			loadProgress.x = ((_ns.segmentStartTime/_ns.duration)*scrubber.width)+scrubber.x;
			loadProgress.width = 0;
			// Scale the video
			if (_ns.width/_ns.height >= 4/3) {
				video.width = 320;
				video.height = 320*_ns.height/_ns.width;
			} else {
				video.height = 240;
				video.width = 240*_ns.width/_ns.height;
			}
		}
		// Handle scrubbing
		private function doScrub(e:SliderEvent):void {
			if (_ns.state == _ns.STATE_ENDED) {
				bPlayPause.label = "PAUSE";
			}
			_ns.seek(e.target.value);
		}
		// Handle the play/pause button press
		private function doPlayPause(e:MouseEvent):void {
			if (bPlayPause.label == "PLAY") {
				bPlayPause.label = "PAUSE";
				if (_ns.state == _ns.STATE_ENDED) {
					_ns.play(cbList.selectedLabel);
				} else {
					_ns.resume();
				}
			} else {
				bPlayPause.label = "PLAY";
				_ns.pause();
			}
		}
		// Change the stop-on-pause behavior of the class
		private function changePauseMode(e:MouseEvent):void {
			_ns.stopOnPause = cbStopDownload.selected;
		}
		// Format the drag handle data tip
		private function dataTip(value:Number):String {
			return _ns.convertToTimeCode(value);
		}
		// Update the address bar
		private function updateAddress(e:Event):void {
			address.text = cbList.selectedItem.label;
		}
	}
}
