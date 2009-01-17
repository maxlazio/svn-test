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

package 
{
	import flash.display.*;
	import flash.events.Event;
	import flash.events.FullScreenEvent;
	import flash.system.Capabilities;
	import flash.geom.Rectangle;
	import org.openvideoplayer.players.akamai.multi.AkamaiMultiPlayer;

	
	/**
	 * This example illustrates the invocation of the Akamai Multi Player in a Flash CS4 project. Note that control
	 * of the fullscreen behavior is externalized, since in many instances the player component itself may not be the only display object
	 * on stage and therefore other layout and positioning methods may have to be called when moving to fullscreen.
	 * 
	 * <p/>
	 * Due to Dynamic Streaming Support, this project must be compiled for Flash Player 10 or higher. 
	 * 
	 * @see org.openvideoplayer.players.akamai.multi.AkamaiMultiPlayer
	 */
	public class AkamaiMultiPlayerExample extends MovieClip 
	{
		
		private var player:AkamaiMultiPlayer;
		
		public function AkamaiMultiPlayerExample():void {
			stage.scaleMode = StageScaleMode.NO_SCALE;
			stage.align = StageAlign.TOP_LEFT;
			stage.addEventListener(Event.RESIZE, resize);
			stage.addEventListener(FullScreenEvent.FULL_SCREEN, exitFullScreen);
			player  = new AkamaiMultiPlayer(950, 400,loaderInfo.parameters);
			player.setNewSource("http://rss.streamos.com/streamos/rss/genfeed.php?feedid=1674&groupname=products");
			player.addEventListener("toggleFullscreen", handleFullscreen);
			addChild(player);
			
		}
		private function resize(e:Event):void {
			player.resizeTo(stage.stageWidth, stage.stageHeight);
		}
		private function handleFullscreen(e:Event):void {
			if (stage.displayState == StageDisplayState.NORMAL) {
				try {
					player.resizeTo(Capabilities.screenResolutionX,Capabilities.screenResolutionY);
					stage.fullScreenSourceRect = new Rectangle(0, 0, Capabilities.screenResolutionX,Capabilities.screenResolutionY);
					stage.displayState = StageDisplayState.FULL_SCREEN;
					stage.addEventListener(FullScreenEvent.FULL_SCREEN, exitFullScreen);
					
				} 
				catch (e:SecurityError) {
					// Fullscreen not available.
				}
			} else {
				stage.displayState = StageDisplayState.NORMAL;
				exitFullScreen(null);
			}
		}
		private function exitFullScreen(e:FullScreenEvent):void
		{
			if (stage.displayState == StageDisplayState.NORMAL)
			{
				stage.fullScreenSourceRect = null;
				player.resizeTo(950,400);
			}
		}
	}
	
}