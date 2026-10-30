using System;
using UnityEngine;

[Serializable] public class CampusData { public CampusPart[] parts; public CampusLabel[] labels; }
[Serializable] public class CampusPart {
    public string name; public string shape; public float[] p; public float[] s; public float[] color;
    public bool solid = true; public int floor;
}
[Serializable] public class CampusLabel { public string text; public float[] p; }
