using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace forum.Utils
{
    public static class Extensions
    {
        public static FileType GetFileType(string path)
        {
            return FileType.image;
        }
        public static IEnumerable<byte[]> GetFileBytes(IFormFileCollection filesInput, string WebRootPath)
        {
            var files = new List<byte[]>();
            foreach (var file in filesInput)
            {
                var path = $"{file.FileName}";
                using FileStream fileStream = new FileStream(WebRootPath + path, FileMode.Open);
                byte[] buf = new byte[fileStream.Length];
                fileStream.Read(buf);
                files.Add(buf);
            }
            return files;
        }
        public static string GetHash(string password, byte[] salt)
        {
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password!,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8
                ));
            return hashed;
        }

    }
}
