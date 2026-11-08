using System.ComponentModel;

namespace CinemaManagement.UI.Forms.Auth;

partial class frmChangePassword
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;

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
        button1 = new System.Windows.Forms.Button();
        label1 = new System.Windows.Forms.Label();
        textBox1 = new System.Windows.Forms.TextBox();
        SuspendLayout();
        // 
        // button1
        // 
        button1.Location = new System.Drawing.Point(60, 224);
        button1.Name = "button1";
        button1.Size = new System.Drawing.Size(97, 38);
        button1.TabIndex = 0;
        button1.Text = "button1";
        button1.UseVisualStyleBackColor = true;
        // 
        // label1
        // 
        label1.Location = new System.Drawing.Point(61, 328);
        label1.Name = "label1";
        label1.Size = new System.Drawing.Size(95, 43);
        label1.TabIndex = 1;
        label1.Text = "label1";
        // 
        // textBox1
        // 
        textBox1.Location = new System.Drawing.Point(80, 134);
        textBox1.Name = "textBox1";
        textBox1.Size = new System.Drawing.Size(138, 27);
        textBox1.TabIndex = 2;
        // 
        // frmChangePassword
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(800, 450);
        Controls.Add(textBox1);
        Controls.Add(label1);
        Controls.Add(button1);
        Text = "frmChangePassword";
        Load += frmChangePassword_Load;
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox textBox1;

    #endregion
}