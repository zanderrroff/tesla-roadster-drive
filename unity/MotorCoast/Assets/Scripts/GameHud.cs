using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace MotorCoast {
public sealed class GameHud : MonoBehaviour {
    public MotorGame Game;
    RectTransform safe;GameObject panel,controls,touchControls,desktopHint,footControls;Text speed,gear,activity,message,driveLabel,reverseLabel,fps,speedUnit,contextLabel;Font font;bool touchLayout;int garagePage,characterPage;
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
        fps=Label(controls.transform,"",new Vector2(190,-92),new Vector2(300,38),22,accent,TextAnchor.MiddleLeft,new Vector2(0,1));
        Tool("Пауза",new Vector2(-78,-78),()=>Game.Pause(),"pause");
        Tool("Камера",new Vector2(-222,-78),()=>Game.Camera.Cycle(),"camera");
        Tool("Вернуть машину",new Vector2(-366,-78),()=>{if(Game.OnFoot)Game.RecoverPerson();else Game.ResetCar();},"reset");
        Tool("Гараж",new Vector2(-510,-78),()=>Game.Pause(true),"garage");
        speed=Label(controls.transform,"000",new Vector2(0,108),new Vector2(210,100),88,ink,TextAnchor.MiddleCenter,new Vector2(.5f,0));
        speedUnit=Label(controls.transform,"КМ/Ч",new Vector2(2,48),new Vector2(260,28),19,ink,TextAnchor.MiddleCenter,new Vector2(.5f,0));
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
        var interact=Hit(controls.transform,"Enter or exit vehicle",new Vector2(-108,712),new Vector2(142,142),new Vector2(1,0));
        var contextBackground=Art(interact,"button-circle",Vector2.zero,new Vector2(108,108),.85f);Art(interact,"log-out",Vector2.zero,new Vector2(45,45));
        var contextButton=interact.gameObject.AddComponent<Button>();contextButton.targetGraphic=contextBackground;contextButton.onClick.AddListener(()=>Game.Interact());
        contextLabel=Label(interact,"ВЫЙТИ · E",new Vector2(0,-80),new Vector2(190,30),19,ink,TextAnchor.MiddleCenter);
        footControls=Rect("On-foot controls",controls.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject;
        var joystick=Hit(footControls.transform,"Movement joystick",new Vector2(210,210),new Vector2(340,340),Vector2.zero);Art(joystick,"button-circle",Vector2.zero,new Vector2(300,300),.30f);
        var stick=joystick.gameObject.AddComponent<MovePad>();stick.Input=Game.Input;stick.Knob=Art(joystick,"button-circle",Vector2.zero,new Vector2(130,130),.85f).rectTransform;
        FootButton("ПРЫЖОК", "arrow-up",new Vector2(-116,180),()=>Game.Person.Jump());
        FootButton("ПРИСЕСТЬ", "arrow-down",new Vector2(-300,105),()=>Game.Person.ToggleCrouch());
        FootButton("ПОЛЗТИ", "move-horizontal",new Vector2(-480,185),()=>Game.Person.ToggleProne());
        var sprint=FootButton("БЕГ", "chevrons-up",new Vector2(-300,335),null);sprint.gameObject.AddComponent<HoldControl>().Changed=v=>Game.Input.TouchRun=v;
        FootButton("ЭМОЦИИ", "smile",new Vector2(-485,380),Emotes);
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
        fps.text=Game.ShowFps&&Game.Quality?(Game.Quality.MeasuredFps>0?Game.Quality.MeasuredFps.ToString("0")+" FPS":"FPS · измерение…"):"";
        speed.fontSize=Game.OnFoot?32:88;speed.text=Game.OnFoot?(Game.Person.Prone?"ПОЛЗКОМ":Game.Person.Crouched?"ПРИСЕД":Game.Person.HorizontalSpeed>3?"БЕГ":"ПЕШКОМ"):Mathf.RoundToInt(Game.Car.Kph).ToString("000");gear.gameObject.SetActive(!Game.OnFoot);speedUnit.text=Game.OnFoot?(touchLayout?"ИССЛЕДОВАНИЕ":"E — СЕСТЬ В МАШИНУ"):"КМ/Ч";contextLabel.text=Game.OnFoot?(touchLayout?"СЕСТЬ":"СЕСТЬ · E"):(touchLayout?"ВЫЙТИ":"ВЫЙТИ · E");gear.text=Game.Car.Gear<0?"R":Game.Car.Gear==0?"N":"D";
        driveLabel.color=Game.Input.RequestedGear==1?accent:muted;reverseLabel.color=Game.Input.RequestedGear==-1?accent:muted;
        if(Game.Car.Gear!=Game.Input.RequestedGear)gear.text+="…";
        activity.text=!string.IsNullOrEmpty(Game.Notice)?Game.Notice:Game.OnFoot?WorldCatalog.Maps[Game.MapIndex].Name.ToUpper()+"  /  ИССЛЕДОВАНИЕ":Game.Challenges?Game.Challenges.Status:"СВОБОДНОЕ ВОЖДЕНИЕ";
    }
    public void Driving(){ClearPortraits();panel.SetActive(false);controls.SetActive(true);SetTouchLayout(touchLayout);if(desktopHint)desktopHint.GetComponent<Text>().text=Game.OnFoot?"WASD — идти   SHIFT — бежать   E — сесть\nПРОБЕЛ — прыжок / уступ   CTRL — присесть   X — ползти   F — эмоции":"WASD / СТРЕЛКИ   ПРОБЕЛ — ручник\nR — сброс   C — камера   E — выйти";}
    void ClearPortraits(){foreach(var p in panel.GetComponentsInChildren<CharacterPreview>(true))Destroy(p.gameObject);}
    void ClearPanel(){panel.SetActive(true);controls.SetActive(false);foreach(Transform c in panel.transform){c.gameObject.SetActive(false);Destroy(c.gameObject);}}
    public void Loading(string text){ClearPanel();Label(panel.transform,"MOTOR COAST",new Vector2(0,90),new Vector2(900,100),64,ink,TextAnchor.MiddleCenter);message=Label(panel.transform,text,new Vector2(0,-20),new Vector2(870,100),26,muted,TextAnchor.MiddleCenter);}
    public void LoadingProgress(string text){if(message)message.text=text;}
    public void Error(string text){Loading("Не удалось загрузить игру.\n"+text);Button(panel.transform,"ПОВТОРИТЬ",new Vector2(0,-180),new Vector2(330,90),()=>Game.Retry());}
    public void Menu(bool first=false,bool garage=false) {
        ClearPanel();
        Label(panel.transform,first?"MOTOR COAST":"ПАУЗА",new Vector2(0,345),new Vector2(900,75),first?62:44,ink,TextAnchor.MiddleCenter);
        Label(panel.transform,first?"Дрифт. Побережье. Свобода.":Game.Car.Spec.label,new Vector2(0,280),new Vector2(900,50),26,muted,TextAnchor.MiddleCenter);
        Button(panel.transform,first?"ПОЕХАЛИ":"ПРОДОЛЖИТЬ",new Vector2(0,185),new Vector2(840,94),()=>Game.Resume());
        if(garage) { Garage();return; }
        Button(panel.transform,"ВЫБРАТЬ МАШИНУ",new Vector2(-215,70),new Vector2(410,82),()=>Menu(false,true));
        Button(panel.transform,"КАЧЕСТВО И ЗВУК",new Vector2(215,70),new Vector2(410,82),Settings);
        Button(panel.transform,"КАРТЫ",new Vector2(-215,-35),new Vector2(410,82),Maps);
        Button(panel.transform,"ПЕРСОНАЖ",new Vector2(215,-35),new Vector2(410,82),Characters);
        Label(panel.transform,"ТЕЛЕФОН\nРуль слева · педали справа\nПроведите по экрану для обзора\nПешком: джойстик и кнопки действий",new Vector2(-220,-170),new Vector2(420,180),22,muted,TextAnchor.UpperLeft);
        Label(panel.transform,"КЛАВИАТУРА\nWASD · E — выйти / сесть\nПробел — ручник / прыжок\nR — сброс · C — камера · Esc — пауза",new Vector2(230,-170),new Vector2(440,180),22,muted,TextAnchor.UpperLeft);
        Button(panel.transform,"ОБ ИГРЕ И АВТОРАХ",new Vector2(0,-330),new Vector2(840,68),Credits);
    }
    void Garage() {
        ClearPanel();Label(panel.transform,"ГАРАЖ · "+Game.Catalog.vehicles.Length+" МАШИН",new Vector2(0,365),new Vector2(920,60),36,ink,TextAnchor.MiddleCenter);
        int start=garagePage*10;
        for(int i=start;i<Mathf.Min(start+10,Game.Catalog.vehicles.Length);i++) {
            int index=i;var spec=Game.Catalog.vehicles[i];int row=i/2,col=i%2;
            Button(panel.transform,spec.label,new Vector2(col==0?-215:215,255-(i-start)/2*100),new Vector2(410,82),()=>Game.SelectVehicle(index));
        }
        if(start>0)Button(panel.transform,"←",new Vector2(-380,-275),new Vector2(90,64),()=>{garagePage--;Garage();});
        if(start+10<Game.Catalog.vehicles.Length)Button(panel.transform,"→",new Vector2(380,-275),new Vector2(90,64),()=>{garagePage++;Garage();});
        Button(panel.transform,"НАЗАД",new Vector2(0,-355),new Vector2(840,64),()=>Menu());
    }
    void Maps(){
        ClearPanel();Label(panel.transform,"ВЫБЕРИТЕ МАРШРУТ",new Vector2(0,365),new Vector2(920,65),36,ink,TextAnchor.MiddleCenter);
        for(int i=0;i<WorldCatalog.Maps.Length;i++){int index=i;var map=WorldCatalog.Maps[i];Button(panel.transform,(Game.MapIndex==i?"✓ ":"")+map.Name,new Vector2(i%2==0?-215:215,255-i/2*107),new Vector2(410,72),()=>Game.SelectMap(index));Label(panel.transform,map.Description,new Vector2(i%2==0?-215:215,205-i/2*107),new Vector2(410,30),16,muted,TextAnchor.MiddleCenter);}
        Button(panel.transform,"НАЗАД",new Vector2(0,-355),new Vector2(840,64),()=>Menu());
    }
    void Characters(){
        ClearPanel();int selected=Mathf.Clamp(PlayerPrefs.GetInt("character",0),0,Game.Characters.characters.Length-1);
        Label(panel.transform,"ПЕРСОНАЖИ · "+Game.Characters.characters.Length,new Vector2(0,365),new Vector2(920,65),36,ink,TextAnchor.MiddleCenter);
        var portrait=Rect("Character preview",panel.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-255,-20),new Vector2(390,575));portrait.gameObject.AddComponent<RawImage>().raycastTarget=false;var preview=portrait.gameObject.AddComponent<CharacterPreview>();preview.Show(Game.Characters.characters[selected].resource,Game.AnimationsEnabled);
        int chosen=selected;int start=characterPage*10;
        for(int i=start;i<Mathf.Min(start+10,Game.Characters.characters.Length);i++){
            int index=i;string title=(i+1).ToString("00")+" · "+(Game.Characters.characters[i].id.Contains("Female")?"Ж":"М");
            Button(panel.transform,title,new Vector2(90+(i-start)%2*190,260-(i-start)/2*82),new Vector2(174,66),()=>{chosen=index;preview.Show(Game.Characters.characters[index].resource,Game.AnimationsEnabled);});
        }
        Button(panel.transform,"←",new Vector2(82,-178),new Vector2(100,64),()=>{characterPage=(characterPage+(Game.Characters.characters.Length+9)/10-1)%((Game.Characters.characters.Length+9)/10);Characters();});
        Button(panel.transform,"→",new Vector2(290,-178),new Vector2(100,64),()=>{characterPage=(characterPage+1)%((Game.Characters.characters.Length+9)/10);Characters();});
        Label(panel.transform,"ЦВЕТ ОДЕЖДЫ",new Vector2(190,-242),new Vector2(340,30),19,muted,TextAnchor.MiddleCenter);
        for(int i=0;i<CharacterVisual.OutfitColors.Length;i++){int color=i;var b=Button(panel.transform,"",new Vector2(45+i*57,-290),new Vector2(48,48),()=>Game.SetOutfit(color));b.targetGraphic.color=CharacterVisual.OutfitColors[i];}
        Button(panel.transform,"ВЫБРАТЬ",new Vector2(200,-370),new Vector2(370,68),()=>Game.SelectCharacter(chosen));Button(panel.transform,"НАЗАД",new Vector2(-230,-370),new Vector2(370,68),()=>Menu());
    }
    public void Emotes(){
        if(!Game.OnFoot)return;
        if(!Game.AnimationsEnabled){Game.Notify("Анимации отключены в настройках");return;}
        Game.Pause();ClearPanel();Label(panel.transform,"ЭМОЦИИ",new Vector2(0,330),new Vector2(900,65),40,ink,TextAnchor.MiddleCenter);
        var names=new[]{"ТАНЕЦ","ПРИВЕТСТВИЕ","АПЛОДИСМЕНТЫ","РАДОСТЬ","СМЕХ","ТАНЕЦ 2"};var clips=new[]{"Dance","Wave","Clap","Cheer","Laugh","FemaleDance"};
        for(int i=0;i<clips.Length;i++){string clip=clips[i];Button(panel.transform,names[i],new Vector2(i%2==0?-215:215,180-i/2*120),new Vector2(410,92),()=>{Game.Resume();Game.Person.Emote(clip);});}
        Button(panel.transform,"НАЗАД",new Vector2(0,-330),new Vector2(840,70),()=>Game.Resume());
    }
    void Settings() {
        ClearPanel();Label(panel.transform,"НАСТРОЙКИ",new Vector2(0,360),new Vector2(900,60),44,ink,TextAnchor.MiddleCenter);
        var names=new[]{"СТАНДАРТ","УЛЬТРА"};
        for(int i=0;i<2;i++){int q=i;Button(panel.transform,(Game.Quality.Level==i?"✓ ":"")+names[i],new Vector2(i==0?-215:215,245),new Vector2(410,86),()=>{Game.ChangeQuality(q);if(!Game.IsLoading)Settings();});}
        Label(panel.transform,Game.Quality.Ultra?"Детальная модель · контактные тени · отражения · 4× MSAA":"Мягкие тени · HDR · облегчённые текстуры · 2× MSAA",new Vector2(0,169),new Vector2(880,50),22,muted,TextAnchor.MiddleCenter);
        Button(panel.transform,Game.SoundEnabled?"ЗВУК: ВКЛЮЧЁН":"ЗВУК: ВЫКЛЮЧЕН",new Vector2(-215,65),new Vector2(410,80),()=>{Game.ToggleSound();Settings();});
        bool useArrows=PlayerPrefs.GetInt("steeringArrows",0)==1;
        Button(panel.transform,useArrows?"РУЛЬ: КНОПКИ":"РУЛЬ: ВРАЩЕНИЕ",new Vector2(215,65),new Vector2(410,80),()=>{SetArrows(!useArrows);Settings();});
        Button(panel.transform,Game.ShowFps?"СЧЁТЧИК FPS: ВКЛ":"СЧЁТЧИК FPS: ВЫКЛ",new Vector2(-215,-40),new Vector2(410,80),()=>{Game.ToggleFps();Settings();});
        Button(panel.transform,Game.AnimationsEnabled?"АНИМАЦИИ: ВКЛ":"АНИМАЦИИ: ВЫКЛ",new Vector2(215,-40),new Vector2(410,80),()=>{Game.ToggleAnimations();Settings();});
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
        float height=Mathf.Max(620,text.preferredHeight+40);content.sizeDelta=new Vector2(0,height);text.rectTransform.sizeDelta=new Vector2(820,height);
        Button(panel.transform,"НАЗАД",new Vector2(0,-370),new Vector2(840,70),()=>Menu());
    }
    RectTransform Card(Transform parent,string name,Vector2 p,Vector2 size,Vector2 anchor,float alpha=.62f) {
        var r=Rect(name,parent,anchor,anchor,p,size);var i=r.gameObject.AddComponent<Image>();i.sprite=PanelSprite();i.type=Image.Type.Sliced;i.color=new Color(.055f,.075f,.082f,alpha);return r;
    }
    Button Button(Transform parent,string title,Vector2 p,Vector2 size,Action action,string icon=null,Vector2? anchor=null) {
        var r=Card(parent,title,p,size,anchor??new Vector2(.5f,.5f),.88f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.onClick.AddListener(()=>action());
        var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.8f,.92f,.65f);colors.pressedColor=new Color(.54f,.70f,.48f);b.colors=colors;
        if(icon!=null){Art(r,icon,Vector2.zero,new Vector2(46,46));}
        if(!string.IsNullOrEmpty(title))Label(r,title,Vector2.zero,size-new Vector2(20,10),24,ink,TextAnchor.MiddleCenter);
        return b;
    }
    public void SetTouchLayout(bool touch){touchLayout=touch;touchControls.SetActive(touch&&!Game.OnFoot);footControls.SetActive(touch&&Game.OnFoot);desktopHint.SetActive(!touch);}
    RectTransform FootButton(string title,string icon,Vector2 p,Action action){var r=Hit(footControls.transform,title,p,new Vector2(150,150),new Vector2(1,0));var bg=Art(r,"button-circle",Vector2.zero,new Vector2(116,116),.85f);Art(r,icon,Vector2.zero,new Vector2(46,46));Label(r,title,new Vector2(0,-78),new Vector2(190,28),19,ink,TextAnchor.MiddleCenter);if(action!=null){var b=r.gameObject.AddComponent<Button>();b.targetGraphic=bg;b.onClick.AddListener(()=>action());}return r;}
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
    static Sprite PanelSprite() {if(rounded)return rounded;var t=Resources.Load<Texture2D>("GUI/panel");if(!t)throw new InvalidOperationException("Kenney UI panel missing");rounded=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(10,10,10,10));return rounded;}
}
public sealed class MovePad:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler,ICancelHandler {
    public PlayerInput Input;public RectTransform Knob;int pointer=int.MinValue;
    public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;OnDrag(e);}
    public void OnDrag(PointerEventData e){if(e.pointerId!=pointer||Input.Blocked)return;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var point);Input.TouchMove=Vector2.ClampMagnitude(point/115,1);if(Knob)Knob.anchoredPosition=Input.TouchMove*100;}
    public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer)Release();}
    public void OnCancel(BaseEventData e)=>Release();void OnDisable()=>Release();void OnApplicationFocus(bool focus){if(!focus)Release();}void OnApplicationPause(bool paused){if(paused)Release();}
    void Release(){pointer=int.MinValue;if(Input!=null)Input.TouchMove=Vector2.zero;if(Knob)Knob.anchoredPosition=Vector2.zero;}
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
public sealed class OrbitPad:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler,ICancelHandler {
    public MotorGame Game;readonly Dictionary<int,Vector2> points=new Dictionary<int,Vector2>();float lastDistance;
    public void OnPointerDown(PointerEventData e){if(Game.IsPaused)return;points[e.pointerId]=e.position;lastDistance=Distance();}
    public void OnDrag(PointerEventData e){if(Game.IsPaused||!points.ContainsKey(e.pointerId))return;var old=points[e.pointerId];points[e.pointerId]=e.position;if(points.Count>1){float d=Distance();Game.Camera.Zoom(d-lastDistance);lastDistance=d;}else Game.Camera.Orbit(e.position-old);}
    public void OnPointerUp(PointerEventData e){points.Remove(e.pointerId);lastDistance=Distance();}
    public void OnCancel(BaseEventData e){points.Clear();lastDistance=0;}
    float Distance(){var list=new List<Vector2>(points.Values);return list.Count>1?Vector2.Distance(list[0],list[1]):0;}
    void OnDisable(){points.Clear();}void OnApplicationFocus(bool focus){if(!focus)points.Clear();}
}
}
