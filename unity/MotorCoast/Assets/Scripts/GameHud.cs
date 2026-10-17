using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace MotorCoast {
public sealed class GameHud : MonoBehaviour {
    public MotorGame Game;
    RectTransform safe;GameObject panel,controls;Text speed,gear,activity,message;Font font;
    readonly Color ink=new Color(.92f,.95f,.92f),muted=new Color(.56f,.66f,.67f),accent=new Color(.79f,.91f,.53f);
    static Sprite rounded;WheelPad wheelPad;GameObject arrows;Rect lastSafe;
    public void Build(MotorGame game) {
        Game=game;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
        var scale=gameObject.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=1;
        gameObject.AddComponent<GraphicRaycaster>();
        if(!FindFirstObjectByType<EventSystem>()) {var e=new GameObject("Touch event system",typeof(EventSystem),typeof(StandaloneInputModule));}
        var orbit=Rect("Camera gestures",transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var image=orbit.gameObject.AddComponent<Image>();image.color=new Color(0,0,0,.001f);orbit.gameObject.AddComponent<OrbitPad>().Game=Game;
        safe=Rect("Safe area",transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);UpdateSafeArea();
        controls=Rect("Driving controls",safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject;
        Label(controls.transform,"MOTOR / COAST",new Vector2(210,-48),new Vector2(340,45),28,ink,TextAnchor.MiddleLeft,new Vector2(0,1));
        Button(controls.transform,"",new Vector2(-60,-62),new Vector2(84,84),()=>Game.Pause(),"pause",new Vector2(1,1));
        Button(controls.transform,"",new Vector2(-164,-62),new Vector2(84,84),()=>Game.Camera.Cycle(),"camera",new Vector2(1,1));
        Button(controls.transform,"",new Vector2(-268,-62),new Vector2(84,84),()=>Game.ResetCar(),"reset",new Vector2(1,1));
        Button(controls.transform,"ГАРАЖ",new Vector2(-425,-62),new Vector2(184,84),()=>Game.Pause(true),null,new Vector2(1,1));
        speed=Label(controls.transform,"00",new Vector2(0,122),new Vector2(200,90),76,ink,TextAnchor.MiddleCenter,new Vector2(.5f,0));
        Label(controls.transform,"КМ/Ч",new Vector2(0,68),new Vector2(150,32),19,muted,TextAnchor.MiddleCenter,new Vector2(.5f,0));
        gear=Label(controls.transform,"D",new Vector2(130,126),new Vector2(55,70),32,accent,TextAnchor.MiddleCenter,new Vector2(.5f,0));
        activity=Label(controls.transform,"СВОБОДНОЕ ВОЖДЕНИЕ",new Vector2(0,-48),new Vector2(720,50),23,ink,TextAnchor.MiddleCenter,new Vector2(.5f,1));
        var wheel=Card(controls.transform,"Steering",new Vector2(205,205),new Vector2(300,300),new Vector2(0,0));wheelPad=wheel.gameObject.AddComponent<WheelPad>();wheelPad.Input=Game.Input;
        var glyph=Rect("Steering icon",wheel,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(190,190));glyph.gameObject.AddComponent<IconGraphic>().Kind="wheel";wheelPad.Visual=glyph;
        Label(wheel,"РУЛЬ",new Vector2(0,-115),new Vector2(180,35),18,muted,TextAnchor.MiddleCenter);
        arrows=Rect("Arrow controls",controls.transform,Vector2.zero,Vector2.zero,new Vector2(205,190),new Vector2(350,160)).gameObject;
        Hold(arrows.transform,"",new Vector2(-87,0),new Vector2(156,156),v=>Game.Input.TouchLeft=v,"left");
        Hold(arrows.transform,"",new Vector2(87,0),new Vector2(156,156),v=>Game.Input.TouchRight=v,"right");arrows.SetActive(false);
        Hold(controls.transform,"ГАЗ",new Vector2(-124,190),new Vector2(180,260),v=>Game.Input.TouchGas=v?1:0,"gas",new Vector2(1,0));
        Hold(controls.transform,"ТОРМОЗ",new Vector2(-330,138),new Vector2(180,156),v=>Game.Input.TouchBrake=v?1:0,"brake",new Vector2(1,0));
        Hold(controls.transform,"РУЧНИК",new Vector2(-330,324),new Vector2(180,146),v=>Game.Input.TouchHandbrake=v?1:0,"handbrake",new Vector2(1,0));
        Button(controls.transform,"D / R",new Vector2(-124,398),new Vector2(180,92),()=>{Game.Input.RequestedGear=Game.Input.RequestedGear==1?-1:1;},null,new Vector2(1,0));
        Button(controls.transform,"",new Vector2(405,109),new Vector2(90,90),()=>Game.Horn(),"horn",new Vector2(0,0));
        panel=Card(safe,"Menu",Vector2.zero,new Vector2(1040,900),new Vector2(.5f,.5f),.96f).gameObject;
        controls.SetActive(false);Loading("Подготовка побережья…");
    }
    void UpdateSafeArea() {
        var a=Screen.safeArea;if(a==lastSafe)return;lastSafe=a;
        safe.anchorMin=new Vector2(a.xMin/Screen.width,a.yMin/Screen.height);safe.anchorMax=new Vector2(a.xMax/Screen.width,a.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
    }
    void Update() {
        if(!safe)return;UpdateSafeArea();if(!Game.Car)return;
        speed.text=Mathf.RoundToInt(Game.Car.Kph).ToString("00");gear.text=Game.Car.Gear<0?"R":Game.Car.Gear==0?"N":"D";
        activity.text=Game.Challenges?Game.Challenges.Status:"СВОБОДНОЕ ВОЖДЕНИЕ";
    }
    public void Driving(){panel.SetActive(false);controls.SetActive(true);}
    void ClearPanel(){panel.SetActive(true);controls.SetActive(false);foreach(Transform c in panel.transform){c.gameObject.SetActive(false);Destroy(c.gameObject);}}
    public void Loading(string text){ClearPanel();Label(panel.transform,"MOTOR COAST",new Vector2(0,90),new Vector2(900,100),64,ink,TextAnchor.MiddleCenter);message=Label(panel.transform,text,new Vector2(0,-20),new Vector2(870,100),26,muted,TextAnchor.MiddleCenter);}
    public void Error(string text){Loading("Не удалось загрузить игру.\n"+text);Button(panel.transform,"ПОВТОРИТЬ",new Vector2(0,-180),new Vector2(330,90),()=>Game.Retry());}
    public void Menu(bool first=false,bool garage=false) {
        ClearPanel();
        Label(panel.transform,first?"MOTOR COAST":"ПАУЗА",new Vector2(0,345),new Vector2(900,75),first?62:44,ink,TextAnchor.MiddleCenter);
        Label(panel.transform,first?"Дрифт. Побережье. Свобода.":Game.Car.Spec.label,new Vector2(0,280),new Vector2(900,50),26,muted,TextAnchor.MiddleCenter);
        Button(panel.transform,first?"ПОЕХАЛИ":"ПРОДОЛЖИТЬ",new Vector2(0,185),new Vector2(840,94),()=>Game.Resume());
        if(garage) { Garage();return; }
        Button(panel.transform,"ВЫБРАТЬ МАШИНУ",new Vector2(-215,70),new Vector2(410,82),()=>Menu(false,true));
        Button(panel.transform,"КАЧЕСТВО И ЗВУК",new Vector2(215,70),new Vector2(410,82),Settings);
        Label(panel.transform,"ТЕЛЕФОН\nРуль слева · педали справа\nОбзор: проведите по свободной части экрана\nДва пальца: приближение камеры",new Vector2(-220,-100),new Vector2(420,180),23,muted,TextAnchor.UpperLeft);
        Label(panel.transform,"КЛАВИАТУРА\nWASD / стрелки — управление\nПробел — ручник · R — восстановить\nC — камера · Esc — пауза · H — сигнал",new Vector2(230,-100),new Vector2(440,180),23,muted,TextAnchor.UpperLeft);
        Button(panel.transform,"ОБ ИГРЕ И АВТОРАХ",new Vector2(0,-330),new Vector2(840,68),Credits);
    }
    void Garage() {
        for(int i=0;i<Game.Catalog.vehicles.Length;i++) {
            int index=i;var spec=Game.Catalog.vehicles[i];int row=i/2,col=i%2;
            Button(panel.transform,spec.label,new Vector2(col==0?-215:215,70-row*100),new Vector2(410,82),()=>Game.SelectVehicle(index));
        }
        Button(panel.transform,"НАЗАД",new Vector2(0,-355),new Vector2(840,64),()=>Menu());
    }
    void Settings() {
        ClearPanel();Label(panel.transform,"НАСТРОЙКИ",new Vector2(0,360),new Vector2(900,60),44,ink,TextAnchor.MiddleCenter);
        var names=new[]{"ПЛАВНОСТЬ · цель 60 FPS","БАЛАНС · цель 60 FPS","ДЕТАЛИ · цель 30 FPS"};
        for(int i=0;i<3;i++){int q=i;Button(panel.transform,(Game.Quality.Level==i?"✓ ":"")+names[i],new Vector2(0,245-i*100),new Vector2(840,80),()=>{Game.ChangeQuality(q);Settings();});}
        Button(panel.transform,Game.SoundEnabled?"ЗВУК: ВКЛЮЧЁН":"ЗВУК: ВЫКЛЮЧЕН",new Vector2(-215,-80),new Vector2(410,80),()=>{Game.ToggleSound();Settings();});
        bool useArrows=PlayerPrefs.GetInt("steeringArrows",0)==1;
        Button(panel.transform,useArrows?"РУЛЬ: КНОПКИ":"РУЛЬ: СВАЙП",new Vector2(215,-80),new Vector2(410,80),()=>{SetArrows(!useArrows);Settings();});
        Label(panel.transform,"Автоматическое снижение разрешения при перегрузке.\nТекущий рендер: "+Game.Quality.ResolutionLabel+"\nСредняя частота в игре: "+Game.Quality.MeasuredFps.ToString("0.0")+" FPS",new Vector2(0,-205),new Vector2(840,110),23,muted,TextAnchor.MiddleCenter);
        Button(panel.transform,"НАЗАД",new Vector2(0,-350),new Vector2(840,70),()=>Menu());
    }
    public void SetArrows(bool value){PlayerPrefs.SetInt("steeringArrows",value?1:0);Game.Input.Clear();arrows.SetActive(value);wheelPad.gameObject.SetActive(!value);}
    void Credits() {
        ClearPanel();Label(panel.transform,"ОБ ИГРЕ",new Vector2(0,370),new Vector2(900,70),44,ink,TextAnchor.MiddleCenter);
        var viewport=Rect("Credits scroll",panel.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,5),new Vector2(860,620));viewport.gameObject.AddComponent<RectMask2D>();viewport.gameObject.AddComponent<Image>().color=new Color(0,0,0,.001f);
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        var content=Rect("Credits",viewport,new Vector2(0,1),new Vector2(1,1),Vector2.zero,new Vector2(0,4800));content.pivot=new Vector2(.5f,1);scroll.content=content;scroll.viewport=viewport;
        var text=Label(content,Resources.Load<TextAsset>("Data/CREDITS").text,Vector2.zero,new Vector2(820,4800),22,muted,TextAnchor.UpperLeft,new Vector2(.5f,1));text.rectTransform.pivot=new Vector2(.5f,1);text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;
        Button(panel.transform,"НАЗАД",new Vector2(0,-370),new Vector2(840,70),()=>Menu());
    }
    RectTransform Card(Transform parent,string name,Vector2 p,Vector2 size,Vector2 anchor,float alpha=.62f) {
        var r=Rect(name,parent,anchor,anchor,p,size);var i=r.gameObject.AddComponent<Image>();i.sprite=RoundSprite();i.type=Image.Type.Sliced;i.color=new Color(.025f,.045f,.052f,alpha);return r;
    }
    Button Button(Transform parent,string title,Vector2 p,Vector2 size,Action action,string icon=null,Vector2? anchor=null) {
        var r=Card(parent,title,p,size,anchor??new Vector2(.5f,.5f),.88f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.onClick.AddListener(()=>action());
        var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.8f,.92f,.65f);colors.pressedColor=new Color(.54f,.70f,.48f);b.colors=colors;
        if(icon!=null){var g=Rect("Icon",r,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(46,46));g.gameObject.AddComponent<IconGraphic>().Kind=icon;}
        if(!string.IsNullOrEmpty(title))Label(r,title,Vector2.zero,size-new Vector2(20,10),24,ink,TextAnchor.MiddleCenter);
        return b;
    }
    void Hold(Transform parent,string title,Vector2 p,Vector2 size,Action<bool> action,string icon,Vector2? anchor=null) {
        var r=Card(parent,title,p,size,anchor??new Vector2(.5f,.5f));var h=r.gameObject.AddComponent<HoldControl>();h.Changed=action;
        var g=Rect("Icon",r,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,string.IsNullOrEmpty(title)?0:16),new Vector2(62,62));g.gameObject.AddComponent<IconGraphic>().Kind=icon;
        if(!string.IsNullOrEmpty(title))Label(r,title,new Vector2(0,-size.y*.5f+30),new Vector2(size.x,36),19,ink,TextAnchor.MiddleCenter);
    }
    Text Label(Transform parent,string text,Vector2 p,Vector2 size,int pixels,Color color,TextAnchor alignment,Vector2? anchor=null) {
        var r=Rect(text.Length>40?text.Substring(0,40):text,parent,anchor??new Vector2(.5f,.5f),anchor??new Vector2(.5f,.5f),p,size);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=pixels;t.text=text;t.color=color;t.alignment=alignment;t.raycastTarget=false;t.supportRichText=false;return t;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 p,Vector2 size) {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.anchoredPosition=p;r.sizeDelta=size;return r;
    }
    static Sprite RoundSprite() {
        if(rounded)return rounded;const int n=48;var t=new Texture2D(n,n,TextureFormat.RGBA32,false);var pixels=new Color[n*n];
        for(int y=0;y<n;y++)for(int x=0;x<n;x++){var d=new Vector2(Mathf.Max(Mathf.Abs(x-23.5f)-11.5f,0),Mathf.Max(Mathf.Abs(y-23.5f)-11.5f,0));pixels[y*n+x]=new Color(1,1,1,Mathf.Clamp01(12-d.magnitude));}
        t.SetPixels(pixels);t.Apply();rounded=Sprite.Create(t,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(18,18,18,18));return rounded;
    }
}
public sealed class HoldControl:MonoBehaviour,IPointerDownHandler,IPointerUpHandler {
    public Action<bool> Changed;int pointer=int.MinValue;
    public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;Changed?.Invoke(true);GetComponent<Image>().color=new Color(.23f,.36f,.26f,.9f);}
    public void OnPointerUp(PointerEventData e){if(e.pointerId!=pointer)return;Release();}
    void OnDisable()=>Release();
    void Release(){pointer=int.MinValue;Changed?.Invoke(false);var i=GetComponent<Image>();if(i)i.color=new Color(.025f,.045f,.052f,.62f);}
}
public sealed class WheelPad:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler {
    public PlayerInput Input;public RectTransform Visual;int pointer=int.MinValue;float start;
    public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var p);start=p.x;}
    public void OnDrag(PointerEventData e){if(pointer!=e.pointerId)return;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var p);Input.TouchSteer=Mathf.Clamp((p.x-start)/100,-1,1);}
    public void OnPointerUp(PointerEventData e){if(pointer==e.pointerId)Clear();}
    void OnDisable()=>Clear();
    void Clear(){pointer=int.MinValue;if(Input!=null)Input.TouchSteer=0;}
    void Update(){if(Visual&&Input!=null)Visual.localRotation=Quaternion.Euler(0,0,-Input.TouchSteer*115);}
}
public sealed class OrbitPad:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler {
    public MotorGame Game;readonly Dictionary<int,Vector2> points=new Dictionary<int,Vector2>();float lastDistance;
    public void OnPointerDown(PointerEventData e){if(Game.IsPaused)return;points[e.pointerId]=e.position;lastDistance=Distance();}
    public void OnDrag(PointerEventData e){if(Game.IsPaused||!points.ContainsKey(e.pointerId))return;var old=points[e.pointerId];points[e.pointerId]=e.position;if(points.Count>1){float d=Distance();Game.Camera.Zoom(d-lastDistance);lastDistance=d;}else Game.Camera.Orbit(e.position-old);}
    public void OnPointerUp(PointerEventData e){points.Remove(e.pointerId);lastDistance=Distance();}
    float Distance(){var list=new List<Vector2>(points.Values);return list.Count>1?Vector2.Distance(list[0],list[1]):0;}
    void OnDisable(){points.Clear();}void OnApplicationFocus(bool focus){if(!focus)points.Clear();}
}
public sealed class IconGraphic:Graphic {
    public string Kind="camera";
    protected override void OnPopulateMesh(VertexHelper v) {
        v.Clear();float s=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
        void Line(Vector2 a,Vector2 b,float w=.07f){a*=s;b*=s;var n=new Vector2(-(b-a).y,(b-a).x).normalized*w*s;int k=v.currentVertCount;var c=new Color(.9f,.95f,.9f);v.AddVert(a-n,c,Vector2.zero);v.AddVert(a+n,c,Vector2.zero);v.AddVert(b+n,c,Vector2.zero);v.AddVert(b-n,c,Vector2.zero);v.AddTriangle(k,k+1,k+2);v.AddTriangle(k,k+2,k+3);}
        void Circle(float r){for(int i=0;i<40;i++){float a=i*2*Mathf.PI/40,b=(i+1)*2*Mathf.PI/40;Line(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r,.045f);}}
        raycastTarget=false;
        if(Kind=="pause"){Line(new Vector2(-.3f,-.6f),new Vector2(-.3f,.6f),.12f);Line(new Vector2(.3f,-.6f),new Vector2(.3f,.6f),.12f);}
        else if(Kind=="left"||Kind=="right"){float sign=Kind=="right"?1:-1;Line(new Vector2(-.3f*sign,.65f),new Vector2(.35f*sign,0));Line(new Vector2(.35f*sign,0),new Vector2(-.3f*sign,-.65f));}
        else if(Kind=="wheel"||Kind=="handbrake"){Circle(.8f);Circle(.2f);Line(new Vector2(-.76f,0),new Vector2(.76f,0));Line(Vector2.zero,new Vector2(0,-.76f));}
        else if(Kind=="reset"){Circle(.65f);Line(new Vector2(-.8f,.8f),new Vector2(-.8f,.2f));Line(new Vector2(-.8f,.2f),new Vector2(-.2f,.2f));}
        else if(Kind=="camera"){Circle(.3f);Line(new Vector2(-.8f,-.55f),new Vector2(.8f,-.55f));Line(new Vector2(.8f,-.55f),new Vector2(.8f,.55f));Line(new Vector2(.8f,.55f),new Vector2(-.8f,.55f));Line(new Vector2(-.8f,.55f),new Vector2(-.8f,-.55f));}
        else if(Kind=="horn"){Line(new Vector2(-.8f,-.3f),new Vector2(-.8f,.3f));Line(new Vector2(-.8f,.3f),new Vector2(.15f,.65f));Line(new Vector2(.15f,.65f),new Vector2(.15f,-.65f));Line(new Vector2(.15f,-.65f),new Vector2(-.8f,-.3f));Line(new Vector2(.5f,-.4f),new Vector2(.7f,0));Line(new Vector2(.7f,0),new Vector2(.5f,.4f));}
        else {float width=Kind=="gas"?.35f:.75f;Line(new Vector2(-width,-.7f),new Vector2(-width,.7f));Line(new Vector2(width,-.7f),new Vector2(width,.7f));Line(new Vector2(-width,.7f),new Vector2(width,.7f));Line(new Vector2(-width,-.7f),new Vector2(width,-.7f));for(int i=0;i<4;i++)Line(new Vector2(-width+.12f,-.45f+i*.3f),new Vector2(width-.12f,-.45f+i*.3f),.04f);}
    }
}
}
