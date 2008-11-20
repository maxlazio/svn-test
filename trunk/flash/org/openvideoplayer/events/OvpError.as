// OvpError.as
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

package org.openvideoplayer.events
{
	/**
	 * The OvpError class contains all of the error codes and descriptions for the Open Video Player code base.
	 * 
	 * <p/>
	 * Use this class with the OvpEvent class to dispatch error events, such as:
	 * <p/>
	 * <listing>
	 * dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.STREAM_NOT_FOUND)));
	 * </listing>
	 * 
	 * <p/>
	 * The listener for the error event can use the methods in this class to get the error number and the description, such as:
	 * <p/>
	 * <listing>
	 * private function errorHandler(e:OvpEvent):void {
	 * &#xA0;&#xA0;&#xA0;&#xA0;trace("Error event [" + e.data.errorNumber+ "]: " + e.data.errorDescription);
	 * }
	 * </listing>
	 * 
	 * @see OvpEvent 
	 */
	public class OvpError
	{
		public static const HOSTNAME_EMPTY:uint 			= 1;
		public static const BUFFER_LENGTH:uint 				= 2;
		public static const PROTOCOL_NOT_SUPPORTED:uint 	= 3;
		public static const PORT_NOT_SUPPORTED:uint 		= 4;
		public static const IDENT_REQUEST_FAILED:uint 		= 5;
		public static const CONNECTION_TIMEOUT:uint 		= 6;
		public static const STREAM_NOT_DEFINED:uint			= 8;
		public static const STREAM_NOT_FOUND:uint			= 9;
		public static const STREAM_LENGTH_REQ_ERROR:uint	= 10;
		public static const VOLUME_OUT_OF_RANGE:uint		= 11;
		public static const NETWORK_FAILED:uint				= 12;
		public static const CONNECTION_REJECTED:uint		= 13;
		public static const HTTP_LOAD_FAILED:uint			= 14;
		public static const XML_MALFORMED:uint				= 15;
		public static const XML_MEDIARSS_MALFORMED:uint		= 16;
		public static const CLASS_BUSY:uint					= 17;
		public static const XML_BOSS_MALFORMED:uint			= 18;
		public static const STREAM_FASTSTART_INVALID:uint	= 19;
		public static const XML_LOAD_TIMEOUT:uint			= 20;
		public static const STREAM_IO_ERROR:uint			= 21;
		public static const STREAM_BUFFER_EMPTY:uint		= 24;
		
		private static const _errorMap:Array = [
			{n:HOSTNAME_EMPTY, 			d:"Hostname cannot be empty"}, 
			{n:BUFFER_LENGTH, 			d:"Buffer length must be > 0.1"},
			{n:PROTOCOL_NOT_SUPPORTED, 	d:"Warning - this protocol is not supported"},
			{n:PORT_NOT_SUPPORTED, 		d:"Warning - this port is not supported"},
			{n:IDENT_REQUEST_FAILED, 	d:"Warning - unable to load XML data from ident request, will use domain name to connect"},
			{n:CONNECTION_TIMEOUT, 		d:"Timed out while trying to connect"},
			{n:STREAM_NOT_DEFINED, 		d:"Cannot play, pause, seek, or resume since the stream is not defined"},
			{n:STREAM_NOT_FOUND, 		d:"Timed out trying to find the stream"},
			{n:STREAM_LENGTH_REQ_ERROR,	d:"Error requesting stream length"},
			{n:VOLUME_OUT_OF_RANGE, 	d:"Volume value out of range"},
			{n:NETWORK_FAILED, 			d:"Network failure - unable to play the live stream"},
			{n:CONNECTION_REJECTED,		d:"Connection attempt rejected by server"},
			{n:HTTP_LOAD_FAILED,		d:"HTTP loading operation failed"},
			{n:XML_MALFORMED,			d:"XML is not well formed"},
			{n:XML_MEDIARSS_MALFORMED,	d:"XML does not conform to Media RSS standard"},
			{n:CLASS_BUSY,				d:"Class is busy and cannot process your request"},
			{n:XML_BOSS_MALFORMED,		d:"XML does not conform to BOSS standard"},
			{n:STREAM_FASTSTART_INVALID,d:"The Fast Start feature cannot be used with live streams"},
			{n:XML_LOAD_TIMEOUT,		d:"Timed out trying to load the XML file"},
			{n:STREAM_IO_ERROR,			d:"NetStream IO Error event"},
			{n:STREAM_BUFFER_EMPTY,		d:"NetStream buffer has remained empty past timeout threshold"} ];
			
		private var _num:uint;
		private var _desc:String;
		
		/**
		 * Constructor
		 */
		public function OvpError(errorCode:uint)
		{
			_num = errorCode;
			_desc = "";
			
			for (var i:uint = 0; i < _errorMap.length; i++) {
				if (_errorMap[i].n == _num) {
					_desc = _errorMap[i].d;
					break;
				}
			}
		}
		
		/**
		 * The error number for the error dispatched.
		 */
		public function get errorNumber():uint { return _num; }
		
		/**
		 * The error description for the error dispatched.
		 */
		public function get errorDescription():String { return _desc; }
	}
}
