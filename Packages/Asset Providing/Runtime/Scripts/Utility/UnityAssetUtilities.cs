
using UnityEngine;

namespace Arman.AssetProviding.Utility
{
    public static class UnityAssetUtilities
    {

        // TODO: Refactor this.
        public static bool IsOfAssetType<T>(Object obj, out T castedObj) where T : Object
        {
            if (obj is GameObject gObj)
            {
                var comp = gObj.GetComponent<T>();
                if (comp != null)
                {
                    castedObj = comp;
                    return true;
                }
            }
            else if (obj is T tObj)
            {
                castedObj = tObj;
                return true;
            }

            castedObj = null;
            return false;
        }

        public static T CastToAsset<T>(Object obj) where T : Object
        {
            if (obj is GameObject gObj)
                return gObj.GetComponent<T>();
            else
                return obj as T;
        }

    }
}