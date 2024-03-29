namespace glvertexid;

public struct HeightmapVertex
{
    public void Reset(float altitude)
    {
        this.altitude = altitude;
    }

    public float altitude;
}

public unsafe class HeightmapVertexBuffer : VertexBuffer<HeightmapVertex>
{
    public HeightmapVertexBuffer() : base(4) { }

    protected override void SetupVAO()
    {
        Gl.EnableVertexAttribArray(0);

        Gl.VertexAttribPointer(0, 1, VertexAttribPointerType.Float, false, vertexSize, null);
    }
}