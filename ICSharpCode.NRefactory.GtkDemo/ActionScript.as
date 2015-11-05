package {
        import flash.display.Sprite;
        import flash.text.TextField;

        public class HelloWorld extends Sprite {

                public function HelloWorld() {
                        var display_txt:TextField = new TextField();
                        display_txt.text = "Hello World!";
                        addChild(display_txt);
                }
        }
}
