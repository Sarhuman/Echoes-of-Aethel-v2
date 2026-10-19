using UnityEngine;

namespace EchoesOfAethel.V2
{
    [RequireComponent(typeof(Camera))]
    public sealed class AethelIsometricCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(8.5f, 12f, -9f);
        [SerializeField, Min(.01f)] private float smoothTime = .12f;
        [SerializeField, Min(1f)] private float orthographicSize = 6f;
        [SerializeField] private Vector3 focusOffset = new Vector3(0f, .8f, 0f);
        private Vector3 velocity;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
            if (target == null) return;
            transform.position = target.position + offset;
            transform.LookAt(target.position + focusOffset);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.LookAt(target.position + focusOffset);
            Camera view = GetComponent<Camera>();
            view.orthographic = true;
            view.orthographicSize = orthographicSize;
        }
    }
}
