using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimation : MonoBehaviour
{
    [SerializeField] private SpriteAnimClip clip;

    [SerializeField] private SpriteRenderer spriteRenderer;

    private int currentFrame;
    private float timer;
    private bool isPlaying;

    private void Awake()
    {
    }

    private void OnEnable()
    {
        if (AnimationManager.Instance != null)
            AnimationManager.Instance.Register(this);
    }

    private void OnDisable()
    {

        if (AnimationManager.Instance != null)
            AnimationManager.Instance.Unregister(this);
    }

    public void ManualUpdate(float deltaTime)
    {
        if (!isPlaying || clip == null)
            return;

        if (clip.frames == null || clip.frames.Length == 0)
            return;

        timer += deltaTime;

        float frameDuration = 1f / clip.frameRate;

        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            NextFrame();
        }
    }

    private void NextFrame()
    {
        currentFrame++;

        if (currentFrame >= clip.frames.Length)
        {
            if (clip.loop)
            {
                currentFrame = 0;
            }
            else
            {
                currentFrame = clip.frames.Length - 1;
                isPlaying = false;
            }
        }

        spriteRenderer.sprite = clip.frames[currentFrame];
    }

    public void Play()
    {
        if (clip == null)
        {
            Debug.LogWarning($"[SpriteAnimation] No clip assigned on {name}.");
            return;
        }

        currentFrame = 0;
        timer = 0f;
        isPlaying = true;

        if (clip.frames.Length > 0)
        {
            spriteRenderer.sprite = clip.frames[0];
        }
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public bool IsPlaying => isPlaying;

    public void SetClip(SpriteAnimClip newClip)
    {
        if (clip == newClip) return;
        clip = newClip;
        Stop();
    }
}
