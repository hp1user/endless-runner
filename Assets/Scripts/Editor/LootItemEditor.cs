using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LootItem))]
public class LootItemEditor : Editor
{
    SerializedProperty typeProp;
    SerializedProperty amountProp;
    SerializedProperty ammoCategoryProp;
    SerializedProperty specificWeaponIDProp;
    SerializedProperty worldMoveSpeedProp;

    private void OnEnable()
    {
        typeProp = serializedObject.FindProperty("type");
        amountProp = serializedObject.FindProperty("amount");
        ammoCategoryProp = serializedObject.FindProperty("ammoCategory");
        specificWeaponIDProp = serializedObject.FindProperty("specificWeaponID");
        worldMoveSpeedProp = serializedObject.FindProperty("worldMoveSpeed");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Loot Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(typeProp);

        LootItem.LootType currentType = (LootItem.LootType)typeProp.enumValueIndex;

        switch (currentType)
        {
            case LootItem.LootType.Health:
            case LootItem.LootType.Armor:
                EditorGUILayout.PropertyField(amountProp, new GUIContent("Amount to Heal/Restore"));
                break;
            case LootItem.LootType.Ammo:
                EditorGUILayout.PropertyField(amountProp, new GUIContent("Ammo Count"));
                EditorGUILayout.PropertyField(ammoCategoryProp, new GUIContent("Ammo Category"));
                break;
            case LootItem.LootType.Weapon:
                EditorGUILayout.PropertyField(specificWeaponIDProp, new GUIContent("Specific Weapon ID"));
                break;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(worldMoveSpeedProp);

        serializedObject.ApplyModifiedProperties();
    }
}