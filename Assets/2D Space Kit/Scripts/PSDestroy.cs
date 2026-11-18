using UnityEngine;
using System.Collections;

public class PSDestroy : MonoBehaviour {

	// Use this for initialization
	void Start () {
		ParticleSystem ps = GetComponent<ParticleSystem>();
		float duration = ps != null ? ps.main.duration : 1.0f;
		Destroy(gameObject, duration);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
