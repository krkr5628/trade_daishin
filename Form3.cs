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
using System.Text.RegularExpressions;


namespace WindowsFormsApp1
{
    public partial class Transaction : Form
    {
        public Transaction()
        {
            InitializeComponent();
            initial_Table();
            start();
        }

        //실시간 조건 검색 용 테이블(누적 저장)
        private DataTable Trade_History = new DataTable();

        private void initial_Table()
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("시각", typeof(string));
            dataTable.Columns.Add("구분", typeof(string));
            dataTable.Columns.Add("상태", typeof(string)); // '매수' '매도'
            dataTable.Columns.Add("종목코드", typeof(string));
            dataTable.Columns.Add("종목명", typeof(string));
            dataTable.Columns.Add("거래량", typeof(string));
            dataTable.Columns.Add("편입가", typeof(string));
            Trade_History = dataTable;
            dataGridView1.DataSource = Trade_History;
        }

        private void start()
        {
            // 파일이 있는 폴더 경로
            string folderPath = @"C:\Auto_Trade_Creon\Log_Trade";

            // 해당 폴더의 모든 파일을 가져오기
            string[] files = Directory.GetFiles(folderPath).OrderByDescending(file => file).ToArray();

            if (files.Length == 0)
            {
                MessageBox.Show("파일이 없습니다.");
            }

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
            string folderPath = @"C:\Auto_Trade_Creon\Log_Trade\";

            try
            {
                // 파일 열기
                using (StreamReader reader = new StreamReader(folderPath + listBox1.SelectedItem.ToString()))
                {
                    Trade_History.Clear();
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("정상완료"))
                        {
                            string pattern = @"\[(.*?)\]\[Order\] : \[(.*?)/(.*?)/(.*?)\] : (.*?)\((.*?)\) (\d+)개 ([\d,]+)원";
                            Match match = Regex.Match(line, pattern);
                            //
                            if (match.Success)
                            {
                                Trade_History.Rows.Add(
                                    match.Groups[1].Value, // 11:05:02
                                    match.Groups[4].Value, // 01
                                    match.Groups[2].Value.Substring(0, 2), // 매수
                                    match.Groups[6].Value, // A083450
                                    match.Groups[5].Value, // GST
                                    match.Groups[7].Value, // 851
                                    match.Groups[8].Value.Replace(",", "") // 49,148 -> 49148
                                );

                                Trade_History.AcceptChanges();
                                dataGridView1.DataSource = Trade_History;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 읽기 중 오류 발생: " + ex.Message);
            }
        }
    }
}
