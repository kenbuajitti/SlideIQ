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
public sealed class SudokuIQApp : MonoBehaviour
{
    public bool startInGame;
    static readonly Color Ink = new Color(.08f,.16f,.12f), Green = new Color(.17f,.34f,.22f), Gold = new Color(.95f,.77f,.35f);
    readonly List<SudokuPuzzle> puzzles = new List<SudokuPuzzle>();
    readonly Dictionary<string,SudokuProgress> saves = new Dictionary<string,SudokuProgress>();
    readonly Stack<string> undo = new Stack<string>();
    readonly List<Button> digits = new List<Button>();
    UnityEngine.Rect lastSafe; Canvas canvas; RectTransform root, board; GameObject modal, screen;
    Button[] cells = new Button[81]; TMP_Text[] labels = new TMP_Text[81];
    TMP_Text status, timer, counter, noteLabel; Button eraseButton;
    SudokuProgress active; int[] values; int selected=1; bool pencil, erasing, playing, revealing;
    string level="Beginner", variant="classic"; int lastWidth, lastHeight; float width, height;
    TMP_FontAsset font; IQMusic music;
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void SudokuDownload(string filename, string json);
    [DllImport("__Internal")] static extern void SudokuPickFile(string receiver);
    [DllImport("__Internal")] static extern void SudokuOpenGames();
#endif
    void Awake()
    {
        gameObject.name="SudokuIQ";
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
        var cg=new GameObject("SudokuCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas=cg.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var bg=Rect("Backdrop",cg.transform); Stretch(bg); bg.gameObject.AddComponent<Image>().color=Green;
        root=Rect("Safe layout",cg.transform); root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);
        music=IQMusic.GetPlayer(); gameObject.AddComponent<AudioListener>();
        var camera=gameObject.AddComponent<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Green; camera.cullingMask=0;
        try
        {
            var asset=Resources.Load<TextAsset>("sudokuiq-puzzles");
            if(asset==null) throw new ArgumentException("Missing starter puzzle pack.");
            var pack=JsonUtility.FromJson<SudokuPack>(asset.text); ValidatePack(pack);
            puzzles.AddRange(pack.puzzles);
            SelectPuzzle(puzzles[0]); playing=startInGame; Build();
        }
        catch(Exception e) { playing=false; Build(); Message("Unable to load puzzles",e.Message); }
    }
    void Update()
    {
        if(Screen.width!=lastWidth || Screen.height!=lastHeight || Screen.safeArea!=lastSafe) Build();
        if(playing && modal==null && active!=null && !SudokuRules.Complete(values, active.puzzle.variant)) active.seconds+=Time.unscaledDeltaTime;
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
        timer=null; status=null; digits.Clear();
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
            var image=Rect("SudokuIQ cover",r);Place(image,(width-artW)/2,(height-artH)/2,artW,artH);
            var raw=image.gameObject.AddComponent<RawImage>();raw.texture=texture;raw.raycastTarget=false;
        }
        float top=(height-artH)/2, cx=(width-360)/2;
        // Reserve space below the 214-unit button group for both footer lines.
        float y=Mathf.Min(top+artH*.45f,top+artH-300);
        float shadeTop=y-68;
        var shade=Rect("Menu readability",r);Place(shade,(width-artW)/2,shadeTop,artW,top+artH-shadeTop);
        var tint=shade.gameObject.AddComponent<Image>();tint.color=new Color(.02f,.035f,.10f,.57f);tint.raycastTarget=false;
        Text(r,"Nine numbers. One solution.",20,y-60,width-40,44,29);
        Button(r,active!=null && saves.Count>0 && active.seconds>0?"RESUME / PLAY":"PLAY",cx,y,360,50,()=>{playing=true;Build();});
        Button(r,"HOW TO PLAY",cx,y+62,360,44,Help);
        Button(r,"IMPORT SAVE / PUZZLE PACK",cx,y+118,360,42,Import);
        Button(r,"ALL IQ GAMES  ↗",cx,y+172,360,42,OpenAllGames);
        Text(r,"Classic Sudoku + Sudoku-X",20,top+artH-70,width-40,28,21);
        Text(r,"Export progress before closing.",20,top+artH-38,width-100,28,18);
        Sound(r,(width+artW)/2-65,top+artH-52);
    }
    void OpenAllGames()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SudokuOpenGames();
#else
        Application.OpenURL("https://playiqgames.itch.io/");
