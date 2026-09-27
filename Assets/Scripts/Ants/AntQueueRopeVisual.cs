using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AntsJam.Ropes
{
    /// <summary>
    /// Visual-only controller for GoldenRope rendering during parallel same-hole queue movement.
    /// Strictly visual: does not participate in collision, movement, pathfinding, or gameplay logic.
    /// </summary>
    [DisallowMultipleComponent]
    public class AntQueueRopeVisual : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float thicknessScale = 0.60f;
        [SerializeField] private float ropeHeight = 0.045f;
        [SerializeField] private float antBodyMargin = 0.25f;

        private AntQueueController ownerQueue;
        private GameObject ropeInstance;
        private Transform ropeTransform;
        private MeshRenderer ropeRenderer;
        private float baseMeshLength = 3.0f;
        private bool isRopeActive = false;
        private HoleController destinationHole;
        private Vector3 entryForward = Vector3.forward;

        public bool IsRopeActive => isRopeActive;
        public GameObject RopeInstance => ropeInstance;
        public float ThicknessScale => thicknessScale;
        public float RopeHeight => ropeHeight;

        private void Awake()
        {
            ownerQueue = GetComponent<AntQueueController>();
        }

        private void OnEnable()
        {
            if (ownerQueue == null)
            {
                ownerQueue = GetComponent<AntQueueController>();
            }
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        /// <summary>
        /// Explicit initialization for dynamically attached or configured visual controllers.
        /// </summary>
        public void Initialize(AntQueueController queue)
        {
            ownerQueue = queue;
        }

        /// <summary>
        /// Activates the GoldenRope visual for parallel movement toward destinationHole.
        /// </summary>
        public void EnableParallelRope(HoleController hole, Vector2Int forwardDir)
        {
            if (isRopeActive) return;
            if (hole == null || ownerQueue == null) return;

            destinationHole = hole;
            entryForward = new Vector3(forwardDir.x, 0f, forwardDir.y).normalized;
            if (entryForward.sqrMagnitude < 0.001f)
            {
                entryForward = ownerQueue.transform.forward;
                entryForward.y = 0f;
                entryForward.Normalize();
            }

            SpawnRopePrefab();
            isRopeActive = true;
            UpdateRopeTransform();
        }

        private void SpawnRopePrefab()
        {
            if (ropeInstance != null) return;

            int count = ownerQueue != null ? ownerQueue.AntCount : 3;
            string variantName = "GoldenRope_3Cells";
            baseMeshLength = 3.0f;

            if (count <= 2)
            {
                variantName = "GoldenRope_2Cells";
                baseMeshLength = 2.0f;
            }
            else if (count == 3)
            {
                variantName = "GoldenRope_3Cells";
                baseMeshLength = 3.0f;
            }
            else if (count == 4)
            {
                variantName = "GoldenRope_4Cells";
                baseMeshLength = 4.0f;
            }
            else
            {
                variantName = "GoldenRope_5Cells";
                baseMeshLength = 5.0f;
            }

            GameObject prefab = Resources.Load<GameObject>($"GoldenRope/{variantName}");
#if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/GoldenRope/Prefabs/{variantName}.prefab");
            }
#endif

            if (prefab == null)
            {
                Debug.LogWarning($"[AntQueueRopeVisual] Could not load GoldenRope prefab '{variantName}'.");
                return;
            }

            ropeInstance = Instantiate(prefab);
            ropeInstance.name = $"GoldenRope_{ownerQueue?.QueueId ?? "Visual"}";

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

            // Do not parent to moving ant to prevent walking bobbing from affecting rope
            ropeTransform.SetParent(null, true);
        }

        private void LateUpdate()
        {
            if (!isRopeActive) return;

            // Auto-disable if owner queue finished or destroyed
            if (ownerQueue == null || !ownerQueue.IsMoving)
            {
                Cleanup();
                return;
            }

            UpdateRopeTransform();
        }

        /// <summary>
        /// Updates rope position, orientation, and length based on remaining active ants and hole entrance clamp.
        /// </summary>
        public void UpdateRopeTransform()
        {
            if (ropeTransform == null || ownerQueue == null) return;

            // Collect active (non-swallowed) ant units
            var units = ownerQueue.Units;
            AntUnit headUnit = null;
            AntUnit tailUnit = null;

            if (units != null)
            {
                for (int i = 0; i < units.Count; i++)
                {
                    var u = units[i];
                    if (u != null && u.gameObject.activeInHierarchy)
                    {
                        if (headUnit == null) headUnit = u;
                        tailUnit = u;
                    }
                }
            }

            // If all ants have been swallowed, rope is finished
            if (headUnit == null || tailUnit == null)
            {
                Cleanup();
                return;
            }

            // Derive approach entry forward
            Vector3 fwd = entryForward;
            if (destinationHole == null)
            {
                destinationHole = ownerQueue.DestinationHole;
            }
            if (fwd.sqrMagnitude < 0.001f && ownerQueue.ForwardDirection != Vector2Int.zero)
            {
                fwd = new Vector3(ownerQueue.ForwardDirection.x, 0f, ownerQueue.ForwardDirection.y).normalized;
                entryForward = fwd;
            }

            Vector3 headPos = headUnit.transform.position;
            Vector3 tailPos = tailUnit.transform.position;

            Vector3 frontPos = headPos + fwd * antBodyMargin;
            Vector3 backPos = tailPos - fwd * antBodyMargin;

            // Clamp front position so rope terminates at hole entrance without penetrating through
            if (destinationHole != null)
            {
                Vector3 holeEntry = destinationHole.GetHoleEntryPoint(ownerQueue.HoleEntranceCoordinate, ownerQueue.ForwardDirection, ownerQueue);
                float maxFrontProj = Vector3.Dot(holeEntry, fwd);
                float frontProj = Vector3.Dot(frontPos, fwd);
                if (frontProj > maxFrontProj)
                {
                    // Clamp front to entrance rim along fwd
                    float excess = frontProj - maxFrontProj;
                    frontPos -= fwd * excess;
                }

                float backProj = Vector3.Dot(backPos, fwd);
                if (backProj > maxFrontProj)
                {
                    float excess = backProj - maxFrontProj;
                    backPos -= fwd * excess;
                }
            }

            float currentLength = Vector3.Dot(frontPos - backPos, fwd);
            if (currentLength < 0.08f)
            {
                // Last ant is completely in pit, hide renderer until cleanup
                if (ropeRenderer != null) ropeRenderer.enabled = false;
                return;
            }

            if (ropeRenderer != null) ropeRenderer.enabled = true;

            Vector3 center = (frontPos + backPos) * 0.5f;
            center.y = ropeHeight;

            ropeTransform.position = center;
            ropeTransform.rotation = Quaternion.LookRotation(fwd, Vector3.up);

            // Scale only Z (length axis); maintain constant X/Y thickness
            float scaleZ = Mathf.Max(0.01f, currentLength / baseMeshLength);
            ropeTransform.localScale = new Vector3(thicknessScale, thicknessScale, scaleZ);
        }

        /// <summary>
        /// Cleanly destroys the instantiated rope object.
        /// </summary>
        public void Cleanup()
        {
            isRopeActive = false;
            destinationHole = null;

            if (ropeInstance != null)
            {
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
    }
}
