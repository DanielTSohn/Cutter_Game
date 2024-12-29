using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(TimeScaleParameters))]
[CanEditMultipleObjects]
public class TimeScaleParametersInspector : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var name = property.FindPropertyRelative("ID");
        var multiplier = property.FindPropertyRelative("Multiplier");
        var time = property.FindPropertyRelative("Time");
        var inProportion = property.FindPropertyRelative("InProportion");
        var outProportion = property.FindPropertyRelative("OutProportion");

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.indentLevel++;

        Rect rectFoldout = new (position.min.x, position.min.y, position.size.x, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(rectFoldout, property.isExpanded, label);
        int lines = 1;
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            Rect namePosition = new(position.min.x, position.min.y + lines++ * EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
            name.stringValue = EditorGUI.TextField(namePosition, name.stringValue);

            Rect multiplierPosition = new(position.min.x, position.min.y + lines++ * EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
            multiplier.floatValue = EditorGUI.Slider(multiplierPosition, multiplier.name, multiplier.floatValue, 0, 2);
            
            Rect timePosition = new (position.min.x, position.min.y + lines++ * EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
            var timeSetValue = EditorGUI.FloatField(timePosition, time.name, time.floatValue);
            if (timeSetValue > 0) time.floatValue = timeSetValue;

            Rect inSetPosition = new (position.min.x, position.min.y + lines++ * EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
            Rect outSetPosition = new (position.min.x, position.min.y + lines++ * EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
            var inSetValue = EditorGUI.Slider(inSetPosition, inProportion.name, inProportion.floatValue, 0, 1);
            var outSetValue = EditorGUI.Slider(outSetPosition, outProportion.name, outProportion.floatValue, 0, 1);

            if (inSetValue + outSetValue > 1)
            {
                if (inSetValue > outSetValue)
                {
                    inSetValue = 1 - outSetValue;
                }
                else
                {
                    outSetValue = 1 - inSetValue;
                }
            }
            inProportion.floatValue = inSetValue;
            outProportion.floatValue = outSetValue;
        
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lines = 1;
        if (property.isExpanded) lines = 5;

        return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * (lines - 1);
    }
}
