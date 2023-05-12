namespace FileManager.Core.Extensions
{
    internal static class Id
    {
        internal static uint GetRandomUInt32() => (uint)System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MaxValue);
        internal static ulong GetRandomUInt64() => ((ulong)GetRandomUInt32() << 32) | GetRandomUInt32();
    }
}
