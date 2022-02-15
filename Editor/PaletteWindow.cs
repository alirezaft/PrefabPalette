using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.EditorCoroutines.Editor;


public class PaletteWindow : EditorWindow, IHasCustomMenu
{
    #region paths

    private const string MAIN_VISUAL_ASSET_TREE_PATH = "Packages/com.alirezaft.prefabpalette/Editor/PaletteWindow.uxml";
    private const string PREFAB_SLOT_VISUAL_TREE_PATH = "Packages/com.alirezaft.prefabpalette/Editor/PrefabSlot.uxml";
    private const string PREFAB_PALETTE_MENU_PATH = "Window/Prefab Palette";

    private const string NO_PREFAB_SELECTED_IMAGE_PATH =
        "Packages/com.alirezaft.prefabpalette/Editor/Textures/NoPrefabImg.jpg";

    private const string EMPTY_PREFAB_IMAGE_PATH =
        "Packages/com.alirezaft.prefabpalette/Editor/Textures/EmptyPrefab.png";

    private const string ADD_PREFAB_IMAGE_PATH = "Packages/com.alirezaft.prefabpalette/Editor/Textures/AddItem.png";

    #endregion

    #region uxml element names

    private const string SCROLL_VIEW_PREFAB_CONTAINER = "prefab-container";
    private const string IMAGE_PREFAB_FIELD = "prefab-field";
    private const string LABEL_PREFAB_NAME = "prefab-name";
    private const string PREFAB_SLOT_CONTAINER_NAME = "prefab-container-slot";
    private const string TEXT_FIELD_PREFAB_SEARCH_BAR = "prefab-search-field";

    #endregion

    #region uxml element classes

    private const string OBJECT_FIELD_DISPLAY_LABEL_CLASS = "unity-object-field-display__lebel";

    #endregion

    #region texts

    private const string NO_PREFAB_TEXT = "No  prefab selected";
    private const string DRAG_PREFABS_HERE_TEXT = "Drag prefabs here to add to the palette";

    #endregion

    #region numerical constants

    private const float PREFAB_PREVIEW_IMAGE_SIZE = 70;

    #endregion

    #region color values

    private const float EVEN_SLOT_GREY = 0.16f;
    private const float ODD_SLOT_GREY = 0.18f;

    #endregion

    private bool m_IsMouseOnElement;
    private bool m_IsInstantiating;
    private bool m_IsInContextMenu;
    private int m_NumberOfItems;
    private int m_CurrentIndex;
    private bool m_IsClicked;

    private List<GameObject> m_Palette;
    private List<GameObject> m_SearchResult;

    private ScrollView m_ScrollView;
    private VisualElement m_CurrentElemetn;
    private ToolbarSearchField m_PrefabSearchBar;

    private GameObject m_GetPreviewForThis;
    private bool m_ManualGeometryChange;
    private bool m_IsHorizontal = false;
    private bool m_AddingNewSlot = false;
    private bool m_IsSearching;


    private void OnEnable()
    {
        var uxmlFile = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MAIN_VISUAL_ASSET_TREE_PATH);
        var root = this.rootVisualElement;
        uxmlFile.CloneTree(root);
        m_NumberOfItems = 1;

        m_PrefabSearchBar = root.Q<ToolbarSearchField>(TEXT_FIELD_PREFAB_SEARCH_BAR);
        m_ScrollView = root.Query<ScrollView>(SCROLL_VIEW_PREFAB_CONTAINER).First();

