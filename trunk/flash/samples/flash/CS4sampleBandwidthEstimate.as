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

// This sample file demonstrates usage of the HTTPBandwidthEstimate utility class
// for estimating bandwidth based on a HTTP download.

package {
	// AS3 generic imports
	import flash.display.MovieClip;
	import flash.events.*
	
	// CS4 specific imports
	import fl.controls.Button;
	import fl.controls.TextArea;
	
	// OVP specific imports
	import org.openvideoplayer.utilities.HTTPBandwidthEstimate;

	public class CS4sampleBandwidthEstimate extends MovieClip {
		
		// Declare vars
		private var _button:Button;
		private var _output:TextArea;
		private var _bw:HTTPBandwidthEstimate;

		// Constructor
		public function CS4sampleBandwidthEstimate():void {
			initChildren();
		}
		// Initialize the children on the stage
		private function initChildren():void {
			_button = new Button();
			_button.width = 200;
			_button.move(20,20);
			_button.label = "Begin bandwidth estimate";
			_button.addEventListener(MouseEvent.CLICK,doBegin);
			addChild(_button);
			_output  = new TextArea();
			_output.move(20,50);
			_output.setSize(300,300);
			addChild(_output);
			_bw = new HTTPBandwidthEstimate();
			_bw.addEventListener("complete",showBandwidth);
			_bw.addEventListener(ErrorEvent.ERROR,showError);
		}
		// Begin a new estimate
		private function doBegin(e:MouseEvent){
			_output.text += "Measurement started ...\n";
			_bw.start("http://products.edgeboss.net/download/products/jsherry/testfiles/stream001.flv");
		}
		// Display the result
		private function showBandwidth(e:Event):void {
			_output.text += "Bandwidth estimated at " + HTTPBandwidthEstimate(e.target).bandwidth + " kbps\n";
		}
		// Display the error
		private function showError(e:ErrorEvent):void {
			_output.text += e.text + "\n";
		}
	}
}
