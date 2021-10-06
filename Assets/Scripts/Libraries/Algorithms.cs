using UnityEngine;

namespace Faizan314
{
    public static class Algorithms
    {
        public static void ShuffleArray<T>(T[] array)
        {
            int size = array.Length;
            if (size < 1)
                return;
            for (int i = 0; i < size - 1; i++)
            {
                int index = Random.Range(1, array.Length);
                T temp = array[index];
                array[index] = array[0];
                array[0] = temp;
            }
        }
        public static void ShuffleArray<T>(T[] array, int seed)
        {
            int size = array.Length;
            if (size < 1)
                return;
            var prng = new System.Random(seed);
            for (int i = 0; i < size - 1; i++)
            {
                int index = prng.Next(1, array.Length);
                T temp = array[index];
                array[index] = array[0];
                array[0] = temp;
            }
        }
    }
}