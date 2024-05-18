using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApp1
{
    public partial class Log : Form
    {
        public Log()
        {
            InitializeComponent();
            //
            start();
        }
        
        private void start()
        {
            // 파일이 있는 폴더 경로
            string folderPath = @"C:\Auto_Trade_Creon\Log";

            // 해당 폴더의 모든 파일을 가져오기
            string[] files = Directory.GetFiles(folderPath).OrderByDescending(file => file).ToArray();

            // 파일명 출력
            foreach (string file in files)
            {
                listBox1.Items.Add(Path.GetFileName(file));
            }

            //
            listBox1.SelectedIndexChanged += read;
        }

        private void read(object sender, EventArgs e)
        {
            // 파일이 있는 폴더 경로
            string folderPath = @"C:\Auto_Trade_Creon\Log\";

            try
            {
                // 파일 열기
                using (StreamReader reader = new StreamReader(folderPath + listBox1.SelectedItem.ToString()))
                {
                    // 파일 내용 읽기
                    string content = reader.ReadToEnd();

                    // 파일 내용 출력
                    richTextBox1.Clear();
                    richTextBox1.AppendText(content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 읽기 중 오류 발생: " + ex.Message);
            }
        }
    }
}
