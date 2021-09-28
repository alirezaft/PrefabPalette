using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
 

public class PaletteWindow : EditorWindow
{
    private int m_NumberOfItems;

    private ScrollView m_ScrollView;

    private void OnEnable(){
        var uxmlFile = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/PaletteWindow.uxml");
        var root = this.rootVisualElement;
        uxmlFile.CloneTree(root);
        m_NumberOfItems = 1;

        root.Query<Button>("add-prefab-button").First().clicked += OnAddSlotButtonPressed;
        m_ScrollView = root.Query<ScrollView>("prefab-container").First();
        InstantiateNewPrefabSlot();        
    }       

    [MenuItem("Window/Prefab Palette")]
    public static void OpenPrefabPalette(){
        var window = EditorWindow.GetWindow<PaletteWindow>();
        window.Show();
    }

    private void InstantiateNewPrefabSlot(){
        var newSlot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/PrefabSlot.uxml");
        m_ScrollView.Add(newSlot.Instantiate());
        m_ScrollView.Query<ObjectField>().Last().objectType = typeof(GameObject);

    }

    private void OnPrefabDrag(VisualElement vis){
        // vis.RegisterCallback<>
    }

    private void OnAddSlotButtonPressed(){
        InstantiateNewPrefabSlot();
    }
}
