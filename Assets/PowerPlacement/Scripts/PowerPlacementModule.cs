using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KeepCoding;

public class PowerPlacementModule : ModuleScript {
	public const float CELLS_INTERVAL = 0.018f;
	public const float OBJECTS_HEIGHT = 0.002f;
	public const float CONNECTION_WIDTH = 0.001f;
	public const float CONNECTION_HEIGHT = 0.0015f;

	public GameObject GridContainer;
	public KMSelectable Selectable;
	public KMSelectable ResetButton;
	public CellComponent CellPrefab;
	public CellBorderComponent CellBorderPrefab;
	public ReceiverComponent ReceiverPrefab;
	public EnergyCellComponent EnergyCellPrefab;
	public ShieldComponent ShieldPrefab;
	public ConnectionComponent ConnectionPrefab;

	private CellComponent[][] _cellGrid;
	private PowerPlacementPuzzle _puzzle;
	private object[][] _objects;

	private void Start() {
		_puzzle = new PowerPlacementPuzzle();
		_cellGrid = new CellComponent[PowerPlacementPuzzle.SIZE][];
		InitGrid();
		for (int x = -1; x < PowerPlacementPuzzle.SIZE; x++) {
			for (int z = -1; z < PowerPlacementPuzzle.SIZE; z++) {
				if (x >= 0) {
					CellBorderComponent border = CreateCellBorder(x, z + 0.5f, 90);
					if (z >= 0) _cellGrid[x][z].Borders.Add(border);
					if (z + 1 < PowerPlacementPuzzle.SIZE) _cellGrid[x][z + 1].Borders.Add(border);
				}
				if (z >= 0) {
					CellBorderComponent border = CreateCellBorder(x + 0.5f, z, 0);
					if (x >= 0) _cellGrid[x][z].Borders.Add(border);
					if (x + 1 < PowerPlacementPuzzle.SIZE) _cellGrid[x + 1][z].Borders.Add(border);
				}
			}
		}
	}

	private void InitGrid() {
		List<KMSelectable> selectables = new List<KMSelectable>();
		_objects = new object[PowerPlacementPuzzle.SIZE][];
		for (int x = 0; x < PowerPlacementPuzzle.SIZE; x++) {
			_objects[x] = new object[PowerPlacementPuzzle.SIZE];
			_cellGrid[x] = new CellComponent[PowerPlacementPuzzle.SIZE];
			for (int z = 0; z < PowerPlacementPuzzle.SIZE; z++) {
				CellComponent cell = Instantiate(CellPrefab);
				cell.transform.parent = GridContainer.transform;
				cell.transform.localPosition = new Vector3(x * CELLS_INTERVAL, 0, -z * CELLS_INTERVAL);
				cell.transform.localScale = Vector3.one;
				cell.transform.localRotation = Quaternion.identity;
				if (_puzzle.Solution[x][z].Type != PowerPlacementPuzzle.CellType.RECEIVER) {
					cell.Selectable.Parent = Selectable;
					Vector2Int pos = new Vector2Int(x, z);
					cell.Selectable.OnInteract += () => { OnCellPressed(pos); return false; };
					selectables.Add(cell.Selectable);
				} else {
					ReceiverComponent receiver = Instantiate(ReceiverPrefab);
					receiver.transform.parent = GridContainer.transform;
					receiver.transform.localPosition = new Vector3(x * CELLS_INTERVAL, OBJECTS_HEIGHT, -z * CELLS_INTERVAL);
					receiver.transform.localScale = Vector3.one;
					receiver.transform.localRotation = Quaternion.identity;
					receiver.Center.material.color = Color.white;
					if (_puzzle.Solution[x][z].Flags == 0) {
						receiver.Outline.material.color = Color.green;
					}
					for (int d = 0; d < 4; d++) {
						if ((_puzzle.Solution[x][z].Flags & (1 << d)) > 0) continue;
						receiver.Sides[d].gameObject.SetActive(false);
					}
					_objects[x][z] = receiver;
				}
				_cellGrid[x][z] = cell;
			}
		}
		selectables.Add(ResetButton);
		Selectable.Children = selectables.ToArray();
		Selectable.UpdateChildren();
	}

