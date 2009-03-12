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
	
	import flash.display.*;
	import flash.events.*; 
	import flash.geom.Rectangle;
	import flash.net.URLRequest;
	import flash.ui.*;
	import flash.utils.Timer;
	import flash.external.ExternalInterface;
	import flash.system.Capabilities;
	import flash.net.navigateToURL;
	
	import org.openvideoplayer.events.OvpEvent;
	import model.Model;
	import view.*;
	import advertising.AdManager;
	
	/**
	* This class represents the document class of the AkamaiMultiPlayer. In order to render the views, the player depends upon graphic elements
	* stored in library of the fla. <p/>
	* 
	* The AkamaiMultiPlayer offers a robust AS3- based platform for playing back a wide variety of streaming and
	* progressive media delivered by the Akamai platform. The player can handle and differentiate between the following source formats:
	* <ol>
	* <li> Stream OS media RSS playlists</li>
	* <li> Stream OS metafiles, Type I, Type II, Type IV</li>
	* <li> Stream OS progressive download links</li>
	* <li> AMD streaming links, both ondemand and live/li>
	* <li> AMD progressive links</li>
	* <li> Dynamic Streaming packages (as SMIL files)</li>
	* </ol>
	* The player has the following features:
	* <ul>
	* <li>Dynamically scalable - all views are re-scaled and positioned each time the flash player is resized.</li>
	* <li>Supports two layout modes - overlay (where the controls are overlaid over the video and hide themselves if you mouse-off the player) and
	* side-by-side, where the controls are permanently on the screen and the playlist view is located to the right of the video content area. You may switch
	* between layout modes during runtime.</li>
	* <li>Three video rendering modes - fit, strech and native</li>
	* <li>Standard controls - play/pause, volume, seek, current position, duration</li>
	* <li>Fullscreen mode</li>
	* <li>Supports link and embed data for re-distribution.</li>
	* <li>Playlist button allows playlist visibility to be toggled</li>
	* <li>Built-in debug screen to assist with debugging connection and play back problems</li>
	* <li>Right-click context menu control over display mode, video scaling mode and debug panel</li>
	* </ul>
	* The player can be initialized via certain flashvars. These are detailed below. Note that only one - the src property - is required. The remainder will be initiliazed
	* with default values. All values are passed as strings.
	* <ul>
	* <li>src - the source content which the player is expected to play. This must be a valid and well structured reference to AMD or Stream OS content, or a HTTP link to
	* progressive content on any web server. Valid formats include:
	* 	<ul>
	* 	<li>http://products.edgeboss.net/flash/products/mediaframework/fms/0223_quikpro_highlights_700.flv?xmlvers=1</li>
	* 	<li>http://products.edgeboss.net/download/products/mediaframework/fms/0223_quikpro_lgwaveoftheday_web_700.flv</li>
	* 	<li>http://products.edgeboss.net/flash-live/products/40504/500_products_productsdemo_080602.flv</li>
	* 	<li>rtmp://cp34973.live.edgefcs.net/live/Flash_live_bench_mb&#64;3725</li>
	* 	<li>rtmp://cp27886.edgefcs.net/ondemand/14808/nocc_small307K.flv</li>
	* 	<li>rtmp://cp39443.live.edgefcs.net/live/mystream&#64;s34</li>
	* 	<li>rtmp://cp14808.edgefcs.net/ondemand/mp3:14808/nocc_small.mp3?a=1&b=2</li>
	* 	<li>http://metadata.streamos.com/adobe/sample_feed/xml</li>
	* 	<li>http://rss.streamos.com/streamos/rss/genfeed.php?feedid=1453&groupname=openvideoplayer</li>
	* 	<li>http://sessions.adobe.com/360FlexSJ2008/feed.xml</li>
	* 	<li>my-stub-playlist.xml</li>
	* 	</ul>
	* </li>
	* <li>mode - the layout mode. Pass in "overlay" to specify that the player starts in overlay mode or "sidebyside" for the side-by-side mode.
	* Default is "sidebyside".</li>
	* <li>scaleMode - the scaling mode for the video, one of four possible values: 
	* 	<ul>
	* 		<li>"fit" - [Default] the video is scaled as large as possible to fit within the confines of the player while still preserving its native aspect ratio.</li>
	* 		<li>"stretch" - the video is is stretched to fit exactly within the confines of the player. Native aspect ratio is not preserved.</li>
	* 		<li>"native" - the video is scaled to the native size it was encoded at. Note that this size may be larger than the player that is trying to render the video,
	* 		in which case the video will be centered within the available player space. </li>
	* 		<li>"nativeorsmaller" - the video will be scaled to its native size unless that is larger than the player in which case "fit" scaling will be invoked.</li>
	* 	</ul>
	* <li>frameColor - the HEX value for the frame color, for example "FF0000". Do not prepend with "0x" or "#". Default is "333333"</li>
	* <li>fontColor - the HEX value for the control bar font color, for example "FF0000". Do not prepend with "0x" or "#". Default is "CCCCCC"</li>
	* <li>themeColor - the HEX value for the theme color, for example "FF0000". The theme color is used in multiple locations throughout the player,
	* including the button mouse-over highlights, scrub bar shading. volume control shading and playlist title font color. Do not prepend with "0x" or "#".
	* Default is "0395D3"</li>
	* <li>autostart - if set to "true", video starts playing the moment the player is loaded. if set to "false", the player will render the first keyframe in the video
	* to create a splash screen and then stop. Default is "true".</li>
	* <li>link - the URL which the user can use to link to a mounted instance of the player. This parameter must be escaped (url-encoded) or else it will mask other flashvar
	* attributes. Note that the button to surface the link/embded panel will only be visible if either the link or embed parameter has a non empty-string value.
	* Default is empty-string.</li>
	* <li>embed - the URL which the user can use to embed the player. This parameter must be escaped (url-encoded) or else it will mask other flashvar
	* attributes. Note that the button to surface the link/embded panel will only be visible if either the link or embed parameter has a non empty-string value.
	* Default is empty-string.</li>
	* </ul>
	*/

	/**
	* Dispatched when the user clicks the fullscreen toggle button
	*/
	[Event (name = "toggleFullscreen", type = "flash.events.Event")]
	
	/**
	* Dispatched when the underlying video stream begins playing.
	*/
	[Event (name = "playStart", type = "flash.events.Event")]
	
	/**
	* Dispatched when the underlying video stream finishes playing.
	*/
	[Event (name = "endOfItem", type = "flash.events.Event")]
	
	/**
	* Dispatched when this player isresized
	*/
	[Event (name = "resize", type = "flash.events.Event")]
	
	/**
	* Dispatched when the volume changes
	*/
	[Event (name = "volumeChanged", type = "flash.events.Event")]


	public class AkamaiMultiPlayer extends MovieClip {

		private var _model:Model;
		private var _playlist:PlaylistView;
		private var _controlbar:ControlbarView;
		private var _background:BackgroundView;
		private var _video:VideoView;
		private var _shareEmbed:ShareEmbedView;
		private var _debugPanel:DebugPanelView;
		private var _errorDisplay:ErrorDisplayView;
		private var _adMC:MovieClip;
		private var _contextMenu:ContextMenu;
		private var _timer:Timer;
		private var _lastWidth:Number;
		private var _lastHeight:Number;
		private var _src:String;
		private var _flashvars:Object;
		private var _adManager:AdManager;


		/**
		 * Constructor
		 * @param	starting width of the player
		 * @param	starting height of the player
		 * @param	flashvars - the loaderInfo.parameters object passed in from the HTML wrapper
		 */

		public function AkamaiMultiPlayer(width:Number = 774, height:Number = 473, flashvars:Object = null):void {
			init(flashvars == null ? new Object():flashvars,width,height);
			createChildren();
			start();
		}

		private function init(flashvars:Object,width:Number,height:Number):void {
			_model = new Model(flashvars);
			_model.addEventListener(_model.EVENT_LOAD_UI, loadUIhandler);
			_model.addEventListener(_model.EVENT_TOGGLE_FULLSCREEN, toggleFullscreenHandler);
			_model.addEventListener(_model.EVENT_RESIZE, resizeHandler);
			_model.addEventListener(_model.EVENT_PLAY_START, playStartHandler);
			_model.addEventListener(_model.EVENT_VOLUME_CHANGE, volumeChangeHandler);
			_model.addEventListener(_model.EVENT_END_OF_ITEM, endOfItemHandler);
			_model.addEventListener(OvpEvent.NETSTREAM_CUEPOINT, cuePointHandler);

			this.addEventListener(MouseEvent.MOUSE_MOVE, mouseMoveHandler);
			this.addEventListener(MouseEvent.ROLL_OUT, leaveStageHandler);

			_lastWidth = width;
			_lastHeight = height;
			_timer = new Timer(5000, 1);
			_timer.addEventListener(TimerEvent.TIMER_COMPLETE, leaveStageHandler);

			if (ExternalInterface.available) {
                try {
                    ExternalInterface.addCallback("setNewSource", setNewSource);
                } catch (e:Error) {
					// don't notify the user since ExternalInterface is not necessary for standard operation;
				}
            } 
		}

		private function createChildren():void {
			_errorDisplay = new ErrorDisplayView(_model);
			addChild(_errorDisplay);

			if (ExternalInterface.available) {
				_model.debug("External Interface available = " + ExternalInterface.available);
				flash.external.ExternalInterface.call("isReady");
			}
		}

		public function setNewSource(src:String):void{
			_model.debug("Setting new source to "+ src);
			_model.src  = src;
		}

		public function stopPlayback():void {
			_model.stopPlayback();
		}
		
		public function pausePlayback():void {
			_model.pause();
		}
		
		public function resumePlayback():void {
			_model.play();
		}
		
		public function enableControls():void {
			_model.enableControls();
		}
		
		public function disableControls():void {
			_model.disableControls();
		}
		
		public function getAdMovieClip():MovieClip {
			return _adMC;
		}
		
		public function get volume():Number {
			return _model.volume;
		}
		
		public function getAdRectangle():Rectangle {
			return new Rectangle(3, 3, _model.availableVideoWidth, _model.availableVideoHeight);
		}
		
		public function set adManager(_value:AdManager):void {
			_adManager = _value;
			_adManager.addEventListener("adStart", adStartHandler);
			_adManager.addEventListener("adEnd", adEndHandler);
			if (_adManager.cuePointManager) {
				_model.cuePointManager = _adManager.cuePointManager;
			}
		}
		
		private function adStartHandler(e:Event):void {
			_model.adStarted();
		}
		
		private function adEndHandler(e:Event):void {
			_model.adEnded();
		}
		
		private function cuePointHandler(e:OvpEvent):void {
			dispatchEvent(new OvpEvent(OvpEvent.NETSTREAM_CUEPOINT, e.data));
		}

		private function loadUIhandler(e:Event):void {
			_background = new BackgroundView(_model);
			addChild(_background);
			
			_video = new VideoView(_model);
			addChild(_video);
			
			_adMC = new MovieClip();
			addChild(_adMC);
			
			_shareEmbed = new ShareEmbedView(_model);
			addChild(_shareEmbed);

			_controlbar = new ControlbarView(_model);
			addChild(_controlbar);

			if (_model.hasPlaylist) {
				_playlist = new PlaylistView(_model);
				addChild(_playlist);
			}

			_debugPanel = new DebugPanelView(_model);
			addChild(_debugPanel);

			addChild(_errorDisplay);

			resize(null);

			addContextMenu();
			_model.UIready();
		}

		private function addContextMenu():void {
			contextMenu = new ContextMenu();
            contextMenu.hideBuiltInItems();

			var idItem:ContextMenuItem = new ContextMenuItem("Built on OpenVideoPlayer", true);
			idItem.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, idItemSelectHandler);
			contextMenu.customItems.push(idItem);

			var modeItem:ContextMenuItem = new ContextMenuItem("Toggle layout mode (overlay|side-by-side)",true);
			modeItem.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, modeItemSelectHandler);

			contextMenu.customItems.push(modeItem);

			var fitItem:ContextMenuItem = new ContextMenuItem("Video scale: FIT",true,_model.scaleMode != _model.SCALE_MODE_FIT);
            fitItem.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, fitItemSelectHandler);

			contextMenu.customItems.push(fitItem);

			var stretchItem:ContextMenuItem = new ContextMenuItem("Video scale: STRETCH",false,_model.scaleMode != _model.SCALE_MODE_STRETCH);
            stretchItem.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, stretchItemSelectHandler);

			contextMenu.customItems.push(stretchItem);

			var nativeItem:ContextMenuItem = new ContextMenuItem("Video scale: NATIVE",false,_model.scaleMode != _model.SCALE_MODE_NATIVE);
            nativeItem.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, nativeItemSelectHandler);

			contextMenu.customItems.push(nativeItem);

			var nativeOrSmallerItem:ContextMenuItem = new ContextMenuItem("Video scale: NATIVE OR SMALLER",false,_model.scaleMode != _model.SCALE_MODE_NATIVE_OR_SMALLER);
            nativeOrSmallerItem.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, nativeOrSmallerItemSelectHandler);

			contextMenu.customItems.push(nativeOrSmallerItem);

			var debugItem:ContextMenuItem = new ContextMenuItem("Toggle Statistics Panel",true,true);
            debugItem.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, debugItemSelectHandler);

			contextMenu.customItems.push(debugItem);

			var autoItem:ContextMenuItem = new ContextMenuItem("Enable Auto Bitrate Switching",true,_model.isMultiBitrate && !_model.useAutoDynamicSwitching);
			autoItem.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT,autoSwitchHandler);

			contextMenu.customItems.push(autoItem);

			var manualItem:ContextMenuItem = new ContextMenuItem("Enable Manual Switching",false,_model.isMultiBitrate && _model.useAutoDynamicSwitching);
			manualItem.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, manualSwitchHandler);

			contextMenu.customItems.push(manualItem);

			var switchUpItem:ContextMenuItem = new ContextMenuItem("Switch up",false,false);
			switchUpItem.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, switchUpHandler);

			contextMenu.customItems.push(switchUpItem);

			var switchDownItem:ContextMenuItem = new ContextMenuItem("Switch down",false,false);
			switchDownItem.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, switchDownHandler);

			contextMenu.customItems.push(switchDownItem);
		}

		private function resizeHandler(e:Event):void {
			if (contextMenu is ContextMenu) {
				if (_model.isMultiBitrate) {
					contextMenu.customItems[7].enabled = !_model.useAutoDynamicSwitching
					contextMenu.customItems[8].enabled = _model.useAutoDynamicSwitching
					contextMenu.customItems[9].enabled = !_model.useAutoDynamicSwitching
					contextMenu.customItems[10].enabled = !_model.useAutoDynamicSwitching
				} else {
					contextMenu.customItems[7].enabled = false;
					contextMenu.customItems[8].enabled = true;
					contextMenu.customItems[9].enabled = false;
					contextMenu.customItems[10].enabled = false;
				}
			}
			dispatchEvent(new Event("resize"));
		}

		private function start():void {
			resize(null);
			_model.start();
		}

		private function idItemSelectHandler(e:ContextMenuEvent):void {
			navigateToURL(new URLRequest("http://openvideoplayer.sourceforge.net/"), "_blank");
		}
		
		private function modeItemSelectHandler(e:ContextMenuEvent):void {
			_model.isOverlay = !_model.isOverlay;
			resize(null);
		}

		private function fitItemSelectHandler(e:ContextMenuEvent):void {
			_model.scaleMode = _model.SCALE_MODE_FIT;
			contextMenu.customItems[2].enabled = false;
			contextMenu.customItems[3].enabled = true;
			contextMenu.customItems[4].enabled = true;
			contextMenu.customItems[5].enabled = true;
			resize(null);
		}

		private function stretchItemSelectHandler(e:ContextMenuEvent):void {
			_model.scaleMode = _model.SCALE_MODE_STRETCH;
			contextMenu.customItems[2].enabled = true
			contextMenu.customItems[3].enabled = false;
			contextMenu.customItems[4].enabled = true;
			contextMenu.customItems[5].enabled = true;

			resize(null);
		}

		private function nativeItemSelectHandler(e:ContextMenuEvent):void {
			_model.scaleMode = _model.SCALE_MODE_NATIVE;
			contextMenu.customItems[2].enabled = true;
			contextMenu.customItems[3].enabled = true;
			contextMenu.customItems[4].enabled = false;
			contextMenu.customItems[5].enabled = true;
			resize(null);
		}

		private function nativeOrSmallerItemSelectHandler(e:ContextMenuEvent):void {
			_model.scaleMode = _model.SCALE_MODE_NATIVE_OR_SMALLER;
			contextMenu.customItems[2].enabled = true;
			contextMenu.customItems[3].enabled = true;
			contextMenu.customItems[4].enabled = true;
			contextMenu.customItems[5].enabled = false;
			resize(null);
		}

		private function autoSwitchHandler(e:ContextMenuEvent):void {
			_model.useAutoDynamicSwitching = true;
			contextMenu.customItems[7].enabled = false;
			contextMenu.customItems[8].enabled = true;
			contextMenu.customItems[9].enabled = false;
			contextMenu.customItems[10].enabled = false;
		}

		private function manualSwitchHandler(e:ContextMenuEvent):void {
			_model.useAutoDynamicSwitching = false;
			contextMenu.customItems[7].enabled = true;
			contextMenu.customItems[8].enabled = false;
			contextMenu.customItems[9].enabled = true
			contextMenu.customItems[10].enabled = true
		}

		private function switchUpHandler(e:ContextMenuEvent):void {
			_model.switchUp();
		}
		
		private function switchDownHandler(e:ContextMenuEvent):void {
			_model.switchDown();
		}

		private function debugItemSelectHandler(e:ContextMenuEvent):void {
			_model.toggleDebugPanel();
		}

		private function mouseMoveHandler(e:MouseEvent):void {
			_model.showControlBar(true);
			if (stage.displayState == StageDisplayState.FULL_SCREEN) {
				_timer.reset();
				_timer.start();
			}
		}

		private function leaveStageHandler(e:Event):void {
			_model.showControlBar(false);
		}

		private function toggleFullscreenHandler(e:Event):void {
			dispatchEvent(new Event("toggleFullscreen"));
		}
		
		private function playStartHandler(e:Event):void {
			dispatchEvent(new Event("playStart"));
		}
		
		private function endOfItemHandler(e:Event):void {
			dispatchEvent(new Event("endOfItem"));
		}
		
		private function volumeChangeHandler(e:Event):void {
			dispatchEvent(new Event("volumeChanged"));
		}
		
		public function resizeTo(w:Number,h:Number):void {
			_lastWidth = w;
			_lastHeight = h;
			resize(null);
		}

		private function resize(e:Event):void {
			_model.resize(_lastWidth, _lastHeight);
		}
	}
}
