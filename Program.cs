using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenGLApp511;

public class Game : GameWindow
{
    // ---- Аналоги глобальных переменных ----
    private const int NumVAOs = 1;
    private const int NumVBOs = 2;

    private float cameraX, cameraY, cameraZ;
    private float pyrLocX, pyrLocY, pyrLocZ;

    private int renderingProgram;
    private readonly int[] vao = new int[NumVAOs];
    private readonly int[] vbo = new int[NumVBOs];

    private int projLoc, mvLoc, tfLoc;   // tfLoc в C++ объявлен, но не используется
    private int brickTexture;

    private int width, height;
    private float aspect;

    private float rotX = 0.0f, rotY = 0.0f, rotZ = 0.0f;

    private Matrix4 pMat, vMat, mMat, mvMat;

    private readonly float[] pyramidPositions =
    {
        -1.0f, -1.0f,  1.0f,   1.0f, -1.0f,  1.0f,   0.0f,  1.0f,  0.0f,
         1.0f, -1.0f,  1.0f,   1.0f, -1.0f, -1.0f,   0.0f,  1.0f,  0.0f,
         1.0f, -1.0f, -1.0f,  -1.0f, -1.0f, -1.0f,   0.0f,  1.0f,  0.0f,
        -1.0f, -1.0f, -1.0f,  -1.0f, -1.0f,  1.0f,   0.0f,  1.0f,  0.0f,
        -1.0f, -1.0f, -1.0f,   1.0f, -1.0f,  1.0f,  -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f,  1.0f,  -1.0f, -1.0f, -1.0f,   1.0f, -1.0f, -1.0f
    };

    private readonly float[] pyTexCoords =
    {
        0.0f, 0.0f,  1.0f, 0.0f,  0.5f, 1.0f,
        0.0f, 0.0f,  1.0f, 0.0f,  0.5f, 1.0f,
        0.0f, 0.0f,  1.0f, 0.0f,  0.5f, 1.0f,
        0.0f, 0.0f,  1.0f, 0.0f,  0.5f, 1.0f,
        0.0f, 0.0f,  1.0f, 0.0f,  0.5f, 1.0f,
        0.0f, 0.0f,  1.0f, 0.0f,  0.5f, 1.0f
    };

    public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

    // ---- setupVertices() ----
    private void SetupVertices()
    {
        GL.GenVertexArrays(NumVAOs, vao);
        GL.BindVertexArray(vao[0]);

        GL.GenBuffers(NumVBOs, vbo);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo[0]);
        GL.BufferData(BufferTarget.ArrayBuffer,
                      pyramidPositions.Length * sizeof(float),
                      pyramidPositions,
                      BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo[1]);
        GL.BufferData(BufferTarget.ArrayBuffer,
                      pyTexCoords.Length * sizeof(float),
                      pyTexCoords,
                      BufferUsageHint.StaticDraw);
    }

    // ---- init(GLFWwindow*) ----
    protected override void OnLoad()
    {
        base.OnLoad();

        renderingProgram = Utils.CreateShaderProgram(
            "./shaders/vertex_shader51.glsl",
            "./shaders/fragment_shader51.glsl");

        if (Utils.CheckOpenGLError())
            Console.WriteLine("ERROR: Could not create the shader program");

        width = Size.X;
        height = Size.Y;
        aspect = width / (float)height;
        pMat = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(60.0f), aspect, 0.1f, 50.0f);

        cameraX = 0.0f; cameraY = 0.0f; cameraZ = 6.0f;
        pyrLocX = 0.0f; pyrLocY = 0.0f; pyrLocZ = 2.0f;

        brickTexture = Utils.LoadTexture("textures/brick.png");

        SetupVertices();

        // ---- Привязка sampler2D "brickTex" -> texture unit 0 ----
        GL.UseProgram(renderingProgram);
        int texLoc = GL.GetUniformLocation(renderingProgram, "brickTex");
        GL.Uniform1(texLoc, 0);
        GL.UseProgram(0);

        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
    }

    // ---- display(GLFWwindow*, double) ----
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.DepthBufferBit);
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        GL.UseProgram(renderingProgram);

        projLoc = GL.GetUniformLocation(renderingProgram, "proj_matrix");
        mvLoc = GL.GetUniformLocation(renderingProgram, "mv_matrix");

        vMat = Matrix4.CreateTranslation(-cameraX, -cameraY, -cameraZ);
        mMat = Matrix4.CreateTranslation(pyrLocX, pyrLocY, pyrLocZ);

        // В C++ порядок: сначала translate, затем повороты с накоплением.
        // mMat = rotate(mMat, ...) эквивалентно mMat = R * mMat (пост-умножение
        // слева). В OpenTK так же: R * mMat.
        mMat = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotX)) * mMat;
        mMat = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotY)) * mMat;
        mMat = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotZ)) * mMat;

        mvMat = vMat * mMat;

        GL.UniformMatrix4(projLoc, false, ref pMat);
        GL.UniformMatrix4(mvLoc, false, ref mvMat);

        // Атрибут 0 — позиции
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo[0]);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
        GL.EnableVertexAttribArray(0);

        // Атрибут 1 — текстурные координаты
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo[1]);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
        GL.EnableVertexAttribArray(1);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, brickTexture);

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 18);

        SwapBuffers();
    }

    // ---- key_callback ----
    // В C++ используются GLFW_PRESS + GLFW_REPEAT (событийно).
    // Здесь — IsKeyDown в OnUpdateFrame, что даёт вращение каждый кадр
    // (визуально плавнее, чем системный автоповтор).
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        var k = KeyboardState;

        // Умножаем на время кадра (сек), чтобы скорость вращения
        // не зависела от частоты кадров и не дёргалась.
        const float speed = 90.0f; // градусов в секунду
        float step = speed * (float)args.Time;

        if (k.IsKeyDown(Keys.A)) rotY += step;
        if (k.IsKeyDown(Keys.D)) rotY -= step;
        if (k.IsKeyDown(Keys.W)) rotX += step;
        if (k.IsKeyDown(Keys.S)) rotX -= step;
        if (k.IsKeyDown(Keys.Q)) rotZ -= step;
        if (k.IsKeyDown(Keys.E)) rotZ += step;

        if (k.IsKeyDown(Keys.Escape))
            Close();
    }
}

public static class Program
{
    public static void Main()
    {
        // В C++ окно создаётся на весь экран по primary monitor.
        var gws = GameWindowSettings.Default;
        var nws = new NativeWindowSettings
        {
            Size = new Vector2i(1920, 1080), // будет перекрыто Fullscreen
            Title = " program_5_1_1 ",
            WindowState = WindowState.Fullscreen,
            APIVersion = new Version(4, 3),
            Profile = ContextProfile.Core,
            Flags = ContextFlags.ForwardCompatible
        };

        using var game = new Game(gws, nws);
        game.Run();
    }
}