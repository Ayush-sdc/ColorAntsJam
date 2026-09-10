using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask interactableLayers = ~0;

    private bool canInteract = true;
    private int movingQueuesCount = 0;

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
        canInteract = false;
    }

    private void HandleMoveCompleted(AntQueueController queue)
    {
        movingQueuesCount = Mathf.Max(0, movingQueuesCount - 1);
        if (movingQueuesCount == 0)
        {
            canInteract = true;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[MOUSE INPUT REGISTERED]");
        }

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

        if (!canInteract) return;

        // Support both mouse click and screen touch
        bool inputTriggered = false;
        Vector2 screenPosition = Vector2.zero;

        if (Input.GetMouseButtonDown(0))
        {
            inputTriggered = true;
            screenPosition = Input.mousePosition;
        }
        else if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                inputTriggered = true;
                screenPosition = touch.position;
            }
        }

        if (inputTriggered)
        {
            ProcessClickOrTap(screenPosition);
        }
    }

    private void ProcessClickOrTap(Vector2 screenPos)
    {
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
        int pathLen = 0;

        if (targetQueue != null && BoardManager.Instance != null)
        {
            var path = AntPathfinder.FindPath(BoardManager.Instance, targetQueue);
            if (path != null && path.Count >= 2)
            {
                canMove = true;
                pathLen = path.Count;
            }
        }

        // Concise diagnostic log per click
        Debug.Log($"[INPUT] screen={screenPos} world={worldHitPos} grid={gridCoord} queue={(targetQueue != null ? targetQueue.QueueId : "null")} movingQueuesCount={movingQueuesCount} canMove={canMove} pathLength={pathLen}");

        if (targetQueue != null)
        {
            targetQueue.TryStartMove();
        }
    }
}
