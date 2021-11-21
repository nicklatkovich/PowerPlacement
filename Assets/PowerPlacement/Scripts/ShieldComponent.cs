using UnityEngine;

public class ShieldComponent : MonoBehaviour {
	public Renderer Renderer;

	private Color _color = Color.white;
	public Color Color { get { return _color; } set { if (_color == value) return; _color = value; UpdateColor(); } }

	private void Start() {
		UpdateColor();
	}

	private void UpdateColor() {
		Renderer.material.color = Color;
	}
}
