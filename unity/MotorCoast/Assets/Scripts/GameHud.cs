using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace MotorCoast {
public sealed class GameHud : MonoBehaviour {
    public MotorGame Game;
    RectTransform safe;GameObject panel,controls,touchControls,desktopHint;Text speed,gear,activity,message,driveLabel,reverseLabel;Font font;
    readonly Color ink=new Color(.92f,.95f,.92f),muted=new Color(.56f,.66f,.67f),accent=new Color(1f,.76f,.30f);
    static Sprite rounded;static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();WheelPad wheelPad;GameObject arrows;Rect lastSafe;
    public void Build(MotorGame game) {
        Game=game;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
        var scale=gameObject.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=1;
        gameObject.AddComponent<GraphicRaycaster>();
        if(!FindFirstObjectByType<EventSystem>()) {var e=new GameObject("Touch event system",typeof(EventSystem),typeof(StandaloneInputModule));}
        var orbit=Rect("Camera gestures",transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var image=orbit.gameObject.AddComponent<Image>();image.color=new Color(0,0,0,.001f);orbit.gameObject.AddComponent<OrbitPad>().Game=Game;
        safe=Rect("Safe area",transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);UpdateSafeArea();
        controls=Rect("Driving controls",safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject;
        Label(controls.transform,"MOTOR COAST",new Vector2(190,-47),new Vector2(300,40),22,ink,TextAnchor.MiddleLeft,new Vector2(0,1));
        Tool("Пауза",new Vector2(-78,-78),()=>Game.Pause(),"pause");
        Tool("Камера",new Vector2(-222,-78),()=>Game.Camera.Cycle(),"camera");
        Tool("Вернуть машину",new Vector2(-366,-78),()=>Game.ResetCar(),"reset");
        Tool("Гараж",new Vector2(-510,-78),()=>Game.Pause(true),"garage");
        speed=Label(controls.transform,"000",new Vector2(0,108),new Vector2(210,100),88,ink,TextAnchor.MiddleCenter,new Vector2(.5f,0));
        Label(controls.transform,"КМ/Ч",new Vector2(2,48),new Vector2(160,28),19,ink,TextAnchor.MiddleCenter,new Vector2(.5f,0));
        gear=Label(controls.transform,"D",new Vector2(135,112),new Vector2(60,65),34,accent,TextAnchor.MiddleCenter,new Vector2(.5f,0));
        activity=Label(controls.transform,"СВОБОДНОЕ ВОЖДЕНИЕ",new Vector2(0,-47),new Vector2(710,40),20,ink,TextAnchor.MiddleCenter,new Vector2(.5f,1));
        touchControls=Rect("Touch controls",controls.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject;
        var wheel=Hit(touchControls.transform,"Steering wheel",new Vector2(218,218),new Vector2(370,370),Vector2.zero);
        wheelPad=wheel.gameObject.AddComponent<WheelPad>();wheelPad.Input=Game.Input;
        wheelPad.Visual=Art(wheel,"wheel",Vector2.zero,new Vector2(330,330),.88f).rectTransform;
        arrows=Rect("Arrow controls",touchControls.transform,Vector2.zero,Vector2.zero,new Vector2(218,190),new Vector2(370,170)).gameObject;
        SpriteHold(arrows.transform,"left",new Vector2(-94,0),new Vector2(176,176),v=>Game.Input.TouchLeft=v,new Vector2(.5f,.5f));
        SpriteHold(arrows.transform,"right",new Vector2(94,0),new Vector2(176,176),v=>Game.Input.TouchRight=v,new Vector2(.5f,.5f));arrows.SetActive(false);
        SpriteHold(touchControls.transform,"gas",new Vector2(-110,176),new Vector2(160,280),v=>Game.Input.TouchGas=v?1:0,new Vector2(1,0));
        SpriteHold(touchControls.transform,"brake",new Vector2(-306,145),new Vector2(190,218),v=>Game.Input.TouchBrake=v?1:0,new Vector2(1,0));
        var hand=Hit(touchControls.transform,"Handbrake",new Vector2(-310,355),new Vector2(134,134),new Vector2(1,0));
        var handArt=Art(hand,"button-circle",Vector2.zero,new Vector2(116,116),.88f);
        var hold=hand.gameObject.AddComponent<HoldControl>();hold.Visual=handArt;hold.Changed=v=>Game.Input.TouchHandbrake=v?1:0;
        Label(hand,"P",Vector2.zero,new Vector2(75,70),40,ink,TextAnchor.MiddleCenter);
        Label(hand,"РУЧНИК",new Vector2(0,-82),new Vector2(150,26),18,ink,TextAnchor.MiddleCenter);
        driveLabel=GearButton("D",1,new Vector2(-108,540));reverseLabel=GearButton("R",-1,new Vector2(-108,400));
        var horn=Hit(touchControls.transform,"Horn",new Vector2(445,105),new Vector2(96,96),Vector2.zero);
        var hornBg=Art(horn,"button-circle",Vector2.zero,new Vector2(86,86),.8f);Art(horn,"horn",Vector2.zero,new Vector2(35,35));
        var hornButton=horn.gameObject.AddComponent<Button>();hornButton.targetGraphic=hornBg;hornButton.onClick.AddListener(()=>Game.Horn());
        desktopHint=Label(controls.transform,"WASD  /  СТРЕЛКИ\nПРОБЕЛ  ручник     R  сброс     C  камера",new Vector2(365,72),new Vector2(650,100),20,ink,TextAnchor.MiddleLeft,Vector2.zero).gameObject;
        SetTouchLayout(Application.isMobilePlatform||UnityEngine.Input.touchSupported);
        panel=Card(safe,"Menu",Vector2.zero,new Vector2(1040,900),new Vector2(.5f,.5f),.96f).gameObject;
        controls.SetActive(false);Loading("Подготовка побережья…");
    }
    void UpdateSafeArea() {
        var a=Screen.safeArea;if(a==lastSafe)return;lastSafe=a;
        safe.anchorMin=new Vector2(a.xMin/Screen.width,a.yMin/Screen.height);safe.anchorMax=new Vector2(a.xMax/Screen.width,a.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
    }
    void Update() {
        if(!safe)return;UpdateSafeArea();if(!Game.Car)return;
        speed.text=Mathf.RoundToInt(Game.Car.Kph).ToString("000");gear.text=Game.Car.Gear<0?"R":Game.Car.Gear==0?"N":"D";
        driveLabel.color=Game.Input.RequestedGear==1?accent:muted;reverseLabel.color=Game.Input.RequestedGear==-1?accent:muted;
        if(Game.Car.Gear!=Game.Input.RequestedGear)gear.text+="…";
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
        var names=new[]{"ПЛАВНОСТЬ · цель 60 FPS","БАЛАНС · цель 60 FPS","ДЕТАЛИ · цель 60 FPS"};
        for(int i=0;i<3;i++){int q=i;Button(panel.transform,(Game.Quality.Level==i?"✓ ":"")+names[i],new Vector2(0,245-i*100),new Vector2(840,80),()=>{Game.ChangeQuality(q);Settings();});}
        Button(panel.transform,Game.SoundEnabled?"ЗВУК: ВКЛЮЧЁН":"ЗВУК: ВЫКЛЮЧЕН",new Vector2(-215,-80),new Vector2(410,80),()=>{Game.ToggleSound();Settings();});
        bool useArrows=PlayerPrefs.GetInt("steeringArrows",0)==1;
        Button(panel.transform,useArrows?"РУЛЬ: КНОПКИ":"РУЛЬ: ВРАЩЕНИЕ",new Vector2(215,-80),new Vector2(410,80),()=>{SetArrows(!useArrows);Settings();});
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
        if(icon!=null){Art(r,icon,Vector2.zero,new Vector2(46,46));}
        if(!string.IsNullOrEmpty(title))Label(r,title,Vector2.zero,size-new Vector2(20,10),24,ink,TextAnchor.MiddleCenter);
        return b;
    }
    public void SetTouchLayout(bool touch){touchControls.SetActive(touch);desktopHint.SetActive(!touch);}
    RectTransform Hit(Transform parent,string name,Vector2 p,Vector2 size,Vector2 anchor){
        var r=Rect(name,parent,anchor,anchor,p,size);r.gameObject.AddComponent<Image>().color=new Color(0,0,0,.001f);return r;
    }
    static Sprite Artwork(string name){
        if(sprites.TryGetValue(name,out var cached))return cached;
        var t=Resources.Load<Texture2D>("GUI/"+name);if(!t)throw new InvalidOperationException("GUI asset missing: "+name);
        return sprites[name]=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),100);
    }
    Image Art(Transform parent,string name,Vector2 position,Vector2 size,float opacity=1){
        var r=Rect(name,parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),position,size);var image=r.gameObject.AddComponent<Image>();
        image.sprite=Artwork(name);image.preserveAspect=true;image.raycastTarget=false;image.color=new Color(1,1,1,opacity);return image;
    }
    void Tool(string name,Vector2 position,Action action,string icon){
        var r=Hit(controls.transform,name,position,new Vector2(132,132),new Vector2(1,1));var background=Art(r,"button-circle",Vector2.zero,new Vector2(104,104),.85f);
        Art(r,icon,Vector2.zero,new Vector2(42,42));var button=r.gameObject.AddComponent<Button>();button.targetGraphic=background;button.onClick.AddListener(()=>action());
    }
    Text GearButton(string title,int value,Vector2 position){
        var r=Hit(touchControls.transform,"Gear "+title,position,new Vector2(132,132),new Vector2(1,0));var background=Art(r,"button-circle",Vector2.zero,new Vector2(88,88),.8f);
        var button=r.gameObject.AddComponent<Button>();button.targetGraphic=background;button.onClick.AddListener(()=>Game.Input.RequestedGear=value);
        return Label(r,title,Vector2.zero,new Vector2(75,75),38,ink,TextAnchor.MiddleCenter);
    }
    void SpriteHold(Transform parent,string name,Vector2 p,Vector2 size,Action<bool> changed,Vector2 anchor){
        var r=Hit(parent,name,p,size,anchor);var visual=Art(r,name,Vector2.zero,size-new Vector2(24,24),.83f);
        var h=r.gameObject.AddComponent<HoldControl>();h.Visual=visual;h.Changed=changed;
    }
    Text Label(Transform parent,string text,Vector2 p,Vector2 size,int pixels,Color color,TextAnchor alignment,Vector2? anchor=null) {
        var r=Rect(text.Length>40?text.Substring(0,40):text,parent,anchor??new Vector2(.5f,.5f),anchor??new Vector2(.5f,.5f),p,size);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=pixels;t.text=text;t.color=color;t.alignment=alignment;t.raycastTarget=false;t.supportRichText=false;
        var shadow=r.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.85f);shadow.effectDistance=new Vector2(1,-1);return t;
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
public sealed class HoldControl:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,ICancelHandler {
    public Action<bool> Changed;public Image Visual;int pointer=int.MinValue;Color idle;bool initialized;
    public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;if(Visual&&!initialized){idle=Visual.color;initialized=true;}Changed?.Invoke(true);if(Visual)Visual.color=new Color(1,.79f,.42f,1);}
    public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer)Release();}
    public void OnCancel(BaseEventData e)=>Release();
    void OnDisable()=>Release();void OnApplicationFocus(bool focus){if(!focus)Release();}void OnApplicationPause(bool pause){if(pause)Release();}
    void Release(){pointer=int.MinValue;Changed?.Invoke(false);if(Visual&&initialized)Visual.color=idle;}
}
public sealed class WheelPad:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler,ICancelHandler {
    public PlayerInput Input;public RectTransform Visual;int pointer=int.MinValue;float previousAngle,turn,display;
    public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;previousAngle=Angle(e);turn=Input.TouchSteer*140;}
    float Angle(PointerEventData e){RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var p);return Mathf.Atan2(p.y,p.x)*Mathf.Rad2Deg;}
    public void OnDrag(PointerEventData e){if(pointer!=e.pointerId)return;float angle=Angle(e);turn=Mathf.Clamp(turn-Mathf.DeltaAngle(previousAngle,angle),-140,140);previousAngle=angle;Input.TouchSteer=turn/140;}
    public void OnPointerUp(PointerEventData e){if(pointer==e.pointerId)Clear();}
    public void OnCancel(BaseEventData e)=>Clear();
    void OnDisable()=>Clear();void OnApplicationFocus(bool focus){if(!focus)Clear();}void OnApplicationPause(bool pause){if(pause)Clear();}
    void Clear(){pointer=int.MinValue;turn=0;if(Input!=null)Input.TouchSteer=0;}
    void Update(){if(!Visual||Input==null)return;display=Mathf.Lerp(display,Input.TouchSteer,1-Mathf.Exp(-14*Time.unscaledDeltaTime));Visual.localRotation=Quaternion.Euler(0,0,-display*140);}
}
public sealed class OrbitPad:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler {
    public MotorGame Game;readonly Dictionary<int,Vector2> points=new Dictionary<int,Vector2>();float lastDistance;
    public void OnPointerDown(PointerEventData e){if(Game.IsPaused)return;points[e.pointerId]=e.position;lastDistance=Distance();}
    public void OnDrag(PointerEventData e){if(Game.IsPaused||!points.ContainsKey(e.pointerId))return;var old=points[e.pointerId];points[e.pointerId]=e.position;if(points.Count>1){float d=Distance();Game.Camera.Zoom(d-lastDistance);lastDistance=d;}else Game.Camera.Orbit(e.position-old);}
    public void OnPointerUp(PointerEventData e){points.Remove(e.pointerId);lastDistance=Distance();}
    float Distance(){var list=new List<Vector2>(points.Values);return list.Count>1?Vector2.Distance(list[0],list[1]):0;}
    void OnDisable(){points.Clear();}void OnApplicationFocus(bool focus){if(!focus)points.Clear();}
}
}
