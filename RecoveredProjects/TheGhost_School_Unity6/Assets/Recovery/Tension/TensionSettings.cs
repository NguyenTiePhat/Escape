using UnityEngine;

public sealed class TensionSettings : MonoBehaviour
{
    public static bool Reduced { get; private set; }
    public static float Visuals { get; private set; } = 1;
    public static float Shake { get; private set; } = 1;
    public static float Heartbeat { get; private set; } = 1;
    public static bool Muffle { get; private set; } = true;
    public static bool Scares { get; private set; } = true;
    public static float VisualStrength => Reduced ? 0 : Visuals;
    public static float ShakeStrength => Reduced ? 0 : Shake;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Load()
    {
        Reduced=PlayerPrefs.GetInt("Tension.Reduced",0)!=0; Visuals=Mathf.Clamp01(PlayerPrefs.GetFloat("Tension.Visuals",1));
        Shake=Mathf.Clamp01(PlayerPrefs.GetFloat("Tension.Shake",1));Heartbeat=Mathf.Clamp01(PlayerPrefs.GetFloat("Tension.Heartbeat",1));
        Muffle=PlayerPrefs.GetInt("Tension.Muffle",1)!=0;Scares=PlayerPrefs.GetInt("Tension.Scares",1)!=0;
    }
    public static void SetReduced(bool value) { Reduced=value; PlayerPrefs.SetInt("Tension.Reduced",value?1:0); }
    public static void DrawSettings()
    {
        GUILayout.Label("Căng thẳng / Trợ năng");
        bool reduced=GUILayout.Toggle(Reduced,"Giảm hiệu ứng (tắt hình ảnh, rung, hù dọa và nghẹt âm)");
        if(reduced!=Reduced)SetReduced(reduced);
        Visuals=Slider("Hiệu ứng hình ảnh",Visuals,"Tension.Visuals");
        Shake=Slider("Rung camera",Shake,"Tension.Shake");
        Heartbeat=Slider("Âm lượng nhịp tim",Heartbeat,"Tension.Heartbeat");
        bool muffle=GUILayout.Toggle(Muffle,"Làm mờ âm thanh môi trường");if(muffle!=Muffle){Muffle=muffle;PlayerPrefs.SetInt("Tension.Muffle",muffle?1:0);}
        bool scares=GUILayout.Toggle(Scares,"Cho phép hù dọa bất ngờ");if(scares!=Scares){Scares=scares;PlayerPrefs.SetInt("Tension.Scares",scares?1:0);}
    }
    static float Slider(string label,float value,string key){GUILayout.Label(label);float result=GUILayout.HorizontalSlider(value,0,1);if(result!=value)PlayerPrefs.SetFloat(key,result);return result;}
    void OnApplicationQuit(){PlayerPrefs.Save();}
}
