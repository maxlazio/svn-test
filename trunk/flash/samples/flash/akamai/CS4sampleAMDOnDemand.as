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


// AkamaiConnection class AS3 Reference Player
//
// This class demonstrates use of the AkamaiConnection class
// in connecting to the Akamai CDN and in rendering and controlling
// a streaming video. 

package {

	import flash.display.MovieClip;
	import flash.events.MouseEvent;
	import flash.events.NetStatusEvent;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.media.Video;
	import fl.controls.Button;
	import fl.controls.Slider;
	import fl.events.SliderEvent;
	
	import org.openvideoplayer.events.*;
	import org.openvideoplayer.net.*;
	
	import com.akamai.net.*

	public class CS4sampleAMDOnDemand extends MovieClip {

		// UI elements
		private var _video:Video;
		private var _bPlayPause:Button;
		private var _videoSlider:Slider;
		private var _volumeSlider:Slider;
		private var _timeDisplay:TextField;
		private var _bufferingDisplay:TextField;

		// Variables
		private var _nc:AkamaiConnection;
		private var _ns:OvpNetStream;
		private var _dragging:Boolean;
		private var _streamLength:Number;
		
		
		// Constants - replace these with references to your own content
		private const HOSTNAME:String = "cp27886.edgefcs.net/ondemand"; 
		private const FILENAME:String = "14808/nocc_small307K";
				
		// Constructor
		public function CS4sampleAMDOnDemand():void {
			addChildren();
			initVars();
		}
		
		// Add the children to the stage
		private function addChildren():void {
			// Draw background
			this.graphics.beginFill(0x444444);
			this.graphics.drawRoundRect(10,10,338,250,10);
			this.graphics.endFill();
			// Add video
			_video = new Video(318,180);
			_video.x = 20;
			_video.y = 20;
			addChild(_video);
			// Add playPause button
			_bPlayPause = new Button();
			_bPlayPause.width = 60;
			_bPlayPause.label = "Pause";
			_bPlayPause.enabled = false;
			_bPlayPause.x = 20;
			_bPlayPause.y = 230;
			_bPlayPause.addEventListener(MouseEvent.CLICK,doPlayPause);
			addChild(_bPlayPause);
			// Add the video slider
			_videoSlider = new Slider();
			_videoSlider.width = 220;
			_videoSlider.x = 20;
			_videoSlider.y = 210;
			_videoSlider.minimum = 0;
			_videoSlider.enabled = false;
			_videoSlider.addEventListener(SliderEvent.THUMB_PRESS,beginDrag);
			_videoSlider.addEventListener(SliderEvent.THUMB_RELEASE,endDrag);
			addChild(_videoSlider);
			// Add the volume slider
			_volumeSlider = new Slider();
			_volumeSlider.width = 80;
			_volumeSlider.x = 250;
			_volumeSlider.y = 210;
			_volumeSlider.minimum = 0;
			_volumeSlider.maximum = 100;
			_volumeSlider.value = 80;
			_volumeSlider.enabled = false;
			_volumeSlider.addEventListener(SliderEvent.CHANGE,volumeHandler);
			addChild(_volumeSlider);
			// Add the time display
			var format:TextFormat = new TextFormat();
			format.color = 0xFFFFFF;
			format.font = "Verdana";
			format.bold = true;
			_timeDisplay = new TextField();
			_timeDisplay.x = 240;
			_timeDisplay.y = 230;
			_timeDisplay.width = 200;
			_timeDisplay.defaultTextFormat = format;
			addChild(_timeDisplay);
			// Add the buffering display
			_bufferingDisplay = new TextField();
			_bufferingDisplay.x = 100;
			_bufferingDisplay.y = 230;
			_bufferingDisplay.width = 200;
			_bufferingDisplay.defaultTextFormat = format;
			_bufferingDisplay.text = "Loading ...";
			addChild(_bufferingDisplay);
		}
		
		// Handle the start of a video scrub
		private function beginDrag(e:SliderEvent):void {
			_dragging = true;
		}
		
		// handle the end of a video scrub
		private function endDrag(e:SliderEvent):void {
			_ns.seek(_videoSlider.value);
		}
		
		// Update the volume
		private function volumeHandler(e:SliderEvent):void {
			_ns.volume = _volumeSlider.value/100;
		}
		
		// Handles the playPause button press
		private function doPlayPause(e:MouseEvent):void {
			switch (_bPlayPause.label) {
				case "Pause" :
				_ns.pause();
				_bPlayPause.label = "Play";
					break;
				case "Play" :
				_ns.resume();
				_bPlayPause.label = "Pause";
					break;
			}
		}
		
		// Initializes variables with starting values
		private function initVars():void {
			_dragging = false;
			_nc = new AkamaiConnection();
			_nc.addEventListener(OvpEvent.BANDWIDTH,bandwidthHandler);
			_nc.addEventListener(OvpEvent.STREAM_LENGTH,streamlengthHandler);
			_nc.addEventListener(NetStatusEvent.NET_STATUS,netStatusHandler);
			_nc.addEventListener(OvpEvent.ERROR,onError);
			_nc.connect(HOSTNAME);			
		}
		
		// Handles a successful connection
		private function connectedHandler():void {
			trace("Successfully connected using " + _nc.actualProtocol + " on port " + _nc.actualPort);
			// As soon as we are connected, we'll measure the available bandwidth
			_nc.detectBandwidth();
			
			_ns = new OvpNetStream(_nc.netConnection);
			
			_ns.addEventListener(OvpEvent.PROGRESS,progressHandler);
			_ns.addEventListener(OvpEvent.COMPLETE,completeHandler);
			_ns.addEventListener(NetStatusEvent.NET_STATUS,netStreamStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_METADATA,metadataHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_CUEPOINT,cuepointHandler);
			_ns.addEventListener(OvpEvent.ASYNC_ERROR, asyncErrorHandler);
			_ns.addEventListener(OvpEvent.ONFI, onFIHandler);
		}
		
		// Handles a successful bandwidth measurement
		private function bandwidthHandler(e:OvpEvent):void {
			trace("The bandwidth is " + e.data.bandwidth + "kbps and the latency is " + e.data.latency + " ms.");
			// At this point, you could use the bandwidth value to decide which file to play.
			_nc.requestStreamLength(FILENAME);
		}
		// Handles a successful stream length request
		private function streamlengthHandler(e:OvpEvent):void {
			_videoSlider.maximum = e.data.streamLength;
			_streamLength = e.data.streamLength;
			_video.attachNetStream(_ns);
			_ns.play(FILENAME);
			_bPlayPause.enabled = true;
			_videoSlider.enabled = true;
			_volumeSlider.enabled = true;
			_ns.volume = .8;
		}
		// Updates the UI elements as the  video plays
		private function progressHandler(e:OvpEvent):void {
			_timeDisplay.text = _ns.timeAsTimeCode + " | " + _nc.streamLengthAsTimeCode(_streamLength);
			if (!_dragging) {
			_videoSlider.value = _ns.time;
			}
			_bufferingDisplay.visible = _ns.isBuffering;
			_bufferingDisplay.text = "Buffering: " + _ns.bufferPercentage+"%";
		}
		
		// Handles netstream status events. We trap the buffer full event
		// in order to start updating our slider again, to prevent the
		// slider bouncing after a drag.
		private function netStreamStatusHandler(e:NetStatusEvent):void {
			switch (e.info.code) {
				case "NetStream.Buffer.Full":
					_dragging = false;
				break;
			}
		}
		
		// Handles NetConnection status events. The description notifier is displayed
		// for rejection events.
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
		// Catches the end of the stream. Note that the OvpEvent.COMPLETE
		// event should only be used to detect the end of streaming files. For progressive delivery,
		// use the OvpEvent.END_OF_STREAM event. 
		private function completeHandler(e:OvpEvent):void {
			_ns.pause();
			_ns.seek(0);
			_bPlayPause.label = "Play";
		}
		// Handles metadata that is released by the stream
		private function metadataHandler(e:OvpEvent):void {
			trace("Metadata received:");
				for (var propName:String in e.data) {
					trace("metadata: "+propName+" = "+e.data[propName]);
				}
		}
		// Receives all cuepoint events dispatched by the NetStream
		private function cuepointHandler(e:OvpEvent):void {
			trace("Cuepoint received:");
				for (var propName:String in e.data) {
						if (propName != "parameters") {
							trace(propName+" = "+e.data[propName]);
						} else {
							trace("parameters =");
							if (e.data.parameters != undefined) {
								for (var paramName:String in e.data.parameters) {
									trace(" "+paramName+": "+e.data.parameters[paramName]);
								}
							} else {
								trace("undefined");
							}
						}
				}
		}
		
		// Handles any errors dispatched by the connection class.
		private function onError(e:OvpEvent):void {
			trace("Error: " + e.data.errorDescription);
		}
		
		// Handles async errors dispatched by the net stream class
		private function asyncErrorHandler(e:OvpEvent):void {
			trace("Async Error: " + e.data);
		}
		
		// Handles the onFI event which can be added by the Flash Media Live Encoder and 
		// bubbled up by the OVP code base
		private function onFIHandler(e:OvpEvent):void {
			//trace("onFI event: timecode (hh:mm:ss:ff) = " + ((e.data.tc) ? e.data.tc : "null") + 
			//	", system date (dd-mm-yy) : " + ((e.data.sd) ? e.data.sd : "null") +
			//	", system time (hh:mm:ss.ms) : " + ((e.data.st) ? e.data.st : "null"));
		}
	}
}
