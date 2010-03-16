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
package {
	
	import advertising.AdManager;
	
	import flash.display.*;
	import flash.events.*;
	import flash.external.ExternalInterface;
	import flash.geom.Rectangle;
	import flash.net.URLRequest;
	import flash.net.navigateToURL;
	import flash.ui.*;
	import flash.utils.Timer;
	
	import model.Model;
	
	import org.openvideoplayer.events.OvpEvent;
	
	import view.*;
	

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


	public class BlinkxExampleAkamaiMultiPlayer extends MovieClip {

		protected var _model:Model;
		protected var _playlist:PlaylistView;
		protected var _controlbar:ControlbarView;
		protected var _background:BackgroundView;
		protected var _video:VideoView;
		protected var _shareEmbed:ShareEmbedView;
		protected var _debugPanel:DebugPanelView;
		protected var _errorDisplay:ErrorDisplayView;
		protected var _adMC:MovieClip;
		protected var _contextMenu:ContextMenu;
		protected var _timer:Timer;
		protected var _lastWidth:Number;
		protected var _lastHeight:Number;
		protected var _src:String;
		protected var _flashvars:Object;
		protected var _adManager:AdManager;


		/**
		 * Constructor
		 * @param	starting width of the player
		 * @param	starting height of the player
		 * @param	flashvars - the loaderInfo.parameters object passed in from the HTML wrapper
		 */

		public function BlinkxExampleAkamaiMultiPlayer(width:Number = 774, height:Number = 473, flashvars:Object = null):void {
			init(flashvars == null ? new Object():flashvars,width,height);
			createChildren();
			start();
			_model.playlistVisible = true;
			
		}
		
		
		protected function init(flashvars:Object,width:Number,height:Number):void {
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

		protected function createChildren():void {
			_errorDisplay = new ErrorDisplayView(_model);
			addChild(_errorDisplay);

			if (ExternalInterface.available) {
				_model.debug("External Interface available = " + ExternalInterface.available);
				flash.external.ExternalInterface.call("isReady");
			}
		}

		public function showError(err:String):void{
			_model.showError(err);
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
		
		protected function adStartHandler(e:Event):void {
			_model.adStarted();
		}
		
		protected function adEndHandler(e:Event):void {
			_model.adEnded();
		}
		
		protected function cuePointHandler(e:OvpEvent):void {
			dispatchEvent(new OvpEvent(OvpEvent.NETSTREAM_CUEPOINT, e.data));
		}

		protected function loadUIhandler(e:Event):void {
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

		protected function addContextMenu():void {
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

		protected function resizeHandler(e:Event):void {
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

		protected function start():void {
			resize(null);
			_model.start();
		}

		protected function idItemSelectHandler(e:ContextMenuEvent):void {
			navigateToURL(new URLRequest("http://openvideoplayer.sourceforge.net/"), "_blank");
		}
		
		protected function modeItemSelectHandler(e:ContextMenuEvent):void {
			_model.isOverlay = !_model.isOverlay;
			resize(null);
		}

		protected function fitItemSelectHandler(e:ContextMenuEvent):void {
			_model.scaleMode = _model.SCALE_MODE_FIT;
			contextMenu.customItems[2].enabled = false;
			contextMenu.customItems[3].enabled = true;
			contextMenu.customItems[4].enabled = true;
			contextMenu.customItems[5].enabled = true;
			resize(null);
		}

		protected function stretchItemSelectHandler(e:ContextMenuEvent):void {
			_model.scaleMode = _model.SCALE_MODE_STRETCH;
			contextMenu.customItems[2].enabled = true
			contextMenu.customItems[3].enabled = false;
			contextMenu.customItems[4].enabled = true;
			contextMenu.customItems[5].enabled = true;

			resize(null);
		}

		protected function nativeItemSelectHandler(e:ContextMenuEvent):void {
			_model.scaleMode = _model.SCALE_MODE_NATIVE;
			contextMenu.customItems[2].enabled = true;
			contextMenu.customItems[3].enabled = true;
			contextMenu.customItems[4].enabled = false;
			contextMenu.customItems[5].enabled = true;
			resize(null);
		}

		protected function nativeOrSmallerItemSelectHandler(e:ContextMenuEvent):void {
			_model.scaleMode = _model.SCALE_MODE_NATIVE_OR_SMALLER;
			contextMenu.customItems[2].enabled = true;
			contextMenu.customItems[3].enabled = true;
			contextMenu.customItems[4].enabled = true;
			contextMenu.customItems[5].enabled = false;
			resize(null);
		}

		protected function autoSwitchHandler(e:ContextMenuEvent):void {
			_model.useAutoDynamicSwitching = true;
			contextMenu.customItems[7].enabled = false;
			contextMenu.customItems[8].enabled = true;
			contextMenu.customItems[9].enabled = false;
			contextMenu.customItems[10].enabled = false;
		}

		protected function manualSwitchHandler(e:ContextMenuEvent):void {
			_model.useAutoDynamicSwitching = false;
			contextMenu.customItems[7].enabled = true;
			contextMenu.customItems[8].enabled = false;
			contextMenu.customItems[9].enabled = true
			contextMenu.customItems[10].enabled = true
		}

		protected function switchUpHandler(e:ContextMenuEvent):void {
			_model.switchUp();
		}
		
		protected function switchDownHandler(e:ContextMenuEvent):void {
			_model.switchDown();
		}

		protected function debugItemSelectHandler(e:ContextMenuEvent):void {
			_model.toggleDebugPanel();
		}

		protected function mouseMoveHandler(e:MouseEvent):void {
			_model.showControlBar(true);
			if (stage.displayState == StageDisplayState.FULL_SCREEN) {
				_timer.reset();
				_timer.start();
			}
		}

		protected function leaveStageHandler(e:Event):void {
			_model.showControlBar(false);
		}

		protected function toggleFullscreenHandler(e:Event):void {
			dispatchEvent(new Event("toggleFullscreen"));
		}
		
		protected function playStartHandler(e:Event):void {
			dispatchEvent(new Event("playStart"));
		}
		
		protected function endOfItemHandler(e:Event):void {
			dispatchEvent(new Event("endOfItem"));
		}
		
		protected function volumeChangeHandler(e:Event):void {
			dispatchEvent(new Event("volumeChanged"));
		}
		
		public function resizeTo(w:Number,h:Number):void {
			_lastWidth = w;
			_lastHeight = h;
			resize(null);
		}

		protected function resize(e:Event):void {
			_model.resize(_lastWidth, _lastHeight);
		}
		
		public function playVideos(videos:Array):void{
			_model.setPlaylist( videos );
		}
	}
}
