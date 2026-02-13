using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Header("Cameras")]
    public Camera menuCamera;      
    public Camera screenCamera;    

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotateSpeed = 120f;
    public float fovSpeed = 40f;

    [Header("FOV")]
    public float zoomInFOV = 60f;

    [Header("Idle Unstable Movement")]
    public float idleMoveAmount = 3f;
    public float idleMoveSpeed = 0.5f;

    private Vector3 homePos;
    private Quaternion homeRot;
    private float homeFOV;

    private Vector3 idleBasePos;
    private float idleTime;

    private enum CameraState { Idle, ZoomIn, ZoomOut }
    private CameraState state = CameraState.Idle;

    void Start()
    {
        homePos = menuCamera.transform.position;
        homeRot = menuCamera.transform.rotation;
        homeFOV = menuCamera.fieldOfView;

        idleBasePos = homePos;
    }

    void Update()
    {
        if (state == CameraState.Idle)
        {
            ApplyIdleUnstableMovement();
            return;
        }

        Vector3 targetPos;
        Quaternion targetRot;
        float targetFOV;

        if (state == CameraState.ZoomIn)
        {
            // Uses the screen camera's transform rahrah
            targetPos = screenCamera.transform.position;
            targetRot = screenCamera.transform.rotation;
            targetFOV = zoomInFOV;
        }
        else 
        {
            targetPos = homePos;
            targetRot = homeRot;
            targetFOV = homeFOV;
        }

        //forda moving ng camera
        menuCamera.transform.position = Vector3.MoveTowards(
            menuCamera.transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );

        // rotation to the rigt angle
        menuCamera.transform.rotation = Quaternion.RotateTowards(
            menuCamera.transform.rotation,
            targetRot,
            rotateSpeed * Time.deltaTime
        );

        //field of view change
        menuCamera.fieldOfView = Mathf.MoveTowards(
            menuCamera.fieldOfView,
            targetFOV,
            fovSpeed * Time.deltaTime
        );

        // idle
        if (Vector3.Distance(menuCamera.transform.position, targetPos) < 0.01f &&
            Quaternion.Angle(menuCamera.transform.rotation, targetRot) < 0.5f)
        {
            menuCamera.transform.position = targetPos;
            menuCamera.transform.rotation = targetRot;
            menuCamera.fieldOfView = targetFOV;

            idleBasePos = targetPos;
            state = CameraState.Idle;
        }
    }

    void ApplyIdleUnstableMovement()
    {
        idleTime += Time.deltaTime * idleMoveSpeed;

        float x = Mathf.Sin(idleTime) * idleMoveAmount * 0.6f;
        float y = Mathf.Sin(idleTime * 1.3f) * idleMoveAmount;

        menuCamera.transform.position =
            idleBasePos +
            menuCamera.transform.right * x +
            menuCamera.transform.up * y;
    }

    public void ZoomIn()
    {
        state = CameraState.ZoomIn;
        Debug.Log("Zoom IN");
    }

    public void ZoomOut()
    {
        state = CameraState.ZoomOut;
        Debug.Log("Zoom OUT");
    }
}
