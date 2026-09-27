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

public sealed class CodeIQApp : MonoBehaviour
{
    public bool startInGame;
    static readonly Color Ink=new Color(.04f,.07f,.16f), Green=new Color(.07f,.14f,.25f), Gold=new Color(.95f,.77f,.35f);
    static readonly Color[] Palette={new Color(1,.48f,.45f),new Color(.35f,.7f,1),new Color(.4f,.9f,.6f),new Color(1,.83f,.3f),new Color(.8f,.55f,1),new Color(1,.64f,.3f),new Color(.4f,.95f,.92f),new Color(1,.65f,.85f)};
    static readonly string[] Levels={"Beginner","Intermediate","Advanced"};
    Canvas canvas; RectTransform root; GameObject screen,modal;
    TMP_FontAsset font; IQMusic music; TMP_Text timer; CodeProgress active;
    bool playing; int selected,lastWidth,lastHeight; float width,height; UnityEngine.Rect lastSafe;
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void CodeDownload(string filename,string json);
    [DllImport("__Internal")] static extern void CodePickFile(string receiver);
    [DllImport("__Internal")] static extern void CodeOpenGames();
#endif
    void Awake()
    {
        gameObject.name="CodeIQ";
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
        var cg=new GameObject("CodeCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas=cg.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var bg=Rect("Backdrop",cg.transform);Stretch(bg);bg.gameObject.AddComponent<Image>().color=Green;
        root=Rect("Safe layout",cg.transform);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);
        music=IQMusic.GetPlayer();gameObject.AddComponent<AudioListener>();
        var camera=gameObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Green;camera.cullingMask=0;
        active=CodeRules.New(0,1);playing=startInGame;Build();
    }
    void Update()
    {
        if(Screen.width!=lastWidth || Screen.height!=lastHeight || Screen.safeArea!=lastSafe)Build();
        if(playing && modal==null && !CodeRules.Finished(active))active.seconds+=Time.unscaledDeltaTime;
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
        Message("How to play CodeIQ","Discover the hidden combination in 10 guesses. Tap a slot, then choose a symbol A–H. Symbols have colors too, but letters are enough to play. Fill every slot and press SUBMIT.\n\nEXACT counts correct symbols in the correct position. OTHER counts correct symbols in a different position. Clues are totals, not matched to particular slots. Each secret symbol is counted only once.\n\nBeginner: 4 slots, 6 symbols, no repeats. Intermediate: 4 slots, 6 symbols, repeats allowed. Advanced: 5 slots, 8 symbols, repeats allowed.\n\nShare the level and challenge number to play the same code. Reveal ends the attempt. Export before closing and import to resume.");
    }
    void Change(int level,int number)
    {
        Action change=()=>{active=CodeRules.New(level,number);selected=0;Build();};
        if(active.guesses.Length>0 && !CodeRules.Finished(active))Confirm("Leave this attempt?","Export first if you want to keep your current progress.",change);else change();
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
        Text(r,"CODE IQ",145,16,width-290,52,38,Gold).fontStyle=FontStyles.Bold;Sound(r,width-76,20);
        float tw=(width-64)/3;
        for(int i=0;i<3;i++){int l=i;Button(r,Levels[i],24+i*(tw+8),82,tw,46,()=>Change(l,active.challenge),active.level==i?Gold:Color.white);}
        Button(r,"PREV",24,143,95,42,()=>Change(active.level,active.challenge==1?999999:active.challenge-1));
        Button(r,"# "+active.challenge.ToString("000000"),127,143,width-365,42,ChallengeDialog);
        Button(r,"NEXT",width-230,143,95,42,()=>Change(active.level,active.challenge==999999?1:active.challenge+1));
        Button(r,"RANDOM",width-127,143,103,42,()=>Change(active.level,UnityEngine.Random.Range(1,1000000)));
        int slots=CodeRules.Slots(active.level),symbols=CodeRules.Symbols(active.level);
        float bx=24,bw=portrait?602:590,by=206,rowH=portrait?43:49;
        Text(r,"YOUR GUESSES",bx,by,bw*.62f,30,22,Gold);
        Text(r,"EXACT  OTHER",bx+bw*.64f,by,bw*.36f,30,20,Gold);
        for(int row=0;row<10;row++)
        {
            float y=by+36+row*rowH;Text(r,(row+1).ToString(),bx,y,30,rowH-4,19);
            var guess=row<active.guesses.Length?active.guesses[row].values:null;
            float sw=(bw*.61f-35)/slots;
            for(int col=0;col<slots;col++)
            {
                int v=guess==null?-1:guess[col];var tile=Rect("Guess",r);Place(tile,bx+35+col*sw,y,sw-5,rowH-5);tile.gameObject.AddComponent<Image>().color=v<0?Ink:Palette[v];
                Text(tile,v<0?"·":((char)('A'+v)).ToString(),0,0,sw-5,rowH-5,23,v<0?Color.gray:Ink);
            }
            if(guess!=null){int exact,other;CodeRules.Score(CodeRules.Secret(active.level,active.challenge),guess,out exact,out other);Text(r,exact+"          "+other,bx+bw*.65f,y,bw*.34f,rowH-5,24);}
        }
        float cx=portrait?24:646,cy=portrait?720:214,cw=portrait?602:450;
        bool done=CodeRules.Finished(active);
        string state=CodeRules.Won(active)?"CODE CRACKED!":active.revealed?"Code revealed":active.guesses.Length==10?"Out of guesses":$"Guess {active.guesses.Length+1} of 10";
        Text(r,state,cx,cy,cw,36,27,Gold);cy+=42;
        Text(r,$"{slots} slots • {symbols} symbols • "+(active.level==0?"No repeats":"Repeats allowed"),cx,cy,cw,32,21);cy+=40;
        float slotW=cw/slots;int[] shown=done?CodeRules.Secret(active.level,active.challenge):active.draft;
        for(int i=0;i<slots;i++)
        {
            int k=i,v=shown[i];var b=Button(r,v<0?"?":((char)('A'+v)).ToString(),cx+i*slotW,cy,slotW-8,52,()=>{selected=k;Build();},v<0?Color.white:Palette[v]);b.interactable=!done;
            if(!done && selected==i)Text(r,"▲",cx+i*slotW,cy+50,slotW-8,22,20,Gold);
        }
        cy+=78;
        if(!done)
        {
            float pw=cw/symbols;
            for(int i=0;i<symbols;i++){int v=i;Button(r,((char)('A'+i)).ToString(),cx+i*pw,cy,pw-5,46,()=>Pick(v),Palette[i]);}
            cy+=58;Button(r,"CLEAR",cx,cy,cw*.31f,46,()=>{active.draft=Enumerable.Repeat(-1,slots).ToArray();selected=0;Build();});
            Button(r,"SUBMIT",cx+cw*.34f,cy,cw*.66f,46,Submit,Gold);cy+=57;
        }
        else {Text(r,"Secret combination",cx,cy,cw,28,21);cy+=40;}
        float third=(cw-16)/3;
        Button(r,"RESET",cx,cy,third,42,()=>Confirm("Reset this challenge?","Your guesses and timer will be cleared.",()=>{active=CodeRules.New(active.level,active.challenge);selected=0;Build();}));
        Button(r,"REVEAL",cx+third+8,cy,third,42,()=>{if(!done)Confirm("Reveal the code?","This ends your current attempt.",()=>{active.revealed=true;Build();});});
        Button(r,"HELP",cx+2*(third+8),cy,third,42,Help);cy+=54;
        Button(r,"EXPORT",cx,cy,(cw-8)/2,40,Export);Button(r,"IMPORT",cx+(cw+8)/2,cy,(cw-8)/2,40,Import);cy+=48;
        timer=Text(r,"",cx,cy,cw,30,21);
        if(!portrait)Text(r,"EXACT = right symbol, right place\nOTHER = right symbol, different place\n\nClue counts do not identify specific slots.",cx,cy+52,cw,100,22);
    }
    void Pick(int v)
    {
        if(CodeRules.Finished(active))return;
        if(active.level==0 && active.draft.Where((x,i)=>i!=selected).Contains(v)){Message("No repeats on Beginner","Choose a different symbol, or clear the row to start again.");return;}
        active.draft[selected]=v;selected=(selected+1)%active.draft.Length;Build();
    }
    void Submit()
    {
        if(CodeRules.Finished(active))return;
        if(active.draft.Any(x=>x<0)){Message("Complete your guess","Choose a symbol for every slot first.");return;}
        active.guesses=active.guesses.Concat(new[]{new CodeGuess{values=(int[])active.draft.Clone()}}).ToArray();
        active.draft=Enumerable.Repeat(-1,CodeRules.Slots(active.level)).ToArray();selected=0;Build();
    }
    string SaveJson(){return JsonUtility.ToJson(active,true);}
    public void OnFileError(string error){Message("Import failed",error);}
    public void OnImport(string json)
    {
        try
        {
            if(string.IsNullOrEmpty(json)||json.Length>2000000)throw new ArgumentException("Empty file or file larger than 2 MB.");
            var candidate=JsonUtility.FromJson<CodeProgress>(json);CodeRules.Validate(candidate);
            Confirm("Restore progress?","This replaces the current attempt. Export first if you need to keep it.",()=>{active=candidate;selected=0;playing=true;Build();});
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
        // Fit the orientation-specific cover, with no crop and no extra title over its lettering.
        var texture=Resources.Load<Texture2D>(height>width?"IQMenu/Portrait":"IQMenu/Landscape");
        float artW=width,artH=height;
        if(texture!=null)
        {
            float fit=Mathf.Min(width/texture.width,height/texture.height);
            artW=texture.width*fit;artH=texture.height*fit;
            var image=Rect("CodeIQ cover",r);Place(image,(width-artW)/2,(height-artH)/2,artW,artH);
            var raw=image.gameObject.AddComponent<RawImage>();raw.texture=texture;raw.raycastTarget=false;
        }
        float top=(height-artH)/2, cx=(width-360)/2;
        // Reserve space below the 214-unit button group for both footer lines.
        float y=Mathf.Min(top+artH*.45f,top+artH-300);
        float shadeTop=y-68;
        var shade=Rect("Menu readability",r);Place(shade,(width-artW)/2,shadeTop,artW,top+artH-shadeTop);
        var tint=shade.gameObject.AddComponent<Image>();tint.color=new Color(.02f,.035f,.10f,.57f);tint.raycastTarget=false;
        Text(r,"Crack the code. Sharpen your mind.",20,y-60,width-40,44,29);
        Button(r,active!=null && active.guesses.Length>0?"RESUME / PLAY":"PLAY",cx,y,360,50,()=>{playing=true;Build();});
        Button(r,"HOW TO PLAY",cx,y+62,360,44,Help);
        Button(r,"IMPORT PROGRESS",cx,y+118,360,42,Import);
        Button(r,"ALL IQ GAMES  ↗",cx,y+172,360,42,OpenAllGames);
        Text(r,"Read the clues. Find the secret combination.",20,top+artH-70,width-40,28,21);
        Text(r,"Export progress before closing.",20,top+artH-38,width-100,28,18);
        Sound(r,(width+artW)/2-65,top+artH-52);
    }
    void OpenAllGames()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        CodeOpenGames();
#else
        Application.OpenURL("https://playiqgames.itch.io/");
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
        string json=SaveJson(),filename="CodeIQ-progress-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".json";
#if UNITY_WEBGL && !UNITY_EDITOR
        CodeDownload(filename,json);
        Message("Save download requested","Keep the downloaded JSON file. Import it here to resume this challenge. Check your browser downloads before closing.");
#elif UNITY_EDITOR
        string path=UnityEditor.EditorUtility.SaveFilePanel("Export CodeIQ progress","",filename,"json");
        if(!string.IsNullOrEmpty(path)) {try {System.IO.File.WriteAllText(path,json);Message("Progress exported","Saved to:\n"+path);}catch(Exception e){Message("Export failed",e.Message);}}
#else
        try {string path=System.IO.Path.Combine(Application.persistentDataPath,filename);System.IO.File.WriteAllText(path,json);Message("Progress exported",path);}catch(Exception e){Message("Export failed",e.Message);}
#endif
    }
    void Import()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        CodePickFile(gameObject.name);
#elif UNITY_EDITOR
        string path=UnityEditor.EditorUtility.OpenFilePanel("Import CodeIQ progress","","json");
        if(!string.IsNullOrEmpty(path)) {try {if(new System.IO.FileInfo(path).Length>2000000)throw new ArgumentException("File is too large (2 MB limit).");OnImport(System.IO.File.ReadAllText(path));}catch(Exception e){Message("Import failed",e.Message);}}
#else
        Message("Import","This release supports file import in WebGL and the Unity Editor. Use the browser version to resume a saved file.");
#endif
    }
}
