using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using static MegEditor.MegaFile;

namespace MegEditor
{
    public sealed class EmbeddedStream : Stream
    {
        public EmbeddedStream(Stream parent, long size)
            : this(parent, size, false)
        {
            Debug.Assert(parent != null);
            if (!parent.CanRead)
            {
                throw new ArgumentException();
            }
        }

        public EmbeddedStream(Stream parent, long size, bool leaveOpen)
        {
            m_parent = parent;
            m_size = size;
            m_leaveOpen = leaveOpen;
        }

        public override bool CanRead => m_parent.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => m_size;

        public override long Position { get => m_position; set => Seek(value, SeekOrigin.Begin); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentException();
            }

            count = (int)Math.Min(count, m_size - m_position);
            count = m_parent.Read(buffer, offset, count);

            // Advance cursor by read bytes
            m_position += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        { 
            if (disposing) {
                if (m_parent != null && !m_leaveOpen)
                {
                    m_parent.Dispose();
                    m_parent = null;
                }
            }
        }

        private Stream m_parent;
        private long m_size;
        private long m_position = 0;
        private bool m_leaveOpen;
    }

    public sealed class MegaFile : IDisposable
    {
        public struct EncryptionKey
        {
            public byte[] Key;
            public byte[] InitialVector;

            public EncryptionKey(byte[] key, byte[] initialVector)
            {
                Key = key;
                InitialVector = initialVector;
            }
        };

        public enum Format
        {
            V1,
            V2,
            V3
        };

        public MegaFile()
        {
        }

