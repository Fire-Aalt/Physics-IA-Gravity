using UnityEngine;

public class TrailScaler : MonoBehaviour
{
    [SerializeField] private float _minZoomWidth = 1f;
    [SerializeField] private float _maxZoomWidth = 20f;
    
    private void LateUpdate()
    {
        var bodies = SimulationManager.Instance.Bodies;
        foreach (var body in bodies)
        {
            if (body.DoNotScaleTrail) continue;
            var widthMult = Mathf.Lerp(_minZoomWidth, _maxZoomWidth, 1 - ScrollZoom.Instance.ZoomProgress);
            body.trailRenderer.widthMultiplier = Utils.ToSimulationLength(body.RealRadius * widthMult);
        }
    }
}
