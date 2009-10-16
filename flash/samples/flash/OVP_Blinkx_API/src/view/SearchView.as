package view 
{
	
	import fl.containers.ScrollPane;
	import fl.controls.Button;
	import fl.controls.ComboBox;
	import fl.controls.Label;
	import fl.controls.ScrollPolicy;
	import fl.controls.TextInput;
	import fl.core.UIComponent;
	import fl.data.DataProvider;
	import fl.events.ComponentEvent;
	
	import flash.display.MovieClip;
	import flash.events.Event;
	import flash.events.MouseEvent;
	import flash.text.AntiAliasType;
	import flash.text.TextFieldAutoSize;
	import flash.text.TextFormat;
	
	import ui.AkamaiArialBold;
	
	import view.skin.SearchViewBackgroundSkin;
	
	
	public class SearchView extends MovieClip {
		
		public static const SEARCH_REQUESTED:String = "SEARCH_REQUESTED";
		public static const SEARCH_CANCELLED:String = "SEARCH_CANCELLED";
		
		protected var scrollPane:ScrollPane;
		
		protected var contentHolder:UIComponent;
		
		protected var searchTitle:Label;
		protected var termLabel:Label;
		protected var termField:TextInput;
		protected var resetButton:Button;
		protected var submitButton:Button;
		protected var cancelButton:Button;
		
		protected var dateLabel:Label;
		protected var dateDrop:ComboBox;
		
		protected var sortLabel:Label;
		protected var sortDrop:ComboBox;
		
		
		protected var _width:Number;
		protected var _height:Number;
		
		public function SearchView(){
			createChildren();
		}
		
		protected function createChildren():void{
			contentHolder = new UIComponent();
			
			searchTitle  = new Label();
			searchTitle.autoSize = TextFieldAutoSize.LEFT;
			searchTitle.setStyle( "textFormat", new TextFormat((new AkamaiArialBold()).fontName,18,0x000000,true) );
			searchTitle.text = "Search Options:";
			searchTitle.textField.embedFonts = true;
			searchTitle.textField.antiAliasType = AntiAliasType.ADVANCED;
			
			termLabel  = new Label();
			termLabel.autoSize = TextFieldAutoSize.LEFT;
			termLabel.setStyle( "textFormat", new TextFormat((new AkamaiArialBold().fontName),12,0x000000,true) );
			termLabel.text = "Search Term";
			termLabel.textField.embedFonts = true;
			termLabel.textField.antiAliasType = AntiAliasType.ADVANCED;
			
			termField = new TextInput();
			termField.text = "";
			termField.addEventListener(ComponentEvent.ENTER,onSubmitClick);
			//termField.addEventListener(TextEvent.TEXT_INPUT,onTextChange);
			termField.textField.addEventListener(Event.CHANGE,onTextChange);
			
			dateLabel  = new Label();
			dateLabel.autoSize = TextFieldAutoSize.LEFT;
			dateLabel.setStyle( "textFormat", new TextFormat((new AkamaiArialBold().fontName),12,0x000000,true) );
			dateLabel.text = "Published After";
			dateLabel.textField.embedFonts = true;
			dateLabel.textField.antiAliasType = AntiAliasType.ADVANCED;
			
			dateDrop = new ComboBox();
			dateDrop.rowCount = 6;
			dateDrop.dataProvider = new DataProvider([{label:"No Limit",data:0},{label:"5 days ago",data:5},{label:"10 days ago",data:10},{label:"15 days ago",data:15},{label:"30 days ago",data:30},{label:"60 days ago",data:60}]);
			
			sortLabel  = new Label();
			sortLabel.autoSize = TextFieldAutoSize.LEFT;
			sortLabel.setStyle( "textFormat", new TextFormat((new AkamaiArialBold().fontName),12,0x000000,true) );
			sortLabel.text = "Sort By";
			sortLabel.textField.embedFonts = true;
			sortLabel.textField.antiAliasType = AntiAliasType.ADVANCED;
			
			sortDrop = new ComboBox();
			sortDrop.dataProvider = new DataProvider([{label:"Relevance",data:""},{label:"Date",data:"date"}]);
			
			
			resetButton = new Button();
			resetButton.label = "Reset";
			resetButton.addEventListener(MouseEvent.CLICK,onResetClick);
			
			submitButton = new Button();
			submitButton.label = "Search";
			submitButton.addEventListener(MouseEvent.CLICK,onSubmitClick);
			
			cancelButton = new Button();
			cancelButton.label = "Cancel";
			cancelButton.addEventListener(MouseEvent.CLICK,onCancelClick);
			
			scrollPane = new ScrollPane();
			scrollPane.horizontalScrollPolicy = ScrollPolicy.OFF;
			scrollPane.setStyle("skin",SearchViewBackgroundSkin);
			scrollPane.setStyle("upSkin",SearchViewBackgroundSkin);
			
			contentHolder.addChild(dateLabel);
			contentHolder.addChild(dateDrop);
			contentHolder.addChild(sortLabel);
			contentHolder.addChild(sortDrop);
			contentHolder.addChild(searchTitle);
			contentHolder.addChild(termLabel);
			contentHolder.addChild(termField);
			contentHolder.addChild(resetButton);
			contentHolder.addChild(submitButton);
			contentHolder.addChild(cancelButton);
			
			addChild(contentHolder);
			
			addChild(scrollPane);
			
			scrollPane.source = contentHolder;
			
			onTextChange();
		}
		
		protected function onTextChange(event:Event = null):void{
			submitButton.enabled = termField.text != null && termField.text.length > 0;
			
		}
		protected function onCancelClick(event:MouseEvent):void{
			dispatchEvent(new Event(SEARCH_CANCELLED));
		}
		
		protected function onResetClick(event:MouseEvent):void{
			termField.text = "";
			dateDrop.selectedIndex = 0;
			sortDrop.selectedIndex = 0;
			onTextChange();
		}
		
		protected function onSubmitClick(event:Event):void{
			//check that at least the terms field is filled out
			var terms:String = termField.text;
			if(terms == null || terms.length < 1 ){
				
			}else {
				dispatchEvent(new Event(SEARCH_REQUESTED));
			}
		}
		
		protected function layout():void{
			scrollPane.setSize(_width,_height);
			scrollPane.move(0,0);
			
			var sideBuffer:Number = 15;
			var contentWidth:Number = _width - (sideBuffer*2);
			var curX:Number = sideBuffer;
			var curY:Number = 15;
			var vertGap:Number = 10;
			var hGap:Number = 8;
			var column:Number = sideBuffer + dateLabel.width + (hGap * 2);
			
			searchTitle.move(curX,curY);
			curY += searchTitle.height + vertGap;
			
			termLabel.move(curX,curY);
			termField.setSize(contentWidth - column,20);
			termField.move(column,curY);
			curY += termField.height + vertGap;
			
			dateLabel.move(curX,curY);
			dateDrop.move(column,curY);
			curY += dateLabel.height + vertGap;
			
			sortLabel.move(curX,curY);
			sortDrop.move(column,curY);
			curY += dateLabel.height + vertGap;
			curY += vertGap;
			
			cancelButton.move(_width - sideBuffer - cancelButton.width,curY);
			resetButton.move(cancelButton.x - hGap - resetButton.width,curY);
			submitButton.move(resetButton.x - hGap - submitButton.width,curY);
			curY += submitButton.height + vertGap;
			
			contentHolder.width = _width;
			contentHolder.height = curY;
			
			
			scrollPane.update();
		}
		
		public function getCutOffDate():Date{
			var days:Number = dateDrop.selectedItem.data;
			if(days <1 ){
				return null;
			}
			var date:Date = new Date();
			date.setDate(date.getDate() - days);
			return date;
		}
		public function isSortByDate():Boolean{
			return this.sortDrop.selectedIndex == 1;
		}
		public function setSearchTerms(terms:String):void{
			this.termField.text = terms;
			onTextChange();
		}
		public function getSearchTerms():String{
			return termField.text;
		}
		public function resizeTo(wid:Number,hei:Number):void{
			this._width = wid;
			this._height = hei;
			
			layout();
		
		}
	}
}