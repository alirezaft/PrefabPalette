using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
 

public class PaletteWindow : EditorWindow
{
    #region paths
    private const string MAIN_VISUAL_ASSET_TREE_PATH = "Assets/Editor/PaletteWindow.uxml";
    private const string PREFAB_SLOT_VISUAL_TREE_PATH = "Assets/Editor/PrefabSlot.uxml";
    private const string PREFAB_PALETTE_MENU_PATH = "Window/Prefab Palette";
    private const string NO_PREFAB_SELECTED_IMAGE_PATH = "Assets/Editor/Textures/NoPrefabImg.jpg";
    #endregion

    #region uxml_element_names
    private const string BUTTON_ADD_PREFAB = "add-prefab-button";
    private const string SCROLL_VIEW_PREFAB_CONTAINER = "prefab-container";
    private const string IMAGE_PREFAB_FIELD = "prefab-field";
    private const string LABEL_PREFAB_NAME = "prefab-name";
    #endregion

    #region uxml_element_classes
    private const string OBJECT_FIELD_DISPLAY_LABEL_CLASS = "unity-object-field-display__lebel";
    #endregion
    
    #region texts
    private const string NO_PREFAB_TEXT = "No  prefab selected";
    #endregion

    #region numerical_constants
    private const float PREFAB_PREVIEW_IMAGE_SIZE = 112;

    #endregion

    private bool m_IsMouseOnElement;
    private int m_NumberOfItems;
    private int m_CurrentIndex;
    
    private List<GameObject> m_Palette;

    private ScrollView m_ScrollView;
    private VisualElement m_CurrentElemetn;

    private void OnEnable(){
        var uxmlFile = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MAIN_VISUAL_ASSET_TREE_PATH);
        var root = this.rootVisualElement;
        uxmlFile.CloneTree(root);
        m_NumberOfItems = 1;
        
        root.Query<Button>(BUTTON_ADD_PREFAB).First().clicked += OnAddSlotButtonPressed;
        m_ScrollView = root.Query<ScrollView>(SCROLL_VIEW_PREFAB_CONTAINER).First();
        m_ScrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChange);
        InstantiateNewPrefabSlot();
        m_Palette = new List<GameObject>();  
        
    }    

    [MenuItem(PREFAB_PALETTE_MENU_PATH)]
    public static void OpenPrefabPalette(){
        var window = EditorWindow.GetWindow<PaletteWindow>(false, "Prefab Palette", true);
        window.Show();
    }

    private void OnScrollViewGeometryChange(GeometryChangedEvent evt){
         
        if(!m_ScrollView.verticalScroller.ClassListContains(VisualElement.disabledUssClassName))
            m_ScrollView.verticalScroller.value = m_ScrollView.verticalScroller.highValue;
        
    }

    private void InstantiateNewPrefabSlot(){
        var newSlot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PREFAB_SLOT_VISUAL_TREE_PATH);
        m_ScrollView.Add(newSlot.Instantiate()); 
        var createdField = m_ScrollView.Query<Image>(IMAGE_PREFAB_FIELD).Last();

        RegisterDragAndDropCallbacks(createdField);
        SetPrefabSelectorImage(AssetDatabase.LoadAssetAtPath<Texture2D>(NO_PREFAB_SELECTED_IMAGE_PATH), createdField);
        SetPrefabLabel(NO_PREFAB_TEXT, m_ScrollView.Query<Label>(LABEL_PREFAB_NAME).Last());

        m_NumberOfItems++; 
        m_ScrollView.ScrollTo(m_ScrollView.Query<Image>(IMAGE_PREFAB_FIELD).Last());      
    }

    private void RegisterDragAndDropCallbacks(Image element){
        element.RegisterCallback<DragPerformEvent>(OnPrefabDroppedIntoSlot);
        element.RegisterCallback<MouseEnterEvent>(OnMouseEnteredSlotBounds);
        element.RegisterCallback<MouseLeaveEvent>(OnPrefabLeaveSlotBounds);
        element.RegisterCallback<DragUpdatedEvent>(OnDraggingPrefabUpdated);
        // element.RegisterCallback<DragExitedEvent>(OnPrefabDragExit);
    }

    private void OnPrefabDroppedIntoSlot(DragPerformEvent evt){
        var element = evt.target as VisualElement;
        var index = m_ScrollView.IndexOf(element.parent.parent);
        // EditorUtility.Select
        var prefabs = DragAndDrop.objectReferences;
        if(prefabs.Length > 0){
            if(prefabs[0] is GameObject && AssetDatabase.Contains(prefabs[0])){
                if(m_Palette.Count < index + 1){
                    for(int i = m_Palette.Count; i < index; i++){
                        m_Palette.Add(null);
                    }
                }

                SetPrefabSelectorImage(AssetPreview.GetAssetPreview(prefabs[0]), element as Image);
                SetPrefabLabel(prefabs[0].name, element.parent.Q<Label>());
            }
        }
        ChangeBorderColor(element, Color.white);
    }

    private void OnDraggingPrefabUpdated(DragUpdatedEvent evt){
        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        var element = evt.target as VisualElement;
        var parent = element.parent.parent;
        var index = m_ScrollView.IndexOf(element.parent.parent);
        
        if(DragAndDrop.objectReferences[0] is GameObject){
            ChangeBorderColor(element, Color.green);
        }else{
            ChangeBorderColor(element, Color.red);
        }
    }


    private void OnMouseEnteredSlotBounds(MouseEnterEvent evt){
        var element = evt.target as VisualElement;
        var parent = element.parent.parent;
        
        ChangeBorderColor(element, Color.white);        
        m_CurrentIndex = m_ScrollView.IndexOf(element.parent.parent);
 
    }

    private void OnPrefabLeaveSlotBounds(MouseLeaveEvent evt){
        var element = evt.target as VisualElement;
        ChangeBorderColor(element);
        m_CurrentIndex = -1;
        m_IsMouseOnElement = false;

    }

    private void OnPrefabDragExit(DragExitedEvent evt){
        //Do nothing
    }

    private void OnAddSlotButtonPressed(){
        InstantiateNewPrefabSlot();
    }

    private void SetPrefabLabel(string text, Label label){
        label.text = text;

    }

    private void SetPrefabSelectorImage(Texture2D background, Image selector){
        selector.image = background;
        selector.style.width = new StyleLength(PREFAB_PREVIEW_IMAGE_SIZE);
        selector.style.height = new StyleLength(PREFAB_PREVIEW_IMAGE_SIZE);
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
}
