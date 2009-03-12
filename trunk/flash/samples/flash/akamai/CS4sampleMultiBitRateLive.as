//
// Copyright (c) 2008, the Open Video Player authors. All rights reserved.
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
	// AS3 generic imports
	import flash.display.MovieClip;
	import flash.display.Loader;
	import flash.events.*;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.media.Video;
	import flash.utils.Timer;
	import flash.net.URLRequest;
	import flash.net.NetConnection;
	import flash.geom.Rectangle;
	import flash.display.Stage;
	import flash.display.StageDisplayState;

	// CS4 specific imports
	import fl.controls.Button;
	import fl.controls.Slider;
	import fl.events.SliderEvent;
	import fl.data.DataProvider;
	import fl.managers.StyleManager;
	
	// OVP specific imports
	import org.openvideoplayer.net.*;
	import org.openvideoplayer.events.*;
	import org.openvideoplayer.rss.*;
	import org.openvideoplayer.parsers.*;
	
	// Akamai specific imports
	import com.akamai.net.*;
	
	
	// This sample plays live multi-bitrate streams from the Akamai CDN.
	// It has not yet been verified for operation with live streams protected by connection-level auth, and
	// should only be used to play back unprotected streams. 
	
	public class CS4sampleMultiBitRateLive extends MovieClip {

		// Declare private variables
		private var _dragging:Boolean;
		private var _smilMetafile:DynamicSmilParser;
		private var _nc:AkamaiConnection;
		private var _ns:AkamaiDynamicNetStream;
		private var _bandwidthMeasured:Boolean;
		private var _hasEnded:Boolean;
		private var _videoSettings:Object;
		private var _streamLength:Number;
		private var _chkboxTextFormat:TextFormat;
		private var _transitionMsgTextFormat:TextFormat;
		private var _transitionMsgTimer:Timer;
		private var _lastSwitch:int;
		
		
		// Declare private constants
		private const _DEFAULT_SMIL_FILE_:String = "Enter the path to a SMIL file referencing the target multi-bitrate streams";
		private const _DEFAULT_FONT_:String = "Verdana";
		private const _DEFAULT_FONT_SIZE_:uint = 11;
		private const _DEFAULT_FONT_COLOR_:Number = 0xffffff;
		private const _SWITCH_REQUEST_MSG_:String = "Requesting switch...";
		private const _SWITCH_UNDERWAY_MSG_:String = "Starting stream transition...";
		private const _SWITCH_COMPLETE_MSG_:String = "Stream transition complete.";
		private const _STREAM_TRANSITION_AT_HIGHEST_:String = "Already playing the highest quality stream.";
		private const _STREAM_TRANSITION_AT_LOWEST_:String = "Already playing the lowest quality stream.";		
		private const _TRANSITION_MSG_DISPLAY_TIME_:int = 2000;

		// Constructor
		public function CS4sampleMultiBitRateLive():void {
			// 
			_multiBRCtrls._btnSwitchUp.visible = false;
			_multiBRCtrls._btnSwitchDown.visible = false;
			_multiBRCtrls.visible = false;
			_multiBRCtrls._chkboxAuto.addEventListener("click", onClickAuto);
			_multiBRCtrls._btnSwitchUp.addEventListener("click", onClickSwitchUp);
			_multiBRCtrls._btnSwitchDown.addEventListener("click", onClickSwitchDown);
			_transitionMsgMC.visible = false;
			//
			this.stage.addEventListener(FullScreenEvent.FULL_SCREEN, handleReturnFromFullScreen);
			//
			_smilMetafile = new DynamicSmilParser();
			_smilMetafile.addEventListener(OvpEvent.PARSED,smilParsedHandler);
			_smilMetafile.addEventListener(OvpEvent.ERROR,errorHandler);
			//
			bLoad.addEventListener(MouseEvent.CLICK,doLoad);
			bPlayPause.enabled = false;
			bPlayPause.addEventListener(MouseEvent.CLICK,doPlayPause);
			//
			volumeSlider.enabled = false;
			volumeSlider.addEventListener(SliderEvent.CHANGE,volumeHandler);
			//
			bFullscreen.addEventListener(MouseEvent.CLICK,switchToFullScreen);
			//
			bufferingDisplay.text = "Waiting ...";
			smilLink.text = _DEFAULT_SMIL_FILE_;
			_dragging = false;
			video.smoothing = true;
			//
			_chkboxTextFormat = new TextFormat();
			_chkboxTextFormat.color = _DEFAULT_FONT_COLOR_;
			_multiBRCtrls._chkboxAuto.setStyle("textFormat", _chkboxTextFormat);
			
			_transitionMsgTextFormat = new TextFormat();
			_transitionMsgTextFormat.font = _DEFAULT_FONT_;
			_transitionMsgTextFormat.color = _DEFAULT_FONT_COLOR_;
			_transitionMsgTextFormat.size = _DEFAULT_FONT_SIZE_;
			_transitionMsgTextFormat.align = "right";

			_transitionMsgMC._lblTransitionMsg.setStyle("textFormat", _transitionMsgTextFormat);
			//
			_nc = new AkamaiConnection();
			_nc.addEventListener(OvpEvent.ERROR,errorHandler);
			_nc.addEventListener(NetStatusEvent.NET_STATUS,netStatusHandler);
			_nc.addEventListener(OvpEvent.STREAM_LENGTH,streamLengthHandler);
			// 
			_transitionMsgTimer = new Timer(_TRANSITION_MSG_DISPLAY_TIME_);
			_transitionMsgTimer.addEventListener(TimerEvent.TIMER, onTransitionMsgTimer);
			//
			_lastSwitch = 0;
		}
		
		// Handles the LOAD button click event
		private function doLoad(e:MouseEvent):void {
			bufferingDisplay.text = "Loading ...";
			output.text = "";
			bPlayPause.enabled = false;
			bFullscreen.enabled = false;
			_hasEnded = false;
			_multiBRCtrls.visible = false;

			// Clean up from previous session, if it exists
			if (_nc.netConnection is NetConnection) {
				_ns.useFastStartBuffer = false;
				_nc.close();
			}
			
			// Start parsing the SMIL file
			_smilMetafile.load(smilLink.text);
		}
				
		// Handles the notification that the BOSS feed was successfully parsed
		private function smilParsedHandler(e:OvpEvent):void {
			write("SMIL parsed successfully:");
			write("  Host name: " + _smilMetafile.hostName);
			write("  Stream name: " + _smilMetafile.streamName);
			
			// Establish the connection
			_nc.connect(_smilMetafile.hostName);
		}

		// Handle the start of a video scrub
		private function beginDrag(e:SliderEvent):void {
			_dragging = true;
		}
		
		// Update the volume
		private function volumeHandler(e:SliderEvent):void {
			_ns.volume = volumeSlider.value/100;
		}
		
   		// Handles play and pause
   		private function doPlayPause(e:MouseEvent):void {
			switch (bPlayPause.label){
				case "PAUSE":
					bPlayPause.label = "PLAY";
					_ns.pause();
				break;
				case "PLAY":
					bPlayPause.label = "PAUSE";
					if (_hasEnded) {
						_hasEnded = false;
						_ns.play(_smilMetafile.dsi);
					} else {
						_ns.resume();
					}
				break;
			}
   		}

		// Once a good connection is found, this handler will be called
		private function connectedHandler():void {
			_ns = new AkamaiDynamicNetStream(_nc);
			_ns.addEventListener(OvpEvent.ERROR, errorHandler);
			_ns.addEventListener(OvpEvent.DEBUG, debugMsgHandler);
			_ns.addEventListener(OvpEvent.COMPLETE, endHandler);
			_ns.addEventListener(OvpEvent.PROGRESS, update);
			_ns.addEventListener(NetStatusEvent.NET_STATUS, streamStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_PLAYSTATUS, streamPlayStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_METADATA, metadataHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_CUEPOINT, cuepointHandler);
			_ns.addEventListener(OvpEvent.STREAM_LENGTH, streamLengthHandler);
			_ns.addEventListener(OvpEvent.SUBSCRIBE_ATTEMPT, handleSubscribeAttempt);
			_ns.maxBufferLength = 10;
			_ns.isLive = true;
			_ns.useFastStartBuffer = false;
			
			video.attachNetStream(_ns);

			write("Successfully connected to: " + _nc.netConnection.uri);
			write("Port: " + _nc.actualPort);
			write("Protocol: " + _nc.actualProtocol);
			write("Server IP address: " + _nc.serverIPaddress);
			bPlayPause.enabled = true;
			volumeSlider.enabled = true;
			bFullscreen.enabled = true;
			_ns.volume = .8;
			_ns.play(_smilMetafile.dsi);
		}
				
		// Handles a successful stream length request
		private function streamLengthHandler(e:OvpEvent):void {
			write("Stream length=" + e.data.streamLength);
			_streamLength = e.data.streamLength;
			bPlayPause.enabled = true;
			volumeSlider.enabled = true;
			bFullscreen.enabled = true;
			_ns.volume = .8;
			_ns.play(_smilMetafile.dsi);
		}
		
		// Handle a resubscribe attempt
		private function handleSubscribeAttempt(e:OvpEvent):void {
			write("Trying to re-subscribe to the live stream ...");
		}
		
		// Receives information that the end of a streaming stream has been reached. 
		private function endHandler(e:OvpEvent):void {
			write("End of stream detected (streaming)");
			_hasEnded = true;
			bPlayPause.label = "PLAY";
		}
		
		// Receives all debug events dispatched from the OvpDynamicNetStream object
		private function debugMsgHandler(e:OvpEvent):void {
			write(String(e.data));
		}
		
		// Receives all onPlayStatus events dispatched by the active NetStream
		private function streamPlayStatusHandler(e:OvpEvent):void {
			write(e.data.code);
			if (e.data.code == "NetStream.Play.TransitionComplete") {
				_multiBRCtrls.visible = true;
				showTransitionMsg(_SWITCH_COMPLETE_MSG_, _TRANSITION_MSG_DISPLAY_TIME_); 
			}
		}
		
		// Updates the UI elements as the video plays
		private function update(e:OvpEvent):void {
			timeDisplay.text = _ns.timeAsTimeCode + " | LIVE";
			bufferingDisplay.visible = _ns.isBuffering;
			bufferingDisplay.text = "Buffering: " + _ns.bufferPercentage+"%";
		}
		
		// Handles NetConnection status events. The description notifier is displayed
		// for rejection events.
		private function netStatusHandler(e:NetStatusEvent):void {
			write(e.info.code);
			
			switch (e.info.code) {
				case "NetConnection.Connect.Rejected":
					write("Rejected by server. Reason is "+e.info.description);
					break;
				case "NetConnection.Connect.Success":
					connectedHandler();
					break;
			}
		}
		
		// Receives all status events dispatched by the active NetStream
		private function streamStatusHandler(e:NetStatusEvent):void {
			write(e.info.code);
			switch (e.info.code) {
				case "NetStream.Buffer.Full":
					_dragging = false;
					break;
				case "NetStream.Play.Transition":
					showTransitionMsg(_SWITCH_UNDERWAY_MSG_);
					break;
			}
		}
		
		// Handles metadata that is released by the stream
		private function metadataHandler(e:OvpEvent):void {
			write("Metadata received: "+e.data["width"]+"x"+e.data["height"]);
			// Adjust the video dimensions on the stage if they do not match the metadata
			if ((Number(e.data["width"]) != video.width)  || (Number(e.data["height"]) != video.height)) {
				scaleVideo(Number(e.data["width"]),Number(e.data["height"]));
			}
		}
		
		// Scales the video to fit into the 480x360 window while preserving aspect ratio.
		private function scaleVideo(w:Number, h:Number):void {
			if (w/h >= 4/3) {
				video.width = 480;
				video.height = 480*h/w;
			} else {
				video.width = 360*w/h;
				video.height = 360;
			}
			video.visible = true;
		}
			
		// Receives all cuepoint events dispatched by the NetStream
		private function cuepointHandler(e:OvpEvent):void {
			write("Cuepoint received:");
			for (var propName:String in e.data) {
				if (propName != "parameters") {
					write(propName+" = "+e.data[propName]);
				} else {
					write("parameters =");
					if (e.data.parameters != undefined) {
						for (var paramName:String in e.data.parameters) {
							write(" "+paramName+": "+e.data.parameters[paramName]);
						}
					} else {
						write("undefined");
					}
				}
			}
		}
		
		// Handles all error events for the connection, stream, and MediaRSS classes
		private function errorHandler(e:OvpEvent):void {
			switch (e.data.errorNumber) {
				case OvpError.INVALID_INDEX:
					handleInvalidIndexError();
					break;
				default:
					write("Error #" + e.data.errorNumber + " " +e.data.errorDescription + " " + e.currentTarget);
					break;
			}
		}
		
		// Writes trace statements to the output display
		private function write(msg:String):void {
			output.text += msg + "\n";
			output.verticalScrollPosition = output.maxVerticalScrollPosition+1;
		}
		
		// Switches to full screen mode
		private function switchToFullScreen(e:MouseEvent):void {
			// when going out of full screen mode 
			// we use these values
			_videoSettings = new Object();
			_videoSettings.savedWidth = video.width;
			_videoSettings.savedHeight = video.height;
			_videoSettings.x = video.x;
			_videoSettings.y = video.y;
			// Set the size of the video object to the 
			// original size of the video stream); 
			video.width =  video.videoWidth;
			video.height =  video.videoHeight;
			video.smoothing = false;
			video.x = 1000;
			video.y = 0;
			this.stage["fullScreenSourceRect"] = new Rectangle(1000,0,video.videoWidth ,video.videoHeight);	 
			this.stage["displayState"] = StageDisplayState.FULL_SCREEN;				
		}
		
		// Handles the return from fullscreen
		private function handleReturnFromFullScreen(e:FullScreenEvent):void {
			if (!e.fullScreen) {
				video.x = _videoSettings.x;
				video.y = _videoSettings.y;
				video.width = _videoSettings.savedWidth;
				video.height = _videoSettings.savedHeight;
				video.smoothing = true;
			}
		}
		
		private function onClickAuto(event:MouseEvent):void {
			if (_ns) {
				_ns.useManualSwitchMode(!event.target.selected);
			}

			_multiBRCtrls._btnSwitchUp.visible = _multiBRCtrls._btnSwitchDown.visible = !event.target.selected;
		}
		
		private function onClickSwitchUp(event:MouseEvent):void {
			showTransitionMsg(_SWITCH_REQUEST_MSG_);
			_lastSwitch = 1;
			_ns.switchUp();
		}
		
		private function onClickSwitchDown(event:MouseEvent):void {
			showTransitionMsg(_SWITCH_REQUEST_MSG_);
			_lastSwitch = -1;
			_ns.switchDown();
		}
		
		private function handleInvalidIndexError():void {
			if (_lastSwitch > 0) {
				showTransitionMsg(_STREAM_TRANSITION_AT_HIGHEST_, _TRANSITION_MSG_DISPLAY_TIME_);
			}
			else if (_lastSwitch < 0) {
				showTransitionMsg(_STREAM_TRANSITION_AT_LOWEST_, _TRANSITION_MSG_DISPLAY_TIME_);
			}
		}
		
		private function showTransitionMsg(msg:String, _time:Number=0):void {
			_transitionMsgMC._lblTransitionMsg.text = msg;
			_transitionMsgMC.visible = true;
			if (_time > 0) {
				_transitionMsgTimer.delay = _time;
				_transitionMsgTimer.start();
			}
			enableSwitchButtons(false);
		}
		
		private function hideTransitionMsg():void {
			_transitionMsgMC._lblTransitionMsg.text = "";
			_transitionMsgTimer.stop();
			_transitionMsgMC.visible = false;
			enableSwitchButtons();
		}
		
		private function onTransitionMsgTimer(event:TimerEvent):void {
			hideTransitionMsg();
		}
		
		private function enableSwitchButtons(_enable:Boolean=true):void {
			_multiBRCtrls._btnSwitchUp.enabled = _multiBRCtrls._btnSwitchDown.enabled = _enable;
		}
	}
}