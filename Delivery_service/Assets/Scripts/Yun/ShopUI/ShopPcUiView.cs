using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace DeliveryService.Yun.ShopUI
{
    /// <summary>Presentation only: never calls ordering, money, stock or opening-hours APIs.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class ShopPcUiView : MonoBehaviour
    {
        [Serializable]
        public sealed class IngredientRow
        {
            public string name;
            public int quantity = 1;
            public int minimum = 1;
            public int maximum = 999;
            public Sprite icon;
            public int artworkKind;
            public const int UnitsPerBundle = 10;
            public long TotalUnits => (long)quantity * UnitsPerBundle;
            public IngredientRow(string name, int artworkKind) { this.name = name; this.artworkKind = artworkKind; }
        }

        [Tooltip("화면 표시용 재료와 수량. 구매/재고 데이터와 연결하지 않습니다.")]
        public List<IngredientRow> ingredients = new List<IngredientRow>
        {
            new IngredientRow("페페로니", 3), new IngredientRow("초콜릿", 4),
            new IngredientRow("파인애플", 5), new IngredientRow("젤리", 6)
        };

        public int SelectedTab { get; private set; }
        public string LastClickedAction { get; private set; }
        public int ClickCount { get; private set; }
        public event Action<string> ActionClicked;

        private static readonly Color Navy = new Color32(25, 43, 66, 255);
        private static readonly Color Teal = new Color32(21, 143, 164, 255);
        private static readonly Color Pale = new Color32(242, 246, 249, 255);
        private static readonly Color Border = new Color32(216, 224, 231, 255);
        private readonly GameObject[] pages = new GameObject[3];
        private readonly Button[] tabs = new Button[3];
        private readonly ShopUiInputScope inputScope = new ShopUiInputScope();
        private readonly List<Text> quantityLabels = new List<Text>();
        private readonly List<Button> minusButtons = new List<Button>();
        private readonly List<Button> plusButtons = new List<Button>();
        private ShopPC pc;
        private Font font;
        private bool ownsFont;
        private bool built;
        private Text feedback;
        private float feedbackUntil;
        private GameObject ownedEventSystem;
        private Sprite roundedSprite;
        private Texture2D roundedTexture;

        public void Build(ShopPC owner)
        {
            if (built) return;
            pc = owner;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Arial" }, 32);
            ownsFont = font != null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform background = Box("Background", transform, Color.white, 0, 0, 1, 1);
            RectTransform sidebar = Box("Sidebar", background, Navy, 0, 0, .18f, 1);
            Artwork(sidebar, -1, .34f, .86f, .66f, .97f);
            Label("Brand", sidebar, "DELIVERY\nSERVICE", 25, Color.white, .05f, .76f, .95f, .87f, TextAnchor.MiddleCenter);
            string[] names = { "주문", "영업", "발주" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                tabs[i] = ButtonAt("Tab_" + i, sidebar, names[i], Navy, Color.white,
                    .07f, .61f - i * .115f, .93f, .70f - i * .115f, () => SelectTab(index), 31);
                pages[i] = Box("Page_" + i, background, Color.white, .18f, .075f, 1, .94f).gameObject;
            }
            Label("OrderAlert", pages[0].transform, "주문이 들어왔어요!", 52, Navy, .08f, .53f, .92f, .70f, TextAnchor.MiddleCenter);
            ButtonAt("Accept", pages[0].transform, "수락하기", Teal, Color.white, .17f, .32f, .49f, .48f, () => Notify("수락하기"));
            OutlineButton("Reject", pages[0].transform, "거절하기", .52f, .32f, .84f, .48f, () => Notify("거절하기"));
            OutlineButton("CloseBusiness", pages[1].transform, "마감하기", .17f, .40f, .49f, .60f, () => Notify("마감하기"));
            ButtonAt("OpenBusiness", pages[1].transform, "영업하기", Teal, Color.white, .52f, .40f, .84f, .60f, () => Notify("영업하기"));
            BuildSupplyPage();
            ButtonAt("CloseWindow", background, "닫기  ×", Pale, Navy, .90f, .945f, .985f, .988f, Close, 19);
            Label("Hint", background, "E / ESC  닫기", 17, Navy, .20f, .012f, .37f, .052f);
            feedback = Label("ClickFeedback", background, "", 18, Teal, .40f, .012f, .98f, .052f, TextAnchor.MiddleRight);
            built = true;
            SelectTab(0);
        }

        private void BuildSupplyPage()
        {
            Transform page = pages[2].transform;
            Label("SupplyTitle", page, "재료 발주", 43, Navy, .04f, .88f, .96f, 1);
            RectTransform scrollRect = Box("IngredientsScroll", page, Color.white, .035f, .01f, .965f, .88f);
            scrollRect.gameObject.AddComponent<RectMask2D>();
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            scroll.viewport = scrollRect;
            RectTransform content = Box("Content", scrollRect, Color.white, 0, 1, 1, 1);
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(0, ingredients.Count * 94);
            scroll.content = content;
            for (int i = 0; i < ingredients.Count; i++)
            {
                int index = i;
                IngredientRow data = ingredients[i];
                RectTransform row = Box("Ingredient_" + i, content, Border, 0, 1, 1, 1);
                row.pivot = new Vector2(.5f, 1);
                row.anchoredPosition = new Vector2(0, -i * 94);
                row.sizeDelta = new Vector2(0, 84);
                RectTransform inner = Box("Card", row, Color.white, 0, 0, 1, 1);
                inner.offsetMin = new Vector2(2, 2);
                inner.offsetMax = new Vector2(-2, -2);
                RectTransform badge = Box("IngredientIcon", inner, Pale, .02f, .14f, .09f, .86f);
                if (data.icon != null)
                {
                    Image img = badge.GetComponent<Image>();
                    img.sprite = data.icon;
                    img.color = Color.white;
                    img.preserveAspect = true;
                }
                else Artwork(badge, data.artworkKind, .06f, .06f, .94f, .94f);
                Label("IngredientName", inner, data.name + " ×" + IngredientRow.UnitsPerBundle, 28, Navy, .12f, .1f, .53f, .9f);
                minusButtons.Add(ButtonAt("Minus", inner, "−", Pale, Navy, .55f, .16f, .61f, .84f, () => ChangeQuantity(index, -1), 29));
                quantityLabels.Add(Label("Quantity", inner, data.quantity.ToString(), 26, Navy, .62f, .15f, .69f, .85f, TextAnchor.MiddleCenter));
                plusButtons.Add(ButtonAt("Plus", inner, "+", Pale, Navy, .70f, .16f, .76f, .84f, () => ChangeQuantity(index, 1), 29));
                ButtonAt("Purchase", inner, "구매하기", Teal, Color.white, .80f, .16f, .98f, .84f,
                    () => Notify(ingredients[index].name + " " + ingredients[index].quantity + "묶음 (" + ingredients[index].TotalUnits + "개) 구매하기"), 25);
                RefreshQuantity(i);
            }
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= pages.Length) return;
            SelectedTab = index;
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(i == index);
                tabs[i].GetComponent<Image>().color = i == index ? Teal : Navy;
            }
            if (feedback != null) feedback.text = "";
        }

        public void ChangeQuantity(int index, int delta)
        {
            if (index < 0 || index >= ingredients.Count) return;
            IngredientRow row = ingredients[index];
            int min = Mathf.Max(1, row.minimum);
            int max = Mathf.Max(min, row.maximum);
            row.quantity = (int)Math.Max(min, Math.Min(max, (long)row.quantity + delta));
            RefreshQuantity(index);
        }

        private void RefreshQuantity(int index)
        {
            IngredientRow row = ingredients[index];
            int min = Mathf.Max(1, row.minimum);
            int max = Mathf.Max(min, row.maximum);
            row.quantity = Mathf.Clamp(row.quantity, min, max);
            quantityLabels[index].text = row.quantity.ToString();
            minusButtons[index].interactable = row.quantity > min;
            plusButtons[index].interactable = row.quantity < max;
        }

        private void Notify(string action)
        {
            LastClickedAction = action;
            ClickCount++;
            feedback.text = action + " 선택됨";
            feedbackUntil = Time.unscaledTime + 1.8f;
            ActionClicked?.Invoke(action);
        }

        public void Close() { gameObject.SetActive(false); }

        private void OnEnable()
        {
            if (!built) return;
            EnsureEventSystem();
            inputScope.Acquire(pc, false);
        }

        private void LateUpdate() { inputScope.Maintain(); }

        private void OnDisable()
        {
            inputScope.Release();
            if (!built) return;
            if (feedback != null) feedback.text = "";
        }

        private void Update()
        {
            if (feedback != null && Time.unscaledTime > feedbackUntil) feedback.text = "";
            for (int i = 0; i < quantityLabels.Count && i < ingredients.Count; i++) RefreshQuantity(i);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            ownedEventSystem = new GameObject("Yun UI EventSystem", typeof(EventSystem));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ownedEventSystem, gameObject.scene);
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            ownedEventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            ownedEventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private void OnDestroy()
        {
            ReleaseObject(ownedEventSystem);
            if (ownsFont) ReleaseObject(font);
            ReleaseObject(roundedSprite);
            ReleaseObject(roundedTexture);
        }

        private static void ReleaseObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        private void Artwork(Transform parent, int kind, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(ShopUiArtwork));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<ShopUiArtwork>().Kind = kind;
            go.GetComponent<ShopUiArtwork>().raycastTarget = false;
        }

        private Sprite RoundedSprite()
        {
            if (roundedSprite != null) return roundedSprite;
            const int size = 32;
            const float radius = 12;
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            roundedTexture.name = "Yun UI rounded mask";
            roundedTexture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - (x + .5f), x + .5f - (size - radius), 0);
                float dy = Mathf.Max(radius - (y + .5f), y + .5f - (size - radius), 0);
                float alpha = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                roundedTexture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            roundedTexture.Apply();
            roundedSprite = Sprite.Create(roundedTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return roundedSprite;
        }

        private RectTransform Box(string name, Transform parent, Color color, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.color = color;
            if (name != "Background" && name != "Sidebar" && !name.StartsWith("Page_") && name != "Content")
            {
                image.sprite = RoundedSprite();
                image.type = Image.Type.Sliced;
            }
            return rect;
        }

        private Text Label(string name, Transform parent, string value, int size, Color color,
            float x0, float y0, float x1, float y1, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        private Button ButtonAt(string name, Transform parent, string label, Color fill, Color ink,
            float x0, float y0, float x1, float y1, UnityEngine.Events.UnityAction action, int size = 35)
        {
            RectTransform rect = Box(name, parent, fill, x0, y0, x1, y1);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(.87f, .94f, .97f);
            colors.pressedColor = new Color(.65f, .78f, .84f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = .08f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(action);
            Label("Label", rect, label, size, ink, .02f, .02f, .98f, .98f, TextAnchor.MiddleCenter);
            return button;
        }

        private void OutlineButton(string name, Transform parent, string label,
            float x0, float y0, float x1, float y1, UnityEngine.Events.UnityAction action)
        {
            Button button = ButtonAt(name, parent, label, Color.white, Navy, x0, y0, x1, y1, action);
            var outline = button.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Navy;
            outline.effectDistance = new Vector2(2, -2);
        }
    }
}
