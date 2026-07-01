using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : Singleton<AnimationManager>
{
    public List<SpriteAnimation> animations = new List<SpriteAnimation>();
    public List<SpriteAnimation> toRemove = new List<SpriteAnimation>();

    public void Register(SpriteAnimation anim)
    {
        if (!animations.Contains(anim))
            animations.Add(anim);
    }

    public void Unregister(SpriteAnimation anim)
    {
        toRemove.Add(anim);
    }

    private void Update()
    {
        // Cleanup();

        float dt = Time.deltaTime;
        for (int i = 0; i < animations.Count; i++)
        {
            animations[i].ManualUpdate(dt);
        }
    }

    private void Cleanup()
    {
        if (toRemove.Count == 0) return;
        for (int i = 0; i < toRemove.Count; i++)
            animations.Remove(toRemove[i]);
        toRemove.Clear();
    }
}
