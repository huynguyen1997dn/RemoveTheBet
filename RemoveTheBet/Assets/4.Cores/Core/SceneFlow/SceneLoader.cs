using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{

    public void LoadSceneAsync(string sceneName, Action<float> onProgress, Action onComplete)
    {
        StartCoroutine(LoadRoutine(sceneName, onProgress, onComplete));
    }

    private System.Collections.IEnumerator LoadRoutine(
        string sceneName,
        Action<float> onProgress,
        Action onComplete)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            onProgress?.Invoke(op.progress);
            yield return null;
        }

        onProgress?.Invoke(1f);
        yield return new WaitForSeconds(0.3f);

        op.allowSceneActivation = true;
        onComplete?.Invoke();
    }
}