using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace ServiceComposer.AspNetCore
{
    //courtesy of NServiceBus https://github.com/Particular/NServiceBus/blob/4954af83fad81cc80769c8ed161ee4a37812d443/src/NServiceBus.Core/Hosting/Helpers/AssemblyValidator.cs
    class AssemblyValidator
    {
        public static void ValidateAssemblyFile(string assemblyPath, out bool shouldLoad, out string reason)
        {
            using (var stream = File.OpenRead(assemblyPath))
            using (var file = new PEReader(stream))
            {
                var hasMetadata = false;

                try
                {
                    hasMetadata = file.HasMetadata;
                }
                catch (BadImageFormatException) { }

                if (!hasMetadata)
                {
                    shouldLoad = false;
                    reason = "File is not a .NET assembly.";
                    return;
                }

                var reader = file.GetMetadataReader();
                var assemblyDefinition = reader.GetAssemblyDefinition();

                if (!assemblyDefinition.PublicKey.IsNil)
                {
                    var publicKey = reader.GetBlobBytes(assemblyDefinition.PublicKey);
                    var publicKeyToken = GetPublicKeyToken(publicKey);

                    if (IsRuntimeAssembly(publicKeyToken))
                    {
                        shouldLoad = false;
                        reason = "File is a .NET runtime assembly.";
                        return;
                    }
                }

                shouldLoad = true;
                reason = "File is a .NET assembly.";
            }
        }

        static byte[] GetPublicKeyToken(byte[] publicKey)
        {
            using (var sha1 = SHA1.Create())
            {
                var hash = sha1.ComputeHash(publicKey);
                var publicKeyToken = new byte[8];

                for (var i = 0; i < 8; i++)
                {
                    publicKeyToken[i] = hash[hash.Length - (i + 1)];
                }

                return publicKeyToken;
            }
        }

        public static bool IsRuntimeAssembly(byte[] publicKeyToken)
        {
            var tokenString = BitConverter.ToString(publicKeyToken).Replace("-", string.Empty).ToLowerInvariant();

            //Compare token to known Microsoft tokens
            switch (tokenString)
            {
                case "b77a5c561934e089":
                case "7cec85d7bea7798e":
                case "b03f5f7f11d50a3a":
                case "31bf3856ad364e35":
                case "cc7b13ffcd2ddd51":
                case "adb9793829ddae60":
                    return true;
                default:
                    return false;
            }
        }
    }
}