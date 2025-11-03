using UnityEngine;
using UnityEditor;

public class AddRabbitToScene : EditorWindow
{
    [MenuItem("GameObject/Add Dancing Rabbit", false, 10)]
    static void AddRabbit()
    {
        // Load the Rabbit FBX
        GameObject rabbitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3D Models/Rabbit.fbx");

        if (rabbitPrefab == null)
        {
            Debug.LogError("Rabbit.fbx not found at Assets/3D Models/Rabbit.fbx");
            return;
        }

        // Instantiate the rabbit in the scene
        GameObject rabbit = (GameObject)PrefabUtility.InstantiatePrefab(rabbitPrefab);
        rabbit.name = "Dancing Rabbit";

        // Position it in front of the camera
        rabbit.transform.position = new Vector3(0, 1, 5);
        rabbit.transform.localScale = new Vector3(10, 10, 10);

        // Load and add the animator controller
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/RabbitAnimator.controller");

        if (controller == null)
        {
            Debug.LogError("RabbitAnimator.controller not found");
            return;
        }

        // Add or get Animator component
        Animator animator = rabbit.GetComponent<Animator>();
        if (animator == null)
        {
            animator = rabbit.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/3D Models/Rabbit.fbx");

        // Select the rabbit
        Selection.activeGameObject = rabbit;

        Debug.Log("Dancing Rabbit added to scene at position (0, 1, 5) with scale 10x. Press Play to see it dance!");
    }
}
