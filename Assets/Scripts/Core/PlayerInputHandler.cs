using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask interactableLayers = ~0;

    private bool canInteract = true;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        AntQueueController.OnQueueMoveStarted += HandleMoveStarted;
        AntQueueController.OnQueueMoveCompleted += HandleMoveCompleted;
    }

    private void OnDestroy()
    {
        AntQueueController.OnQueueMoveStarted -= HandleMoveStarted;
        AntQueueController.OnQueueMoveCompleted -= HandleMoveCompleted;
    }

    private void HandleMoveStarted(AntQueueController queue)
    {
        // One queue moves at a time rule: disable interaction while moving
        canInteract = false;
    }

    private void HandleMoveCompleted(AntQueueController queue)
    {
        canInteract = true;
    }

    private void Update()
    {
        if (!canInteract) return;

        // Support both mouse click and screen touch
        bool inputTriggered = false;
        Vector2 screenPosition = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputTriggered = true;
            screenPosition = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            inputTriggered = true;
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (inputTriggered)
        {
            ProcessClickOrTap(screenPosition);
        }
    }

    private void ProcessClickOrTap(Vector2 screenPos)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 150f, interactableLayers))
        {
            // Check if user clicked directly on an ant unit
            var antUnit = hit.collider.GetComponentInParent<AntUnit>();
            if (antUnit != null)
            {
                var queue = antUnit.GetComponentInParent<AntQueueController>();
                if (queue != null)
                {
                    queue.TryStartMove();
                    return;
                }
            }

            // Or if user clicked on a grid cell occupying an ant
            Vector2Int clickedGrid = BoardManager.Instance.WorldToGridPosition(hit.point);
            var occupyingQueue = BoardManager.Instance.GetQueueOccupying(clickedGrid);
            if (occupyingQueue != null)
            {
                occupyingQueue.TryStartMove();
            }
        }
    }
}
