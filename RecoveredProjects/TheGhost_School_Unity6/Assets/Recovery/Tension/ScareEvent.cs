using UnityEngine;

public enum ScareKind { Sound, DoorSlam, Shadow }
public sealed class ScareEvent : MonoBehaviour
{
    public string eventId;
    public ScareKind kind;
    public BoxCollider trigger;
    public AudioSource sound;
    public Door door;
    public Transform shadow, shadowStart, shadowEnd;
    [Range(0,1)] public float maximumTension=.35f;
    public bool HasPlayed {get;private set;}
    bool occupied;
    public bool Entered(Vector3 point)
    {
        if(!trigger||!trigger.enabled)return false;
        Vector3 p=trigger.transform.InverseTransformPoint(point)-trigger.center, h=trigger.size*.5f;
        bool inside=Mathf.Abs(p.x)<=h.x&&Mathf.Abs(p.y)<=h.y&&Mathf.Abs(p.z)<=h.z;
        bool entered=inside&&!occupied;occupied=inside;return entered;
    }
    public bool Ready => !HasPlayed && isActiveAndEnabled &&
        (kind==ScareKind.Sound ? sound&&sound.clip : kind==ScareKind.DoorSlam ? door&&door.IsOpen&&!door.isLocked : shadow&&shadowStart&&shadowEnd);
    public bool Begin()
    {
        if(!Ready)return false;
        if(kind==ScareKind.DoorSlam&&!door.TrySetOpen(false,door.strongSpeed))return false;
        HasPlayed=true;
        if(sound&&sound.clip)sound.Play();
        if(kind==ScareKind.Shadow){shadow.position=shadowStart.position;shadow.gameObject.SetActive(true);}
        return true;
    }
    public void Finish(){if(sound)sound.Stop();if(shadow)shadow.gameObject.SetActive(false);}
    public void ResetRound(){Finish();HasPlayed=false;occupied=false;}
}
