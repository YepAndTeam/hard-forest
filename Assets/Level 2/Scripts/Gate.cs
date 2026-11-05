using UnityEngine;

public class Gate : MonoBehaviour
{
    public void Open()
    {
        Destroy(gameObject);
    }
}