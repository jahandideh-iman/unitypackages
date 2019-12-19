

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AssetEditorUtilities {


    public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
    {
        return FindAssetsByType<T>("", true);
    }

    public static List<T> FindAssetsByType<T>(string path, bool searchInChildFolders = false) where T : UnityEngine.Object
    {
        List<T> assets = new List<T>();
        string[] guids;

        if (path.Equals(""))
            guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
        else
            guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)), new string[] { path });
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                if(searchInChildFolders == true || IsInRoot(assetPath, path))
                    assets.Add(asset);

            }
        }
        return assets;
    }

    public static List<object> FindAssetsByType(string path, Type type, bool searchInChildFolders = false) 
    {
        List<object> assets = new List<object>();
        string[] guids;

        if (path.Equals(""))
            guids = AssetDatabase.FindAssets(string.Format("t:{0}", type));
        else
            guids = AssetDatabase.FindAssets(string.Format("t:{0}", type), new string[] { path });
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            object asset = AssetDatabase.LoadAssetAtPath(assetPath, type);
            if (asset != null)
            {
                if (searchInChildFolders == true || IsInRoot(assetPath, path))
                    assets.Add(asset);

            }
        }
        return assets;
    }


    private static bool IsInRoot(string assetPath, string rootPath)
    {
        var assetFolder = Path.GetDirectoryName(assetPath);

        return Path.GetFullPath(assetFolder).Equals(Path.GetFullPath(rootPath));
    }

    public static string AssetFolder(UnityEngine.Object asset)
    {
        return Path.GetDirectoryName(AssetDatabase.GetAssetPath(asset));
    }

    public static string RelativeAssetPath(string absolutePath)
    {
        string relativePath = absolutePath;
        if (absolutePath.StartsWith(Application.dataPath, StringComparison.Ordinal))
            relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
        

        return relativePath;
    }
}
