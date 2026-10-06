using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private float dragThresholdPixels = 12f;

    private bool canInteract = true;
    private int movingQueuesCount = 0;

    public static PlayerInputHandler Instance { get; private set; }
    public bool CanInteract => canInteract;
    public void SetInteractable(bool value) => canInteract = value;

    private class TouchTrack
    {
        public int fingerId;
        public Vector2 downPos;
        public float downTime;
        public bool isDragging;
        public bool isPinch;
    }

    private readonly Dictionary<int, TouchTrack> activeTouches = new Dictionary<int, TouchTrack>();
    private bool isPinchActive = false;

    // Mouse tracking for Desktop / Editor
    private bool isMouseDown = false;
    private bool isMouseDragging = false;
    private Vector2 mouseDownPos;

    public static Vector3 MouseWorldPosition { get; private set; } = Vector3.zero;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        EnsureCamera();
        AntQueueController.OnQueueMoveStarted += HandleMoveStarted;
        AntQueueController.OnQueueMoveCompleted += HandleMoveCompleted;
    }

    private void Start()
    {
        EnsureCamera();
        Debug.Log($"[INPUT INITIALIZATION] movingQueuesCount={movingQueuesCount} canInteract={canInteract} mainCamera={(mainCamera != null ? mainCamera.name : "null")}");
    }

    private void EnsureCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var camObj = GameObject.FindWithTag("MainCamera");
                if (camObj != null) mainCamera = camObj.GetComponent<Camera>();
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        AntQueueController.OnQueueMoveStarted -= HandleMoveStarted;
        AntQueueController.OnQueueMoveCompleted -= HandleMoveCompleted;
    }

    private void HandleMoveStarted(AntQueueController queue)
    {
        movingQueuesCount++;
    }

    private void HandleMoveCompleted(AntQueueController queue)
    {
        movingQueuesCount = Mathf.Max(0, movingQueuesCount - 1);
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            EnsureCamera();
        }

        // Continuously update MouseWorldPosition for ant eye gaze tracking
        Vector2 currentPointerScreenPos = Input.mousePosition;
        if (Input.touchCount > 0)
        {
            currentPointerScreenPos = Input.GetTouch(0).position;
        }

        if (mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(currentPointerScreenPos);
            Plane gazePlane = new Plane(Vector3.up, new Vector3(0f, 0.32f, 0f));
            if (gazePlane.Raycast(ray, out float enter))
            {
                MouseWorldPosition = ray.GetPoint(enter);
            }
        }

        // Keyboard Shortcuts for Level Navigation in Game Mode:
        // 'E' -> Next Level
        // 'Q' -> Previous Level
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NextLevel();
                return;
            }
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PreviousLevel();
                return;
            }
        }

        // Block input if modal active or not in Playing state
        if ((GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) ||
            (GameUIManager.Instance != null && GameUIManager.Instance.IsAnyModalActive()))
        {
            bool wasDragging = isMouseDragging;
            foreach (var t in activeTouches.Values)
            {
                if (t.isDragging) { wasDragging = true; break; }
            }
            if (wasDragging)
            {
                BoardCameraController.Instance?.OnDragEnd();
            }
            isMouseDown = false;
            isMouseDragging = false;
            activeTouches.Clear();
            isPinchActive = false;
            return;
        }

        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        else
        {
            HandleMouseInput();
        }
    }

    private void HandleTouchInput()
    {
        // 1. Two-finger pinch-to-zoom detection on mobile
        if (Input.touchCount >= 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 touch0Prev = touch0.position - touch0.deltaPosition;
            Vector2 touch1Prev = touch1.position - touch1.deltaPosition;

            float prevTouchDeltaMag = (touch0Prev - touch1Prev).magnitude;
            float touchDeltaMag = (touch0.position - touch1.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
            if (Mathf.Abs(deltaMagnitudeDiff) > 1.2f)
            {
                isPinchActive = true;
                if (activeTouches.TryGetValue(touch0.fingerId, out var tt0)) tt0.isPinch = true;
                if (activeTouches.TryGetValue(touch1.fingerId, out var tt1)) tt1.isPinch = true;

                if (BoardCameraController.Instance != null)
                {
                    float zoomDelta = (deltaMagnitudeDiff / Mathf.Max(1, Screen.height)) * BoardCameraController.Instance.ZoomSensitivity * 16f;
                    BoardCameraController.Instance.AdjustZoom(zoomDelta);
                }
            }
        }
        else
        {
            isPinchActive = false;
        }

        // 2. Individual touch tracking per finger
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            int id = touch.fingerId;

            if (touch.phase == TouchPhase.Began)
            {
                bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(id);
                if (!isOverUI)
                {
                    activeTouches[id] = new TouchTrack
                    {
                        fingerId = id,
                        downPos = touch.position,
                        downTime = Time.unscaledTime,
                        isDragging = false,
                        isPinch = false
                    };
                }
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                if (activeTouches.TryGetValue(id, out var track))
                {
                    if (!track.isPinch && !isPinchActive)
                    {
                        float dist = Vector2.Distance(touch.position, track.downPos);
                        if (!track.isDragging && dist >= dragThresholdPixels)
                        {
                            track.isDragging = true;
                            if (BoardCameraController.Instance != null)
                            {
                                BoardCameraController.Instance.OnDragStart(track.downPos);
                                BoardCameraController.Instance.OnDrag(touch.position);
                            }
                        }
                        else if (track.isDragging)
                        {
                            BoardCameraController.Instance?.OnDrag(touch.position);
                        }
                    }
                }
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                if (activeTouches.TryGetValue(id, out var track))
                {
                    if (track.isDragging)
                    {
                        BoardCameraController.Instance?.OnDragEnd();
                    }
                    else if (!track.isPinch && !isPinchActive)
                    {
                        // Independent tap on this finger!
                        ProcessClickOrTap(track.downPos);
                    }
                    activeTouches.Remove(id);
                }
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                if (activeTouches.TryGetValue(id, out var track))
                {
                    if (track.isDragging)
                    {
                        BoardCameraController.Instance?.OnDragEnd();
                    }
                    activeTouches.Remove(id);
                }
            }
        }

        if (activeTouches.Count < 2)
        {
            isPinchActive = false;
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!isOverUI)
            {
                isMouseDown = true;
                isMouseDragging = false;
                mouseDownPos = Input.mousePosition;
            }
        }
        else if (isMouseDown && Input.GetMouseButton(0))
        {
            Vector2 mousePos = Input.mousePosition;
            if (!isMouseDragging)
            {
                float moveDist = Vector2.Distance(mousePos, mouseDownPos);
                if (moveDist >= dragThresholdPixels)
                {
                    isMouseDragging = true;
                    if (BoardCameraController.Instance != null)
                    {
                        BoardCameraController.Instance.OnDragStart(mouseDownPos);
                        BoardCameraController.Instance.OnDrag(mousePos);
                    }
                }
            }
            else
            {
                BoardCameraController.Instance?.OnDrag(mousePos);
            }
        }
        else if (isMouseDown && Input.GetMouseButtonUp(0))
        {
            if (isMouseDragging)
            {
                BoardCameraController.Instance?.OnDragEnd();
            }
            else
            {
                ProcessClickOrTap(mouseDownPos);
            }
            isMouseDown = false;
            isMouseDragging = false;
        }
    }

    public void ProcessClickOrTap(Vector2 screenPos)
    {
        if (!canInteract) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
        EnsureCamera();
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        AntQueueController targetQueue = null;
        Vector3 worldHitPos = Vector3.zero;
        Vector2Int gridCoord = new Vector2Int(-9999, -9999);

        // 1. Direct Physics Raycast against ant unit / queue colliders
        if (Physics.Raycast(ray, out RaycastHit hit, 150f, interactableLayers))
        {
            worldHitPos = hit.point;
            var antUnit = hit.collider.GetComponentInParent<AntUnit>();
            if (antUnit != null)
            {
                targetQueue = antUnit.GetComponentInParent<AntQueueController>();
            }
            if (targetQueue == null && BoardManager.Instance != null)
            {
                gridCoord = BoardManager.Instance.WorldToGridPosition(hit.point);
                targetQueue = BoardManager.Instance.GetQueueOccupying(gridCoord);
            }
        }

        // 2. Mathematical Board Ground Plane Raycast (covers full 1x1 grid cell area without missing gaps)
        if (targetQueue == null && BoardManager.Instance != null)
        {
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enterDist))
            {
                worldHitPos = ray.GetPoint(enterDist);
                gridCoord = BoardManager.Instance.WorldToGridPosition(worldHitPos);
                targetQueue = BoardManager.Instance.GetQueueOccupying(gridCoord);
            }
        }

        // Prevent repeated taps on an already moving or returning queue from launching it twice.
        // If a busy queue was tapped, check if there is an idle stationary queue at that grid cell underneath it.
        if (targetQueue != null && (targetQueue.IsMoving || targetQueue.MoveState != AntQueueController.QueueMoveState.Idle))
        {
            if (BoardManager.Instance != null && gridCoord.x != -9999)
            {
                var stationaryCandidate = BoardManager.Instance.GetQueueOccupying(gridCoord);
                if (stationaryCandidate != null && !stationaryCandidate.IsMoving && stationaryCandidate.MoveState == AntQueueController.QueueMoveState.Idle)
                {
                    targetQueue = stationaryCandidate;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        bool canMove = false;
        if (targetQueue != null && BoardManager.Instance != null)
        {
            canMove = BoardManager.Instance.IsPathClearForQueue(targetQueue);
        }

        // Concise diagnostic log per click
        Debug.Log($"[INPUT] screen={screenPos} world={worldHitPos} grid={gridCoord} queue={(targetQueue != null ? targetQueue.QueueId : "null")} movingQueuesCount={movingQueuesCount} canMove={canMove}");

        if (targetQueue != null && !targetQueue.IsMoving && targetQueue.MoveState == AntQueueController.QueueMoveState.Idle)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayQueueSelect();
            }

            targetQueue.TryStartMove();
        }
    }
}
