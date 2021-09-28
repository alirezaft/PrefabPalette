using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class iMGUIPaletteWindow : EditorWindow
{
    private static Texture2D m_NoPrefabSelectedImage;
    private Vector2 m_ScrollPosition;
    private GameObject[] m_Prefabs;
    private static float m_FieldWidth = 170f;
    private float m_PrefabPreviewWidth = 170f;

    #region constants

    private const float m_minimumWindowHeight = 10f;
    private const int m_initialPrefabCapacity = 10;
    private const float m_space = 10f;

    #endregion

    // [MenuItem("Window/Prefab Palette")]
    // public static void OpenPrefabPalette(){
    //     var window = EditorWindow.GetWindow<PaletteWindow>();
    //     window.minSize = new Vector2(m_FieldWidth + m_space, m_minimumWindowHeight);
    //     window.maxSize = new Vector2((m_FieldWidth * 2) + m_space, window.maxSize.y);        
    //     window.Show();
    // }

    private void Awake(){
        m_Prefabs = new GameObject[m_initialPrefabCapacity];
        m_NoPrefabSelectedImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Textures/NoPrefabMaterial.mat");
    }

    private void OnGUI(){
        m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
        for(int i = 0; i < m_Prefabs.Length; i++){
            
            GUILayout.Space(m_space);
            if(m_Prefabs[i] == null){
                GUILayout.Label(m_NoPrefabSelectedImage, GUILayout.MinWidth(m_FieldWidth), GUILayout.MinHeight(m_FieldWidth));
            }else{
                var prefabPreview = AssetPreview.GetAssetPreview(m_Prefabs[i]);
                // prefabPreview.Resize((int)m_PrefabPreviewWidth, (int)m_PrefabPreviewWidth); 
                // prefabPreview.Apply();
                GUILayout.Label(prefabPreview, GUILayout.MinWidth(m_FieldWidth), GUILayout.MinHeight(m_FieldWidth));
            }
            m_Prefabs[i] = EditorGUILayout.ObjectField(m_Prefabs[i], typeof(GameObject), false,
             GUILayout.MinWidth(m_FieldWidth)) as GameObject;
        }
        GUILayout.EndScrollView();

    } 

    
}
