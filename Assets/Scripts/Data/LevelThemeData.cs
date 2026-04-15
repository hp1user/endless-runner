using UnityEngine;

[CreateAssetMenu(fileName = "New Level Theme", menuName = "Game Data/Level Theme")]
public class LevelThemeData : ScriptableObject
{
    public string themeName = "City Ruins";

    [Tooltip("The different visual variants for this level (Child Chunks)")]
    public GameObject[] chunkVariants;

    [Tooltip("The special chunk that spawns at the end of the level to transition to the next")]
    public GameObject transitionBridge;
}