using UnityEngine;

public class TrailScaler : MonoBehaviour
{
    [SerializeField] private TimeRange _trailDuration;
    [SerializeField] private float _minZoomWidth = 1f;
    [SerializeField] private float _maxZoomWidth = 20f;
    
    private void LateUpdate()
    {
        var sim = SimulationManager.Instance;
        if (sim.IsSimulationPaused) return;
        
        var bodies = sim.Bodies;
        foreach (var body in bodies)
        {
            if (body.DoNotScaleTrail) continue;
            var widthMult = Mathf.Lerp(_minZoomWidth, _maxZoomWidth, 1 - ScrollZoom.Instance.ZoomProgress);
            
            body.trailRenderer.widthMultiplier = Utils.ToSimulationLength(body.RealRadius * widthMult);
            body.trailRenderer.time = (float)_trailDuration.Get() / (float)sim.FinalTimeScale / Mathf.Sqrt((float)sim.gravityConstantMultiplier);
        }
    }
}
