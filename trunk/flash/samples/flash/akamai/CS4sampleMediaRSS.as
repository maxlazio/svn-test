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
This application is a reference Flash CS4 application demonstrating integration between the AkamaiConnection, AkamaiMediaRSS and
AkamaiBOSSParser classes, in conjunction the event and RSS subclasses which together comprise the Flash Media Player framework.

The application loads a RSS feed from StreamOS, parses it, displays the items and begins autoplaying the first item. The player
handles both streaming and progressive links and has a connection optimization routine so that existing AkamaiConnection
instances are re-used as much as possible. The class supports the group tag in the MediaRSS for streaming links, allowing 
measured bandwidth to dictate which of the grouped content items to use. 

The UI and functional elements of this player have been wrapped into this single .as file for ease of distribution. In a real-world application
Akamai recommends breaking at the various UI and functional items into reusable sub-classes and deploying a traditional MVC architecture. 
*/

package {
	// AS3 generic imports
	import flash.display.MovieClip;
	import flash.display.Loader;
	import flash.display.LoaderInfo;
	import flash.events.*;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.media.Video;
	import flash.utils.Timer;
	import flash.net.URLRequest;
	// CS4 specific imports
	import fl.controls.Button;
	import fl.controls.Slider;
	import fl.controls.RadioButton;
	import fl.controls.RadioButtonGroup;
	import fl.events.SliderEvent;
	import fl.data.DataProvider;
	import fl.managers.StyleManager;
	// OVP specific imports
	import org.openvideoplayer.net.*;
	import org.openvideoplayer.events.*;
	import org.openvideoplayer.rss.*;
	// Akamai specific imports
	import com.akamai.net.*;
	import com.akamai.rss.*;

	public class CS4sampleMediaRSS extends MovieClip {

		// Declare private variables
		private var _dragging:Boolean;
		private var _filename:String;
		private var _playlist:AkamaiMediaRSS;
		private var _bossFeed:AkamaiBOSSParser;
		private var _existingConnections:Object;
		private var _currentKey:String;
		private var _activeNC:AkamaiConnection;
		private var _ns:AkamaiNetStream;
		private var _measuredBandwidth:Number;
		private var _playlistIndex:Number;
		private var _mustSelectStream:Boolean;
		private var _currentDeliveryType:String;
		private var _thumbHolder:MovieClip;
		private var _filteredList:Array;
		private var _rssFieldChoice:String;
		private var _rssMatchChoice:String;
		private var _streamLength:Number;
		private var _useFilteredList:Boolean

		
		private const SAMPLE_URL:String = "http://rss.streamos.com/streamos/rss/genfeed.php?feedid=1674&groupname=products";


		// Constructor
		public function CS4sampleMediaRSS():void {
			_rssFieldChoice = "all";
			_rssMatchChoice = "any";
			_useFilteredList = false;
			_dragging = false;

			initChildren();
		}
		
		// Initialize the children on the stage
		private function initChildren():void {
			_playlist = new AkamaiMediaRSS();
			_playlist.addEventListener(OvpEvent.PARSED,rssParsedHandler);
			_playlist.addEventListener(OvpEvent.LOADED,rssLoadHandler);
			_playlist.addEventListener(OvpEvent.ERROR,errorHandler);
			//
			_bossFeed = new AkamaiBOSSParser();
			_bossFeed.addEventListener(OvpEvent.PARSED,bossParsedHandler);
			_bossFeed.addEventListener(OvpEvent.LOADED,bossLoadHandler);
			_bossFeed.addEventListener(OvpEvent.ERROR,errorHandler);
			//
			playlistPath.text = SAMPLE_URL;
			bLoad.addEventListener(MouseEvent.CLICK,doLoad);
			filterOptions.bFilter.addEventListener(MouseEvent.CLICK,doFilter);
			bPlayPause.enabled = false;
			bPlayPause.addEventListener(MouseEvent.CLICK,doPlayPause);
			videoSlider.enabled = false;
			videoSlider.addEventListener(SliderEvent.THUMB_PRESS,beginDrag);
			videoSlider.addEventListener(SliderEvent.THUMB_RELEASE,endDrag);
			volumeSlider.enabled = false;
			volumeSlider.addEventListener(SliderEvent.CHANGE,volumeHandler);
			bufferingDisplay.text = "Waiting ...";
			_existingConnections = new Object();
			progressBar.minimum = 0;
			progressBar.maximum = 100;
			//
			filterOptions.visible = false;
			var tf:TextFormat = new TextFormat();
			tf.color = 0xffffff;
			filterOptions.rbFieldTitle.setStyle("textFormat", tf);
			filterOptions.rbFieldTitle.addEventListener(MouseEvent.CLICK,rssFieldsClickHandler);
			filterOptions.rbFieldDesc.setStyle("textFormat", tf);
			filterOptions.rbFieldDesc.addEventListener(MouseEvent.CLICK,rssFieldsClickHandler);
			filterOptions.rbFieldAll.setStyle("textFormat", tf);
			filterOptions.rbFieldAll.addEventListener(MouseEvent.CLICK,rssFieldsClickHandler);
			filterOptions.rbMatchAny.setStyle("textFormat", tf);
			filterOptions.rbMatchAny.addEventListener(MouseEvent.CLICK,rssMatchClickHandler);
			filterOptions.rbMatchAll.setStyle("textFormat", tf);
			filterOptions.rbMatchAll.addEventListener(MouseEvent.CLICK,rssMatchClickHandler);
		}
		
		// Handles RSS field radio button group click event
		private function rssFieldsClickHandler(e:MouseEvent):void {
			_rssFieldChoice = e.target.value;
		}
		
		private function rssMatchClickHandler(e:MouseEvent):void {
			_rssMatchChoice = e.target.value;
		}
		
		// Handles the LOAD button click event
		private function doLoad(e:MouseEvent):void {
			bufferingDisplay.text = "Loading RSS ...";
			_playlistIndex = 0;
			errorDisplay.text = "";
			_playlist.load(playlistPath.text);
		}
		
		// Handles the FILTER button click event
		private function doFilter(e:MouseEvent):void {
			_playlistIndex = 0;
			if (filterOptions.filterText.text == "" || filterOptions.filterText.text == "*") {
				populateScrollPane(_playlist.itemArray);
				_useFilteredList = false;
				return;
			}
				
			var filterFields:RSSFilterFields = new RSSFilterFields();
			switch (_rssFieldChoice)
			{
				case "title":
					filterFields.title = true;
					break;
				case "description":
					filterFields.description = true;
					break;
				case "all":
					filterFields.setAll(true);
					break;
			}
			
			var match:int = _rssMatchChoice == "any" ? _playlist.FILTER_ANY : _playlist.FILTER_ALL;
			_filteredList = _playlist.filterItemList(filterOptions.filterText.text, filterFields, match);			
			populateScrollPane(_filteredList);
			_useFilteredList = true;
		}
		
		// Handles the notification that the rss feed was successfully loaded.
		private function rssLoadHandler(e:OvpEvent):void {
			bLoad.enabled = true;
			trace("RSS loaded successfully");
		}
		
		// Called when the RSS feed has been successfully parsed
		private function rssParsedHandler(e:OvpEvent):void {
			trace("RSS parsed successfully");
			filterOptions.visible = true;
			populateScrollPane(_playlist.itemArray);
			playSelectedItem();

		}
		
		private function populateScrollPane(ar:Array) {
			_thumbHolder = new MovieClip();
			for (var i:uint=0; i<ar.length; i++) {
				var thumb:Thumb = new Thumb();
				thumb.title.text = ar[i].title;
				// The media item may not contain a thumbnail
				if (ar[i].media.thumbnail != null) {
					var loader:Loader = new Loader();
					loader.contentLoaderInfo.addEventListener(Event.COMPLETE,scaleThumb);
					loader.load(new URLRequest(ar[i].media.thumbnail.url));
					loader.x = 16;
					loader.y=43;
					thumb.addChild(loader);
				}
				thumb.x = (i%3)*145;
				thumb.y = Math.floor(i/3)*140;
				thumb["selected"] = i == 0;
				thumb["index"] = i;
				thumb.backgroundOver.visible = false;
				thumb.backgroundSelected.visible = i ==0;
				thumb.useHandCursor = true;
				thumb.addEventListener(MouseEvent.ROLL_OVER, handleRollOver);
				thumb.addEventListener(MouseEvent.ROLL_OUT, handleRollOut);
				thumb.addEventListener(MouseEvent.CLICK, handleThumbSelect);
				_thumbHolder.addChild(thumb);
				
			}
			scrollPane.source = _thumbHolder;
		}
		
		private function scaleThumb(e:Event):void {
			var w:Number = e.target.width;
			var h:Number = e.target.height;
			if (w/h >= 120/90) {
				e.target.loader.width = 120;
				e.target.loader.height = 120*h/w;
			} else {
				e.target.loader.height = 90;
				e.target.loader.width = 90*w/h;
			}
			e.target.loader.x = 16 + (120-e.target.loader.width)/2;
			e.target.loader.y = 43 + (90-e.target.loader.height)/2;
		}
		
		// Called when the mouse rolls over a playlist item
		private function handleRollOver(e:MouseEvent):void {
			e.currentTarget.backgroundOver.visible = true;
			e.currentTarget.backgroundSelected.visible = false;
		}
		
		// Called when the mouse rolls off a playlist item
		private function handleRollOut(e:MouseEvent):void {
			e.currentTarget.backgroundOver.visible = false;
			e.currentTarget.backgroundSelected.visible = e.currentTarget["selected"];
		}
		
		// Called when the user selects a playlist thumbnail for playback
		private function handleThumbSelect(e:MouseEvent):void {
			_playlistIndex = e.currentTarget["index"];
			playSelectedItem();

		}
		
		// Highlights the active item in the playlist
		private function highlightActiveItem():void {
			for (var i:uint=0; i<_thumbHolder.numChildren; i++) {
				_thumbHolder.getChildAt(i)["selected"] = i == _playlistIndex;
				Thumb(_thumbHolder.getChildAt(i)).backgroundOver.visible = false;
				Thumb(_thumbHolder.getChildAt(i)).backgroundSelected.visible = i == _playlistIndex;
			}
		}
		
		// Plays the selected item
		private function playSelectedItem():void {
			highlightActiveItem();
						
			var item:ItemTO = _useFilteredList ? _filteredList[_playlistIndex] : _playlist.getItemAt(_playlistIndex);
			
			var url:String;
			if (item.media.contentArray.length > 1) {
				// Item has group tag, so we need to figure out the correct file to play based
				// on the bandwidth measurement. We assume the content items are specified
				// in an unknown order and therefore we must sort them first.
				if (isNaN(_measuredBandwidth)) {
					// Bandwidth has not yet been measured, but we only know bandwidth after connecting.
					// Therefore, use the first item simply to connect and measure bandwdith and set a flag
					// to revisit this selection routine once the bandwidth is known. 
					url = item.media.getContentAt(0).url;
					_mustSelectStream = true;
				} else {
					_mustSelectStream = false;
					var temp:Array  = new Array();
					for (var i:uint=0; i<item.media.contentArray.length; i++) {
						temp.push({index:i,bitrate:Number(item.media.getContentAt(i).bitrate.toString())});
					}
					// Default order will be lowest to highest
					temp.sortOn(sortOnBitrate);
					url = item.media.getContentAt(0).url;
					for (var j:uint=0; j<item.media.contentArray.length; j++) {
						if ( _measuredBandwidth > 1.5*item.media.getContentAt(j).bitrate) {
							url = item.media.getContentAt(j).url;
						}
					}
				}
			} else {
				url = item.media.getContentAt(0).url;
			}
			_currentDeliveryType = item.enclosure.type;
			// At this stage, we branch for progressive/streaming playback
			switch (_currentDeliveryType) {
					// Progressive
				case "video/x-flv" :
					progressBar.setProgress(0,100);
					progressBar.visible = true;
					startPlayback("null",url);
					break;
					// Streaming
				case "application/xml" :
					progressBar.visible = false;
					_bossFeed.load(url);
					break;
					// Unrecognized
				default:
					errorDisplay.text = "ERROR: the media item #" + _playlistIndex + " does not contain a recognized type attribute.";
   					break;
			}
		}
		
		// Sorting function for the bitrate array
		private function sortOnBitrate(a:Object, b:Object):Number {
			var aBit:Number = a["bitrate"];
			var bBit:Number = b["bitrate"];
			if (aBit> bBit) {
				return 1;
			} else if (aBit < bBit) {
				return -1;
			} else {
				return 0;
			}
		}
		
		// Called when the BOSS feed has been successfully loaded
		private function bossLoadHandler(e:OvpEvent):void {
			trace("BOSS loaded successfully");
		}
		
		// Called whe the BOSS feed has been successfully parsed
		private function bossParsedHandler(e:OvpEvent):void {
			trace("BOSS parsed successfully");
			var protocol:String = _bossFeed.versionOfMetafile == _bossFeed.METAFILE_VERSION_IV ? _bossFeed.protocol.indexOf("rtmpe") != -1 ? "rtmpe,rtmpte":"any":"any";
			startPlayback(_bossFeed.hostName,_bossFeed.streamName,_bossFeed.connectAuthParams,protocol);
		}
		
		// Commences connection to an ondemand stream
		private function startPlayback(hostname:String,streamname:String,authParams:String="",protocol:String = "any"):void {
			// The combination of hostname and authParams defines a unique key
			// which we can use to reference stored connections.
			_currentKey = hostname+authParams;
			_filename = streamname;
			if (_activeNC is AkamaiConnection) {
				if (_activeNC.isProgressive) {
					_ns.pause();
				} else {
					_ns.close();
				}
			}
			// Check if the connection already exists
			if (_existingConnections[_currentKey]is AkamaiConnection) {
				trace("Using existing netconnection");
				_activeNC = _existingConnections[_currentKey];
				startUsingNC();
			} else {
				trace("Estabishing a new connection");
				var _nc:AkamaiConnection = new AkamaiConnection();
 
				_nc.addEventListener(OvpEvent.ERROR,errorHandler);
				_nc.addEventListener(OvpEvent.BANDWIDTH,bandwidthHandler);
				_nc.addEventListener(NetStatusEvent.NET_STATUS, netStatusHandler);
				_nc.addEventListener(OvpEvent.STREAM_LENGTH,streamLengthHandler);
				
				_nc.requestedProtocol = protocol;
				if (authParams != "") {
					_nc.connectionAuth = authParams;
				}
				_nc.connect(_currentDeliveryType == "progressive" ? "":hostname);
			}
		}
		
		// Initates playback of the video on the active AkamaiConnection
		private function startUsingNC():void {
			bPlayPause.enabled = true;
			
			_ns = new AkamaiNetStream(_activeNC);
				
			_ns.addEventListener(OvpEvent.COMPLETE,endHandler);
			_ns.addEventListener(OvpEvent.PROGRESS,update);
			_ns.addEventListener(NetStatusEvent.NET_STATUS,streamStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_PLAYSTATUS,streamPlayStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_METADATA,metadataHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_CUEPOINT,cuepointHandler);
			_ns.addEventListener(OvpEvent.MP3_ID3,id3Handler);
			_ns.addEventListener(OvpEvent.STREAM_LENGTH, streamLengthHandler);
			
			_ns.maxBufferLength = 5;
			// Use fastStart if you are sure the connectivity of your clients is at least
			// twice the bitrate of the video they will be viewing.
			_ns.useFastStartBuffer = true;
			
			video.attachNetStream(_ns);
			if (_currentDeliveryType == "video/x-flv") {
				bPlayPause.label = "PAUSE";
				_ns.play(_filename);
			} else {
				// start the asynchronous process of requesting the stream length
				_activeNC.requestStreamLength(_filename);
				// start the asynchronous process of estimating bandwidth. Only do this if 
				// bandwidth has not already been measured.
				if (isNaN(_measuredBandwidth)) {
					_activeNC.detectBandwidth();
				} else {
					bPlayPause.label = "PAUSE";
					_ns.play(_filename);
				}
			}
		}
		
		// Handle the start of a video scrub
		private function beginDrag(e:SliderEvent):void {
			_dragging = true;
		}
		
		// Handle the end of a video scrub
		private function endDrag(e:SliderEvent):void {
			_ns.seek(videoSlider.value);
		}
		
		// Update the volume
		private function volumeHandler(e:SliderEvent):void {
			_ns.volume = volumeSlider.value/100;
		}
		
		// Handles the playPause button press
		private function doPlayPause(e:MouseEvent):void {
			switch (bPlayPause.label) {
				case "PAUSE" :
					_ns.pause();
					bPlayPause.label = "PLAY";
					break;
				case "PLAY" :
					_ns.resume();
					bPlayPause.label = "PAUSE";
					break;
			}
		}
		
		// Called once a good connection is found by the active AkamaiConnection
		private function connectedHandler(e:NetStatusEvent):void {
			_existingConnections[_currentKey] = AkamaiConnection(e.currentTarget);
			_activeNC = _existingConnections[_currentKey];
			startUsingNC();
		}
		
		// Handles the result of the bandwidth estimate
		private function bandwidthHandler(e:OvpEvent):void {
			_measuredBandwidth = e.data.bandwidth;
			trace("Bandwidth measured at " + e.data.bandwidth+ " kbps and latency is " + e.data.latency + " ms.");
			if (_mustSelectStream) {
				_mustSelectStream = false;
				playSelectedItem();
			} else {
				bPlayPause.label = "PAUSE";
				_ns.play(_filename);
			}
		}
		
		// Handles a successful stream length request
		private function streamLengthHandler(e:OvpEvent):void {
			trace("Stream length=" + e.data.streamLength);
			videoSlider.maximum = e.data.streamLength;
			bPlayPause.enabled = true;
			videoSlider.enabled = true;
			volumeSlider.enabled = true;
			_ns.volume = .8;
			_streamLength = e.data.streamLength;

		}
		
		// Receives information that the end of a streaming stream has been reached. 
		private function endHandler(e:OvpEvent):void {
				trace("End of stream detected (streaming)");
				playNext();
		}
				
		// Receives all onPlayStatus events dispatched by the active NetStream
		private function streamPlayStatusHandler(e:OvpEvent):void {
			trace(e.data.code);
		}
		
		// Plays the next item in the playlist.
		private function playNext():void {
			var itemCount = _useFilteredList ? _filteredList.length : _playlist.itemCount;
			
			_playlistIndex = _playlistIndex + 1 >= itemCount ? 0 : _playlistIndex + 1;
			playSelectedItem();
		}
		
		// Updates the UI elements as the video plays
		private function update(e:OvpEvent):void {
			timeDisplay.text = _ns.timeAsTimeCode + " | " + _activeNC.streamLengthAsTimeCode(_streamLength);
			if (!_dragging) {
				videoSlider.value = _ns.time;
			}
			bufferingDisplay.visible = _ns.isBuffering;
			bufferingDisplay.text = "Buffering: " + _ns.bufferPercentage+"%";
			if (_currentDeliveryType == "video/x-flv") {
				progressBar.setProgress(_ns.bytesLoaded,_ns.bytesTotal);
			}
		}
		
		// Receives all status events dispatched by the active NetStream
		private function streamStatusHandler(e:NetStatusEvent):void {
			trace(e.info.code);
			switch (e.info.code) {
				case "NetStream.Buffer.Full" :
					_dragging = false;
					break;
			}
		}
		// Here comes our id3 info in response to a request to getMp3Id3Info(name)
		private function id3Handler(e:OvpEvent):void {
			for (var i:String in e.data) {
				trace("ID3: " + i + " " + e.data[i]);
			}
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
		
		// Receives all status events dispatched by the active NetConnection
		private function netStatusHandler(e:NetStatusEvent):void {
			switch(e.info.code) {
				case "NetConnection.Connect.Success":
					connectedHandler(e);
					break;
				// If a connection closes due to a disconnect or idle timeut, then remove it
				// for the list of available connections
				case "NetConnection.Connect.Closed":
					var key:String = AkamaiConnection(e.target).hostName + "/" + AkamaiConnection(e.target).appNameInstanceName + AkamaiConnection(e.target).connectionAuth;
					delete _existingConnections[key];
				break;
			}

		}
		
		// Handles all error events for the connection and MediaRSS classes
		private function errorHandler(e:OvpEvent):void {
			trace("Error #" + e.data.errorNumber + " " +e.data.errorDescription + " " + e.currentTarget);
			errorDisplay.text = "Error #" + e.data.errorNumber + ": " +e.data.errorDescription;
		}
	}
}