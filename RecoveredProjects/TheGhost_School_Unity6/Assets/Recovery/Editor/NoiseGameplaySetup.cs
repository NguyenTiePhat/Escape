using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class NoiseGameplaySetup
{
    const string Root = "Assets/Recovery/Noise";
    static NoiseGameplaySetup() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists("RecoveryReports/noise-gameplay-setup.request") || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
        File.Delete("RecoveryReports/noise-gameplay-setup.request"); Run();
    }
    public static void RunBatch()
    {
        try { EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity"); Run(); EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    [MenuItem("Tools/M3/Set up footsteps and object noise")]
    public static void Run()
    {
        var player = Object.FindAnyObjectByType<SchoolExplorer>();
        if (!player || EditorApplication.isPlaying) throw new InvalidOperationException("Open School_Test in Edit Mode.");
        if (!File.Exists("RecoveryReports/School_Test-before-noise-gameplay.unity")) File.Copy("Assets/Recovery/Scenes/School_Test.unity", "RecoveryReports/School_Test-before-noise-gameplay.unity");
        Directory.CreateDirectory(Root); AssetDatabase.Refresh();
        var tagsAsset = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagsAsset.FindProperty("tags");
        foreach(string name in new[]{"Tile","Wood","Metal","Carpet","Gạch","Gỗ","Kim loại","Thảm"})
        {
            bool exists = false; for(int i=0;i<tags.arraySize;i++) if(tags.GetArrayElementAtIndex(i).stringValue==name) exists=true;
            if(!exists){int i=tags.arraySize;tags.InsertArrayElementAtIndex(i);tags.GetArrayElementAtIndex(i).stringValue=name;}
        }
        tagsAsset.ApplyModifiedPropertiesWithoutUndo();
        foreach(string name in new[]{"Tile","Wood","Metal","Carpet"})
            if(!AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Root+"/"+name+".physicMaterial")) AssetDatabase.CreateAsset(new PhysicsMaterial(name),Root+"/"+name+".physicMaterial");
        var motor = player.GetComponent<PlayerMotor>(); if(!motor)motor=player.gameObject.AddComponent<PlayerMotor>();
        var footsteps = player.GetComponent<FootstepNoise>(); if(!footsteps)footsteps=player.gameObject.AddComponent<FootstepNoise>();
        // A dedicated source avoids overriding other player audio settings.
        var sourceObject=player.transform.Find("Footstep Audio");
        if(!sourceObject){sourceObject=new GameObject("Footstep Audio").transform;sourceObject.SetParent(player.transform,false);}
        var source=sourceObject.GetComponent<AudioSource>();if(!source)source=sourceObject.gameObject.AddComponent<AudioSource>();
        source.playOnAwake=false;source.loop=false;source.spatialBlend=1;source.minDistance=1;source.maxDistance=25;source.rolloffMode=AudioRolloffMode.Linear;
        footsteps.audioSource=source;
        footsteps.tileClips=new[]{Clip("pl_tile1.ogg"),Clip("pl_tile2.ogg")};
        footsteps.woodClips=new[]{Clip("WoodStep.ogg"),Clip("WoodStep2.ogg")};
        footsteps.metalClips=new[]{Clip("MetalFootstep.ogg"),Clip("MetalFootstep2.ogg")};
        footsteps.carpetClips=new[]{MakeCarpet(footsteps.tileClips[0],"CarpetStep1"),MakeCarpet(footsteps.tileClips[1],"CarpetStep2")};
        if(!player.GetComponent<InventoryNoise>())player.gameObject.AddComponent<InventoryNoise>();
        int doors=0;
        foreach(var door in Object.FindObjectsByType<Door>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(door.decorative)continue;
            if(!door.GetComponent<DoorNoise>())door.gameObject.AddComponent<DoorNoise>();doors++;
        }
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Recovery/Items"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var prefab=PrefabUtility.LoadPrefabContents(path);
            if(prefab.GetComponent<PickupItem>() && prefab.GetComponent<Rigidbody>() && !prefab.GetComponent<NoiseOnImpact>())
            {prefab.AddComponent<NoiseOnImpact>();PrefabUtility.SaveAsPrefabAsset(prefab,path);}
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        foreach(var pickup in Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None))
            if(pickup.GetComponent<Rigidbody>()&&!pickup.GetComponent<NoiseOnImpact>())pickup.gameObject.AddComponent<NoiseOnImpact>();
        EditorUtility.SetDirty(footsteps);EditorUtility.SetDirty(source);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();
        string report="PASS: FootstepNoise + dedicated spatial AudioSource; four clip sets and material/tag mappings; InventoryNoise; "+doors+" DoorNoise bindings; pickup impact components.\n";
        foreach(var clips in new[]{footsteps.tileClips,footsteps.woodClips,footsteps.metalClips,footsteps.carpetClips}) foreach(var clip in clips)report+=clip.name+" length="+clip.length+"s\n";
        File.WriteAllText("RecoveryReports/noise-gameplay-setup.txt",report);
    }
    static AudioClip Clip(string name)
    {
        var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AudioClip/"+name);
        if(!clip)throw new InvalidOperationException("Missing audio: "+name);
        return clip;
    }
    static AudioClip MakeCarpet(AudioClip input,string name)
    {
        string path=Root+"/"+name+".wav";
        var existing=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(existing)return existing;
        var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(input));
        var settings=importer.defaultSampleSettings;
        if(settings.loadType!=AudioClipLoadType.DecompressOnLoad){settings.loadType=AudioClipLoadType.DecompressOnLoad;importer.defaultSampleSettings=settings;importer.SaveAndReimport();}
        input.LoadAudioData();var samples=new float[input.samples*input.channels];
        if(!input.GetData(samples,0))throw new InvalidOperationException("Cannot read source footstep audio");
        var state=new float[input.channels];float alpha=1-Mathf.Exp(-2*Mathf.PI*600/input.frequency);
        using(var writer=new BinaryWriter(File.Create(path)))
        {
            int bytes=samples.Length*2;writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+bytes);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)input.channels);writer.Write(input.frequency);writer.Write(input.frequency*input.channels*2);writer.Write((short)(input.channels*2));writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(bytes);
            for(int i=0;i<samples.Length;i++){int c=i%input.channels;state[c]+=alpha*(samples[i]-state[c]);writer.Write((short)(Mathf.Clamp(state[c]*.65f,-1,1)*32767));}
        }
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
