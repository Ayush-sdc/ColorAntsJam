using UnityEngine;
using DG.Tweening;

namespace AntsJam.Ropes
{
    /// <summary>
    /// Visual-only controller for GoldenRope connecting two linked ant queues.
    /// Manages horizontal GoldenRope spanning between the leaders of Queue A and Queue B.
    /// Strictly visual: does not participate in collision, movement, pathfinding, or gameplay logic.
    /// </summary>
    [DisallowMultipleComponent]
    public class LinkedAntsRopeVisual : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float thicknessScale = 0.48f;
        [SerializeField] private float ropeHeight = 0.20f;
        [SerializeField] private float backOffset = 0.08f;
        [SerializeField] private float maxSpanDistance = 5.5f;

        private AntQueueController queueA;
        private AntQueueController queueB;
        private GameObject ropeInstance;
        private Transform ropeTransform;
        private MeshRenderer ropeRenderer;
        private float baseMeshLength = 2.0f;
        private bool isRopeActive = false;
        private bool isDisappearing = false;

        public bool IsRopeActive => isRopeActive;
        public GameObject RopeInstance => ropeInstance;
        public AntQueueController QueueA => queueA;
        public AntQueueController QueueB => queueB;

        public void Initialize(AntQueueController a, AntQueueController b)
        {
            queueA = a;
            queueB = b;

            SpawnRopePrefab();
            isRopeActive = true;
            isDisappearing = false;

            UpdateRopeTransform();
        }

        private void SpawnRopePrefab()
        {
            if (ropeInstance != null) return;

            string variantName = "GoldenRope_2Cells";
            baseMeshLength = 2.0f;

            GameObject prefab = Resources.Load<GameObject>($"GoldenRope/{variantName}");
#if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/GoldenRope/Prefabs/{variantName}.prefab");
            }
#endif
            if (prefab == null)
            {
                Debug.LogWarning($"[LinkedAntsRopeVisual] Could not load GoldenRope prefab '{variantName}'.");
                return;
            }

            ropeInstance = Instantiate(prefab);
            ropeInstance.name = $"LinkedRope_{queueA?.QueueId ?? "A"}_{queueB?.QueueId ?? "B"}";

            // Strip any extraneous physics or collider components (purely visual)
            foreach (var col in ropeInstance.GetComponentsInChildren<Collider>(true))
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
            foreach (var rb in ropeInstance.GetComponentsInChildren<Rigidbody>(true))
            {
                if (Application.isPlaying) Destroy(rb);
                else DestroyImmediate(rb);
            }

            ropeTransform = ropeInstance.transform;
            ropeRenderer = ropeInstance.GetComponentInChildren<MeshRenderer>();

            // Root-level placement so ant walking bob does NOT affect rope
            ropeTransform.SetParent(null, true);
        }

        private void LateUpdate()
        {
            if (!isRopeActive || isDisappearing) return;

            // Check if either queue was destroyed or both have finished
            if (queueA == null || queueB == null || !queueA.gameObject.activeInHierarchy || !queueB.gameObject.activeInHierarchy)
            {
                DismissRope();
                return;
            }

            // Check if either leader has entered the hole pit
            if (HasLeaderEnteredHole(queueA) || HasLeaderEnteredHole(queueB))
            {
                DismissRope();
                return;
            }

            UpdateRopeTransform();
        }

        private bool HasLeaderEnteredHole(AntQueueController queue)
        {
            if (queue == null) return true;
            if (!queue.gameObject.activeInHierarchy) return true;

            // Only moving queues can enter a hole. While waiting at rest, leaders are NEVER in a hole.
            if (!queue.IsMoving)
            {
                return false;
            }

            // If head unit is inactive or descending into the hole pit below floor level
            if (queue.Units != null && queue.Units.Count > 0)
            {
                var headUnit = queue.Units[0];
                if (headUnit == null || !headUnit.gameObject.activeInHierarchy || headUnit.transform.position.y < -0.10f)
                {
                    return true;
                }
            }

            // If queue completed or all units swallowed
            if (queue.Units == null || queue.Units.Count == 0)
            {
                return true;
            }

            return false;
        }

        public void UpdateRopeTransform()
        {
            if (ropeTransform == null || queueA == null || queueB == null) return;

            Vector3 attachA = GetLeaderAttachmentPoint(queueA);
            Vector3 attachB = GetLeaderAttachmentPoint(queueB);

            Vector3 delta = attachB - attachA;
            float dist = delta.magnitude;

            // If stretched beyond safe threshold, dismiss cleanly
            if (dist > maxSpanDistance || dist < 0.05f)
            {
                DismissRope();
                return;
            }

            Vector3 mid = (attachA + attachB) * 0.5f;
            ropeTransform.position = mid;

            Vector3 ropeForward = delta.normalized;
            ropeTransform.rotation = Quaternion.LookRotation(ropeForward, Vector3.up);

            // Scale only the length axis (Z); keep thickness (X and Y) constant
            float scaleZ = Mathf.Max(0.01f, dist / baseMeshLength);
            ropeTransform.localScale = new Vector3(thicknessScale, thicknessScale, scaleZ);
        }

        private Vector3 GetLeaderAttachmentPoint(AntQueueController queue)
        {
            Vector3 headPos;
            Vector3 fwd = queue.GetHeadForwardWorld();

            if (queue.Units != null && queue.Units.Count > 0 && queue.Units[0] != null)
            {
                headPos = queue.Units[0].transform.position;
            }
            else if (queue.CurrentBodyCoordinates != null && queue.CurrentBodyCoordinates.Count > 0 && BoardManager.Instance != null)
            {
                headPos = BoardManager.Instance.GridToWorldPosition(queue.CurrentBodyCoordinates[0]);
            }
            else
            {
                headPos = queue.transform.position;
            }

            // Offset slightly behind the face so eyes/antennas/mouth remain unobscured
            Vector3 attach = headPos - fwd * backOffset;
            attach.y = ropeHeight;
            return attach;
        }

        public void DismissRope()
        {
            if (isDisappearing) return;
            isDisappearing = true;

            if (ropeTransform != null && Application.isPlaying)
            {
                ropeTransform.DOKill();
                ropeTransform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    Cleanup();
                });
            }
            else
            {
                Cleanup();
            }
        }

        public void Cleanup()
        {
            isRopeActive = false;
            isDisappearing = false;

            if (ropeInstance != null)
            {
                ropeTransform?.DOKill();
                if (Application.isPlaying)
                {
                    Destroy(ropeInstance);
                }
                else
                {
                    DestroyImmediate(ropeInstance);
                }
                ropeInstance = null;
                ropeTransform = null;
                ropeRenderer = null;
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
