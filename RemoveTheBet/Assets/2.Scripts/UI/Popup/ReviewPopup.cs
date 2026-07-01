using UnityEngine;
using UnityEngine.UI;

public partial class PopupId
{
    public static string ReviewPopup = "ReviewPopup";
}

public class ReviewPopup : PopupBase
{
    [SerializeField] private Button _buttonCancel;
    [SerializeField] private Button _buttonReview;

    private const string APP_REVIEW_URL = "https://apps.apple.com/app/id6773774387?action=write-review";

    private void OnEnable()
    {
        _buttonCancel.onClick.AddListener(OnCancel);
        _buttonReview.onClick.AddListener(OnReview);
    }

    private void OnCancel()
    {
        Hide();
    }

    private void OnReview()
    {
        Application.OpenURL(APP_REVIEW_URL);
        Hide();
    }

    private void OnDisable()
    {
        _buttonCancel.onClick.RemoveListener(OnCancel);
        _buttonReview.onClick.RemoveListener(OnReview);
    }
}
