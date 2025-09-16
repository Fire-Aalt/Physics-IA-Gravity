using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public static class NativeArrayExtensions
{
    public static ref T ElementAt<T>(this NativeArray<T> array, int index)
        where T : struct
    {
        CheckIndex(index, array.Length);
        unsafe
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }
    }

    [Conditional("UNITY_EDITOR")]
    private static void CheckIndex(int index, int length)
    {
        if (index < 0 || index >= length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}