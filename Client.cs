using Silk.NET.Input;
using Silk.NET.Windowing;

namespace glvertexid;

public unsafe partial class Client
{
    public Client()
    {
        // Create a Silk.NET window
        var options = WindowOptions.Default;
        options.API = new GraphicsAPI(ContextAPI.OpenGL, new APIVersion(3, 3));
        options.Position = new(200, 200);
        options.PreferredDepthBufferBits = 32;
        options.Title = "gl_VertexID";

        window = Window.Create(options);

        // Callback when the window is created
        window.Load += () =>
        {
            // Create an OpenGL Context
            Gl = window.CreateOpenGL();
            SilkOnDidCreateOpenGLContext();


            // Precalculate input stuff
            inputContext = window.CreateInput();
            keyboard = inputContext.Keyboards[0];
            mouse = inputContext.Mice[0];
            mouse.DoubleClickTime = 1;

            keyboard.KeyDown += SilkOnKeyDown;
        };

        window.Render += (_) => Render();

        window.Size = new(1920, 1080);
        window.FramesPerSecond = 144;
        window.UpdatesPerSecond = 144;
        window.VSync = false;
        window.FocusChanged += SilkOnFocusChanged;

        // Initialise OpenGL and input context
        window.Initialize();
    }

    public void Run()
    {
        // Run forever
        window.Run();
    }

    void SilkOnDidCreateOpenGLContext()
    {
        var major = Gl.GetInteger(GetPName.MajorVersion);
        var minor = Gl.GetInteger(GetPName.MinorVersion);

        var version = major * 10 + minor;
        Console.WriteLine($"OpenGL Version: {version}");


        buffer = new();
        var bytes_vertexData = Marshal.SizeOf<HeightmapVertex>() * Constants.VERTICES_PER_CHUNK;
        var vertexData = (HeightmapVertex*)Allocator.Alloc(bytes_vertexData);

        GenerateBuffer(vertexData);
        buffer.BufferData(Constants.VERTICES_PER_CHUNK, vertexData);
        Allocator.Free(ref vertexData, ref bytes_vertexData);


#if DEBUG
        // Set up the OpenGL debug message callback (NVIDIA only)
        debugDelegate = DebugCallback;

        Gl.Enable(EnableCap.DebugOutput);
        Gl.Enable(EnableCap.DebugOutputSynchronous);
        Gl.DebugMessageCallback(debugDelegate, null);
#endif
    }

    void SilkOnFocusChanged(bool focused)
    {
        if (!focused)
            captureMouse = false;
        else
            captureMouse = true;

        lastMouse = mouse.Position;
    }


    // Heightmap helpers
    float GetHeight(int x, int z) => MathF.Sin(x * 0.5f) + MathF.Cos(z * 0.25f) * 2;

    void GenerateBuffer(HeightmapVertex* write)
    {
        // Generate 32 triangle strips
        for (int z = 0; z < Constants.HEIGHTMAP_SIZE; z++)
        {
            int x = 0;

            var altitude0 = GetHeight(x, z);
            var altitude1 = GetHeight(x, z + 1);
            var altitude2 = GetHeight(x + 1, z);


            // First vertex is a degenerate
            write++->Reset(altitude0); 


            // Create the first triangle
            write++->Reset(altitude0);
            write++->Reset(altitude1);
            write++->Reset(altitude2);

            // Rest of the strip
            x += 1;
            var altitude = GetHeight(x, z + 1);
            write++->Reset(altitude);

            x += 1;
            for (; x <= Constants.HEIGHTMAP_SIZE; x++)
            {
                altitude = GetHeight(x, z);
                write++->Reset(altitude);

                altitude = GetHeight(x, z + 1);
                write++->Reset(altitude);
            }


            // Degenerate
            altitude = GetHeight(x - 1, z + 1);
            write++->Reset(altitude);
        }
    }


    // Rendering
    void Render()
    {
        UpdateCamera();


        // Prepare OpenGL
        PreRenderSetup();


        // Prepare the shader
        HeightmapShader.UseProgram();
        HeightmapShader.mvp.Set(GetViewProjection());
        HeightmapShader.showWireframe.Set(keyboard.IsKeyPressed(Key.Space));

        Gl.FrontFace(FrontFaceDirection.Ccw);
        buffer.primitiveType = PrimitiveType.TriangleStrip;
        buffer.BindAndDraw();
    }

