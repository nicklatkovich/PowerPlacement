using UnityEngine;

public class CellBorderComponent : MonoBehaviour {
	private bool _active;
	public bool Active { get { return _active; } set { if (_active == value) return; _active = value; UpdateActivity(); } }

	private void Start() {
		UpdateActivity();
	}

	private void UpdateActivity() {
		gameObject.SetActive(_active);
	}
}
