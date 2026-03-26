using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Скролл камеры свайпом вверх/вниз (и мышью в редакторе).
/// Камера смотрит вертикально вниз, поэтому скролл = движение по оси Z.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Scroll Bounds")]
    [SerializeField] private float minZ = -14f;
    [SerializeField] private float maxZ =  14f;

    [Header("Feel")]
    [SerializeField] private float dragSensitivity = 0.015f;
    [SerializeField] private float smoothSpeed     = 4f;

    [SerializeField] private float dragThreshold = 8f;  // пикселей до начала скролла

    // PlacementSystem проверяет этот флаг чтобы не ставить юнита после свайпа
    public static bool IsDragging { get; private set; }

    private float   targetZ;
    private Vector2 pressPos;
    private Vector2 lastPointerPos;
    private bool    trackingPress;

    private void Start()
    {
        targetZ = transform.position.z;
    }

    private void Update()
    {
        HandleInput();
        var pos = transform.position;
        pos.z = Mathf.Lerp(pos.z, targetZ, Time.deltaTime * smoothSpeed);
        transform.position = pos;
    }

    private void HandleInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            var phase = touch.phase.ReadValue();

            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                pressPos = touch.position.ReadValue();
                lastPointerPos = pressPos;
                trackingPress = true;
                IsDragging = false;
            }
            else if (trackingPress && (phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                                       phase == UnityEngine.InputSystem.TouchPhase.Stationary))
            {
                Vector2 current = touch.position.ReadValue();
                if (!IsDragging && Vector2.Distance(current, pressPos) > dragThreshold)
                    IsDragging = true;
                if (IsDragging)
                {
                    float delta = (lastPointerPos.y - current.y) * dragSensitivity;
                    targetZ = Mathf.Clamp(targetZ + delta, minZ, maxZ);
                }
                lastPointerPos = current;
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                     phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                trackingPress = false;
                IsDragging = false;
            }
        }
        else if (Mouse.current != null)
        {
            // Левая кнопка — свайп (с порогом, чтобы не конфликтовать с кликом)
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                pressPos = Mouse.current.position.ReadValue();
                lastPointerPos = pressPos;
                trackingPress = true;
                IsDragging = false;
            }
            else if (trackingPress && Mouse.current.leftButton.isPressed)
            {
                Vector2 current = Mouse.current.position.ReadValue();
                if (!IsDragging && Vector2.Distance(current, pressPos) > dragThreshold)
                    IsDragging = true;
                if (IsDragging)
                {
                    float delta = (lastPointerPos.y - current.y) * dragSensitivity;
                    targetZ = Mathf.Clamp(targetZ + delta, minZ, maxZ);
                }
                lastPointerPos = current;
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                trackingPress = false;
                IsDragging = false;
            }

            // Правая кнопка и колёсико тоже работают
            if (Mouse.current.rightButton.wasPressedThisFrame)
                lastPointerPos = Mouse.current.position.ReadValue();
            else if (Mouse.current.rightButton.isPressed)
            {
                Vector2 current = Mouse.current.position.ReadValue();
                float delta = (lastPointerPos.y - current.y) * dragSensitivity;
                targetZ = Mathf.Clamp(targetZ + delta, minZ, maxZ);
                lastPointerPos = current;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                targetZ = Mathf.Clamp(targetZ + scroll * 0.5f, minZ, maxZ);
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            pressPos = Input.mousePosition;
            lastPointerPos = Input.mousePosition;
            trackingPress = true;
            IsDragging = false;
        }
        else if (trackingPress && Input.GetMouseButton(0))
        {
            Vector2 current = Input.mousePosition;
            if (!IsDragging && Vector2.Distance(current, pressPos) > dragThreshold)
                IsDragging = true;
            if (IsDragging)
            {
                float delta = (lastPointerPos.y - current.y) * dragSensitivity;
                targetZ = Mathf.Clamp(targetZ + delta, minZ, maxZ);
            }
            lastPointerPos = current;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            trackingPress = false;
            IsDragging = false;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
            targetZ = Mathf.Clamp(targetZ + scroll * 5f, minZ, maxZ);
#endif
    }
}
