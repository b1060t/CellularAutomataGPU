## Cellular Automata based on GPU

**`.rle` file supported**

TODO: Scale larger than `65535*1024` can't be supported due to Unity compute shader's constraints and no way to get the reference of subarray in C# without using pointer in `unsafe` mode. Larger scale may be implemented via C++ and OpenGL.

**Controll:**

`Space`: Pause/Start

`wasd`: Move

`MouseWheel`: Scale

`Click`: Add/Remove

**2K 500FPS GTX1080:**
![](run.png)

**4K 450FPS GTX1080**

**Turing machine:**
![](turing.png)