        // Contructs the MegaFile from the stream.
        // Note: this object takes ownership of the stream and will close the stream when closed.
        public MegaFile(Stream stream, EncryptionKey? key)
        {
            if (!stream.CanSeek)
            {
                throw new ArgumentException();
            }

            // File flags
            uint filenamesSize = 0;

            // Read counts
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                var numFilenames = reader.ReadUInt32();
                var numFiles = reader.ReadUInt32();

                m_format = Format.V1;
                if (numFilenames != numFiles)
                {
                    // Verify the header
                    var flags = numFilenames;
                    Verify(flags == 0x8FFFFFFF || flags == 0xFFFFFFFF);
                    Verify(numFiles == 0x3F7D70A4);

                    // Skip the header size field, v1 doesn't have it
                    // so the code doesn't use it anyway.
                    reader.ReadUInt32();

                    // Now read the actual counts
                    numFilenames = reader.ReadUInt32();
                    numFiles = reader.ReadUInt32();

                    // v3 has a filenames size field here, which overlaps with the 
                    // first filename in v2, try to guess which version it is...
                    filenamesSize = reader.ReadUInt32();

                    // If the high-order half (two bytes) are printables and the low-order half is a valid path length,
                    // this file is VERY likely v2.

                    if (IsPrintable((byte)((filenamesSize >> 24) & 0xFF)) &&
                        IsPrintable((byte)((filenamesSize >> 16) & 0xFF)) &&
                        (filenamesSize & 0xFFFF) < MAX_PATH)
                    {
                        m_format = Format.V2;

                        // Note: V2 cannot be encrypted: it's missing the filenamesSize field 
                        // so we don't know how much to decrypt to read the filenames.
                        Verify(flags == 0xFFFFFFFF);
                        filenamesSize = 0;

                        // Undo the read of the filenames size, which doesn't exist in v2.
                        stream.Seek(-4, SeekOrigin.Current);
                    }
                    else
                    {
                        m_format = Format.V3;

                        // V3 supports encryption
                        bool is_encrypted = (flags == 0x8FFFFFFF);
                        if (is_encrypted)
                        {
                            if (key == null)
                            {
                                throw new FormatException("Missing decryption key for reading Mega File.");
                            }
                            m_encryption_key = key;
                        }
                    }
                }

                // We should have as many filenames as files
                Verify(numFilenames == numFiles);

                // Read filenames
                m_filenames = new List<string>((int)numFilenames);
                if (filenamesSize > 0)
                {
                    if (m_encryption_key.HasValue)
                    {
                        using (var aes = CreateAes(m_encryption_key.Value))
                        {
                            using (MemoryStream msDecrypt = new MemoryStream(reader.ReadBytes((int)filenamesSize)))
                            {
                                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))
                                {
                                    using (var nameReader = new BinaryReader(csDecrypt))
                                    {
                                        for (int i = 0; i < numFilenames; ++i)
                                        {
                                            var size = nameReader.ReadUInt16();
                                            m_filenames.Add(System.Text.UTF8Encoding.Default.GetString(nameReader.ReadBytes(size)));
                                        }
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        for (int i = 0; i < numFilenames; ++i)
                        {
                            var size = reader.ReadUInt16();
                            m_filenames.Add(System.Text.UTF8Encoding.Default.GetString(reader.ReadBytes(size)));
                        }
                    }

                }
                else
                {
                    // Read filenames one by one
                    for (int i = 0; i < numFilenames; ++i)
                    {
                        var size = reader.ReadUInt16();
                        m_filenames.Add(System.Text.UTF8Encoding.Default.GetString(reader.ReadBytes(size)));
                    }
                }

                // Read files
                m_files = new List<FileInfo>((int)numFiles);
                for (int i = 0; i < numFiles; ++i)
                {
                    FileInfo info;
                    if (m_format == Format.V3)
                    {
                        var flags = reader.ReadUInt16();
                        if (flags == 1)
                        {
                            // Entry is encrypted; must match file encryption
                            Debug.Assert(m_encryption_key.HasValue);
                            using (var aes = CreateAes(m_encryption_key.Value))
                            {
                                var buffer = reader.ReadBytes(EncryptedSize(FILEDESC_V3_SIZE));
                                using (MemoryStream msDecrypt = new MemoryStream(buffer))
                                {
                                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))
                                    {
                                        using (var descReader = new BinaryReader(csDecrypt))
                                        {
                                            info = ReadFileDescV3(descReader);
                                            Verify(info.filename < m_filenames.Count);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Entry is unencrypted; must match file encryption
                            Debug.Assert(!m_encryption_key.HasValue);
                            info = ReadFileDescV3(reader);
                        }
                    }
                    else
                    {
                        info = ReadFileDescV1(reader);
                    }

                    // Verify that the filename index is in range
                    Verify(info.filename < m_filenames.Count);

                    // Verify that the index is sorted on CRC
                    Verify(i == 0 || m_files[i - 1].crc <= info.crc);

                    m_files.Add(info);
                }
            }

            m_stream = stream;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && m_stream != null)
            {
                m_stream.Dispose();
                m_stream = null;
            }
        }

        public Format GetFormat()
        {
            return Format.V1;
        }

        public Stream OpenFile(string path)
        {
            path = path.ToUpper();
            // Find the file
            var crc = CRC32(path);
            for (int i = LowerBound(crc); i < m_files.Count && m_files[i].crc == crc; ++i)
            {
                if (path == m_filenames[(int)m_files[i].filename])
                {
                    // Found
                    return OpenFile(i);
                }
            }
            return null;
        }

        private Stream OpenFile(int index)
        {
            Debug.Assert(index < m_files.Count);
            var file = m_files[index];

            if (file.stream != null)
            {
                // Custom stream (i.e., external file)
                return file.stream;
            }
            m_stream.Seek(file.start, SeekOrigin.Begin);

            // Stored in this mega file
            if (m_encryption_key != null)
            {
                // Encrypted files are aligned by block size, so expand the embedded file to align
                using (var aes = CreateAes(m_encryption_key.Value))
                {
                    int file_size = EncryptedSize((int)file.size);
                    var stream1 = new EmbeddedStream(m_stream, file_size, true);

                    // Then wrap a decryption stream around it
                    var decryptor = aes.CreateDecryptor();
                    var stream2 = new CryptoStream(stream1, decryptor, CryptoStreamMode.Read);

                    // Then wrap an embedded stream around that to truncate to the actual file size (after decryption)
                    return new EmbeddedStream(stream2, file.size);
                }
            }

            return new EmbeddedStream(m_stream, file.size, true);

        }

        public bool InsertFile(string path, Stream stream)
        {
            path = path.ToUpper();
            if (!stream.CanRead || !stream.CanSeek) {
                throw new ArgumentException();
            }

            long size = stream.Length;

            // Find the file
            uint crc = CRC32(path);
            int i = LowerBound(crc);
            for (; i < m_files.Count && m_files[i].crc == crc; ++i)
            {
                if (path == m_filenames[(int)m_files[i].filename])
                {
                    return false;
                }
            }
            // Not found, insert it

            // Add the file entry (in the correct spot to keep the array sorted on CRC)
            var info = new FileInfo();
            info.crc = crc;
            info.start = 0;
            info.stream = stream;
            info.size = (uint)size;
            info.filename = (uint)m_filenames.Count;
            m_files.Insert(i, info);

            // Store the filename
            m_filenames.Add(path);

            return true;
        }

        public bool RenameFile(string oldpath, string newpath)
        {
            oldpath = oldpath.ToUpper();
            newpath = newpath.ToUpper();
            // Find the file
            uint oldcrc = CRC32(oldpath);
            uint newcrc = CRC32(newpath);
            for (int i = LowerBound(oldcrc); i < m_files.Count && m_files[i].crc == oldcrc; ++i)
            {
                if (oldpath == m_filenames[(int)m_files[i].filename])
                {
                    // Found it
                    // Find the destination file
                    int j = LowerBound(newcrc);
                    for (; j < m_files.Count && m_files[j].crc == newcrc; ++j)
                    {
                        if (newpath == m_filenames[(int)m_files[j].filename])
                        {
                            // The new key already exists, we cannot rename
                            return false;
                        }
                    }

                    // Move the file from the i-th position to the j-th position
                    if (j >= i)
                    {
                        // Deleting i means j will shift down in this case
                        --j;
                    }

                    FileInfo info = m_files[i];
                    info.crc = newcrc;
                    m_files.RemoveAt(i);
                    m_files.Insert(j, info);
                    m_filenames[(int)info.filename] = newpath;
                    return true;
                }
            }
            return false;
        }

        public ICollection<string> GetFiles()
        {
            return m_filenames.AsReadOnly();
        }

        public uint? GetFileSize(string path)
        {
            path = path.ToUpper();
            // Find the file
            uint crc = CRC32(path);
            for (int i = LowerBound(crc); i < m_files.Count && m_files[i].crc == crc; ++i)
            {
                if (path == m_filenames[(int)m_files[i].filename]) {
                    // Found
                    return m_files[i].size;
                }
            }
            return null;
        }

        public bool DeleteFile(string path)
        {
            path = path.ToUpper();
            // Find the file
            uint crc = CRC32(path);
            for (int i = LowerBound(crc); i < m_files.Count && m_files[i].crc == crc; ++i)
            {
                if (path == m_filenames[(int)m_files[i].filename])
                {
                    // Found, delete it

                    // Move the last filename into the gap
                    uint filename = m_files[i].filename;
                    m_filenames[(int)filename] = m_filenames.Last();
                    m_filenames.RemoveAt(m_filenames.Count - 1);

                    // Update the file whose filename has moved. Unfortunately, this is O(n).
                    for (int j = 0; j < m_files.Count; ++j)
                    {
                        if (m_files[j].filename == m_filenames.Count)
                        {
                            // This file used to have its filename at the old position.
                            // Update to point to new position.
                            var info = m_files[j];
                            info.filename = filename;
                            m_files[j] = info;
                            break;
                        }
                    }

                    // Erase the file
                    m_files.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        bool IsEncrypted()
        {
            return m_encryption_key != null;
        }

        public void Save(Stream stream, Format format, EncryptionKey? key)
        {
            if (stream == null || !stream.CanSeek || !stream.CanWrite)
            {
                throw new ArgumentException();
            }

            if (key != null && format != Format.V3)
            {
                throw new NotSupportedException("Encryption only supported for Format 3");
            }

            // Write (and possibly encrypt) the filenames into a buffer
            byte[] filenames = MaybeEncrypt(key, s => {
                using (var writer = new BinaryWriter(s, Encoding.UTF8, true))
                {
                    foreach (var info in m_files)
                    {
                        var filename = m_filenames[(int)info.filename];
                        writer.Write((ushort)filename.Length);
                        writer.Write(filename.ToCharArray());
                    }

                }
            });

            // Write the mega file to the stream
            long data_start_field = -1;

            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                if (format == Format.V2 || format == Format.V3)
                {
                    // Write v2/v3 header
                    writer.Write((key != null) ? 0x8FFFFFFFU : 0xFFFFFFFFU);
                    writer.Write(0x3F7D70A4U);

                    data_start_field = stream.Position;
                    writer.Write(0U);    // Will get overwritten later
                }

                // Write counts
                writer.Write((uint)m_files.Count);
                writer.Write((uint)m_files.Count);

                if (format == Format.V3)
                {
                    // Write filenames buffer size
                    writer.Write(filenames.Length);
                }

                // Write the filename buffer
                writer.Write(filenames);

                var entry_size = (format == Format.V3)
                    ? 2 + ((key != null)
                        ? EncryptedSize(FILEDESC_V3_SIZE)
                        : FILEDESC_V3_SIZE)
                    : FILEDESC_V1_SIZE;
                var data_start = stream.Position + m_files.Count * entry_size;

                if (data_start_field != -1)
                {
                    // Write the data start position in the header
                    long position = stream.Position;
                    stream.Seek(data_start_field, SeekOrigin.Begin);
                    writer.Write((uint)data_start);
                    stream.Seek(position, SeekOrigin.Begin);
                }

                // Write index of file descriptors
                if (format == Format.V3)
                {
                    for (int i = 0; i != m_files.Count; ++i)
                    {
                        var info = m_files[i];

                        // Write flags for this file descriptors
                        writer.Write((ushort)(key != null ? 1 : 0));

                        byte[] file_desc = MaybeEncrypt(key, s => {
                            using (var info_writer = new BinaryWriter(s, Encoding.UTF8, true))
                            {
                                info_writer.Write(info.crc);
                                info_writer.Write(i);
                                info_writer.Write(info.size);
                                info_writer.Write((uint)data_start);
                                info_writer.Write((ushort)i);
                            }
                        });

                        stream.Write(file_desc, 0, file_desc.Length);
                        data_start += (key!= null) ? EncryptedSize((int)m_files[i].size) : (int)m_files[i].size;
                    }
                }
                else
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var info_writer = new BinaryWriter(ms, Encoding.UTF8, true))
                        {
                            for (int i = 0; i != m_files.Count; ++i)
                            {
                                var info = m_files[i];
                                info_writer.Write(info.crc);
                                info_writer.Write(i);
                                info_writer.Write(info.size);
                                info_writer.Write((uint)data_start);
                                info_writer.Write(i);

                                data_start += info.size;
                            }
                        }
                        ms.WriteTo(stream);
                    }
                }

                // Write files
                for (int i = 0; i != m_files.Count; ++i)
                {
                    if (m_files[i].size > 0)
                    {
                        MaybeEncrypt(stream, key, s => {
                            using (var source_stream = OpenFile(i))
                            {
                                source_stream.CopyTo(s);
                            }
                        });
                    }
                }
            }
        }

        int LowerBound(uint crc)
        {
            int low = 0, high = m_files.Count - 1;
            while (high >= low)
            {
                int mid = (low + high) / 2;
                if (crc == m_files[mid].crc) {
                    // We found a match, but we have to find the earliest one.
                    // So we search backward for the first similar CRC.
                    while (mid > 0 && crc == m_files[mid - 1].crc) {
                        mid--;
                    }
                    return mid;
                }
                if (crc > m_files[mid].crc) low = mid + 1;
                else high = mid - 1;
            }
            // The file was not found, but 'low' points to the earliest file
            // with a greater CRC.
            return low;
        }

        private static int EncryptedSize(int unencrypted_size)
        {
            return (unencrypted_size + AES_BLOCK_SIZE - 1) & -AES_BLOCK_SIZE;
        }

        private static void Verify(bool expr)
        {
            if (!expr)
            {
                throw new FormatException("A Mega File does not conform to the expected file format specification.");
            }
        }

        private static bool IsPrintable(byte ch)
        {
            return !Char.IsControl((char)ch) || Char.IsWhiteSpace((char)ch);
        }

        private static Aes CreateAes(EncryptionKey key)
        {
            var aes = Aes.Create();
            aes.Key = key.Key;
            aes.IV = key.InitialVector;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            return aes;
        }

        private static readonly int FILEDESC_V1_SIZE = 20;
        private static readonly int FILEDESC_V3_SIZE = 18;  // without flags

        private static FileInfo ReadFileDescV1(BinaryReader reader)
        {
            var info = new FileInfo();
            info.crc = reader.ReadUInt32();
            reader.ReadUInt32(); // skip the index
            info.size = reader.ReadUInt32();
            info.start = reader.ReadUInt32();
            info.filename = reader.ReadUInt32();
            return info;
        }

        private static FileInfo ReadFileDescV3(BinaryReader reader)
        {
            var info = new FileInfo();
            info.crc = reader.ReadUInt32();
            reader.ReadUInt32(); // skip the index
            info.size = reader.ReadUInt32();
            info.start = reader.ReadUInt32();
            info.filename = reader.ReadUInt16();
            return info;
        }

        // Writes the encrypted data provided by data_provider to the stream if key is set, otherwise writes the data unencrypted.
        private static void MaybeEncrypt(Stream stream, EncryptionKey? key, Action<Stream> data_provider)
        {
            if (key.HasValue)
            {
                using (var aes = CreateAes(key.Value))
                {
                    using (var crypto_stream = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write, true))
                    {
                        data_provider(crypto_stream);
                    }
                }
            }
            else
            {
                data_provider(stream);
            }
        }

