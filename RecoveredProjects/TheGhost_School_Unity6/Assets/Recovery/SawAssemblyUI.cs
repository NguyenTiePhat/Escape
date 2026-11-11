using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SawAssemblyUI : MonoBehaviour
{
    public SawAssemblyStation station;
    public Image[] ingredientImages = new Image[3];
    public Image[] borders = new Image[3];
    public Image resultImage;
    void OnEnable() { if (station) station.OnAssemblyChanged += Refresh; Refresh(); }
    void OnDisable() { if (station) station.OnAssemblyChanged -= Refresh; }
    public void Refresh()
    {
        if (!station) return;
        for (int i = 0; i < 3; i++)
        {
            bool ready = station.IsInstalled(i);
            if (i < ingredientImages.Length && ingredientImages[i])
            {
                ingredientImages[i].sprite = station.Ingredient(i) ? station.Ingredient(i).icon : null;
                ingredientImages[i].color = ready ? Color.white : new Color(.26f, .26f, .26f, 1);
            }
            if (i < borders.Length && borders[i]) borders[i].color = ready ? new Color(.6f, .88f, .46f, 1) : new Color(.2f, .22f, .21f, 1);
        }
        if (resultImage) { resultImage.sprite = station.finishedSaw ? station.finishedSaw.icon : null; resultImage.color = station.IsComplete ? Color.white : new Color(.26f, .26f, .26f, 1); }
    }
}
