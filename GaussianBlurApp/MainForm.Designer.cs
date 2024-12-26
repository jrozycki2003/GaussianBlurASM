using System;
using System.Reflection;

namespace GaussianBlur
{
    partial class MainForm
    {

        private void InitializeComponent()
        {
            this.loadImageButton = new System.Windows.Forms.Button();
            this.applyBlurButton = new System.Windows.Forms.Button();
            this.threadCountTrackBar = new System.Windows.Forms.TrackBar();
            this.blurAmountTrackBar = new System.Windows.Forms.TrackBar();
            this.threadCountLabel = new System.Windows.Forms.Label();
            this.blurAmountLabel = new System.Windows.Forms.Label();
            this.librarySelectorLabel = new System.Windows.Forms.Label();
            this.librarySelector = new System.Windows.Forms.ComboBox();
            this.originalImageBox = new System.Windows.Forms.PictureBox();
            this.blurredImageBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.threadCountTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.blurAmountTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.originalImageBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.blurredImageBox)).BeginInit();
            this.SuspendLayout();

            // loadImageButton
            this.loadImageButton.Location = new System.Drawing.Point(20, 20);
            this.loadImageButton.Name = "loadImageButton";
            this.loadImageButton.Size = new System.Drawing.Size(100, 30);
            this.loadImageButton.TabIndex = 0;
            this.loadImageButton.Text = "Load Image";
            this.loadImageButton.UseVisualStyleBackColor = true;
            this.loadImageButton.Click += new System.EventHandler(this.LoadImageButton_Click);

            // applyBlurButton
            this.applyBlurButton.Location = new System.Drawing.Point(140, 20);
            this.applyBlurButton.Name = "applyBlurButton";
            this.applyBlurButton.Size = new System.Drawing.Size(100, 30);
            this.applyBlurButton.TabIndex = 1;
            this.applyBlurButton.Text = "Apply Blur";
            this.applyBlurButton.UseVisualStyleBackColor = true;
            this.applyBlurButton.Click += new System.EventHandler(this.ApplyBlurButton_Click);

            // threadCountTrackBar
            this.threadCountTrackBar.Location = new System.Drawing.Point(20, 78);
            this.threadCountTrackBar.Maximum = 64;
            this.threadCountTrackBar.Minimum = 1;
            this.threadCountTrackBar.Name = "threadCountTrackBar";
            this.threadCountTrackBar.Size = new System.Drawing.Size(104, 45);
            this.threadCountTrackBar.TabIndex = 2;
            this.threadCountTrackBar.Value = 16;
            this.threadCountTrackBar.Scroll += new System.EventHandler(this.ThreadCountTrackBar_Scroll);
 
            // blurAmountTrackBar
            this.blurAmountTrackBar.Location = new System.Drawing.Point(20, 137);
            this.blurAmountTrackBar.Minimum = 1;
            this.blurAmountTrackBar.Name = "blurAmountTrackBar";
            this.blurAmountTrackBar.Size = new System.Drawing.Size(104, 45);
            this.blurAmountTrackBar.TabIndex = 4;
            this.blurAmountTrackBar.Value = 1;
            this.blurAmountTrackBar.Scroll += new System.EventHandler(this.BlurAmountTrackBar_Scroll);

            // threadCountLabel
            this.threadCountLabel.AutoSize = true;
            this.threadCountLabel.Location = new System.Drawing.Point(20, 60);
            this.threadCountLabel.Name = "threadCountLabel";
            this.threadCountLabel.Size = new System.Drawing.Size(0, 13);
            this.threadCountLabel.TabIndex = 3;
            this.threadCountLabel.Text = $"Thread Count: {this.threadCountTrackBar.Value}";

            // blurAmountLabel
            this.blurAmountLabel.AutoSize = true;
            this.blurAmountLabel.Location = new System.Drawing.Point(20, 121);
            this.blurAmountLabel.Name = "blurAmountLabel";
            this.blurAmountLabel.Size = new System.Drawing.Size(0, 13);
            this.blurAmountLabel.TabIndex = 5;
            this.blurAmountLabel.Text = $"Blur Amount: {this.blurAmountTrackBar.Value}";

            // librarySelectorLabel
            this.librarySelectorLabel.AutoSize = true;
            this.librarySelectorLabel.Location = new System.Drawing.Point(20, 274);
            this.librarySelectorLabel.Name = "librarySelectorLabel";
            this.librarySelectorLabel.Size = new System.Drawing.Size(95, 13);
            this.librarySelectorLabel.TabIndex = 6;
            this.librarySelectorLabel.Text = "Select Blur Library:";

            // librarySelector
            this.librarySelector.Location = new System.Drawing.Point(23, 299);
            this.librarySelector.Name = "librarySelector";
            this.librarySelector.Size = new System.Drawing.Size(200, 21);
            this.librarySelector.TabIndex = 7;

            // originalImageBox
            this.originalImageBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.originalImageBox.Location = new System.Drawing.Point(250, 20);
            this.originalImageBox.Name = "originalImageBox";
            this.originalImageBox.Size = new System.Drawing.Size(300, 300);
            this.originalImageBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.originalImageBox.TabIndex = 8;
            this.originalImageBox.TabStop = false;

            // blurredImageBox
            this.blurredImageBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.blurredImageBox.Location = new System.Drawing.Point(570, 20);
            this.blurredImageBox.Name = "blurredImageBox";
            this.blurredImageBox.Size = new System.Drawing.Size(300, 300);
            this.blurredImageBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.blurredImageBox.TabIndex = 9;
            this.blurredImageBox.TabStop = false;

            // MainForm
            this.ClientSize = new System.Drawing.Size(900, 350);
            this.Controls.Add(this.loadImageButton);
            this.Controls.Add(this.applyBlurButton);
            this.Controls.Add(this.threadCountTrackBar);
            this.Controls.Add(this.threadCountLabel);
            this.Controls.Add(this.blurAmountTrackBar);
            this.Controls.Add(this.blurAmountLabel);
            this.Controls.Add(this.librarySelectorLabel);
            this.Controls.Add(this.librarySelector);
            this.Controls.Add(this.originalImageBox);
            this.Controls.Add(this.blurredImageBox);
            this.Name = "MainForm";
            this.Text = "Gaussian Blur Application";
            ((System.ComponentModel.ISupportInitialize)(this.threadCountTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.blurAmountTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.originalImageBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.blurredImageBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Button loadImageButton;
        private System.Windows.Forms.Button applyBlurButton;
        private System.Windows.Forms.TrackBar threadCountTrackBar;
        private System.Windows.Forms.TrackBar blurAmountTrackBar;
        private System.Windows.Forms.Label threadCountLabel;
        private System.Windows.Forms.Label blurAmountLabel;
        private System.Windows.Forms.Label librarySelectorLabel;
        private System.Windows.Forms.ComboBox librarySelector;
        private System.Windows.Forms.PictureBox originalImageBox;
        private System.Windows.Forms.PictureBox blurredImageBox;
    }
}