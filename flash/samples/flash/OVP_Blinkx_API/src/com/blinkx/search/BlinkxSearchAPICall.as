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

package com.blinkx.search
{
	import org.openvideoplayer.events.OvpEvent;
	import org.openvideoplayer.parsers.ParserBase;
	import org.openvideoplayer.rss.ContentTO;
	import org.openvideoplayer.rss.EnclosureTO;
	import org.openvideoplayer.rss.ItemTO;
	import org.openvideoplayer.rss.Media;
	import org.openvideoplayer.rss.ThumbnailTO;
	
	/**
	 * Dispatched when an error condition has occurred. The event provides an error number and a verbose description
	 * of each error.  The error object contained within the OvpEvent is a BlinkxError object.
	 * @see org.openvideoplayer.events.OvpEvent#ERROR
	 */
 	[Event (name="error", type="org.openvideoplayer.events.OvpEvent")]
	/**
	 * Dispatched when the Blinkx API call response has been successfully loaded and parsed. 
	 * 
	 * @see org.openvideoplayer.events.OvpEvent#LOADED
	 */
 	[Event (name="loaded", type="org.openvideoplayer.events.OvpEvent")]
 	/**
	 * Dispatched when the Blinkx API call response times out. 
	 * 
	 * @see org.openvideoplayer.events.OvpEvent#TIMEOUT
	 */
 	[Event (name="timeout", type="org.openvideoplayer.events.OvpEvent")]
 	
	/**
	 * Dispatched when the Blinkx API call response times out. 
	 * 
	 * @see org.openvideoplayer.events.OvpEvent#PARSED
	 */
 	[Event (name="parsed", type="org.openvideoplayer.events.OvpEvent")]
	
 	/**
	 * The BlinkxSearchAPICall class loads and parses XML api calls to the Blinkx Search API. 
	 * Each instance of the BlinkxSearchAPICall respresents a single page of results for a call to the Search API and its results.
	 * A Blinkx partner ID is required to use the class.  
	 * For more information about the Blinkx Search API, visit http://usp1.blinkx.com/partnerapi/help/actions/Query/Query.html.
	 * 
	 * Set all call parameters before calling doSearch().
	 * After a call is loaded, if you want to retrieve different pages of results for the same call
	 * use the getNextPage, getPreviousPage, and getCallForPage functions.  pageIndex and the page number passed to
	 * getCallForPage are zero based, so the first page of results is page 0.
	 */
	public class BlinkxSearchAPICall extends ParserBase{
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Automotive category.
		 */ 
		public static const AUTOMOTIVE_CATEOGORY:String = "Automotive";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Business category.
		 */ 
		public static const BUSINESS_CATEGORY:String = "Business";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Cartoon category.
		 */ 
		public static const CARTOON_CATEGORY:String = "Cartoon";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Comedy category.
		 */ 
		public static const COMEDY_CATEGORY:String = "Comedy";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Culture category.
		 */ 
		public static const CULTURE_CATEGORY:String = "Culture";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Education category.
		 */ 
		public static const EDUCATION_CATEGORY:String = "Education";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Entertainment category.
		 */ 
		public static const ENTERTAINMENT_CATEGORY:String = "Entertainment";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Fashion category.
		 */ 
		public static const FASHION_CATEGORY:String = "Fashion";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Fitness category.
		 */ 
		public static const FITNESS_CATEGORY:String = "Fitness";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Food category.
		 */ 
		public static const FOOD_CATEGORY:String = "Food";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Gaming category.
		 */ 
		public static const GAMING_CATEGORY:String = "Gaming";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Health category.
		 */ 
		public static const HEALTH_CATEGORY:String = "Health";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Home category.
		 */ 
		public static const HOME_CATEGORY:String = "Home";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Instructional category.
		 */ 
		public static const INSTRUCTIONAL_CATEGORY:String = "Instructional";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Money category.
		 */ 
		public static const MONEY_CATEGORY:String = "Money";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Movies category.
		 */ 
		public static const MOVIES_CATEGORY:String = "Movies";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Music category.
		 */ 
		public static const MUSIC_CATEGORY:String = "Music";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Nature category.
		 */ 
		public static const NATURE_CATEGORY:String = "Nature";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the News category.
		 */ 
		public static const NEWS_CATEGORY:String = "News";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Politics category.
		 */ 
		public static const POLITICS_CATEGORY:String = "Politics";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Scitech category.
		 */ 
		public static const SCITECH_CATEGORY:String = "Scitech";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Sports category.
		 */ 
		public static const SPORTS_CATEGORY:String = "Sports";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Travel category.
		 */ 
		public static const TRAVEL_CATEGORY:String = "Travel";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Tv category.
		 */ 
		public static const TV_CATEGORY:String = "Tv";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Viral category.
		 */ 
		public static const VIRAL_CATEGORY:String = "Viral";
		/**
		 * Constant used to set the categoryFilter.  When
		 * categoryFiler is set to this constant, results are
		 * limited to the Weather category.
		 */ 
		public static const WEATHER_CATEGORY:String = "Weather";
		
		/**
		 * Constanst used to set the country bias with the function setCountryBias.
		 * When setCountryBias is called with this constant, results are biased to Australia.
		 */
		public static const AUSTRALIA_COUNTRY:String = "AU";
		/**
		 * Constanst used to set the country bias with the function setCountryBias.
		 * When setCountryBias is called with this constant, results are biased to Germany.
		 */
		public static const GERMANY_COUNTRY:String = "DE";
		/**
		 * Constanst used to set the country bias with the function setCountryBias.
		 * When setCountryBias is called with this constant, results are biased to the UK.
		 */
		public static const UNITED_KINGDOM_COUNTRY:String = "UK";
		/**
		 * Constanst used to set the country bias with the function setCountryBias.
		 * When setCountryBias is called with this constant, results are biased to Russia.
		 */
		public static const RUSSIA_COUNTRY:String = "RU";
		/**
		 * Constanst used to set the country bias with the function setCountryBias.
		 * When setCountryBias is called with this constant, results are biased to France.
		 */
		public static const FRANCE_COUNTRY:String = "FR";
		/**
		 * Constanst used to set the country bias with the function setCountryBias.
		 * When setCountryBias is called with this constant, results are biased to Italy.
		 */
		public static const ITALY_COUNTRY:String = "IT";
		/**
		 * Constanst used to set the country bias with the function setCountryBias.
		 * When setCountryBias is called with this constant, results are biased to Spain.
		 */
		public static const SPAIN_COUNTRY:String = "ES";
		/**
		 * Constanst used to set the country bias with the function setCountryBias.
		 * When setCountryBias is called with this constant, results are biased to the Netherlands.
		 */
		public static const NETHERLANDS_COUNTRY:String = "NL";
		/**
		 * Constanst used to set the country bias with the function setCountryBias.
		 * When setCountryBias is called with this constant, results are biased to Sweden.
		 */
		public static const SWEDEN_COUNTRY:String = "SE";
		
		/**
		 * Constanst used to specify the data source type by setting dataSourceTypeFilter.
		 * When dataSourceTypeFilter is set to this constant, results are all of the type News.
		 */
		public static const NEWS_DATASOURCE_TYPE:String = "News";
		/**
		 * Constanst used to specify the data source type by setting dataSourceTypeFilter.
		 * When dataSourceTypeFilter is set to this constant, results are all of the type Celebrity.
		 */
		public static const CELEBRITY_DATASOURCE_TYPE:String = "Celebrity";
		/**
		 * Constanst used to specify the data source type by setting dataSourceTypeFilter.
		 * When dataSourceTypeFilter is set to this constant, results are all of the type How To.
		 */
		public static const HOWTO_DATASOURCE_TYPE:String = "HowTo";
		
		//location of search api search server
		protected static const _blinkxSearchAPIURL:String = "http://usp1.blinkx.com/partnerapi/";
		
		//search phrase used in call
		protected var _searchTerms:String = "";
		//whether or not adult content is in the results
		protected var _filterAdultContent:Boolean = false;
		//amount of bias given to the date. value between 0 and 100 
		protected var _dateBiasAmount:int = -1;
		//category search results are required to be a part of
		protected var _categoryFilter:String;
		//ip address of the consumer.  If set to "caller" the ip address of the api request is used.
		protected var _clientIP:String = "caller";
		//country biased in search results
		protected var _countryOfBias:String;
		//amount of bias given to _countryBiasAmount
		protected var _countryBiasAmount:int = -1;
		//source type search results are required to be a part of
		protected var _dataSourceTypeFilter:String;
		//source search results are required to be a part of.  For example Fox+BBC
		protected var _dataSourceFilter:String;
		//language results are required to be a part of.  See available languages at http://usp1.blinkx.com/partnerapi/help/languages.xml.
		protected var _languageFilter:String;
		//whether or not all search terms must be matched
		protected var _matchAllSearchTerms:Boolean = false;
		//date results must have been published before
		protected var _maxDate:Date;
		//date results must have been published after
		protected var _minDate:Date;
		//id of Blinkx partner
		protected var _partnerId:String;
		//amount of results returned for each call
		protected var _pageAmount:uint = 10;
		//current page of results, zero indexed
		protected var _currentPage:uint = 0;
		
		//Array of ItemTO, results return from call
		protected var _videosReturned:Array;
		//Total number of results to a call, not just those returned
		protected var _totalNumOfResults:uint;
		//total number of pages needed to see all results
		protected var _totalPages:uint;
		
		/**
		 * Constructor. partnerId is required to be set for api calls to be achieved.
		 */ 
		public function BlinkxSearchAPICall(partnerId:String,searchTerms:String = null){
			this._partnerId = partnerId;
			this._searchTerms = searchTerms;
		}
		
		//creates url and query string of api call base on current parameters
		protected function createCallURL():String{
			var url:String = _blinkxSearchAPIURL;
			url += _partnerId + "/";
			
			url += "?AdultFilter=" + _filterAdultContent;
			if(_dateBiasAmount > -1){
				url += "&BiasDate=" + _dateBiasAmount;
			}
			if(_categoryFilter != null && _categoryFilter.length > 0){
				url += "&Category=" + _categoryFilter; 
			}
			if(_clientIP != null && _clientIP.length > 0){
				url += "&ClientIP=" + _clientIP;
			}
			if(_countryOfBias != null && _countryOfBias.length > 0){
				if(_countryBiasAmount < 0){
					_countryBiasAmount = 14;
				}
				url += "&Country=" + _countryOfBias + _countryBiasAmount;
			}
			if(_dataSourceTypeFilter != null && _dataSourceTypeFilter.length > 0){
				url += "&DatabaseList=" + _dataSourceTypeFilter;
			}
			if(_dataSourceFilter != null && _dataSourceFilter.length > 0){
				url += "&DatabaseMatch=" + _dataSourceFilter;
			}
			if(_languageFilter != null && _languageFilter.length > 0){
				url += "&LanguageType=" + _languageFilter;
			}
			url += "&MatchAllTerms=" + _matchAllSearchTerms;
			
			if(_maxDate != null && _minDate != null){//make sure max date is after min date
				if(_maxDate.time < _minDate.time ){
					_minDate = null;
				}
			}
			
			if(_maxDate != null){
				url += "&MaxDate=" + createBlinkxDateString(_maxDate);
			}
			if(_minDate != null){
				url += "&MinDate=" + createBlinkxDateString(_minDate);
			}
			
			if(_currentPage > 0){
				url += "&Start=" + ((_currentPage * _pageAmount)+1);
			}
			if(_pageAmount > 0){
				url += "&MaxResults=" +_pageAmount * (_currentPage+1);
			}
			
		
			url += "&Text=" + encodeURIComponent(_searchTerms);
			url += "&printfields=media_duration";
			
			return url;
		}
		
		//changes a date to Blinkx format
		protected function createBlinkxDateString(date:Date):String{
			var monthS:String = date.month + 1 + "";
			if(monthS.length == 1){
				monthS = "0" + monthS;
			}	
			var dayS:String = date.date + "";
			if(dayS.length == 1){
				dayS = "0" + dayS;
			}
			var yearS:String = date.fullYear + "";
			
			return dayS + "/" + monthS +"/" + yearS;
		}
		
		//parses results and dispathes error event if they are malformed or the call failed
		override protected function parseXML():void {
			//parse xml into ovp itemto object
			var responseType:String = _xml.response + "";
			if(responseType.toLowerCase() != "success"){
				var errorText:String = _xml.error + "";
				dispatchEvent(new OvpEvent(OvpEvent.ERROR, new BlinkxError(BlinkxError.API_ERROR,errorText)));
				return;
			}
			var mainNode:XML = _xml.responsedata[0];
			this._totalNumOfResults = parseInt(mainNode.totalhits.toString());
			this._totalPages = Math.ceil( _totalNumOfResults / _pageAmount );
			var items:XMLList = mainNode.hit;
			var curItemNode:XML;
			var curItem:ItemTO;
			_videosReturned = new Array(items.length());
			for(var x:uint=0; x<items.length(); x++){
				curItemNode = items[x];
				curItem = parseContentItemNode(curItemNode);
				_videosReturned[x] = curItem;
			}
			
			dispatchEvent(new OvpEvent(OvpEvent.PARSED, this));
		}
		
		//changes an xml node into a ItemTO object
		protected function parseContentItemNode(node:XML):ItemTO{
			var item:ItemTO = new ItemTO();
			item.title = node.title.toString();
			item.description = node.summary.toString();
			
			var unixtime:int = parseInt(node.date.toString());
			var date:Date = new Date(unixtime * 1000);
			item.pubDate = date.toDateString();
			
			var mediaLengthInMilla:int = parseInt(node.media_duration);
			var mediaLengthInSec:int = Math.round(mediaLengthInMilla/1000);
			var contentNode:XML = node;
			
			var media:Media = new Media();
			media.description = item.description;
			media.title = item.title;
			
			//parse the video content items
			var mediaNodes:XMLList = contentNode.media;
			var videoContents:Array = new Array(mediaNodes.length());
			var curContent:ContentTO;
			var contentItemNode:XML;
			var format:String;
			for(var x:uint=0; x<videoContents.length; x++){
				contentItemNode = mediaNodes[x];
				curContent = new ContentTO();
				curContent.bitrate = Number(contentItemNode.bitrate);
				curContent.url = contentItemNode.url;
				format = contentItemNode.format;
				if(format.toLowerCase() == "mp4"){
					curContent.type = "video/mp4";
				}else{
					curContent.type = "video/x-flv";
				}
				curContent.duration = mediaLengthInSec+"";
				videoContents[x] = curContent;
			}
			media.contentArray = videoContents;
			//sort the video content based on bitrate
			videoContents = videoContents.sortOn("bitrate",Array.NUMERIC);
			
			//put a mid range bitrate item in the enclosure
			var encItem:ContentTO = videoContents[Math.floor(videoContents.length/2)];
			var enclosure:EnclosureTO = new EnclosureTO();
			enclosure.type = encItem.type;
			enclosure.url = encItem.url;
			
			var thumbnail:ThumbnailTO = new ThumbnailTO();
			thumbnail.url = contentNode.staticpreview.toString();
			
			var sourceThumnnail:ThumbnailTO = new ThumbnailTO();
			sourceThumnnail.url = contentNode.footerimage.toString();
			
			var sourceAltThumnnail:ThumbnailTO = new ThumbnailTO();
			sourceAltThumnnail.url = contentNode.altimage.toString();
			
			media.thumbnail = thumbnail;
			media.thumbnailArray = [thumbnail,sourceThumnnail,sourceAltThumnnail];
			
			item.media = media;
			item.enclosure = enclosure;
			
			return item;
		}
		
		/**
		 * duplicates this call in its current state, not the results but the search parameters
		 */ 
		public function duplicateCall():BlinkxSearchAPICall{
			var call:BlinkxSearchAPICall = new BlinkxSearchAPICall(this._partnerId,this._searchTerms);
			call._categoryFilter = this._categoryFilter;
			call._clientIP = this._clientIP;
			call._countryBiasAmount = this._countryBiasAmount;
			call._countryOfBias = this._countryOfBias;
			call._currentPage = this._currentPage;
			call._dataSourceFilter = this._dataSourceFilter;
			call._dataSourceTypeFilter = this._dataSourceTypeFilter;
			call._dateBiasAmount = this._dateBiasAmount;
			call._filterAdultContent = this._filterAdultContent;
			call._languageFilter = this._languageFilter;
			call._matchAllSearchTerms = this._matchAllSearchTerms;
			
			if(this._maxDate != null){
				call._maxDate = new Date(this._maxDate.time);
			}
			if(this._minDate != null){
				call._minDate = new Date(this._minDate.time);	
			}
		
			call._pageAmount = this._pageAmount
			call._totalNumOfResults  = this._totalNumOfResults
			call._totalPages = this._totalPages
			
			return call;
		}
		
		/**
		 * Calls for search load.  Set all desired parameters before calling this function.
		 * Listen for OvpEvent.ERROR and OvpEvent.LOADED events before calling this function
		 */ 
		public function doSearch():void{
			var url:String = createCallURL();
			load(url);
		}
		/**
		 * Creates and returns a BlinkxSearchAPICall that has the same search parameters
		 * as this instance, but call is the previous page of results.
		 * If there is not a previous page, because this is the first page, a BlinkxSearchAPICall
		 * that is the same page as this instance is returned.
		 * This function should not be called until the this instance is loaded.
		 */ 
		public function getPreviousPage():BlinkxSearchAPICall{
			var prev:BlinkxSearchAPICall = duplicateCall();
			if(prev._currentPage > 0){
				prev._currentPage = prev._currentPage-1;
			}
			
			return prev;
		}
		/**
		 * Creates and returns a BlinkxSearchAPICall that has the same search parameters
		 * as this instance, but call is the next page of results.
		 * If there is not a next page, because this is the last or only page, a BlinkxSearchAPICall
		 * that is the same page as this instance is returned.
		 * This function should not be called until the this instance is loaded.
		 */ 
		public function getNextPage():BlinkxSearchAPICall{
			var next:BlinkxSearchAPICall = duplicateCall();
			if(next._currentPage < totalPages - 1){
				next._currentPage = next._currentPage+1;
			}
			
			return next;
		}
		/**
		 * Creates and returns a BlinkxSearchAPICall that has the same search parameters
		 * as this instance, but the call is a specified page of results.
		 * This function should not be called until the this instance is loaded.
		 * pageIndex is zero based position of page
		 */
		public function getCallForPage(pageIndex:uint):BlinkxSearchAPICall{
			var page:BlinkxSearchAPICall = duplicateCall();
			page._currentPage = pageIndex;
			
			return page;
		}
		
		/**
		 * Retrieves the result item at i index.  If the index does not exist, null is returned.
		 */ 
		public function getItemAt(i:uint):ItemTO {
			if(_videosReturned == null || _videosReturned.length <= i){
				return null;
			}
			return _videosReturned[i];
		}
		/**
		 * The size of each page of results
		 */ 
		public function get pageSize():uint{
			return _pageAmount;
		}
		/**
		 * Sets the size of each page of results
		 */ 
		public function set pageSize(itemsPerPage:uint):void{
			this._pageAmount = itemsPerPage;	
		}
		/**
		 * The number of items returned from the search call.  
		 * Differs from totalItemCount in that totalItemCount is the
		 * total number of results, while itemCount is at max pageSize.
		 */
		public function get itemCount():int {
			if(_videosReturned == null){
				return 0;
			}
			return _videosReturned.length;
		}
		/**
		 * The 1 indexed position of the first item in this page of results
		 */ 
		public function get firstItemPosition():int{
			if(_totalNumOfResults < 1){
				return 0;
			}
			return (_pageAmount * _currentPage) + 1;
		}
		/**
		 * The 1 indexed position of the last item in this page of results
		 */ 
		public function get lastItemPosition():int{
			var last:int = firstItemPosition + _pageAmount - 1;
			if(last > _totalNumOfResults){
				last = _totalNumOfResults;
			}
			return last;
		}
		/**
		 * The total number of results to this search query.
		 */ 
		public function get totalItemCount():int {
			return _totalNumOfResults
		}
		/**
		 * The total number of pages
		 */
		public function get totalPages():int {
			return _totalPages;
		}
		/**
		 * Array of ItemTO.  The results to this api call.
		 */
		public function get itemArray():Array {
			return _videosReturned;
		}
		/**
		 * Sets whether or not adult content should be returned
		 */ 
		public function set filterAdultContent(turnOfFiltering:Boolean):void{
			this._filterAdultContent = turnOfFiltering;
		}
		/**
		 * returns true if adult content is allowed in search results
		 */ 
		public function get filterAdultContent():Boolean{
			return this._filterAdultContent;
		}
		/**
		 * Sets the amount of date bias used on the results.  Value must be between 0 and 100.
		 * A value of 100 would mean the results are sorted by date.
		 */ 
		public function set dateBiasAmount(amountOfBias:int):void{
			if(amountOfBias > 100){
				amountOfBias = 100;
			}else if(amountOfBias < 0){
				amountOfBias = 0;
			}
			this._dateBiasAmount = amountOfBias;
		}
		/**
		 * returns the amount of date bias used on the results
		 */ 
		public function get dateBiasAmount():int{
			return this._dateBiasAmount;
		}
		
		/**
		 * Sets a category that search results are required to be a part of. Use the *_CATEGORY constants
		 * in the class to set.
		 */
		public function set categoryFilter(requiredCategory:String):void{
			this._categoryFilter = requiredCategory;
		}
		/**
		 * returns the category that is required of search results.  Returns null if no category is set.'
		 */ 
		public function get categoryFilter():String{
			return this._categoryFilter;
		}
		/**
		 * Sets a country that search results are biased towards. Use the *_COUNTRY constants
		 * in the class to set.
		 * The biasAmount is between 1 and 99.
		 */
		public function setCountryBias(countryCode:String,biasAmount:int = 14):void{
			_countryOfBias = countryCode;
			if(biasAmount < 1){
				biasAmount = 1;
			}else if(biasAmount > 99){
				biasAmount = 99;
			}
			if(_countryOfBias == null || _countryOfBias.length == 0){
				_countryBiasAmount = -1;
			}else{
				_countryBiasAmount = biasAmount;
			}
		}
		/**
		 * returns the country that search results are biased towards.
		 */ 
		public function getCountryOfBias():String{
			return	_countryOfBias;
		}
		/**
		 * returns the amount of the country bias.  Value between 1 and 99, or -1 if not set
		 */ 
		public function getCountryBiasAmount():int{
			return _countryBiasAmount;
		}
		/**
		 * Sets a data source type that search results will be filtered through. Use the *_DATASOURCE_TYPE constants
		 * in the class to set.
		 */
		public function set dataSourceTypeFilter(requiredSourceType:String):void{
			this._dataSourceTypeFilter = requiredSourceType;
		}
		/**
		 * return current data source filter type
		 */ 
		public function get dataSourceTypeFilter():String{
			return this._dataSourceTypeFilter;
		}
		/**
		 * Sets a data source to filter by.  Set to a content provider to search videos only provided by that provider.
		 */ 
		public function set dataSourceFilter(requiredSource:String):void{
			this._dataSourceFilter = requiredSource;
		}
		/**
		 * return the current data source to filter by, null if no filter is set.
		 * 
		 */ 
		public function get dataSourceFilter():String{
			return this._dataSourceFilter;
		}
		/**
		 * Sets a language to filter results by. See available languages at http://usp1.blinkx.com/partnerapi/help/languages.xml.
		 */ 
		public function set languageFilter(requiredResultLanguage:String):void{
			this._languageFilter = requiredResultLanguage;
		}
		/**
		 * returns set language filter, null if none is set
		 */ 
		public function get languageFilter():String{
			return this._languageFilter;
		}
		/**
		 * set to true to require all search terms in be in results, false to require only some terms.  Default is false
		 */ 
		public function set matchAllSearchTerms(requireAllTerms:Boolean):void{
			this._matchAllSearchTerms = requireAllTerms;
		}
		/**
		 * return Boolean value of true if all search terms must be in result titles, false otherwise;
		 */ 
		public function get matchAllSearchTerms():Boolean{
			return this._matchAllSearchTerms;
		}
		/**
		 * Sets the date that search results must have been published before. Default is null which means results may include titles published up to the present time.
		 */ 
		public function set newestSearchDate(date:Date):void{
			this._maxDate = date;
		}
		/**
		 * returns the date that search results must have been published before.  null if not set.  Default is null which means results may include titles published up to the present time.
		 */ 
		public function get newestSearchDate():Date{
			return this._maxDate;
		}
		/**
		 * Sets the date that search results must have been published after. Default is null which means results may include titles published at any date in the past.
		 */ 
		public function set oldestSearchDate(date:Date):void{
			this._minDate = date;
		}
		/**
		 * returns the date that search results must have been published after. Default is null which means results may include titles published at any date in the past.
		 */ 
		public function get oldestSearchDate():Date{
			return this._minDate;
		}
		/**
		 * Sets partner id which identifies client to the Blinkx API servers.  The partnerId is required to be set before placing an API call.
		 */
		public function set partnerId(id:String):void{
			this._partnerId = id;
		}
		/**
		 * returns the partner id which identifies client to the Blinkx API servers.
		 */
		public function get partnerId():String{
			return this._partnerId;
		}
		/** 
		 * Sets word or words to search by.  The searchTerms is required to be set before placing an API call.
		 * see matchAllSearchTerms for whether or not all words set will be matched in a results title
		 */ 
		public function set searchTerms(terms:String):void{
			this._searchTerms = terms;
		}
		/** 
		 * returns word or words to search by.  
		 */ 
		public function get searchTerms():String{
			return this._searchTerms;
		}
		/**
		 * return which page of results this call is going to load.  pageIndex is zero based, so first page of results is 0.
		 */ 
		public function get pageIndex():uint{
			return this._currentPage;
		}
		/**
		 * set which page of results this call is going to load.  pageIndex is zero based, so first page of results is 0.
		 */ 
		public function set pageIndex(pageIndex:uint):void{
			this._currentPage = pageIndex;
		}
	}
}