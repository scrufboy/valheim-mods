using System;
using System.IO;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>Loads an uncompressed PCM WAV (8/16/24/32-bit, any channel count) into a Unity AudioClip.</summary>
    internal static class WavClip
    {
        internal static AudioClip Load(string path)
        {
            byte[] b = File.ReadAllBytes(path);
            if (b.Length < 44 || b[0] != 'R' || b[1] != 'I' || b[2] != 'F' || b[3] != 'F' || b[8] != 'W' || b[9] != 'A' || b[10] != 'V' || b[11] != 'E')
                throw new InvalidDataException("not a WAV: " + Path.GetFileName(path));
            int channels = 0, rate = 0, bits = 0, format = 0, dataAt = -1, dataLen = 0;
            for (int i = 12; i + 8 <= b.Length; )
            {
                string id = "" + (char)b[i] + (char)b[i + 1] + (char)b[i + 2] + (char)b[i + 3];
                int len = BitConverter.ToInt32(b, i + 4);
                if (id == "fmt ")
                {
                    format = BitConverter.ToUInt16(b, i + 8);
                    channels = BitConverter.ToUInt16(b, i + 10);
                    rate = BitConverter.ToInt32(b, i + 12);
                    bits = BitConverter.ToUInt16(b, i + 22);
                }
                else if (id == "data") { dataAt = i + 8; dataLen = Math.Min(len, b.Length - dataAt); break; }
                i += 8 + len + (len & 1);
            }
            if (format != 1 || channels < 1 || rate < 1 || dataAt < 0 || (bits != 8 && bits != 16 && bits != 24 && bits != 32))
                throw new InvalidDataException("unsupported WAV (PCM only): " + Path.GetFileName(path));
            int bytes = bits / 8, count = dataLen / bytes;
            var samples = new float[count];
            for (int s = 0, o = dataAt; s < count; s++, o += bytes)
            {
                switch (bits)
                {
                    case 8: samples[s] = (b[o] - 128) / 128f; break;
                    case 16: samples[s] = BitConverter.ToInt16(b, o) / 32768f; break;
                    case 24: samples[s] = ((b[o] << 8 | b[o + 1] << 16 | b[o + 2] << 24) >> 8) / 8388608f; break;
                    default: samples[s] = BitConverter.ToInt32(b, o) / 2147483648f; break;
                }
            }
            AudioClip clip = AudioClip.Create(Path.GetFileNameWithoutExtension(path), count / channels, channels, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
