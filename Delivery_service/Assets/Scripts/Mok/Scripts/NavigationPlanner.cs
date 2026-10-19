using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Mok.Navigation
{
    // 손과 오토바이가 공유하는 기존 휴대폰 화면 안에 표시한다.
    public sealed class NavigationPlanner : MonoBehaviour
    {
        public NavigationManager manager;
        public Camera overviewCamera; // 기존 씬 참조 유지를 위한 필드이며 활성화하지 않는다.
        public Font font;
        public RenderTexture overviewTexture;
        public enum Screen { Home, Destinations, Map, Stocks, News, Reviews }
        public Screen CurrentScreen { get; private set; } = Screen.Map;
        public int Selection { get; private set; }
        public bool IsRiding => bikes != null && bikes.Any(b => b != null && b.isDriven);
        public bool IsOpen => IsRiding || (phone != null && phone.IsPhoneOpen());
        PhoneController phone;
        MotorcycleController[] bikes;
        GameObject home, destinations;
        DeliveryService.Mok.Stocks.StockPhoneApp stocks;
        DeliveryService.Mok.Stocks.StockPhoneNews news;
        DeliveryService.Mok.ReviewPhoneApp reviews;
        Text[] apps, rows;
        PhoneAppIcon[] appIcons;
        Text notice, hint;
        bool wasRiding, wasOpen;
        NavigationPlace[] places;
        readonly string[] names = { "배달지", "매장", "수리점", "탈것" };
        readonly NavigationPlaceKind[] kinds = { NavigationPlaceKind.Other, NavigationPlaceKind.Pizza, NavigationPlaceKind.Repair, NavigationPlaceKind.Bike };
        void Start()
        {
            phone = FindFirstObjectByType<PhoneController>();
            bikes = FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None);
            if (manager == null) manager = FindFirstObjectByType<NavigationManager>();
            if (overviewCamera != null) overviewCamera.enabled = false;
            var display = FindObjectsByType<RawImage>(FindObjectsSortMode.None).FirstOrDefault(r => r.name == "NavigationFinalDisplay");
            if (display == null) { enabled = false; return; }
            Build(display.rectTransform);
            Show(Screen.Map);
        }
        void LateUpdate()
        {
            if (home == null) return;
            home.transform.parent.SetAsLastSibling();
            bool riding = IsRiding;
            bool open = phone != null && phone.IsPhoneOpen();
            if (riding != wasRiding) { Show(Screen.Map); wasRiding = riding; }
            if (!riding && open && !wasOpen) Show(Screen.Home);
            if (!riding && !open && wasOpen) Show(Screen.Map);
            wasOpen = open;
            if (IsOpen && Time.timeScale > 0 && Cursor.lockState == CursorLockMode.Locked)
            {
                if(CurrentScreen==Screen.Reviews){
                    if(Input.GetKeyDown(KeyCode.UpArrow))reviews.Move(-1);
                    if(Input.GetKeyDown(KeyCode.DownArrow))reviews.Move(1);
                    if(Input.GetKeyDown(KeyCode.Backspace))Show(Screen.Home);
                    return;
                }
                if(CurrentScreen==Screen.Home){
                    if(Input.GetKeyDown(KeyCode.UpArrow))MoveSelection(-2);
                    else if(Input.GetKeyDown(KeyCode.DownArrow))MoveSelection(2);
                    else if(Input.GetKeyDown(KeyCode.LeftArrow))MoveSelection(-1);
                    else if(Input.GetKeyDown(KeyCode.RightArrow))MoveSelection(1);
                    if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))Confirm();
                    return;
                }
                if(CurrentScreen==Screen.News){
                    if(Input.GetKeyDown(KeyCode.UpArrow))news.Move(-1);
                    if(Input.GetKeyDown(KeyCode.DownArrow))news.Move(1);
                    if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))news.Confirm();
                    if(Input.GetKeyDown(KeyCode.Backspace)&&!news.Back())Show(Screen.Home);
                    return;
                }
                if(CurrentScreen==Screen.Stocks){
                    if(Input.GetKeyDown(KeyCode.UpArrow))stocks.Move(1,false);
                    if(Input.GetKeyDown(KeyCode.DownArrow))stocks.Move(-1,false);
                    if(Input.GetKeyDown(KeyCode.LeftArrow))stocks.Move(-1,true);
                    if(Input.GetKeyDown(KeyCode.RightArrow))stocks.Move(1,true);
                    if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))stocks.Confirm();
                    if(Input.GetKeyDown(KeyCode.Backspace)&&!stocks.Back())Show(Screen.Home);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow)) MoveSelection(-1);
                else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow)) MoveSelection(1);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Confirm();
                if (Input.GetKeyDown(KeyCode.Backspace)) Back();
            }
            Refresh();
        }
        public void MoveSelection(int direction)
        {
            if (CurrentScreen == Screen.Map) { Show(Screen.Destinations); return; }
            int count = CurrentScreen == Screen.Home ? 4 : names.Length;
            Selection = (Selection + direction + count) % count;
            if (notice != null) notice.text = "";
            Refresh();
        }
        public void Confirm()
        {
            if (CurrentScreen == Screen.Map) { Show(Screen.Destinations); return; }
            if (CurrentScreen == Screen.Home)
            {
                if (Selection == 0) Show(Screen.Destinations);
                else Show(Selection==1?Screen.Stocks:Selection==2?Screen.News:Screen.Reviews);
                return;
            }
            if (manager == null) return;
            if (Selection == 0)
            {
                if (manager.CurrentDestination == null) { notice.text = "진행 중인 배달이 없습니다"; return; }
                manager.ResumeDelivery();
            }
            else
            {
                var place = SelectedPlace(Selection);
                if (place == null || !place.IsAvailable) { notice.text = IsRiding && Selection == 3 ? "현재 탑승 중인 탈것입니다" : "등록된 목적지가 없습니다"; return; }
                manager.SelectPlace(place);
            }
            Show(Screen.Map);
        }
        public void Back()
        {
            if (IsRiding) Show(CurrentScreen == Screen.Map ? Screen.Destinations : Screen.Map);
            else Show(CurrentScreen == Screen.Map ? Screen.Destinations : Screen.Home);
        }
        NavigationPlace SelectedPlace(int i) => places == null ? null : places.FirstOrDefault(p => p != null && p.kind == kinds[i]);
        public void Show(Screen screen)
        {
            if (IsRiding && (screen == Screen.Home || screen == Screen.Stocks || screen == Screen.News || screen == Screen.Reviews)) screen = Screen.Map;
            CurrentScreen = screen; Selection = 0;
            if (home == null) return;
            places = FindObjectsByType<NavigationPlace>(FindObjectsSortMode.None);
            home.SetActive(screen == Screen.Home);
            destinations.SetActive(screen == Screen.Destinations);
            stocks.gameObject.SetActive(screen==Screen.Stocks);if(screen==Screen.Stocks)stocks.Open();
            news.gameObject.SetActive(screen==Screen.News);if(screen==Screen.News)news.Open();
            reviews.gameObject.SetActive(screen==Screen.Reviews);if(screen==Screen.Reviews)reviews.Open();
            notice.text = "";
            Refresh();
        }
        void Refresh()
        {
            if (home == null) return;
            for (int i = 0; i < apps.Length; i++) { appIcons[i].Selected=i==Selection; apps[i].color=i==Selection?new Color(.96f,.85f,.60f):Color.white; }
            for (int i = 0; i < rows.Length; i++)
            {
                bool available = i == 0 ? manager != null && manager.CurrentDestination != null : SelectedPlace(i) != null && SelectedPlace(i).IsAvailable;
                rows[i].text = (i == Selection ? ">  " : "    ") + names[i] + (!available ? i == 3 && IsRiding ? "  (탑승 중)" : "  (없음)" : "");
                rows[i].color = available ? Color.white : new Color(.6f,.66f,.7f);
                rows[i].transform.parent.GetComponent<Image>().color = i == Selection ? new Color(.08f,.43f,.48f) : new Color(.12f,.18f,.23f);
            }
            hint.text = "";
        }
        GameObject Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var g = new GameObject(name, typeof(RectTransform)); g.layer = parent.gameObject.layer; g.transform.SetParent(parent, false);
            var r = (RectTransform)g.transform; r.anchorMin = r.anchorMax = new Vector2(0,1); r.pivot = new Vector2(0,1); r.anchoredPosition = new Vector2(x,-y); r.sizeDelta = new Vector2(w,h); return g;
        }
        Text Label(string text, Transform parent, float x,float y,float w,float h,int size)
        {
            var t=Rect("Label",parent,x,y,w,h).AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=Color.white;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;
        }
        GameObject Panel(string name,Transform parent,float x,float y,float w,float h,Color color)
        { var g=Rect(name,parent,x,y,w,h);g.AddComponent<Image>().color=color;g.GetComponent<Image>().raycastTarget=false;return g; }
        void Build(RectTransform display)
        {
            // 실제 휴대폰 크기는 유지하고 화면 내부 배치만 크기에 맞춘다.
            var root = Rect("Phone Apps",display,0,0,540,960);
            root.transform.localScale = new Vector3(display.rect.width/540f,display.rect.height/960f,1);
            font=DeliveryService.Mok.MokUIFont.Font;
            var distance=display.Find("FinalDistanceText");if(distance!=null && distance is RectTransform distanceRect) distanceRect.anchoredPosition += new Vector2(0,90);
            var oldHint=display.Find("DestinationHint");if(oldHint!=null)oldHint.gameObject.SetActive(false);
            home=Panel("Home",root.transform,0,0,540,960,new Color(.055f,.12f,.19f));
            Label("DELIVERY",home.transform,36,48,470,45,27);
            Label("내 휴대폰",home.transform,36,160,470,70,48);
            Label("도시 생활의 모든 것",home.transform,36,236,470,42,25).color=new Color(.62f,.74f,.79f);
            // 네 앱을 두 줄로 배치하고 방향키로 이동한다.
            apps=new Text[4];appIcons=new PhoneAppIcon[4];
            for(int i=0;i<4;i++){
                var tile=Rect("App "+i,home.transform,86+(i%2)*220,330+(i/2)*210,148,192);
                appIcons[i]=Rect("App icon",tile.transform,8,0,132,132).AddComponent<PhoneAppIcon>();
                appIcons[i].App=i;appIcons[i].raycastTarget=false;
                apps[i]=Label(i==0?"지도":i==1?"주식":i==2?"뉴스":"별점",tile.transform,0,144,148,44,28);
                apps[i].alignment=TextAnchor.MiddleCenter;
            }
            // 배경의 도시 실루엣은 앱 선택과 겹치지 않는 하단에 둔다.
            for(int i=0;i<8;i++)Panel("Wallpaper city",home.transform,i*72,850-(i%3)*24,64,110+(i%3)*24,new Color(.075f,.16f,.22f));
            Panel("Home indicator",home.transform,210,924,120,5,new Color(.55f,.67f,.71f));
            destinations=Panel("Destinations",root.transform,14,350,512,530,new Color(.06f,.10f,.14f,.98f));
            Label("목적지 선택",destinations.transform,24,12,460,60,34);
            rows=new Text[4];
            for(int i=0;i<4;i++){var row=Panel(names[i],destinations.transform,16,82+i*88,480,76,Color.gray);rows[i]=Label("",row.transform,12,0,458,76,32);}
            notice=Label("",root.transform,30,818,480,64,24);
            var footer=Panel("Controls",root.transform,0,890,540,70,new Color(.045f,.08f,.12f));
            hint=Label("",footer.transform,22,0,496,70,23);footer.SetActive(false);
            stocks=DeliveryService.Mok.Stocks.StockPhoneApp.Build(root.transform,font);
            news=DeliveryService.Mok.Stocks.StockPhoneNews.Build(root.transform,font);
            reviews=DeliveryService.Mok.ReviewPhoneApp.Build(root.transform,font);
        }
    }
}


