using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;


namespace TheGarbageCollector {

    public class AudioResourceLoader {

        public static uint LoadedSoundBankID = 0;

        public static readonly string BankName = "GCSFX.bnk";
        
        public static bool InitAudio() {
            return LoadBankFromModProject(TheGarbageCollector.ModName + "." + BankName, out LoadedSoundBankID);
        }

        public static bool LoadBankFromModProject(string path, out uint bankid) {
            path = path.Replace("/", ".").Replace("\\", ".");
            if (!path.EndsWith(".bnk"))path += ".bnk";
            Assembly assembly = Assembly.GetCallingAssembly();
            bankid = 0;
            using (Stream m_Stream = assembly.GetManifestResourceStream(path)) {
                if (m_Stream != null) {
                    string name = path.Substring(0, path.LastIndexOf('.'));
                    string bankNameFromPath = path;
                    if (name.LastIndexOf('.') >= 0) bankNameFromPath = name.Substring(name.LastIndexOf('.') + 1);
                    return LoadSoundbankFromStream(m_Stream, bankNameFromPath, out bankid);
                }
            }
            return false;
        }

        private static bool LoadSoundbankFromStream(Stream stream, string bankName, out uint bankID) {
            byte[] array = StreamToByteArray(stream);
            IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
            AKRESULT akresult = AKRESULT.AK_Fail;
            bankID = 0;
            try {
                Marshal.Copy(array, 0, intPtr, array.Length);
                akresult = AkSoundEngine.LoadAndDecodeBankFromMemory(intPtr, (uint)array.Length, false, bankName, false, out bankID);
            }
            finally { Marshal.FreeHGlobal(intPtr); }
            return (akresult == AKRESULT.AK_Success);
        }

        private static byte[] StreamToByteArray(Stream input) {
            byte[] array = new byte[16384];
            byte[] result;
            using (MemoryStream memoryStream = new MemoryStream()) {
                int count;
                while ((count = input.Read(array, 0, array.Length)) > 0) { memoryStream.Write(array, 0, count); }
                result = memoryStream.ToArray();
            }
            return result;
        }
    }
}

