using System.Collections.Generic;

// Keep existing scene GUIDs and historical editor tools compatible; Door owns all runtime behaviour.
public sealed class SchoolDoor : Door
{
    public static readonly HashSet<SchoolDoor> Active = new HashSet<SchoolDoor>();
    void OnEnable() { if (!decorative) Active.Add(this); }
    void OnDisable() { Active.Remove(this); }
}
