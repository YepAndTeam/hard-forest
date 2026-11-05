using UnityEngine;

public class KeepScale : MonoBehaviour
{
    private Vector3 startScale;

    void Awake() { startScale = transform.localScale; }

    void LateUpdate()
    {
        var parent = transform.parent.localScale;
        transform.localScale = new Vector3(
            startScale.x * Mathf.Sign(parent.x),
            startScale.y,
            startScale.z);
    }
}