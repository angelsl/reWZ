# reWZ
...is a simple and clean .NET library for _reading_ MapleStory WZ files. It does not support writing or modifying WZ files. 

Written with simplicity in mind, it might be slower than other libraries, but it will always be easier to use.

##Usage
Simply reference reWZ.dll in your code, and then load a WZ file like this:

    WZFile xz = new WZFile(@"D:\Effect.wz", WZVariant.GMS, true);

You can resolve a path like so:

    Bitmap m = xz.ResolvePath("BasicEff.img/LevelUp/7").ValueOrDie<Bitmap>();

Or, if you prefer to go the old-fashioned way:

    WZCanvasProperty wzcp = (WZCanvasProperty)xz.MainDirectory["BasicEff.img"]["LevelUp"]["7"];
    Bitmap m = wzcp.Value;

Otherwise, reWZ is pretty well documented (via inline XMLdoc), but if you have any questions, feel free to send angelsl a message.

##License
reWZ is licensed under the GNU GPL v3.0 with Classpath Exception.

##Acknowledgements

 * jonyleeson, haha01haha01, Snow, and others who contributed to the original C# WzLib, and [later versions of it](http://code.google.com/p/maplelib2/).
 * [Fiel](http://www.southperry.net/member.php?u=1) from [Southperry](http://www.southperry.net), for giving me a reason to write this. And all his help.
 * [retep998](https://github.com/retep998), for continually boasting how [his library written in C++](https://github.com/NoLifeDev/NoLifeWz) parses the entire set of v40b WZ files in 7 seconds.