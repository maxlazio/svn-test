package view
{
	import fl.controls.Button;
	import fl.controls.Label;
	import fl.controls.TextInput;
	import fl.events.ComponentEvent;
	
	import flash.display.Graphics;
	import flash.display.Shape;
	import flash.display.Sprite;
	import flash.events.Event;
	import flash.events.MouseEvent;
	import flash.text.AntiAliasType;
	import flash.text.TextFieldAutoSize;
	import flash.text.TextFormat;

	public class SearchResultsBar extends Sprite
	{
		public static const SEARCH_REQUESTED:String = "SEARCH_REQUESTED";
		public static const PAGE_FORWARD_CALLED:String = "PAGE_FORWARD_CALLED";
		public static const PAGE_BACK_CALLED:String = "PAGE_BACK_CALLED";
		public static const SEARCH_ADVANCED_CALLED:String = "SEARCH_ADVANCED_CALLED";
		
		protected var _width:Number;
		protected var _height:Number = 30;
		
		protected var searchSubmitButton:Button;
		protected var pageForwardButton:Button;
		protected var pageBackwardButton:Button;
		protected var advancedButton:Button;
		
		protected var termField:TextInput;
		
		protected var enterText:Label;
		protected var positionText:Label;
		
		protected var hGap:int = 7;
		protected var margin:int = 5;
		protected var showingLoad:Boolean = false;
		
		public function SearchResultsBar()
		{
			super();
			createChildren();
		}
		
		protected function onAdvancedSearchClick(event:MouseEvent):void{
			dispatchEvent(new Event(SEARCH_ADVANCED_CALLED));
		}
		
		protected function onForwardClick(event:MouseEvent):void{
			dispatchEvent(new Event(PAGE_FORWARD_CALLED));
		}
		
		protected function onBackwardClick(event:MouseEvent):void{
			dispatchEvent(new Event(PAGE_BACK_CALLED));
		}
		
		protected function createChildren():void{
			searchSubmitButton = new Button();
			searchSubmitButton.label = "Search";
			searchSubmitButton.addEventListener(MouseEvent.CLICK,onSubmitClick);
			searchSubmitButton.useHandCursor = true;
			
			advancedButton = new Button();
			advancedButton.label = "Advanced";
			advancedButton.addEventListener(MouseEvent.CLICK,onAdvancedSearchClick);
			advancedButton.setStyle( "textFormat",  new TextFormat("Arial",12,0xffffff,false,null,true) );
			advancedButton.setStyle( "upSkin", Shape);
			advancedButton.setStyle( "downSkin", Shape);
			advancedButton.setStyle( "overSkin", Shape);
			advancedButton.useHandCursor = true;
			
			pageForwardButton = new Button();
			pageForwardButton.label = "Next Page";
			pageForwardButton.addEventListener(MouseEvent.CLICK,onForwardClick);
			pageForwardButton.useHandCursor = true;
			
			pageBackwardButton = new Button();
			pageBackwardButton.label = "Previous Page";
			pageBackwardButton.addEventListener(MouseEvent.CLICK,onBackwardClick);
			pageBackwardButton.useHandCursor = true;
			
			enterText = new Label();
			enterText.autoSize = TextFieldAutoSize.LEFT;
			enterText.setStyle( "textFormat",  new TextFormat("Arial",14,0xffffff,true) );
			enterText.text = "Enter Text:";
			enterText.textField.antiAliasType = AntiAliasType.ADVANCED;
			enterText.textField.embedFonts = true;
			
			positionText = new Label();
			positionText.autoSize = TextFieldAutoSize.LEFT;
			positionText.setStyle( "textFormat",  new TextFormat("Arial",12,0xffffff,false) );
			positionText.text = "Showing 0-0 of 0";
			
			termField = new TextInput();
			termField.text = "";
			termField.addEventListener(ComponentEvent.ENTER,onSubmitClick);
			termField.textField.addEventListener(Event.CHANGE,onTextChange);
			
			addChild(advancedButton);
			addChild(enterText);
			addChild(termField);
			addChild(positionText);
			addChild(searchSubmitButton);
			addChild(pageForwardButton);
			addChild(pageBackwardButton);
			
			pageBackwardButton.enabled = false;
			pageForwardButton.enabled = false;
			searchSubmitButton.enabled = false;	
		}
		
		protected function onTextChange(event:Event = null):void{
			if(!showingLoad){
				searchSubmitButton.enabled = termField.text != null && termField.text.length > 0;
			}	
		}
		
		protected function onSubmitClick(event:Event):void{
			//check that at least the terms field is filled out
			var terms:String = termField.text;
			if(terms == null || terms.length < 1 ){
				
			}else {
				dispatchEvent(new Event(SEARCH_REQUESTED));
			}
		}
		
		protected function drawLayout():void{
			enterText.move(margin,(_height - enterText.height)/2);
			
			termField.setSize(250,20);
			termField.move(enterText.x+enterText.width+hGap,(_height - termField.height)/2);
			
			searchSubmitButton.setSize(85,searchSubmitButton.height);
			searchSubmitButton.move(termField.x + termField.width + hGap,(_height - searchSubmitButton.height)/2);
			
			advancedButton.setSize(68,advancedButton.height);
			advancedButton.move(searchSubmitButton.x + searchSubmitButton.width +hGap,(_height - advancedButton.height)/2);
			
			var curX:int = _width - margin - pageForwardButton.width;
			pageForwardButton.move(curX,searchSubmitButton.y);
			curX -= hGap + positionText.width;
			positionText.move(curX,(_height - positionText.height)/2);
			curX -= hGap + pageBackwardButton.width;
			pageBackwardButton.move(curX,searchSubmitButton.y);
			
			
		}
		
		
		public function updatePaging(searchTerm:String,shownItemStart:uint,shownItemEnd:uint,totalItems:uint):void{
			showingLoad = false;
			positionText.text = "Showing: "+shownItemStart+" - "+shownItemEnd+" of "+totalItems;
			
			pageBackwardButton.enabled = shownItemStart > 1;
			pageForwardButton.enabled = shownItemEnd < totalItems;
			
			searchSubmitButton.label = "Search";
			onTextChange();
			
			drawLayout();
		}
		public function showLoading():void{
			showingLoad = true;
			searchSubmitButton.label = "Loading...";
			searchSubmitButton.enabled = false;	
			pageBackwardButton.enabled = false;
			pageForwardButton.enabled = false;
		}
		
		public function resizeTo(newWidth:Number):void{
			_width = newWidth;
			drawLayout();
			
			var g:Graphics = graphics;
			g.clear();
			g.beginFill(0x333333,1);
			g.drawRect(0,0,_width,_height);
			g.endFill();
		}
		
		override public function get height():Number{
			return _height;
		}
		
		public function getSearchTerms():String{
			return termField.text;
		}
		
		public function setSearchTerms(terms:String):void{
			termField.text = terms;
		}
	}
}