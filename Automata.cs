using UnityEngine;
using UnityEngine.Tilemaps;

public struct Cell
{
	public int posx;
	public int posy;
	public int pre;
	public int val;
}

public class Automata : MonoBehaviour {

	public ComputeShader compute;


	Cell[] data;
	int size;
	ComputeBuffer buffer;

	RenderTexture texture;

	private void Awake() {
		data = new Cell[Screen.width * Screen.height];
		size = 4 * sizeof(int);
	}

	private void Start() {
		texture = new RenderTexture(Screen.width, Screen.height, 24);
		texture.enableRandomWrite = true;
		texture.useMipMap = false;
		texture.Create();

		buffer = new ComputeBuffer(data.Length, size);
		compute.SetBuffer(0, "cells", buffer);
		compute.SetInt("width", Screen.width);
		compute.SetInt("height", Screen.height);
		compute.Dispatch(0, data.Length/80, 1, 1);
		while(!buffer.IsValid()) {}
		buffer.GetData(data);
	}

	private void Update() {
		compute.SetBuffer(1, "cells", buffer);
		compute.SetInt("width", Screen.width);
		compute.SetInt("height", Screen.height);
		compute.Dispatch(1, data.Length/80, 1, 1);
		buffer.GetData(data);

		compute.SetBuffer(2, "cells", buffer);
		compute.SetInt("width", Screen.width);
		compute.SetInt("height", Screen.height);
		compute.Dispatch(2, data.Length/80, 1, 1);
		buffer.GetData(data);

		compute.SetTexture(3, "tex", texture);
		compute.SetBuffer(3, "cells", buffer);
		compute.SetInt("width", Screen.width);
		compute.SetInt("height", Screen.height);
		compute.Dispatch(3, texture.width/4, texture.height/4, 1);
		buffer.GetData(data);
	}

	private void OnRenderImage(RenderTexture src, RenderTexture dest) {
		Graphics.Blit(texture, dest);
	}

	private void OnDestroy() {
		buffer.Dispose();
	}
}