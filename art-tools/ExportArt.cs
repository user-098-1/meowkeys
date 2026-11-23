using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// A build-time exporter. The reference WPF assemblies and sources are only required on Windows.
internal static class ExportArt
{
    const BindingFlags StaticMember = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags ScalarMember = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const int Scale = 2;
    static string output;
    static readonly Dictionary<string, object> sprites = new Dictionary<string, object>();
    static readonly Dictionary<string, object> keys = new Dictionary<string, object>();
    static readonly Type Tuxedo = typeof(CatArt).GetNestedType("Tuxedo", BindingFlags.NonPublic);

    static T Field<T>(Type type,string name) { return (T)type.GetField(name,StaticMember).GetValue(null); }
    static void Call(Type type,string name,params object[] args) { type.GetMethod(name,StaticMember).Invoke(null,args); }
    static double[] Position(Point value) { return new double[] {value.X,value.Y}; }

    static void Sprite(string id,Action<DrawingContext> paint)
    {
        var drawing=new DrawingGroup();
        using(DrawingContext dc=drawing.Open()) paint(dc);
        Rect b=drawing.Bounds;
        if(b.IsEmpty) throw new Exception("Empty sprite: "+id);
        int x=(int)Math.Floor(b.Left)-1,y=(int)Math.Floor(b.Top)-1;
        int w=(int)Math.Ceiling(b.Right)+1-x,h=(int)Math.Ceiling(b.Bottom)+1-y;
        var visual=new DrawingVisual();
        using(DrawingContext dc=visual.RenderOpen()){
            dc.PushTransform(new TranslateTransform(-x,-y));
            dc.DrawDrawing(drawing);
            dc.Pop();
        }
        var bmp=new RenderTargetBitmap(w*Scale,h*Scale,96*Scale,96*Scale,PixelFormats.Pbgra32);
        bmp.Render(visual);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));
        using(var stream=File.Create(Path.Combine(output,id+".png"))) encoder.Save(stream);
        sprites.Add(id,new {file=id+".png",x=x,y=y,w=w,h=h});
    }

    static void Cats()
    {
        for(int c=0;c<2;c++){
            int cat=c;
            bool tuxedo=c==1;
            Sprite("body-"+c, delegate(DrawingContext dc){
                Geometry body=Field<Geometry>(typeof(CatArt),"Body");
                dc.DrawGeometry(Field<Brush>(typeof(CatArt),"Fur"),null,body);
                if(tuxedo)Call(Tuxedo,"DrawBodyMarkings",dc,body);
            });
            for(int phase=0;phase<8;phase++){
                double angular=2*Math.PI*phase/8;
                Sprite("tail-"+c+"-"+phase, delegate(DrawingContext dc){
                    Call(typeof(CatArt),"Tail",dc,angular/1.9,tuxedo);
                });
            }
            for(int ix=0;ix<3;ix++)for(int iy=0;iy<3;iy++)for(int lid=0;lid<3;lid++){
                double gx=ix-1.0,gy=iy-1.0,blink=lid*.5;
                Sprite("head-"+c+"-"+ix+"-"+iy+"-"+lid,delegate(DrawingContext dc){
                    Call(typeof(CatArt),"DrawHead",dc,tuxedo,gx,gy,blink);
                });
            }
            Sprite("portrait-"+c,delegate(DrawingContext dc){CatArt.DrawPortrait(dc,(CatStyle)cat);});
        }
    }

    static void Peripherals()
    {
        for(int vk=0;vk<256;vk++)keys.Add(vk.ToString(),Position(DeskArt.KeyPoint(vk)));
        IDictionary surfaces=Field<IDictionary>(typeof(DeskArt),"Surfaces");
        MethodInfo lookup=typeof(DeskArt).GetMethod("Lookup",StaticMember);
        for(int i=0;i<4;i++){
            var style=(KeyboardStyle)i;
            Sprite("keyboard-"+i,delegate(DrawingContext dc){DeskArt.DrawKeyboard(dc,style,null);});
            object surface=surfaces[style];
            var lights=(IDictionary)surface.GetType().GetField("Lights",ScalarMember).GetValue(surface);
            foreach(DictionaryEntry item in lights){
                int code=(int)item.Key;
                var overlay=(DrawingGroup)item.Value;
                Sprite("key-"+i+"-"+code,delegate(DrawingContext dc){
                    dc.PushTransform(new TranslateTransform(0,DeskLayout.Forward));
                    dc.DrawDrawing(overlay);dc.Pop();
                });
            }
            // Aliases share one PNG and use the same projection as the live WPF keyboard.
            for(int vk=0;vk<256;vk++){
                string id="key-"+i+"-"+vk;
                if(sprites.ContainsKey(id))continue;
                object key=lookup.Invoke(null,new object[]{vk});
                if(key==null)continue;
                int keyCode=(int)key.GetType().GetField("Code",ScalarMember).GetValue(key);
                sprites.Add(id,sprites["key-"+i+"-"+keyCode]);
            }
        }
        int mouseStyles=Enum.GetValues(typeof(MouseStyle)).Length;
        for(int i=0;i<mouseStyles;i++){
            var mouse=(MouseStyle)i;
            foreach(string state in new[]{"normal","left","right","both"}){
                bool l=state=="left"||state=="both",r=state=="right"||state=="both";
                Sprite("mouse-"+i+"-"+state,delegate(DrawingContext dc){MouseArt.Draw(dc,mouse,l?1:0,r?1:0);});
            }
        }
        // Cursor overlays are the 512px Windows cursor PNGs copied by Export-Art.ps1;
        // the sizes are recorded in pixels so the macOS overlay draws at 256 points.
        sprites.Add("cursor-0",new {file="cursor-0.png",x=0,y=0,w=512,h=512});
        sprites.Add("cursor-1",new {file="cursor-1.png",x=0,y=0,w=512,h=512});
    }

    [STAThread]public static int Main(string[] args)
    {
        output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);
        Cats();Peripherals();
        var manifest=new {scale=Scale,sceneWidth=480,sceneHeight=320,mouseCenter=Position(MouseArt.Center),mousePaw=Position(MouseArt.PawTarget),sprites=sprites,keys=keys};
        File.WriteAllText(Path.Combine(output,"atlas.json"),new JavaScriptSerializer().Serialize(manifest),new System.Text.UTF8Encoding(false));
        Console.WriteLine("{0} sprite IDs, {1} transparent PNGs; {2} logical key locations.",sprites.Count,Directory.GetFiles(output,"*.png").Length,keys.Count);
        return 0;
    }
}
