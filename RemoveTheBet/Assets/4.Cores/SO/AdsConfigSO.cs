using UnityEngine;

[CreateAssetMenu(fileName = "AdsConfig", menuName = "Configs/Ads Config")]
public class AdsConfigSO : ScriptableObject
{
    [Header("IronSource")]
    public string ironSourceAppKey;
    public string ironSourceBannerAdUnitId;
    public string ironSourceInterstitialAdUnitId;
    public string ironSourceRewardedAdUnitId;

    [Header("Adsmode")]
    public string adsmodeAppKey;
    public string adsmodeBannerAdUnitId;
    public string adsmodeInterstitialAdUnitId;
    public string adsmodeRewardedAdUnitId;
}
