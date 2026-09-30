using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private float dragThresholdPixels = 12f;

    private bool canInteract = true;
    private int movingQueuesCount = 0;

    private bool isPointerDown = false;
    private bool isCameraDragging = false;
    private Vector2 pointerDownScreenPos;

    public static Vector3 MouseWorldPosition { get; private set; } = Vector3.zero;

    private void Awake()
    {
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

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            if (isCameraDragging)
            {
                BoardCameraController.Instance?.OnDragEnd();
            }
            isPointerDown = false;
            isCameraDragging = false;
            return;
        }

        HandleUnifiedPointerInput();
    }

    private void HandleUnifiedPointerInput()
    {
        bool pointerDown = false;
        bool pointerHeld = false;
        bool pointerUp = false;
        Vector2 pointerPos = Vector2.zero;
        int pointerId = -1;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointerPos = touch.position;
            pointerId = touch.fingerId;

            if (touch.phase == TouchPhase.Began) 
            {
                pointerDown = true;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) 
            {
                pointerHeld = true;
            }
            else if (touch.phase == TouchPhase.Ended) 
            {
                pointerUp = true;
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                // Cancelled Android touches must NEVER trigger taps
                isPointerDown = false;
                if (isCameraDragging)
                {
                    BoardCameraController.Instance?.OnDragEnd();
                    isCameraDragging = false;
                }
                return;
            }
        }
        else
        {
            pointerPos = Input.mousePosition;
            if (Input.GetMouseButtonDown(0)) pointerDown = true;
            else if (Input.GetMouseButton(0)) pointerHeld = true;
            if (Input.GetMouseButtonUp(0)) pointerUp = true;
        }

        if (pointerDown)
        {
            bool isOverUI = false;
            if (EventSystem.current != null)
            {
                if (Input.touchCount > 0)
                    isOverUI = EventSystem.current.IsPointerOverGameObject(pointerId);
                else
                    isOverUI = EventSystem.current.IsPointerOverGameObject();
            }

            if (!isOverUI)
            {
                isPointerDown = true;
                isCameraDragging = false;
                pointerDownScreenPos = pointerPos;
            }
        }

        if (isPointerDown && pointerHeld)
        {
            if (!isCameraDragging)
            {
                float moveDist = Vector2.Distance(pointerPos, pointerDownScreenPos);
                if (moveDist >= dragThresholdPixels)
                {
                    isCameraDragging = true;
                    if (BoardCameraController.Instance != null)
                    {
                        BoardCameraController.Instance.OnDragStart(pointerDownScreenPos);
                        BoardCameraController.Instance.OnDrag(pointerPos);
                    }
                }
            }
            else
            {
                if (BoardCameraController.Instance != null)
                {
                    BoardCameraController.Instance.OnDrag(pointerPos);
                }
            }
        }

        if (pointerUp)
        {
            if (isPointerDown)
            {
                if (isCameraDragging)
                {
                    if (BoardCameraController.Instance != null)
                    {
                        BoardCameraController.Instance.OnDragEnd();
                    }
                }
                else
                {
                    // TAP GESTURE: process ant selection / move
                    ProcessClickOrTap(pointerDownScreenPos);
                }
            }
            isPointerDown = false;
            isCameraDragging = false;
        }
    }

    public void ProcessClickOrTap(Vector2 screenPos)
    {
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

        bool canMove = false;
        if (targetQueue != null && BoardManager.Instance != null)
        {
            canMove = BoardManager.Instance.IsPathClearForQueue(targetQueue);
        }

        // Concise diagnostic log per click
        Debug.Log($"[INPUT] screen={screenPos} world={worldHitPos} grid={gridCoord} queue={(targetQueue != null ? targetQueue.QueueId : "null")} movingQueuesCount={movingQueuesCount} canMove={canMove}");

        if (targetQueue != null)
        {
            targetQueue.TryStartMove();
        }
    }
}
