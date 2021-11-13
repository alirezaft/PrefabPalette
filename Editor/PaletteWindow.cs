using System.Collections;
using System.Collections.Generic;
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
    private const string NO_PREFAB_SELECTED_IMAGE_PATH = "Packages/com.alirezaft.prefabpalette/Editor/Textures/NoPrefabImg.jpg";
    private const string EMPTY_PREFAB_IMAGE_PATH = "Packages/com.alirezaft.prefabpalette/Editor/Textures/EmptyPrefab.png";
    #endregion

    #region uxml element names
    private const string BUTTON_ADD_PREFAB = "add-prefab-button";
    private const string SCROLL_VIEW_PREFAB_CONTAINER = "prefab-container";
    private const string IMAGE_PREFAB_FIELD = "prefab-field";
    private const string LABEL_PREFAB_NAME = "prefab-name";
    private const string PREFAB_SLOT_CONTAINER_NAME = "prefab-container-slot";
    #endregion

    #region uxml element classes
    private const string OBJECT_FIELD_DISPLAY_LABEL_CLASS = "unity-object-field-display__lebel";
    #endregion
    
    #region texts
    private const string NO_PREFAB_TEXT = "No  prefab selected";
    private const string PALETTE_GAMEOBJECT_NAME_EDITOR_PREFS = "palgo";
    private const string PALETTE_SIZE_EDITOR_PREFS = "PaletteSize";
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
    private Dictionary<int, int> slotToListDictionary;

    private ScrollView m_ScrollView;
    private VisualElement m_CurrentElemetn;

    private GameObject m_GetPreviewForThis;
    private static PaletteWindow m_Instance;

    private bool m_ManualGeometryChange;
    private bool m_IsHorizontal = false;
    private bool m_AddingNewSlot = false;
    

    private void OnEnable(){
        var uxmlFile = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MAIN_VISUAL_ASSET_TREE_PATH);
        var root = this.rootVisualElement;
        uxmlFile.CloneTree(root);
        m_NumberOfItems = 1;
        
        root.Query<Button>(BUTTON_ADD_PREFAB).First().clicked += OnAddSlotButtonPressed;
        m_ScrollView = root.Query<ScrollView>(SCROLL_VIEW_PREFAB_CONTAINER).First();
        m_ScrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChange);
        rootVisualElement.RegisterCallback<GeometryChangedEvent>(ChangeImageSizeOnWindowSizeChange);
        InstantiateNewPrefabSlot();
        m_Palette = new List<GameObject>();
        slotToListDictionary = new Dictionary<int, int>();
        m_IsInstantiating = false;
        m_ManualGeometryChange = false;
        m_GetPreviewForThis = null;
        int previousPaletteSize = 0;
        if(EditorPrefs.HasKey(PALETTE_SIZE_EDITOR_PREFS)){
            previousPaletteSize = EditorPrefs.GetInt(PALETTE_SIZE_EDITOR_PREFS);
        }
        if(previousPaletteSize != 0){
            ReloadPaletteAfterPlayMode(previousPaletteSize);
        }
    }

    private void ReloadPaletteAfterPlayMode(int size){
        Debug.Log(size);
        for(int i = 0; i < size; i++){
            var elementName = PALETTE_GAMEOBJECT_NAME_EDITOR_PREFS + i;
            var guid = EditorPrefs.GetString(elementName);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            m_Palette.Add(go);
            slotToListDictionary.Add(i, i);
            SetPrefabSelectorImage(GetAssetPreview(go), rootVisualElement.Query<Image>().Last());
            SetPrefabLabel(go.name, rootVisualElement.Query<Label>().Last());
            if(i != size - 1){
                InstantiateNewPrefabSlot();
            }
        }
    }

    private void OnGUI() {
        var el = rootVisualElement.Q<Image>();
        if(position.width > position.height){
            rootVisualElement.style.flexDirection = FlexDirection.Row;
            m_ScrollView.contentContainer.style.flexDirection = FlexDirection.Row;
            m_ScrollView.contentViewport.style.flexDirection = FlexDirection.Row;
            m_IsHorizontal = true;
        }else{
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            m_ScrollView.contentViewport.style.flexDirection = FlexDirection.Column;
            m_ScrollView.contentContainer.style.flexDirection = FlexDirection.Column;
            m_IsHorizontal = false;
        }
    }

    private void ChangeImageSizeOnWindowSizeChange(GeometryChangedEvent evt){  
        var imgQuery = rootVisualElement.Query<Image>();
        imgQuery.ForEach((Image img) => {
            if(position.width <= position.height){
                EditorCoroutineUtility.StartCoroutine(SetHeight(img), this);
            }else{
                EditorCoroutineUtility.StartCoroutine(SetWidth(img), this);
            }
        });

        if(m_ManualGeometryChange){
            m_ManualGeometryChange = false;
            evt.Dispose();
        }
    }

    private IEnumerator SetHeight(Image img){
        
        yield return new EditorWaitForSeconds(0.02f);
        img.style.width = new StyleLength(new Length(PREFAB_PREVIEW_IMAGE_SIZE, LengthUnit.Percent));
        float percent = ((img.parent.contentRect.width * (img.style.width.value.value * 0.01f)) / img.parent.contentRect.height) * 100;

        img.style.height = new StyleLength(new Length(img.contentRect.width, LengthUnit.Pixel));
    }

    private IEnumerator SetWidth(Image img){
        yield return new EditorWaitForSeconds(0.02f);
        img.style.height = new StyleLength(new Length(PREFAB_PREVIEW_IMAGE_SIZE, LengthUnit.Percent));

        float percent = ((img.parent.contentRect.height * (img.style.height.value.value * 0.01f)) / img.parent.contentRect.width) * 100;

        img.style.width = new StyleLength(new Length(img.contentRect.height, LengthUnit.Pixel));
    }
    

    public void AddItemsToMenu(GenericMenu menu){
        menu.AddItem(new GUIContent("Save palette"), false, SavePalette);
        menu.AddItem(new GUIContent("Load palette"), false, LoadPalette);
        menu.AddItem(new GUIContent("New palette"), false, NewPalette);
    }

    [MenuItem(PREFAB_PALETTE_MENU_PATH)]
    public static void OpenPrefabPalette(){
        m_Instance = EditorWindow.GetWindow<PaletteWindow>(false, "Prefab Palette", true);
        m_Instance.Show();
    }

    private void OnScrollViewGeometryChange(GeometryChangedEvent evt){
         
        if(!m_ScrollView.verticalScroller.ClassListContains(VisualElement.disabledUssClassName) && m_AddingNewSlot){
            m_ScrollView.verticalScroller.value = m_ScrollView.verticalScroller.highValue; 
            m_AddingNewSlot = false;
        }
        
    }

    private void InstantiateNewPrefabSlot(){
        m_AddingNewSlot = true;
        var newSlot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PREFAB_SLOT_VISUAL_TREE_PATH); 
        m_ScrollView.Add(newSlot.Instantiate()); 
        var createdField = m_ScrollView.Query<Image>(IMAGE_PREFAB_FIELD).Last();

        if(m_ScrollView.childCount % 2 == 0){
            SetSlotBackgroundColor(m_ScrollView.Query<TemplateContainer>().Last(), 
            new Color(EVEN_SLOT_GREY, EVEN_SLOT_GREY, EVEN_SLOT_GREY));
        }else{
            SetSlotBackgroundColor(m_ScrollView.Query<TemplateContainer>().Last(), 
            new Color(ODD_SLOT_GREY, ODD_SLOT_GREY, ODD_SLOT_GREY)); 
        }

        RegisterDragAndDropCallbacks(createdField);
        SetPrefabSelectorImage(AssetDatabase.LoadAssetAtPath<Texture2D>(NO_PREFAB_SELECTED_IMAGE_PATH), createdField);
        SetPrefabLabel(NO_PREFAB_TEXT, m_ScrollView.Query<Label>(LABEL_PREFAB_NAME).Last());

        m_NumberOfItems++; 
        m_ScrollView.ScrollTo(m_ScrollView.Query<Image>(IMAGE_PREFAB_FIELD).Last());      
    }

    private void SetSlotBackgroundColor(VisualElement vis, Color col){
        vis.style.backgroundColor = new StyleColor(col);
    }

    private void RegisterDragAndDropCallbacks(Image element){
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

    private void OnPrefabSlotDragLEave(DragLeaveEvent evt){
        
        var element = evt.target as VisualElement;
        // var index = m_ScrollView.IndexOf(element.parent.parent);

        ChangeBorderColor(element);
        m_IsInstantiating = false;
    }

    private void OnRightClickOnPrefabPreview(ContextualMenuPopulateEvent evt){
        var element = evt.target as VisualElement;
        m_CurrentIndex = m_ScrollView.IndexOf(element.parent.parent);
        m_IsInContextMenu = true;
        
        bool foundGo = slotToListDictionary.ContainsKey(m_CurrentIndex) && m_Palette[slotToListDictionary[m_CurrentIndex]] != null;
        
        evt.menu.AppendAction("Remove prefab", RemovePrefab, foundGo ? DropdownMenuAction.Status.Normal : 
         DropdownMenuAction.Status.Disabled);

        evt.menu.AppendAction("Ping", PingPrefabAsset, foundGo ? DropdownMenuAction.Status.Normal :
         DropdownMenuAction.Status.Disabled);
    }

    private void RemovePrefab(DropdownMenuAction action){
        var el = m_ScrollView.ElementAt(m_CurrentIndex);

        if (slotToListDictionary.Count == 1 && m_Palette.Count == 1 && m_ScrollView.childCount == 1)
        {
            m_Palette.Clear();
            slotToListDictionary.Clear();
            SetPrefabLabel(NO_PREFAB_TEXT, el.Q<Label>());
            SetPrefabSelectorImage(AssetDatabase.LoadAssetAtPath<Texture2D>(NO_PREFAB_SELECTED_IMAGE_PATH), el.Q<Image>());
            return;
        }
        if (m_CurrentIndex == m_ScrollView.childCount - 1)
        {
            m_Palette.RemoveAt(slotToListDictionary[m_CurrentIndex]);
            slotToListDictionary.Remove(m_CurrentIndex);
            SetPrefabSelectorImage(AssetDatabase.LoadAssetAtPath<Texture2D>(NO_PREFAB_SELECTED_IMAGE_PATH), 
             el.Q<Image>());
            SetPrefabLabel(NO_PREFAB_TEXT, el.Q<Label>()); 
            return; 
        }
        
        
        for (int i = m_CurrentIndex; i <slotToListDictionary.Count - 1; i++)
        {
            if(i == m_CurrentIndex)
            {
                m_Palette.RemoveAt(slotToListDictionary[m_CurrentIndex]);
            }
            
            slotToListDictionary[i] = slotToListDictionary[i + 1] - 1;
        }
          
        slotToListDictionary.Remove(m_ScrollView.childCount - 1);
        {
            m_ScrollView.RemoveAt(m_CurrentIndex);
        }

        m_IsInContextMenu = false;
        m_CurrentIndex = -1;
    }

    
    private void SavePalette(){
        PaletteData data = ScriptableObject.CreateInstance<PaletteData>();
        data.Palette = new List<GameObject>(m_Palette);
        var path = EditorUtility.SaveFilePanel("Save Prefab palette", "Assets", "", "asset");
        
        path = TurnFullPathToUnityPath(path);
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
    }

    private void LoadPalette(){
        string path = EditorUtility.OpenFilePanel("Choose palette", "Assets", "asset");
        path = TurnFullPathToUnityPath(path);
        
        if(!AssetDatabase.GetMainAssetTypeAtPath(path).Equals(typeof(PaletteData))){
            Debug.LogError("The chosen asset is not a Palette data. Please choose another asset.");
            return;
        }

        var loadedPalette = AssetDatabase.LoadAssetAtPath<PaletteData>(path);
        PaintUIOnLoad(loadedPalette);
    }

    private void NewPalette(){
        m_ScrollView.Clear();
        m_Palette.Clear();
        slotToListDictionary.Clear();
        InstantiateNewPrefabSlot();
    }

    private void PaintUIOnLoad(PaletteData loadedPalette){
        EditorPrefs.SetInt(PALETTE_SIZE_EDITOR_PREFS, 0);
        m_ScrollView.Clear();
        m_Palette.Clear();
        slotToListDictionary.Clear();
        int i = 0;

        foreach(GameObject go in loadedPalette.Palette){
            InstantiateNewPrefabSlot();
            var slotimg = m_ScrollView.Query<Image>("prefab-field").Last();
            var preview = GetAssetPreview(go);
            
            if(preview != null){
                SetPrefabSelectorImage(GetAssetPreview(go), slotimg);
            }else{
                SetPrefabSelectorImage(AssetDatabase.LoadAssetAtPath<Texture2D>(EMPTY_PREFAB_IMAGE_PATH), slotimg);
            }

            var slotlabel = m_ScrollView.Query<Label>("prefab-name").Last();
            slotToListDictionary.Add(i, i);
            m_Palette.Add(go);
            SetPrefabLabel(go.name, slotlabel);
            string guid;
            long fileid;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(go, out guid, out fileid);
            EditorPrefs.SetString(PALETTE_GAMEOBJECT_NAME_EDITOR_PREFS + 1, guid);
            EditorPrefs.SetInt(PALETTE_SIZE_EDITOR_PREFS, m_Palette.Count);
            i++;
        }
    }

    

    private string TurnFullPathToUnityPath(string path){
        path = path.Remove(0, path.IndexOf("/Assets/"));
        path = path.Remove(0, 1);

        return path;
    }

    private void OnPrefabDroppedIntoSlot(DragPerformEvent evt)
    {
        if(EditorPrefs.HasKey(PALETTE_SIZE_EDITOR_PREFS)){
            EditorPrefs.SetInt(PALETTE_SIZE_EDITOR_PREFS, EditorPrefs.GetInt(PALETTE_SIZE_EDITOR_PREFS) + 1);
        }else{
            EditorPrefs.SetInt(PALETTE_SIZE_EDITOR_PREFS, 1);
        }

        var element = evt.target as VisualElement;
        var index = m_ScrollView.IndexOf(element.parent.parent);
        // EditorUtility.Select
        var prefabs = DragAndDrop.objectReferences;
        if(prefabs.Length > 0){
            if(prefabs[0] is GameObject && AssetDatabase.Contains(prefabs[0])){
                if (!slotToListDictionary.ContainsKey(index)) 
                {
                    m_Palette.Add(prefabs[0] as GameObject);
                    slotToListDictionary.Add(index, m_Palette.Count - 1);
                    string guid;
                    long fileid;
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefabs[0], out guid, out fileid);
                    EditorPrefs.SetString(PALETTE_GAMEOBJECT_NAME_EDITOR_PREFS + (m_Palette.Count - 1), guid);
//                    m_Palette.Add(prefabs[0] as GameObject);
                }
                else
                {
                    m_Palette.Insert(slotToListDictionary[index], prefabs[0] as GameObject); 
                }
                
                var preview = GetAssetPreview(prefabs[0] as GameObject);

                if(preview != null){
                    SetPrefabSelectorImage(GetAssetPreview(prefabs[0] as GameObject), element as Image);
                }else{
                    SetPrefabSelectorImage(AssetDatabase.LoadAssetAtPath<Texture2D>(EMPTY_PREFAB_IMAGE_PATH), element as Image);
                }
                SetPrefabLabel(prefabs[0].name, element.parent.Q<Label>());
                
            }
        }
        ChangeBorderColor(element, Color.white);
    }

    private void OnDraggingPrefabUpdated(DragUpdatedEvent evt){
        var element = evt.target as VisualElement;
        var parent = element.parent.parent;
        var index = m_ScrollView.IndexOf(element.parent.parent);
        
        if(DragAndDrop.objectReferences[0] is GameObject && AssetDatabase.Contains(DragAndDrop.objectReferences[0]) && !m_IsInstantiating){
            ChangeBorderColor(element, Color.green);
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        }else if(!m_IsInstantiating){
            ChangeBorderColor(element, Color.red);
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }
    }


    private void OnMouseEnteredSlotBounds(MouseEnterEvent evt){
        
        
        var element = evt.target as VisualElement;
        var parent = element.parent.parent;
        
        if(DragAndDrop.objectReferences.Length == 0){
            ChangeBorderColor(element, Color.white);        
            m_CurrentIndex = m_ScrollView.IndexOf(element.parent.parent);
        }
 
    }

    private void OnPrefabLeaveSlotBounds(MouseLeaveEvent evt){
            
        
        var element = evt.target as VisualElement;
        if(!m_IsInstantiating){
            ChangeBorderColor(element);
        }
        
        if(!m_IsInContextMenu)
            m_CurrentIndex = -1;
        
        m_IsMouseOnElement = false;

    }

    private void OnPrefabDragExit(DragExitedEvent evt){}

    private void OnPrefabSlotDragStart(PointerDownEvent evt){
        var element = evt.target as VisualElement;
        var parent = element.parent.parent;
        var index = m_ScrollView.IndexOf(element.parent.parent);

        if (!slotToListDictionary.ContainsKey(index))
        { 
            return;
        } 
        
        m_CurrentElemetn = element;

        if(!m_IsInstantiating && slotToListDictionary.ContainsKey(index) && evt.button != 1){ 
            m_CurrentIndex = index;
            ChangeBorderColor(element, Color.blue);
            m_IsInstantiating = true;
            m_IsClicked = true;
        }
    }

    private void PointerMove(PointerMoveEvent evt){
        if(m_IsClicked){
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.StartDrag("Instantiate");
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            DragAndDrop.objectReferences = new Object[]{m_Palette[slotToListDictionary[m_CurrentIndex]]};
            m_IsClicked = false;
        }
    }

    private void OnPrefabSlotDragFinished(PointerUpEvent evt){
        var element = evt.target as VisualElement;
        var parent = element.parent.parent;
        var index = m_ScrollView.IndexOf(element.parent.parent);
        m_IsClicked = false;

        DragAndDrop.visualMode = DragAndDropVisualMode.None;
        m_IsInstantiating = false;
        ChangeBorderColor(element);
    }

    private void OnAddSlotButtonPressed(){
        InstantiateNewPrefabSlot();
    }

    private void SetPrefabLabel(string text, Label label){
        label.text = text;
    }

    private void SetPrefabSelectorImage(Texture2D background, Image selector){
        selector.image = background;
        selector.style.width = new StyleLength(new Length(PREFAB_PREVIEW_IMAGE_SIZE, LengthUnit.Percent));

        float percent = ((selector.parent.contentRect.width * (selector.contentRect.width * 0.01f)) / selector.parent.contentRect.height) * 100;
        selector.style.height = new StyleLength(new Length(selector.contentRect.width, LengthUnit.Pixel));
        
        EditorCoroutineUtility.StartCoroutine(CallGeometryChangedManually(), this);
        

    }

    private IEnumerator CallGeometryChangedManually(){
        yield return new EditorWaitForSeconds(0.002f);
        m_ManualGeometryChange = true;
        var evt = GeometryChangedEvent.GetPooled(rootVisualElement.contentRect, rootVisualElement.contentRect);
        ChangeImageSizeOnWindowSizeChange(evt);
    }

    private void PingPrefabAsset(DropdownMenuAction action){
        int index = slotToListDictionary[m_CurrentIndex];
        EditorGUIUtility.PingObject(m_Palette[index]);
    }

    private void ChangeBorderColor(VisualElement element, Color color){
        element.style.borderTopColor = new StyleColor(color);
        element.style.borderBottomColor = new StyleColor(color);
        element.style.borderLeftColor = new StyleColor(color);
        element.style.borderRightColor = new StyleColor(color);
        element.style.borderBottomWidth = new StyleFloat(3f);
        element.style.borderRightWidth = new StyleFloat(3f);
        element.style.borderLeftWidth = new StyleFloat(3f);
        element.style.borderTopWidth = new StyleFloat(3f);
    }

    private void ChangeBorderColor(VisualElement element){
        element.style.borderTopColor = new StyleColor();
        element.style.borderBottomColor = new StyleColor();
        element.style.borderLeftColor = new StyleColor();
        element.style.borderRightColor = new StyleColor();
    }

    private Texture2D GetAssetPreview(GameObject obj){
        if(obj.GetComponent<Renderer>() == null && obj.GetComponentInChildren<Renderer>() == null){
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
