package org.openvideoplayer.net
{
	import flash.events.EventDispatcher;
	import flash.events.NetStatusEvent;
	import flash.events.TimerEvent;
	import flash.utils.Timer;
	
	import org.openvideoplayer.events.*;
	
	//-----------------------------------------------------------------
	//
	// Events
	//
	//-----------------------------------------------------------------

	/**
	 * Dispatched when an OVP error condition has occurred. The OvpEvent object contains a data
	 * property.  The contents of the data property will contain the error number and a description.
	 * 
	 * @see org.openvideoplayer.events.OvpError
	 * @see org.openvideoplayer.events.OvpEvent
	 */
	[Event (name="error", type="org.openvideoplayer.events.OvpEvent")]
	
	/**
	 * The OvpCuePointManager class allows you to add cue points with ActionScript
	 * and have them fire at specified times in the stream.
	 * 
	 * This type of cue point is considered an ActionScript cue point, as opposed to
	 * an embedded cue point added at encoding time.
	 */
	public class OvpCuePointManager extends EventDispatcher
	{
		private static const _DEFAULT_INTERVAL_:Number = 100;	// The default interval (in milliseconds) at which 
																// the class will check for cue points
		private var _ns:OvpNetStream;
		private var _aCuePoints:Array;
		private var _tolerance:Number;
		private var _lastFiredCuePointIndex:int;
		private var _intervalTimer:Timer;
		private var _checkInterval:Number;		
		
		//-------------------------------------------------------------------
		// 
		// Constructor
		//
		//-------------------------------------------------------------------

		/**
		 * Constructs a new OvpCuePointManager object, with the OvpNetStream object it will use.
		 * The OvpNetStream object can also be set via the <code>netStream</code> property.
		 * 
		 * @param ns The OvpNetStream object the class should use.
		 */
		public function OvpCuePointManager(ns:OvpNetStream = null)
		{
			this.netStream = ns;
			_aCuePoints = new Array();
			_checkInterval = _DEFAULT_INTERVAL_;
			_intervalTimer = new Timer(_checkInterval);
			_intervalTimer.addEventListener(TimerEvent.TIMER, onIntervalTimer);
			reset();
		}

		//-------------------------------------------------------------------
		//
		// Properties
		//
		//-------------------------------------------------------------------
		
		/**
		 * Set the OvpNetStream object this class will use.
		 */
		public function set netStream(ns:OvpNetStream):void {
			if (!ns || !(ns is OvpNetStream)) {
				dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.STREAM_NOT_DEFINED)));
				return;
			}
			_ns = ns;
			_ns.addEventListener(NetStatusEvent.NET_STATUS, onStreamStatus);
			_tolerance = (_checkInterval*5) / 1000;
			reset();
		}
		
		/** 
		 * Get the sorted, cue point array.
		 * 
		 * Please consider the returned array to be "read-only", modifying this array
		 * directly will lead to unexpected behavior.
		 */
		public function get cuePoints():Array {
			return _aCuePoints;
		}		
		
		//-------------------------------------------------------------------
		//
		// Public methods
		//
		//-------------------------------------------------------------------
		
		/**
		 * Add a single cue point as an object.  The object must contain the following properties:
		 * <ul>
		 * <li> time: Number in seconds.</li>
		 * <li> name: String naming the cue point.</li>
		 * </ul>
		 * 
		 * @see #addCuePoints()
		 * 
		 * @return True if the cue point passed in is valid and was successfully added to the class' 
		 * internal collection of cue points.  Otherwise, returns False.
		 */
		public function addCuePoint(cuePoint:Object):Boolean {
			var _cuePoint:Object = deepCopyObject(cuePoint);
			
			if (!_cuePoint.time || isNaN(_cuePoint.time) || _cuePoint.time < 0) {
				dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.INVALID_CUEPOINT_TIME)));
				return false;
			}
			
			if (!_cuePoint.name) {
				dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.INVALID_CUEPOINT_NAME)));
				return false;
			}
			
			if (!_aCuePoints.length) {
				_aCuePoints.push(_cuePoint);
				return true;
			}
			
			// Find the index where we should insert this cue point
			var index:int = findCuePoint(0, _aCuePoints.length - 1, _cuePoint.time);
			
			// A negative index value means it doesn't exist in the array and the absolute value is the
			// index where it should be inserted.  A positive index means a value exists and in this
			// case we will overwrite the existing cue point rather than insert a duplicate.
			if (index < 0) {
				index *= -1;
				_aCuePoints.splice(index, 0, _cuePoint);
			}
			else {
				_aCuePoints[index] = _cuePoint;
			}
							
			return true;
		}
		
		/**
		 * Add an array of cue points.  Each object in the array must contain the following properties:
		 * <ul>
		 * <li> time: Number in seconds.</li>
		 * <li> name: String naming the cue point.</li>
		 * </ul>
		 * 
		 * @see #addCuePoint()
		 * 
		 * @return True if the array of cue points passed in is valid and each object
		 * in the array was successfully added.  Otherwise, returns False.
		 */
		public function addCuePoints(cuePoints:Array):Boolean {
			if (!cuePoints || !cuePoints.length) {
				dispatchEvent(new OvpEvent(OvpEvent.ERROR, new OvpError(OvpError.INVALID_CUEPOINT)));
				return false;
			} 
			
			var _retVal:Boolean = true;
			
			for each(var cp:Object in cuePoints) {
				_retVal = addCuePoint(cp);
				if (!_retVal) {
					break;
				}
			}
			
			return _retVal;
		}
		
		/**
		 * Removes all cue points.
		 */
		public function removeAllCuePoints():void {
			_aCuePoints.length = 0;
		}
				
		//-------------------------------------------------------------------
		//
		// Private Methods
		//
		//-------------------------------------------------------------------
		
		/**
		 * Perform a reset on the class instance, i.e., after a seek.
		 */
		private function reset():void {
			_lastFiredCuePointIndex = -1;
		}
				
		/**
		 * Returns the index of the cue point object matching the time. If no match is found, returns
		 * the index where the value should be inserted as a negative number.
		 */
		private function findCuePoint(firstIndex:int, lastIndex:int, time:Number):int {
			if (firstIndex <= lastIndex) {
				var mid:int = (firstIndex + lastIndex) / 2;	// divide and conquer
				if (time == _aCuePoints[mid].time) {
					return mid;
				}
				else if (time < _aCuePoints[mid].time) {
					// search the lower part
					return findCuePoint(firstIndex, mid - 1, time);
				}
				else {
					// search the upper part
					return findCuePoint(mid + 1, lastIndex, time);
				}
			}
			return -(firstIndex);
		}
		
		/**
		 * Returns a deep copy of the object passed in.
		 */		
		private static function deepCopyObject(obj:Object, recurseLevel:uint=0):Object {
			if (obj == null) 
				return obj;
			var newObj:Object = new Object();
			
			for (var i:* in obj) {
				if (recurseLevel == 0 && (i == "array" || i == "index")) {
					// skip it
				} else if (typeof obj[i] == "object") {
					newObj[i] = deepCopyObject(obj[i], recurseLevel+1);
				} else {
					newObj[i] = obj[i];
				}
			}
			return newObj;
		}
		
		/**
		 * Checks for cue points around the current play time and dispatches 
		 * an event if a cue point is found.  The event is dispatched by the 
		 * OvpNetStream object.
		 * 
		 */
   		private function checkForCuePoints(e:TimerEvent):void {
   			if (!_ns || _ns.isBuffering || !(_ns is OvpNetStream)) {
   				return;
   			}

			var now:Number = _ns.time;
			
			// Let's find a cue point, we'll start looking one index past the last one we found
			var index:int = findCuePoint(_lastFiredCuePointIndex + 1, _aCuePoints.length - 1, now);
			
			// A negative index value means it doesn't exist in the array and the absolute value is the
			// index where it should be inserted.  Therefore, to get the closest match, we'll look at the index
			// before this one.  A positive index means an exact match was found.
			if (index <= 0) {
				index *= -1;
				index = (index > 0) ? (index - 1) : 0;
			}
			
			// See if the value at this index is within our tolerance
			if (!checkCuePoint(index, now) && ((index + 1) < _aCuePoints.length)) {
				// Look at the next one, see if it is close enough to fire
				checkCuePoint(index+1, now);
			}
   		}
   		
   		/**
   		 * Checks the cue point at the index passed in with the time passed in.
   		 * If the cue point is within the class' tolerance, a cue point event is fired.
   		 * 
   		 * Returns True if a cue point is fired, otherwise False.
   		 */
   		private function checkCuePoint(index:int, now:Number):Boolean { 			
			if ( (_aCuePoints[index].time >= (now - _tolerance)) && (_aCuePoints[index].time <= (now + _tolerance)) && (index != _lastFiredCuePointIndex)) {
				_lastFiredCuePointIndex = index;
				var info:Object = deepCopyObject(_aCuePoints[index]);
				_ns.dispatchEvent(new OvpEvent(OvpEvent.NETSTREAM_CUEPOINT, info));
				
				// Adjust the timer interval if necessary
				var thisTime:Number = _aCuePoints[index].time;
				var nextTime:Number = _aCuePoints[((index + 1) < _aCuePoints.length) ? (index + 1) : (_aCuePoints.length - 1)].time;
				var newDelay:Number = ((nextTime - thisTime)*1000)/4;
				newDelay = (newDelay > _checkInterval) ? newDelay : _checkInterval;
				
				if (newDelay != this._intervalTimer.delay) {
					this._intervalTimer.reset();
					this._intervalTimer.delay = newDelay;
					this._intervalTimer.start();
				}
				return true;
			}
			return false;				
   		}

   		
		//-------------------------------------------------------------------
		//
		// Event Handlers
		//
		//-------------------------------------------------------------------
		
		/**
		 * The interval timer event handler.
		 */
		private function onIntervalTimer(event:TimerEvent):void {
			this.checkForCuePoints(event);
		}

		/**
		 * The stream status handler.
		 */
		private function onStreamStatus(e:NetStatusEvent):void {
			switch (e.info.code) {
				case "NetStream.Play.Start":
				case "NetStream.Buffer.Full":
				case "NetStream.Unpause.Notify":
	   				_intervalTimer.start();
					break;
				case "NetStream.Buffer.Empty":
				case "NetStream.Play.Stop":
				case "NetStream.Pause.Notify":
					_intervalTimer.stop();
					break;
				case "NetStream.Seek.Notify":
					_intervalTimer.stop();
					this.reset();
					break;
			}
		}
	}
}
