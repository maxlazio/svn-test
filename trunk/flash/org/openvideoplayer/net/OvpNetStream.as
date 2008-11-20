// OvpNetStream.as
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

package org.openvideoplayer.net
{
	import flash.events.*;
	import flash.media.SoundTransform;
	import flash.net.NetConnection;
	import flash.net.NetStream;
	import flash.utils.Timer;
	
	import org.openvideoplayer.events.OvpError;
	import org.openvideoplayer.events.OvpEvent;
	import org.openvideoplayer.utilities.TimeUtil;
	
	//-----------------------------------------------------------------
	//
	// Events
	//
	//-----------------------------------------------------------------
	
	[Event (name="metadata", type="org.openvideoplayer.events.OvpEvent")]
	[Event (name="cuepoint", type="org.openvideoplayer.events.OvpEvent")]
	[Event (name="imagedata", type="org.openvideoplayer.events.OvpEvent")]
	[Event (name="textdata", type="org.openvideoplayer.events.OvpEvent")]
	[Event (name="playstatus", type="org.openvideoplayer.events.OvpEvent")]
	[Event (name="error", type="org.openvideoplayer.events.OvpEvent")]
	[Event (name="end", type="org.openvideoplayer.events.OvpEvent")]
	[Event (name="progress", type="org.openvideoplayer.events.OvpEvent")]
	[Event (name="complete", type="org.openvideoplayer.events.OvpEvent")]
	[Event (name="id3", type="org.openvideoplayer.events.OvpEvent")]
	[Event (name="streamlength", type="org.openvideoplayer.events.OvpEvent")]
	

	public class OvpNetStream extends NetStream
	{
		// Declare private/protected vars
		protected var _progressTimer:Timer;
		protected var _streamTimer:Timer;
		protected var _isProgressive:Boolean;
		protected var _maxBufferLength:uint;
		protected var _useFastStartBuffer:Boolean;
		protected var _aboutToStop:uint;
		protected var _isBuffering:Boolean;
		protected var _bufferFailureTimer:Timer;
		protected var _watchForBufferFailure:Boolean;
		protected var _nc:NetConnection;
		protected var _nsId3:OvpNetStream;
		protected var _volume:Number;
		protected var _panning:Number;
		protected var _streamTimeout:uint;
		
		// Declare private constants
		private const DEFAULT_PROGRESS_INTERVAL:Number = 100;
		private const BUFFER_FAILURE_INTERVAL:Number = 20000;
		private const DEFAULT_STREAM_TIMEOUT:Number = 5000;

		
		//-------------------------------------------------------------------
		// 
		// Constructor
		//
		//-------------------------------------------------------------------

		/**
		 * Constructor
		 * 
		 * @param This object can be either an OvpConnection object or a NetConnection object. If an OvpConnection
		 * object is provided, the constructor will use the NetConnection object within it.
		 */
		public function OvpNetStream(connection:Object)
		{
			var _connection:NetConnection = null;
			
			if (connection is NetConnection)
				_connection = NetConnection(connection);
			else if (connection is OvpConnection)
				_connection = NetConnection(connection.netConnection);
				
			super(_connection);
			
			_isProgressive = (_connection.uri == null || _connection.uri == "null") ? true : false;
			_nc = _connection;
			_maxBufferLength = 3
			_useFastStartBuffer = false;
			_aboutToStop = 0;
			_isBuffering = false;
			_watchForBufferFailure = false;
			_volume = 1;
			_panning = 0;
			
			// So we know when the connection closes
			_nc.addEventListener(NetStatusEvent.NET_STATUS, connectionStatus);
			
			addEventListener(NetStatusEvent.NET_STATUS, streamStatus);
			
			_progressTimer = new Timer(DEFAULT_PROGRESS_INTERVAL);
			_progressTimer.addEventListener(TimerEvent.TIMER, updateProgress);
			_bufferFailureTimer = new Timer(BUFFER_FAILURE_INTERVAL,1);
			_bufferFailureTimer.addEventListener(TimerEvent.TIMER_COMPLETE, handleBufferFailure);
			_streamTimer = new Timer(DEFAULT_STREAM_TIMEOUT);
			_streamTimer.addEventListener(TimerEvent.TIMER_COMPLETE, streamTimeoutHandler);
		}
		
		
		//-------------------------------------------------------------------
		//
		// Properties
		//
		//-------------------------------------------------------------------
		
		public function get isProgressive():Boolean {
			return _isProgressive;
		}
		
		public function get progressInterval():Number {
			return _progressTimer.delay;
		}
		
		public function set progressInterval(delay:Number):void {
			_progressTimer.delay = delay;
		}

		public function get maxBufferLength():Number {
			return _maxBufferLength;
		}
		
		public function set maxBufferLength(length:Number):void {
			if (length < 0.1)
				dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.BUFFER_LENGTH))); 
			else
				_maxBufferLength = length;
		}
		
		public function get useFastStartBuffer():Boolean {
			return _useFastStartBuffer;
		}
		
		public function set useFastStartBuffer(value:Boolean):void {
			_useFastStartBuffer = value;
			if (!value)
				this.bufferTime = _maxBufferLength;
		}
		
		public function get isBuffering():Boolean {
			return _isBuffering;
		}
		
		public function get bufferTimeout():Number {
			return _bufferFailureTimer.delay;
		}
		
		public function set bufferTimeout(value:Number):void {
			_bufferFailureTimer.delay = value;
		}
		
		public function get bufferPercentage():Number {
			return Math.min(100,(Math.round(bufferLength*100/bufferTime)));
		}
		
		public function get timeAsTimeCode():String {
			return TimeUtil.timeCode(this.time);
		}
		
		/**
		 * The volume of the NetStream. Possible volume values lie between 0 (silent) and 1 (full volume).
		 * 
		 * @default 1
		 */
		public function get volume():Number{
			return _volume;
		}
		
		/**
		 * @private
		 */
		public function set volume(vol:Number):void {
			if (vol < 0 || vol > 1) {
				dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.VOLUME_OUT_OF_RANGE)));
				return;
			} 
		
			_volume = vol;
			soundTransform = (new SoundTransform(_volume,_panning));
		}
		/**
		 * The panning of the current NetStream. Possible volume values lie between -1 (full left) to 1 (full right).
		 * 
		 * @default 0
		 */
		public function get panning():Number{
			return _panning;
		}
		/**
		 * @private
		 */
		public function set panning(panning:Number):void {
			if (panning < -1 || panning > 1) {
				dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.VOLUME_OUT_OF_RANGE)));
				return;
			} 
			_panning = panning;
			soundTransform = (new SoundTransform(_volume,_panning));
		}		
		
		/**
		 * The maximum number of seconds the class should wait before timing out while trying to locate a stream
		 * on the network. This time begins decrementing the moment a <code>play</code> request is made. 
		 * After this master time out has been triggered, the class will issue
		 * an Error event OvpError.STREAM_NOT_FOUND.
		 * 
		 * @default 3600
		 */
		public function get streamTimeout():Number {
			return _streamTimeout/1000;
		}
		/**
		 * @private
		 */
		public function set streamTimeout(numOfSeconds:Number):void {
			_streamTimeout = numOfSeconds*1000;
			_streamTimer.delay = _streamTimeout;
		}
		
		//-------------------------------------------------------------------
		//
		// Public methods
		//
		//-------------------------------------------------------------------
		
		/**
		 * Initiates the process of extracting the ID3 information from an MP3 file. Since this process is asynchronous,
		 * the actual ID3 metadata is retrieved by listening for the OvpEvent.MP3_ID3 and inspecting the <code>info</code> parameter.
		 * 
		 * @return false if the NetConnection has not yet defined, otherwise true. 
		 */
		public function getMp3Id3Info(filename:String):Boolean {
			if (!_nc || !_nc.connected)
				return false;

			if (!(_nsId3 is OvpNetStream)) {
				_nsId3 = new OvpNetStream(_nc);
				_nsId3.addEventListener(Event.ID3,onId3);
    		}
			if (filename.slice(0, 4) == "mp3:" || filename.slice(0, 4) == "id3:") {
				filename = filename.slice(4);
			}
			_nsId3.play("id3:"+filename);
			return true;
		}
		
		public override function play(... arguments):void {
			super.play.apply(this, arguments);
			if (!_progressTimer.running)
				_progressTimer.start();
			if (!_streamTimer.running)
				_streamTimer.start();
		}

		public override function close():void {
			_progressTimer.stop();
			_streamTimer.stop();
			super.close();	
		}		
		
		//-------------------------------------------------------------------
		//
		// Private Methods
		//
		//-------------------------------------------------------------------

		protected function handleEnd():void {
			dispatchEvent(new OvpEvent(OvpEvent.END_OF_STREAM));
		}
				
		//-------------------------------------------------------------------
		//
		// Event Handlers
		//
		//-------------------------------------------------------------------
		
		protected function updateProgress(e:TimerEvent):void {
			dispatchEvent(new OvpEvent(OvpEvent.PROGRESS)); 
		}
		
		protected function connectionStatus(event:NetStatusEvent):void {
			switch (event.info.code) {				
				case "NetConnection.Connect.Closed":
					close();
    				break;
			}
		}
		
		protected function streamStatus(event:NetStatusEvent):void {
			
			if (_useFastStartBuffer) {
				if (event.info.code == "NetStream.Play.Start" || event.info.code == "NetStream.Buffer.Empty") 
					this.bufferTime = 0.5;
				
				if (event.info.code == "NetStream.Buffer.Full") 
					this.bufferTime = _maxBufferLength;
			}
			
			switch(event.info.code) {
				
				case "NetStream.Play.Start":
					_aboutToStop = 0;
					_isBuffering = true;
					_watchForBufferFailure  = true;
					_streamTimer.stop();
					break;
					
				case "NetStream.Play.Stop":
					if (_aboutToStop == 2) {
						_aboutToStop = 0;
						handleEnd();
					} 
					else 
						_aboutToStop = 1
					
					_watchForBufferFailure  = false;
					_bufferFailureTimer.reset();
					break;
					
				case "NetStream.Buffer.Empty":
					if (_aboutToStop == 1) {
						_aboutToStop = 0;
						handleEnd();
					} 
					else 
						_aboutToStop = 2
					
					if (_watchForBufferFailure) 
						_bufferFailureTimer.start();
					break;
					
				case "NetStream.Buffer.Full":
					_isBuffering = false;
					_bufferFailureTimer.reset();
					break;
										
				case "NetStream.Buffer.Flush":
					_isBuffering = false;
					break;				
			}
		}
		
		public function onMetaData(info:Object):void {
			dispatchEvent(new OvpEvent(OvpEvent.NETSTREAM_METADATA, info));
			if (_isProgressive && !isNaN(info["duration"])) {
				var data:Object = new Object();
				data.streamLength = Number(info["duration"]);;
				dispatchEvent(new OvpEvent(OvpEvent.STREAM_LENGTH, data));
			}
		}
		
   		/** Catches netstream onImageData events  - only relevent when playing H.264 content
    	 * @private
    	 */
		public function onImageData(info:Object):void {
        	dispatchEvent(new OvpEvent(OvpEvent.NETSTREAM_IMAGEDATA,info));
    	}
    	/** Catches netstream onTextData events  - only relevent when playng H.264 content
    	 * @private
    	 */
		public function onTextData(info:Object):void {
        	dispatchEvent(new OvpEvent(OvpEvent.NETSTREAM_TEXTDATA,info));
    	}
    	/** Catches netstream cuepoint events
    	 * @private
    	 */
		public function onCuePoint(info:Object):void {
        	dispatchEvent(new OvpEvent(OvpEvent.NETSTREAM_CUEPOINT,info));
    	}
    	
		public function onPlayStatus(info:Object):void {
        	dispatchEvent(new OvpEvent(OvpEvent.NETSTREAM_PLAYSTATUS,info));
        	if (info.code == "NetStream.Play.Complete") {
        		dispatchEvent(new OvpEvent(OvpEvent.COMPLETE));
        		_bufferFailureTimer.reset();
        		handleEnd();
        	}
    	}
    	
		protected function handleBufferFailure(e:TimerEvent):void {
			if (!_isProgressive) {
				dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.STREAM_BUFFER_EMPTY)));
			}
		}
    	
		protected function onId3(info:Object):void {
			dispatchEvent(new OvpEvent(OvpEvent.MP3_ID3, info));
		}
		
		protected function streamTimeoutHandler(e:TimerEvent):void {
			dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.STREAM_NOT_FOUND)));
		}							    	  			    	
	}
}
