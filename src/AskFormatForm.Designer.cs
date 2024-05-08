namespace MegEditor
{
    partial class AskFormatForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.Label label1;
            this.formatButton1 = new System.Windows.Forms.RadioButton();
            this.formatButton2 = new System.Windows.Forms.RadioButton();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.formatButton3 = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.encryption8BitArmies = new System.Windows.Forms.RadioButton();
            this.encryptionGreyGoo = new System.Windows.Forms.RadioButton();
            this.encryptionNone = new System.Windows.Forms.RadioButton();
            this.encryptionTheGreatWarWesternFront = new System.Windows.Forms.RadioButton();
            label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 13);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(263, 13);
            label1.TabIndex = 0;
            label1.Text = "Please specify the format of the to-be saved Mega file.";
            // 
            // formatButton1
            // 
            this.formatButton1.AutoSize = true;
            this.formatButton1.Location = new System.Drawing.Point(15, 35);
            this.formatButton1.Name = "formatButton1";
            this.formatButton1.Size = new System.Drawing.Size(459, 17);
            this.formatButton1.TabIndex = 1;
            this.formatButton1.TabStop = true;
            this.formatButton1.Text = "Format 1. Used by Empire at War, Empire at War: Forces of Corruption and Universe" +
    " at War.";
            this.formatButton1.UseVisualStyleBackColor = true;
            // 
            // formatButton2
            // 
            this.formatButton2.AutoSize = true;
            this.formatButton2.Location = new System.Drawing.Point(15, 60);
            this.formatButton2.Name = "formatButton2";
            this.formatButton2.Size = new System.Drawing.Size(210, 17);
            this.formatButton2.TabIndex = 2;
            this.formatButton2.TabStop = true;
            this.formatButton2.Text = "Format 2. Used by Guardians of Graxia.";
            this.formatButton2.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(217, 223);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 3;
            this.okButton.Text = "&OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(301, 223);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 4;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // formatButton3
            // 
            this.formatButton3.AutoSize = true;
            this.formatButton3.Location = new System.Drawing.Point(15, 83);
            this.formatButton3.Name = "formatButton3";
            this.formatButton3.Size = new System.Drawing.Size(533, 17);
            this.formatButton3.TabIndex = 5;
            this.formatButton3.TabStop = true;
            this.formatButton3.Text = "Format 3. Used by Rise of Immortals: Battle for Graxia, Grey Goo, 8-Bit Armies, t" +
    "he Great War: Western Front";
            this.formatButton3.UseVisualStyleBackColor = true;
            this.formatButton3.CheckedChanged += new System.EventHandler(this.formatButton3_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.encryptionTheGreatWarWesternFront);
            this.groupBox1.Controls.Add(this.encryption8BitArmies);
            this.groupBox1.Controls.Add(this.encryptionGreyGoo);
            this.groupBox1.Controls.Add(this.encryptionNone);
            this.groupBox1.Location = new System.Drawing.Point(33, 106);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(515, 111);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Encryption Key";
            // 
            // encryption8BitArmies
            // 
            this.encryption8BitArmies.AutoSize = true;
            this.encryption8BitArmies.Location = new System.Drawing.Point(6, 65);
            this.encryption8BitArmies.Name = "encryption8BitArmies";
            this.encryption8BitArmies.Size = new System.Drawing.Size(80, 17);
            this.encryption8BitArmies.TabIndex = 8;
            this.encryption8BitArmies.TabStop = true;
            this.encryption8BitArmies.Text = "8-Bit Armies";
            this.encryption8BitArmies.UseVisualStyleBackColor = true;
            // 
            // encryptionGreyGoo
            // 
            this.encryptionGreyGoo.AutoSize = true;
            this.encryptionGreyGoo.Location = new System.Drawing.Point(6, 42);
            this.encryptionGreyGoo.Name = "encryptionGreyGoo";
            this.encryptionGreyGoo.Size = new System.Drawing.Size(70, 17);
            this.encryptionGreyGoo.TabIndex = 7;
            this.encryptionGreyGoo.TabStop = true;
            this.encryptionGreyGoo.Text = "Grey Goo";
            this.encryptionGreyGoo.UseVisualStyleBackColor = true;
            // 
            // encryptionNone
            // 
            this.encryptionNone.AutoSize = true;
            this.encryptionNone.Location = new System.Drawing.Point(6, 19);
            this.encryptionNone.Name = "encryptionNone";
            this.encryptionNone.Size = new System.Drawing.Size(51, 17);
            this.encryptionNone.TabIndex = 0;
            this.encryptionNone.TabStop = true;
            this.encryptionNone.Text = "None";
            this.encryptionNone.UseVisualStyleBackColor = true;
            // 
            // encryptionTheGreatWarWesternFront
            // 
            this.encryptionTheGreatWarWesternFront.AutoSize = true;
            this.encryptionTheGreatWarWesternFront.Location = new System.Drawing.Point(6, 88);
            this.encryptionTheGreatWarWesternFront.Name = "encryptionTheGreatWarWesternFront";
            this.encryptionTheGreatWarWesternFront.Size = new System.Drawing.Size(169, 17);
            this.encryptionTheGreatWarWesternFront.TabIndex = 9;
            this.encryptionTheGreatWarWesternFront.TabStop = true;
            this.encryptionTheGreatWarWesternFront.Text = "The Great War: Western Front";
            this.encryptionTheGreatWarWesternFront.UseVisualStyleBackColor = true;
            // 
            // AskFormatForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(563, 258);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.formatButton3);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.formatButton2);
            this.Controls.Add(this.formatButton1);
            this.Controls.Add(label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AskFormatForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Specify File Format";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton formatButton1;
        private System.Windows.Forms.RadioButton formatButton2;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.RadioButton formatButton3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton encryptionNone;
        private System.Windows.Forms.RadioButton encryptionGreyGoo;
        private System.Windows.Forms.RadioButton encryption8BitArmies;
        private System.Windows.Forms.RadioButton encryptionTheGreatWarWesternFront;
    }
}