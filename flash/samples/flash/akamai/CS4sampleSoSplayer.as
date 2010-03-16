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


// Note: this reference player will use a FlashVar named "url" that supplies
// the path to the Stream OS feed. See the initChildren method below.
package {
	// AS3 generic imports
	import flash.display.MovieClip;
	import flash.display.Loader;
	import flash.events.*;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.text.TextFieldAutoSize;
	import flash.text.AntiAliasType;
	import flash.media.Video;
	import flash.utils.Timer;
	import flash.net.URLRequest;
	import flash.net.NetConnection;
	import flash.geom.Rectangle;
	import flash.display.Stage;
	import flash.display.StageDisplayState;
	import flash.display.StageAlign;
    import flash.display.StageScaleMode;
	import flash.display.Shape;

	// CS4 specific imports
	import fl.controls.Button;
	import fl.controls.Slider;
	import fl.events.SliderEvent;
	import fl.data.DataProvider;
	import fl.managers.StyleManager;
	import fl.controls.ProgressBar;
	import fl.controls.ProgressBarMode;
	import fl.controls.ComboBox;
	
	// Ovp specific imports
	import org.openvideoplayer.net.*;
	import org.openvideoplayer.events.*;
	import org.openvideoplayer.rss.*;
	
	// Akamai specific imports
	import com.akamai.net.*;
	import com.akamai.rss.*;

	public class CS4sampleSoSplayer extends MovieClip {

		// Declare private variables
		private var _dragging:Boolean;
		private var _bossMetafile:AkamaiBOSSParser;
		private var _nc:AkamaiConnection;
		private var _ns:AkamaiNetStream;
		private var _hasEnded:Boolean;
		private var _videoSettings:Object;
		private var _path:String;
		private var _streamLength:Number;
		
		//UIObjects
		private var _video:Video;
		private var _bPlayPause:Button;
		private var _bFullscreen:Button;
		private var _cbVideoScaleMode:ComboBox;
		private var _videoSlider:Slider;
		private var _volumeSlider:Slider;
		private var _progressBar:ProgressBar;
		private var _timeDisplay:TextField;
		private var _statusDisplay:TextField;
		private var _videoBackground:Shape;
		private var _status:String;
		
		// You may use these sample files while testing these code.
		private const SAMPLES:Array = [
								{label:"Metafile version I",data:"http://products.edgeboss.net/flash/products/mediaframework/fms/0223_quikpro_highlights_700.flv?xmlvers=1"},
								{label:"Metafile version II",data:"http://products.edgeboss.net/flash/products/mediaframework/fms/akamai_10ya_nab_700k.flv?xmlvers=2"},
								{label:"Progressive sample 1",data:"http://products.edgeboss.net/download/products/mediaframework/fms/0223_quikpro_lgwaveoftheday_web_700.flv"},
								{label:"Progressive sample 2",data:"http://products.edgeboss.net/download/products/mediaframework/fms/0406_bells_35thyearparty_big.flv"}
								];

		// Constructor
		public function CS4sampleSoSplayer():void {
			_dragging = false;
			_status = "";
			_streamLength = 0;

			initChildren();
		}
		
		// Add the children to the stage
		private function initChildren():void {
			stage.scaleMode = StageScaleMode.NO_SCALE;
			stage.align = StageAlign.TOP_LEFT;
			stage.addEventListener(FullScreenEvent.FULL_SCREEN, handleReturnFromFullScreen);
			stage.addEventListener(Event.RESIZE,handleResize);
			//
			_bossMetafile = new AkamaiBOSSParser();
			_bossMetafile.addEventListener(OvpEvent.PARSED,bossParsedHandler);
			_bossMetafile.addEventListener(OvpEvent.LOADED,bossLoadHandler);
			_bossMetafile.addEventListener(OvpEvent.ERROR,errorHandler);
			//
			_videoBackground = new Shape();
			_videoBackground.graphics.beginFill(0x000000);
            _videoBackground.graphics.drawRect(0,0,10,10);
			_videoBackground.graphics.endFill();
			addChild(_videoBackground);
			//
			_video = new Video();
			_video.smoothing = true;
			_video.visible = false;
			addChild(_video);
			//
			_bPlayPause = new Button();
			_bPlayPause.width = 70;
			_bPlayPause.label = "PAUSE";
			_bPlayPause.enabled = false;
			_bPlayPause.addEventListener(MouseEvent.CLICK,doPlayPause);
			addChild(_bPlayPause);
			//
			_bFullscreen = new Button();
			_bFullscreen.width = 90;
			_bFullscreen.label = "FULLSCREEN";
			_bFullscreen.enabled = false;
			_bFullscreen.addEventListener(MouseEvent.CLICK,switchToFullScreen);
			addChild(_bFullscreen);
			//
			_cbVideoScaleMode = new ComboBox();
			_cbVideoScaleMode.enabled = false;
			_cbVideoScaleMode.width = 80;
			_cbVideoScaleMode.dataProvider  = new DataProvider([{label:"Fit"},{label:"Stretch"},{label:"Native"}]);
			_cbVideoScaleMode.selectedIndex = 2;
			_cbVideoScaleMode.addEventListener(Event.CHANGE, scaleVideo);
			addChild(_cbVideoScaleMode);
			//
			_videoSlider = new Slider();
			_videoSlider.enabled = false;
			_videoSlider.addEventListener(SliderEvent.THUMB_PRESS,beginDrag);
			_videoSlider.addEventListener(SliderEvent.THUMB_RELEASE,endDrag);
			addChild(_videoSlider);
			//
			_progressBar = new ProgressBar();
			_progressBar.minimum = 0;
			_progressBar.maximum = 100;
			_progressBar.mode = ProgressBarMode.MANUAL 
			addChild(_progressBar);
			//
			_timeDisplay = new TextField();
			_timeDisplay.width = 150;
			_timeDisplay.selectable = false;
			_timeDisplay.antiAliasType = AntiAliasType.ADVANCED
			_timeDisplay.defaultTextFormat = new TextFormat("verdana",20,0x000000);
			_timeDisplay.text = "00:00|00:00";
			addChild(_timeDisplay);
			//
			_statusDisplay = new TextField();
			_statusDisplay.autoSize = TextFieldAutoSize.LEFT
			_statusDisplay.selectable = false;
			_statusDisplay.defaultTextFormat = new TextFormat("verdana",12,0x000000);
			_statusDisplay.text = "WAITING ...";
			addChild(_statusDisplay);
			//
			_volumeSlider = new Slider();
			_volumeSlider.enabled = false;
			_volumeSlider.liveDragging = true;
			_volumeSlider.minimum = 0;
			_volumeSlider.maximum = 100;
			_volumeSlider.value = 100;
			_volumeSlider.addEventListener(SliderEvent.CHANGE,volumeHandler);
			addChild(_volumeSlider);
			//
			_nc = new AkamaiConnection();
			_nc.addEventListener(OvpEvent.ERROR,errorHandler);
			_nc.addEventListener(NetStatusEvent.NET_STATUS,netStatusHandler);
			_nc.addEventListener(OvpEvent.STREAM_LENGTH,streamLengthHandler);		
			//
			handleResize(null);
			//
			_path = loaderInfo.parameters.url == undefined ? this.SAMPLES[0].data : loaderInfo.parameters.url;
			begin();
		}
		
		// Begins loading the metafile
		private function begin():void {
			_statusDisplay.text = "LOADING";
			_bPlayPause.enabled = false;
			_bFullscreen.enabled = false;
			_hasEnded = false;
			// Clean up from previous session, if it exists
			if (_nc.netConnection is NetConnection) {
				_ns.useFastStartBuffer = false;
				_nc.close();
			}
			// Decide if the link will return a BOSS xml metafile, or if it is
			// a direct reference to a progressive FLV file. 
			if (_path.split("/").length > 3 && _path.split("/")[3].toUpperCase() == "DOWNLOAD") {
				_nc.connect(null);
			} else {
				_bossMetafile.load(_path);
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
				case _bossMetafile.METAFILE_VERSION_I :
					write("  Server name: " + _bossMetafile.serverName);
					write("  Fallback server name: " + _bossMetafile.fallbackServerName);
					write("  App name: " + _bossMetafile.appName);
					write("  Stream name: " + _bossMetafile.streamName);
					write("  Is live: " + _bossMetafile.isLive);
					write("  Buffer time: " + _bossMetafile.bufferTime);
					_nc.requestedProtocol = "any";
					break;
				case _bossMetafile.METAFILE_VERSION_II :
					write("  Server name: " + _bossMetafile.serverName);
					write("  App name: " + _bossMetafile.appName);
					write("  Stream name: " + _bossMetafile.streamName);
					write("  Is live: " + _bossMetafile.isLive);
					write("  Buffer time: " + _bossMetafile.bufferTime);
					_nc.requestedProtocol = "any";
					break;
				case _bossMetafile.METAFILE_VERSION_IV :
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
			write("calling seek to " + _videoSlider.value);
			if (_hasEnded) {
				_hasEnded = false;
				_ns.play(_nc.isProgressive ? _path: _bossMetafile.streamName);
				_bPlayPause.label = "PAUSE";
			}
			_ns.seek(_videoSlider.value);
		}
		
		// Update the volume
		private function volumeHandler(e:SliderEvent):void {
			_ns.volume = _volumeSlider.value/100;
		}
		
		// Handles play and pause
		private function doPlayPause(e:MouseEvent):void {
			switch (_bPlayPause.label) {
				case "PAUSE" :
					_bPlayPause.label = "PLAY";
					_ns.pause();
					break;
				case "PLAY" :
					_bPlayPause.label = "PAUSE";
					if (_hasEnded) {
						_hasEnded = false;
						_ns.play(_ns.isProgressive ? _path: _bossMetafile.streamName);
					} else {
						_ns.resume();
					}
					break;
			}
		}

		// Once a good connection is found, this function will be called
		private function connectedHandler():void {
			_bPlayPause.enabled = true;
			_videoSlider.enabled = true;
			_volumeSlider.enabled = true;
			_bFullscreen.enabled = true;
			_volumeSlider.enabled = true;
			_cbVideoScaleMode.enabled = true;
			
			_ns = new AkamaiNetStream(_nc);
			_ns.addEventListener(OvpEvent.COMPLETE,endHandler);
			_ns.addEventListener(OvpEvent.PROGRESS,update);
			_ns.addEventListener(NetStatusEvent.NET_STATUS,streamStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_PLAYSTATUS,streamPlayStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_METADATA,metadataHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_CUEPOINT,cuepointHandler);
			_ns.addEventListener(OvpEvent.MP3_ID3,id3Handler);
			_ns.maxBufferLength = 45;			
			_ns.liveStreamAuthParams = _bossMetafile.playAuthParams;
			_ns.volume = _volumeSlider.value/100;
			_ns.useFastStartBuffer = !_bossMetafile.isLive;
			
			_video.attachNetStream(_ns);
			_bPlayPause.label = "PAUSE";
			// Branchbased on the playback mode
			if (_nc.isProgressive) {
				write("Progressive connection established");
				_progressBar.setProgress(0,100);
				_progressBar.visible = true;
				_ns.play(_path);
				
			} else {
				_progressBar.visible = false;
				_videoSlider.visible = !_bossMetafile.isLive;
				write("Successfully connected to: " + _nc.netConnection.uri);
				write("Port: " + _nc.actualPort);
				write("Protocol: " + _nc.actualProtocol);
				write("Server IP address: " + _nc.serverIPaddress);
				// If an ondemand stream, start the asynchronous process of requesting the stream length 
				if (!_bossMetafile.isLive) {
					_nc.requestStreamLength(_bossMetafile.streamName);
				}
				// Use fastStart if you are sure the connectivity of your clients is at least
				// twice the bitrate of the video they will be viewing.
				_ns.useFastStartBuffer = !_bossMetafile.isLive;
				_ns.play(_bossMetafile.streamName);
				_bPlayPause.label = "PAUSE";
			}
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
		
		// Handles a successful stream length request
		private function streamLengthHandler(e:OvpEvent):void {
			write("Stream length=" + e.data.streamLength);
			_streamLength = e.data.streamLength;
			_videoSlider.maximum = _streamLength;
		}
		
		// Receives information that the end of a streaming stream has been reached. 
		private function endHandler(e:OvpEvent):void {
			write("End of stream detected (streaming)");
			_hasEnded = true;
			_status = "ENDED";
			_bPlayPause.label = "PLAY";
		}
				
		// Receives all onPlayStatus events dispatched by the active NetStream
		private function streamPlayStatusHandler(e:OvpEvent):void {
			write(e.data.code);
		}
		
		// Updates the UI elements as the video plays
		private function update(e:OvpEvent):void {
			_timeDisplay.text = _bossMetafile.isLive ? _ns.timeAsTimeCode + " [Live]": _ns.timeAsTimeCode + "|"+ _nc.streamLengthAsTimeCode(_streamLength);
			if (!_dragging) {
				_videoSlider.value = _ns.time;
			}
			_statusDisplay.text = _status;
			if (_nc.isProgressive) {
				_progressBar.setProgress(_ns.bytesLoaded,_ns.bytesTotal);
			}
			_status = _status.indexOf("BUFFERING") != -1 ? "BUFFERING " + _ns.bufferPercentage +"%":_status;
		}
		
		// Receives all status events dispatched by the active NetStream
		private function streamStatusHandler(e:NetStatusEvent):void {
			write(_ns.time + " " + e.info.code);
			switch (e.info.code) {
					case "NetStream.Buffer.Full":
						_dragging = false;
						_video.visible = true;
					 	scaleVideo(null);
						_status = "PLAYING";
					break;
					case "NetStream.Pause.Notify":
						_status = "PAUSED";
					break;
					case "NetStream.Play.Start":
						_status = "BUFFERING";
					break;
					case "NetStream.Seek.Notify":
						_status = "SEEKING";
					break;
					case "NetStream.Buffer.Empty":
						if (!_hasEnded) {
							_status = "SEEKING";
						}
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
			_status = "ERROR: " + e.data.errorDescription;
			_statusDisplay.text = _status;
		}
		
		// Writes trace statements to the output display
		private function write(msg:String):void {
			trace(msg);
		}
		
		// Switches to full screen mode
		private function switchToFullScreen(e:MouseEvent):void {
			// when going out of full screen mode 
			// we use these values
			_videoSettings = new Object();
			_videoSettings.savedWidth = _video.width;
			_videoSettings.savedHeight = _video.height;
			_videoSettings.x = _video.x;
			_videoSettings.y = _video.y;
			// Pop the video to the top of the display stack 
			addChild(_video);
			this.stage["fullScreenSourceRect"] = new Rectangle(0,0,_video.videoWidth ,_video.videoHeight);
			this.stage["displayState"] = StageDisplayState.FULL_SCREEN;
			_video.width =  _video.videoWidth;
			_video.height =  _video.videoHeight;
			_video.smoothing = false;
			_video.x = 0;
			_video.y = 0;
		
		}
		
		// Handles the return from fullscreen
		private function handleReturnFromFullScreen(e:FullScreenEvent):void {
			if (!e.fullScreen) {
				_video.x = _videoSettings.x;
				_video.y = _videoSettings.y;
				_video.width = _videoSettings.savedWidth;
				_video.height = _videoSettings.savedHeight;
				_video.smoothing = true;
			}
		}
		
		// Scales the video to fit into the 320x240 window while preserving aspect ratio.
		private function scaleVideo(e:Event):void {
			switch (_cbVideoScaleMode.selectedItem.label) {
				case "Fit":
					if (_video.videoWidth/_video.videoHeight >= _videoBackground.width/_videoBackground.height) {
						_video.width = _videoBackground.width;
						_video.height = _videoBackground.width*_video.videoHeight/_video.videoWidth;
					} else {
						_video.width = _videoBackground.height*_video.videoWidth/_video.videoHeight;
						_video.height = _videoBackground.height;
					}
					_video.mask = null;
					break;
				case "Stretch":
					_video.width = _videoBackground.width;
					_video.height = _videoBackground.height;
					_video.mask = null
					break;
				case "Native":
					_video.width = _video.videoWidth;
					_video.height = _video.videoHeight;
					_video.mask = (_video.width > _videoBackground.width || _video.height> _videoBackground.height) ? _videoBackground:null;
					break;
			}
			_video.x = 10 + ((_videoBackground.width - _video.width)/2);
			_video.y = 10 + ((_videoBackground.height- _video.height)/2);
			
		}
			
		// Handle the stage resize event
		private function handleResize(e:Event):void {
			if (this.stage["displayState"] != StageDisplayState.FULL_SCREEN) {
				var w = stage.stageWidth;
				var h = stage.stageHeight;
				this.graphics.clear();
				this.graphics.beginFill(0xdddddd);
				this.graphics.lineStyle(1, 0xdddddd, 1, true);
				this.graphics.drawRoundRect(5, 5, w-10, h-10, 8);
	
				_videoBackground.width = w-20;
				_videoBackground.height = h-90;
				_videoBackground.x = 10;
				_videoBackground.y = 10;
				
				_bPlayPause.x = 20;
				_bPlayPause.y = h - 40;
				_bFullscreen.x = 95;
				_bFullscreen.y = h-40;
				_cbVideoScaleMode.x = 190;
				_cbVideoScaleMode.y = h-40;
				_statusDisplay.x = 280;
				_statusDisplay.y = h-38;
				//
				_volumeSlider.x = w - 107;
				_volumeSlider.y = h-30;
				_videoSlider.x = 20;
				_videoSlider.y = h - 60;
				_videoSlider.width = w - 200;
				_timeDisplay.x = w-160;
				_timeDisplay.y = h-74;
				_progressBar.x = 20;
				_progressBar.y = h - 63;
				_progressBar.width = w - 200;
				
				scaleVideo(null);
			}		
		}
	}
}
