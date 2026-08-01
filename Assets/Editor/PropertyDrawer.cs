using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
public class SerializableDictionaryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var keys = property.FindPropertyRelative("keys");
        var values = property.FindPropertyRelative("values");

        EditorGUI.BeginProperty(position, label, property);

        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + 2;

            for (int i = 0; i < keys.arraySize; i++)
            {
                float rowHeight = Mathf.Max(
                    EditorGUI.GetPropertyHeight(keys.GetArrayElementAtIndex(i)),
                    EditorGUI.GetPropertyHeight(values.GetArrayElementAtIndex(i)));

                float keyWidth = position.width * 0.35f;
                float valueWidth = position.width * 0.55f;
                float removeWidth = position.width * 0.08f;

                var keyRect = new Rect(position.x, y, keyWidth, rowHeight);
                var valueRect = new Rect(position.x + keyWidth + 4, y, valueWidth, rowHeight);
                var removeRect = new Rect(position.x + keyWidth + valueWidth + 8, y, removeWidth, EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(keyRect, keys.GetArrayElementAtIndex(i), GUIContent.none);
                EditorGUI.PropertyField(valueRect, values.GetArrayElementAtIndex(i), GUIContent.none);

                if (GUI.Button(removeRect, "-"))
                {
                    keys.DeleteArrayElementAtIndex(i);
                    values.DeleteArrayElementAtIndex(i);
                    property.serializedObject.ApplyModifiedProperties();
                    break;
                }

                y += rowHeight + 2;
            }

            var addRect = new Rect(position.x, y, 60, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(addRect, "+"))
            {
                keys.arraySize++;
                values.arraySize++;
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        var keys = property.FindPropertyRelative("keys");
        var values = property.FindPropertyRelative("values");

        float height = EditorGUIUtility.singleLineHeight + 2;

        for (int i = 0; i < keys.arraySize; i++)
        {
            float rowHeight = Mathf.Max(
                EditorGUI.GetPropertyHeight(keys.GetArrayElementAtIndex(i)),
                EditorGUI.GetPropertyHeight(values.GetArrayElementAtIndex(i)));
            height += rowHeight + 2;
        }

        height += EditorGUIUtility.singleLineHeight + 4; // add button row
        return height;
    }
}