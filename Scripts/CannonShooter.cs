using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class CannonShooter : MonoBehaviour
{
    [Header("References")]
    public Rigidbody projectilePrefab;
    public Transform muzzle;
    [FormerlySerializedAs("pitchPivot")]
    public Transform head;
    public LineRenderer trajectoryLine;

    [Header("Firing")]
    public float launchSpeed = 15f;
    public float minSpeed = 5f;
    public float maxSpeed = 40f;
    public float fireCooldown = 0.4f;
    public float projectileLifetime = 6f;

    [Header("Aiming")]
    public float rotateSpeed = 40f;
    public float powerChangeSpeed = 10f;
    public Vector3 pitchAxis = Vector3.right;
    public float minElevation = 0f;
    public float maxElevation = 85f;

    [Header("Trajectory Preview")]
    public int previewPoints = 40;
    public float previewTimeStep = 0.08f;

    float nextFireTime;
    float yawAngle;
    float pitchAngle;
    Quaternion startLocalRotation;

    void Start()
    {
        if (head != null) startLocalRotation = head.localRotation;
    }

    void Update()
    {
        HandleAiming();
        HandlePower();

        if (FirePressed() && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireCooldown;
        }

        DrawTrajectory();
    }

    bool FirePressed()
    {
        bool key = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return key || mouse;
    }

    void ApplyHeadRotation()
    {
        head.localRotation = Quaternion.AngleAxis(yawAngle, Vector3.up) * startLocalRotation * Quaternion.AngleAxis(pitchAngle, pitchAxis);
    }

    void HandleAiming()
    {
        var kb = Keyboard.current;
        if (kb == null || head == null) return;

        float yaw = (kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.leftArrowKey.isPressed ? 1f : 0f);
        float pitch = (kb.upArrowKey.isPressed ? 1f : 0f) - (kb.downArrowKey.isPressed ? 1f : 0f);

        if (yaw == 0f && pitch == 0f) return;

        float before = Elevation();
        float oldPitch = pitchAngle;

        yawAngle += yaw * rotateSpeed * Time.deltaTime;
        pitchAngle -= pitch * rotateSpeed * Time.deltaTime;
        ApplyHeadRotation();

        float after = Elevation();
        if ((after < minElevation && after < before) || (after > maxElevation && after > before))
        {
            pitchAngle = oldPitch;
            ApplyHeadRotation();
        }
    }

    void HandlePower()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.eKey.isPressed) launchSpeed += powerChangeSpeed * Time.deltaTime;
        if (kb.qKey.isPressed) launchSpeed -= powerChangeSpeed * Time.deltaTime;
        launchSpeed = Mathf.Clamp(launchSpeed, minSpeed, maxSpeed);
    }

    void Fire()
    {
        if (projectilePrefab == null || muzzle == null) return;

        Rigidbody rb = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
        rb.gameObject.SetActive(true);
        rb.AddForce(muzzle.forward * launchSpeed, ForceMode.VelocityChange);
        Destroy(rb.gameObject, projectileLifetime);
    }

    float Elevation()
    {
        return Mathf.Asin(Mathf.Clamp(muzzle.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
    }

    void DrawTrajectory()
    {
        if (trajectoryLine == null || muzzle == null) return;

        Vector3 p0 = muzzle.position;
        Vector3 v0 = muzzle.forward * launchSpeed;
        Vector3 g = Physics.gravity;

        trajectoryLine.positionCount = previewPoints;
        for (int i = 0; i < previewPoints; i++)
        {
            float t = i * previewTimeStep;
            trajectoryLine.SetPosition(i, p0 + v0 * t + 0.5f * g * t * t);
        }
    }

    void OnGUI()
    {
        if (muzzle == null) return;

        float g = Mathf.Abs(Physics.gravity.y);
        float theta = Elevation() * Mathf.Deg2Rad;
        float v = launchSpeed;

        float range = v * v * Mathf.Sin(2f * theta) / g;
        float maxHeight = v * v * Mathf.Pow(Mathf.Sin(theta), 2f) / (2f * g);
        float flightTime = 2f * v * Mathf.Sin(theta) / g;

        GUI.Box(new Rect(10, 10, 280, 150), "Cannon Physics");
        GUI.Label(new Rect(20, 35, 260, 20), $"Speed (v):  {v:F1} m/s   [Q/E]");
        GUI.Label(new Rect(20, 55, 260, 20), $"Angle (θ):  {theta * Mathf.Rad2Deg:F1}°   [Up/Down]");
        GUI.Label(new Rect(20, 75, 260, 20), $"Range:  {range:F1} m");
        GUI.Label(new Rect(20, 95, 260, 20), $"Max height:  {maxHeight:F1} m");
        GUI.Label(new Rect(20, 115, 260, 20), $"Flight time:  {flightTime:F2} s");
        GUI.Label(new Rect(20, 135, 260, 20), "Fire: Space / Left Click");
    }
}