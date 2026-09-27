using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Both build scenes use this controller. All UI is generated at runtime.
public sealed class MineIQApp : MonoBehaviour
{
    public bool startInGame;
    static readonly Color Ink = new Color(.08f,.16f,.12f), Green = new Color(.17f,.34f,.22f), Gold = new Color(.95f,.77f,.35f);
    readonly List<MinePuzzle> puzzles = new List<MinePuzzle>();
    readonly Dictionary<string,MineProgress> saves = new Dictionary<string,MineProgress>();
    UnityEngine.Rect lastSafe; Canvas canvas; RectTransform root, board; GameObject modal, screen;
    Button[] cells; TMP_Text[] labels;
    TMP_Text status, timer, counter; Button modeButton;
    MineProgress active; bool flagMode, playing, revealing;

    string level="Beginner", variant="classic"; int lastWidth, lastHeight; float width, height;
    TMP_FontAsset font; IQMusic music;
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void MineDownload(string filename, string json);
    [DllImport("__Internal")] static extern void MinePickFile(string receiver);
    [DllImport("__Internal")] static extern void MineOpenGames();
#endif
    void Awake()
    {
        gameObject.name="MineIQ";
        font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if(font==null) font=TMP_Settings.defaultFontAsset;
        if (EventSystem.current==null)
        {
            var e=new GameObject("EventSystem",typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            e.AddComponent<InputSystemUIInputModule>();
#else
            e.AddComponent<StandaloneInputModule>();
#endif
        }
        var cg=new GameObject("MineCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas=cg.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var bg=Rect("Backdrop",cg.transform); Stretch(bg); bg.gameObject.AddComponent<Image>().color=Green;
        root=Rect("Safe layout",cg.transform); root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);
        music=IQMusic.GetPlayer(); gameObject.AddComponent<AudioListener>();
        var camera=gameObject.AddComponent<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Green; camera.cullingMask=0;
        try
        {
            var asset=Resources.Load<TextAsset>("mineiq-puzzles");
            if(asset==null) throw new ArgumentException("Missing starter puzzle pack.");
            var pack=JsonUtility.FromJson<MinePack>(asset.text); ValidatePack(pack);
            puzzles.AddRange(pack.puzzles);
            SelectPuzzle(puzzles[0]); playing=startInGame; Build();
        }
        catch(Exception e) { playing=false; Build(); Message("Unable to load puzzles",e.Message); }
    }
    void Update()
    {
        if(Screen.width!=lastWidth || Screen.height!=lastHeight || Screen.safeArea!=lastSafe) Build();
        if(playing && modal==null && active!=null && !MineRules.Complete(active) && !active.lost && !active.revealed) active.seconds+=Time.unscaledDeltaTime;
        if(timer!=null && active!=null)
        {
            var t=TimeSpan.FromSeconds(active.seconds);
            timer.text=$"TIME  {(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
        }
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
        lastWidth=Screen.width; lastHeight=Screen.height;
        bool portrait=Screen.height>Screen.width; width=portrait?650:1120; height=portrait?1140:840;
        lastSafe=Screen.safeArea;
        UnityEngine.Rect safe=lastSafe.width>0 && lastSafe.height>0?lastSafe:new UnityEngine.Rect(0,0,Screen.width,Screen.height);
        canvas.scaleFactor=Mathf.Max(.01f,Mathf.Min(safe.width/width,safe.height/height));
        canvas.GetComponent<CanvasScaler>().scaleFactor=canvas.scaleFactor;
        root.anchoredPosition=(safe.center-new Vector2(Screen.width/2f,Screen.height/2f))/canvas.scaleFactor;
        root.sizeDelta=new Vector2(width,height);
        if(screen!=null) {screen.SetActive(false);Destroy(screen);}
        if(modal!=null) { modal.SetActive(false);Destroy(modal); modal=null; revealing=false; }
        var r=Rect("Screen",root); Stretch(r); screen=r.gameObject;
        timer=null; status=null; 
        canvas.transform.Find("Backdrop").GetComponent<Image>().color=playing?Green:new Color(.025f,.045f,.12f);
        if(playing && active!=null) BuildGame(r,portrait); else BuildMenu(r);
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
            var image=Rect("MineIQ cover",r);Place(image,(width-artW)/2,(height-artH)/2,artW,artH);
            var raw=image.gameObject.AddComponent<RawImage>();raw.texture=texture;raw.raycastTarget=false;
        }
        float top=(height-artH)/2, cx=(width-360)/2;
        // Reserve space below the 214-unit button group for both footer lines.
        float y=Mathf.Min(top+artH*.45f,top+artH-300);
        float shadeTop=y-68;
        var shade=Rect("Menu readability",r);Place(shade,(width-artW)/2,shadeTop,artW,top+artH-shadeTop);
        var tint=shade.gameObject.AddComponent<Image>();tint.color=new Color(.02f,.035f,.10f,.57f);tint.raycastTarget=false;
        Text(r,"Find every mine. Explain every move.",20,y-60,width-40,44,29);
        Button(r,active!=null && saves.Count>0 && active.seconds>0?"RESUME / PLAY":"PLAY",cx,y,360,50,()=>{playing=true;Build();});
        Button(r,"HOW TO PLAY",cx,y+62,360,44,Help);
        Button(r,"IMPORT SAVE / PUZZLE PACK",cx,y+118,360,42,Import);
        Button(r,"ALL IQ GAMES  ↗",cx,y+172,360,42,OpenAllGames);
        Text(r,"Classic Minesweeper + Survey",20,top+artH-70,width-40,28,21);
        Text(r,"Export progress before closing.",20,top+artH-38,width-100,28,18);
        Sound(r,(width+artW)/2-65,top+artH-52);
    }
    void OpenAllGames()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        MineOpenGames();
#else
        Application.OpenURL("https://playiqgames.itch.io/");
#endif
    }
    void Help()
    {
        Message("How to play", "Open every safe tile without hitting a mine. A number counts mines in the eight surrounding tiles, including diagonals. Empty areas open automatically.\n\nChoose OPEN or FLAG, then tap a tile. F marks a suspected mine; tap it again in FLAG mode to remove it. Right-click also toggles a flag. Flags do not prove a tile is safe.\n\nEvery puzzle begins with a safe area already open. Survey mode also shows the total mines in each row and column. These totals stay fixed as you flag.\n\nReveal ends the attempt and shows the minefield. Reset lets you try again. Export before closing; Import restores your progress. Starter boards are checked for a logical solution without guessing.");
    }

    List<MinePuzzle> Filtered() { return puzzles.Where(p=>p.difficulty==level && p.variant==variant).ToList(); }
    void SelectPuzzle(MinePuzzle p)
    {
        if(!saves.TryGetValue(p.id,out active))
        { active=MineRules.NewProgress(p); saves.Add(p.id,active); }
        level=p.difficulty; variant=p.variant; revealing=false; flagMode=false;
    }

    void Navigate(int direction)
    {
        var list=Filtered(); int i=list.FindIndex(p=>p.id==active.puzzle.id);
        SelectPuzzle(list[(i+direction+list.Count)%list.Count]); Build();
    }
    void BuildGame(RectTransform r,bool portrait)
    {
        Button(r,"MENU",24,20,110,44,()=>{playing=false;Build();});
        Text(r,"MINE IQ",145,16,width-290,52,38).fontStyle=FontStyles.Bold;
        Sound(r,width-76,20);
        float tabWidth=(width-64)/3;
        string[] levels={"Beginner","Intermediate","Advanced"};
        for(int i=0;i<3;i++)
        {
            string l=levels[i];
            Button(r,l,24+i*(tabWidth+8),84,tabWidth,48,()=>{var p=puzzles.FirstOrDefault(x=>x.difficulty==l && x.variant==variant);if(p!=null){SelectPuzzle(p);Build();}},level==l?Gold:Color.white);
        }
        for(int i=0;i<2;i++)
        {
            string v=i==0?"classic":"survey";
            Button(r,i==0?"CLASSIC":"SURVEY",24+i*((width-56)/2+8),149,(width-56)/2,42,()=>
            {
                var p=puzzles.FirstOrDefault(x=>x.variant==v && x.difficulty==level);
                if(p!=null){SelectPuzzle(p);Build();}
            },variant==v?Gold:Color.white);
        }
        Button(r,"PREV",24,207,110,42,()=>Navigate(-1));
        Button(r,"NEXT",width-134,207,110,42,()=>Navigate(1));
        counter=Text(r,"",144,207,width-288,42,23);
        float side=portrait?594:550, bx=portrait?28:24, by=265;
        board=Rect("Minefield",r); Place(board,bx,by,side,side); board.gameObject.AddComponent<Image>().color=Ink;
        int n=active.puzzle.size; float margin=variant=="survey"?38:4, step=(side-margin-4)/n;
        cells=new Button[n*n]; labels=new TMP_Text[n*n];
        if(variant=="survey")
        {
            for(int k=0;k<n;k++)
            {
                Text(board,MineRules.RowTotal(active.puzzle,k).ToString(),0,margin+k*step,margin,step,22,Gold);
                Text(board,MineRules.ColumnTotal(active.puzzle,k).ToString(),margin+k*step,0,step,margin,22,Gold);
            }
        }
        for(int i=0;i<n*n;i++)
        {
            int index=i;
            cells[i]=Button(board,"",margin+(i%n)*step+1,margin+(i/n)*step+1,step-2,step-2,()=>Tap(index));
            var pointer=cells[i].gameObject.AddComponent<MineRightClick>(); pointer.action=()=>ToggleFlag(index);
            labels[i]=cells[i].GetComponentInChildren<TMP_Text>(); labels[i].fontSizeMax=Mathf.Min(32,step*.55f);labels[i].fontSizeMin=12;
        }
        float cx=portrait?28:606, cy=portrait?876:280, cw=portrait?594:490, third=(cw-16)/3;
        modeButton=Button(r,"",cx,cy,cw,48,()=>{flagMode=!flagMode;Refresh();}); cy+=60;
        Button(r,"REVEAL",cx,cy,third,42,Reveal);
        Button(r,"EXPORT",cx+third+8,cy,third,42,Export);
        Button(r,"IMPORT",cx+2*(third+8),cy,third,42,Import); cy+=54;
        Button(r,"RESET PUZZLE",cx,cy,cw,40,()=>Confirm("Reset this puzzle?","This clears flags, opened tiles and time for this puzzle.",()=>{saves.Remove(active.puzzle.id);SelectPuzzle(active.puzzle);Build();})); cy+=48;
        timer=Text(r,"",cx,cy,cw,28,20,Gold); cy+=30;
        status=Text(r,"",cx,cy,cw,portrait?60:110,portrait?18:23);
        if(!portrait) Text(r,"Numbers count adjacent mines, including diagonals.\nF = flagged tile; * = mine.\n"+(variant=="survey"?"Gold edge numbers are fixed row/column mine totals.":"Switch to FLAG to mark suspected mines."),cx,cy+130,cw,125,22);
        Refresh();
    }
    void Tap(int i)
    {
        if(active.lost || active.revealed || MineRules.Complete(active))return;
        if(flagMode){ToggleFlag(i);return;}
        if(active.flags[i] || active.opened[i])return;
        if(active.puzzle.mines[i]=='1'){active.lost=true;active.opened[i]=true;}
        else MineRules.Open(active,i);
        Refresh();
    }
    void ToggleFlag(int i)
    {
        if(active.lost || active.revealed || MineRules.Complete(active) || active.opened[i])return;
        music.Begin(); active.flags[i]=!active.flags[i];Refresh();
    }
    void Refresh()
    {
        if(!playing || active==null || status==null)return;
        bool done=MineRules.Complete(active), show=active.lost || active.revealed;
        var list=Filtered();
        counter.text=$"{level} • {list.FindIndex(p=>p.id==active.puzzle.id)+1} of {list.Count}";
        int flags=active.flags.Count(x=>x), mines=active.puzzle.mines.Count(x=>x=='1');
        for(int i=0;i<cells.Length;i++)
        {
            bool mine=active.puzzle.mines[i]=='1', opened=active.opened[i];
            cells[i].interactable=!show && !done && !opened;
            cells[i].GetComponent<Image>().color=show && mine?new Color(.95f,.45f,.35f):opened || show?new Color(.88f,.92f,.87f):active.flags[i]?Gold:new Color(.40f,.57f,.65f);
            labels[i].color=Ink;
            int count=MineRules.Adjacent(active.puzzle,i);
            labels[i].text=show && mine?"*":active.flags[i] && !opened?(show && !mine?"X":"F"):opened || show?(count==0?"":count.ToString()):"";
        }
        modeButton.GetComponentInChildren<TMP_Text>().text=flagMode?"FLAG MODE • Tap to mark / unmark":"OPEN MODE • Tap to uncover";
        modeButton.GetComponent<Image>().color=flagMode?Gold:Color.white;
        status.text=active.lost?"Mine hit. Reset to try again.":active.revealed?"Solution revealed. Reset for a new attempt.":done?"Solved! Every safe tile is open.":$"{mines} mines • {flags} flags\n"+(variant=="survey"?"Gold edge numbers = row / column totals.":"Use the numbers to find the mines.");
    }
    RectTransform Modal(string title,float h=650)
    {
        if(modal!=null) {modal.SetActive(false);Destroy(modal);}
        var shade=Rect("Dialog",root);Stretch(shade);shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.82f);modal=shade.gameObject;
        float w=Math.Min(width-40,720);h=Math.Min(h,height-40);
        var panel=Rect("Panel",shade);Place(panel,(width-w)/2,(height-h)/2,w,h);panel.gameObject.AddComponent<Image>().color=Green;
        Text(panel,title,20,16,w-40,55,31,Gold);return panel;
    }
    void CloseModal() {if(modal!=null){modal.SetActive(false);Destroy(modal);modal=null;}revealing=false;}
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
    void Reveal()
    {
        Confirm("Reveal this minefield?","This ends this attempt and marks the puzzle as revealed. Reset to play again.",()=>{active.revealed=true;Refresh();});
    }

    string SaveJson()
    {
        // Include untouched imported puzzles too, so a save is portable without its add-in pack.
        var all=puzzles.Select(p=>saves.ContainsKey(p.id)?saves[p.id]:MineRules.NewProgress(p)).ToArray();
        return JsonUtility.ToJson(new MineSave{activePuzzleId=active?.puzzle.id,progress=all},true);
    }
    void Export()
    {
        string json=SaveJson(),filename="MineIQ-progress-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".json";
#if UNITY_WEBGL && !UNITY_EDITOR
        MineDownload(filename,json);
        Message("Save download requested","Keep the downloaded JSON file. Import it here to resume all your puzzles. Check your browser downloads before closing.");
#elif UNITY_EDITOR
        string path=UnityEditor.EditorUtility.SaveFilePanel("Export MineIQ progress","",filename,"json");
        if(!string.IsNullOrEmpty(path)) {try {System.IO.File.WriteAllText(path,json);Message("Progress exported","Saved to:\n"+path);}catch(Exception e){Message("Export failed",e.Message);}}
#else
        try {string path=System.IO.Path.Combine(Application.persistentDataPath,filename);System.IO.File.WriteAllText(path,json);Message("Progress exported",path);}catch(Exception e){Message("Export failed",e.Message);}
#endif
    }
    void Import()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        MinePickFile(gameObject.name);
