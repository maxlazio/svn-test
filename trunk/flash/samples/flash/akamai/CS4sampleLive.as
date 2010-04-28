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

// AkamaiConnection class AS3 Reference Player - Live Streams
//
// This class demonstrates use of the AkamaiConnection class
// in connecting to the Akamai CDN and in rendering and controlling
// a live streaming video. 

package {

	import flash.display.MovieClip;
	import flash.events.*;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.media.Video;
	import flash.utils.getTimer;
	import fl.controls.Button;
	import fl.controls.Slider;
	import fl.events.SliderEvent;
	
	import org.openvideoplayer.net.*;
	import org.openvideoplayer.events.*;
	
	import com.akamai.net.*;

	public class CS4sampleLive extends MovieClip {

		// UI elements
		private var _video:Video;
		private var _bPlayPause:Button;
		private var _volumeSlider:Slider;
		private var _timeDisplay:TextField;
		private var _bufferingDisplay:TextField;
		private var _statusDisplay:TextField;

		// Variables
		private var _nc:AkamaiConnection;
		private var _ns:AkamaiNetStream;
		private var _timeRemaining:uint;
		
		// Constants - replace these with references to your own content
		// The auth params are only required if you are viewing a secure stream.
		// You may set them to empty string "" for non-secured content.
		
		private const HOSTNAME:String = "cp34973.live.edgefcs.net/live";
		private const FILENAME:String = "Flash_live_bm_500K@9319";
		
		private const STREAM_AUTH_PARAMS = "auth=yyyy&amp;aifp=zzzz";

		


		// Constructor
		public function CS4sampleLive():void {
			addChildren();
			initVars();
		}
		
		// Add the children to the stage
		private function addChildren():void {
			// Draw background
			this.graphics.beginFill(0x444444);
			this.graphics.drawRoundRect(10,10,340,320,10);
			this.graphics.endFill();
			// Add video
			_video = new Video(320,240);
			_video.x = 20;
			_video.y = 20;
			addChild(_video);
			// Add playPause button
			_bPlayPause = new Button();
			_bPlayPause.width = 60;
			_bPlayPause.label = "Pause";
			_bPlayPause.enabled = false;
			_bPlayPause.x = 20;
			_bPlayPause.y = 265;
			_bPlayPause.addEventListener(MouseEvent.CLICK,doPlayPause);
			addChild(_bPlayPause);

			// Add the volume slider
			_volumeSlider = new Slider();
			_volumeSlider.width = 80;
			_volumeSlider.x = 200;
			_volumeSlider.y = 270;
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
			_timeDisplay.x = 290;
			_timeDisplay.y = 265;
			_timeDisplay.width = 200;
			_timeDisplay.defaultTextFormat = format;
			addChild(_timeDisplay);
			// Add the buffering display
			_bufferingDisplay = new TextField();
			_bufferingDisplay.x = 85;
			_bufferingDisplay.y = 265;
			_bufferingDisplay.width = 200;
			_bufferingDisplay.defaultTextFormat = format;
			_bufferingDisplay.text = "Loading ...";
			addChild(_bufferingDisplay);
			// Add the buffering display
			_statusDisplay = new TextField();
			_statusDisplay.x = 20;
			_statusDisplay.y = 295;
			_statusDisplay.defaultTextFormat = format;
			_statusDisplay.width = 300;
			_statusDisplay.height= 80;
			_statusDisplay.multiline = true;
			_statusDisplay.wordWrap = true;
			_statusDisplay.text = "Connecting to server ...";
			addChild(_statusDisplay);
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
			_nc = new AkamaiConnection();
			_nc.addEventListener(OvpEvent.ERROR,onError);
			_nc.addEventListener(NetStatusEvent.NET_STATUS,netStatusHandler);
			_nc.connect(HOSTNAME);	
		}
		
		// Handles a successful connection
		private function connectedHandler():void {
			trace("Successfully connected using " + _nc.actualProtocol + " on port " + _nc.actualPort);
			_ns = new AkamaiNetStream(_nc);
			_ns.liveStreamAuthParams = STREAM_AUTH_PARAMS;
			_ns.addEventListener(OvpEvent.SUBSCRIBED, subscribedHandler);
			_ns.addEventListener(OvpEvent.UNSUBSCRIBED, unsubscribedHandler);
			_ns.addEventListener(OvpEvent.SUBSCRIBE_ATTEMPT, subscribeAttemptHandler);
			_ns.addEventListener(NetStatusEvent.NET_STATUS, netStatusHandler);
			_ns.addEventListener(OvpEvent.ERROR, onError);
			_ns.addEventListener(OvpEvent.PROGRESS,progressHandler);
			_video.attachNetStream(_ns);
			_statusDisplay.text = "Connected successfully";
			_ns.play(FILENAME);
			_volumeSlider.enabled = true;
			_ns.volume = .8;
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
				case "NetStream.Failed":
					_statusDisplay.text = "NetStream play failed";
					break;
				case "NetStream.Buffer.Full":
					_bPlayPause.enabled = true;
					break;
			}
		}

		// Updates the UI elements as the  video plays
		private function progressHandler(e:OvpEvent):void {
			_timeDisplay.text = _ns.timeAsTimeCode;
			_bufferingDisplay.visible = _ns.isBuffering;
			_bufferingDisplay.text = "Buffering: " + _ns.bufferPercentage+"%";
		}
		
		// Handles any errors dispatched by the connection class.
		private function onError(e:OvpEvent):void {
			_statusDisplay.text = "Error: " + e.data.errorDescription;
		}
		
		// Catches the subscription notification
		private function subscribedHandler(e:OvpEvent):void {
			_bPlayPause.enabled = true;
			_statusDisplay.text = "Subscribed";
		}
		
		// Catches the unsubscription notification
		private function unsubscribedHandler(e:OvpEvent):void {
			_statusDisplay.text = "Un-subscribed";
			_bPlayPause.enabled = false;
			_timeRemaining = getTimer();
		}
		
		// Catches the subscribe attempt notification
		private function subscribeAttemptHandler(e:OvpEvent):void {
			_statusDisplay.text = "Attempting to subscribe to the stream.\n" + (_ns.liveStreamMasterTimeout - Math.floor((getTimer() - _timeRemaining)/1000))+"s remaining until timeout.";
 		}
		
			
	}
}