    void PreRenderSetup()
    {
        // Prepare rendering
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        Gl.Enable(EnableCap.DepthTest);
        Gl.Disable(EnableCap.Blend);
        Gl.Disable(EnableCap.StencilTest);
        Gl.Enable(EnableCap.CullFace);
        Gl.FrontFace(FrontFaceDirection.CW);


        // Clear everything
        Gl.ClearDepth(1.0f);
        Gl.DepthFunc(DepthFunction.Less);

        Gl.ColorMask(true, true, true, true);
        Gl.DepthMask(true);

        Gl.ClearColor(0, 0, 0, 0);
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);


        // Set the viewport to the window size
        Gl.Viewport(0, 0, (uint)window.Size.X, (uint)window.Size.Y);
    }

    Matrix4x4 GetViewProjection()
    {
        var view = Helper.CreateFPSView(cameraPos, cameraPitch, cameraYaw);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, Aspect, NearPlane, FarPlane);

        return view * proj;
    }


    // Input
    void SilkOnKeyDown(IKeyboard keyboard, Key key, int something)
    {
        if (key == Key.Escape)
        {
            captureMouse = !captureMouse;

            // Don't snip the camera when capturing the mouse
            lastMouse = mouse.Position;
        }
    }

    void UpdateCamera()
    {
        if (firstRender)
        {
            cameraPos = new Vector3(0, 18, 0) - Helper.FromPitchYaw(cameraPitch, cameraYaw) * 24;
            lastMouse = mouse.Position;
            firstRender = false;
        }


        // Mouse movement
        if (captureMouse)
        {
            var diff = lastMouse - mouse.Position;

            cameraYaw -= diff.X * 0.003f;
            cameraPitch += diff.Y * 0.003f;

            mouse.Position = new Vector2(window.Size.X / 2, window.Size.Y / 2);
            lastMouse = mouse.Position;
            mouse.Cursor.CursorMode = CursorMode.Hidden;
        }
        else
            mouse.Cursor.CursorMode = CursorMode.Normal;


        // Fly camera movement
        float movementSpeed = 0.15f;

        if (keyboard.IsKeyPressed(Key.W))
            cameraPos += Helper.FromPitchYaw(cameraPitch, cameraYaw) * movementSpeed;
        else if (keyboard.IsKeyPressed(Key.S))
            cameraPos -= Helper.FromPitchYaw(cameraPitch, cameraYaw) * movementSpeed;

        if (keyboard.IsKeyPressed(Key.A))
            cameraPos += Helper.FromPitchYaw(0, cameraYaw - MathF.PI / 2) * movementSpeed;
        else if (keyboard.IsKeyPressed(Key.D))
            cameraPos += Helper.FromPitchYaw(0, cameraYaw + MathF.PI / 2) * movementSpeed;

        if (keyboard.IsKeyPressed(Key.E))
            cameraPos += Helper.FromPitchYaw(MathF.PI / 2, 0) * movementSpeed;
        else if (keyboard.IsKeyPressed(Key.Q))
            cameraPos += Helper.FromPitchYaw(-MathF.PI / 2, 0) * movementSpeed;
    }


#if DEBUG
    // Debug OpenGL callbacks (Works on NVIDIA, not sure about AMD/Intel)
    DebugProc debugDelegate;

    unsafe void DebugCallback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint messageInt, nint userParam)
    {
        // TODO
        var message = Marshal.PtrToStringAnsi(messageInt);

        if (message == "Pixel-path performance warning: Pixel transfer is synchronized with 3D rendering.")
        {
            // TODO: Use proper id for this
            return;
        }

        // Skip our own notifications
        if (severity == GLEnum.DebugSeverityNotification)
            return;

        // Buffer detailed info
        if (id == 131185)
            return;

        // "Program/shader state performance warning: Vertex shader in program 69 is being recompiled based on GL state."
        if (id == 131218)
            return;

        // "Buffer performance warning: Buffer object 15 (bound to NONE, usage hint is GL_DYNAMIC_DRAW) is being copied/moved from VIDEO memory to HOST memory."
        if (id == 131186)
            return;

        AssertFalse();
        Console.WriteLine(message);
    }
#endif



    // Silk
    IWindow window;
    IMouse mouse;
    IKeyboard keyboard;
    IInputContext inputContext;


    // Camera
    Vector2 lastMouse;
    Vector3 cameraPos;
    float cameraPitch = -MathF.PI / 4;
    float cameraYaw = MathF.PI / 4;

    bool captureMouse = true;


    // Rendering
    bool firstRender = true;

    float FieldOfView = 50.0f / 180.0f * MathF.PI;
    float Aspect => window.Size.X / (float)window.Size.Y;
    float NearPlane = 1.0f;
    float FarPlane = 256.0f;

    HeightmapVertexBuffer buffer;
}