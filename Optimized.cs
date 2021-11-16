using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Optimized : MonoBehaviour
{
    public ComputeShader compute;
    public int WIDTH = 2550;
    public int HEIGHT = 1440;

    Vector2 camPos;
    Vector2 originPos;
    float camZoom = 0;
    ComputeBuffer buffer;
    ComputeBuffer sumBuffer;
    uint[] data;
    uint[] horizontalSum;
    RenderTexture texture;
    int cellWidth;
    int cellHeight;

    bool pause = true;
	bool random = true;

    int KernelInit;
    int KernelSum;
    int KernelProcess;
    int KernelUpdate;
    int KernelRender;

    int rule = Convert.ToInt32("000001000000001100", 2);

    void Awake()
    {
        KernelInit = compute.FindKernel("Init");
        KernelSum = compute.FindKernel("Sum");
        KernelProcess = compute.FindKernel("Process");
        KernelUpdate = compute.FindKernel("Update");
        KernelRender = compute.FindKernel("Render");
    }

    void Start()
    {
        camPos = new Vector2(WIDTH / 2, HEIGHT / 2);
        originPos = new Vector2(0, 0);

        cellWidth = WIDTH;
        cellHeight = HEIGHT / 16;
        data = new uint[cellWidth*cellHeight];
        buffer = new ComputeBuffer(data.Length, sizeof(int));
        horizontalSum = new uint[cellWidth*cellHeight];
        sumBuffer = new ComputeBuffer(horizontalSum.Length, sizeof(int));

        texture = new RenderTexture(Screen.width, Screen.height, 24);
		texture.enableRandomWrite = true;
		texture.useMipMap = false;
		texture.Create();

        Parse();

        compute.SetBuffer(KernelInit, "data", buffer);
        compute.SetBuffer(KernelSum, "data", buffer);
        compute.SetBuffer(KernelSum, "sum", sumBuffer);
        compute.SetBuffer(KernelProcess, "data", buffer);
        compute.SetBuffer(KernelProcess, "sum", sumBuffer);
        compute.SetBuffer(KernelUpdate, "data", buffer);
        compute.SetBuffer(KernelRender, "data", buffer);
        compute.SetTexture(KernelRender, "tex", texture);
        compute.SetInt("width", cellWidth);
        compute.SetInt("height", cellHeight);
		compute.SetInt("rule", rule);

        if(random) compute.Dispatch(KernelInit, cellWidth*cellHeight/100, 1, 1);
    }

    // Update is called once per frame
    void Update()
    {
        Viewport();
        if(Input.GetKeyUp(KeyCode.Space)) pause = !pause;
		if(!pause) Compute();
        Process();
    }

    private void Process()
	{
		if(Input.GetMouseButtonUp(0))
		{
			buffer.GetData(data);
			Vector2 pos = Input.mousePosition / camZoom;
			Vector2Int posi = new Vector2Int((int)pos.x + (int)originPos.x, (int)pos.y + (int)originPos.y);
			if(posi.x < 0 || posi.x > WIDTH || posi.y < 0 || posi.y > HEIGHT) return;
            int offset = posi.y % 16;
            int idx = (posi.y/16)*WIDTH+posi.x;
            data[idx] = data[idx] ^ (uint)(1 << offset);
			buffer.SetData(data);
			compute.Dispatch(KernelUpdate, cellWidth*cellHeight/100, 1, 1);
		}
	}

    private void Compute()
	{
		compute.Dispatch(KernelUpdate, cellWidth*cellHeight/100, 1, 1);
        compute.Dispatch(KernelSum, cellWidth*cellHeight/100, 1, 1);
        compute.Dispatch(KernelProcess, cellWidth*cellHeight/100, 1, 1);
        compute.Dispatch(KernelRender, Screen.width/4, Screen.height/4, 1);
	}

    private void OnRenderImage(RenderTexture src, RenderTexture dest) {

		compute.Dispatch(KernelRender, Screen.width/4, Screen.height/4, 1);

		Graphics.Blit(texture, dest);
	}

    private void Viewport()
	{
		camZoom += Input.GetAxis("Mouse ScrollWheel");
		camZoom = camZoom < 1 ? 1 : camZoom;
		compute.SetFloat("zoom", camZoom);
		camPos += new Vector2(Input.GetAxis("Horizontal") * 10 / camZoom, Input.GetAxis("Vertical") * 10 / camZoom);
		float xoffset = Screen.width / (2 * camZoom);
		float yoffset = Screen.height / (2 * camZoom);
		camPos.x = Mathf.Max(Mathf.Min(camPos.x, WIDTH - xoffset), xoffset);
		camPos.y = Mathf.Max(Mathf.Min(camPos.y, HEIGHT - yoffset), yoffset);
		originPos.x = camPos.x - xoffset;
		originPos.y = camPos.y - yoffset;
		compute.SetInt("posx", (int)originPos.x);
		compute.SetInt("posy", (int)originPos.y);
	}

    private void OnDestroy() {
        buffer.Dispose();
        sumBuffer.Dispose();
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

			Vector2Int startPoint = new Vector2Int((WIDTH - x) / 2, (HEIGHT - y) / 2);
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
                                    int offset = row % 16;
                                    int idx = (row/16)*WIDTH+ptr+startPoint.x;
									data[idx] ^= (uint)(1 << offset);
								}
								num = 0;
							}
							else
							{
								int offset = row % 16;
                                int idx = (row/16)*WIDTH+ptr+startPoint.x;
								data[idx] ^= (uint)(1 << offset);
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
