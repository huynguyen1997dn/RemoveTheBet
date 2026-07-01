using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpriteAnimClip
{
    public Sprite[] frames;
    public float frameRate = 12f;
    public bool loop = true;
}

[System.Serializable]
public class SnakeSkin
{
    public Sprite head;
    public Texture body;
    public Sprite tail;
    public SpriteAnimClip headAnim;
    public SpriteAnimClip tailAnim;
}

[CreateAssetMenu(fileName = "SkinDatabase", menuName = "GamePlay/SkinDatabase", order = 1)]
public class SkinDatabaseSO : ScriptableObject
{
    [SerializeField] private List<SnakeSkin> skins = new List<SnakeSkin>();

    public SnakeSkin GetRandomSkin()
        => skins != null && skins.Count > 0 ? skins[Random.Range(0, skins.Count)] : null;

    public SnakeSkin GetSkin(int index)
        => skins != null && index >= 0 && index < skins.Count ? skins[index] : null;

    public int SkinCount => skins?.Count ?? 0;
}