        // Returns the encrypted data provided by data_provider if key is set, otherwise returns the data unencrypted.
        private static byte[] MaybeEncrypt(EncryptionKey? key, Action<Stream> data_provider)
        {
            using (var ms = new MemoryStream())
            {
                MaybeEncrypt(ms, key, data_provider);
                return ms.ToArray();
            }
        }

        // Calculates the 32-bit Cyclic Redundancy Checksum (CRC-32) of a block of data
        private static uint CRC32(IEnumerable<char> data)
        {
            uint crc = 0xFFFFFFFF;
            for (int j = 0; j < data.Count(); j++)
            {
                crc = ((crc >> 8) & 0x00FFFFFF) ^ lookupTable[(crc ^ (uint)data.ElementAt(j)) & 0xFF];
            }
            return crc ^ 0xFFFFFFFF;
        }

        private static uint[] lookupTable = CreateLookupTable();

        private static uint[] CreateLookupTable()
        {
            uint[] table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                {
                    crc = ((crc & 1) != 0) ? (crc >> 1) ^ 0xEDB88320 : (crc >> 1);
                }
                table[i] = crc & 0xFFFFFFFF;
            }
            return table;
        }

        private struct FileInfo
        {
            public uint crc;
            public uint start;
            public uint size;
            public uint filename;
            public Stream stream;
        };

        private static readonly int MAX_PATH = 260;
        private static readonly int AES_BLOCK_SIZE = 16; // in bytes

        private Stream m_stream;
        private EncryptionKey? m_encryption_key;
        private Format m_format;
        private List<string> m_filenames;
        private List<FileInfo> m_files;
    }
}