#endif
    }
    void Help()
    {
        Message("How to play", "Fill each row, column and 3 × 3 box with 1–9, without repeats. In Sudoku-X, BOTH marked corner-to-corner diagonals must also each contain 1–9. Dark numbers are fixed clues.\n\nChoose a number, then click a white cell to place it. Grey cells are blocked by the current board. A legal move can still be wrong later.\n\nPencil mode adds or removes small candidate notes. Erase removes an entry or its notes. Undo reverses your recent moves on this puzzle.\n\nReveal shows the answer separately and marks the puzzle as revealed.\n\nProgress stays in this session as you change puzzles. Export saves ALL puzzle progress and imported packs to a JSON file; Import restores it on another day or device. Export before closing or refreshing.");
    }
    List<SudokuPuzzle> Filtered() { return puzzles.Where(p=>p.difficulty==level && p.variant==variant).ToList(); }
    void SelectPuzzle(SudokuPuzzle p)
    {
        if(!saves.TryGetValue(p.id,out active))
        { active=new SudokuProgress{puzzle=p,values=p.givens}; saves.Add(p.id,active); }
        values=SudokuRules.Parse(active.values); level=p.difficulty; variant=p.variant; undo.Clear(); revealing=false;
    }
    void Navigate(int direction)
    {
        var list=Filtered(); int i=list.FindIndex(p=>p.id==active.puzzle.id);
        SelectPuzzle(list[(i+direction+list.Count)%list.Count]); Build();
    }
    void BuildGame(RectTransform r,bool portrait)
    {
        Button(r,"MENU",24,20,110,44,()=>{playing=false;Build();});
        Text(r,"SUDOKU IQ",145,16,width-290,52,38).fontStyle=FontStyles.Bold;
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
            string v=i==0?"classic":"sudoku-x";
            var choice=Button(r,i==0?"CLASSIC":"SUDOKU-X",24+i*((width-56)/2+8),149,(width-56)/2,42,()=>
            {
                var p=puzzles.FirstOrDefault(x=>x.variant==v && x.difficulty==level)??puzzles.FirstOrDefault(x=>x.variant==v);
                if(p!=null){SelectPuzzle(p);Build();}
            },variant==v?Gold:Color.white);
            choice.interactable=puzzles.Any(x=>x.variant==v);
        }
        Button(r,"PREV",24,207,110,42,()=>Navigate(-1));
        Button(r,"NEXT",width-134,207,110,42,()=>Navigate(1));
        var list=Filtered();
        counter=Text(r,$"{level}  •  {list.FindIndex(p=>p.id==active.puzzle.id)+1} of {list.Count}",144,207,width-288,42,23);
        float side=portrait?594:550, bx=portrait?28:24, by=265;
        board=Rect("Sudoku board",r); Place(board,bx,by,side,side); board.gameObject.AddComponent<Image>().color=Ink;
        for(int i=0;i<81;i++)
        {
            int index=i; float step=side/9; float gap=2;
            float left=(i%9)*step+(i%3==0?4:gap), top=(i/9)*step+(i/9%3==0?4:gap);
            cells[i]=Button(board,"",left,top,step-(i%3==0?6:4),step-(i/9%3==0?6:4),()=>PlaceNumber(index));
            labels[i]=cells[i].GetComponentInChildren<TMP_Text>(); labels[i].fontStyle=FontStyles.Normal;
        }
        float cx=portrait?28:606, cy=portrait?876:275, cw=portrait?594:490;
        if(portrait)
        {
            for(int n=1;n<=9;n++) {int k=n;digits.Add(Button(r,n.ToString(),cx+(n-1)*(cw/9),cy,cw/9-5,49,()=>Choose(k)));}
        }
        else
        {
            Text(r,"CHOOSE A NUMBER",cx,cy,cw,30,22,Gold); cy+=44;
            for(int n=1;n<=9;n++) {int k=n;digits.Add(Button(r,n.ToString(),cx+(n-1)%3*(cw/3),cy+(n-1)/3*61,cw/3-8,53,()=>Choose(k)));}
            cy+=183;
        }
        if(portrait) cy+=60;
        float third=(cw-16)/3;
        var note=Button(r,"PENCIL",cx,cy,third,44,()=>{pencil=!pencil;erasing=false;Refresh();}); noteLabel=note.GetComponentInChildren<TMP_Text>();
        eraseButton=Button(r,"ERASE",cx+third+8,cy,third,44,()=>{erasing=!erasing;Refresh();});
        Button(r,"UNDO",cx+2*(third+8),cy,third,44,Undo); cy+=54;
        Button(r,"REVEAL",cx,cy,third,42,Reveal);
        Button(r,"EXPORT",cx+third+8,cy,third,42,Export);
        Button(r,"IMPORT",cx+2*(third+8),cy,third,42,Import); cy+=51;
        if(!portrait)
        {
            Button(r,"RESET PUZZLE",cx,cy,cw,40,()=>Confirm("Reset this puzzle?","This clears entries, notes and time for this puzzle. Export first if you want a backup.",()=>{saves.Remove(active.puzzle.id);SelectPuzzle(active.puzzle);Build();})); cy+=52;
        }
        timer=Text(r,"",cx,cy,cw,28,20,Gold); cy+=28;
        status=Text(r,"",cx,cy,cw,portrait?30:55,portrait?17:21);
        if(variant=="sudoku-x") Text(r,"X: both tinted diagonals also contain 1–9.",cx,cy+(portrait?32:58),cw,23,17,Gold);
        Refresh();
    }
    void Choose(int n) {selected=n;erasing=false;Refresh();}
    void RecordUndo() {undo.Push(JsonUtility.ToJson(active));}
    void Undo()
    {
        if(undo.Count==0) {status.text="No more moves to undo on this puzzle.";return;}
        var old=JsonUtility.FromJson<SudokuProgress>(undo.Pop()); active.values=old.values;active.notes=old.notes; values=SudokuRules.Parse(active.values);Refresh();
    }
    void PlaceNumber(int i)
    {
        if(revealing || active.puzzle.givens[i]!='0') return;
        if(erasing)
        {
            if(values[i]==0 && active.notes[i]==0) return;
            RecordUndo(); values[i]=0;active.notes[i]=0;
        }
        else
        {
            if(pencil && values[i]!=0) return;
            if(!SudokuRules.Legal(values,i,selected,variant)) return;
            if(!pencil && values[i]==selected) return;
            RecordUndo();
            if(pencil) active.notes[i]^=1<<selected;
            else
            {
                values[i]=selected;active.notes[i]=0;
                for(int j=0;j<81;j++) if(SudokuRules.Peer(i,j,variant)) active.notes[j]&=~(1<<selected);
            }
        }
        active.values=SudokuRules.Encode(values);Refresh();
    }
    void Refresh()
    {
        if(!playing || active==null || status==null) return;
        bool done=SudokuRules.Complete(values, active.puzzle.variant);
        var list=Filtered();
        counter.text=$"{level} • {list.FindIndex(p=>p.id==active.puzzle.id)+1} of {list.Count}"+(active.revealed?" • Revealed":"");
        for(int i=0;i<81;i++)
        {
            bool given=active.puzzle.givens[i]!='0';
            bool legal=!given && (erasing ? (values[i]!=0 || active.notes[i]!=0) : SudokuRules.Legal(values,i,selected,variant) && (!pencil || values[i]==0));
            cells[i].interactable=legal;
            bool diagonal=variant=="sudoku-x" && SudokuRules.OnDiagonal(i);
            cells[i].GetComponent<Image>().color=given?(diagonal?new Color(.80f,.82f,.95f):new Color(.81f,.86f,.79f))
                :legal?(diagonal?new Color(.90f,.91f,1f):Color.white):(diagonal?new Color(.54f,.55f,.68f):new Color(.59f,.64f,.59f));
            labels[i].color=given?Ink:new Color(.1f,.3f,.65f);
            labels[i].fontStyle=given?FontStyles.Bold:FontStyles.Normal;
            labels[i].fontSizeMax=values[i]!=0?38:15; labels[i].fontSizeMin=values[i]!=0?28:12;
            if(values[i]!=0) labels[i].text=values[i].ToString();
            else
            {
                string notes="";
                for(int n=1;n<=9;n++) {notes+=(active.notes[i]&(1<<n))!=0?n.ToString():" ";if(n%3==0 && n<9) notes+="\n";else if(n%3!=0) notes+=" ";}
                labels[i].text=notes;
            }
        }
        for(int i=0;i<digits.Count;i++) digits[i].GetComponent<Image>().color=selected==i+1 && !erasing?Gold:Color.white;
        noteLabel.text=pencil?"PENCIL ON":"PENCIL OFF";
        eraseButton.GetComponent<Image>().color=erasing?Gold:Color.white;
        status.text=done?(active.revealed?"Completed • solution was revealed":"Solved! Well done."):(erasing?"Click an entry or notes to erase.":pencil?"Click a white cell to toggle a pencil mark.":$"Place {selected} in a white cell. Grey = blocked.");
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
        Confirm("Reveal this solution?","Your entries will remain unchanged. This puzzle will be marked as revealed.",()=>
        {
            active.revealed=true; revealing=true;
            var p=Modal("Completed puzzle",760);float w=p.sizeDelta.x,h=p.sizeDelta.y,side=Math.Min(w-40,h-165),x=(w-side)/2;
            var b=Rect("Solution",p);Place(b,x,80,side,side);b.gameObject.AddComponent<Image>().color=Ink;
            for(int i=0;i<81;i++)
            {
                float step=side/9,gx=i%3==0?4:2,gy=i/9%3==0?4:2;
                var cell=Rect("Answer",b);Place(cell,i%9*step+gx,i/9*step+gy,step-gx-2,step-gy-2);cell.gameObject.AddComponent<Image>().color=Color.white;
                if(variant=="sudoku-x" && SudokuRules.OnDiagonal(i)) cell.GetComponent<Image>().color=new Color(.84f,.86f,1f);
                Text(cell,active.puzzle.solution[i].ToString(),0,0,step-gx-2,step-gy-2,32,Ink);
            }
            Button(p,"BACK TO PUZZLE",(w-260)/2,h-65,260,45,()=>{CloseModal();Refresh();});
        });
    }
    string SaveJson()
    {
        // Include untouched imported puzzles too, so a save is portable without its add-in pack.
        var all=puzzles.Select(p=>saves.ContainsKey(p.id)?saves[p.id]:new SudokuProgress{puzzle=p,values=p.givens}).ToArray();
        return JsonUtility.ToJson(new SudokuSave{activePuzzleId=active?.puzzle.id,progress=all},true);
    }
    void Export()
    {
        string json=SaveJson(),filename="SudokuIQ-progress-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".json";
#if UNITY_WEBGL && !UNITY_EDITOR
        SudokuDownload(filename,json);
        Message("Save download requested","Keep the downloaded JSON file. Import it here to resume all your puzzles. Check your browser downloads before closing.");
#elif UNITY_EDITOR
        string path=UnityEditor.EditorUtility.SaveFilePanel("Export SudokuIQ progress","",filename,"json");
        if(!string.IsNullOrEmpty(path)) {try {System.IO.File.WriteAllText(path,json);Message("Progress exported","Saved to:\n"+path);}catch(Exception e){Message("Export failed",e.Message);}}
#else
        try {string path=System.IO.Path.Combine(Application.persistentDataPath,filename);System.IO.File.WriteAllText(path,json);Message("Progress exported",path);}catch(Exception e){Message("Export failed",e.Message);}
#endif
    }
    void Import()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SudokuPickFile(gameObject.name);
