using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public GameObject cardPrefab;
    public Transform gridParent;
    public TMP_Text scoreText;
    public GameObject Win;

    [Header("Grid Settings")]
    public int rows = 2, cols = 3;

    [Header("Sprites & Audio")]
    public List<Sprite> cardSprites;
    public AudioClip flipSFX, matchSFX, mismatchSFX, winSFX;

    AudioSource audioSource;
    List<Card> allCards = new();
    List<Card> flipped = new();
    int score;

    const string SAVE_KEY = "CardMatch_Save";

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        GenerateGrid();
        LoadProgress();
        UpdateScore();
    }

    void GenerateGrid()
    {
        foreach (Transform child in gridParent) Destroy(child.gameObject);
        allCards.Clear();

        int total = rows * cols;
        int pairs = Mathf.Min(cardSprites.Count, total / 2);

        // Build and shuffle ID list
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
        PlaySFX(flipSFX);

        if (flipped.Count < 2) return;

        var a = flipped[^2];
        var b = flipped[^1];

        StartCoroutine(a.id == b.id ? Match(a, b) : Mismatch(a, b));
    }

    IEnumerator Match(Card a, Card b)
    {
        a.isMatched = b.isMatched = true;
        score += 100; UpdateScore();
        PlaySFX(matchSFX);
        yield return new WaitForSeconds(0.25f);
        flipped.Remove(a); flipped.Remove(b);
        SaveProgress();
        CheckGameOver();
    }

    IEnumerator Mismatch(Card a, Card b)
    {
        yield return new WaitForSeconds(0.6f);
        a.ResetCard(); b.ResetCard();
        flipped.Remove(a); flipped.Remove(b);
        score = Mathf.Max(0, score - 5);
        UpdateScore();
        PlaySFX(mismatchSFX);
        SaveProgress();
    }

    void UpdateScore() => scoreText.text = $"Score: {score}";

    void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    void CheckGameOver()
    {
        if (allCards.TrueForAll(c => c.isMatched))
        {
            Win.SetActive(true);
            PlaySFX(winSFX);
            PlayerPrefs.DeleteKey(SAVE_KEY); // clear saved state
        }
    }

    #region Save / Load System
    void SaveProgress()
    {
        SaveData data = new();

        data.score = score;

        data.cardOrder = new List<int>();
        foreach (var c in allCards)
            data.cardOrder.Add(c.id);

        data.matchedIndexes = new List<int>();
        for (int i = 0; i < allCards.Count; i++)
            if (allCards[i].isMatched)
                data.matchedIndexes.Add(i);

        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }


    void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

        SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SAVE_KEY));

        score = data.score;
        UpdateScore();

        // rebuild cards to match saved layout
        for (int i = 0; i < allCards.Count; i++)
            allCards[i].SetCard(cardSprites[data.cardOrder[i]], data.cardOrder[i]);

        foreach (int idx in data.matchedIndexes)
        {
            var card = allCards[idx];
            card.isMatched = true;
            card.Flip(true);
        }
    }


    public void ClearProgressAndRestart()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    [System.Serializable]
    class SaveData
    {
        public int score;
        public List<int> cardOrder;       // card IDs in grid order
        public List<int> matchedIndexes;  // positions of matched cards
    }
    #endregion
}
