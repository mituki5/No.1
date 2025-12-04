using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Group References")]
    public GameObject titleGroup;
    public GameObject stageSelectGroup;
    public GameObject difficultySelectGroup;
    public GameObject gameGroup;
    public GameObject clearGroup;

    [Header("Game UI Elements")]
    public Text countdownText;
    public Text mainText;
    public Image mainImage; // イラスト表示用
    public Text questionNumberText;
    public Button[] answerButtons;
    public Text retryText;
    public Text clearTimeText;

    [Header("Illustrations")]
    public Sprite[] allIllustrations; // Resources/イラストなどに入れる

    private string currentStageType = "";
    private string currentDifficulty = "";
    private float difficultySpeed = 1.0f;

    private List<string> currentSequence = new List<string>();
    private List<Sprite> currentIllustrations = new List<Sprite>();
    private int currentQuestionIndex = 0;
    private int wrongCount = 0;
    private float gameTimer = 0f;
    private bool isPlaying = false;

    void Start()
    {
        ShowTitle();
    }

    //==================== 表示切替 ====================//
    public void ShowTitle()
    {
        HideAll();
        titleGroup.SetActive(true);
    }

    public void ShowStageSelect()
    {
        HideAll();
        stageSelectGroup.SetActive(true);
    }

    public void ShowDifficultySelect(string stageType)
    {
        HideAll();
        currentStageType = stageType;

        // 総理大臣・徳川家は難易度なし
        if (stageType == "総理大臣" || stageType == "徳川家")
        {
            ShowGame("Normal");
        }
        else
        {
            difficultySelectGroup.SetActive(true);
        }
    }

    public void ShowGame(string difficulty)
    {
        HideAll();
        currentDifficulty = difficulty;

        // 難易度ごとのスピード設定
        difficultySpeed = (difficulty == "Easy") ? 1.2f : (difficulty == "Normal" ? 0.7f : 0.4f);

        gameGroup.SetActive(true);
        StartCoroutine(GameRoutine());
    }

    public void ShowClear()
    {
        HideAll();
        clearGroup.SetActive(true);
        clearTimeText.text = $"クリアタイム：{gameTimer:F2}秒";
        StartCoroutine(ReturnToTitleAfterDelay(3f));
    }

    private void HideAll()
    {
        titleGroup.SetActive(false);
        stageSelectGroup.SetActive(false);
        difficultySelectGroup.SetActive(false);
        gameGroup.SetActive(false);
        clearGroup.SetActive(false);
        mainText.gameObject.SetActive(false);
        mainImage.gameObject.SetActive(false);
    }

    //==================== ゲームメイン処理 ====================//
    IEnumerator GameRoutine()
    {
        // カウントダウン（総理・徳川はなし）
        if (!(currentStageType == "総理大臣" || currentStageType == "徳川家"))
        {
            countdownText.gameObject.SetActive(true);
            for (int i = 3; i > 0; i--)
            {
                countdownText.text = i.ToString();
                yield return new WaitForSeconds(1f);
            }
            countdownText.gameObject.SetActive(false);
        }

        currentQuestionIndex = 0;
        wrongCount = 0;
        isPlaying = true;
        gameTimer = 0f;

        // シーケンス生成
        if (currentStageType == "イラスト")
        {
            currentIllustrations.Clear();
            for (int i = 0; i < 10; i++)
            {
                Sprite s = allIllustrations[Random.Range(0, allIllustrations.Length)];
                currentIllustrations.Add(s);
            }
            yield return StartCoroutine(ShowIllustrationSequence());
        }
        else
        {
            currentSequence = GenerateSequence(currentStageType);
            yield return StartCoroutine(ShowTextSequence());
        }

        SetupAnswerButtons();
    }

    IEnumerator ShowTextSequence()
    {
        if (currentStageType != "総理大臣" && currentStageType != "徳川家")
        {
            mainText.gameObject.SetActive(true);
            mainImage.gameObject.SetActive(false);

            foreach (string item in currentSequence)
            {
                mainText.text = item;
                yield return new WaitForSeconds(difficultySpeed);
            }
            mainText.text = "";
        }
    }

    IEnumerator ShowIllustrationSequence()
    {
        mainText.gameObject.SetActive(false);
        mainImage.gameObject.SetActive(true);

        foreach (Sprite img in currentIllustrations)
        {
            mainImage.sprite = img;
            mainImage.preserveAspect = true;
            yield return new WaitForSeconds(difficultySpeed);
        }
        mainImage.sprite = null;
    }

    void SetupAnswerButtons()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].gameObject.SetActive(true);
        }

        if (currentStageType == "イラスト")
        {
            questionNumberText.text = "1 / " + currentIllustrations.Count;
            SetIllustrationOptions();
        }
        else
        {
            questionNumberText.text = "1 / " + currentSequence.Count;
            SetAnswerOptions();
        }
    }

    void SetAnswerOptions()
    {
        string correct = currentSequence[currentQuestionIndex];
        List<string> options = new List<string> { correct };

        while (options.Count < 4)
        {
            string rand = GetRandomItem(currentStageType);
            if (!options.Contains(rand)) options.Add(rand);
        }

        Shuffle(options);

        for (int i = 0; i < 4; i++)
        {
            Text btnText = answerButtons[i].GetComponentInChildren<Text>();
            btnText.color = Color.black; // 黒に戻す
            btnText.text = options[i];

            Button btn = answerButtons[i];
            btn.onClick.RemoveAllListeners();
            string captured = options[i];
            btn.onClick.AddListener(() => OnAnswerClick(captured, btn));
        }
    }

    void SetIllustrationOptions()
    {
        Sprite correct = currentIllustrations[currentQuestionIndex];
        List<Sprite> options = new List<Sprite> { correct };

        while (options.Count < 4)
        {
            Sprite rand = allIllustrations[Random.Range(0, allIllustrations.Length)];
            if (!options.Contains(rand)) options.Add(rand);
        }

        Shuffle(options);

        for (int i = 0; i < 4; i++)
        {
            Image btnImage = answerButtons[i].GetComponent<Image>();
            btnImage.sprite = options[i];
            btnImage.preserveAspect = true;

            Button btn = answerButtons[i];
            btn.onClick.RemoveAllListeners();
            Sprite captured = options[i];
            btn.onClick.AddListener(() => OnImageButtonClick(captured, btn));
        }
    }

    //==================== 回答処理 ====================//
    void OnAnswerClick(string choice, Button clickedButton)
    {
        if (!isPlaying) return;

        string correct = currentSequence[currentQuestionIndex];

        if (choice == correct)
        {
            StartCoroutine(FlashButtonColor(clickedButton, Color.green));
            currentQuestionIndex++;
            if (currentQuestionIndex >= currentSequence.Count)
            {
                isPlaying = false;
                ShowClear();
                return;
            }
            questionNumberText.text = $"{currentQuestionIndex + 1} / {currentSequence.Count}";
            SetAnswerOptions();
        }
        else
        {
            StartCoroutine(FlashButtonColor(clickedButton, Color.red));
            wrongCount++;
            if (wrongCount >= 5)
            {
                difficultySpeed *= 1.2f; // 間違い5回で速度落とす
                retryText.gameObject.SetActive(true);
                StartCoroutine(ResetGameAfterDelay());
            }
            else
            {
                currentQuestionIndex = 0;
                SetAnswerOptions();
                questionNumberText.text = "1 / " + currentSequence.Count;
            }
        }
    }

    void OnImageButtonClick(Sprite choice, Button clickedButton)
    {
        if (!isPlaying) return;

        Sprite correct = currentIllustrations[currentQuestionIndex];

        if (choice == correct)
        {
            StartCoroutine(FlashButtonColor(clickedButton, Color.green));
            currentQuestionIndex++;
            if (currentQuestionIndex >= currentIllustrations.Count)
            {
                isPlaying = false;
                ShowClear();
                return;
            }
            questionNumberText.text = $"{currentQuestionIndex + 1} / {currentIllustrations.Count}";
            SetupAnswerButtons();
        }
        else
        {
            StartCoroutine(FlashButtonColor(clickedButton, Color.red));
            wrongCount++;
            if (wrongCount >= 5)
            {
                difficultySpeed *= 1.2f; // 間違い5回で速度落とす
                retryText.gameObject.SetActive(true);
                StartCoroutine(ResetGameAfterDelay());
            }
            else
            {
                currentQuestionIndex = 0;
                SetupAnswerButtons();
                questionNumberText.text = "1 / " + currentIllustrations.Count;
            }
        }
    }

    IEnumerator FlashButtonColor(Button btn, Color color)
    {
        Text btnText = btn.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            Color original = btnText.color;
            btnText.color = color;
            yield return new WaitForSeconds(0.2f);
            btnText.color = original;
        }
    }

    IEnumerator ResetGameAfterDelay()
    {
        isPlaying = false;
        yield return new WaitForSeconds(2f);
        retryText.gameObject.SetActive(false);
        ShowGame(currentDifficulty);
    }

    IEnumerator ReturnToTitleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowTitle();
    }

    void Update()
    {
        if (isPlaying) gameTimer += Time.deltaTime;
    }

    //==================== データ生成 ====================//
    List<string> GenerateSequence(string type)
    {
        List<string> list = new List<string>();
        switch (type)
        {
            case "数字":
                for (int i = 0; i < 10; i++) list.Add(Random.Range(0, 101).ToString());
                break;
            case "文字":
                string iroha = "いろはにほへとちりぬるをわかよたれそつねならむ";
                for (int i = 0; i < 10; i++) list.Add(iroha[Random.Range(0, iroha.Length)].ToString());
                break;
            case "矢印":
                string[] arrows = { "↑", "↓", "→", "←", "↗", "↘", "↙", "↖" };
                for (int i = 0; i < 10; i++) list.Add(arrows[Random.Range(0, arrows.Length)]);
                break;
            case "記号":
                string[] symbols = { "★", "☆", "◆", "◇", "◎", "△", "□", "※", "♪", "♣" };
                for (int i = 0; i < 10; i++) list.Add(symbols[Random.Range(0, symbols.Length)]);
                break;
            case "名前":
                //list = new List<string> { "謝憐", "三郎", "花城", "霊文", "南風", "扶揺", "君吾", "風信", "慕情", "師青玄", "風師", "師無渡", "水師", "郎千秋", "裴茗", "裴宿", "半月", "明儀", "戚容" };
                Shuffle(list);
                list = list.GetRange(0, Mathf.Min(10, list.Count));
                break;
            case "総理大臣":
                list = new List<string> {"伊藤博文", "黒田清隆", "三條實美", "山縣有朋", "松方正義", "伊藤博文(2)", "黒田清隆(2)", "松方正義(2)", "伊藤博文(3)", "大隈重信",
                                          "山縣有朋", "伊藤博文(4)", "西園寺公望", "桂太郎", "西園寺公望(2)", "桂太郎(2)", "西園寺公望(3)", "桂太郎(3)", "山本権兵衛", "大隈重信(2)",
                                          "寺内正毅","原敬","内田康哉","髙橋是清","加藤友三郎","内田康哉(2)","山本權兵衞(2)","清浦奎吾","加藤高明","若槻禮次郎",
                                          "田中義一","濱口雄幸","幣原喜重郎","若槻禮次郎(2)","犬養毅","髙橋是清(2)","齋藤實","岡田啓介","廣田弘毅","林銑十郎",
                                          "近衞文麿","平沼騏一郎","阿部信行","米内光政","近衞文麿(2)","近衞文麿(3)","東條英機","小磯國昭","鈴木貫太郎","東久邇宮稔彦王",
                                          "幣原喜重郎","吉田茂","片山哲","芦田均","吉田茂(2)","吉田茂(3)","吉田茂(4)","吉田茂(5)","鳩山一郎","鳩山一郎(2)",
                                          "鳩山一郎(3)","石橋湛山","岸信介","岸信介(2)","池田勇人","池田勇人(2)","池田勇人(3)","佐藤榮作","佐藤榮作(2)","佐藤榮作(3)",
                                          "田中角榮","田中角榮(2)","三木武夫","福田赳夫","大平正芳","大平正芳(2)","鈴木善幸","中曽根康弘","中曽根康弘(2)","中曽根康弘(3)",
                                          "竹下登","宇野宗佑","海部俊樹","海部俊樹(2)","宮澤喜一","細川護煕","羽田孜","村山富市","橋本龍太郎","橋本龍太郎(2)",
                                          "小渕恵三","森喜朗","森喜朗(2)","小泉純一郎","小泉純一郎(2)","小泉純一郎(3)","安倍晋三","福田康夫","麻生太郎","鳩山由紀夫",
                                          "菅直人","野田佳彦","安倍晋三(2)","安倍晋三(3)","安倍晋三(4)","菅義偉","岸田文雄","岸田文雄(2)","石破茂","石破茂(2)","高市早苗" };
                break;
            case "徳川家":
                list = new List<string> { "家康", "秀忠", "家光", "家綱", "綱吉", "家宣", "家継", "吉宗", "家重", "家治", "家斉", "家慶", "家定", "家茂", "慶喜" };
                break;
            default:
                for (int i = 0; i < 10; i++) list.Add("？");
                break;
        }
        return list;
    }

    string GetRandomItem(string type)
    {
        return GenerateSequence(type)[Random.Range(0, 10)];
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int r = Random.Range(i, list.Count);
            list[i] = list[r];
            list[r] = temp;
        }
    }
}
