using System;
using UnityEditor;
using UnityEngine;
using Rustbound.World;

// Targeted, repeatable migration. Does not rebuild the authored level.
public static class Level03BackgroundRevision
{
    const string Path="Assets/Prefabs/Levels/Level03_Map.prefab";
    public static void Run()
    {
        var root=PrefabUtility.LoadPrefabContents(Path);
        try
        {
            var layers=root.transform.Find("SceneryLayers");
            if(layers==null)throw new InvalidOperationException("Level03 SceneryLayers is missing.");
            foreach(var parallax in layers.GetComponentsInChildren<SceneryParallax>(true))
                parallax.verticalScrollRatio=parallax.name=="FarCity"?0f:.8f;

            var camera=root.GetComponentInChildren<WorldCamera>(true);
            var backdrop=camera!=null?camera.transform.Find("DistantFactory"):null;
            if(backdrop==null)throw new InvalidOperationException("Level03 camera backdrop is missing.");
            if(backdrop.GetComponent<CameraBackdropCover>()==null)
                backdrop.gameObject.AddComponent<CameraBackdropCover>();

            // A quiet industrial silhouette at each upper floor retains depth
            // while leaving the route and interactable objects clearly visible.
            var previous=layers.Find("UpperTowerDecoration");
            if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var upper=new GameObject("UpperTowerDecoration");
            upper.transform.SetParent(layers,false);
            Add(upper.transform,"ColumnTall",2f,5.5f,.95f,.35f);
            Add(upper.transform,"ColumnTall",21f,11.5f,.95f,.35f);
            Add(upper.transform,"NearBrokenTank",3.5f,17f,.95f,.48f);
            Add(upper.transform,"NearScrapHeap",20.5f,19f,.95f,.48f);
            PrefabUtility.SaveAsPrefabAsset(root,Path);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL03_BACKGROUND_REVISION_OK");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Add(Transform parent,string spriteName,float x,float y,float ratio,float tint)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Pixel/World/"+spriteName+".png");
        if(sprite==null)throw new InvalidOperationException("Missing decoration "+spriteName);
        var item=new GameObject(spriteName);
        item.transform.SetParent(parent,false);item.transform.localPosition=new Vector3(x,y,0);
        var renderer=item.AddComponent<SpriteRenderer>();renderer.sprite=sprite;
        renderer.sortingOrder=-15;renderer.color=new Color(tint,tint,tint,1);
        var parallax=item.AddComponent<SceneryParallax>();
        parallax.scrollRatio=ratio;parallax.verticalScrollRatio=ratio;
    }
}
