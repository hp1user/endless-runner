using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level Database", menuName = "Game Data/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelThemeData> allThemes;
}