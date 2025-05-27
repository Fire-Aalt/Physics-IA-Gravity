using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CinemachineCamera))]
public class ScrollZoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private InputActionReference _scroll;
    [SerializeField] private float _zoomSpeed = 10f;
    [SerializeField] private float _minZoom = 2f;
    [SerializeField] private float _maxZoom = 20f;
    [SerializeField] private AnimationCurve _zoomCurve;

    private CinemachineFollow _cinemachineFollow;

    private void Awake()
    {
        _cinemachineFollow = GetComponent<CinemachineFollow>();
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

        Vector3 offset = _cinemachineFollow.FollowOffset;
        var t = 1-Mathf.InverseLerp(_minZoom, _maxZoom, offset.y);
        offset.y += scrollInput * -_zoomSpeed * _zoomCurve.Evaluate(t);
        offset.y = Mathf.Clamp(offset.y, _minZoom, _maxZoom);
        _cinemachineFollow.FollowOffset = offset;
    }
}