	public override void OnActivate() {
		base.OnActivate();
		ResetButton.OnInteract += () => { OnResetPressed(); return false; };
	}

	private CellBorderComponent CreateCellBorder(float x, float z, float angle) {
		CellBorderComponent result = Instantiate(CellBorderPrefab);
		result.transform.parent = GridContainer.transform;
		result.transform.localPosition = new Vector3(x * CELLS_INTERVAL, 0, -z * CELLS_INTERVAL);
		result.transform.localScale = Vector3.one;
		result.transform.localRotation = Quaternion.Euler(0, angle, 0);
		return result;
	}

	private void OnCellPressed(Vector2Int pos) {
		PowerPlacementPuzzle.Cell cell = _puzzle.Grid[pos.x][pos.y];
		if (cell.Type == PowerPlacementPuzzle.CellType.EMPTY) {
			cell.Type = PowerPlacementPuzzle.CellType.ENERGY;
			EnergyCellComponent obj = Instantiate(EnergyCellPrefab);
			obj.transform.parent = GridContainer.transform;
			obj.transform.localPosition = new Vector3(pos.x * CELLS_INTERVAL, OBJECTS_HEIGHT, -pos.y * CELLS_INTERVAL);
			obj.transform.localScale = Vector3.one;
			obj.transform.localRotation = Quaternion.identity;
			_objects[pos.x][pos.y] = obj;
			UpdateColorsOnCross(pos);
			OnObjectPlaced(pos);
			for (int d = 0; d < 4; d++) {
				PowerPlacementPuzzle.Cell objCell = _puzzle.GridRaycast(pos, d);
				if (objCell == null || objCell.Type != PowerPlacementPuzzle.CellType.RECEIVER) continue;
				obj.Connections[d] = CreateConnection(pos, objCell.Position, (objCell.Flags & (1 << ((d + 2) % 4))) > 0 ? Color.green : Color.red);
			}
		} else if (cell.Type == PowerPlacementPuzzle.CellType.ENERGY) {
			cell.Type = PowerPlacementPuzzle.CellType.SHIELD;
			EnergyCellComponent energyCell = _objects[pos.x][pos.y] as EnergyCellComponent;
			foreach (ConnectionComponent connection in energyCell.Connections.Where(c => c != null)) Destroy(connection.gameObject);
			Destroy(energyCell.gameObject);
			ShieldComponent obj = Instantiate(ShieldPrefab);
			obj.transform.parent = GridContainer.transform;
			obj.transform.localPosition = new Vector3(pos.x * CELLS_INTERVAL, OBJECTS_HEIGHT, -pos.y * CELLS_INTERVAL);
			obj.transform.localScale = Vector3.one;
			obj.transform.localRotation = Quaternion.identity;
			_objects[pos.x][pos.y] = obj;
			UpdateColorsOnCross(pos);
			OnObjectPlaced(pos);
		} else if (cell.Type == PowerPlacementPuzzle.CellType.SHIELD) OnShieldPressed(pos, cell);
	}

	private void OnResetPressed() {
		if (IsSolved) return;
		List<Vector2Int> cellsToUpdateColor = new List<Vector2Int>();
		for (int x = 0; x < PowerPlacementPuzzle.SIZE; x++) {
			for (int y = 0; y < PowerPlacementPuzzle.SIZE; y++) {
				PowerPlacementPuzzle.Cell cell = _puzzle.Grid[x][y];
				if (cell.Type == PowerPlacementPuzzle.CellType.EMPTY) continue;
				if (cell.Type == PowerPlacementPuzzle.CellType.RECEIVER) {
					cellsToUpdateColor.Add(new Vector2Int(x, y));
					continue;
				}
				if (cell.Type == PowerPlacementPuzzle.CellType.ENERGY) {
					EnergyCellComponent obj = _objects[x][y] as EnergyCellComponent;
					foreach (ConnectionComponent connection in obj.Connections.Where(conn => conn != null)) Destroy(connection.gameObject);
					Destroy(obj.gameObject);
				} else Destroy((_objects[x][y] as ShieldComponent).gameObject);
				_objects[x][y] = null;
				_puzzle.Grid[x][y].Type = PowerPlacementPuzzle.CellType.EMPTY;
			}
		}
		foreach (Vector2Int pos in cellsToUpdateColor) UpdateColorOnCell(pos);
	}

