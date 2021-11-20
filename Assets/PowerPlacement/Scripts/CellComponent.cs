using System.Collections.Generic;
using UnityEngine;

public class CellComponent : MonoBehaviour {
	public KMSelectable Selectable;
	public List<CellBorderComponent> Borders = new List<CellBorderComponent>();

	private void Start() {
		Selectable.OnHighlight += () => {
			foreach (CellBorderComponent border in Borders) border.Active = true;
		};
		Selectable.OnHighlightEnded += () => {
			foreach (CellBorderComponent border in Borders) border.Active = false;
		};
	}
}