        m_ScrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChange);

        m_PrefabSearchBar.RegisterCallback<ChangeEvent<string>>(OnSearchBarValueChanged);
        rootVisualElement.RegisterCallback<GeometryChangedEvent>(ChangeImageSizeOnWindowSizeChange);

        InstantiateAddPrefabSlot();
        m_Palette = new List<GameObject>();
        m_IsInstantiating = false;
        m_ManualGeometryChange = false;
        m_IsSearching = false;
        m_GetPreviewForThis = null;

        if (PlaymodePaletteKeeper.instance.m_TempPalette.Count > 0)
        {
            ReloadPaletteAfterPlayMode(PlaymodePaletteKeeper.instance.m_TempPalette);
        }

        m_SearchResult = new List<GameObject>();
    }

    private void ReloadPaletteAfterPlayMode(List<GameObject> pal)
    {
        m_ScrollView.Clear();
        InstantiateAddPrefabSlot();
        InstantiateNewPrefabSlot();
        int i = 0;
        foreach (GameObject gameObject in pal)
        {
            var img = rootVisualElement.Query<Image>().AtIndex(m_IsSearching ? m_ScrollView.childCount - 1 : m_ScrollView.childCount - 2);
            SetPrefabSelectorImage(GetAssetPreview(gameObject), img);
            var lbl = rootVisualElement.Query<Label>().AtIndex(m_IsSearching ? m_ScrollView.childCount - 1 : m_ScrollView.childCount - 2);
            SetPrefabLabel(gameObject.name, lbl);
            m_Palette.Add(gameObject);
            if (i != pal.Count - 1)
            {
                InstantiateNewPrefabSlot();
            }

            i++;
        }
    }

    private void ReloadPaletteForSearch(List<GameObject> pal)
    {
        m_ScrollView.Clear();
        Debug.Log("Search res count: " + pal.Count);
        if (pal.Count == 0) return;

        InstantiateNewPrefabSlot();
        int i = 0;
        foreach (GameObject gameObject in pal)
        {
            Debug.Log(gameObject.name);
            var img = rootVisualElement.Query<Image>().Last();
            SetPrefabSelectorImage(GetAssetPreview(gameObject), img);
            var lbl = rootVisualElement.Query<Label>().Last();
            SetPrefabLabel(gameObject.name, lbl);
//            m_Palette.Add(gameObject);
            if (i != pal.Count - 1)
            {
                InstantiateNewPrefabSlot();
            }

            i++;
        }
    }

    private void ReloadPaletteForSearch()
    {
        m_ScrollView.Clear();
        Debug.Log("Rebuilding your palette");
        InstantiateAddPrefabSlot();

        int i = 0;
        foreach (GameObject gameObject in m_Palette)
        {
            Debug.Log(gameObject.name);
            InstantiateNewPrefabSlot();
            var img = rootVisualElement.Query<Image>().AtIndex(m_ScrollView.childCount - 2);
            SetPrefabSelectorImage(GetAssetPreview(gameObject), img);
            var lbl = rootVisualElement.Query<Label>().AtIndex(m_ScrollView.childCount - 2);
            SetPrefabLabel(gameObject.name, lbl);
//            m_Palette.Add(gameObject);
            i++;
        }
    }

    private void ChangeImageSizeOnWindowSizeChange(GeometryChangedEvent evt)
    {
        var imgQuery = rootVisualElement.Query<Image>();
        imgQuery.ForEach((Image img) =>
        {
            if (position.width <= position.height)
            {
                EditorCoroutineUtility.StartCoroutine(SetHeight(img), this);
            }
            else
            {
                EditorCoroutineUtility.StartCoroutine(SetWidth(img), this);
            }
        });

        if (m_ManualGeometryChange)
        {
            m_ManualGeometryChange = false;
            evt.Dispose();
        }
    }

    private IEnumerator SetHeight(Image img)
    {
        yield return new EditorWaitForSeconds(0.02f);
        img.style.width = new StyleLength(new Length(PREFAB_PREVIEW_IMAGE_SIZE, LengthUnit.Percent));
        float percent = ((img.parent.contentRect.width * (img.style.width.value.value * 0.01f)) /
                         img.parent.contentRect.height) * 100;

        img.style.height = new StyleLength(new Length(img.contentRect.width, LengthUnit.Pixel));
    }

    private IEnumerator SetWidth(Image img)
    {
        yield return new EditorWaitForSeconds(0.02f);
        img.style.height = new StyleLength(new Length(PREFAB_PREVIEW_IMAGE_SIZE, LengthUnit.Percent));

        float percent = ((img.parent.contentRect.height * (img.style.height.value.value * 0.01f)) /
                         img.parent.contentRect.width) * 100;

        img.style.width = new StyleLength(new Length(img.contentRect.height, LengthUnit.Pixel));
    }


    public void AddItemsToMenu(GenericMenu menu)
    {
        menu.AddItem(new GUIContent("Save palette"), false, SavePalette);
        menu.AddItem(new GUIContent("Load palette"), false, LoadPalette);
        menu.AddItem(new GUIContent("New palette"), false, NewPalette);
    }

    [MenuItem(PREFAB_PALETTE_MENU_PATH)]
    public static void OpenPrefabPalette()
    {
        var win = EditorWindow.GetWindow<PaletteWindow>(false, "Prefab Palette", true);
        win.Show();
    }

    private void OnScrollViewGeometryChange(GeometryChangedEvent evt)
    {
        if (!m_ScrollView.verticalScroller.ClassListContains(VisualElement.disabledUssClassName) && m_AddingNewSlot)
        {
            m_ScrollView.verticalScroller.value = m_ScrollView.verticalScroller.highValue;
            m_AddingNewSlot = false;
        }
    }

    private void InstantiateNewPrefabSlot()
    {
        m_AddingNewSlot = true;
        var newSlot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PREFAB_SLOT_VISUAL_TREE_PATH);
