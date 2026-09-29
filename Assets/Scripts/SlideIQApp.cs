using System;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public sealed class SlideIQApp : MonoBehaviour
{
    public bool startInGame;
    static readonly Color Ink=new Color(.04f,.07f,.16f), Green=new Color(.07f,.14f,.25f), Gold=new Color(.95f,.77f,.35f);
    static readonly Color[] Palette={new Color(1,.48f,.45f),new Color(.35f,.7f,1),new Color(.4f,.9f,.6f),new Color(1,.83f,.3f),new Color(.8f,.55f,1),new Color(1,.64f,.3f),new Color(.4f,.95f,.92f),new Color(1,.65f,.85f)};
    static readonly string[] Levels={"Beginner","Intermediate","Advanced"};
    Canvas canvas; RectTransform root; GameObject screen,modal;
    TMP_FontAsset font; IQMusic music; TMP_Text timer; SlideProgress active;
    bool playing; int lastWidth,lastHeight; float width,height; UnityEngine.Rect lastSafe;
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void SlideDownload(string filename,string json);
    [DllImport("__Internal")] static extern void SlidePickFile(string receiver);
    [DllImport("__Internal")] static extern void SlideOpenGames();
#endif
    void Awake()
    {
        gameObject.name="SlideIQ";
        font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if(font==null)font=TMP_Settings.defaultFontAsset;
        if(EventSystem.current==null)
        {
            var e=new GameObject("EventSystem",typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            e.AddComponent<InputSystemUIInputModule>();
#else
            e.AddComponent<StandaloneInputModule>();
#endif
        }
        var cg=new GameObject("SlideCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas=cg.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var bg=Rect("Backdrop",cg.transform);Stretch(bg);bg.gameObject.AddComponent<Image>().color=Green;
        root=Rect("Safe layout",cg.transform);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);
        music=IQMusic.GetPlayer();gameObject.AddComponent<AudioListener>();
        var camera=gameObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Green;camera.cullingMask=0;
        active=SlideRules.New(0,1);playing=startInGame;Build();
    }
    void Update()
    {
        if(Screen.width!=lastWidth || Screen.height!=lastHeight || Screen.safeArea!=lastSafe)Build();
        if(playing && modal==null && !SlideRules.Finished(active))active.seconds+=Time.unscaledDeltaTime;
        if(timer!=null){var t=TimeSpan.FromSeconds(active.seconds);timer.text=$"TIME  {(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";}
    }
    RectTransform Rect(string name,Transform parent)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false); return r;
    }
    static void Stretch(RectTransform r)
    { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
    static void Place(RectTransform r,float x,float y,float w,float h)
    { r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); }
    TMP_Text Text(Transform parent,string text,float x,float y,float w,float h,float size=24,Color? color=null)
    {
        var r=Rect("Label",parent); Place(r,x,y,w,h); var t=r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=font; t.text=text; t.fontSize=size; t.color=color??Color.white; t.alignment=TextAlignmentOptions.Center;
        t.enableAutoSizing=true; t.fontSizeMin=Math.Min(12,size); t.fontSizeMax=size; t.raycastTarget=false; t.richText=false;
        return t;
    }
    Button Button(Transform parent,string caption,float x,float y,float w,float h,Action action,Color? background=null)
    {
        var r=Rect(caption,parent); Place(r,x,y,w,h); var img=r.gameObject.AddComponent<Image>(); img.color=background??Color.white;
        var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=img;
        var nav=b.navigation; nav.mode=Navigation.Mode.None; b.navigation=nav;
        var colors=b.colors; colors.highlightedColor=new Color(.88f,.94f,.87f); colors.pressedColor=Gold; b.colors=colors;
        b.onClick.AddListener(()=>{music.Begin();action();});
        var t=Text(r,caption,4,2,w-8,h-4,22,Color.black); t.fontStyle=FontStyles.Bold; return b;
    }
    void Build()
    {
        lastWidth=Screen.width;lastHeight=Screen.height;lastSafe=Screen.safeArea;
        bool portrait=Screen.height>Screen.width;width=portrait?650:1120;height=portrait?1140:840;
        var safe=lastSafe.width>0 && lastSafe.height>0?lastSafe:new UnityEngine.Rect(0,0,Screen.width,Screen.height);
        canvas.scaleFactor=Mathf.Max(.01f,Mathf.Min(safe.width/width,safe.height/height));
        canvas.GetComponent<CanvasScaler>().scaleFactor=canvas.scaleFactor;
        root.anchoredPosition=(safe.center-new Vector2(Screen.width/2f,Screen.height/2f))/canvas.scaleFactor;
        root.sizeDelta=new Vector2(width,height);
        if(screen!=null){screen.SetActive(false);Destroy(screen);}
        if(modal!=null)CloseModal();
        var r=Rect("Screen",root);Stretch(r);screen=r.gameObject;timer=null;
        if(playing)BuildGame(r,portrait);else BuildMenu(r);
    }
    void Help()
    {
        Message("How to play SlideIQ","Arrange the numbered tiles from left to right, top to bottom, with the empty space at the bottom right. Tap a tile next to the empty space to slide it. Only horizontal and vertical moves are allowed.\n\nBeginner: 3 x 3 board. Intermediate: 4 x 4. Advanced: 5 x 5. Every challenge is generated with legal moves, so every starting board can be solved.\n\nAim for fewer moves and a faster time. RESET restarts the same challenge. REVEAL shows the goal and ends the attempt; it does not count as a win.\n\nShare the level and challenge number to play the same board. Export before closing and import to resume.");
    }

    void Change(int level,int number)
    {
        Action change=()=>{active=SlideRules.New(level,number);Build();};
        if(active.moves>0 && !SlideRules.Finished(active))Confirm("Leave this attempt?","Export first if you want to keep your current progress.",change);else change();
    }
    void ChallengeDialog()
    {
        var p=Modal("Choose a challenge",360);float w=p.sizeDelta.x;
        Text(p,"Enter a number from 1 to 999999.\nShare this number AND the difficulty.",24,80,w-48,70,24);
        var r=Rect("Challenge number",p);Place(r,40,160,w-80,55);r.gameObject.AddComponent<Image>().color=Color.white;
        var field=r.gameObject.AddComponent<TMP_InputField>();
        var label=Text(r,active.challenge.ToString(),10,2,w-100,51,28,Ink);
        field.textViewport=r;field.textComponent=(TextMeshProUGUI)label;field.contentType=TMP_InputField.ContentType.IntegerNumber;field.characterLimit=6;field.text=active.challenge.ToString();
        Button(p,"CANCEL",28,270,(w-72)/2,48,CloseModal);
        Button(p,"PLAY",44+(w-72)/2,270,(w-72)/2,48,()=>{int n;if(!int.TryParse(field.text,out n)||n<1||n>999999){Message("Invalid number","Use a challenge number from 1 to 999999.");return;}CloseModal();Change(active.level,n);},Gold);
    }
    void BuildGame(RectTransform r,bool portrait)
    {
        Button(r,"MENU",24,20,110,44,()=>{playing=false;Build();});
        Text(r,"SLIDE IQ",145,16,width-290,52,38,Gold).fontStyle=FontStyles.Bold;Sound(r,width-76,20);
        float tw=(width-64)/3;
        for(int i=0;i<3;i++){int l=i;Button(r,Levels[i],24+i*(tw+8),82,tw,46,()=>Change(l,active.challenge),active.level==i?Gold:Color.white);}
        Button(r,"PREV",24,143,95,42,()=>Change(active.level,active.challenge==1?999999:active.challenge-1));
        Button(r,"# "+active.challenge.ToString("000000"),127,143,width-365,42,ChallengeDialog);
        Button(r,"NEXT",width-230,143,95,42,()=>Change(active.level,active.challenge==999999?1:active.challenge+1));
        Button(r,"RANDOM",width-127,143,103,42,()=>Change(active.level,UnityEngine.Random.Range(1,1000000)));
        int n=SlideRules.Size(active.level);bool done=SlideRules.Finished(active);
        float side=portrait?602:568,cell=side/n,bx=24,by=211;
        var board=active.revealed?SlideRules.Goal(n):active.tiles;
        for(int i=0;i<board.Length;i++)
        {
            int index=i,value=board[i];float x=bx+(i%n)*cell,y=by+(i/n)*cell;
            if(value==0){var blank=Rect("Empty space",r);Place(blank,x,y,cell-6,cell-6);blank.gameObject.AddComponent<Image>().color=Ink;continue;}
            bool movable=!done&&SlideRules.CanMove(active,index);
            var button=Button(r,value.ToString(),x,y,cell-6,cell-6,()=>{if(SlideRules.Move(active,index))Build();},movable?Gold:new Color(.78f,.87f,.95f));
            button.interactable=movable;
            var colors=button.colors;colors.disabledColor=Color.white;button.colors=colors;
            var label=button.GetComponentInChildren<TMP_Text>();label.fontSizeMax=portrait?40:36;label.fontSize=label.fontSizeMax;
        }
        float cx=portrait?24:632,cy=portrait?835:230,cw=portrait?602:464;
        Text(r,SlideRules.Won(active)?"PUZZLE SOLVED!":active.revealed?"GOAL REVEALED — attempt ended":"Slide the tiles into order",cx,cy,cw,42,28,Gold);cy+=48;
        Text(r,"MOVES  "+active.moves,cx,cy,cw/2,34,25);timer=Text(r,"",cx+cw/2,cy,cw/2,34,22);cy+=50;
        float third=(cw-16)/3;
        Button(r,"RESET",cx,cy,third,44,()=>Confirm("Reset this challenge?","Your moves and timer will be cleared.",()=>{active=SlideRules.New(active.level,active.challenge);Build();}));
        Button(r,"REVEAL",cx+third+8,cy,third,44,()=>{if(!done)Confirm("Reveal the goal?","This ends your current attempt without marking it as solved.",()=>{active.revealed=true;Build();});});
        Button(r,"HELP",cx+2*(third+8),cy,third,44,Help);cy+=56;
        Button(r,"EXPORT",cx,cy,(cw-8)/2,44,Export);Button(r,"IMPORT",cx+(cw+8)/2,cy,(cw-8)/2,44,Import);
        if(!portrait)Text(r,"Tap a tile beside the empty space.\nGold tiles can move.\n\nGoal: numbers in reading order,\nempty space at the bottom right.\n\nExport progress before closing.",cx,cy+75,cw,210,24);
    }
    string SaveJson(){return JsonUtility.ToJson(active,true);}
    public void OnFileError(string error){Message("Import failed",error);}
    public void OnImport(string json)
    {
        try
        {
            if(string.IsNullOrEmpty(json)||json.Length>2000000)throw new ArgumentException("Empty file or file larger than 2 MB.");
            var candidate=JsonUtility.FromJson<SlideProgress>(json);SlideRules.Validate(candidate);
            Confirm("Restore progress?","This replaces the current attempt. Export first if you need to keep it.",()=>{active=candidate;playing=true;Build();});
        }
        catch(Exception e){Message("Import failed",e.Message+"\nYour current attempt was kept.");}
    }
    void Sound(Transform parent,float x,float y)
    {
        var b=Button(parent,"",x,y,50,44,()=>music.ToggleSound(),Color.clear);
        var icon=Rect("Speaker",b.transform); Place(icon,10,8,30,26);
        var speaker=icon.gameObject.AddComponent<IQSpeakerGraphic>(); speaker.color=Color.white; speaker.raycastTarget=false; speaker.IsOn=music.SoundEnabled;
        b.onClick.AddListener(()=>{speaker.IsOn=music.SoundEnabled;speaker.SetVerticesDirty();});
    }
    void BuildMenu(RectTransform r)
    {
        float titleY=height>width?110:45;
        Text(r,"SLIDE IQ",20,titleY,width-40,90,72,Gold).fontStyle=FontStyles.Bold;
        Text(r,"Slide. Think. Solve.",20,titleY+92,width-40,42,30);
        float tileSize=62,artX=(width-3*tileSize)/2,artY=titleY+155;
        int[] demo={1,2,3,4,0,6,7,5,8};
        for(int i=0;i<9;i++)
        {
            var tile=Rect("Decorative tile",r);Place(tile,artX+i%3*tileSize,artY+i/3*tileSize,tileSize-5,tileSize-5);
            tile.gameObject.AddComponent<Image>().color=demo[i]==0?Ink:Gold;
            if(demo[i]!=0)Text(tile,demo[i].ToString(),0,0,tileSize-5,tileSize-5,30,Ink);
        }
        float y=artY+215,cx=(width-360)/2;
        Text(r,"Put every tile in its place.",20,y-10,width-40,40,28);y+=50;
        Button(r,active.moves>0?"RESUME / PLAY":"PLAY",cx,y,360,50,()=>{playing=true;Build();});
        Button(r,"HOW TO PLAY",cx,y+62,360,44,Help);
        Button(r,"IMPORT PROGRESS",cx,y+118,360,42,Import);
        Button(r,"OTHER IQ GAMES",cx,y+172,360,42,OpenAllGames);
        Text(r,"Three board sizes. One satisfying challenge.",20,y+238,width-40,32,23);
        Text(r,"Export progress before closing.",20,height-60,width-100,28,18);
        Sound(r,width-76,height-66);
    }
    void OpenAllGames()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SlideOpenGames();
