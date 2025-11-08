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
    public TMP_Text comboText;
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
    int comboCount = 1;

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

        // Build and shuffle IDs
        List<int> ids = new();
        for (int i = 0; i < pairs; i++) { ids.Add(i); ids.Add(i); }
        for (int i = 0; i < ids.Count; i++)
        {
            int rand = Random.Range(i, ids.Count);
            (ids[i], ids[rand]) = (ids[rand], ids[i]);
        }

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

        int comboBonus = 100 * comboCount;
        score += comboBonus;

        UpdateScore();
        PlaySFX(matchSFX);
        Debug.Log($"Combo x{comboCount}! +{comboBonus} points");
        comboCount++; // increase combo chain

        yield return new WaitForSeconds(0.25f);
        flipped.Remove(a);
        flipped.Remove(b);

        SaveProgress();
        CheckGameOver();
    }

    IEnumerator Mismatch(Card a, Card b)
    {
        yield return new WaitForSeconds(0.6f);
        comboCount = 1; // reset combo chain
        a.ResetCard();
        b.ResetCard();
        flipped.Remove(a);
        flipped.Remove(b);
        score = Mathf.Max(0, score - 5);
        UpdateScore();
        PlaySFX(mismatchSFX);
        SaveProgress();
    }

    void UpdateScore()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
        if (comboText != null)
            comboText.text = comboCount > 1 ? $"Combo x{comboCount}" : "";
    }

    void PlaySFX(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
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
        SaveData data = new()
        {
            score = score,
            comboCount = comboCount,
            cardOrder = new(),
            matchedIndexes = new()
        };

        foreach (var c in allCards)
            data.cardOrder.Add(c.id);

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
        comboCount = data.comboCount;
        UpdateScore();

        for (int i = 0; i < allCards.Count; i++)
            allCards[i].SetCard(cardSprites[data.cardOrder[i]], data.cardOrder[i]);

        foreach (int idx in data.matchedIndexes)
        {
            var card = allCards[idx];
            card.isMatched = true;
            card.Flip(true);
        }
    }
    #endregion

    public void ClearProgressAndRestart()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    [System.Serializable]
    class SaveData
    {
        public int score;
        public int comboCount;
        public List<int> cardOrder;
        public List<int> matchedIndexes;
    }
}
