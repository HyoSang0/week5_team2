using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 패널 루트에 붙여 패드 입력이 감지될 때만 기본 선택 대상을 EventSystem에 지정하는 공용 컴포넌트.
/// 활성화 시에는 선택을 비워 두고, 패드 입력이 있으면 기본 대상을 선택하며 마우스 입력이 있으면 선택을 해제한다.
/// </summary>
[DefaultExecutionOrder(100)]
public class UIDefaultSelection : MonoBehaviour
{
    private const float MOVE_INPUT_THRESHOLD = 0.5f;
    private const float MOUSE_MOVE_THRESHOLD = 2f;

    [Header("Default Selection")]
    [SerializeField] private Selectable _defaultSelectable;
    [SerializeField] private float _inputLockSeconds = 0.3f;

    private float _enabledTime;

    /// <summary>
    /// 활성화된 직후부터 _inputLockSeconds초(unscaled 시간)가 지나지 않았는지 나타낸다.
    /// 선택 표시는 즉시 허용하고 클릭/확인 입력만 잠그는 데 사용한다.
    /// </summary>
    public bool IsInputLocked => Time.unscaledTime - _enabledTime < _inputLockSeconds;

    /// <summary>
    /// 활성화 시점을 기록하고 EventSystem의 이전 선택을 해제해 아무것도 선택되지 않은 상태로 시작한다.
    /// EventSystem.current가 없으면 시점 기록만 수행한다.
    /// </summary>
    void OnEnable()
    {
        _enabledTime = Time.unscaledTime;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// 패드 입력이 있는데 현재 선택이 없거나 무효하거나 이 패널의 자식이 아니면 기본 대상을 선택한다.
    /// 마우스 입력이 있는데 현재 선택이 이 패널의 자식이면 선택을 해제해 마우스 사용 중 강조를 남기지 않는다.
    /// </summary>
    void Update()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        if (HasGamepadInput() && (IsSelectionInvalid() || !IsSelectedInPanel()))
        {
            SelectDefault();
            return;
        }

        if (HasMouseInput() && IsSelectedInPanel())
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// 기본 선택 대상을 selectable로 교체한다. 반환값은 없고 즉시 선택하지는 않는다.
    /// </summary>
    public void SetDefault(Selectable selectable)
    {
        _defaultSelectable = selectable;
    }

    /// <summary>
    /// 기본 선택 대상이 유효하면 EventSystem.current의 현재 선택으로 지정한다.
    /// 대상이 null/비활성/interactable이 아니거나 EventSystem이 없으면 아무것도 하지 않는다.
    /// </summary>
    public void SelectDefault()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        if (_defaultSelectable == null
            || !_defaultSelectable.gameObject.activeInHierarchy
            || !_defaultSelectable.interactable)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(_defaultSelectable.gameObject);
    }

    /// <summary>
    /// 현재 EventSystem 선택이 이 패널(transform)의 자식일 때만 SelectDefault을 호출한다.
    /// 패드로 조작 중일 때만 기본 선택으로 되돌리는 데 사용한다.
    /// </summary>
    public void SelectDefaultIfNavigating()
    {
        if (EventSystem.current != null && IsSelectedInPanel())
        {
            SelectDefault();
        }
    }

    /// <summary>
    /// EventSystem의 현재 선택이 이 패널(transform)의 자식인지 판별한다.
    /// 선택이 없으면 false를 반환한다.
    /// </summary>
    private bool IsSelectedInPanel()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        return selected != null && selected.transform.IsChildOf(transform);
    }

    /// <summary>
    /// EventSystem의 현재 선택이 없거나 비활성이거나 interactable이 아닌 Selectable인지 판별한다.
    /// </summary>
    private bool IsSelectionInvalid()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy)
        {
            return true;
        }

        Selectable selectable = selected.GetComponent<Selectable>();
        return selectable != null && !selectable.interactable;
    }

    /// <summary>
    /// 연결된 Gamepad의 스틱/십자키 이동 입력이나 ABXY 버튼 눌림이 이번 프레임에 있었는지 판별한다.
    /// </summary>
    private bool HasGamepadInput()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null)
        {
            return false;
        }

        if (gamepad.leftStick.ReadValue().magnitude > MOVE_INPUT_THRESHOLD
            || gamepad.dpad.ReadValue().magnitude > MOVE_INPUT_THRESHOLD)
        {
            return true;
        }

        return gamepad.buttonSouth.wasPressedThisFrame
            || gamepad.buttonEast.wasPressedThisFrame
            || gamepad.buttonNorth.wasPressedThisFrame
            || gamepad.buttonWest.wasPressedThisFrame;
    }

    /// <summary>
    /// Mouse.current의 이동 델타 크기가 MOUSE_MOVE_THRESHOLD보다 크거나 좌/우 버튼이 이번 프레임에 눌렸는지 판별한다.
    /// </summary>
    private bool HasMouseInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return false;
        }

        if (mouse.delta.ReadValue().magnitude > MOUSE_MOVE_THRESHOLD)
        {
            return true;
        }

        return mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame;
    }
}