//        Debug.Log(m_ScrollView.ElementAt(m_ScrollView.childCount - 1).Q<Label>().t);
//        newSlot.Instantiate().PlaceBehind(m_ScrollView.ElementAt(m_ScrollView.childCount - 1));
        if(!m_IsSearching){
            m_ScrollView.Insert(m_ScrollView.childCount == 1 ? 0 : m_ScrollView.childCount - 1, newSlot.Instantiate());
        }
        else
        {
            m_ScrollView.Add(newSlot.Instantiate());
        }
        var createdField = m_ScrollView.Query<Image>(IMAGE_PREFAB_FIELD).AtIndex(m_IsSearching ? m_ScrollView.childCount - 1 : m_ScrollView.childCount - 2);

        if (m_ScrollView.childCount % 2 == 0)
        {
            SetSlotBackgroundColor(m_ScrollView.Query<TemplateContainer>().AtIndex(m_IsSearching ? m_ScrollView.childCount - 1 : m_ScrollView.childCount - 2),
                new Color(EVEN_SLOT_GREY, EVEN_SLOT_GREY, EVEN_SLOT_GREY));
        }
        else
        {
            SetSlotBackgroundColor(m_ScrollView.Query<TemplateContainer>().AtIndex(m_IsSearching ? m_ScrollView.childCount - 1 : m_ScrollView.childCount - 2),
                new Color(ODD_SLOT_GREY, ODD_SLOT_GREY, ODD_SLOT_GREY));
        }

        RegisterDragAndDropCallbacks(createdField);
        SetPrefabSelectorImage(AssetDatabase.LoadAssetAtPath<Texture2D>(NO_PREFAB_SELECTED_IMAGE_PATH), createdField);
        SetPrefabLabel(NO_PREFAB_TEXT,
            m_ScrollView.Query<Label>(LABEL_PREFAB_NAME).AtIndex(m_IsSearching ? m_ScrollView.childCount - 1 : m_ScrollView.childCount - 2));

        m_NumberOfItems++;
        m_ScrollView.ScrollTo(m_ScrollView.Query<Image>(IMAGE_PREFAB_FIELD).Last());
    }

    private void InstantiateAddPrefabSlot()
    {
        var newSlot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PREFAB_SLOT_VISUAL_TREE_PATH);
        m_ScrollView.Add(newSlot.Instantiate());
        var createdField = m_ScrollView.Query<Image>(IMAGE_PREFAB_FIELD).Last();

        if (m_ScrollView.childCount % 2 == 0)
        {
            SetSlotBackgroundColor(m_ScrollView.Query<TemplateContainer>().Last(),
                new Color(EVEN_SLOT_GREY, EVEN_SLOT_GREY, EVEN_SLOT_GREY));
        }
        else
        {
            SetSlotBackgroundColor(m_ScrollView.Query<TemplateContainer>().Last(),
                new Color(ODD_SLOT_GREY, ODD_SLOT_GREY, ODD_SLOT_GREY));
        }

        SetPrefabSelectorImage(AssetDatabase.LoadAssetAtPath<Texture2D>(ADD_PREFAB_IMAGE_PATH), createdField);
        var lbl = m_ScrollView.Query<Label>(LABEL_PREFAB_NAME).Last();
        SetPrefabLabel(DRAG_PREFABS_HERE_TEXT, lbl);
        lbl.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
        lbl.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
        RegisterCallbacksForAddPrefabSlot(m_ScrollView.Query<Image>().Last());
    }

    private void SetSlotBackgroundColor(VisualElement vis, Color col)
    {
        vis.style.backgroundColor = new StyleColor(col);
    }

    private void RegisterDragAndDropCallbacks(Image element)
    {
        element.RegisterCallback<DragPerformEvent>(OnPrefabDroppedIntoSlot);
        element.RegisterCallback<DragExitedEvent>(OnPrefabDragExit);
        element.RegisterCallback<MouseEnterEvent>(OnMouseEnteredSlotBounds);
        element.RegisterCallback<MouseLeaveEvent>(OnPrefabLeaveSlotBounds);
        element.RegisterCallback<DragUpdatedEvent>(OnDraggingPrefabUpdated);
        element.RegisterCallback<PointerDownEvent>(OnPrefabSlotDragStart);
        element.RegisterCallback<PointerMoveEvent>(PointerMove);
        element.RegisterCallback<PointerUpEvent>(OnPrefabSlotDragFinished);
        element.RegisterCallback<DragLeaveEvent>(OnPrefabSlotDragLEave);
        element.AddManipulator(new ContextualMenuManipulator(OnRightClickOnPrefabPreview));
    }

    private void RegisterCallbacksForAddPrefabSlot(Image el)
    {
        el.RegisterCallback<DragPerformEvent>(OnPrefabDroppedInAddItemArea);
        el.RegisterCallback<DragLeaveEvent>(OnPrefabSlotDragLEave);
        el.RegisterCallback<DragUpdatedEvent>(OnPrefabDraggedOverAddItemArea);
    }

    private void OnPrefabDraggedOverAddItemArea(DragUpdatedEvent evt)
    {
        var element = evt.target as VisualElement;
        var parent = element.parent.parent;
        var index = m_ScrollView.IndexOf(element.parent.parent);

        var prefabs = DragAndDrop.objectReferences;
        if (prefabs.Count(a => a is GameObject) == prefabs.Length &&
            prefabs.Count(AssetDatabase.Contains) == prefabs.Length)
        {
            ChangeBorderColor(element, Color.green);
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        }
        else if (!m_IsInstantiating)
        {
            ChangeBorderColor(element, Color.red);
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }
    }

    private void OnPrefabDroppedInAddItemArea(DragPerformEvent evt)
    {
        Debug.Log("I GOT YOUR THING!");
        var element = evt.target as VisualElement;
        var index = m_ScrollView.IndexOf(element.parent.parent);
        // EditorUtility.Select
        var prefabs = DragAndDrop.objectReferences;
        ChangeBorderColor(m_ScrollView.Query<Image>().Last());
        if (prefabs.Count(a => a is GameObject) == prefabs.Length &&
            prefabs.Count(AssetDatabase.Contains) == prefabs.Length)
        {
            foreach (var go in prefabs)
            {
                m_Palette.Add(go as GameObject);
                PlaymodePaletteKeeper.instance.m_TempPalette.Add(go as GameObject);
                InstantiateNewPrefabSlot();
                var img = m_ScrollView.Query<Image>().AtIndex(m_ScrollView.childCount - 2);
                var lbl = m_ScrollView.Query<Label>().AtIndex(m_ScrollView.childCount - 2);

                var preview = GetAssetPreview(go as GameObject);

                if (preview != null)
                {
                    SetPrefabSelectorImage(preview, img);
                }
                else
                {
                    SetPrefabSelectorImage(AssetDatabase.LoadAssetAtPath<Texture2D>(EMPTY_PREFAB_IMAGE_PATH),
                        img);
                }
                
                SetPrefabLabel(go.name, lbl);

            }
        }
        else
        {
            Debug.LogWarning("Objects you dragged are either not from your assets or not a prefab");
        }

        
    }

    private void OnPrefabSlotDragLEave(DragLeaveEvent evt)
    {
        var element = evt.target as VisualElement;
        // var index = m_ScrollView.IndexOf(element.parent.parent);

        ChangeBorderColor(element);
        m_IsInstantiating = false;
    }

    private void OnRightClickOnPrefabPreview(ContextualMenuPopulateEvent evt)
    {
        var element = evt.target as VisualElement;
        m_CurrentIndex = m_ScrollView.IndexOf(element.parent.parent);
        m_IsInContextMenu = true;
        
        evt.menu.AppendAction("Remove prefab", RemovePrefab);

        evt.menu.AppendAction("Ping", PingPrefabAsset);
    }

    private void OnSearchBarValueChanged(ChangeEvent<string> evt)
    {
        m_SearchResult.Clear();
        Debug.Log("\"" + evt.newValue + "\"");
        if (evt.newValue.Equals(""))
        {
            m_IsSearching = false;
            ReloadPaletteForSearch();
            return;
        }

        m_IsSearching = true;
        foreach (GameObject gameObject in m_Palette)
        {
            if (gameObject.name.ToLower().Contains(evt.newValue.ToLower()))
            {
                m_SearchResult.Add(gameObject);
            }
        }

        ReloadPaletteForSearch(m_SearchResult);
    }

    private void RemovePrefab(DropdownMenuAction action)
    {

        if (!m_IsSearching)
        {
            m_Palette.RemoveAt(m_CurrentIndex);
            PlaymodePaletteKeeper.instance.m_TempPalette.RemoveAt(m_CurrentIndex);
        }
        else
        {
            m_Palette.RemoveAt(m_Palette.IndexOf(m_SearchResult[m_CurrentIndex]));
            PlaymodePaletteKeeper.instance.m_TempPalette.RemoveAt(PlaymodePaletteKeeper.instance.m_TempPalette.IndexOf(m_SearchResult[m_CurrentIndex]));
            m_SearchResult.RemoveAt(m_CurrentIndex);
            m_ScrollView.RemoveAt(m_CurrentIndex);
        }

        m_IsInContextMenu = false;
        m_CurrentIndex = -1;
    }

    private void SavePalette()
    {
        PaletteData data = ScriptableObject.CreateInstance<PaletteData>();
        data.Palette = new List<GameObject>(m_Palette);
        var path = EditorUtility.SaveFilePanel("Save Prefab palette", "Assets", "", "asset");

        path = TurnFullPathToUnityPath(path);
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
    }

    private void LoadPalette()
    {
        string path = EditorUtility.OpenFilePanel("Choose palette", "Assets", "asset");
        path = TurnFullPathToUnityPath(path);


        if (!AssetDatabase.GetMainAssetTypeAtPath(path).Equals(typeof(PaletteData)))
        {
            Debug.LogError("The chosen asset is not a Palette data. Please choose another asset.");
            return;
        }

        var loadedPalette = AssetDatabase.LoadAssetAtPath<PaletteData>(path);
        PaintUIOnLoad(loadedPalette);
    }

    private void NewPalette()
    {
        PlaymodePaletteKeeper.instance.m_TempPalette.Clear();
        m_ScrollView.Clear();
        m_Palette.Clear();
        InstantiateAddPrefabSlot();
    }

    private void PaintUIOnLoad(PaletteData loadedPalette)
    {
        PlaymodePaletteKeeper.instance.m_TempPalette = new List<GameObject>(loadedPalette.Palette);
        m_ScrollView.Clear();
        m_Palette.Clear();
        int i = 0;
        InstantiateAddPrefabSlot();
        
        foreach (GameObject go in loadedPalette.Palette)
        {
            InstantiateNewPrefabSlot();
            var slotimg = m_ScrollView.Query<Image>("prefab-field").AtIndex(m_ScrollView.childCount - 2);
            var preview = GetAssetPreview(go);

            if (preview != null)
            {
                SetPrefabSelectorImage(GetAssetPreview(go), slotimg);
            }
            else
            {
                SetPrefabSelectorImage(AssetDatabase.LoadAssetAtPath<Texture2D>(EMPTY_PREFAB_IMAGE_PATH), slotimg);
            }

            var slotlabel = m_ScrollView.Query<Label>("prefab-name").AtIndex(m_ScrollView.childCount - 2);
            m_Palette.Add(go);
            SetPrefabLabel(go.name, slotlabel);
            i++;
        }
    }


    private string TurnFullPathToUnityPath(string path)
    {
        path = path.Remove(0, path.IndexOf("/Assets/"));
        path = path.Remove(0, 1);

        return path;
    }

    private void OnPrefabDroppedIntoSlot(DragPerformEvent evt)
    {
        var element = evt.target as VisualElement;
        var index = m_ScrollView.IndexOf(element.parent.parent);
        // EditorUtility.Select
        var prefabs = DragAndDrop.objectReferences;
        if (prefabs.Length == 1)
        {
            if (prefabs[0] is GameObject && AssetDatabase.Contains(prefabs[0]))
            {
                
                m_Palette[index] = prefabs[0] as GameObject;
                PlaymodePaletteKeeper.instance.m_TempPalette[index] = prefabs[0] as GameObject;
//                    m_Palette.Add(prefabs[0] as GameObject);
                
                

                var preview = GetAssetPreview(prefabs[0] as GameObject);

                if (preview != null)
                {
                    SetPrefabSelectorImage(GetAssetPreview(prefabs[0] as GameObject), element as Image);
                }
                else
                {
                    SetPrefabSelectorImage(AssetDatabase.LoadAssetAtPath<Texture2D>(EMPTY_PREFAB_IMAGE_PATH),
                        element as Image);
                }

                SetPrefabLabel(prefabs[0].name, element.parent.Q<Label>());
            }
        }
        else
        {
            Debug.LogWarning("You can't drop more than one prefab into a single slot.");
        }

        ChangeBorderColor(element, Color.white);
    }

    private void OnDraggingPrefabUpdated(DragUpdatedEvent evt)
    {
        var element = evt.target as VisualElement;
        var parent = element.parent.parent;
        var index = m_ScrollView.IndexOf(element.parent.parent);

        if (DragAndDrop.objectReferences[0] is GameObject && AssetDatabase.Contains(DragAndDrop.objectReferences[0]) &&
            !m_IsInstantiating)
        {
            ChangeBorderColor(element, Color.green);
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        }
        else if (!m_IsInstantiating)
        {
            ChangeBorderColor(element, Color.red);
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }
    }


    private void OnMouseEnteredSlotBounds(MouseEnterEvent evt)
    {
        var element = evt.target as VisualElement;
        var parent = element.parent.parent;

        if (DragAndDrop.objectReferences.Length == 0)
        {
            ChangeBorderColor(element, Color.white);
            m_CurrentIndex = m_ScrollView.IndexOf(element.parent.parent);
        }
    }

    private void OnPrefabLeaveSlotBounds(MouseLeaveEvent evt)
    {
        var element = evt.target as VisualElement;
        if (!m_IsInstantiating)
        {
            ChangeBorderColor(element);
        }

        if (!m_IsInContextMenu)
            m_CurrentIndex = -1;

        m_IsMouseOnElement = false;
    }

    private void OnPrefabDragExit(DragExitedEvent evt)
    {
    }

    private void OnPrefabSlotDragStart(PointerDownEvent evt)
    {
        var element = evt.target as VisualElement;
        var parent = element.parent.parent;
        var index = m_ScrollView.IndexOf(element.parent.parent);

        m_CurrentElemetn = element;

        if (!m_IsInstantiating && evt.button != 1)
        {
            m_CurrentIndex = index;
            ChangeBorderColor(element, Color.blue);
            m_IsInstantiating = true;
            m_IsClicked = true;
        }
    }

    private void PointerMove(PointerMoveEvent evt)
    {
        if (m_IsClicked && !m_IsSearching)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.StartDrag("Instantiate");
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            DragAndDrop.objectReferences = new Object[] {m_Palette[m_CurrentIndex]};
            m_IsClicked = false;
        }
        else if (m_IsClicked && m_IsSearching)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.StartDrag("Instantiate");
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            DragAndDrop.objectReferences = new Object[] {m_SearchResult[m_CurrentIndex]};
            m_IsClicked = false;
        }
    }

    private void OnPrefabSlotDragFinished(PointerUpEvent evt)
    {
        var element = evt.target as VisualElement;
        var parent = element.parent.parent;
        var index = m_ScrollView.IndexOf(element.parent.parent);
        m_IsClicked = false;

        DragAndDrop.visualMode = DragAndDropVisualMode.None;
        m_IsInstantiating = false;
        ChangeBorderColor(element);
    }

    private void SetPrefabLabel(string text, Label label)
    {
        label.text = text;
    }

    private void SetPrefabSelectorImage(Texture2D background, Image selector)
    {
        selector.image = background;
        selector.style.width = new StyleLength(new Length(PREFAB_PREVIEW_IMAGE_SIZE, LengthUnit.Percent));

        float percent = ((selector.parent.contentRect.width * (selector.contentRect.width * 0.01f)) /
                         selector.parent.contentRect.height) * 100;
        selector.style.height = new StyleLength(new Length(selector.contentRect.width, LengthUnit.Pixel));

        EditorCoroutineUtility.StartCoroutine(CallGeometryChangedManually(), this);
    }

    private IEnumerator CallGeometryChangedManually()
    {
        yield return new EditorWaitForSeconds(0.002f);
        m_ManualGeometryChange = true;
        var evt = GeometryChangedEvent.GetPooled(rootVisualElement.contentRect, rootVisualElement.contentRect);
        ChangeImageSizeOnWindowSizeChange(evt);
    }

    private void PingPrefabAsset(DropdownMenuAction action)
    {
        if (m_IsSearching)
        {
            EditorGUIUtility.PingObject(m_SearchResult[m_CurrentIndex]);
        }
        else
        {
            EditorGUIUtility.PingObject(m_Palette[m_CurrentIndex]);
        }
    }

    private void ChangeBorderColor(VisualElement element, Color color)
    {
        element.style.borderTopColor = new StyleColor(color);
        element.style.borderBottomColor = new StyleColor(color);
        element.style.borderLeftColor = new StyleColor(color);
        element.style.borderRightColor = new StyleColor(color);
        element.style.borderBottomWidth = new StyleFloat(3f);
        element.style.borderRightWidth = new StyleFloat(3f);
        element.style.borderLeftWidth = new StyleFloat(3f);
        element.style.borderTopWidth = new StyleFloat(3f);
    }

    private void ChangeBorderColor(VisualElement element)
    {
        element.style.borderTopColor = new StyleColor();
        element.style.borderBottomColor = new StyleColor();
        element.style.borderLeftColor = new StyleColor();
        element.style.borderRightColor = new StyleColor();
    }

    private Texture2D GetAssetPreview(GameObject obj)
    {
        if (obj.GetComponent<Renderer>() == null && obj.GetComponentInChildren<Renderer>() == null)
        {
            return null;
        }

        Texture2D res = null;

        do
        {
            res = AssetPreview.GetAssetPreview(obj);
        } while (res == null);

        return res;
    }
}