#elif UNITY_EDITOR
        string path=UnityEditor.EditorUtility.OpenFilePanel("Import SudokuIQ save or puzzle pack","","json");
        if(!string.IsNullOrEmpty(path)) {try {if(new System.IO.FileInfo(path).Length>2000000)throw new ArgumentException("File is too large (2 MB limit).");OnImport(System.IO.File.ReadAllText(path));}catch(Exception e){Message("Import failed",e.Message);}}
#else
        Message("Import","This release supports file import in WebGL and the Unity Editor. Use the browser version to resume a saved file.");
#endif
    }
    static void ValidatePack(SudokuPack pack)
    {
        if(pack==null || pack.schemaVersion!=1 || pack.puzzles==null || pack.puzzles.Length<1 || pack.puzzles.Length>100) throw new ArgumentException("Invalid puzzle pack (1–100 puzzles, schemaVersion 1).");
        var ids=new HashSet<string>();
        foreach(var p in pack.puzzles) {SudokuRules.Validate(p);if(!ids.Add(p.id))throw new ArgumentException("Duplicate puzzle ID.");}
    }
    public void OnFileError(string error) {Message("Import failed",error);}
    public void OnImport(string json)
    {
        try
        {
            if(string.IsNullOrEmpty(json) || json.Length>2000000) throw new ArgumentException("Empty file or file larger than 2 MB.");
            var save=JsonUtility.FromJson<SudokuSave>(json);
            // Presence of the progress array distinguishes saves from packs; field initializers are not used as a discriminator.
            if(save!=null && save.progress!=null)
            {
                if(save.kind!="SudokuIQ-progress" || save.schemaVersion!=1 || save.progress.Length<1 || save.progress.Length>100)throw new ArgumentException("Invalid save format.");
                var ids=new HashSet<string>();
                foreach(var s in save.progress) {SudokuRules.ValidateProgress(s);if(!ids.Add(s.puzzle.id))throw new ArgumentException("Duplicate puzzle ID in save.");}
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
                var pack=JsonUtility.FromJson<SudokuPack>(json);ValidatePack(pack);
                var additions=new List<SudokuPuzzle>();
                foreach(var p in pack.puzzles)
                {
                    var existing=puzzles.FirstOrDefault(x=>x.id==p.id);
                    if(existing!=null && (existing.givens!=p.givens || existing.solution!=p.solution || existing.difficulty!=p.difficulty || existing.variant!=p.variant))throw new ArgumentException("Puzzle ID conflicts with an existing puzzle: "+p.id);
                    if(existing==null)additions.Add(p);
                }
                if(puzzles.Count+additions.Count>100)throw new ArgumentException("Maximum 100 puzzles per session.");
                puzzles.AddRange(additions);Build();Message("Puzzle pack imported",$"Added {additions.Count} new puzzles. Existing progress was kept. Export to include this pack in your save.");
            }
        }
        catch(Exception e) {Message("Import failed",e.Message+"\nYour current session was kept.");}
    }
}
