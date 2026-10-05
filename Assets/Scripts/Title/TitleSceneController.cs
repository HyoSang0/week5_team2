using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using TMPro;

/// <summary>타이틀 메뉴와 조작 안내를 제어하고 본게임으로 전환한다.</summary>
public class TitleSceneController : MonoBehaviour
{
    private static TitleSceneController _instance;

    [Header("Scene")]
    [SerializeField] private string _gameSceneName = "MainScene";

    [Header("Menu")]
    [SerializeField] private RectTransform _menuPanel;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _helpButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private GameObject _helpPanel;
    [SerializeField] private Button _closeHelpButton;
    [SerializeField] private TMP_Text _menuHint;
    private bool _menuFocused;
    private bool _loading;

    public static bool IsPointerOverMenu => _instance != null
        && Mouse.current != null
        && RectTransformUtility.RectangleContainsScreenPoint(
            _instance._menuPanel, Mouse.current.position.ReadValue());

    void Awake()
    {
        _instance = this;
        GamePause.ResetAll();
        _helpPanel.SetActive(false);
    }

    void Start()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.sendNavigationEvents = false;
    }

    void OnEnable()
    {
        _startButton.onClick.AddListener(StartGame);
        _helpButton.onClick.AddListener(OpenHelp);
        _quitButton.onClick.AddListener(QuitGame);
        _closeHelpButton.onClick.AddListener(CloseHelp);
    }

    void OnDisable()
    {
        _startButton.onClick.RemoveListener(StartGame);
        _helpButton.onClick.RemoveListener(OpenHelp);
        _quitButton.onClick.RemoveListener(QuitGame);
        _closeHelpButton.onClick.RemoveListener(CloseHelp);
        GamePause.Resume(PauseReason.TitleMenu);
        if (_instance == this)
        {
            _instance = null;
        }
    }

    void Update()
    {
        if (_loading)
        {
            return;
        }

        bool toggle = (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
        bool cancel = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        if (_helpPanel.activeSelf)
        {
            if (toggle || cancel)
            {
                CloseHelp();
            }
            return;
        }

        if (toggle || (cancel && _menuFocused))
        {
            SetMenuFocus(!_menuFocused);
        }
    }

    /// <summary>
    /// 연습 중인 상태를 종료하고 설정된 본게임 씬을 비동기로 불러온다.
    /// _gameSceneName을 사용하며 중복 실행을 막고 일시정지와 런 통계를 초기화한다.
    /// </summary>
    public void StartGame()
    {
        if (_loading)
        {
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(_gameSceneName))
        {
            _menuHint.text = "게임 씬을 불러올 수 없습니다. Build Settings를 확인해주세요.";
            return;
        }

        _loading = true;
        _menuHint.text = "게임을 불러오는 중…";
        _startButton.interactable = false;
        _helpButton.interactable = false;
        _quitButton.interactable = false;
        EventSystem.current.sendNavigationEvents = true;
        GamePause.ResetAll();
        StatisticsManager.Instance.ResetRun();
        SceneManager.LoadSceneAsync(_gameSceneName);
    }

    /// <summary>
    /// 조작 안내 패널을 열고 연습을 일시정지한다.
    /// 연결된 패널과 닫기 버튼을 사용하며 UI 탐색 및 선택 상태를 변경한다.
    /// </summary>
    public void OpenHelp()
    {
        _helpPanel.SetActive(true);
        EventSystem.current.sendNavigationEvents = true;
        EventSystem.current.SetSelectedGameObject(_closeHelpButton.gameObject);
        GamePause.Pause(PauseReason.TitleMenu);
    }

    /// <summary>
    /// 조작 안내를 닫고 이전 메뉴 집중 상태로 돌아간다.
    /// _menuFocused를 사용하여 메뉴 선택 또는 연습 재개 상태를 복원한다.
    /// </summary>
    public void CloseHelp()
    {
        _helpPanel.SetActive(false);
        SetMenuFocus(_menuFocused);
    }

    /// <summary>
    /// 키보드·패드 메뉴 조작과 연습 조작 사이를 전환한다.
    /// focused를 저장하고 UI 탐색, 기본 선택, 일시정지 상태와 안내 문구를 갱신한다.
    /// </summary>
    private void SetMenuFocus(bool focused)
    {
        _menuFocused = focused;
        EventSystem.current.sendNavigationEvents = focused;
        EventSystem.current.SetSelectedGameObject(focused ? _startButton.gameObject : null);
        _menuHint.text = focused
            ? "메뉴 선택 중 · Tab / 시작 버튼으로 연습 재개"
            : "Tab / 패드 시작 버튼 · 메뉴 선택";
        if (focused)
        {
            GamePause.Pause(PauseReason.TitleMenu);
        }
        else
        {
            GamePause.Resume(PauseReason.TitleMenu);
        }
    }

    /// <summary>
    /// 실행 중인 게임의 종료를 요청한다.
    /// 별도 입력값은 없으며 Editor에서는 종료 대신 안내 문구를 표시한다.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        _menuHint.text = "게임 종료는 빌드된 게임에서 동작합니다.";
#else
        Application.Quit();
#endif
    }
}
