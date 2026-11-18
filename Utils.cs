using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace OpenGLApp511;

public static class Utils
{
    /// <summary>Аналог Utils::createShaderProgram(vertPath, fragPath).</summary>
    public static int CreateShaderProgram(string vertPath, string fragPath)
    {
        int vertShader = CompileShader(ShaderType.VertexShader,
                                       File.ReadAllText(vertPath), vertPath);
        int fragShader = CompileShader(ShaderType.FragmentShader,
                                       File.ReadAllText(fragPath), fragPath);

        int program = GL.CreateProgram();
        GL.AttachShader(program, vertShader);
        GL.AttachShader(program, fragShader);
        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int ok);
        if (ok == 0)
        {
            Console.WriteLine("ERROR::LINKING_PROGRAM\n" + GL.GetProgramInfoLog(program));
            CheckOpenGLError();
        }

        GL.DeleteShader(vertShader);
        GL.DeleteShader(fragShader);
        return program;
    }

    private static int CompileShader(ShaderType type, string source, string path)
    {
        int id = GL.CreateShader(type);
        GL.ShaderSource(id, source);
        GL.CompileShader(id);

        GL.GetShader(id, ShaderParameter.CompileStatus, out int ok);
        if (ok == 0)
        {
            Console.WriteLine($"=== SHADER COMPILE ERROR ===");
            Console.WriteLine($"File : {Path.GetFullPath(path)}");
            Console.WriteLine($"Type : {type}");
            Console.WriteLine(GL.GetShaderInfoLog(id));
            Console.WriteLine($"=== END OF LOG ===");
            CheckOpenGLError();
        }

        return id;
    }

    /// <summary>Аналог Utils::checkOpenGLError().</summary>
    public static bool CheckOpenGLError()
    {
        bool foundError = false;
        ErrorCode err;
        while ((err = GL.GetError()) != ErrorCode.NoError)
        {
            Console.WriteLine($"glError: {err}");
            foundError = true;
        }
        return foundError;
    }

    /// <summary>Аналог Utils::loadTexture(path).</summary>
    public static int LoadTexture(string path)
    {
        StbImage.stbi_set_flip_vertically_on_load(1);

        int texId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texId);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                        (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                        (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                        (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                        (int)TextureWrapMode.Repeat);

        if (!File.Exists(path))
        {
            Console.WriteLine($"ERROR: texture not found: {path}");
            return texId;
        }

        using FileStream fs = File.OpenRead(path);
        ImageResult image = ImageResult.FromStream(fs, ColorComponents.RedGreenBlueAlpha);

        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba,
            image.Width,
            image.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            image.Data);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        return texId;
    }
}