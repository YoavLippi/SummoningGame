using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
	private Light _light;
	public float minIntensity = 0.5f;
	public float maxIntensity = 1.5f;
	public float smoothing = 0.1f;

	void Start()
	{
		_light = GetComponent<Light>();
	}

	void Update()
	{
		// Randomly pick a new intensity and smoothly move toward it
		float targetIntensity = Random.Range(minIntensity, maxIntensity);
		_light.intensity = Mathf.Lerp(_light.intensity, targetIntensity, smoothing);
	}
}
