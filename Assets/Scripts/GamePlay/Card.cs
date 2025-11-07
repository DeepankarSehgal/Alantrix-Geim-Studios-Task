using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Card : MonoBehaviour
{
    [Header("References")]
    public Image frontImage, backImage;

    [HideInInspector] public bool isFlipped, isMatched;
    [HideInInspector] public int id;

    public event System.Action<Card> OnCardFlipped;

    Button button;
    Coroutine flipRoutine;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            if (!isMatched && !isFlipped) Flip(true);
        });
        ResetInstant();
    }

    public void SetCard(Sprite frontSprite, int cardId)
    {
        frontImage.sprite = frontSprite;
        id = cardId;
    }

    public void Flip(bool toFront)
    {
        if (flipRoutine != null) StopCoroutine(flipRoutine);
        flipRoutine = StartCoroutine(FlipAnim(toFront));
    }

    IEnumerator FlipAnim(bool toFront)
    {
        const float dur = 0.35f;
        float t = 0, half = dur * 0.5f;
        var start = transform.localRotation;
        var mid = Quaternion.Euler(0, 90, 0);
        var end = Quaternion.identity;
        System.Func<float, float> ease = x => x * x * (3f - 2f * x);

        // rotate to mid
        while (t < half)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Lerp(start, mid, ease(t / half));
            yield return null;
        }

        // swap sides
        frontImage.enabled = toFront;
        backImage.enabled = !toFront;

        // rotate back
        t = 0;
        while (t < half)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Lerp(mid, end, ease(t / half));
            yield return null;
        }

        transform.localRotation = end;
        isFlipped = toFront;
        flipRoutine = null;

        if (toFront) OnCardFlipped?.Invoke(this);
    }

    public void ResetCard()
    {
        if (isFlipped && !isMatched) Flip(false);
    }

    public void ResetInstant()
    {
        if (flipRoutine != null) StopCoroutine(flipRoutine);
        isFlipped = isMatched = false;
        frontImage.enabled = false;
        backImage.enabled = true;
        transform.localRotation = Quaternion.identity;
    }
}
