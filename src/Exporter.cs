using System.Collections;

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Players;
using Il2CppRUMBLE.Players.Subsystems;

using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using UnityEngine;
using UnityMesh = UnityEngine.Mesh;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppList_Vector2 = Il2CppSystem.Collections.Generic.List<UnityEngine.Vector2>;
using MelonLoader;
using MelonLoader.Utils;
using SharpGLTF.Transforms;
using Buffer = System.Buffer;


[assembly: MelonInfo(typeof(glTF_Exporter.Exporter), glTF_Exporter.BuildInfo.Name, glTF_Exporter.BuildInfo.Version, glTF_Exporter.BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]
[assembly: MelonColor(255, 255, 170, 238)] // #FAE pink :3


namespace glTF_Exporter;

public static class GeneralExtensions
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector2 ToNumerics(this Vector2 vec)
    {
        return new System.Numerics.Vector2(vec.x, vec.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector3 ToNumerics(this Vector3 vec)
    {
        return new System.Numerics.Vector3(vec.x, vec.y, vec.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector4 ToNumerics(this Vector4 vec)
    {
        return new System.Numerics.Vector4(vec.x, vec.y, vec.z, vec.w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector4 ToNumerics(this Color32 color)
    {
        return new System.Numerics.Vector4(color.r, color.g, color.b, color.a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Matrix4x4 ToNumerics(this Matrix4x4 unityMatrix)
    {
        return new System.Numerics.Matrix4x4(
            unityMatrix.m00, unityMatrix.m10, unityMatrix.m20, unityMatrix.m30,
            unityMatrix.m01, unityMatrix.m11, unityMatrix.m21, unityMatrix.m31,
            unityMatrix.m02, unityMatrix.m12, unityMatrix.m22, unityMatrix.m32,
            unityMatrix.m03, unityMatrix.m13, unityMatrix.m23, unityMatrix.m33
        );
    }

}

public sealed class Exporter : MelonMod
{
    readonly WaitForEndOfFrame _frameEnd = new();
    internal bool MeshGenerationLocked;
    public override void OnLateInitializeMelon()
    {
        Directory.CreateDirectory($"{MelonEnvironment.UserDataDirectory}/glTF_Exporter/");
    }

    internal void StartExport(string username, Il2CppRUMBLE.Players.PlayerVisualData data)
    {
        string fileName = GenerateFileName(username, data);
        if (File.Exists($"{MelonEnvironment.UserDataDirectory}/glTF_Exporter/{fileName}.glb"))
        {
            LoggerInstance.Msg($"Skipping: {username}, Already exists at {MelonEnvironment.UserDataDirectory}/glTF_Exporter/{fileName}.glb");
            return;
        }
        File.Create($"{MelonEnvironment.UserDataDirectory}/glTF_Exporter/{fileName}.glb").Close(); // Preallocate file
        MelonCoroutines.Start(Export_glTF(username));
    }

    private IEnumerator Export_glTF(string publicUsername)
    {
        yield return _frameEnd; // we wait til frame end so the GPU Can upload our texture
        if (MeshGenerationLocked)
            yield return null;
        MeshGenerationLocked = true;
        var visuals = Il2CppRUMBLE.CharacterCreation.CharacterCreationLookupTable.instance.GeneratedPlayerVisualsCache[publicUsername];
        // Fetch Texture
        MaterialBuilder material = new MaterialBuilder("Player Controller");
        Texture2D tex = TexDownload(visuals.GeneratedTexture);

        ImageBuilder baseColor = ImageBuilder.From(new(tex.EncodeToPNG()));
        material.WithChannelImage(KnownChannel.BaseColor, baseColor);

        UnityMesh unityMesh = visuals.GeneratedMesh;

        Il2CppList_Vector2 uv = new();
        unityMesh.GetUVs(0, uv);

        LoggerInstance.Msg($"Exporting {publicUsername}");

        // glTF Scene Setup
        ModelRoot model = ModelRoot.CreateModel();
        Scene root = model.UseScene("Player Character");
        // Copyright
        model.Asset.Copyright = "Copyright 2025 \u00a9 Buckethead Entertainment. All rights are reserved.";
        model.Asset.Generator = $"glTF_Exporter {BuildInfo.Version} Using SharpGLTF 1.0.4";


        // Mesh Generation
        #region MeshGeneration
        MeshBuilder<VertexPositionNormal, VertexColor1Texture1, VertexJoints4> mesh =
            new(visuals.Data.ToPlayfabDataString());

        PopulateMesh(unityMesh.triangles,
            unityMesh.vertices,
            unityMesh.normals,
            unityMesh.colors32,
            mesh,
            material,
            unityMesh.boneWeights,
            uv.ToArray());

        var glbMesh = model.CreateMesh(mesh);
        mesh.Validate();
        Node meshResult = root.CreateNode("CC_Result - Copyright 2026 \u00a9 Buckethead Entertainment. All rights are reserved.");
        #endregion

        // Setup Bones
        meshResult.WithSkinnedMesh(glbMesh, ReconstructSkeleton(meshResult, unityMesh));
        string fileName = GenerateFileName(publicUsername, visuals.Data);
        model.SaveGLB($"{MelonEnvironment.UserDataDirectory}/glTF_Exporter/{fileName}.glb");
        LoggerInstance.Msg($"Saved model to {MelonEnvironment.UserDataDirectory}/glTF_Exporter/{fileName}.glb");
        MeshGenerationLocked = false;
    }

    private string GenerateFileName(string publicUsername, PlayerVisualData data)
    {
        string playerName = Regex.Replace(publicUsername, "<.*?>|\\(.*?\\)|[^a-zA-Z0-9_ ]", "").TrimStart().TrimEnd();
        return playerName + "-" + CreateCosmeticHash(data);
    }

    private string CreateCosmeticHash(PlayerVisualData data)
    {
        System.Security.Cryptography.SHA1 hasher = System.Security.Cryptography.SHA1.Create(); // sha1 because its fast and odds of collision are minimal 
        List<short> cosmeticIndexes = new();
        cosmeticIndexes.Add((short)data.Identity);
        cosmeticIndexes.AddRange(data.ColorationIndexes);
        cosmeticIndexes.AddRange(data.CustomizationPartIndexes);
        cosmeticIndexes.AddRange(data.TextureCustomizationIndexes);
        cosmeticIndexes.AddRange(data.TextureOpacityIndexes);
        cosmeticIndexes.AddRange(data.WeightAdjustementIndexes);
        byte[] bufBytes = new byte[cosmeticIndexes.Count * sizeof(short)];

        Buffer.BlockCopy(cosmeticIndexes.ToArray(), 0, bufBytes, 0, cosmeticIndexes.Count * sizeof(short));
        return BitConverter.ToString(hasher.ComputeHash(bufBytes)).Replace("-", "").Substring(0, 8);
    }


    private Texture2D TexDownload(Texture2D gpuTex)
    {
        if (gpuTex == null) LoggerInstance.Msg("what the fuck");
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            gpuTex.width,
            gpuTex.height,
            0,
            gpuTex.graphicsFormat,
            1
        );

        Texture2D readableTexture = new(gpuTex.width, gpuTex.height, TextureFormat.RGBA32, false);

        Graphics.Blit(gpuTex, renderTexture);
        RenderTexture.active = renderTexture;
        readableTexture.ReadPixels(new Rect(0, 0, gpuTex.width, gpuTex.height), 0, 0);


        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);

        return readableTexture;
    }

    private (Node Joint, System.Numerics.Matrix4x4 IBM)[] ReconstructSkeleton(Node nodeRoot, UnityMesh mesh)
    {
        List<(Node Joint, System.Numerics.Matrix4x4 IBM)> boneBinds = new();

        var bindPoses = mesh.bindposes;
        var nodeBones = PlayerManager.Instance.LocalPlayer.Controller.GetComponentInChildren<SkinnedMeshRenderer>().bones;


        foreach (var (bone, bind) in nodeBones.Zip(bindPoses))
        {
            System.Numerics.Matrix4x4 matrix = bind.ToNumerics();
            Node boneNode = nodeRoot.CreateNode(bone.name);
            boneNode.WithLocalTransform(AffineTransform.CreateDecomposed(bind.inverse.ToNumerics()));
            boneBinds.Add((boneNode, matrix));
        }


        return boneBinds.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void PopulateMesh(
        Il2CppStructArray<int> triangles,
        Il2CppStructArray<Vector3> vertices,
        Il2CppStructArray<Vector3> normals,
        Il2CppStructArray<Color32> vertexColors,
        MeshBuilder<VertexPositionNormal, VertexColor1Texture1, VertexJoints4> mesh,
        MaterialBuilder material,
        Il2CppStructArray<BoneWeight> weights,
        Vector2[] uv)
    {
        foreach (var tri in triangles.Chunk(3))
        {
            var v1 = VertexBuilder<VertexPositionNormal, VertexColor1Texture1, VertexJoints4>.Create(
                vertices[tri[0]].ToNumerics(),
                 normals[tri[0]].ToNumerics());

            var v2 = VertexBuilder<VertexPositionNormal, VertexColor1Texture1, VertexJoints4>.Create(
                vertices[tri[1]].ToNumerics(),
                 normals[tri[1]].ToNumerics());

            var v3 = VertexBuilder<VertexPositionNormal, VertexColor1Texture1, VertexJoints4>.Create(
                vertices[tri[2]].ToNumerics(),
                 normals[tri[2]].ToNumerics());

            System.Numerics.Vector2 uv1 = uv[tri[0]].ToNumerics();
            System.Numerics.Vector2 uv2 = uv[tri[1]].ToNumerics();
            System.Numerics.Vector2 uv3 = uv[tri[2]].ToNumerics();

            // +1 to account for flipping offset
            uv1.Y = (uv1.Y * -1) + 1;
            uv2.Y = (uv2.Y * -1) + 1;
            uv3.Y = (uv3.Y * -1) + 1;

            v1.Material.TexCoord = uv1;
            v2.Material.TexCoord = uv2;
            v3.Material.TexCoord = uv3;

            v1.Material.Color = vertexColors[tri[0]].ToNumerics();
            v2.Material.Color = vertexColors[tri[1]].ToNumerics();
            v3.Material.Color = vertexColors[tri[2]].ToNumerics();

            #region WeightSetup 
            v1.Skinning.Joints = new(
                  weights[tri[0]].boneIndex0,
                  weights[tri[0]].boneIndex1,
                  weights[tri[0]].boneIndex2,
                w: weights[tri[0]].boneIndex3);

            v1.Skinning.Weights = new(
                  weights[tri[0]].weight0,
                  weights[tri[0]].weight1,
                  weights[tri[0]].weight2,
                w: weights[tri[0]].weight3);

            v2.Skinning.Joints = new(
                  weights[tri[1]].boneIndex0,
                  weights[tri[1]].boneIndex1,
                  weights[tri[1]].boneIndex2,
                w: weights[tri[1]].boneIndex3);

            v2.Skinning.Weights = new(
                  weights[tri[1]].weight0,
                  weights[tri[1]].weight1,
                  weights[tri[1]].weight2,
                w: weights[tri[1]].weight3);

            v3.Skinning.Joints = new(
                  weights[tri[2]].boneIndex0,
                  weights[tri[2]].boneIndex1,
                  weights[tri[2]].boneIndex2,
                w: weights[tri[2]].boneIndex3);

            v3.Skinning.Weights = new(
                  weights[tri[2]].weight0,
                  weights[tri[2]].weight1,
                  weights[tri[2]].weight2,
                w: weights[tri[2]].weight3);
            #endregion
            mesh.UsePrimitive(material).AddTriangle(v1, v2, v3);
        }
    }
}
