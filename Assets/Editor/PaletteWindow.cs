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

    private int m_NumberOfItems;
    
    private List<GameObject> m_Palette;

    private ScrollView m_ScrollView;

    private void OnEnable(){
        var uxmlFile = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MAIN_VISUAL_ASSET_TREE_PATH);
        var root = this.rootVisualElement;
        uxmlFile.CloneTree(root);
        m_NumberOfItems = 1;

        root.Query<Button>(BUTTON_ADD_PREFAB).First().clicked += OnAddSlotButtonPressed;
        m_ScrollView = root.Query<ScrollView>(SCROLL_VIEW_PREFAB_CONTAINER).First();
        InstantiateNewPrefabSlot();
        m_Palette = new List<GameObject>();  
    }       

    [MenuItem(PREFAB_PALETTE_MENU_PATH)]
    public static void OpenPrefabPalette(){
        var window = EditorWindow.GetWindow<PaletteWindow>();
        window.Show();
    }

    private void InstantiateNewPrefabSlot(){
        var newSlot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PREFAB_SLOT_VISUAL_TREE_PATH);
        m_ScrollView.Add(newSlot.Instantiate()); 
        var createdField = m_ScrollView.Query<Image>(IMAGE_PREFAB_FIELD).Last();
        // createdField.objectType = typeof(Texture2D);
        SetPrefabSelectorImage(AssetDatabase.LoadAssetAtPath<Texture2D>(NO_PREFAB_SELECTED_IMAGE_PATH), createdField);
        SetPrefabLabel(NO_PREFAB_TEXT, m_ScrollView.Query<Label>(LABEL_PREFAB_NAME).Last());
        m_NumberOfItems++;
    }

    private void OnPrefabDrag(VisualElement vis){
        // vis.RegisterCallback<>
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
}
