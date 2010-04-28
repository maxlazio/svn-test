﻿// Copyright (c) 2009-2010, the Open Video Player authors. All rights reserved.
//
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions are 
// met:
//
//    * Redistributions of source code must retain the above copyright 
//notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above 
//copyright notice, this list of conditions and the following 
//disclaimer in the documentation and/or other materials provided 
//with the distribution.
//    * Neither the name of the openvideoplayer.org nor the names of its 
//contributors may be used to endorse or promote products derived 
//from this software without specific prior written permission.
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

// AkamaiConnection class AS3 Reference Player - RSS Feeds
//
// This class demonstrates use of the AkamaiConnection class
// in connecting to the Akamai CDN and in rendering and controlling
// a RSS Feed with MAST/VAST integration. 

package {

	// AS3 generic imports
	import flash.display.DisplayObject;
	import flash.display.Loader;
	import flash.display.LoaderInfo;
	import flash.display.MovieClip;
	import flash.display.Sprite;
	import flash.display.Stage;
	import flash.display.StageDisplayState;
	import flash.events.MouseEvent;
	import flash.events.Event;
	import flash.events.FullScreenEvent;
	import flash.events.IOErrorEvent;
	import flash.events.NetStatusEvent;
	import flash.events.SecurityErrorEvent;
	import flash.geom.Rectangle;
	import flash.media.Video;
	import flash.net.URLRequest;
	import flash.net.NetConnection;
	import flash.system.ApplicationDomain;
	import flash.system.LoaderContext;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.utils.Timer;

	// CS4 specific imports
	import fl.controls.Button;
	import fl.controls.Slider;
	import fl.controls.TileList;
	import fl.core.UIComponent;
	import fl.data.DataProvider;
	import fl.events.SliderEvent;
	import fl.managers.StyleManager;

	// OVP specific imports
	import org.openvideoplayer.rss.RSSFilterFields;
	import org.openvideoplayer.rss.ItemTO;
	import org.openvideoplayer.advertising.IVPAID;
	import org.openvideoplayer.plugins.*;
	import org.openvideoplayer.net.*;
	import org.openvideoplayer.events.*;
	import org.openvideoplayer.plugins.IOvpPlayer;
	import org.openvideoplayer.plugins.OvpPlayerEvent;

	// Akamai specific imports
	import com.akamai.net.AkamaiConnection;
	import com.akamai.net.AkamaiNetStream;
	import com.akamai.rss.AkamaiBOSSParser;
	import com.akamai.rss.AkamaiMediaRSS;

	public class CS4sampleRSS extends MovieClip implements IOvpPlayer {

		//-------------------------------------------------------------------
		//
		// Events
		//
		//-------------------------------------------------------------------

		[Event(name = "cuepoint",type = "org.openvideoplayer.plugins.OvpPlayerEvent")]
		[Event(name = "error",type = "org.openvideoplayer.plugins.OvpPlayerEvent")]
		[Event(name = "statechange",type = "org.openvideoplayer.plugins.OvpPlayerEvent")]

		//-------------------------------------------------------------------
		//
		// Constants
		//
		//-------------------------------------------------------------------

		private const _DEFAULT_VIDEO_WIDTH_:Number = 320;
		private const _DEFAULT_VIDEO_HEIGHT_:Number = 240;
		private const _PROGRESS_MIN_:Number = 0;
		private const _PROGRESS_MAX_:Number = 100;
		private const _PROGRESSIVE_:String = "video/x-flv";
		private const _SAMPLE_RSS_FEED_:String = "http://rss.streamos.com/streamos/rss/genfeed.php?feedid=1674&groupname=products";
		private const _STREAMING_:String = "application/xml";

		//-------------------------------------------------------------------
		//
		// Private vars
		//
		//-------------------------------------------------------------------

		private var _activeNC:AkamaiConnection;
		private var _bossFeed:AkamaiBOSSParser;
		private var _cuePointMgr:OvpCuePointManager;
		private var _currentDeliveryType:String;
		private var _currentKey:String;
		private var _currentState:String;
		private var _existingConnections:Object;
		private var _filename:String;
		private var _filteredList:Array;
		private var _inAdMode:Boolean;
		private var linearAdMC:MovieClip;
		private var _measuredBandwidth:Number;
		private var _metadata:Object;
		private var _mustSelectStream:Boolean;
		private var _ns:AkamaiNetStream;
		private var _parameters:Object;
		private var _playBtnStatePlaying:Boolean;
		private var _playlist:AkamaiMediaRSS;
		private var _playlistIndex:Number;
		private var _plugin_container:UIComponent;
		private var _pluginFiles:Array;
		private var _pluginsLoaded:int;
		private var _plugins:Array;
		private var _pluginsStr:String;
		private var _sliderDragging:Boolean;
		private var _state:String;
		private var _streamLength:Number;
		private var _videoHolder:UIComponent;
		private var _waitForSeek:Boolean;

		// Constructor
		public function CS4sampleRSS() {
			super();
			initApp();
		}

		// Initialize application
		private function initApp():void {
			addChildren();
			initVars();
			addUIListeners();
			loadPlugins();
		}

		private function addChildren():void {
			_plugin_container = new UIComponent();
			_plugin_container.visible = false;
			_plugin_container.width = 0;
			_plugin_container.height = 0;
			addChild(_plugin_container);

			linearAdMC = new MovieClip();
			linearAdMC.x = 0;
			linearAdMC.y = 0;
			linearAdMC.name = "linearAdMC";

			_videoHolder = new UIComponent();
			_videoHolder.width = _DEFAULT_VIDEO_WIDTH_;
			_videoHolder.height = _DEFAULT_VIDEO_HEIGHT_;
			_videoHolder.x = 20;
			_videoHolder.y = 103;
			_videoHolder.addChild(linearAdMC);
			addChild(_videoHolder);
		}

		// Initializes variables with starting values
		private function initVars():void {
			bufferingDisplay.text = "Waiting ...";

			btnLoad.useHandCursor = true;
			btnLoad.buttonMode = true;
			btnStopAd.useHandCursor = true;
			btnStopAd.buttonMode = true;
			btnPlayPause.useHandCursor = true;
			btnPlayPause.buttonMode = true;

			_cuePointMgr = new OvpCuePointManager();
			_currentState = _PROGRESSIVE_;
			
			_existingConnections = new Object();
			
			_inAdMode = false;
			
			_parameters = LoaderInfo(this.root.loaderInfo).parameters;
			_playBtnStatePlaying = false;
			_pluginFiles = new Array();
			_plugins = new Array();
			_pluginsLoaded = 0;
			progressBar.minimum = _PROGRESS_MIN_;
			progressBar.maximum = _PROGRESS_MAX_;

			_sliderDragging = false;
			_streamLength = 0;
			_state = "";

			tiRSSFeed.text = _SAMPLE_RSS_FEED_;

			video.smoothing = true;

			_waitForSeek = false;
		}


		//-------------------------------------------------------------------
		//
		// UI Event Listeners
		//
		//-------------------------------------------------------------------

		// Add Listeners for UI events
		private function addUIListeners():void {
			btnLoad.addEventListener(MouseEvent.CLICK,onLoadRSSFeed);
			btnPlayPause.addEventListener(MouseEvent.CLICK,onPlayPause);
			btnStopAd.addEventListener(MouseEvent.CLICK,onClickStopAd);
			volumeControl.addEventListener(SliderEvent.CHANGE,onChangeVolume);
			slider.addEventListener(SliderEvent.THUMB_PRESS,beginDrag);
			slider.addEventListener(SliderEvent.THUMB_RELEASE,endDrag);
			tileList.addEventListener(Event.CHANGE, playSelectedItem);
		}

		//-------------------------------------------------------------------
		//
		// UI Event Handlers
		//
		//-------------------------------------------------------------------

		// Handles the load button click for the RSS feed
		private function onLoadRSSFeed(event:MouseEvent):void {
			write("RSS Loading: " + tiRSSFeed.text);
			bufferingDisplay.text = "Loading ...";
			enableInputControls(false);
			_playlist.load(tiRSSFeed.text);
		}

		// Handles btnPlayPause button events
		private function onPlayPause(event:MouseEvent):void {
			if (_playBtnStatePlaying) {
				_ns.pause();
				btnPlayPause.label = "PLAY";
				updateState(OvpPlayerEvent.PAUSED);
			} else {
				_ns.resume();
				btnPlayPause.label = "PAUSE";
				updateState(OvpPlayerEvent.PLAYING);
			}
			_playBtnStatePlaying = ! _playBtnStatePlaying;
		}

		// Look for plug-ins that implement IVPAID and tell them to stop playing their ads
		private function onClickStopAd(e:Event):void {
			for (var i:int = 0; i < _plugins.length; i++) {
				var plugInSprite:Object = plugInSprite = _plugins[i];
				if ((plugInSprite is IVPAID) || plugInSprite.hasOwnProperty("getVPAID")) {
					var vpaid:IVPAID = plugInSprite.getVPAID() as IVPAID;
					vpaid.stopAd();
					btnStopAd.visible = false;
					btnStopAd.enabled = false;
					
					enablePlayerControls();
				}
			}
		}

		private function onChangeVolume(e:SliderEvent):void {
			if (_ns) {
				_ns.volume = e.value/100;
				dispatchEvent(new OvpPlayerEvent(OvpPlayerEvent.VOLUME_CHANGE, (e.value/100)));
			}
		}

		// Handle the start of a video scrub
		private function beginDrag(e:SliderEvent):void {
			_sliderDragging = true;
		}

		// Handle the end of a video scrub
		private function endDrag(e:SliderEvent):void {
			write("calling seek to " + slider.value);
			_sliderDragging = false;
			_waitForSeek = true;
			updateState(OvpPlayerEvent.SEEKING);
			_ns.seek(slider.value);
		}


		//-------------------------------------------------------------------
		//
		// Load plug-ins specified in the FlashVars
		//
		//-------------------------------------------------------------------

		private function loadPlugins():void {
			_pluginsStr = _parameters.plugins;
			
			if (_pluginsStr == null) {
				handleAllPluginsLoaded();
				return;
			}
			
			_pluginFiles = _pluginsStr.split(",");

			for (var i:int = 0; i < _pluginFiles.length; i++) {
				var url:String = _pluginFiles[i];

				if (url.search(/.swf$/i) == -1) {
					url += ".swf";
				}

				var loader:Loader = new Loader();
				var req:URLRequest=new URLRequest(url);
				var context:LoaderContext = new LoaderContext();

				// We have to load these into the same Application Domain so
				// the plug-ins can call methods here and we 
				// can listen for events fired by plug-ins
				context.applicationDomain=ApplicationDomain.currentDomain;
				loader.contentLoaderInfo.addEventListener(Event.COMPLETE, onSwfLoadComplete);
				loader.contentLoaderInfo.addEventListener(IOErrorEvent.IO_ERROR, onSwfLoadFailure);
				loader.contentLoaderInfo.addEventListener(SecurityErrorEvent.SECURITY_ERROR,securityErrorHandler);
				loader.load(req, context);
			}
		}
		
		//-------------------------------------------------------------------
		//
		// Plugin Event Handlers
		//
		//-------------------------------------------------------------------

		private function securityErrorHandler(e:Event):void {
			write("securityErrorHandler()");
		}

		private function onSwfLoadComplete(e:Event):void {
			var content:DisplayObject=e.target.content;

			// If we don't add it as a child to something, it won't be in the
			// display list and the stage property will be null
			_plugin_container.addChild(content);
			_plugins.push(content);

			if (content is IOvpPlugIn) {
				loadPlugin(content as IOvpPlugIn);
			}
		}

		private function onSwfLoadFailure(e:IOErrorEvent):void {
			write("Plug-in load failure: " + e.text);
		}

		// Load plugin
		private function loadPlugin(plugIn:IOvpPlugIn):void {
			write("Loading Plugin: " + plugIn.ovpPlugInName);
			plugIn.ovpPlugInTracingOn=true;
			plugIn.initOvpPlugIn(this);
			_pluginsLoaded++;

			if (_pluginsLoaded==_pluginFiles.length) {
				handleAllPluginsLoaded();
			}
		}

		// All plugins loaded successfully, enable the load buttons
		private function handleAllPluginsLoaded():void {
			write(_pluginsLoaded + " plug-ins loaded.");
			if (_pluginsLoaded == 0) {
				write("In order to specify the plugins and the MAST URL, you need to load this from an HTML file which specifies this info as flashvars.");
				write("See the file 'CS4sampleRSS-with-preroll.html' in the same folder as the FLA file for this sample.");
			}
			addEventListener(OvpPlayerEvent.DEBUG_MSG, onDebugMessage, false, 0, true);
			setUpRSSListeners();
			updateState(OvpPlayerEvent.WAITING);
		}


		//-------------------------------------------------------------------
		//
		// RSS Listeners
		//
		//-------------------------------------------------------------------

		private function setUpRSSListeners():void {
			_playlist = new AkamaiMediaRSS();
			_playlist.addEventListener(OvpEvent.PARSED, rssParsedHandler);
			_playlist.addEventListener(OvpEvent.LOADED, rssLoadHandler);
			_playlist.addEventListener(OvpEvent.ERROR, errorHandler);

			_bossFeed = new AkamaiBOSSParser();
			_bossFeed.addEventListener(OvpEvent.PARSED, bossParsedHandler);
			_bossFeed.addEventListener(OvpEvent.LOADED, bossLoadHandler);
			_bossFeed.addEventListener(OvpEvent.ERROR, errorHandler);

			btnLoad.enabled=true;
		}

		//-------------------------------------------------------------------
		//
		// RSS Event Handlers
		//
		//-------------------------------------------------------------------

		// Handles the notification that the rss feed was successfully loaded.
		private function rssLoadHandler(e:OvpEvent):void {
			write("RSS Feed loaded successfully");
			enableInputControls();
		}

		// Handles the notification that the rss feed was successfully parsed.
		private function rssParsedHandler(e:OvpEvent):void {
			write("RSS parsed successfully");
			var dp:DataProvider = new DataProvider();
			for (var i:uint = 0; i < _playlist.itemArray.length; i++) {
				dp.addItem( { label:_playlist.itemArray[i].title, source:_playlist.itemArray[i].media.thumbnail.url, data: _playlist.itemArray[i] } );
			}
			tileList.dataProvider=dp;
			tileList.selectedIndex=0;
			enableInputControls();
			playSelectedItem();
		}

		// Handles the notification that the BOSS feed was successfully loaded.
		private function bossLoadHandler(e:OvpEvent):void {
			write("BOSS loaded successfully");
		}

		// Handles the notification that the BOSS feed was successfully parsed
		private function bossParsedHandler(e:OvpEvent):void {
			write("BOSS parsed successfully");
			var protocol:String=_bossFeed.versionOfMetafile==_bossFeed.METAFILE_VERSION_IV?_bossFeed.protocol.indexOf("rtmpe")!=-1?"rtmpe,rtmpte":"any":"any";
			startPlayback(_bossFeed.hostName, _bossFeed.streamName, _bossFeed.connectAuthParams, protocol);
		}

		// Plays the selected item in the tileList
		private function playSelectedItem(e:Event=null):void {
			var item:ItemTO=ItemTO(tileList.selectedItem.data);
			var url:String;

			updateState(OvpPlayerEvent.START_NEW_ITEM);
			_playBtnStatePlaying=true;
			btnPlayPause.label="PAUSE";
			video.visible=false;
			_playlistIndex=tileList.selectedIndex;

			if (item&&item.media.contentArray.length>1) {
				// Item has group tag, so we need to figure out the correct file to play based
				// on the bandwidth measurement. We assume the content items are specified
				// in an unknown order and therefore we must sort them first.
				if (isNaN(_measuredBandwidth)) {
					// Bandwidth has not yet been measured, but we only know bandwidth after connecting.
					// Therefore, use the first item simply to connect and measure bandwdith and set a flag
					// to revisit this selection routine once the bandwidth is known.
					url=item.media.getContentAt(0).url;
					_mustSelectStream=true;
				} else {
					_mustSelectStream=false;
					var temp:Array = new Array();

					for (var i:uint = 0; i < item.media.contentArray.length; i++) {
						temp.push({index: i, bitrate: Number(item.media.getContentAt(i).bitrate.toString())});
					}

					// Default order will be lowest to highest after sort
					temp.sortOn(sortOnBitrate);
					url=item.media.getContentAt(0).url;
					slider.maximum=timecodeToSeconds(item.media.getContentAt(0).duration);

					for (var j:uint = 0; j < item.media.contentArray.length; j++) {
						if (_measuredBandwidth>1.5*item.media.getContentAt(j).bitrate) {
							url=item.media.getContentAt(j).url;
							slider.maximum=timecodeToSeconds(item.media.getContentAt(j).duration);
						}
					}
				}
			} else {
				slider.maximum=timecodeToSeconds(item.media.getContentAt(0).duration);
				url=item.media.getContentAt(0).url;
			}

			_currentDeliveryType=item.enclosure.type;

			// Branch for progressive/streaming playback
			switch (_currentDeliveryType) {
				case _PROGRESSIVE_ :
					slider.enabled=true;
					_currentState=_PROGRESSIVE_;
					progressBar.setProgress(_PROGRESS_MIN_,_PROGRESS_MAX_);
					progressBar.visible=true;
					startPlayback("null", url);
					break;
				case _STREAMING_ :
					_currentState=_STREAMING_;
					progressBar.visible=false;
					_bossFeed.load(url);
					break;
				default :
					write("UNRECOGNIZED MEDIA ITEM TYPE IN RSS FEED -- the media item titled '" + item.media.title + "' did not contain a recognized type attribute.");
					break;
			}
		}

		private function updateState(state:String):void {
			if (_state!=state) {
				_state=state;
				dispatchEvent(new OvpPlayerEvent(OvpPlayerEvent.STATE_CHANGE, _state));
			}
		}

		// Commences connection to an ondemand stream
		private function startPlayback(hostname:String, streamname:String, authParams:String = "", protocol:String = "any"):void {
			// The combination of hostname and authParams defines a unique key 
			// which we can use to reference stored connections.
			_currentKey=hostname+authParams;
			_filename=streamname;

			video.clear();
			video.attachNetStream(null);

			// Close the current AkamaiConnection 
			if (_activeNC is AkamaiConnection) {
				if (_activeNC.isProgressive) {
					_ns.pause();
				} else {
					_ns.close();
				}
			}

			// If the required connection already exists, then use it
			if (_existingConnections[_currentKey] is AkamaiConnection) {
				_activeNC=_existingConnections[_currentKey];
				startUsingNC();
			} else {
				// Create a new AkamaiConnection
				var _nc:AkamaiConnection = new AkamaiConnection();
				_nc.addEventListener(OvpEvent.ERROR, errorHandler);
				_nc.addEventListener(OvpEvent.BANDWIDTH, bandwidthHandler);
				_nc.addEventListener(OvpEvent.STREAM_LENGTH, streamLengthHandler);
				_nc.addEventListener(NetStatusEvent.NET_STATUS, netStatusHandler);
				_nc.requestedProtocol=protocol;

				if (authParams!="") {
					_nc.connectionAuth=authParams;
				}
				_nc.connect(hostname);
			}
		}

		// Start using new AkamaiConnection instance
		private function startUsingNC():void {
			
			enablePlayerControls();
			_ns=new AkamaiNetStream(_activeNC);

			// Use fastStart if you are sure the connectivity of your clients is at least
			// twice the bitrate of the video they will be viewing.
			_ns.useFastStartBuffer=true;
			_ns.createProgressivePauseEvents=true;
			_ns.volume=0;
			_ns.addEventListener(OvpEvent.COMPLETE, endHandler);
			_ns.addEventListener(OvpEvent.PROGRESS, update);
			_ns.addEventListener(NetStatusEvent.NET_STATUS, streamStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_PLAYSTATUS, streamPlayStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_METADATA, metadataHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_CUEPOINT, cuepointHandler);
			_ns.addEventListener(OvpEvent.STREAM_LENGTH, streamLengthHandler);
			_ns.maxBufferLength=5;

			video.attachNetStream(_ns);

			// Give the cue point manager the OvpNetStream object so it can start monitoring
			_cuePointMgr.netStream=_ns;

			// Progressive streams don't connect to a streaming server and therefore 
			// can't measure bandwidth
			if (_currentDeliveryType==_PROGRESSIVE_) {
				btnPlayPause.label="PAUSE";
				_ns.play(_filename);
			} else {
				// Assume a streaming file and start the asynchronous process of 
				// requesting the stream length
				_activeNC.requestStreamLength(_filename);
				// Start the asynchronous process of estimating bandwidth. Only do this if 
				// bandwidth has not already been measured.
				if (isNaN(_measuredBandwidth)) {
					_activeNC.detectBandwidth();
				} else {
					btnPlayPause.label="PAUSE";
					_ns.play(_filename);
				}
			}
		}


		//-------------------------------------------------------------------
		//
		// Event Handlers
		//
		//-------------------------------------------------------------------

		private function cuepointHandler(e:OvpEvent):void {
			dispatchEvent(new OvpPlayerEvent(OvpPlayerEvent.CUEPOINT, e.data));
		}

		// Handles the OvpEvent.PROGRESS event fired by the OvpNetStream class
		private function update(e:OvpEvent):void {
			if (! _sliderDragging&&! _waitForSeek) {
				slider.value=_ns.time;
			}
			timeDisplay.text=_ns.timeAsTimeCode+"|"+_activeNC.streamLengthAsTimeCode(_streamLength);
			bufferingDisplay.visible=_ns.isBuffering;
			bufferingDisplay.text="Buffering: "+_ns.bufferPercentage+"%";
			if (_currentDeliveryType==_PROGRESSIVE_) {
				progressBar.setProgress(_ns.bytesLoaded, _ns.bytesTotal);
			}
		}

		// Handles all OvpEvent.ERROR events
		private function errorHandler(e:OvpEvent):void {
			write("Error #" + e.data.errorNumber + " " + e.data.errorDescription + " " + e.currentTarget);
			switch (e.data.errorNumber) {
				case OvpError.STREAM_NOT_FOUND :
					write("UNABLE TO FIND STREAM -- connected to the server at " + _activeNC.serverIPaddress + " but timed-out trying to locate the live stream " + _filename);
					break;
				default :
					write("ERROR -- Error #" + e.data.errorNumber + ": " + e.data.errorDescription);
					break;
			}
		}

		private function streamLengthHandler(e:OvpEvent):void {
			write("Stream length is " + e.data.streamLength);
			slider.maximum=e.data.streamLength;
			_streamLength=e.data.streamLength;
			_ns.volume=.8;

			enablePlayerControls();
		}

		// Handles the result of the bandwidth estimate
		private function bandwidthHandler(e:OvpEvent):void {
			write("Bandwidth measured at " + e.data.bandwidth + " kbps and latency is " + e.data.latency + " ms.");
			_measuredBandwidth=e.data.bandwidth;
			if (_mustSelectStream) {
				_mustSelectStream=false;
				playSelectedItem();
			} else {
				btnPlayPause.label="PAUSE";
				_ns.play(_filename);
			}
		}

		// Handles NetStatusEvent.NET_STATUS events fired by the OvpConnection class
		private function netStatusHandler(e:NetStatusEvent):void {
			write(e.info.code);
			switch (e.info.code) {
				case "NetConnection.Connect.Rejected" :
					write("Rejected by server. Reason is " + e.info.description);
					break;
				case "NetConnection.Connect.Success" :
					connectedHandler(e);
					break;
					// If a connection closes due to a disconnect or idle timeut, 
					// then remove it for the list of available connections
				case "NetConnection.Connect.Closed" :
					var key:String=AkamaiConnection(e.target).hostName+"/"+AkamaiConnection(e.target).appNameInstanceName+AkamaiConnection(e.target).connectionAuth;
					delete _existingConnections[key];
					break;
			}
		}

		// Once a good connection is found, this handler will be called
		private function connectedHandler(e:NetStatusEvent):void {
			_existingConnections[_currentKey]=AkamaiConnection(e.currentTarget);
			_activeNC=_existingConnections[_currentKey];
			write("Successfully connected to: " + _activeNC.netConnection.uri);
			write("Port: " + _activeNC.actualPort);
			write("Protocol: " + _activeNC.actualProtocol);
			write("IP address: " + _activeNC.serverIPaddress);
			startUsingNC();
		}

		// Receives information that the end of stream has been reached
		private function endHandler(e:OvpEvent):void {
			write("End of stream detected.");
			_playlistIndex=_playlistIndex+1>=_playlist.itemCount?0:_playlistIndex+1;
			tileList.selectedIndex=_playlistIndex;
			playSelectedItem();
		}

		// Handles the NetStatusEvent.NET_STATUS events fired by the OvpNetStream class
		private function streamStatusHandler(e:NetStatusEvent):void {
			write(e.info.code);
			switch (e.info.code) {
				case "NetStream.Play.StreamNotFound" :
					updateState(OvpPlayerEvent.WAITING);
					dispatchEvent(new OvpPlayerEvent(OvpPlayerEvent.ERROR, "stream not found"));
					break;
				case "NetStream.Play.Start" :
					updateState(OvpPlayerEvent.BUFFERING);
					if (_inAdMode) {
						_ns.pause();
					}
					break;
				case "NetStream.Play.Stop" :
					// See if a PDL file has ended (streaming end is handling in 
					// streamPlayStatusHandler
					if (_ns.isProgressive && (duration >= (_ns.time - 1))) {
						updateState(OvpPlayerEvent.COMPLETE);
					}
					break;
				case "NetStream.Buffer.Full" :
					// _waitForSeek is used to stop the slider from updating while 
					// the stream transtions after a seek
					_waitForSeek=false;
					if (_state!=OvpPlayerEvent.PAUSED) {
						updateState(OvpPlayerEvent.PLAYING);
						_ns.volume=volumeControl.value;
						video.visible=true;
					}
					break;
				case "NetStream.Pause.Notify" :
					updateState(OvpPlayerEvent.PAUSED);
					break;
				case "NetStream.Unpause.Notify" :
					updateState(OvpPlayerEvent.PLAYING);
					break;
				case "NetStream.Seek.Notify" :
					updateState(_state != OvpPlayerEvent.PAUSED ? OvpPlayerEvent.SEEKING : OvpPlayerEvent.PAUSED);
					break;
			}
		}

		// Handles the OvpEvent.NETSTREAM_PLAYSTATUS events fired by the OvpNetStream class
		private function streamPlayStatusHandler(e:OvpEvent):void {
			write(e.data.code);
			switch (e.data.code) {
				case "NetStream.Play.Complete" :
					updateState(OvpPlayerEvent.COMPLETE);
					break;
			}
		}

		// Handles the OvpEvent.NETSTREAM_METADATA events fired by the OvpNetStream class
		private function metadataHandler(e:OvpEvent):void {
			_metadata = e.data;
			video.visible=true;

			// Adjust the video dimensions on the stage if they do not match the metadata
			if ((Number(e.data["width"]) != video.width) || (Number(e.data["height"]) != video.height)) {
				scaleVideo(Number(e.data["width"]), Number(e.data["height"]));
			}
		}


		//-------------------------------------------------------------------
		//
		// Helper Methods
		//
		//-------------------------------------------------------------------

		private function enableInputControls(enable:Boolean = true):void {
			tiRSSFeed.enabled=enable;
			btnLoad.enabled=enable;
		}

		private function enablePlayerControls(enable:Boolean = true):void {
			if (enable && _inAdMode) {
				return;
			}
			btnPlayPause.enabled=enable;
			slider.enabled=enable;
			volumeControl.enabled=enable;
		}

		// Scales the video to fit into the 320x240 window while preserving aspect ratio.
		private function scaleVideo(w:Number, h:Number):void {
			if (w/h>=4/3) {
				video.width=_DEFAULT_VIDEO_WIDTH_;
				video.height=_DEFAULT_VIDEO_WIDTH_*h/w;
			} else {
				video.width=_DEFAULT_VIDEO_HEIGHT_*w/h;
				video.height=_DEFAULT_VIDEO_HEIGHT_;
			}
			video.x = 30 + (_DEFAULT_VIDEO_WIDTH_ - video.width) * 0.5;
			video.y = 104 + (_DEFAULT_VIDEO_HEIGHT_ - video.height) * 0.5;
			video.visible=true;
		}

		private function showScrubTime(val:String):String {
			var sec:Number=Number(val);
			var h:Number=Math.floor(sec/3600);
			var m:Number = Math.floor((sec % 3600) / 60);
			var s:Number = Math.floor((sec % 3600) % 60);

			return (h == 0 ? "" : (h < 10 ? "0" + h.toString() + ":" : h.toString() + ":")) + (m < 10 ? "0" + m.toString() : m.toString()) + ":" + (s < 10 ? "0" + s.toString() : s.toString());
		}

		// Sort function for the bitrate array
		private function sortOnBitrate(a:Object, b:Object):Number {
			var aBit:Number=a["bitrate"];
			var bBit:Number=b["bitrate"];

			if (aBit>bBit) {
				return 1;
			} else if (aBit < bBit) {
				return -1;
			} else {
				return 0;
			}
		}

		// Converts timecode to seconds
		private function timecodeToSeconds(timecode:String):Number {
			return Number(timecode.split(":")[0]) * 3600 + Number(timecode.split(":")[1]) * 60 + Number(timecode.split(":")[2]);
		}

		// Debugging Output
		private function write(... arguments):void {
			text_debug.appendText("> SampleRSS - " + arguments + "\n");
		}

		// Debugging Output
		private function onDebugMessage(event:OvpPlayerEvent):void {
			text_debug.appendText((event.data as String) + "\n");
		}
		
		// ----------------------------------------------------------------
		//
		// IOvpPlayer Implementation
		//
		// ----------------------------------------------------------------

		public function get plugins():Array {
			return _plugins;
		}

		public function get flashvars():Object {			
			return _parameters;
		}

		public function get currentBitrate():int {
			if (_metadata && _metadata.videodatarate) {
				return _metadata.videodatarate;
			}
			return 0;
		}

		public function get duration():Number {
			return _streamLength;
		}

		public function get position():int {
			if (_ns) {
				return _ns.time;
			}
			return 0;
		}

		public function get fullScreen():Boolean {
			return false;
		}

		public function get captionsActive():Boolean {
			return false;
		}

		public function get hasVideo():Boolean {
			return true;
		}

		public function get hasAudio():Boolean {
			return true;
		}

		public function get hasCaptions():Boolean {
			return false;
		}

		public function get itemCount():int {
			return 1;
		}

		public function get itemsPlayed():int {
			return 0;
		}

		public function get playerWidth():int {
			return video.width;
		}

		public function get playerHeight():int {
			return video.height;
		}

		public function get contentWidth():int {
			return video.width;
		}

		public function get contentHeight():int {
			return video.height;
		}

		public function get contentTitle():String {
			return "";
		}

		public function get contentURL():String {
			return _filename;
		}

		public function set advertisingMode(value:Boolean):void {
			if (value) {
				_inAdMode = true;

				if (_ns) {
					_ns.pause();
					_ns.volume = volumeControl.value;
				}

				btnStopAd.visible = true;
				btnStopAd.enabled = true;
				video.visible = false;
				linearAdMC.visible = true;
				enablePlayerControls(false);
				
				// Volume control can affect the ad volume so leave that control enabled
				volumeControl.enabled=true;

				dispatchEvent(new OvpPlayerEvent(OvpPlayerEvent.VOLUME_CHANGE, volumeControl.value/100));
			} else {
				_inAdMode = false;

				if (_playBtnStatePlaying) {
					_ns.resume();
				}

				video.visible = true;
				linearAdMC.visible = false;
				btnStopAd.visible = false;
				btnStopAd.enabled = false;
				
				enablePlayerControls();
			}
		}

		public function getSpriteById(id:String):Sprite {
			if (id.toLowerCase() == "linearadmc") {
				linearAdMC.visible = true;
				return linearAdMC;
			}
			return null;
		}

		public function addCuePoint(cuePoint:Object):void {
			_cuePointMgr.addCuePoint(cuePoint);
		}

		public function pausePlayer():void {
			_ns.pause();
		}

		public function resumePlayer():void {
			_ns.resume();
		}

		public function startPlayer():void {
			playSelectedItem();
		}

		public function stopPlayer():void {

		}		
	}
}