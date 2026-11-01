using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class ItemSystemSetup
{
    static ItemSystemSetup(){EditorApplication.update+=Poll;}
    static void Poll(){if(!File.Exists("RecoveryReports/item-system.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;File.Delete("RecoveryReports/item-system.request");EditorSceneManager.SaveOpenScenes();RunBatch();}
    [MenuItem("Tools/FPS/Check item prompt font")]
    public static void CheckFont()
    {
        if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;EditorApplication.delayCall+=CheckFont;return;}
        const string path="Assets/Recovery/Items/InteractionFont.asset";
        var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
        if(!font)
        {
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            font=TMPro.TMP_FontAsset.CreateFontAsset(source);
            font.name="InteractionFont";
            AssetDatabase.CreateAsset(font,path);
            AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
        }
        string text="[EF] 0123456789 Cửa đã khóa chọn chìa khóa mở khóa Nhặt Chìa khóa phòng 1 Pin Cầu chì Đóng cửa Mở cửa Shift đóng mạnh Nhặt đèn pin Nhặt tờ giấy Tương tác khối thử";
        bool glyphs=font.HasCharacters(text,out uint[] missing,false,true);
        var prompt=Object.FindFirstObjectByType<InteractionPromptUI>();
        if(prompt){prompt.label.font=font;EditorUtility.SetDirty(prompt.label);EditorSceneManager.MarkSceneDirty(prompt.gameObject.scene);EditorSceneManager.SaveScene(prompt.gameObject.scene);}
        EditorUtility.SetDirty(font);foreach(var atlas in font.atlasTextures)EditorUtility.SetDirty(atlas);AssetDatabase.SaveAssets();
        File.WriteAllText("RecoveryReports/item-font-check.txt",(font.atlasTexture.isReadable&&glyphs?"PASS":"FAIL")+": readable="+font.atlasTexture.isReadable+"; missing="+string.Join(",",missing??Array.Empty<uint>()));
    }
    const string Root="Assets/Recovery/Items";
    public static void RunBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
            if(!File.Exists("RecoveryReports/School_Test-before-items.unity")) File.Copy("Assets/Recovery/Scenes/School_Test.unity","RecoveryReports/School_Test-before-items.unity");
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var key=Create("room_1_key","Chìa khóa phòng 1",ItemType.Key,false,1,.08f,new Color(.9f,.7f,.2f));
            var battery=Create("battery","Pin",ItemType.Battery,true,4,.12f,new Color(.25f,.75f,.4f));
            var fuse=Create("fuse","Cầu chì",ItemType.PuzzleItem,true,3,.06f,new Color(.3f,.7f,.95f));
            var player=Object.FindFirstObjectByType<SchoolExplorer>();
            var inventory=player.GetComponent<Inventory>();if(!inventory)inventory=player.gameObject.AddComponent<Inventory>();
            inventory.view=player.view;inventory.interactor=player.GetComponent<PlayerInteractor>();inventory.room=Object.FindFirstObjectByType<StartRoom>();
            inventory.flashlight=player.view.GetComponent<Flashlight>();if(!inventory.flashlight)inventory.flashlight=player.view.gameObject.AddComponent<Flashlight>();
            var input=player.GetComponent<InventoryInput>();if(!input)input=player.gameObject.AddComponent<InventoryInput>();input.inventory=inventory;
            var ui=player.GetComponent<InventoryUI>();if(!ui)ui=player.gameObject.AddComponent<InventoryUI>();ui.inventory=inventory;
            // Place three small props beside the existing note on the instruction table.
            var note=Object.FindObjectsByType<StartRoomItem>(FindObjectsSortMode.None).First(i=>!i.flashlight);
            var origin=note.transform.position;
            var samples=new[]{key,battery,fuse};
            for(int i=0;i<samples.Length;i++)
            {
                string name="Sample pickup - "+samples[i].id;
                var existing=GameObject.Find(name);
                var prop=existing?existing.GetComponent<PickupItem>():(PickupItem)PrefabUtility.InstantiatePrefab(samples[i].pickupPrefab);
                prop.name=name;prop.transform.position=origin+new Vector3((i-1)*.28f,prop.GetComponent<BoxCollider>().size.y*.5f+.002f,-.30f);
                var rayOrigin=prop.transform.position+Vector3.up*.4f;
                if(Physics.Raycast(rayOrigin,Vector3.down,out var support,1f,Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Interactable"),QueryTriggerInteraction.Ignore))
                    prop.transform.position=new Vector3(prop.transform.position.x,support.point.y+prop.GetComponent<BoxCollider>().size.y*.5f+.002f,prop.transform.position.z);
                prop.GetComponent<Rigidbody>().isKinematic=true;
            }
            var exit=Object.FindObjectsByType<Door>(FindObjectsSortMode.None).First(d=>d.name=="Exit door hinge");
            exit.requiredKeyId=key.id;exit.isLocked=false;EditorUtility.SetDirty(exit);
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();
            File.WriteAllText("RecoveryReports/item-system-setup.txt","PASS: three ItemData assets, icons, Rigidbody pickup prefabs, samples beside note, unified five slots; north exit uses room_1_key.\n"+string.Join("\n",Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None).Select(p=>p.name+" "+p.transform.position)));
            InventoryLogicCheck.Run();
            EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity"); if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("RecoveryReports/item-system-setup-error.txt",e.ToString());if(Application.isBatchMode)EditorApplication.Exit(1);}
    }
    static ItemData Create(string id,string title,ItemType type,bool stackable,int maxStack,float weight,Color color)
    {
        string path=Root+"/"+id;
        var data=AssetDatabase.LoadAssetAtPath<ItemData>(path+".asset");
        if(!data){data=ScriptableObject.CreateInstance<ItemData>();AssetDatabase.CreateAsset(data,path+".asset");}
        data.id=id;data.itemName=title;data.type=type;data.stackable=stackable;data.maxStack=maxStack;data.weight=weight;
        data.description=type==ItemType.Key?"Mở khóa cửa phòng 1. Chọn chìa khóa, nhìn vào cửa và nhấn E.":type==ItemType.Battery?"Hồi 25 năng lượng cho đèn pin.":"Linh kiện dành cho câu đố điện.";
        var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)
        {
            bool ink;
            if(type==ItemType.Key){float d=Vector2.Distance(new Vector2(x,y),new Vector2(23,43));ink=(d<13&&d>6)||(x>=20&&x<=26&&y>=9&&y<=34)||(x>=24&&x<=36&&y>=9&&y<=15)||(x>=24&&x<=32&&y>=22&&y<=27);}
            else if(type==ItemType.Battery)ink=(x>=19&&x<=45&&y>=9&&y<=51)||(x>=26&&x<=38&&y>51&&y<57);
            else ink=(x>=23&&x<=41&&y>=8&&y<=55);
            var c=color;if(type==ItemType.Battery && ((x>=28&&x<=36&&y>=36&&y<=40)||(x>=31&&x<=33&&y>=32&&y<=44)))c=Color.white;
            if(type==ItemType.PuzzleItem&&(y<17||y>46))c=new Color(.8f,.83f,.86f);
            tex.SetPixel(x,y,ink?c:Color.clear);
        }
        tex.Apply();File.WriteAllBytes(path+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path+".png");
        var importer=(TextureImporter)AssetImporter.GetAtPath(path+".png");importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.SaveAndReimport();data.icon=AssetDatabase.LoadAssetAtPath<Sprite>(path+".png");
        var material=AssetDatabase.LoadAssetAtPath<Material>(path+".mat");if(!material){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path+".mat");}material.color=color;
        var root=new GameObject(title);root.layer=LayerMask.NameToLayer("Interactable");
        var box=root.AddComponent<BoxCollider>();box.size=type==ItemType.Key?new Vector3(.10f,.025f,.20f):new Vector3(.08f,.15f,.08f);
        var visual=GameObject.CreatePrimitive(type==ItemType.Key?PrimitiveType.Cube:PrimitiveType.Cylinder);visual.transform.SetParent(root.transform,false);visual.transform.localScale=type==ItemType.Key?box.size:new Vector3(.07f,.075f,.07f);Object.DestroyImmediate(visual.GetComponent<Collider>());visual.GetComponent<Renderer>().sharedMaterial=material;
        var body=root.AddComponent<Rigidbody>();body.mass=weight;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        var pickup=root.AddComponent<PickupItem>();pickup.item=data;pickup.dropRadius=.15f;
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,path+".prefab");data.pickupPrefab=prefab.GetComponent<PickupItem>();Object.DestroyImmediate(root);EditorUtility.SetDirty(data);EditorUtility.SetDirty(material);return data;
    }
}





