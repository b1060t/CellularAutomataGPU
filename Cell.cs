using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Diagnostics;
using UnityEngine.Profiling; 

public class Prototype : MonoBehaviour {

	public Tilemap map;
	public Camera cam;
	public ComputeShader compute;

	Tile[] arrTiles;
	Tile cellTile;

	private int resX;
	private int resY;

	Vector3Int boundMax;
	Vector3Int boundMin;
	Vector3Int bound;

	Cell[] data;
	Tile[] tiles;
	int size;
	ComputeBuffer buffer;

	private void Awake() {
		resX = Screen.width;
		resY = Screen.height;
		boundMax = ScreenToCell(new Vector3(resX, resY, 0));
		boundMin = ScreenToCell(new Vector3(0, 0, 0));
		boundMax.x += 1;
		bound = boundMax - boundMin;
		data = new Cell[bound.x * bound.y];
		size = 4 * sizeof(int);
	}
	private void Start() {
		cellTile = Resources.Load<Tile>("tile");
		Vector3 mapPos = map.transform.position;
		cam.transform.position = new Vector3(mapPos.x, mapPos.y, cam.transform.position.z);

		buffer = new ComputeBuffer(data.Length, size);
		compute.SetBuffer(0, "cells", buffer);
		compute.SetInt("width", bound.x);
		compute.SetInt("height", bound.y);
		compute.Dispatch(0, data.Length/20, 1, 1);
		while(!buffer.IsValid()) {}
		buffer.GetData(data);
		buffer.Dispose();

		tiles = new Tile[data.Length];
		int i = 0;
		foreach(Cell c in data)
		{
			Vector3Int pos = ScreenToCell(new Vector3(c.posx, c.posy, 0));
			tiles[i] = ScriptableObject.CreateInstance<Tile>();
			tiles[i].sprite = cellTile.sprite;
			tiles[i++].color = c.pre == 1 ? Color.black : Color.white;
		}
		map.SetTiles(data.ToList<Cell>().Select(c => new Vector3Int(boundMin.x + c.posx, boundMin.y + c.posy, 0)).ToArray<Vector3Int>(), tiles);
	}

	private void Update() {
		if(resX != Screen.width || resY != Screen.height)
		{
			resX = Screen.width;
			resY = Screen.height;
			boundMax = ScreenToCell(new Vector3(resX, resY, 0));
			boundMin = ScreenToCell(new Vector3(0, 0, 0));
		}
		
		buffer = new ComputeBuffer(data.Length, size);
		compute.SetBuffer(1, "cells", buffer);
		compute.SetInt("width", bound.x);
		compute.SetInt("height", bound.y);
		compute.Dispatch(1, data.Length/20, 1, 1);
		while(!buffer.IsValid()) {}
		buffer.GetData(data);
		buffer.Dispose();

		RenderCells();
		
		buffer = new ComputeBuffer(data.Length, size);
		compute.SetBuffer(2, "cells", buffer);
		compute.SetInt("width", bound.x);
		compute.SetInt("height", bound.y);
		compute.Dispatch(2, data.Length/20, 1, 1);
		while(!buffer.IsValid()) {}
		buffer.GetData(data);
		buffer.Dispose();
	}

	private Vector3Int ScreenToCell(Vector3 scrPos)
	{
		return map.WorldToCell(cam.ScreenToWorldPoint(scrPos));
	}

	void RenderCells()
	{
		int i = 0;
		
		foreach(Cell c in data)
		{
			if(c.val != c.pre)
			{
				Vector3Int coord = new Vector3Int(boundMin.x + c.posx, boundMin.y + c.posy, 0);
				tiles[i].color = c.val == 1 ? Color.black : Color.white;
				map.SetTile(coord, tiles[i]);
			}
			i++;
		}
		map.RefreshAllTiles();
	}
}