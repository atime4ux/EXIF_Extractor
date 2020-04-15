using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace EXIF_Extractor
{
    public partial class Form2 : Form
    {
        DataTable DT;

        public Form2()
        {
            InitializeComponent();
        }

        public Form2(DataTable dt)
        {
            InitializeComponent();
            this.DT = dt;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.DataSource = this.DT;
        }
    }
}
