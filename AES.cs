using System;
using System.IO;
using System.Security.Cryptography;

namespace reWZ
{
    public static class AES
    {
        private static readonly byte[] AESKey = {0x13, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0xB4, 0x00, 0x00, 0x00, 0x1B, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x33, 0x00, 0x00, 0x00, 0x52, 0x00, 0x00, 0x00};
        private static readonly byte[] GMSIV = {0x4D, 0x23, 0xC7, 0x2B, 0x4D, 0x23, 0xC7, 0x2B, 0x4D, 0x23, 0xC7, 0x2B, 0x4D, 0x23, 0xC7, 0x2B};
        private static readonly byte[] KMSIV = {0xB9, 0x7D, 0x63, 0xE9, 0xB9, 0x7D, 0x63, 0xE9, 0xB9, 0x7D, 0x63, 0xE9, 0xB9, 0x7D, 0x63, 0xE9};
        private static readonly uint Crypto = 0x581C3F6D;

        public static byte[] GetAESKey(WZVariant version)
        {
            switch((int)version) {
                case 0:
                    return GenerateKey(KMSIV, AESKey);
                case 1:
                    return GenerateKey(GMSIV, AESKey);
                case 2:
                    return new byte[UInt16.MaxValue];
                default:
                    throw new ArgumentException("Invalid WZ variant passed.", "version");
            }
        }

        private static byte[] GenerateKey(byte[] iv, byte[] aesKey)
        {
            //byte[] wzKey = new byte[0x10000];
            MemoryStream memStream = new MemoryStream(0x10000);
            CryptoStream cStream = new CryptoStream(memStream, new AesManaged {KeySize = 256, Key = aesKey, Mode = CipherMode.ECB}.CreateEncryptor(), CryptoStreamMode.Write);
            try {
                cStream.Write(iv, 0, 16);
                for (int i = 0; i < (0x10000 - 16); i += 16)
                    cStream.Write(memStream.GetBuffer(), i, 16);
                cStream.Flush();
                return memStream.ToArray();
            }
            finally
            {
                cStream.Dispose();
                memStream.Dispose();
            }
           
        }
    }

    public enum WZVariant
    {
        MSEA = 0, KMS = 0, KMST = 0, JMS = 0, JMST = 0, EMS = 0,
        GMS = 1, GMST = 1, TMS = 1,
        BMS = 2, Classic = 2
    }
}
