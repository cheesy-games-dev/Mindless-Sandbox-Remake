using UnityEngine;

namespace MindlessSandbox
{
    [DisallowMultipleComponent]
    public class MoveCamera : MonoBehaviour
    {
        public Transform Head;

        void Update()
        {
            if (Head == null) return;
            transform.position = Head.position;
            transform.rotation = Head.rotation;
        }
    }
}