	private void OnShieldPressed(Vector2Int pos, PowerPlacementPuzzle.Cell cell) {
		cell.Type = PowerPlacementPuzzle.CellType.EMPTY;
		Destroy((_objects[pos.x][pos.y] as ShieldComponent).gameObject);
		_objects[pos.x][pos.y] = null;
		UpdateColorsOnCross(pos);
		PowerPlacementPuzzle.Cell[] raycasts = _puzzle.MultiGridRaycast(pos);
		for (int d = 0; d < 4; d++) {
			if (raycasts[d] == null || raycasts[d].Type != PowerPlacementPuzzle.CellType.ENERGY) continue;
			int aD = (d + 2) % 4;
			PowerPlacementPuzzle.Cell aObjCell = raycasts[aD];
			if (aObjCell == null || aObjCell.Type != PowerPlacementPuzzle.CellType.RECEIVER) continue;
			EnergyCellComponent ecObj = _objects[raycasts[d].Position.x][raycasts[d].Position.y] as EnergyCellComponent;
			ecObj.Connections[aD] = CreateConnection(raycasts[d].Position, aObjCell.Position, (aObjCell.Flags & (1 << d)) > 0 ? Color.green : Color.red);
		}
	}

	private void OnObjectPlaced(Vector2Int pos) {
		for (int d = 0; d < 4; d++) {
			PowerPlacementPuzzle.Cell rcCell = _puzzle.GridRaycast(pos, d);
			if (rcCell == null || rcCell.Type != PowerPlacementPuzzle.CellType.ENERGY) continue;
			EnergyCellComponent ecObj = _objects[rcCell.Position.x][rcCell.Position.y] as EnergyCellComponent;
			ConnectionComponent conn = ecObj.Connections[(d + 2) % 4];
			if (conn != null) Destroy(conn.gameObject);
		}
	}

	private void UpdateColorsOnCross(Vector2Int crossPivot) {
		UpdateColorOnCell(crossPivot);
		foreach (Vector2Int dd in PowerPlacementPuzzle.DD) {
			Vector2Int p = crossPivot + dd;
			while (PowerPlacementPuzzle.IsOnGrid(p)) {
				UpdateColorOnCell(p);
				p += dd;
			}
		}
	}

	private void UpdateColorOnCell(Vector2Int pos) {
		PowerPlacementPuzzle.Cell cell = _puzzle.Grid[pos.x][pos.y];
		if (cell.Type == PowerPlacementPuzzle.CellType.EMPTY) return;
		if (cell.Type == PowerPlacementPuzzle.CellType.ENERGY) SetEnergyCellColor(pos);
		else if (cell.Type == PowerPlacementPuzzle.CellType.SHIELD) SetShieldCellColor(pos);
		else if (cell.Type == PowerPlacementPuzzle.CellType.RECEIVER) SetReceiverColor(pos);
	}

	private Color SetReceiverColor(Vector2Int pos) {
		PowerPlacementPuzzle.Cell cell = _puzzle.Grid[pos.x][pos.y];
		ReceiverComponent obj = _objects[pos.x][pos.y] as ReceiverComponent;
		bool hasActiveSides = false;
		bool isRed = false;
		bool activeReceving = true;
		for (int d = 0; d < 4; d++) {
			PowerPlacementPuzzle.Cell raycast = _puzzle.GridRaycast(pos, d);
			if ((cell.Flags & (1 << d)) == 0) {
				if (raycast != null && raycast.Type == PowerPlacementPuzzle.CellType.ENERGY) isRed = true;
				continue;
			}
			hasActiveSides = true;
			if (raycast != null && raycast.Type == PowerPlacementPuzzle.CellType.ENERGY) obj.Sides[d].material.color = Color.green;
			else {
				activeReceving = false;
				obj.Sides[d].material.color = Color.white;
			}
		}
		List<PowerPlacementPuzzle.Cell>[] raycasts = _puzzle.GetGridCrossLines(pos);
		int[] counts = raycasts.SelectMany(line => new[] { PowerPlacementPuzzle.CellType.ENERGY, PowerPlacementPuzzle.CellType.SHIELD }.Select(type => (
			line.Count(c => c.Type == type)
		))).ToArray();
		if (counts.Any(i => i > 1)) obj.Center.material.color = Color.red;
		else obj.Center.material.color = counts.All(i => i == 1) ? Color.green : Color.white;
		if (isRed) return obj.Outline.material.color = Color.red;
		if (activeReceving) return obj.Outline.material.color = Color.green;
		return obj.Outline.material.color = hasActiveSides ? Color.white : Color.green;
	}

