using UnityEngine;

public class CubeCollisionTest : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Cube collided with: {collision.gameObject.name}");
    }
    
    void OnCollisionStay(Collision collision)
    {
        Debug.Log($"Cube staying in contact with: {collision.gameObject.name}");
    }
    
    void OnCollisionExit(Collision collision)
    {
        Debug.Log($"Cube stopped colliding with: {collision.gameObject.name}");
    }
}