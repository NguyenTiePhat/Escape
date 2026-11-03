using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class NoiseSceneCheck
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
            var player=Object.FindAnyObjectByType<SchoolExplorer>();var steps=player.GetComponent<FootstepNoise>();
            Require(steps&&steps.enabled&&steps.audioSource&&steps.audioSource.spatialBlend==1&&!steps.audioSource.playOnAwake,"spatial footstep source");
            Require(Object.FindAnyObjectByType<AudioListener>(),"scene audio listener");
            Require(player.GetComponent<InventoryNoise>()&&player.GetComponent<Inventory>(),"inventory noise hookup");
            int doors=0;foreach(var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))if(!door.decorative){Require(door.GetComponent<DoorNoise>(),"door missing noise: "+door.name);doors++;}
            int clips=0;
            foreach(var set in new[]{steps.tileClips,steps.woodClips,steps.metalClips,steps.carpetClips})
            {
                Require(set!=null&&set.Length>0,"missing surface audio set");
                foreach(var clip in set)
                {
                    Require(clip&&clip.samples>0,"empty audio clip");clip.LoadAudioData();var data=new float[clip.samples*clip.channels];Require(clip.GetData(data,0),"cannot decode "+clip.name);
                    float peak=0;foreach(float value in data)peak=Mathf.Max(peak,Mathf.Abs(value));Require(peak>.00001f,"silent clip "+clip.name);clips++;
                }
            }
            var probe=new GameObject("Surface mapping probe");var collider=probe.AddComponent<BoxCollider>();
            string[] tags={"Tile","Wood","Metal","Carpet","Gạch","Gỗ","Kim loại","Thảm"};
            for(int i=0;i<tags.Length;i++){probe.tag=tags[i];Require(FootstepNoise.ResolveSurface(collider)==(FootstepSurface)(i%4),"tag mapping "+tags[i]);}
            probe.tag="Untagged";
            foreach(string name in new[]{"Tile","Wood","Metal","Carpet"})
            {
                collider.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Recovery/Noise/"+name+".physicMaterial");Require(collider.sharedMaterial&&FootstepNoise.ResolveSurface(collider).ToString()==name,"physics material mapping "+name);
            }
            probe.tag="Metal";Require(FootstepNoise.ResolveSurface(collider)==FootstepSurface.Metal,"tag takes precedence over material");Object.DestroyImmediate(probe);
            foreach(string id in new[]{"room_1_key","battery","fuse"})Require(AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Recovery/Items/"+id+".asset").pickupPrefab.GetComponent<NoiseOnImpact>(),"pickup prefab impact "+id);
            File.WriteAllText("RecoveryReports/noise-scene-check.txt",$"PASS: School_Test footstep spatial source + AudioListener; {clips} decoded non-silent surface clips; {doors} door event components; inventory event component; 3 pickup prefab impacts; 8 English/Vietnamese tags; 4 physics materials; tag precedence.\n");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("RecoveryReports/noise-scene-check.txt","FAIL: "+e);EditorApplication.Exit(1);}
    }
}