	private Color SetShieldCellColor(Vector2Int pos) {
		ShieldComponent obj = _objects[pos.x][pos.y] as ShieldComponent;
		List<PowerPlacementPuzzle.Cell>[] lines = _puzzle.GetGridCrossLines(pos);
		if (lines.Any(line => line.Count(cell => cell.Type == PowerPlacementPuzzle.CellType.SHIELD) > 1)) return obj.Color = Color.red;
		PowerPlacementPuzzle.Cell[] raycasts = _puzzle.MultiGridRaycast(pos);
		bool successfulBlocking = false;
		for (int d = 0; d < 4; d++) {
			PowerPlacementPuzzle.Cell cell = raycasts[d];
			if (cell == null || cell.Type != PowerPlacementPuzzle.CellType.ENERGY) continue;
			PowerPlacementPuzzle.Cell aCell = raycasts[(d + 2) % 4];
			if (aCell == null || aCell.Type != PowerPlacementPuzzle.CellType.RECEIVER) continue;
			if ((aCell.Flags & (1 << d)) > 0) return obj.Color = Color.red;
			successfulBlocking = true;
		}
		if (successfulBlocking) return obj.Color = Color.green;
		return obj.Color = lines.All(line => line.Count(cell => cell.Type == PowerPlacementPuzzle.CellType.ENERGY) == 1) ? Color.green : Color.white;
	}

	private Color SetEnergyCellColor(Vector2Int pos) {
		EnergyCellComponent obj = _objects[pos.x][pos.y] as EnergyCellComponent;
		List<PowerPlacementPuzzle.Cell>[] lines = _puzzle.GetGridCrossLines(pos);
		if (lines.Any(line => line.Count(cell => cell.Type == PowerPlacementPuzzle.CellType.ENERGY) > 1)) return obj.Color = Color.red;
		PowerPlacementPuzzle.Cell[] raycasts = _puzzle.MultiGridRaycast(pos);
		for (int d = 0; d < 4; d++) {
			PowerPlacementPuzzle.Cell cell = raycasts[d];
			if (cell == null || cell.Type != PowerPlacementPuzzle.CellType.RECEIVER) continue;
			if ((cell.Flags & (1 << ((d + 2) % 4))) == 0) return obj.Color = Color.red;
		}
		if (raycasts.Any(cell => cell != null && cell.Type == PowerPlacementPuzzle.CellType.RECEIVER)) return obj.Color = Color.green;
		return obj.Color = lines.All(line => line.Count(cell => cell.Type == PowerPlacementPuzzle.CellType.SHIELD) == 1) ? Color.green : Color.white;
	}

	private ConnectionComponent CreateConnection(Vector2Int from, Vector2Int to, Color color) {
		ConnectionComponent result = Instantiate(ConnectionPrefab);
		result.transform.parent = GridContainer.transform;
		result.transform.localPosition = CELLS_INTERVAL * new Vector3((from.x + to.x) / 2f, 0, -(from.y + to.y) / 2f);
		Vector2 scale = CELLS_INTERVAL * new Vector2(Mathf.Abs(from.x - to.x), Mathf.Abs(from.y - to.y)) + CONNECTION_WIDTH * Vector2.one;
		result.transform.localScale = new Vector3(scale.x, CONNECTION_HEIGHT, scale.y);
		result.transform.localRotation = Quaternion.identity;
		result.Color = color;
		return result;
	}
}
