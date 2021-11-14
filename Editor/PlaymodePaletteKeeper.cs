using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[FilePath("PrefabPalette/tempPalette.pal",FilePathAttribute.Location.PreferencesFolder)]
public class PlaymodePaletteKeeper : ScriptableSingleton<PlaymodePaletteKeeper>
{
    internal List<GameObject> m_TempPalette = new List<GameObject>();
}
