using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AutoFillAssetArrayAttribute))]
public class AutoFillAssetArrayAttributeDrawer : PropertyDrawer
{
    string path;


    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {

        EditorGUI.BeginProperty(position, label, property);
        var targetObject = property.serializedObject.targetObject;

        path = property.stringValue;
        if (string.IsNullOrEmpty(path))
            path = AssetEditorUtilities.AssetFolder(targetObject);

        var pathlabelRect = new Rect(position.x, position.y, position.width - 200, 30);
        var pathButtonRect = new Rect(position.width - 140, position.y, 70, 20);
        var findButtonRect = new Rect(position.width - 70, position.y, 70, 20);

        GUI.Label(pathlabelRect, string.Format("{0}:{1}", AutoFillAttribute().propertyName, path));

        if (GUI.Button(pathButtonRect, "Select Folder"))
            path = AssetEditorUtilities.RelativeAssetPath(EditorUtility.OpenFolderPanel("Select", path, ""));
        if (GUI.Button(findButtonRect, "Find"))
            Find(targetObject);


        property.stringValue = path;
        //if (GUILayout.Button("All", GUILayout.MaxWidth(40))) TransformCopyAll();

        EditorGUI.EndProperty();


    }

    private AutoFillAssetArrayAttribute AutoFillAttribute()
    {
        return attribute as AutoFillAssetArrayAttribute;
    }


    private Type GetFieldType(object targetObject)
    {
        return targetObject.GetType().GetField(AutoFillAttribute().propertyName).GetValue(targetObject).GetType().GetElementType();
    }

    private void SetField(object targetObject, object[] value)
    {
        Array destinationArray = Array.CreateInstance(GetFieldType(targetObject), value.Length);
        Array.Copy(value, destinationArray, value.Length);

        targetObject.GetType().GetField(AutoFillAttribute().propertyName).SetValue(targetObject, destinationArray);

        EditorUtility.SetDirty(targetObject as UnityEngine.Object);
    }


    private void Find(object targetObject)
    {
        var assets = new List<object>(AssetEditorUtilities.FindAssetsByType(path, GetFieldType(targetObject)).ToArray());
        assets.Remove(targetObject);
        SetField(targetObject, assets.ToArray());
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 30;
    }
}
