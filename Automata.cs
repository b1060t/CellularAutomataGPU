using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
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
	bool random = true;

	// Default rule: B3/S23
	int rule = Convert.ToInt32("000001000000001100", 2);

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

		Parse();

		compute.SetBuffer(0, "cells", buffer);
		compute.SetBuffer(1, "cells", buffer);
		compute.SetBuffer(2, "cells", buffer);
		compute.SetBuffer(3, "cells", buffer);
		compute.SetInt("scrWidth", Screen.width);
		compute.SetInt("scrHeight", Screen.height);
		compute.SetInt("rule", rule);
		compute.SetTexture(3, "tex", texture);

		if(random) compute.Dispatch(0, data.Length/320, 1, 1);
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
		camPos += new Vector2(Input.GetAxis("Horizontal") * 10 / camZoom, Input.GetAxis("Vertical") * 10 / camZoom);
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
			if(posi.x < 0 || posi.x > Screen.width || posi.y < 0 || posi.y > Screen.height) return;
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

	private void Parse()
	{
		int x = 0;
		int y = 0;
		try
		{
			random = false;
			string[] rle = File.ReadAllLines(Environment.CurrentDirectory + "\\test.rle");
			rle = rle.ToList().Where(s => s[0]!='#').ToArray();
			string[] configs = rle[0].Replace(" ", "").Split(',');
			StringBuilder sb = new StringBuilder();
			for(int i = 1; i < rle.Length; i++)
			{
				sb.Append(rle[i]);
			}
			string[] contents = sb.ToString().Split('$');

			foreach (string c in configs)
			{
				switch (c[0])
				{
					case 'x':
					{
						x = int.Parse(c.Remove(0, 2));
						break;
					}
					case 'y':
					{
						y = int.Parse(c.Remove(0, 2));
						break;
					}
					// Rule
					default:
					{
						string[] types = c.Remove(0, 5).Split('/');
						char[] b = "000000000".ToCharArray();
						char[] s = "000000000".ToCharArray();
						foreach (string t in types)
						{
							string tmp;
							if(t[0] == 'B')
							{
								tmp = t.Remove(0, 1);
								foreach(char r in tmp)
								{
									b[8-int.Parse(r.ToString())] = '1';
								}
							}
							else if(t[0] == 'S')
							{
								tmp = t.Remove(0, 1);
								foreach(char r in tmp)
								{
									s[8-int.Parse(r.ToString())] = '1';
								}
							}
						}
						rule = Convert.ToInt32(new string(b) + new string(s), 2);
						break;
					}	
				}
			}

			Vector2Int startPoint = new Vector2Int((Screen.width - x) / 2, (Screen.height - y) / 2);
			int row = startPoint.y + y;
			foreach (string s in contents)
			{
				int ptr = 0;
				int num = 0;
				foreach (char c in s)
				{
					switch (c)
					{
						case 'b':
							if(num != 0)
							{
								ptr += num;
								num = 0;
							}
							else
							{
								ptr++;
							}
							break;
						case 'o':
							if(num != 0)
							{
								int max = ptr + num;
								for(;ptr<max;ptr++)
								{
									data[row*Screen.width+ptr+startPoint.x].val = 1;
								}
								num = 0;
							}
							else
							{
								data[row*Screen.width+ptr+startPoint.x].val = 1;
								ptr++;
							}
							break;
						case '!':
						{
							break;
						}
						default:
						{
							int tmp = int.Parse(c.ToString());
							if(num != 0)
							{
								num = 10 * num + tmp;
							}
							else
							{
								num = tmp;
							}
							break;
						}
					}
				}
				if(num != 0)
				{
					row -= num;
				}
				else
				{
					row--;
				}
			}
			buffer.SetData(data);
		}
		catch (System.Exception)
		{
			random = true;
			return;
		}
	}
}