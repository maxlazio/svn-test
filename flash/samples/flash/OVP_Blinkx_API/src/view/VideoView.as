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
package view {
	
	import flash.display.*;
	import flash.events.*;
	import flash.geom.*;
	import flash.media.Video;
	import model.Model;
	import controller.*
	import ui.*;
	/**
	 * Akamai Multi Player - renders and scales the video on the stage.
	 */
	public class VideoView extends MovieClip {
		private var _model:Model;
		private var _video:Video;
		private var _innerShadow:InnerShadow;
		private var _background:MovieClip;
		private var _controller:VideoController;
		private var _availableVideoWidth:Number
		private var _availableVideoHeight:Number
		private var _lastVideoWidth:Number;
		private var _lastVideoHeight:Number;
		public function VideoView(model:Model):void {
			_model = model;
			_model.addEventListener(_model.EVENT_RESIZE, resize);
			_controller = new VideoController(_model,this);
			createChildren();
		}
		public function get video():Video {
			return _video;
		}
		public function showVideo():void {
			_video.visible = true;
		}
		public function hideVideo():void {
			_video.visible = false;
		}
		private function createChildren():void {
			_background = new MovieClip();
			addChild(_background);
			_video = new Video(320, 240);
			_video.smoothing = false;
			_video.deblocking = 1;
			_video.x = 3;
			_video.y = 3;
			
			addChild(_video);
			_innerShadow = new InnerShadow();
			_innerShadow.scale9Grid = new Rectangle(5,5,784,584);
			_innerShadow.x = _innerShadow.y = 3;
			addChild(_innerShadow);
			
			_lastVideoWidth = 400;
			_lastVideoHeight = 300;
		}
		public function scaleVideo(width:Number,height:Number):void {
			_lastVideoWidth = width;
			_lastVideoHeight  = height;
				switch (_model.scaleMode) {
					case _model.SCALE_MODE_FIT:
						if (width/height >= _availableVideoWidth/_availableVideoHeight) {
							video.width = _availableVideoWidth;
							video.height = _availableVideoWidth*height/width;
						} else {
							video.width = _availableVideoHeight*width/height;
							video.height = _availableVideoHeight;
						}
						break;
					case _model.SCALE_MODE_STRETCH:
						video.width = _availableVideoWidth;
						video.height = _availableVideoHeight;
						break;
					case _model.SCALE_MODE_NATIVE:
						video.width = width;
						video.height = height;
						break;
					case _model.SCALE_MODE_NATIVE_OR_SMALLER:
						if (width > _availableVideoWidth || height  > _availableVideoHeight) {
							if (width/height >= _availableVideoWidth/_availableVideoHeight) {
								video.width = _availableVideoWidth;
								video.height = _availableVideoWidth*height/width;
							} else {
								video.width = _availableVideoHeight*width/height;
								video.height = _availableVideoHeight;
							}
						} else {
							video.width = width;
							video.height = height;
						}
						break;
				}
				//_video.smoothing = (width != video.width || height != video.height) && (_model.isFullScreen == false);
				//_model.debug("Smoothing = " + _video.smoothing);
				//_video.smoothing  = false;
				video.x = 3 + ((_availableVideoWidth - video.width)/2);
				video.y = 3 + ((_availableVideoHeight- video.height)/2);
				
		}
		private function resize(e:Event):void  {
			//_availableVideoWidth = _model.width - (_model.isOverlay ? 0:(_model.hasPlaylist && _model.playlistVisible)? _model.playlistWidth+6:0) - 6;
			//_availableVideoHeight = _model.height - (_model.isOverlay ? 0:_model.controlbarHeight) - 6;
			_availableVideoWidth = _model.availableVideoWidth;
			_availableVideoHeight = _model.availableVideoHeight;
			_innerShadow.width = _model.availableVideoWidth;
			_innerShadow.height = _model.availableVideoHeight;
			_background.graphics.clear();
			_background.graphics.beginFill(_model.videoBackgroundColor);
			_background.graphics.drawRect(3,3,_model.availableVideoWidth,_model.availableVideoHeight);
			_background.graphics.endFill();
			scaleVideo(_lastVideoWidth, _lastVideoHeight);
		}
	}
}
