using System.Runtime.InteropServices;
using System.Text;

namespace Compendium.Web.KeyStore;

internal sealed class WindowsCredentialManagerStore : IApiKeyStore
{
    private const string TargetName = "compendium-web/llm-api-key";
    private const int CredTypeGeneric = 1;
    private const int CredPersistLocalMachine = 2;

    public string StoreName => "Windows Credential Manager";
    public bool IsAvailable => OperatingSystem.IsWindows();

    public Task<string?> RetrieveAsync()
    {
        if (!CredRead(TargetName, CredTypeGeneric, 0, out var ptr))
            return Task.FromResult<string?>(null);
        try
        {
            var cred = Marshal.PtrToStructure<NativeCredential>(ptr);
            if (cred.CredentialBlobSize == 0)
                return Task.FromResult<string?>(null);
            var bytes = new byte[cred.CredentialBlobSize];
            Marshal.Copy(cred.CredentialBlob, bytes, 0, bytes.Length);
            return Task.FromResult<string?>(Encoding.Unicode.GetString(bytes));
        }
        finally
        {
            CredFree(ptr);
        }
    }

    public Task StoreAsync(string apiKey)
    {
        var blob = Encoding.Unicode.GetBytes(apiKey);
        var blobPtr = Marshal.AllocHGlobal(blob.Length);
        var targetPtr = Marshal.StringToHGlobalUni(TargetName);
        var userPtr = Marshal.StringToHGlobalUni("compendium");
        try
        {
            Marshal.Copy(blob, 0, blobPtr, blob.Length);
            var cred = new NativeCredential
            {
                Type = CredTypeGeneric,
                TargetName = targetPtr,
                UserName = userPtr,
                CredentialBlobSize = (uint)blob.Length,
                CredentialBlob = blobPtr,
                Persist = (uint)CredPersistLocalMachine,
            };
            if (!CredWrite(ref cred, 0))
                throw new InvalidOperationException(
                    $"CredWrite failed (error {Marshal.GetLastWin32Error()}).");
        }
        finally
        {
            Marshal.FreeHGlobal(blobPtr);
            Marshal.FreeHGlobal(targetPtr);
            Marshal.FreeHGlobal(userPtr);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync()
    {
        CredDelete(TargetName, CredTypeGeneric, 0);
        return Task.CompletedTask;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public int Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, int type, int reserved, out IntPtr credential);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref NativeCredential credential, int flags);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, int type, int reserved);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CredFree(IntPtr credential);
}
