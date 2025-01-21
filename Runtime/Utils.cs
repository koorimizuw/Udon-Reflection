using System;

namespace Yamadev.UdonReflection
{
    public static class Utils
    {
        public static T ForceCast<T>(this object[] objectArray)
        {
            return (T)(object)objectArray;
        }

        public static void Resize<T>(ref T[] array, int newSize)
        {
            if (newSize < 0) array = new T[0];
            T[] array2 = array;
            if (array2 == null) array = new T[newSize];
            else if (array2.Length != newSize)
            {
                T[] array3 = new T[newSize];
                Array.Copy(array2, 0, array3, 0, (array2.Length > newSize) ? newSize : array2.Length);
                array = array3;
            }
        }

        public static T[] Add<T>(this T[] arr, T item)
        {
            Resize(ref arr, arr.Length + 1);
            arr[arr.Length - 1] = item;
            return arr;
        }
    }
}