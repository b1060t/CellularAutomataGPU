using UnityEngine;
using UnityEngine.Tilemaps;

public struct Cell
{
	public int pre;
	public int val;
}

public class Automata : MonoBehaviour {

	public ComputeShader compute;

	public float camZoom = 1;
	public Vector2 camPos;
	Vector2 originPos;

	Cell[] data;
	int size;
	ComputeBuffer buffer;
	RenderTexture texture;
	bool pause = true;

	private void Awake() {
		data = new Cell[Screen.width * Screen.height];
		size = 2 * sizeof(int);
	}

	private void Start() {
		camPos = new Vector2(Screen.width / 2, Screen.height / 2);
		originPos = new Vector2(0, 0);

		texture = new RenderTexture(Screen.width, Screen.height, 24);
		texture.enableRandomWrite = true;
		texture.useMipMap = false;
		texture.Create();

		buffer = new ComputeBuffer(data.Length, size);
		compute.SetBuffer(0, "cells", buffer);
		compute.SetBuffer(1, "cells", buffer);
		compute.SetBuffer(2, "cells", buffer);
		compute.SetBuffer(3, "cells", buffer);
		compute.SetInt("scrWidth", Screen.width);
		compute.SetInt("scrHeight", Screen.height);
		compute.SetTexture(3, "tex", texture);

		compute.Dispatch(0, data.Length/320, 1, 1);
	}

	private void Update() {
		Viewport();
		if(Input.GetKeyUp(KeyCode.Space)) pause = !pause;
		if(!pause) Compute();
		Process();
	}

	private void Viewport()
	{
		camZoom += Input.GetAxis("Mouse ScrollWheel");
		camZoom = camZoom < 1 ? 1 : camZoom;
		compute.SetFloat("zoom", camZoom);
		camPos += new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		float xoffset = Screen.width / (2 * camZoom);
		float yoffset = Screen.height / (2 * camZoom);
		camPos.x = Mathf.Max(Mathf.Min(camPos.x, Screen.width - xoffset), xoffset);
		camPos.y = Mathf.Max(Mathf.Min(camPos.y, Screen.height - yoffset), yoffset);
		originPos.x = camPos.x - xoffset;
		originPos.y = camPos.y - yoffset;
		compute.SetInt("posx", (int)originPos.x);
		compute.SetInt("posy", (int)originPos.y);
	}

	private void Process()
	{
		if(Input.GetMouseButtonUp(0))
		{
			buffer.GetData(data);
			Vector2 pos = Input.mousePosition / camZoom;
			Vector2Int posi = new Vector2Int((int)pos.x + (int)originPos.x, (int)pos.y + (int)originPos.y);
			int idx = posi.y*Screen.width+posi.x;
			data[idx].val = data[idx].val == 0 ? 1 : 0;
			buffer.SetData(data);
			compute.Dispatch(2, data.Length/320, 1, 1);
		}
	}

	private void Compute()
	{
		compute.Dispatch(2, data.Length/320, 1, 1);
		compute.Dispatch(1, data.Length/320, 1, 1);
	}

	private void OnRenderImage(RenderTexture src, RenderTexture dest) {

		compute.Dispatch(3, texture.width/4, texture.height/4, 1);

		Graphics.Blit(texture, dest);
	}

	private void OnDestroy() {
		buffer.Dispose();
	}
}