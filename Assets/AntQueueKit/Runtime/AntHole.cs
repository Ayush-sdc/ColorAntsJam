using UnityEngine;
namespace AntQueueKit {
    public class AntHole : MonoBehaviour {
        public string colorId;
        [Min(0)] public int remaining=4;
        public TextMesh label;
        public void SetRemaining(int value) { remaining=Mathf.Max(0,value); Refresh(); }
        public bool Accept(string incomingColor) {
            if(incomingColor!=colorId || remaining<=0) return false;
            SetRemaining(remaining-1); return true;
        }
        void OnValidate() { Refresh(); }
        void Awake() { Refresh(); }
        void Refresh() { if(label) label.text=remaining.ToString(); }
    }
}
