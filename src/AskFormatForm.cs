using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MegEditor
{
    public partial class AskFormatForm : Form
    {
        public AskFormatForm(MegaFile.Format format, MegaFile.EncryptionKey? key)
        {
            InitializeComponent();
            RadioButton selected = null;
            switch (format)
            {
                case MegaFile.Format.V1: selected = formatButton1; break;
                case MegaFile.Format.V2: selected = formatButton2; break;
                case MegaFile.Format.V3: selected = formatButton3; break;
            }
            
            if (selected != null)
            {
                selected.Select();
            }

            encryptionNone.Select();
            if (key == null)
            {
                encryptionNone.Select();
            }
            else if (key.Value.Equals(EncryptionKeys.GreyGoo))
            {
                encryptionGreyGoo.Select();
            }
            else if (key.Value.Equals(EncryptionKeys.EightBitArmies))
            {
                encryption8BitArmies.Select();
            }
            else if (key.Value.Equals(EncryptionKeys.TheGreatWarWesternFront))
            {
                encryptionTheGreatWarWesternFront.Select();
            }

            updateEncryptionEnabled();
        }

        public MegaFile.Format GetSelectedFormat()
        {
            if (formatButton3.Checked) return MegaFile.Format.V3;
            if (formatButton2.Checked) return MegaFile.Format.V2;
            return MegaFile.Format.V1;
        }

        public MegaFile.EncryptionKey? GetEncryptionKey()
        {
            if (encryptionGreyGoo.Checked) return EncryptionKeys.GreyGoo;
            if (encryption8BitArmies.Checked) return EncryptionKeys.EightBitArmies;
            if (encryptionTheGreatWarWesternFront.Checked) return EncryptionKeys.TheGreatWarWesternFront;
            return null;
        }

        private void formatButton3_CheckedChanged(object sender, EventArgs e)
        {
            updateEncryptionEnabled();
        }

        private void updateEncryptionEnabled()
        {
            encryptionNone.Enabled = encryptionGreyGoo.Enabled = encryption8BitArmies.Enabled = encryptionTheGreatWarWesternFront.Enabled = formatButton3.Checked;
        }
    }
}
