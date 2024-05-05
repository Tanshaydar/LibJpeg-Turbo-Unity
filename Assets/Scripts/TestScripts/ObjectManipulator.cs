using UnityEngine;

public class ObjectManipulator : MonoBehaviour
{
    public GameObject objectToManipulate_Sideways;
    public GameObject objectToManipulate_Rotation;

    private bool ShouldMoveToRight;

    private void Update()
    {
        objectToManipulate_Rotation.transform.Rotate(Vector3.up * (Time.deltaTime * 10));
        if (objectToManipulate_Sideways.transform.position.x < -5)
            ShouldMoveToRight = true;
        else if (objectToManipulate_Sideways.transform.position.x > 5) ShouldMoveToRight = false;

        if (ShouldMoveToRight)
            objectToManipulate_Sideways.transform.Translate(Vector3.right * Time.deltaTime);
        else
            objectToManipulate_Sideways.transform.Translate(Vector3.left * Time.deltaTime);
    }
}