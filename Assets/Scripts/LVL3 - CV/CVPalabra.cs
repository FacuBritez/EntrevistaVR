using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CVPalabra : MonoBehaviour
{
    [SerializeField] TMP_Text TextMesh;

    [Space]
    [SerializeField] Transform meshTransform;
    [SerializeField] RectTransform canvasTransform;

    [Space]
    [SerializeField] AnimationCurve appearAnimCurve, expandAnimCurve;

    // ---

    public CVType.CVFields fieldType { get; private set; }

    // ---

    Vector3 defaultScale = Vector3.one;

    // ---

    private void Awake()
    {
        defaultScale = meshTransform.localScale;
    }

    // ---

    public void SetText(string text, CVType.CVFields sourceField)
    {
        TextMesh.text = text;
        fieldType = sourceField;
    }

    // ---

    public void PlayAppearAnimation(float duration)
    {
        PlayScaleAnimation(Vector3.zero, meshTransform.localScale, duration, appearAnimCurve);
    }

    public void PlayReverseAppearAnimation(float duration)
    {
        PlayScaleAnimation(meshTransform.localScale, Vector3.zero, duration, appearAnimCurve);
    }

    public void PlayExpandAnimation(Vector2 desiredSize, float duration)
    {
        PlayScaleAnimation(meshTransform.localScale, new(desiredSize.x, desiredSize.y, meshTransform.localScale.z), duration, expandAnimCurve);
    }

    public void PlayReverseExpandAnimation(float duration)
    {
        PlayScaleAnimation(meshTransform.localScale, defaultScale, duration, expandAnimCurve);
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
