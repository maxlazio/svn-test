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
	import org.openvideoplayer.components.ui.shared.utils.ShapeFactory;
	
	import flash.display.Shape;
	import flash.display.Sprite;
	import flash.geom.Matrix;
	import org.openvideoplayer.components.ui.shared.view.base.BaseComponent;

	[ExcludeClass]
	
	/**
	 * @author Akamai Technologies, Inc 2011
	 */
	public class BufferingBar extends BaseComponent
	{
		private var bufferBarHeight:uint = 10;	
		private var containerA:Sprite = new Sprite()
		private var containerB:Sprite = new Sprite()
			
		/**
		 * not tested yet 
		 */		
		public static const BARBER_POLL_BUFFERING_BAR:String = "barberPollBufferingBar";
		
		/**
		 * @Constructor 
		 */		
		public function BufferingBar()
		{
			super(BufferingBar.BARBER_POLL_BUFFERING_BAR);
			createBufferStrip()
			createMask()
		}
		
		private function createMask():void
		{
			var maskShape:Shape = ShapeFactory.getRectShape(0xFF0000, 1);
			maskShape.width = controlBarPropertyModel.scrubBarWidth;
			maskShape.height = bufferBarHeight;
			addChild(maskShape);
			this.mask = maskShape;
		}
		
		private function createBufferStrip():void
		{
			var total:int = controlBarPropertyModel.controlBarWidth /30
			for (var i:int = 0; i < total; i++)
			{
				var segment:Shape = getSegment();
				segment.x = (segment.width+4)*i;
				containerA.addChild(segment);
			}
			addChild(containerA)
		}
		
		private function getSegment():Shape
		{
			var s:Shape = ShapeFactory.getRectShape(0xFF0000, 1);
			s.width = bufferBarHeight;
			s.height = bufferBarHeight;
			var matrix:Matrix = s.transform.matrix
			matrix.c = -7
			s.transform.matrix = matrix			
			return s;
		}
	}
}