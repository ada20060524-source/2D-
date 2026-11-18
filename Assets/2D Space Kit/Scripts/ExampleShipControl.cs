using UnityEngine;
using System.Collections;

public class ExampleShipControl : MonoBehaviour {

	public float acceleration_amount = 1f;
	public float rotation_speed = 1f;
	public GameObject turret;
	public float turret_rotation_speed = 3f;

	private Rigidbody2D rb2d;

	// Use this for initialization
	void Start () {
		rb2d = GetComponent<Rigidbody2D>();
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
		}

		if (rb2d == null) return;

		if (Input.GetKey(KeyCode.W)) {
			rb2d.AddForce(transform.up * acceleration_amount * Time.deltaTime);
		}
		if (Input.GetKey(KeyCode.S)) {
			rb2d.AddForce((-transform.up) * acceleration_amount * Time.deltaTime);
		}
		
		if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.LeftShift)) {
			rb2d.AddForce((-transform.right) * acceleration_amount * 0.6f  * Time.deltaTime);
		}
		if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.LeftShift)) {
			rb2d.AddForce((transform.right) * acceleration_amount * 0.6f  * Time.deltaTime);
		}
		
		if (Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.LeftShift)) {
			rb2d.AddTorque(-rotation_speed  * Time.deltaTime);
		}
		if (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.LeftShift)) {
			rb2d.AddTorque(rotation_speed  * Time.deltaTime);
		}	
		if (Input.GetKey(KeyCode.C)) {
			rb2d.angularVelocity = Mathf.Lerp(rb2d.angularVelocity, 0, rotation_speed * 0.06f * Time.deltaTime);
			rb2d.linearVelocity = Vector2.Lerp(rb2d.linearVelocity, Vector2.zero, acceleration_amount * 0.06f * Time.deltaTime);
		}	
		
		if (Input.GetKey(KeyCode.H)) {
			transform.position = new Vector3(0,0,0);
		}	
	}
}
