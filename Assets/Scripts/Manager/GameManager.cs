using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public GameObject cardPrefab;
    public Transform gridParent;

    [Header("Grid Settings")]
    public int rows = 2, cols = 3;

    [Header("Sprites")]
    public List<Sprite> cardSprites;

    [Header("UI")]
    public TMP_Text scoreText;

    List<Card> allCards = new();
    List<Card> flipped = new();
    int score;

    void Start()
    {
        GenerateGrid();
        UpdateScore();
    }

    void GenerateGrid()
    {
        foreach (Transform child in gridParent) Destroy(child.gameObject);
        allCards.Clear();

        int total = rows * cols;
        int pairs = Mathf.Min(cardSprites.Count, total / 2);

        // Build and shuffle id list
        List<int> ids = new();
        for (int i = 0; i < pairs; i++) { ids.Add(i); ids.Add(i); }
        for (int i = 0; i < ids.Count; i++) (ids[i], ids[Random.Range(i, ids.Count)]) = (ids[Random.Range(i, ids.Count)], ids[i]);

        // Spawn cards
        for (int i = 0; i < total; i++)
        {
            var card = Instantiate(cardPrefab, gridParent).GetComponent<Card>();
            card.SetCard(cardSprites[ids[i]], ids[i]);
            card.OnCardFlipped += OnCardFlipped;
            allCards.Add(card);
        }

        // Responsive layout
        if (gridParent.TryGetComponent(out GridLayoutGroup grid))
        {
            var rt = gridParent.GetComponent<RectTransform>();
            float w = (rt.rect.width - grid.spacing.x * (cols - 1)) / cols;
            float h = (rt.rect.height - grid.spacing.y * (rows - 1)) / rows;
            grid.cellSize = new Vector2(w, h);
        }
    }

    void OnCardFlipped(Card card)
    {
        if (card.isMatched) return;
        flipped.Add(card);
        if (flipped.Count < 2) return;

        var a = flipped[^2];
        var b = flipped[^1];
        StartCoroutine(a.id == b.id ? Match(a, b) : Mismatch(a, b));
    }

    IEnumerator Match(Card a, Card b)
    {
        a.isMatched = b.isMatched = true;
        score += 100; UpdateScore();
        yield return new WaitForSeconds(0.2f);
        flipped.Remove(a); flipped.Remove(b);
    }

    IEnumerator Mismatch(Card a, Card b)
    {
        yield return new WaitForSeconds(0.6f);
        a.ResetCard(); b.ResetCard();
        flipped.Remove(a); flipped.Remove(b);
        score = Mathf.Max(0, score - 5); UpdateScore();
    }

    void UpdateScore() => scoreText.text = $"Score: {score}";
}
