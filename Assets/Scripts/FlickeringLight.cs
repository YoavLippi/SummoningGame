using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
	private new Light light;
	[SerializeField] private float minIntensity = 1.0f;
	[SerializeField] private float maxIntensity = 1.8f;
	[SerializeField] private float speed = 1.5f;

	void Start() => light = GetComponent<Light>();

	void Update()
	{
		float noise = Mathf.PerlinNoise(Time.time * speed, 0);
		light.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
	}
}
