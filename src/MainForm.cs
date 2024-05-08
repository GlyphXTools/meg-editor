using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace MegEditor
{
    public partial class MainForm : Form
    {
        private struct EncryptionKeyInfo
        {
            public MegaFile.EncryptionKey? Key;
            public string Description;
        };

        private static readonly EncryptionKeyInfo[] ENCRYPTION_KEYS = {
            new EncryptionKeyInfo { Key = null, Description = "None" },
            new EncryptionKeyInfo { Key = EncryptionKeys.GreyGoo, Description = "Grey Goo" },
            new EncryptionKeyInfo { Key = EncryptionKeys.EightBitArmies, Description = "8-Bit Armies" },
            new EncryptionKeyInfo { Key = EncryptionKeys.TheGreatWarWesternFront, Description = "The Great War: Western Front" },
        };

        private string m_path;
        private bool m_changed;
        private bool m_readonly;
        private MegaFile m_megafile;
        private MegaFile.EncryptionKey? m_encryptionKey;

        private static string FindEncryptionKeyDescription(MegaFile.EncryptionKey? key)
        {
            foreach (EncryptionKeyInfo info in ENCRYPTION_KEYS)
            {
                if (info.Key.Equals(key))
                {
                    return info.Description;
                }
            }
            return null;
        }

        private static bool SupportsEncryption(MegaFile.Format format)
        {
            switch (format)
            {
                case MegaFile.Format.V3:
                    return true;
            }
            return false;
        }

        public MainForm()
        {
            m_path = "";
            m_megafile = null;
            m_changed = false;
            m_readonly = false;

            InitializeComponent();

            string[] args = System.Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1] != null)
            {
                // Argument specified on command line
                if (File.Exists(args[1]))
                {
                    // A valid file has been specified, open it.
                    if (OpenMegaFile(args[1]))
                    {
                        OnFileChanged();
                    }
                }
            }
        }

        private void UpdateTitle()
        {
            string title = this.Text;
            int ofs = title.IndexOf(" - ");
            if (ofs != -1)
            {
                title = title.Substring(0, ofs);
            }
            if (m_megafile != null)
            {
                title += " - [" + (m_path != "" ? m_path : "Unnamed") + "]";
                if (m_changed)
                {
                    title += "*";
                }
                if (m_readonly)
                {
                    title += " (Read-only)";
                }
            }
            this.Text = title;
        }

        private void OnFileChanged()
        {
            if (m_megafile == null)
            {
                // Clear and hide
                this.splitContainer1.Hide();
                this.filelist.Items.Clear();
                this.fileCountLabel.Text = "No File";
                this.fileFormatLabel.Text = "";
                this.encryptionKeyLabel.Text = "";
                this.saveToolStripButton.Enabled = false;
                m_path = "";
            }
            else
            {
                String oldSelectedFile = (this.filelist.SelectedItems.Count > 0) ? this.filelist.SelectedItems[0].Text : null;
                int oldSelectedIndex = (this.filelist.SelectedIndices.Count > 0) ? this.filelist.SelectedIndices[0] : -1;

                // Fill and show
                NumberFormatInfo nfi = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
                nfi.NumberDecimalDigits = 0;

                var files = m_megafile.GetFiles();
                ListViewItem[] items = new ListViewItem[files.Count];
                for (int i = 0; i < files.Count; ++i)
                {
                    items[i] = new ListViewItem(files.ElementAt(i));
                    items[i].SubItems.Add(((int?)m_megafile.GetFileSize(files.ElementAt(i)) ?? -1).ToString("N", nfi));
                }

                this.filelist.Items.Clear();
                this.filelist.Items.AddRange(items);

                // Show number of files in the Mega File
                String fileCountText = files.Count.ToString() + " file";
                if (files.Count != 1)
                {
                    fileCountText += "s";
                }
                this.fileCountLabel.Text = fileCountText;

                // Show encryption key
                string encKeyDesc = FindEncryptionKeyDescription(m_encryptionKey);
                this.encryptionKeyLabel.Text = "Encryption: " + (encKeyDesc != null ? encKeyDesc : "None");

                // Show file format
                switch (m_megafile.GetFormat())
                {
                    default: this.fileFormatLabel.Text = ""; break;
                    case MegaFile.Format.V1: this.fileFormatLabel.Text = "Format 1"; break;
                    case MegaFile.Format.V2: this.fileFormatLabel.Text = "Format 2"; break;
                    case MegaFile.Format.V3: this.fileFormatLabel.Text = "Format 3"; break;
                }

                this.splitContainer1.Show();
                this.saveToolStripButton.Enabled = m_changed && !m_readonly;
            }

            OnSelectionChanged();

            // Adjust title
            UpdateTitle();
        }

        private string GetUniquePath(string basepath)
        {
            for (int i = 0;; ++i)
            {
                string path = basepath + ".tmp" + i;
                if (!File.Exists(basepath + i))
                {
                    return path;
                }
            }
        }

        private bool OpenMegaFile(string path)
        {
            try
            {
                // Check if we can write to the file
                m_readonly = true;
                if (File.GetAttributes(path) != FileAttributes.ReadOnly)
                {
                    try
                    {
                        Stream s = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.None);
                        s.Close();
                        m_readonly = false;
                    }
                    catch (Exception)
                    {
                    }
                }

                // Now open the file.
                // Try every encryption key we know.
                Exception exception = null;
                foreach (EncryptionKeyInfo keyInfo in ENCRYPTION_KEYS)
                {
                    try
                    {
                        Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                        m_megafile = new MegaFile(stream, keyInfo.Key);
                        m_encryptionKey = keyInfo.Key;
                        break;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }

                if (m_megafile == null)
                {
                    // We couldn't open it; re-throw the last exception
                    throw exception;
                }

                m_path = path;
                m_changed = false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open Mega file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        private bool DoOpen(String path)
        {
            if (path == null)
            {
                // Ask which file to open
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.DefaultExt = "meg";
                ofd.Filter = "Mega files (*.meg)|*.MEG|All files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.Cancel)
                {
                    return false;
                }
                DoClose();
                path = ofd.FileName;
            }

            return OpenMegaFile(path);
        }

        private bool DoSave(bool ask_details)
        {
            string          new_path = m_path;
            MegaFile.Format format   = m_megafile.GetFormat();
            MegaFile.EncryptionKey? encryptionKey = m_encryptionKey;

            if (ask_details || m_readonly || new_path == "")
            {
                // Ask for the path where to save
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName   = Path.GetFileName(new_path);
                sfd.DefaultExt = "meg";
                sfd.Filter = "Mega files (*.meg)|*.MEG|All files (*.*)|*.*";
                if (sfd.ShowDialog() == DialogResult.Cancel)
                {
                    return false;
                }

                // Ask for the format
                AskFormatForm f = new AskFormatForm(format, m_encryptionKey);
                if (f.ShowDialog() == DialogResult.Cancel)
                {
                    return false;
                }
                format = f.GetSelectedFormat();
                encryptionKey = SupportsEncryption(format) ? f.GetEncryptionKey() : null;

                new_path = sfd.FileName;
            }

            // Write the file by closing it
            try
            {
                if (new_path == m_path)
                {
                    // We can't write to the original file, so write to a copy, then replace
                    string tmppath = GetUniquePath(m_path);
                    try
                    {
                        using (Stream s = File.Open(tmppath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            File.SetAttributes(tmppath, FileAttributes.Hidden);
                            m_megafile.Save(s, format, encryptionKey);
                        }

                        String path = m_path;
                        DoClose();

                        File.Delete(path);
                        File.Move(tmppath, path);
                        File.SetAttributes(path, FileAttributes.Normal);
                    }
                    catch (Exception ex)
                    {
                        // Something went wrong, delete the temporary file
                        try 
                        {
                            File.Delete(tmppath);
                        }
                        catch (Exception)
                        {

                        }
                        throw ex;
                    }
                }
                else
                {
                    using (Stream s = File.Open(new_path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        m_megafile.Save(s, format, encryptionKey);
                    }
                    DoClose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to save Mega file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Re-open the file
            DoOpen(new_path);
            return true;
        }

        private bool CanClose()
        {
            if (m_changed)
            {
                // Ask if the file should be saved
                DialogResult result = MessageBox.Show("The file has been changed. Save changes?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                switch (result)
                {
                    case DialogResult.Yes:    return DoSave(false);
                    case DialogResult.No:     return true;
                    case DialogResult.Cancel: return false;
                }
            }
            return true;
        }

        private void DoClose()
        {
            if (m_megafile != null)
            {
                m_megafile.Dispose();
                m_megafile = null;
            }
            m_path = "";
            m_changed = false;
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CanClose())
            {
                DoClose();

                m_megafile = new MegaFile();
                m_path = "";
                m_changed = false;
                m_readonly = false;

                OnFileChanged();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CanClose())
            {
                if (DoOpen(null))
                {
                    OnFileChanged();
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_changed)
            {
                DoSave(false);
                OnFileChanged();
            }
        }

        private void saveasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoSave(true);
            OnFileChanged();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CanClose())
            {
                DoClose();
                OnFileChanged();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void deleteFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            deleteSelectedItems();
        }

        private void deleteSelectedItems()
        {
            if (this.filelist.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in this.filelist.SelectedItems)
                {
                    m_megafile.DeleteFile(item.Text);
                }
                m_changed = true;
                OnFileChanged();
            }
        }

        private void renameFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.filelist.SelectedItems[0].BeginEdit();
        }

        private void insertFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.filelist.BeginUpdate();
                Cursor old_cursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;
                try
                {
                    foreach (string filename in ofd.FileNames)
                    {
                        Stream s = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                        m_megafile.InsertFile(filename, s);
                        m_changed = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to open file \"" + filename + "\":\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                OnFileChanged();
                this.filelist.EndUpdate();
                this.Cursor = old_cursor;
            }
        }

        private string FormatBytes(long size)
        {
            string[] orders = new string[] { "Bytes", "KB", "MB", "GB" };
            int order = (int)Math.Floor(Math.Log(size, 2) / 10);
            return string.Format("{0:#.##} {1}", size / Math.Pow(2, order * 10), orders[order]);
        }

        private void insertDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Please select the directory to insert into the Mega file. Existing files will not be overwritten.";
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                // Enumerate the files
                Cursor old_cursor = this.Cursor;
                try
                {
                    string basepath = fbd.SelectedPath;
                    string[] filenames = Directory.GetFiles(basepath, "*.*", SearchOption.AllDirectories);
                    long total_size = 0;
                    foreach (string filename in filenames)
                    {
                        total_size += (new FileInfo(filename)).Length;
                    }

                    basepath = Path.GetDirectoryName(basepath);

                    if (MessageBox.Show("Adding " + FormatBytes(total_size) + " from " + filenames.Length + " files. Continue?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        this.Cursor = Cursors.WaitCursor;
                        this.filelist.BeginUpdate();
                        foreach (string filename in filenames)
                        {
                            Stream s = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                            m_megafile.InsertFile(filename.Substring(basepath.Length + 1), s);
                        }
                        m_changed = true;
                        OnFileChanged();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to open file \"" + filename + "\":\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    this.filelist.EndUpdate();
                    this.Cursor = old_cursor;
                }
            }
        }

        private void filelist_SelectedIndexChanged(object sender, EventArgs e)
        {
            OnSelectionChanged();
        }

        private static Encoding GetStreamEncoding(Stream s)
        {
            // Detect byte order mark if any - otherwise assume default
            byte[] buffer = new byte[4];
            long pos = s.Position;
            if (s.Length - pos >= 4)
            {
                int n = s.Read(buffer, 0, 4);

                if (n >= 3 && buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf) { s.Position = 3; return Encoding.UTF8; }
                if (n >= 2 && buffer[0] == 0xfe && buffer[1] == 0xff) { s.Position = 2; return Encoding.Unicode; }
                if (n >= 4 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xfe && buffer[3] == 0xff) { s.Position = 4; return Encoding.UTF32; }
                if (n >= 3 && buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76) { s.Position = 3; return Encoding.UTF7; }

                s.Position = pos;
            }
            return Encoding.Default;
        }

        private void OnSelectionChanged()
        {
            // Show the selected file
            string text = "";
            if (this.filelist.SelectedItems.Count > 0)
            {
                try
                {
                    Stream stream = m_megafile.OpenFile(this.filelist.SelectedItems[0].Text);
                    if (stream != null && stream.Length > 0)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            text = reader.ReadToEnd();
                        }
                        
                            //Encoding enc = GetStreamEncoding(stream);
                        //byte[] data = new byte[stream.Length];
                        //int n = stream.Read(data, 0, (int)stream.Length);
                        //text = enc.GetString(data);
                    }
                }
                catch (DecoderFallbackException)
                {
                    // Just ignore the error and don't show anything
                    text = "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to read the selected file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            this.richTextBox1.Text = text;
            this.richTextBox1.Visible = (text.Length > 0);
        }

        private void menuStrip1_MenuActivate(object sender, EventArgs e)
        {
            // Set the state of all menu items
            this.saveToolStripMenuItem.Enabled            = (m_megafile != null) && m_changed && !m_readonly;
            this.saveasToolStripMenuItem.Enabled          = (m_megafile != null);
            this.closeToolStripMenuItem.Enabled           = (m_megafile != null);
            this.insertFilesToolStripMenuItem.Enabled     = (m_megafile != null);
            this.insertDirectoryToolStripMenuItem.Enabled = (m_megafile != null);
            this.extractFilesToolStripMenuItem.Enabled    = (m_megafile != null) && (this.filelist.Items.Count > 0);
            this.deleteFilesToolStripMenuItem.Enabled     = (m_megafile != null) && (this.filelist.SelectedItems.Count > 0);
            this.renameFileToolStripMenuItem.Enabled      = (m_megafile != null) && (this.filelist.SelectedItems.Count == 1);
        }

        private void extractFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Compose list of files to extract
            List<String> items = new List<String>();
            if (this.filelist.SelectedItems.Count == 0)
            {
                foreach (ListViewItem item in this.filelist.Items)
                {
                    items.Add(item.Text);
                }
            }
            else
            {
                foreach (ListViewItem item in this.filelist.SelectedItems)
                {
                    items.Add(item.Text);
                }
            }

            try
            {
                // Extract files
                if (items.Count == 1)
                {
                    // We're extracting a single file
                    String extension = Path.GetExtension(items[0]);
                    if (extension != "")
                    {
                        extension = extension.Substring(1).ToUpper();
                    }

                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.FileName = Path.GetFileName(items[0]);
                    sfd.DefaultExt = extension;
                    sfd.Filter = extension + " files (*." + extension.ToLower() + ")|*." + extension + "|All files (*.*)|*.*";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        Stream dest = File.Open(sfd.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                        Stream src = m_megafile.OpenFile(items[0]);
                        byte[] data = new byte[src.Length];
                        int n = src.Read(data, 0, (int)src.Length);
                        src.Close();
                        dest.Write(data, 0, n);
                        dest.Close();
                    }
                }
                else if (items.Count > 0)
                {
                    // We're extracting multiple files. Ask for the directory.
                    FolderBrowserDialog fbd = new FolderBrowserDialog();
                    fbd.Description = "Please select the folder to extract the files into. Existing files will NOT be overwritten.";
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        // Change cursor to show that we're busy
                        Cursor old_cursor = this.Cursor;
                        this.Cursor = Cursors.WaitCursor;
                        int skipped = 0;
                        try
                        {
                            // Extract the files
                            String basepath = fbd.SelectedPath;
                            foreach (String item in items)
                            {
                                String path = Path.Combine(basepath, item);
                                if (!File.Exists(path))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                                    Stream dest = File.Open(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                                    Stream src = m_megafile.OpenFile(item);
                                    byte[] data = new byte[src.Length];
                                    int n = src.Read(data, 0, (int)src.Length);
                                    src.Close();
                                    dest.Write(data, 0, n);
                                    dest.Close();
                                }
                                else
                                {
                                    skipped++;
                                }
                            }
                        }
                        finally
                        {
                            // Restore cursor
                            this.Cursor = old_cursor;
                        }

                        // Notify of success
                        String message = (items.Count - skipped) + " files extracted!";
                        if (skipped != 0)
                        {
                            message += "\n\n(" + skipped + " files skipped)";
                        }
                        MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to extract files:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private delegate void SortFileListDelegate();

        private void SortFileList()
        {
            this.filelist.Sort();
        }

        private void filelist_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label != null)
            {
                if (!m_megafile.RenameFile(this.filelist.Items[e.Item].Text, e.Label))
                {
                    e.CancelEdit = true;
                }
                else
                {
                    this.BeginInvoke(new SortFileListDelegate(SortFileList));
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!CanClose())
            {
                e.Cancel = true;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.ShowDialog();
        }

        private void filelist_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.extractFilesToolStripMenuItem1.Enabled = (this.filelist.Items.Count > 0);
                this.renameFileToolStripMenuItem1.Enabled   = (this.filelist.SelectedItems.Count == 1);
                this.deleteFilesToolStripMenuItem1.Enabled  = (this.filelist.SelectedItems.Count >  0);

                this.fileContextMenu.Show(this.filelist, e.X, e.Y);
            }
        }

        private void filelist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                deleteSelectedItems();
            }
        }
    }
}
