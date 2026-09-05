using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TamaPoke.Utils
{
    public static class SaveEncryptionHelper
    {
        // 🌟 우리 게임만의 32자리 비밀 키 (원하시는 영문/숫자로 자유롭게 변경하셔도 됩니다)
        private static readonly string EncryptionKey = "gomdolit-jeong-TamaPoke-SaveKey";

        // 🌟 16자리 초기화 벡터 (고정값)
        private static readonly string IV = "gomdolit-jeong-IV";

        public static string Encrypt(string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                // 키와 IV의 길이를 안전하게 맞춥니다.
                aesAlg.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
                aesAlg.IV = Encoding.UTF8.GetBytes(IV.PadRight(16).Substring(0, 16));

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
                aesAlg.IV = Encoding.UTF8.GetBytes(IV.PadRight(16).Substring(0, 16));

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}