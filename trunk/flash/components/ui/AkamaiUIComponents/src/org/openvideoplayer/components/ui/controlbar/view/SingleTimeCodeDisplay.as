//
// Copyright (c) 2009-2011, the Open Video Player authors. All rights reserved.
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
package org.openvideoplayer.components.ui.controlbar.view
{
	import org.openvideoplayer.components.ui.shared.ControlType;
	import org.openvideoplayer.components.ui.controlbar.utils.LayoutUtil;
	import org.openvideoplayer.components.ui.shared.view.base.BaseLabel;

	/**
	 * @author Akamai Technologies, Inc 2011
	 */
	public class SingleTimeCodeDisplay extends BaseLabel
	{
		
		/**
		 * @Constructor 
		 */		
		public function SingleTimeCodeDisplay()
		{
			super(ControlType.SINGLE_TIMECODE_DISPLAY, 35);
			setFormatedText("00:00");
		}
		
		/**
		 * You can pass in a formated string to set to the text display. 
		 * An exmaple is the string "LIVE" for live streaming events
		 * instead of a time code.
		 * 
		 * @param value
		 */		
		public function setFormatedText(value:String):void 
		{
			textfield.text = value;
		}
		
		private function formatText(currentTime:Number):String 
		{
			var ctr:int = LayoutUtil.roundNumberDown(currentTime);
			return  (ctr < 10) ? "00:0"+ctr : "00:"+ctr;
		}
	}
}