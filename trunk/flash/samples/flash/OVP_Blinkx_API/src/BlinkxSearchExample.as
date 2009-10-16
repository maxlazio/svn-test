package 
{
	import com.blinkx.search.BlinkxError;
	import com.blinkx.search.BlinkxSearchAPICall;
	
	import flash.display.*;
	import flash.events.Event;
	import flash.events.FullScreenEvent;
	import flash.geom.Rectangle;
	import flash.system.Capabilities;
	
	import org.openvideoplayer.events.OvpError;
	import org.openvideoplayer.events.OvpEvent;
	
	import view.SearchResultsBar;
	import view.SearchView;
	
	public class BlinkxSearchExample extends MovieClip 
	{
		
		private var player:BlinkxExampleAkamaiMultiPlayer;
		private var searchScreen:SearchView;
		private var pageBar:SearchResultsBar;
		private var popupBlock:Sprite;
		
		private var currentAPICall:BlinkxSearchAPICall;
		
		public function BlinkxSearchExample():void {
			stage.scaleMode = StageScaleMode.NO_SCALE;
			stage.align = StageAlign.TOP_LEFT;
			stage.addEventListener(Event.RESIZE, resize);
			stage.addEventListener(FullScreenEvent.FULL_SCREEN, exitFullScreen);
		
			player  = new BlinkxExampleAkamaiMultiPlayer(stage.stageWidth, stage.stageHeight, loaderInfo.parameters);
			
			player.addEventListener("toggleFullscreen", handleFullscreen);
			addChild(player);
			
			pageBar = new SearchResultsBar();
			pageBar.addEventListener(SearchResultsBar.PAGE_BACK_CALLED,onPageBack);
			pageBar.addEventListener(SearchResultsBar.PAGE_FORWARD_CALLED,onPageForward);
			pageBar.addEventListener(SearchResultsBar.SEARCH_ADVANCED_CALLED,onSearchAdvanced);
			pageBar.addEventListener(SearchView.SEARCH_REQUESTED,onSearchCalled);
			
			addChild(pageBar);
			
			popupBlock = new Sprite();
			popupBlock.useHandCursor = false;
			popupBlock.buttonMode = true;
			popupBlock.mouseEnabled = true;
			addChild(popupBlock);
			
			searchScreen = new SearchView();
			searchScreen.addEventListener(SearchView.SEARCH_REQUESTED,onAdvancedSearchCalled);
			searchScreen.addEventListener(SearchView.SEARCH_CANCELLED,onSearchCancelled);
			
			addChild(searchScreen);
			searchScreen.visible = popupBlock.visible = false;
			
			resize();
		}
		
		private function onSearchCancelled(event:Event):void{
			popupBlock.visible = searchScreen.visible = false;
		}
		
		private function onSearchAdvanced(event:Event):void{
			popupBlock.visible = searchScreen.visible = true;
			searchScreen.setSearchTerms(pageBar.getSearchTerms());
		}
		
		private function onPageForward(event:Event):void{
			var call:BlinkxSearchAPICall = currentAPICall.getNextPage();//same as currentAPICall.getCallForPage(currentAPICall.pageIndex+1);
			doSearch(call);
		}
		
		private function onPageBack(event:Event):void{
			var call:BlinkxSearchAPICall = currentAPICall.getPreviousPage();//same as currentAPICall.getCallForPage(currentAPICall.pageIndex-1);
			doSearch(call);
		}
		
		private function onAdvancedSearchCalled(event:Event):void{
			popupBlock.visible = searchScreen.visible = false;
			var terms:String = searchScreen.getSearchTerms();
			pageBar.setSearchTerms(terms);
			var call:BlinkxSearchAPICall = new BlinkxSearchAPICall("akamaidev_37fa482e",terms);
			call.pageSize = 6;
			
			call.oldestSearchDate = searchScreen.getCutOffDate();
			if(searchScreen.isSortByDate()){
				call.dateBiasAmount = 100;
			}else{
				call.dateBiasAmount = 14;
			}
			doSearch(call);
		}
		
		private function onSearchCalled(event:Event):void{
			popupBlock.visible = searchScreen.visible = false;
			var terms:String = pageBar.getSearchTerms();
			var call:BlinkxSearchAPICall = new BlinkxSearchAPICall("akamaidev_37fa482e",terms);
			call.pageSize = 6;
			
			call.oldestSearchDate = searchScreen.getCutOffDate();
			if(searchScreen.isSortByDate()){
				call.dateBiasAmount = 100;
			}else{
				call.dateBiasAmount = 14;
			}
			doSearch(call);
		}
		
		private function doSearch(call:BlinkxSearchAPICall):void{
			if(currentAPICall != null){
				currentAPICall.removeEventListener(OvpEvent.PARSED,onCallLoaded);
				currentAPICall.removeEventListener(OvpEvent.ERROR,onCallError);
				currentAPICall = null;
			}
			currentAPICall = call;
			currentAPICall.addEventListener(OvpEvent.PARSED,onCallLoaded);
			currentAPICall.addEventListener(OvpEvent.ERROR,onCallError);
			pageBar.showLoading();
			
			currentAPICall.doSearch();
		}
		
		private function onCallLoaded(event:OvpEvent):void{
			popupBlock.visible = searchScreen.visible = false;
			player.playVideos(currentAPICall.itemArray);
			
			pageBar.updatePaging(currentAPICall.searchTerms,currentAPICall.firstItemPosition,currentAPICall.lastItemPosition,currentAPICall.totalItemCount);
		}
		
		private function onCallError(event:OvpEvent):void{
			var data:Object = event.data;
			if(data is BlinkxError){
				trace("ON CALL ERROR!! "+(event.data as BlinkxError).errorDescription);
				player.showError("Error querying Blinkx: "+(event.data as BlinkxError).errorDescription);
			}else if(data is OvpError){
				trace("ON CALL ERROR!! "+(event.data as OvpError).errorDescription);
				player.showError("Error querying Blinkx: "+(event.data as OvpError).errorDescription);
			}
			
		}
		
		private function resize(e:Event = null):void {
			pageBar.resizeTo(stage.stageWidth);
		
			player.resizeTo(stage.stageWidth, stage.stageHeight - pageBar.height);
			player.y = pageBar.height;
				
			var searchWid:Number = stage.stageWidth - 50;
			var searchHei:Number = stage.stageHeight - 50;
			if(searchWid > 500){
				searchWid = 500;
			}
			searchScreen.resizeTo(searchWid,searchHei);
			searchScreen.x = Math.round((stage.stageWidth - searchWid)/2);
			searchScreen.y = 25;
			
			var g:Graphics = popupBlock.graphics;
			g.clear();
			g.beginFill(0xffffff,.1);
			g.drawRect(0,0,stage.stageWidth,stage.stageHeight);
			g.endFill();
			
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
				player.resizeTo(stage.stageWidth,stage.stageHeight);
			}
		}
	}
	
}