#else
        Application.OpenURL("https://iqgamesonline.com/?iqreturn=1");
#endif
    }
    RectTransform Modal(string title,float h=650)
    {
        if(modal!=null) {modal.SetActive(false);Destroy(modal);}
        var shade=Rect("Dialog",root);Stretch(shade);shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.82f);modal=shade.gameObject;
        float w=Math.Min(width-40,720);h=Math.Min(h,height-40);
        var panel=Rect("Panel",shade);Place(panel,(width-w)/2,(height-h)/2,w,h);panel.gameObject.AddComponent<Image>().color=Green;
        Text(panel,title,20,16,w-40,55,31,Gold);return panel;
    }
    void CloseModal() {if(modal!=null){modal.SetActive(false);Destroy(modal);modal=null;}}
    void Message(string title,string body)
    {
        var p=Modal(title);float w=p.sizeDelta.x,h=p.sizeDelta.y;
        Text(p,body,28,83,w-56,h-172,24);
        Button(p,"CLOSE",(w-180)/2,h-72,180,48,CloseModal);
    }
    void Confirm(string title,string body,Action yes)
    {
        var p=Modal(title,340);float w=p.sizeDelta.x;
        Text(p,body,28,90,w-56,133,24);
        Button(p,"CANCEL",28,255,(w-72)/2,48,CloseModal);
        Button(p,"CONTINUE",44+(w-72)/2,255,(w-72)/2,48,()=>{CloseModal();yes();},Gold);
    }
    void Export()
    {
        string json=SaveJson(),filename="SlideIQ-progress-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".json";
#if UNITY_WEBGL && !UNITY_EDITOR
        SlideDownload(filename,json);
        Message("Save download requested","Keep the downloaded JSON file. Import it here to resume this challenge. Check your browser downloads before closing.");
#elif UNITY_EDITOR
        string path=UnityEditor.EditorUtility.SaveFilePanel("Export SlideIQ progress","",filename,"json");
        if(!string.IsNullOrEmpty(path)) {try {System.IO.File.WriteAllText(path,json);Message("Progress exported","Saved to:\n"+path);}catch(Exception e){Message("Export failed",e.Message);}}
#else
        try {string path=System.IO.Path.Combine(Application.persistentDataPath,filename);System.IO.File.WriteAllText(path,json);Message("Progress exported",path);}catch(Exception e){Message("Export failed",e.Message);}
#endif
    }
    void Import()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SlidePickFile(gameObject.name);
#elif UNITY_EDITOR
        string path=UnityEditor.EditorUtility.OpenFilePanel("Import SlideIQ progress","","json");
        if(!string.IsNullOrEmpty(path)) {try {if(new System.IO.FileInfo(path).Length>2000000)throw new ArgumentException("File is too large (2 MB limit).");OnImport(System.IO.File.ReadAllText(path));}catch(Exception e){Message("Import failed",e.Message);}}
#else
        Message("Import","This release supports file import in WebGL and the Unity Editor. Use the browser version to resume a saved file.");
#endif
    }
}
