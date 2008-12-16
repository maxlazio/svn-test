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
	
 	/**
	 * Dispatched when the class receives descriptive information embedded in the
	 * video file being played.
	 * 
	 * @see #play()
	 */
	[Event (name="metadata", type="org.openvideoplayer.events.OvpEvent")]
 	/**
	 * Dispatched when an embedded cue point is reached while playing a video.
	 * 
	 * @see #play()
	 */
	[Event (name="cuepoint", type="org.openvideoplayer.events.OvpEvent")]
 	/**
	 * Dispatched when the class receives an image embedded in a H.264 file.
	 * 
	 */
	[Event (name="imagedata", type="org.openvideoplayer.events.OvpEvent")]
 	/**
	 * Dispatched when the class receives text data embedded in a H.264 file.
	 * 
	 */
	[Event (name="textdata", type="org.openvideoplayer.events.OvpEvent")]
 	/**
	 * Dispatched when the class has completely played a stream or switches to a different stream in a server-side playlist.
	 * 
	 * @see #play
	 */
	[Event (name="playstatus", type="org.openvideoplayer.events.OvpEvent")]
	/**
	 * Dispatched when an OVP error condition has occurred. The OvpEvent object contains a data
	 * property.  The contents of the data property will contain the error number and a description.
	 * 
	 * @see org.openvideoplayer.events.OvpError
	 * @see org.openvideoplayer.events.OvpEvent
	 */
	[Event (name="error", type="org.openvideoplayer.events.OvpEvent")]
 	/**
	 * Dispatched when the class has detected, by analyzing the
	 * NetStream.netStatus events, the end of the stream. <p>
	 * Deprecated in favor of <code>OvpEvent.COMPLETE</code>
	 * when communicating with a FMS server. For progressive delivery
	 * of streams however, this event is the only reliable indication provided that the stream
	 * has ended. 
	 * 
	 * @see org.openvideoplayer.events.OvpEvent#COMPLETE
	 */
	[Event (name="end", type="org.openvideoplayer.events.OvpEvent")]
 	/**
	 * Dispatched repeatedly at the <code>progressInterval</code> once the class begins playing a stream. 
	 * Event is halted after <code>close</code> is called. 
	 * 
	 * @see #progressInterval
	 * @see #close
	 */
	[Event (name="progress", type="org.openvideoplayer.events.OvpEvent")]
	/**
	 * Dispatched when a stream from a server is complete. Note that when connecting to progressive
	 * content this event will not be dispatched. The <code>OvpEvent.END_OF_STREAM</code>
	 * should be used instead, in order to detect when the stream has finished playing. 
	 * 
	 * @see org.openvideoplayer.events.OvpEvent#END_OF_STREAM
	 */
	[Event (name="complete", type="org.openvideoplayer.events.OvpEvent")]
 	/**
	 * Dispatched when the class receives information about ID3 data embedded in an MP3 file.
	 * 
	 * @see #getMp3Id3Info()
	 */
	[Event (name="id3", type="org.openvideoplayer.events.OvpEvent")]
 	/**
	 * Dispatched if metadata matching the name "duration" is received
	 * while playing a progressive stream.
	 */
	[Event (name="streamlength", type="org.openvideoplayer.events.OvpEvent")]
	
	/**
	 * The OvpNetStream class extends flash.net.NetStream to provide unique features such as a 
	 * fast start (dual buffer) for streams, and metadata events.
	 */
	public class OvpNetStream extends NetStream
	{
		// Declare protected vars
		/**
		 * @private
		 */
		protected var _progressTimer:Timer;
		/**
		 * @private
		 */
		protected var _streamTimer:Timer;
		/**
		 * @private
		 */
		protected var _isProgressive:Boolean;
		/**
		 * @private
		 */
		protected var _maxBufferLength:uint;
		/**
		 * @private
		 */
		protected var _useFastStartBuffer:Boolean;
		/**
		 * @private
		 */
		protected var _aboutToStop:uint;
		/**
		 * @private
		 */
		protected var _isBuffering:Boolean;
		/**
		 * @private
		 */
		protected var _bufferFailureTimer:Timer;
		/**
		 * @private
		 */
		protected var _watchForBufferFailure:Boolean;
		/**
		 * @private
		 */
		protected var _nc:NetConnection;
		/**
		 * @private
		 */
		protected var _nsId3:OvpNetStream;
		/**
		 * @private
		 */
		protected var _volume:Number;
		/**
		 * @private
		 */
		protected var _panning:Number;
		/**
		 * @private
		 */
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
		 * @param connection This object can be either an OvpConnection object or a 
		 * flash.net.NetConnection object. If an OvpConnection
		 * object is provided, the constructor will use the 
		 * flash.net.NetConnection object within it.
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
			maxBufferLength = 3
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
		
		/**
		 * Informs whether the current connection is progressive or not.
		 * 
		 * @return true if the conenction is progressive or false if not. 
		 * 
		 * @see OvpConnection#connect()
		 */
		public function get isProgressive():Boolean {
			return _isProgressive;
		}
		
		/**
		 * The interval in milliseconds at which the <code>OvpEvent.PROGRESS</code> event is dispatched.
		 * This event commences with the first <code>play()</code> request and continues until <code>close()</code>
		 * is called. 
		 * 
		 * @default 100
		 * 
		 * @see #play()
		 * @see #close()
		 */
		public function get progressInterval():Number {
			return _progressTimer.delay;
		}
		public function set progressInterval(delay:Number):void {
			_progressTimer.delay = delay;
		}

		/**
		 * The desired buffer length set for the NetStream, in seconds. If <code>useFastStartBuffer</code> has
		 * been set to false (the default), then this value will be used to set the constant buffer value on the NetStream. If
		 * <code>useFastStartBuffer</code> has been set to true, then the NetStream buffer will alternate between 0.5
		 * (after a NetStream.Play.Start event) and the value set by this property.
		 * 
		 * @default 3
		 * 
		 * @see #useFastStartBuffer
		 * @see flash.net.NetStream#bufferTime
		 * @see flash.net.NetStream#bufferLength
		 * 
		 */
		public function get maxBufferLength():Number {
			return _maxBufferLength;
		}
		public function set maxBufferLength(length:Number):void {
			if (length < 0.1)
				dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.BUFFER_LENGTH))); 
			else {
				_maxBufferLength = length;
				this.bufferTime = _maxBufferLength;
			}	
		}
		
		/**
		 * Dictates whether a fast start (dual buffer) strategy should be used. A fast start buffer means that the
		 * NetStream buffer is set to value of 0.5 seconds after a NetStream.Play.Start or NetStream.Buffer.Empty event
		 * and then to <code>maxBufferLength</code> after the <code>NetStream.Buffer.Full</code> event is received. 
		 * This gives the advantages of a fast stream start combined with a robust buffer for long-term bandwidth. 
		 * Users whose connections are close to the bitrate of the stream may see very rapid stuttering of the stream with
		 * this approach, so it is best deployed in situations in which each users' bandwidth is several multiples
		 * of the streaming files' bitrate.<p />
		 * 
		 * Note that fast start cannot be used with LIVE STREAMS.
		 * 
		 * @see #maxBufferLength
		 */
		public function get useFastStartBuffer():Boolean {
			return _useFastStartBuffer;
		}	
		public function set useFastStartBuffer(value:Boolean):void {
			_useFastStartBuffer = value;
			if (!value)
				this.bufferTime = _maxBufferLength;
		}
		
		/**
		 * Returns the buffering status of the stream. This value will be <code>true</code> after NetStream.Play.Start
		 * and before NetStream.Buffer.Full or NetStream.Buffer.Flush and <code>false</code> at all other times. 
		 */
		public function get isBuffering():Boolean {
			return _isBuffering;
		}
		
		/**
		 * The buffer timeout value in millseconds. If, during playback, the buffer empties
		 * and does not fill again before the buffer timeout interval passes, then <code>OvpError.STREAM_BUFFER_EMPTY</code>
		 * is dispatched. This value and error are designed to trap very rare network abnormalities in which the server never
		 * fills the buffer, nor sends a close event, thereby leaving the client in a hung state. As a developer,
		 * if you receive this error, you should reestablish the connection.<p/>
		 * Note - this event is only fired for on-demand streaming content, not for live streaming
		 * or progressively delivered content. 
		 * 
		 * @default 20,000
		 */
		public function get bufferTimeout():Number {
			return _bufferFailureTimer.delay;
		}
		public function set bufferTimeout(value:Number):void {
			_bufferFailureTimer.delay = value;
		}
		
		/**
		 * The buffer percentage currently reported by the stream. This property
		 * will always have an integer value between 0 and 100. The max value is 
		 * capped at 100 even if the bufferLength exceeds the bufferTime.
		 * 
		 * @see flash.net.NetStream#bufferTime
		 * @see #maxBufferLength
		 */
		public function get bufferPercentage():Number {
			return Math.min(100,(Math.round(bufferLength*100/bufferTime)));
		}
		
		/**
		 * Returns the current time of the stream, as timecode HH:MM:SS. This property will only return a valid 
		 * value if the NetStream has been established.
		 */
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
		 * Initiates the process of extracting the ID3 information from an MP3 file. 
		 * Since this process is asynchronous, the actual ID3 metadata is retrieved 
		 * by listening for the OvpEvent.MP3_ID3 and inspecting the <code>info</code> parameter.
		 * 
		 * @return false if the NetConnection has not yet been defined, otherwise true. 
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
		
		/**
		 * Begins playing content if it exists. This method supports both streaming
		 * and progressive playback.
		 *
		 * @see flash.net.NetStream#play() 
		 * @param the name of the stream to play. 
		 */
		public override function play(... arguments):void {		
			super.play.apply(this, arguments);
			if (!_progressTimer.running)
				_progressTimer.start();
			if (!_streamTimer.running)
				_streamTimer.start();
		}

		/** 
		 * Closes the stream.
		 * 
		 * @see flash.net.NetStream#close()
		 */
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

		/**
		 * @private
		 */
		protected function handleEnd():void {
			dispatchEvent(new OvpEvent(OvpEvent.END_OF_STREAM));
		}
				
		//-------------------------------------------------------------------
		//
		// Event Handlers
		//
		//-------------------------------------------------------------------
		
		/**
		 * @private
		 */
		protected function updateProgress(e:TimerEvent):void {
			dispatchEvent(new OvpEvent(OvpEvent.PROGRESS)); 
		}
		
		/**
		 * @private
		 */
		protected function connectionStatus(event:NetStatusEvent):void {
			switch (event.info.code) {				
				case "NetConnection.Connect.Closed":
					close();
    				break;
			}
		}
		
		/**
		 * @private
		 */
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
		
		/**
		 * @private
		 */
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
    	
		/**
		 * @private
		 */
		public function onPlayStatus(info:Object):void {
        	dispatchEvent(new OvpEvent(OvpEvent.NETSTREAM_PLAYSTATUS,info));
        	if (info.code == "NetStream.Play.Complete") {
        		dispatchEvent(new OvpEvent(OvpEvent.COMPLETE));
        		_bufferFailureTimer.reset();
        		handleEnd();
        	}
    	}
    	
		/**
		 * @private
		 */
		protected function handleBufferFailure(e:TimerEvent):void {
			if (!_isProgressive) {
				dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.STREAM_BUFFER_EMPTY)));
			}
		}
    	
		/**
		 * @private
		 */
		protected function onId3(info:Object):void {
			dispatchEvent(new OvpEvent(OvpEvent.MP3_ID3, info));
		}
		
		/**
		 * @private
		 */
		protected function streamTimeoutHandler(e:TimerEvent):void {
			dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.STREAM_NOT_FOUND)));
		}							    	  			    	
	}
}
