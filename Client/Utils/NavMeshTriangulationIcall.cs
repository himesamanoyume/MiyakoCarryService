using System;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace MiyakoCarryService.Client.Utils;

public static class NavMeshTriangulationIcall
{
    private const int ScratchSize = 64;

    private static readonly string[] _candidateNames =
    {
        "UnityEngine.AI.NavMesh::CalculateTriangulation_Injected",
        "UnityEngine.AI.NavMesh::CalculateTriangulation",
        "UnityEngine.AI.NavMesh::Triangulate",
        "UnityEngine.AI.NavMesh::Triangulate_Injected",
        "NavMesh::CalculateTriangulation_Injected",
        "NavMesh::CalculateTriangulation",
        "NavMesh::Triangulate",
    };

    private static IntPtr _entryPoint;
    private static bool _resolutionAttempted;
    private static CalculateTriangulationDelegate _calculateTriangulation;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CalculateTriangulationDelegate(
        IntPtr first,
        IntPtr second,
        IntPtr third,
        IntPtr fourth,
        IntPtr fifth,
        IntPtr sixth);

    public static bool TryGetVertices(out Vector3[] vertices)
    {
        vertices = null;

        var calculateTriangulation = GetCalculateTriangulation();
        if (calculateTriangulation == null)
        {
            return false;
        }

        var scratch = Marshal.AllocHGlobal(ScratchSize);
        try
        {
            for (var offset = 0; offset < ScratchSize; offset += IntPtr.Size)
            {
                Marshal.WriteIntPtr(scratch, offset, IntPtr.Zero);
            }

            calculateTriangulation(
                scratch,
                IntPtr.Add(scratch, IntPtr.Size),
                IntPtr.Add(scratch, IntPtr.Size * 2),
                IntPtr.Add(scratch, IntPtr.Size * 3),
                IntPtr.Add(scratch, IntPtr.Size * 4),
                IntPtr.Add(scratch, IntPtr.Size * 5));

            var verticesPointer = Marshal.ReadIntPtr(scratch);
            if (verticesPointer == IntPtr.Zero)
            {
                return false;
            }

            vertices = CopyVertices(verticesPointer);
            return vertices.Length > 0;
        }
        finally
        {
            Marshal.FreeHGlobal(scratch);
        }
    }

    private static Vector3[] CopyVertices(IntPtr verticesPointer)
    {
        var handle = IL2CPP.il2cpp_gchandle_new(verticesPointer, false);
        try
        {
            var source = new Il2CppStructArray<Vector3>(verticesPointer);
            var result = new Vector3[source.Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = source[i];
            }

            return result;
        }
        finally
        {
            IL2CPP.il2cpp_gchandle_free(handle);
        }
    }

    private static CalculateTriangulationDelegate GetCalculateTriangulation()
    {
        if (!_resolutionAttempted)
        {
            _resolutionAttempted = true;
            _entryPoint = Il2CppICalls.Resolve(_candidateNames);
        }

        if (_entryPoint == IntPtr.Zero)
        {
            return null;
        }

        if (_calculateTriangulation == null)
        {
            _calculateTriangulation = Marshal.GetDelegateForFunctionPointer<CalculateTriangulationDelegate>(_entryPoint);
        }

        return _calculateTriangulation;
    }
}
