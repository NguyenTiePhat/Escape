using UnityEngine;

[RequireComponent(typeof(Camera)), DefaultExecutionOrder(200)]
public sealed class TensionVisuals : MonoBehaviour
{
    public TensionManager tension;
    public Shader effectShader;
    Material material;
    Quaternion lastOffset=Quaternion.identity;
    Quaternion lastResult;
    bool applied;
    void Awake(){if(effectShader&&effectShader.isSupported)material=new Material(effectShader){hideFlags=HideFlags.HideAndDontSave};}
    void Update(){RemoveShake();}
    void RemoveShake()
    {
        // Do not undo another camera controller's new pose (look/death/crouch).
        if(applied&&Quaternion.Angle(transform.localRotation,lastResult)<.001f)transform.localRotation*=Quaternion.Inverse(lastOffset);
        applied=false;
    }
    void LateUpdate()
    {
        if(!tension||!tension.ActiveRound||!tension.IsChased||TensionSettings.ShakeStrength<=0)return;
        float a=.18f*tension.Tension*TensionSettings.ShakeStrength;
        lastOffset=Quaternion.Euler(Mathf.Sin(Time.time*19)*a,Mathf.Sin(Time.time*23)*a*.5f,0);
        transform.localRotation*=lastOffset;lastResult=transform.localRotation;applied=true;
    }
    void OnRenderImage(RenderTexture source,RenderTexture destination)
    {
        float amount=tension&&tension.ActiveRound?tension.Tension*TensionSettings.VisualStrength:0;
        if(!material||amount<.0001f){Graphics.Blit(source,destination);return;}
        material.SetFloat("_Tension",amount);material.SetFloat("_NoiseTime",Time.unscaledTime);
        Graphics.Blit(source,destination,material);
    }
    void OnDisable(){RemoveShake();}
    void OnDestroy(){if(material)Destroy(material);}
}