#elif UNITY_EDITOR
        string path=UnityEditor.EditorUtility.OpenFilePanel("Import MineIQ save or puzzle pack","","json");
        if(!string.IsNullOrEmpty(path)) {try {if(new System.IO.FileInfo(path).Length>2000000)throw new ArgumentException("File is too large (2 MB limit).");OnImport(System.IO.File.ReadAllText(path));}catch(Exception e){Message("Import failed",e.Message);}}
#else
        Message("Import","This release supports file import in WebGL and the Unity Editor. Use the browser version to resume a saved file.");
#endif
    }
    static void ValidatePack(MinePack pack)
    {
        if(pack==null || pack.schemaVersion!=1 || pack.puzzles==null || pack.puzzles.Length<1 || pack.puzzles.Length>100) throw new ArgumentException("Invalid puzzle pack (1–100 puzzles, schemaVersion 1).");
        var ids=new HashSet<string>();
        foreach(var p in pack.puzzles) {MineRules.Validate(p);if(!ids.Add(p.id))throw new ArgumentException("Duplicate puzzle ID.");}
    }
    public void OnFileError(string error) {Message("Import failed",error);}
    public void OnImport(string json)
    {
        try
        {
            if(string.IsNullOrEmpty(json) || json.Length>2000000) throw new ArgumentException("Empty file or file larger than 2 MB.");
            var save=JsonUtility.FromJson<MineSave>(json);
            // Presence of the progress array distinguishes saves from packs; field initializers are not used as a discriminator.
            if(save!=null && save.progress!=null)
            {
                if(save.kind!="MineIQ-progress" || save.schemaVersion!=1 || save.progress.Length<1 || save.progress.Length>100)throw new ArgumentException("Invalid save format.");
                var ids=new HashSet<string>();
                foreach(var s in save.progress) {MineRules.ValidateProgress(s);if(!ids.Add(s.puzzle.id))throw new ArgumentException("Duplicate puzzle ID in save.");}
                if(!ids.Contains(save.activePuzzleId??""))throw new ArgumentException("The saved active puzzle is missing.");
                Confirm("Restore saved progress?","This replaces your current session with the imported save. Export your current progress first if you need to keep it.",()=>
                {
                    var retained=puzzles.Where(p=>!ids.Contains(p.id)).ToList();
                    if(retained.Count+save.progress.Length>100)retained.Clear();
                    puzzles.Clear();saves.Clear();
                    foreach(var s in save.progress) {puzzles.Add(s.puzzle);saves.Add(s.puzzle.id,s);}
                    puzzles.AddRange(retained);
                    SelectPuzzle(puzzles.First(p=>p.id==save.activePuzzleId));playing=true;Build();
                });
            }
            else
            {
                var pack=JsonUtility.FromJson<MinePack>(json);ValidatePack(pack);
                var additions=new List<MinePuzzle>();
                foreach(var p in pack.puzzles)
                {
                    var existing=puzzles.FirstOrDefault(x=>x.id==p.id);
                    if(existing!=null && (existing.mines!=p.mines || existing.size!=p.size || existing.start!=p.start || existing.difficulty!=p.difficulty || existing.variant!=p.variant))throw new ArgumentException("Puzzle ID conflicts with an existing puzzle: "+p.id);
                    if(existing==null)additions.Add(p);
                }
                if(puzzles.Count+additions.Count>100)throw new ArgumentException("Maximum 100 puzzles per session.");
                puzzles.AddRange(additions);Build();Message("Puzzle pack imported",$"Added {additions.Count} new puzzles. Existing progress was kept. Export to include this pack in your save.");
            }
        }
        catch(Exception e) {Message("Import failed",e.Message+"\nYour current session was kept.");}
    }
}
