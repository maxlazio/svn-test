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

/*
This application is a reference Flash CS4 application demonstrating integration between the AkamaiConnection and
AkamaiBOSSParser classes, in conjunction the event and RSS subclasses which together comprise the Flash Media Player framework.

The application loads a BOSS feed from StreamOS, parses it and plays the content.
The player handles both streaming and progressive links. 

The UI and functional elements of this player have been wrapped into this single .as file for ease of distribution. In a real-world application
Akamai recommends breaking at the various UI and functional items into reusable sub-classes and deploying a traditional MVC architecture. 
*/

package {
	// AS3 generic imports
	import flash.display.MovieClip;
	import flash.display.Loader;
	import flash.events.MouseEvent;
	import flash.events.Event;
	import flash.events.FullScreenEvent;
	import flash.events.NetStatusEvent;
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
	import org.openvideoplayer.events.*;
	import org.openvideoplayer.rss.*;

	// Akamai specific imports
	import com.akamai.net.AkamaiConnection;
	import com.akamai.net.AkamaiNetStream;
	import com.akamai.rss.*;
	

	public class CS4sampleBOSSplayer extends MovieClip {

		// Declare private variables
		private var _dragging:Boolean;
		private var _playlist:AkamaiMediaRSS;
		private var _bossMetafile:AkamaiBOSSParser;
		private var _existingConnections:Object;
		private var _nc:AkamaiConnection;
		private var _ns:AkamaiNetStream;
		private var _bandwidthMeasured:Boolean;
		private var _playlistIndex:Number;
		private var _hasEnded:Boolean;
		private var _videoSettings:Object;
		private var _streamLength:Number;
		
		// Declare private constants
		private const SAMPLES:Array = [
								{label:"Metafile version I",data:"http://products.edgeboss.net/flash/products/mediaframework/fms/0223_quikpro_highlights_700.flv?xmlvers=1"},
								{label:"Metafile version II",data:"http://products.edgeboss.net/flash/products/mediaframework/fms/akamai_10ya_nab_700k.flv?xmlvers=2"},
								{label:"Progressive sample 1",data:"http://products.edgeboss.net/download/products/mediaframework/fms/0223_quikpro_lgwaveoftheday_web_700.flv"},
								{label:"Progressive sample 2",data:"http://products.edgeboss.net/download/products/mediaframework/fms/0406_bells_35thyearparty_big.flv"}
								];

		// Constructor
		public function CS4sampleBOSSplayer():void {
			initChildren();
		}
		
		// Initialize the children on the stage
		private function initChildren():void {
			this.stage.addEventListener(FullScreenEvent.FULL_SCREEN, handleReturnFromFullScreen);
			//
			_bossMetafile = new AkamaiBOSSParser();
			_bossMetafile.addEventListener(OvpEvent.PARSED,bossParsedHandler);
			_bossMetafile.addEventListener(OvpEvent.LOADED,bossLoadHandler);
			_bossMetafile.addEventListener(OvpEvent.ERROR,errorHandler);
			//
			cbSamples.dataProvider = new DataProvider(SAMPLES);
			cbSamples.selectedIndex = 0;
			cbSamples.addEventListener(Event.CHANGE, handleSampleSelect);
			//
			bLoad.addEventListener(MouseEvent.CLICK,doLoad);
			bPlayPause.enabled = false;
			bPlayPause.addEventListener(MouseEvent.CLICK,doPlayPause);
			//
			videoSlider.enabled = false;
			videoSlider.addEventListener(SliderEvent.THUMB_PRESS,beginDrag);
			videoSlider.addEventListener(SliderEvent.THUMB_RELEASE,endDrag);
			volumeSlider.enabled = false;
			volumeSlider.addEventListener(SliderEvent.CHANGE,volumeHandler);
			//
			bFullscreen.addEventListener(MouseEvent.CLICK,switchToFullScreen);
			//
			bufferingDisplay.text = "Waiting ...";
			bossLink.text = SAMPLES[0].data;
			_existingConnections = new Object();
			_dragging = false;
			progressBar.minimum = 0;
			progressBar.maximum = 100;
			video.smoothing = true;
			//
			_nc = new AkamaiConnection();
			_nc.addEventListener(OvpEvent.ERROR,errorHandler);
			_nc.addEventListener(OvpEvent.BANDWIDTH,bandwidthHandler);
			_nc.addEventListener(NetStatusEvent.NET_STATUS,netStatusHandler);
			_nc.addEventListener(OvpEvent.STREAM_LENGTH,streamLengthHandler);			
		}
		
		// Handles the sample selection event
		private function handleSampleSelect(e:Event):void {
			bossLink.text = cbSamples.selectedItem.data;
		}
		
		// Handles the LOAD button click event
		private function doLoad(e:MouseEvent):void {
			bufferingDisplay.text = "Loading ...";
			_playlistIndex = 0;
			output.text = "";
			bPlayPause.enabled = false;
			bFullscreen.enabled = false;
			_hasEnded = false;
			
			// Clean up from previous session, if it exists
			if (_nc.netConnection is NetConnection) {
				_ns.useFastStartBuffer = false;
				_nc.close();
			}
			
			// Decide if the link will return a BOSS xml metafile, or if it is
			// a direct reference to a progressive FLV file. 
			if (bossLink.text.split("/").length > 3 && bossLink.text.split("/")[3].toUpperCase() == "DOWNLOAD") {
				_nc.connect(null);
			} else {
				_bossMetafile.load(bossLink.text);
			}

		}
		
		// Called when the BOSS feed has been successfully loaded
		private function bossLoadHandler(e:OvpEvent):void {
			write("BOSS loaded successfully");
		}
		
		// Handles the notification that the BOSS feed was successfully parsed
		private function bossParsedHandler(e:OvpEvent):void {
			write("BOSS parsed successfully:");
			write("========== Metafile data ==============");
			switch (_bossMetafile.versionOfMetafile) {
				case _bossMetafile.METAFILE_VERSION_I:
					write("  Server name: " + _bossMetafile.serverName);
					write("  Fallback server name: " + _bossMetafile.fallbackServerName);
					write("  App name: " + _bossMetafile.appName);
					write("  Stream name: " + _bossMetafile.streamName);
					write("  Is live: " + _bossMetafile.isLive);
					write("  Buffer time: " + _bossMetafile.bufferTime);
					write("  Connect Auth Params: " + _bossMetafile.connectAuthParams);
					write("  Play Auth Params: " + _bossMetafile.playAuthParams);
					_nc.requestedProtocol = "any";
				break;
				case _bossMetafile.METAFILE_VERSION_II:
					write("  Server name: " + _bossMetafile.serverName);
					write("  App name: " + _bossMetafile.appName);
					write("  Stream name: " + _bossMetafile.streamName);
					write("  Is live: " + _bossMetafile.isLive);
					write("  Buffer time: " + _bossMetafile.bufferTime);
					write("  Connect Auth Params: " + _bossMetafile.connectAuthParams);
					write("  Play Auth Params: " + _bossMetafile.playAuthParams);
					_nc.requestedProtocol = "any";
				break;
				case _bossMetafile.METAFILE_VERSION_IV:
					write("  Server name: " + _bossMetafile.serverName);
					write("  App name: " + _bossMetafile.appName);
					write("  Stream name: " + _bossMetafile.streamName);
					write("  Is live: " + _bossMetafile.isLive);
					write("  Title: " + _bossMetafile.title);
					write("  Source: " + _bossMetafile.source);
					write("  Author: " + _bossMetafile.author);
					write("  Clip begin: " + _bossMetafile.clipBegin);
					write("  Clip end: " + _bossMetafile.clipEnd);
					write("  Duration: " + _bossMetafile.duration);
					write("  Connect Auth Params: " + _bossMetafile.connectAuthParams);
					write("  Play Auth Params: " + _bossMetafile.playAuthParams);
					write("  Secondary Encoder Source: " + _bossMetafile.secondaryEncoderSrc);
					write("  Keywords: " + _bossMetafile.keywords);
					_nc.requestedProtocol = _bossMetafile.protocol.indexOf("rtmpe") != -1 ? "rtmpe,rtmpte":"any";
				break;
			}
			write("======= End of Metafile data ===========");
			
			// Establish the connection
			_nc.connectionAuth = _bossMetafile.connectAuthParams;
			_nc.connect(_bossMetafile.hostName); 
		}

		// Handle the start of a video scrub
		private function beginDrag(e:SliderEvent):void {
			_dragging = true;
		}
		
		// Handle the end of a video scrub
		private function endDrag(e:SliderEvent):void {
			write("calling seek to " + videoSlider.value);
   			if (_hasEnded) {
   				_hasEnded = false;
   				_ns.play(_nc.isProgressive ? bossLink.text: _bossMetafile.streamName);
   				bPlayPause.label = "PAUSE";
   			} 
   			_ns.seek(videoSlider.value);
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
						_ns.play(_nc.isProgressive ? bossLink.text: _bossMetafile.streamName);
					} else {
						_ns.resume();
					}
				break;
			}
   		}

		// Once a good connection is found, this handler will be called
		private function connectedHandler():void {
			_ns = new AkamaiNetStream(_nc);
			
			_ns.addEventListener(OvpEvent.COMPLETE,endHandler);
			_ns.addEventListener(OvpEvent.PROGRESS,update);
			_ns.addEventListener(NetStatusEvent.NET_STATUS,streamStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_PLAYSTATUS,streamPlayStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_METADATA,metadataHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_CUEPOINT,cuepointHandler);
			_ns.addEventListener(OvpEvent.MP3_ID3,id3Handler);
			_ns.addEventListener(OvpEvent.STREAM_LENGTH, streamLengthHandler);
			
			_ns.maxBufferLength = 5;
			_ns.useFastStartBuffer = !_bossMetafile.isLive;
			_ns.liveStreamAuthParams = _bossMetafile.playAuthParams;
				
			video.attachNetStream(_ns);
			
			// Branch based on the playback mode
			if (_nc.isProgressive) {
				write("Progressive connection established");
				progressBar.setProgress(0,100);
				progressBar.visible = true;
				_ns.play(bossLink.text);
				bPlayPause.label = "PAUSE";
			} else {
				progressBar.visible = false;
				videoSlider.visible = !_bossMetafile.isLive;
				write("Successfully connected to: " + _nc.netConnection.uri);
				write("Port: " + _nc.actualPort);
				write("Protocol: " + _nc.actualProtocol);
				write("Server IP address: " + _nc.serverIPaddress);
				// If an ondemand stream, start the asynchronous process of requesting the stream length 
				if (!_bossMetafile.isLive) {
					_nc.requestStreamLength(_bossMetafile.streamName);
				}
				// Use fastStart if you are sure the connectivity of your clients is at least
				// twice the bitrate of the video they will be viewing. Never use it with live streams. 
				_ns.useFastStartBuffer = !_bossMetafile.isLive;
				// start the asynchronous process of estimating bandwidth if it hasn;t already been esimated
				if (_bandwidthMeasured) {
					_ns.play(_bossMetafile.streamName);
					bPlayPause.label = "PAUSE";
				} else {
					write("Measuring bandwidth ... ");
					_nc.detectBandwidth();
				}
			}
		}
		
		// Handles the result of the bandwidth estimate
		private function bandwidthHandler(e:OvpEvent):void {
			write("Bandwidth measured at " + e.data.bandwidth+ " kbps and latency is " + e.data.latency + " ms.");
			_bandwidthMeasured = true;
			bPlayPause.label = "PAUSE";
			_ns.play(_bossMetafile.streamName);
		}
		
		// Handles a successful stream length request
		private function streamLengthHandler(e:OvpEvent):void {
			write("Stream length=" + e.data.streamLength);
			_streamLength = e.data.streamLength;
			videoSlider.maximum = e.data.streamLength;
			bPlayPause.enabled = true;
			videoSlider.enabled = true;
			volumeSlider.enabled = true;
			bFullscreen.enabled = true;
			_ns.volume = .8;
		}
		
		// Receives information that the end of a streaming stream has been reached. 
		private function endHandler(e:OvpEvent):void {
			write("End of stream detected");
			_hasEnded = true;
			bPlayPause.label = "PLAY";
		}
		
		// Receives all onPlayStatus events dispatched by the active NetStream
		private function streamPlayStatusHandler(e:OvpEvent):void {
			write(e.data.code);
		}
		
		// Updates the UI elements as the video plays
		private function update(e:OvpEvent):void {
			timeDisplay.text = _ns.timeAsTimeCode + " | " + _nc.streamLengthAsTimeCode(_streamLength);
			if (!_dragging) {
				videoSlider.value = _ns.time;
			}
			bufferingDisplay.visible = _ns.isBuffering;
			bufferingDisplay.text = "Buffering: " + _ns.bufferPercentage+"%";
			if (_nc.isProgressive) {
				progressBar.setProgress(_ns.bytesLoaded,_ns.bytesTotal);
			}
		}
		
		// Handles NetConnection status events. The description notifier is displayed
		// for rejection events.
		private function netStatusHandler(e:NetStatusEvent):void {
			trace(e.info.code);
			write(e.info.code);
			
			switch (e.info.code) {
				case "NetConnection.Connect.Rejected":
					trace("Rejected by server. Reason is "+e.info.description);
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
				case "NetStream.Buffer.Full" :
					_dragging = false;
					break;
			}
		}
		
		// Here comes our id3 info in response to a request to getMp3Id3Info(name)
		private function id3Handler(e:OvpEvent):void {
			for (var i:String in e.data) {
				write("ID3: " + i + " " + e.data[i]);
			}
		}
		
		// Handles metadata that is released by the stream
		private function metadataHandler(e:OvpEvent):void {
			write("Metadata received:");
			for (var propName:String in e.data) {
				write("metadata: "+propName+" = "+e.data[propName]);
			}
			// Adjust the video dimensions on the stage if they do not match the metadata
			if ((Number(e.data["width"]) != video.width)  || (Number(e.data["height"]) != video.height)) {
				scaleVideo(Number(e.data["width"]),Number(e.data["height"]));
			}
		}
		
		// Scales the video to fit into the 320x240 window while preserving aspect ratio.
		private function scaleVideo(w:Number,h:Number):void {
			if (w/h >= 4/3) {
				video.width = 320;
				video.height = 320*h/w;
			} else {
				video.width = 240*w/h;
				video.height = 240;
			}
			video.x = 30 + (320-video.width)/2;
			video.y = 104 + (240-video.height)/2;
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
		
		// Handles all error events for the connection and MediaRSS classes
		private function errorHandler(e:OvpEvent):void {
			write("Error #" + e.data.errorNumber + " " +e.data.errorDescription + " " + e.currentTarget);
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
	}
}
