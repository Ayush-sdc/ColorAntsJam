using UnityEngine;

namespace AntQueueKit {
    // A reusable visual queue. Path coordinates are local to this object.
    public class AntQueue : MonoBehaviour {
        public GameObject antPrefab;
        [Min(1)] public int count = 8;
        [Min(0.05f)] public float spacing = 0.75f;
        public float speed = 0.65f;
        public bool moving;
        public Vector2[] path = { new Vector2(-2,0), new Vector2(2,0), new Vector2(2,2) };
        Transform[] ants;
        float travel;
        void Start() {
            if (!antPrefab || path == null || path.Length < 2) return;
            ants = new Transform[count];
            for(int i=0;i<count;i++) ants[i]=Instantiate(antPrefab,transform).transform;
            Place();
        }
        void Update() { if(moving) travel += speed * Time.deltaTime; Place(); }
        public void SetMoving(bool value) { moving=value; }
        void Place() {
            if(ants==null) return;
            float length=0;
            for(int i=1;i<path.Length;i++) length+=Vector2.Distance(path[i-1],path[i]);
            for(int i=0;i<ants.Length;i++) {
                float d=travel+(count-1-i)*spacing;
                ants[i].gameObject.SetActive(d<=length);
                if(d>length) continue;
                for(int j=1;j<path.Length;j++) {
                    Vector2 delta=path[j]-path[j-1]; float segment=delta.magnitude;
                    if(segment<0.0001f) continue;
                    if(d<=segment || j==path.Length-1) {
                        ants[i].localPosition=Vector2.Lerp(path[j-1],path[j],d/segment);
                        ants[i].localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-90);
                        break;
                    }
                    d-=segment;
                }
            }
        }
        void OnDrawGizmosSelected() {
            if(path==null) return;
            Gizmos.color=Color.white;
            for(int i=1;i<path.Length;i++) Gizmos.DrawLine(transform.TransformPoint(path[i-1]),transform.TransformPoint(path[i]));
        }
    }
}

