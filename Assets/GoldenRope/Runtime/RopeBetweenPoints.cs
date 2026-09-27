using UnityEngine;

namespace AntsJam.Ropes
{
    /// <summary>Optional endpoint positioning for an existing baked rope. No physics or gameplay rules.</summary>
    [ExecuteAlways]
    public class RopeBetweenPoints : MonoBehaviour
    {
        public Transform start;
        public Transform end;
        [Min(0.001f)] public float meshLength = 3f;
        [Min(0.001f)] public float thicknessScale = 1f;
        public Vector3 worldOffset = new Vector3(0,0.25f,0);

        void LateUpdate()
        {
            if(!start || !end) return;
            Vector3 delta=end.position-start.position;
            if(delta.sqrMagnitude<0.000001f) return;
            transform.position=(start.position+end.position)*0.5f+worldOffset;
            Vector3 up=Mathf.Abs(Vector3.Dot(delta.normalized,Vector3.up))>.99f?Vector3.forward:Vector3.up;
            transform.rotation=Quaternion.LookRotation(delta,up);
            // Root-level or uniformly scaled parent is recommended.
            Vector3 parentScale=transform.parent?transform.parent.lossyScale:Vector3.one;
            transform.localScale=new Vector3(thicknessScale/Mathf.Max(.0001f,Mathf.Abs(parentScale.x)),thicknessScale/Mathf.Max(.0001f,Mathf.Abs(parentScale.y)),delta.magnitude/meshLength/Mathf.Max(.0001f,Mathf.Abs(parentScale.z)));
        }
    }
}
