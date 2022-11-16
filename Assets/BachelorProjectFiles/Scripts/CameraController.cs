using UnityEngine;

public class CameraController : MonoBehaviour {

    [SerializeField]
    [Range(5, 50)]
    private int movementSpeed = 10;
    [SerializeField]
    [Range(1, 300)]
    private int lookSpeed = 100;

    private Vector2 curRotation = new Vector2();

    void Start() {
        curRotation.x = transform.rotation.y;
        curRotation.y = transform.rotation.x;
    }

    void Update() {

        int newMovementSpeed = Input.GetKey(KeyCode.LeftShift) ? movementSpeed * 3 : movementSpeed;

        //camera movement via WASD (forward/back and left/right based on orientation/rotation)
        float forwardSpeed = Input.GetAxis("Vertical") * newMovementSpeed * Time.deltaTime;
        float rightSpeed = Input.GetAxis("Horizontal") * newMovementSpeed * Time.deltaTime;

        Vector3 worldUpVector = transform.InverseTransformVector(Vector3.up);

        transform.Translate(Vector3.Cross(Vector3.right, worldUpVector).normalized * forwardSpeed);
        transform.Translate(Vector3.right * rightSpeed);

        //camera up down movement in world space, not local space
        if (Input.GetKey(KeyCode.R)) {
            float upSpeed = newMovementSpeed * Time.deltaTime;
            transform.Translate(Vector3.up * upSpeed, Space.World);
        }
        if (Input.GetKey(KeyCode.F)) {
            float downSpeed = newMovementSpeed * Time.deltaTime;
            transform.Translate(Vector3.down * downSpeed, Space.World);
        }

        //camera rotation via mouse 
        //rotate camera while holding right-click and dragging the mouse
        if (Input.GetMouseButton(1)) {
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed * Time.deltaTime;

            curRotation.x += mouseX;
            curRotation.y -= mouseY;
            curRotation.y = Mathf.Clamp(curRotation.y, -90f, 90f);

            transform.localRotation = Quaternion.Euler(curRotation.y, curRotation.x, 0f);
        }

    }
}
