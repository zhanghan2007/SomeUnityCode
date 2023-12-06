using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FixScale))]
public class FixScaleEditor : Editor
{
    SerializedProperty baseBg;
    SerializedProperty justSetBaseBG;
    SerializedProperty defaultWidth;
    SerializedProperty defalutHeight;
    
    private FixScale m_target => target as FixScale;
    
    protected virtual void OnEnable() {
        baseBg = serializedObject.FindProperty("baseBg");
        justSetBaseBG = serializedObject.FindProperty("justSetBaseBG");
        defaultWidth = serializedObject.FindProperty("defaultWidth");
        defalutHeight = serializedObject.FindProperty("defalutHeight");
    }
    
    public override void OnInspectorGUI() {
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(baseBg);
        EditorGUILayout.PropertyField(justSetBaseBG);
        GUI.enabled = false;
        EditorGUILayout.PropertyField(defaultWidth);
        EditorGUILayout.PropertyField(defalutHeight);
        GUI.enabled = true;
        if (EditorGUI.EndChangeCheck()) {
            serializedObject.ApplyModifiedProperties();    
        }
    }
}
