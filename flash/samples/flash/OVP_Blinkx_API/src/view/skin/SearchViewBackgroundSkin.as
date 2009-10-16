package view.skin
{
	import flash.display.Shape;

	public class SearchViewBackgroundSkin extends Shape
	{
		protected var _width:Number = 0;
		protected var _height:Number = 0;
		
		public function SearchViewBackgroundSkin()
		{
			super();
		}
		
		protected function draw():void{
			graphics.clear();
			if(_width == 0 || _height == 0 || isNaN(_width) || isNaN(height)){
				return;
			}
			var borderSize:Number = 1;
			var dBorder:Number = borderSize * 2;
			var contentWid:Number = _width - dBorder;
			var contentHei:Number = _height - dBorder;
			//black border
			graphics.beginFill(0x000000,1);
			graphics.drawRoundRectComplex(0,0,_width,_height,5,5,5,5);
			graphics.drawRoundRectComplex(borderSize,borderSize,contentWid,contentHei,5,5,5,5);
			graphics.endFill();
			
			//grey background
			graphics.beginFill(0xcccccc,.9);
			graphics.drawRoundRectComplex(borderSize,borderSize,contentWid,contentHei,4,4,4,4);
			graphics.endFill();
		}
		
		public function setSize(w:Number,h:Number):void{
			_width = w;
			_height = h;	
			draw();
		}
		
		override public function set width(wid:Number):void{
			setSize(wid,_height);
		}
		
		override public function set height(hei:Number):void{
			setSize(_width,hei);
		}
		
		override public function get width():Number{
			return _width;
		}
		
		override public function get height():Number{
			return _height;
		}
	}
}