namespace glvertexid;

public static class Constants
{
    public const int HEIGHTMAP_SIZE = 32;
    public const int HEIGHTMAP_SIZE_SQUARED = HEIGHTMAP_SIZE * HEIGHTMAP_SIZE;

    public const int VERTICES_PER_RUN = HEIGHTMAP_SIZE * 2 + 4;
    public const int VERTICES_PER_CHUNK = VERTICES_PER_RUN * HEIGHTMAP_SIZE;
    public const int VERTICES_PER_RUN_NOT_DEGENERATE = VERTICES_PER_RUN - 3;
}