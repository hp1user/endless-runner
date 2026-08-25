using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CardEffect))]
public class CardEffectDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        var typeProp = property.FindPropertyRelative("upgradeType");
        var valProp = property.FindPropertyRelative("upgradeValue");

        float padding = 5f;
        float halfWidth = (position.width - padding) / 2f;

        Rect typeRect = new Rect(position.x, position.y, halfWidth, position.height);
        Rect valRect = new Rect(position.x + halfWidth + padding, position.y, halfWidth, position.height);

        EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);
        
        // Add a small label for the value field to make it clear
        EditorGUIUtility.labelWidth = 40f; 
        EditorGUI.PropertyField(valRect, valProp, new GUIContent("Value"));
        EditorGUIUtility.labelWidth = 0f; // Reset

        EditorGUI.EndProperty();
    }
}
