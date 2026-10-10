using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SlitherAnts.Editor
{
    public static class LeaderHeadLookVerificationSuite
    {
        private const string EvidenceDir = "Assets/TestEvidence/HeadLookReaction";

        [MenuItem("Tools/Slither Ants Jam/Head Look/Run Head Look Verification Suite")]
        public static void RunSuite()
        {
            Debug.Log("<color=cyan>===============================================================</color>");
            Debug.Log("<color=cyan>  RUNNING LEADER HEAD-LOOK REACTION VERIFICATION SUITE         </color>");
            Debug.Log("<color=cyan>===============================================================</color>");

            int passed = 0;
            int total = 0;

            void RunTest(string testName, Action testAction)
            {
                total++;
                try
                {
                    testAction();
                    passed++;
                    Debug.Log($"<color=green>[PASS]</color> Test {total}: {testName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"<color=red>[FAIL]</color> Test {total}: {testName}\n{ex}");
                }
            }

            RunTest("ChubbyAnt Prefabs HeadLookPivot Hierarchy Verification (All 18 prefabs)", TestPrefabHierarchy);
            RunTest("Leader vs Worker AntUnit Setup & Component Binding", TestAntUnitBinding);
            RunTest("Board-Space Detection Radius & Long Queue Segment Detection (1.5 cells)", TestDetectionRadiusAndSegments);
            RunTest("Turn Direction (Left vs Right) and 25-Degree Angular Clamping", TestTurnDirectionAndClamping);
            RunTest("Full Reaction State Lifecycle and Return to Neutral Pose", TestLifecycleAndReturnToNeutral);
            RunTest("Reaction Cooldown Enforcement (0.60s)", TestCooldownEnforcement);
            RunTest("Interruption on Own Queue Movement", TestInterruptionOnOwnMove);
            RunTest("Simultaneous / Linked Queue Multi-Start Disambiguation", TestSimultaneousQueueStarts);
            RunTest("Level Simulation & Gameplay Frame Recording", TestGameplayRecording);

            Debug.Log("<color=cyan>===============================================================</color>");
            Debug.Log($"<color={(passed == total ? "green" : "red")}>  HEAD LOOK SUITE FINISHED: {passed} / {total} TESTS PASSED  </color>");
            Debug.Log("<color=cyan>===============================================================</color>");
        }

        private static void TestPrefabHierarchy()
        {
            string[] guids = AssetDatabase.FindAssets("ChubbyAnt_", new[] { "Assets/ChubbyAntColors/Prefabs" });
            if (guids.Length < 18)
            {
                throw new Exception($"Expected at least 18 ChubbyAnt prefabs, found {guids.Length}");
            }

            string[] expectedHeadParts = { "head", "head_accent", "eyes", "pupils", "iris", "glints", "blush" };
            string[] expectedRootParts = { "body", "accent" };

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new Exception($"Failed to load prefab at {path}");

                Transform pivot = prefab.transform.Find("HeadLookPivot");
                if (pivot == null) throw new Exception($"Prefab {prefab.name} is missing 'HeadLookPivot' transform.");

                // Check pivot position (neck connection)
                if (Vector3.Distance(pivot.localPosition, new Vector3(0f, 0.50f, 0.15f)) > 0.01f)
                {
                    throw new Exception($"Prefab {prefab.name} HeadLookPivot localPosition {pivot.localPosition} != (0, 0.50, 0.15)");
                }

                foreach (string part in expectedHeadParts)
                {
                    Transform t = pivot.Find(part);
                    if (t == null) throw new Exception($"Prefab {prefab.name} HeadLookPivot is missing child '{part}'");
                    MeshFilter mf = t.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) throw new Exception($"Prefab {prefab.name} child '{part}' missing MeshFilter or mesh.");
                }

                foreach (string rootPart in expectedRootParts)
                {
                    Transform t = prefab.transform.Find(rootPart);
                    if (t == null) throw new Exception($"Prefab {prefab.name} is missing root child '{rootPart}'");
                    MeshFilter mf = t.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) throw new Exception($"Prefab {prefab.name} root child '{rootPart}' missing MeshFilter or mesh.");
                }
            }
        }

        private static void TestAntUnitBinding()
        {
            GameObject leaderObj = new GameObject("TestLeaderAnt");
            GameObject workerObj = new GameObject("TestWorkerAnt");
            try
            {
                AntUnit leaderUnit = leaderObj.AddComponent<AntUnit>();
                leaderUnit.Setup(AntColor.Red, head: true, tail: false);

                LeaderHeadLook headLook = leaderObj.GetComponent<LeaderHeadLook>();
                if (headLook == null) throw new Exception("Leader AntUnit did not receive LeaderHeadLook component.");
                if (headLook.HeadLookPivot == null) throw new Exception("LeaderHeadLook has null HeadLookPivot.");
                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.Neutral) throw new Exception("Initial state is not Neutral.");

                AntUnit workerUnit = workerObj.AddComponent<AntUnit>();
                workerUnit.Setup(AntColor.Red, head: false, tail: false);

                LeaderHeadLook workerHeadLook = workerObj.GetComponent<LeaderHeadLook>();
                if (workerHeadLook != null) throw new Exception("Worker AntUnit should NOT have LeaderHeadLook component.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(leaderObj);
                UnityEngine.Object.DestroyImmediate(workerObj);
            }
        }

        private static void TestDetectionRadiusAndSegments()
        {
            GameObject leaderObj = new GameObject("Leader_RadiusTest");
            GameObject movingQObj = new GameObject("MovingQ_RadiusTest");
            try
            {
                leaderObj.transform.position = new Vector3(0f, 0f, 0f);
                AntUnit leaderUnit = leaderObj.AddComponent<AntUnit>();
                leaderUnit.Setup(AntColor.Blue, head: true, tail: false);
                LeaderHeadLook headLook = leaderObj.GetComponent<LeaderHeadLook>();
                headLook.DetectionRadius = 1.50f;

                AntQueueController movingQueue = movingQObj.AddComponent<AntQueueController>();
                movingQueue.SetMovingForTesting(true);

                // Segment 0 at (2.0, 0, 0), Segment 1 at (1.2, 0, 0)
                // Leader of moving queue is far (2.0 > 1.5), but segment 1 is adjacent (1.2 <= 1.5)
                GameObject seg0 = new GameObject("Seg0");
                seg0.transform.SetParent(movingQObj.transform);
                seg0.transform.position = new Vector3(2.0f, 0f, 0f);
                AntUnit u0 = seg0.AddComponent<AntUnit>();
                u0.Setup(AntColor.Yellow, head: true, tail: false);

                GameObject seg1 = new GameObject("Seg1");
                seg1.transform.SetParent(movingQObj.transform);
                seg1.transform.position = new Vector3(1.2f, 0f, 0f);
                AntUnit u1 = seg1.AddComponent<AntUnit>();
                u1.Setup(AntColor.Yellow, head: false, tail: true);

                movingQueue.AddUnitForTesting(u0);
                movingQueue.AddUnitForTesting(u1);

                // Fire move start: moving queue is moving
                headLook.HandleGlobalQueueMoveStarted(movingQueue);

                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.TurningToward)
                {
                    throw new Exception($"Expected TurningToward because segment 1 is at 1.2 cells, but state was {headLook.CurrentState}");
                }

                headLook.ResetToNeutral();

                // Move all segments to distance 2.5 (> 1.5 cells)
                seg0.transform.position = new Vector3(2.5f, 0f, 0f);
                seg1.transform.position = new Vector3(2.8f, 0f, 0f);

                headLook.HandleGlobalQueueMoveStarted(movingQueue);
                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.Neutral)
                {
                    throw new Exception($"Expected Neutral when all segments are > 1.5 cells away, but state was {headLook.CurrentState}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(leaderObj);
                UnityEngine.Object.DestroyImmediate(movingQObj);
            }
        }

        private static void TestTurnDirectionAndClamping()
        {
            GameObject leaderObj = new GameObject("Leader_TurnTest");
            GameObject rightQObj = new GameObject("RightQ");
            GameObject leftQObj = new GameObject("LeftQ");
            try
            {
                leaderObj.transform.position = Vector3.zero;
                leaderObj.transform.rotation = Quaternion.LookRotation(Vector3.forward); // facing +Z

                AntUnit leaderUnit = leaderObj.AddComponent<AntUnit>();
                leaderUnit.Setup(AntColor.Green, head: true, tail: false);
                LeaderHeadLook headLook = leaderObj.GetComponent<LeaderHeadLook>();
                headLook.MaxHeadTurnAngle = 25.0f;
                headLook.TurnTowardDuration = 0.15f;

                // 1. Right target at (+1.0, 0, +0.5) -> should turn right (positive angle)
                AntQueueController rightQ = rightQObj.AddComponent<AntQueueController>();
                rightQ.SetMovingForTesting(true);
                GameObject rSeg = new GameObject("RSeg");
                rSeg.transform.SetParent(rightQObj.transform);
                rSeg.transform.position = new Vector3(1.0f, 0f, 0.5f);
                AntUnit ru = rSeg.AddComponent<AntUnit>();
                ru.Setup(AntColor.Red, head: true);
                rightQ.AddUnitForTesting(ru);

                headLook.TestTriggerReaction(rightQ);
                headLook.StepHeadLook(0.15f); // complete turn

                if (headLook.CurrentTurnAngle <= 0f)
                {
                    throw new Exception($"Expected positive turn angle to the right, got {headLook.CurrentTurnAngle:F2} deg");
                }
                if (headLook.CurrentTurnAngle > 25.05f)
                {
                    throw new Exception($"Turn angle exceeded 25 deg clamp: {headLook.CurrentTurnAngle:F2} deg");
                }

                headLook.ResetToNeutral();

                // 2. Left target at (-1.0, 0, +0.5) -> should turn left (negative angle)
                AntQueueController leftQ = leftQObj.AddComponent<AntQueueController>();
                leftQ.SetMovingForTesting(true);
                GameObject lSeg = new GameObject("LSeg");
                lSeg.transform.SetParent(leftQObj.transform);
                lSeg.transform.position = new Vector3(-1.0f, 0f, 0.5f);
                AntUnit lu = lSeg.AddComponent<AntUnit>();
                lu.Setup(AntColor.Red, head: true);
                leftQ.AddUnitForTesting(lu);

                headLook.TestTriggerReaction(leftQ);
                headLook.StepHeadLook(0.15f);

                if (headLook.CurrentTurnAngle >= 0f)
                {
                    throw new Exception($"Expected negative turn angle to the left, got {headLook.CurrentTurnAngle:F2} deg");
                }
                if (headLook.CurrentTurnAngle < -25.05f)
                {
                    throw new Exception($"Turn angle exceeded negative 25 deg clamp: {headLook.CurrentTurnAngle:F2} deg");
                }

                headLook.ResetToNeutral();

                // 3. Target directly behind at (0, 0, -1.0) -> must clamp safely to <= 25 deg, never twist backward
                rSeg.transform.position = new Vector3(0.5f, 0f, -1.0f);
                headLook.TestTriggerReaction(rightQ);
                headLook.StepHeadLook(0.15f);

                if (Mathf.Abs(headLook.CurrentTurnAngle) > 25.05f)
                {
                    throw new Exception($"Target behind twisted past 25 deg limit: {headLook.CurrentTurnAngle:F2} deg");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(leaderObj);
                UnityEngine.Object.DestroyImmediate(rightQObj);
                UnityEngine.Object.DestroyImmediate(leftQObj);
            }
        }

        private static void TestLifecycleAndReturnToNeutral()
        {
            GameObject leaderObj = new GameObject("Leader_LifecycleTest");
            GameObject movingQObj = new GameObject("MovingQ_Lifecycle");
            try
            {
                leaderObj.transform.position = Vector3.zero;
                leaderObj.transform.rotation = Quaternion.LookRotation(Vector3.forward);

                AntUnit leaderUnit = leaderObj.AddComponent<AntUnit>();
                leaderUnit.Setup(AntColor.Pink, head: true);
                LeaderHeadLook headLook = leaderObj.GetComponent<LeaderHeadLook>();
                headLook.TurnTowardDuration = 0.15f;
                headLook.WatchDuration = 0.40f;
                headLook.ReturnDuration = 0.25f;

                Quaternion authoredNeutral = headLook.HeadLookPivot.localRotation;

                AntQueueController movingQ = movingQObj.AddComponent<AntQueueController>();
                movingQ.SetMovingForTesting(true);
                GameObject seg = new GameObject("Seg");
                seg.transform.SetParent(movingQObj.transform);
                seg.transform.position = new Vector3(1.0f, 0f, 0f);
                AntUnit u = seg.AddComponent<AntUnit>();
                u.Setup(AntColor.Cyan, head: true);
                movingQ.AddUnitForTesting(u);

                // Start reaction
                headLook.TestTriggerReaction(movingQ);
                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.TurningToward)
                    throw new Exception($"Expected TurningToward at t=0, got {headLook.CurrentState}");

                // Advance halfway into TurnToward (t = 0.08s)
                headLook.StepHeadLook(0.08f);
                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.TurningToward)
                    throw new Exception($"Expected TurningToward at t=0.08, got {headLook.CurrentState}");

                // Finish TurnToward (t = 0.08 + 0.08 = 0.16s >= 0.15s)
                headLook.StepHeadLook(0.08f);
                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.Watching)
                    throw new Exception($"Expected Watching after 0.15s, got {headLook.CurrentState}");

                // Advance during Watching (t = 0.20s in watch)
                headLook.StepHeadLook(0.20f);
                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.Watching)
                    throw new Exception($"Expected Watching during watch period, got {headLook.CurrentState}");

                // Finish Watching (t = 0.25s more >= 0.40s)
                headLook.StepHeadLook(0.25f);
                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.Returning)
                    throw new Exception($"Expected Returning after watch period, got {headLook.CurrentState}");

                // Advance halfway into Returning (t = 0.12s)
                headLook.StepHeadLook(0.12f);
                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.Returning)
                    throw new Exception($"Expected Returning during return phase, got {headLook.CurrentState}");

                // Finish Returning (t = 0.15s more >= 0.25s)
                headLook.StepHeadLook(0.15f);
                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.Neutral)
                    throw new Exception($"Expected Neutral after return completes, got {headLook.CurrentState}");

                // Check exact return to neutral rotation (0.000 deg offset)
                float angleDiff = Quaternion.Angle(headLook.HeadLookPivot.localRotation, authoredNeutral);
                if (angleDiff > 0.01f)
                {
                    throw new Exception($"Head did not return precisely to authored neutral pose! Residual offset: {angleDiff:F4} deg");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(leaderObj);
                UnityEngine.Object.DestroyImmediate(movingQObj);
            }
        }

        private static void TestCooldownEnforcement()
        {
            GameObject leaderObj = new GameObject("Leader_CooldownTest");
            GameObject movingQObj = new GameObject("MovingQ_Cooldown");
            try
            {
                leaderObj.transform.position = Vector3.zero;
                AntUnit leaderUnit = leaderObj.AddComponent<AntUnit>();
                leaderUnit.Setup(AntColor.Brown, head: true);
                LeaderHeadLook headLook = leaderObj.GetComponent<LeaderHeadLook>();
                headLook.TurnTowardDuration = 0.15f;
                headLook.WatchDuration = 0.40f;
                headLook.ReturnDuration = 0.25f;
                headLook.ReactionCooldown = 0.60f;

                AntQueueController movingQ = movingQObj.AddComponent<AntQueueController>();
                movingQ.SetMovingForTesting(true);
                GameObject seg = new GameObject("Seg");
                seg.transform.SetParent(movingQObj.transform);
                seg.transform.position = new Vector3(1.0f, 0f, 0f);
                AntUnit u = seg.AddComponent<AntUnit>();
                u.Setup(AntColor.Yellow, head: true);
                movingQ.AddUnitForTesting(u);

                // Run cycle to completion
                headLook.TestTriggerReaction(movingQ);
                headLook.StepHeadLook(0.16f);
                headLook.StepHeadLook(0.41f);
                headLook.StepHeadLook(0.26f);
                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.Neutral)
                    throw new Exception("Did not return to Neutral.");

                // Attempt to trigger immediately (on cooldown)
                headLook.HandleGlobalQueueMoveStarted(movingQ);
                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.Neutral)
                {
                    throw new Exception("Leader reacted during cooldown period!");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(leaderObj);
                UnityEngine.Object.DestroyImmediate(movingQObj);
            }
        }

        private static void TestInterruptionOnOwnMove()
        {
            GameObject myQObj = new GameObject("MyQueue_Interruption");
            GameObject otherQObj = new GameObject("OtherQueue_Interruption");
            try
            {
                AntQueueController myQueue = myQObj.AddComponent<AntQueueController>();
                GameObject leaderSeg = new GameObject("LeaderSeg");
                leaderSeg.transform.SetParent(myQObj.transform);
                leaderSeg.transform.position = Vector3.zero;
                leaderSeg.transform.rotation = Quaternion.LookRotation(Vector3.forward);

                AntUnit leaderUnit = leaderSeg.AddComponent<AntUnit>();
                leaderUnit.Setup(AntColor.Cyan, head: true);
                myQueue.AddUnitForTesting(leaderUnit);

                LeaderHeadLook headLook = leaderSeg.GetComponent<LeaderHeadLook>();
                Quaternion authoredNeutral = headLook.HeadLookPivot.localRotation;

                AntQueueController otherQ = otherQObj.AddComponent<AntQueueController>();
                otherQ.SetMovingForTesting(true);
                GameObject otherSeg = new GameObject("OtherSeg");
                otherSeg.transform.SetParent(otherQObj.transform);
                otherSeg.transform.position = new Vector3(1.0f, 0f, 0.5f);
                AntUnit otherU = otherSeg.AddComponent<AntUnit>();
                otherU.Setup(AntColor.Purple, head: true);
                otherQ.AddUnitForTesting(otherU);

                // Trigger reaction towards other queue
                headLook.TestTriggerReaction(otherQ);
                headLook.StepHeadLook(0.15f); // turned towards other queue
                if (Mathf.Abs(headLook.CurrentTurnAngle) < 1f) throw new Exception("Head did not turn.");

                // Now my own queue starts moving
                leaderUnit.StartWalkAnimation();

                // Reaction must be canceled immediately, restored to neutral
                if (headLook.CurrentState != LeaderHeadLook.HeadLookState.Neutral)
                {
                    throw new Exception($"Reaction not canceled when own queue moved! State: {headLook.CurrentState}");
                }

                float angleDiff = Quaternion.Angle(headLook.HeadLookPivot.localRotation, authoredNeutral);
                if (angleDiff > 0.01f)
                {
                    throw new Exception($"Head not restored to neutral on interruption! Offset: {angleDiff:F4} deg");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(myQObj);
                UnityEngine.Object.DestroyImmediate(otherQObj);
            }
        }

        private static void TestSimultaneousQueueStarts()
        {
            GameObject leaderObj = new GameObject("Leader_Simultaneous");
            GameObject qNearObj = new GameObject("Q_Near");
            GameObject qFarObj = new GameObject("Q_Far");
            try
            {
                leaderObj.transform.position = Vector3.zero;
                AntUnit leaderUnit = leaderObj.AddComponent<AntUnit>();
                leaderUnit.Setup(AntColor.Yellow, head: true);
                LeaderHeadLook headLook = leaderObj.GetComponent<LeaderHeadLook>();

                AntQueueController qNear = qNearObj.AddComponent<AntQueueController>();
                qNear.SetMovingForTesting(true);
                qNear.QueueId = "NearQ";
                GameObject nSeg = new GameObject("NSeg");
                nSeg.transform.SetParent(qNearObj.transform);
                nSeg.transform.position = new Vector3(0.8f, 0f, 0f);
                AntUnit nu = nSeg.AddComponent<AntUnit>();
                nu.Setup(AntColor.Red, head: true);
                qNear.AddUnitForTesting(nu);

                AntQueueController qFar = qFarObj.AddComponent<AntQueueController>();
                qFar.SetMovingForTesting(true);
                qFar.QueueId = "FarQ";
                GameObject fSeg = new GameObject("FSeg");
                fSeg.transform.SetParent(qFarObj.transform);
                fSeg.transform.position = new Vector3(1.4f, 0f, 0f);
                AntUnit fu = fSeg.AddComponent<AntUnit>();
                fu.Setup(AntColor.Red, head: true);
                qFar.AddUnitForTesting(fu);

                // First far starts, then on the same frame near starts
                headLook.HandleGlobalQueueMoveStarted(qFar);
                headLook.HandleGlobalQueueMoveStarted(qNear);

                if (headLook.TargetQueueId != "NearQ")
                {
                    throw new Exception($"Leader did not prefer nearest queue on simultaneous start! Target was {headLook.TargetQueueId}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(leaderObj);
                UnityEngine.Object.DestroyImmediate(qNearObj);
                UnityEngine.Object.DestroyImmediate(qFarObj);
            }
        }

        private static void TestGameplayRecording()
        {
            Directory.CreateDirectory(Path.Combine(EvidenceDir, "Frames"));

            // Setup a camera, lights, and two adjacent queues (one moving, one idle observer)
            GameObject root = new GameObject("GameplayRecordRoot");
            try
            {
                GameObject camObj = new GameObject("RecordCam");
                camObj.transform.SetParent(root.transform);
                Camera cam = camObj.AddComponent<Camera>();
                cam.backgroundColor = new Color(0.18f, 0.22f, 0.20f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                camObj.transform.position = new Vector3(1.0f, 4.0f, -0.2f);
                camObj.transform.rotation = Quaternion.Euler(75f, 0f, 0f);

                GameObject lightObj = new GameObject("RecordLight");
                lightObj.transform.SetParent(root.transform);
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                // Queue A (Idle Observer at x = 0.0, z = 0.0, facing +Z)
                GameObject qAObj = new GameObject("QueueA_Observer");
                qAObj.transform.SetParent(root.transform);
                AntQueueController qA = qAObj.AddComponent<AntQueueController>();
                GameObject aLeader = new GameObject("A_Leader");
                aLeader.transform.SetParent(qAObj.transform);
                aLeader.transform.position = new Vector3(0.0f, 0f, 0.0f);
                aLeader.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                AntUnit aUnit = aLeader.AddComponent<AntUnit>();
                aUnit.Setup(AntColor.Red, head: true);
                qA.AddUnitForTesting(aUnit);

                LeaderHeadLook aHeadLook = aLeader.GetComponent<LeaderHeadLook>();

                // Queue B (Moving Queue at x = 1.2, starting at z = -0.5, moving forward to z = 2.0)
                GameObject qBObj = new GameObject("QueueB_Moving");
                qBObj.transform.SetParent(root.transform);
                AntQueueController qB = qBObj.AddComponent<AntQueueController>();
                qB.SetMovingForTesting(true);
                qB.QueueId = "QueueB";
                GameObject bLeader = new GameObject("B_Leader");
                bLeader.transform.SetParent(qBObj.transform);
                bLeader.transform.position = new Vector3(1.2f, 0f, -0.4f);
                bLeader.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                AntUnit bUnit = bLeader.AddComponent<AntUnit>();
                bUnit.Setup(AntColor.Blue, head: true);
                qB.AddUnitForTesting(bUnit);

                GameObject bFollower = new GameObject("B_Follower");
                bFollower.transform.SetParent(qBObj.transform);
                bFollower.transform.position = new Vector3(1.2f, 0f, -1.0f);
                bFollower.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                AntUnit bFollowerUnit = bFollower.AddComponent<AntUnit>();
                bFollowerUnit.Setup(AntColor.Blue, head: false, tail: true);
                qB.AddUnitForTesting(bFollowerUnit);

                // Queue C (Distant Observer at x = -3.0, z = 0.0, facing +Z) -> should remain neutral
                GameObject qCObj = new GameObject("QueueC_Distant");
                qCObj.transform.SetParent(root.transform);
                AntQueueController qC = qCObj.AddComponent<AntQueueController>();
                GameObject cLeader = new GameObject("C_Leader");
                cLeader.transform.SetParent(qCObj.transform);
                cLeader.transform.position = new Vector3(-3.0f, 0f, 0.0f);
                cLeader.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                AntUnit cUnit = cLeader.AddComponent<AntUnit>();
                cUnit.Setup(AntColor.Green, head: true);
                qC.AddUnitForTesting(cUnit);
                LeaderHeadLook cHeadLook = cLeader.GetComponent<LeaderHeadLook>();

                // Trigger move started for Queue B
                aHeadLook.HandleGlobalQueueMoveStarted(qB);
                cHeadLook.HandleGlobalQueueMoveStarted(qB);

                if (cHeadLook.CurrentState != LeaderHeadLook.HeadLookState.Neutral)
                {
                    throw new Exception("Distant leader should remain in Neutral state.");
                }

                // Render 30 frames representing the head look cycle:
                // Frames 0-8: Turn toward (0.15s)
                // Frames 9-20: Watch and track moving ant (0.40s)
                // Frames 21-29: Return to neutral (0.25s)
                int w = 640;
                int h = 480;
                RenderTexture rt = new RenderTexture(w, h, 24);
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);

                string framesDir = Path.Combine(EvidenceDir, "Frames");
                float totalDuration = 0.80f;
                int frameCount = 30;

                for (int f = 0; f < frameCount; f++)
                {
                    float dt = totalDuration / frameCount;

                    // Move queue B forward smoothly
                    float bZ = Mathf.Lerp(-0.4f, 1.8f, (float)f / frameCount);
                    bLeader.transform.position = new Vector3(1.2f, 0f, bZ);
                    bFollower.transform.position = new Vector3(1.2f, 0f, bZ - 0.6f);

                    // Step head look
                    aHeadLook.StepHeadLook(dt);

                    // Render camera
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    cam.targetTexture = null;

                    string framePath = Path.Combine(framesDir, $"head_look_frame_{f:D3}.png");
                    File.WriteAllBytes(framePath, tex.EncodeToPNG());
                }

                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);

                Debug.Log($"<color=cyan>[Evidence]</color> Successfully rendered {frameCount} gameplay frames to {framesDir}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
