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
    public partial class Update : Form
    {
        public Update()
        {
            InitializeComponent();
            //
            read();

            //인증 기능 추가

        }

        public static bool Authentication_Check = false;

        private void Authentication()
        {
            //인증코드확인

            //인증성공
            //Authentication_Check = true;
        }

        private void read()
        {
            // 파일이 있는 폴더 경로
            string folderPath = @"C:\Auto_Trade_Creon\Update\Agreement.txt";
            string folderPath2 = @"C:\Auto_Trade_Creon\Update\Update.txt";

            try
            {
                // 파일 열기
                using (StreamReader reader = new StreamReader(folderPath))
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

            try
            {
                // 파일 열기
                using (StreamReader reader = new StreamReader(folderPath2))
                {
                    // 파일 내용 읽기
                    string content = reader.ReadToEnd();

                    // 파일 내용 출력
                    richTextBox2.Clear();
                    richTextBox2.AppendText(content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 읽기 중 오류 발생: " + ex.Message);
            }
        }
    }
}
