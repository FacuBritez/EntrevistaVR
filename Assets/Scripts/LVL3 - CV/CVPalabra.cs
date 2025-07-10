using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class CVPalabra : MonoBehaviour
{
    [SerializeField] TMP_Text TextMesh;

    [Space]
    [SerializeField] Transform meshTransform;
    [SerializeField] RectTransform canvasTransform;

    [Space]
    [SerializeField] Animator correctionAnimator;

    [Space]
    [SerializeField] AnimationCurve appearAnimCurve, expandAnimCurve;

    [Space]
    [SerializeField] AudioClip popSound;
    [SerializeField] AudioClip unpopSound;
    [SerializeField] AudioClip clickSound;
    [SerializeField] AudioClip writingSound;
    [SerializeField] AudioClip correctingSound;
    [SerializeField] AudioSource CorreccionesSounds;
    [SerializeField] AudioSource CVPalabraSounds;

    // ---

    public CVType.CVFields fieldType { get; private set; }

    public bool IsPlaced;

    // ---

    Vector3 defaultScale = Vector3.one;

    // ---

    private void Awake()
    {
        defaultScale = meshTransform.localScale;
        CVPalabraSounds.clip = popSound;
        CVPalabraSounds.Play();
    }

    void Start()
    {
        LVL3Manager.Instance.OnGameFinished += (w) => PlayReverseAppearAnimation(0.5f); 
    }

    // ---

    public void SetText(string text, CVType.CVFields sourceField)
    {
        TextMesh.text = text;
        fieldType = sourceField;
    }

    public string GetText()
    {
        return TextMesh.text;
    }

    // ---

    public void ShowCorrection(string correction)
    {
        correctionAnimator.GetComponentInChildren<TMP_Text>().text = correction;

        correctionAnimator.SetBool("Incorrect", true);

        CorreccionesSounds.clip = writingSound;
        CorreccionesSounds.Play();
    }

    public void HideCorrection()
    {
        if (!correctionAnimator.GetBool("Incorrect")) return;

        correctionAnimator.GetComponentInChildren<TMP_Text>().text = "";

        correctionAnimator.SetBool("Incorrect", false);

        CorreccionesSounds.clip = correctingSound;
        CorreccionesSounds.Play();
    }

    public void PlayAppearAnimation(float duration)
    {
        PlayScaleAnimation(Vector3.zero, meshTransform.localScale, duration, appearAnimCurve);
    }

    public void PlayReverseAppearAnimation(float duration)
    {
        GetComponent<XRGrabInteractable>().enabled = false;
        if (IsPlaced) return;
        PlayScaleAnimation(meshTransform.localScale, Vector3.zero, duration, appearAnimCurve);
    }

    public void PlayExpandAnimation(Vector2 desiredSize, float duration)
    {
        PlayScaleAnimation(meshTransform.localScale, new(desiredSize.x, desiredSize.y, meshTransform.localScale.z), duration, expandAnimCurve);
        CVPalabraSounds.clip = clickSound;
        CVPalabraSounds.Play();
    }

    public void PlayReverseExpandAnimation(float duration)
    {
        PlayScaleAnimation(meshTransform.localScale, defaultScale, duration, expandAnimCurve);
        CVPalabraSounds.clip = unpopSound;
        CVPalabraSounds.Play();
    }

    public void PlayScaleAnimation(Vector3 startScale, Vector3 endScale, float duration, AnimationCurve animationCurve = null)
    {
        StartCoroutine(ScaleAnimation(startScale, endScale, duration, animationCurve));
    }

    IEnumerator ScaleAnimation(Vector3 startScale, Vector3 endScale, float duration, AnimationCurve animationCurve = null)
    {
        float time = 0f;

        while (time < duration)
        {
            float progress = animationCurve == null ? time / duration : animationCurve.Evaluate(time / duration);

            SetScale(Vector3.Lerp(startScale, endScale, progress));
            time += Time.deltaTime;
            yield return null;
        }

        SetScale(endScale);
    }

    void SetScale(Vector3 scale)
    {
        meshTransform.localScale = scale;
        canvasTransform.sizeDelta = new Vector2(scale.x * 1000f, scale.y * 1000f);
    }
}
