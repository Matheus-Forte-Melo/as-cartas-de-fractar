using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class MapNavigation : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private float orthographicSize = 5f;

    [Header("Navigation")]
    [SerializeField] private float scrollSpeed = 2f;
    [SerializeField] private float dragSensitivity = 1f;
    [SerializeField] private float verticalPadding = 2f;

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private MapGenerator mapGenerator;

    private float _fixedX;
    private float _minY;
    private float _maxY;
    private bool _boundsReady;

    private bool _isDragging;
    private Vector2 _lastPointerPosition;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mapGenerator == null)
            mapGenerator = FindFirstObjectByType<MapGenerator>();

        mainCamera.orthographicSize = orthographicSize;
        _fixedX = mainCamera.transform.position.x;

        TryInitBounds();
    }

    private void TryInitBounds()
    {
        if (_boundsReady || mapGenerator == null || mapGenerator.Graph.Count == 0)
            return;

        float minNodeY = float.MaxValue;
        float maxNodeY = float.MinValue;

        foreach (var node in mapGenerator.Graph.Values)
        {
            if (node.WorldPosition.y < minNodeY) minNodeY = node.WorldPosition.y;
            if (node.WorldPosition.y > maxNodeY) maxNodeY = node.WorldPosition.y;
        }

        _minY = minNodeY - verticalPadding;
        _maxY = maxNodeY + verticalPadding;
        _boundsReady = true;

        Vector3 pos = mainCamera.transform.position;
        pos.x = _fixedX;
        pos.y = _minY + orthographicSize;
        mainCamera.transform.position = pos;
    }

    private void Update()
    {
        if (!_boundsReady)
        {
            TryInitBounds();
            if (!_boundsReady) return;
        }

        HandleScrollWheel();
        HandlePointerDrag();
        ClampCameraPosition();
    }

    private void HandleScrollWheel()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scrollRaw = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scrollRaw) < 0.01f) return;

        Vector3 pos = mainCamera.transform.position;
        pos.y += Mathf.Sign(scrollRaw) * scrollSpeed;
        mainCamera.transform.position = pos;
    }

    private void HandlePointerDrag()
    {
        if (Touch.activeTouches.Count > 0)
        {
            HandleTouchDrag();
            return;
        }

        HandleMouseDrag();
    }

    private void HandleMouseDrag()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _isDragging = true;
            _lastPointerPosition = mouse.position.ReadValue();
        }

        if (mouse.leftButton.wasReleasedThisFrame)
            _isDragging = false;

        if (!_isDragging) return;

        Vector2 current = mouse.position.ReadValue();
        ApplyDragDelta(current);
    }

    private void HandleTouchDrag()
    {
        var touch = Touch.activeTouches[0];

        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            _isDragging = true;
            _lastPointerPosition = touch.screenPosition;
            return;
        }

        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
            touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
        {
            _isDragging = false;
            return;
        }

        if (!_isDragging) return;

        ApplyDragDelta(touch.screenPosition);
    }

    private void ApplyDragDelta(Vector2 currentPosition)
    {
        Vector2 delta = currentPosition - _lastPointerPosition;
        float worldDeltaY = (delta.y / Screen.height) * mainCamera.orthographicSize * 2f * dragSensitivity;

        Vector3 pos = mainCamera.transform.position;
        pos.y -= worldDeltaY;
        mainCamera.transform.position = pos;

        _lastPointerPosition = currentPosition;
    }

    private void ClampCameraPosition()
    {
        float halfView = mainCamera.orthographicSize;
        float clampedMinY = _minY + halfView;
        float clampedMaxY = _maxY - halfView;

        if (clampedMinY > clampedMaxY)
            clampedMinY = clampedMaxY = (_minY + _maxY) * 0.5f;

        Vector3 pos = mainCamera.transform.position;
        pos.x = _fixedX;
        pos.y = Mathf.Clamp(pos.y, clampedMinY, clampedMaxY);
        mainCamera.transform.position = pos;
    }
}
