using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CinemachineCamera))]
public class ScrollZoom : MonoBehaviour
{
    public static ScrollZoom Instance { get; private set; }

    [Header("Zoom Settings")]
    [SerializeField] private InputActionReference _scroll;
    [SerializeField] private float _zoomSpeed = 10f;
    [SerializeField] private float _minZoom = 2f;
    [SerializeField] private float _maxZoom = 20f;
    [SerializeField] private AnimationCurve _zoomCurve;

    private CinemachineFollow _cinemachineFollow;

    public Vector2 ZoomRange => new(_minZoom, _maxZoom);
    public float ZoomProgress => 1 - Mathf.InverseLerp(ZoomRange.x, ZoomRange.y, _cinemachineFollow.FollowOffset.y);

    private void Awake()
    {
        _cinemachineFollow = GetComponent<CinemachineFollow>();
        Instance = this;
    }

    private void OnEnable()
    {
        _scroll.action.performed += Zoom;
    }

    private void OnDisable()
    {
        _scroll.action.performed -= Zoom;
    }
    
    private void Zoom(InputAction.CallbackContext ctx)
    {
        float scrollInput = ctx.ReadValue<Vector2>().y;
        Debug.Log("zoom " + scrollInput);

        Vector3 offset = _cinemachineFollow.FollowOffset;
        offset.y += scrollInput * -_zoomSpeed * _zoomCurve.Evaluate(ZoomProgress);
        offset.y = Mathf.Clamp(offset.y, _minZoom, _maxZoom);
        _cinemachineFollow.FollowOffset = offset;
    }
}