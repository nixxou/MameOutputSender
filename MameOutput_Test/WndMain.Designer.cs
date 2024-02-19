namespace MameOutput_Test
{
    partial class WndMain
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

		#region Code généré par le Concepteur Windows Form

		/// <summary>
		/// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
		/// le contenu de cette méthode avec l'éditeur de code.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			timer1 = new System.Windows.Forms.Timer(components);
			timer2 = new System.Windows.Forms.Timer(components);
			SuspendLayout();
			// 
			// timer1
			// 
			timer1.Interval = 2000;
			timer1.Tick += timer1_Tick;
			// 
			// timer2
			// 
			timer2.Interval = 1000;
			timer2.Tick += timer2_Tick;
			// 
			// WndMain
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(762, 620);
			Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			Name = "WndMain";
			StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			Text = "Form1";
			FormClosing += WndMain_FormClosing;
			Load += WndMain_Load;
			ResumeLayout(false);
		}

		#endregion
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.Timer timer2;
	}
}

