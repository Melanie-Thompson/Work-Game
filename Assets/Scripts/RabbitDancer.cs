using UnityEngine;

public class RabbitDancer : MonoBehaviour
{
    private Transform armR;
    private Transform legL;
    private Transform legR;
    private float time = 0f;

    void Start()
    {
        // Just use the armatures directly - they're the shoulder joints
        armR = transform.Find("Armature.003");
        legL = transform.Find("Armature.001");

        if (armR == null) Debug.LogError("Could not find Armature.003 (right arm)");
        if (legL == null) Debug.LogError("Could not find Armature.001 (left arm)");
    }

    void Update()
    {
        time += Time.deltaTime * 0.5f; // Much slower

        // Wave right arm gently - only X axis
        if (armR != null)
        {
            Vector3 armRot = armR.localEulerAngles;
            armRot.x = Mathf.Sin(time) * 25f; // Slow gentle wave
            armR.localEulerAngles = armRot;
        }

        // Wave left arm gently (opposite timing) - only X axis
        if (legL != null)
        {
            Vector3 armLRot = legL.localEulerAngles;
            armLRot.x = Mathf.Sin(time + Mathf.PI) * 25f; // Slow gentle wave
            legL.localEulerAngles = armLRot;
        }
    }
}
