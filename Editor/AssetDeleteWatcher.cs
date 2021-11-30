using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AssetDeleteWatcher : AssetPostprocessor
{
    public static bool IsAssetDeleted;
    private static string[] m_LostAssetNames;
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        Debug.Log("Processing modified assets");
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

        IsAssetDeleted = true;
        
        var gos = new string[gameObjectIndexes.Count];

        for(int i = 0; i < gameObjectIndexes.Count; i++)
        {
            var nameWithPostfix = deletedAssets[i].Split('/').Last();
            var name = nameWithPostfix.Split('.')[0];
            gos[i] = name;
        }

        m_LostAssetNames = gos;
        PaletteWindow.GetInstance()?.ForceRemovePrefab(gos);
    }

    public static string[] GetDeletedAssetNames()
    {
        return m_LostAssetNames;
    }

    public static void ResetPostprocessor()
    {
        IsAssetDeleted = false;
        m_LostAssetNames = null;
    }
}
