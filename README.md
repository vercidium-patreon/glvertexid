This repository is Part 2 of Vercidium's [Free Friday](https://www.patreon.com/posts/100857028) series.

---

This is a standlone renderer that uses `gl_VertexID` to render a heightmap with minimal data.

This project uses Silk.NET so it *should* be cross-platform, but I've only tested it on Windows.

Key files:
- `Client.cs` creates the buffer and renders it
- `HeightmapShader.cs` applies the gl_VertexID magic

An explanation of this gl_VertexID magic can be found [here](https://www.youtube.com/watch?v=5zlfJW2VGLM).

---

Controls:
- WASD to move around
- QE to move up/down
- Space to view the wireframe

![image](https://github.com/vercidium-patreon/glvertexid/assets/12014138/322f4411-fa2b-4bf0-a740-dac7635b35f5)
