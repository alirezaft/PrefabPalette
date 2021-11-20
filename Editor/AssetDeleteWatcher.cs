using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssetDeleteWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (deletedAssets.Length == 0)
            return;
        
        List<int> gameObjectIndexes = new List<int>();
        for (int i = 0; i < deletedAssets.Length; i++)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(deletedAssets[i]) == typeof(GameObject))
            {
                gameObjectIndexes.Add(i);
            }
        }

        if (gameObjectIndexes.Count == 0)
        {
            return;
        }
        
        var gos = new GameObject[gameObjectIndexes.Count];

        for(int i = 0; i < gameObjectIndexes.Count; i++)
        {
            gos[i] = AssetDatabase.LoadAssetAtPath<GameObject>(deletedAssets[gameObjectIndexes[i]]);
        }
        
        PaletteWindow.GetInstance()?.ForceRemovePrefab(gos);
    }
}
