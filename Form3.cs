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
        private DataTable Trade_History2 = new DataTable();

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

            DataTable dataTable2 = new DataTable();
            dataTable2.Columns.Add("구분", typeof(string));       
            dataTable2.Columns.Add("종목코드", typeof(string));
            dataTable2.Columns.Add("종목명", typeof(string));
            dataTable2.Columns.Add("총매수(개)", typeof(string));
            dataTable2.Columns.Add("총매수(원)", typeof(string));
            dataTable2.Columns.Add("총매도(개)", typeof(string));
            dataTable2.Columns.Add("총매도(원)", typeof(string));
            dataTable2.Columns.Add("총수익(원)", typeof(string));
            dataTable2.Columns.Add("총수익(%)", typeof(string));
            Trade_History2 = dataTable2;
            dataGridView2.DataSource = Trade_History2;
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
                listBox1.Items.Add(Path.GetFileName(file).Replace("_trade.txt",""));
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
                using (StreamReader reader = new StreamReader(folderPath + listBox1.SelectedItem.ToString() + "_trade.txt"))
                {
                    Trade_History.Clear();
                    Trade_History2.Clear();
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

                                /*
                                    dataTable2.Columns.Add("구분", typeof(string));       
                                    dataTable2.Columns.Add("종목코드", typeof(string));
                                    dataTable2.Columns.Add("종목명", typeof(string));
                                    dataTable2.Columns.Add("매수시간", typeof(string));
                                    dataTable2.Columns.Add("총매수(개)", typeof(string));
                                    dataTable2.Columns.Add("총매수(원)", typeof(string));
                                    dataTable2.Columns.Add("매도시간", typeof(string));
                                    dataTable2.Columns.Add("총매도(개)", typeof(string));
                                    dataTable2.Columns.Add("총매도(원)", typeof(string));
                                    dataTable2.Columns.Add("총수익(원)", typeof(string));
                                    dataTable2.Columns.Add("총수익(%)", typeof(string));
                                */
                                //
                                if (match.Groups[2].Value.Substring(0, 2).Equals("매수"))
                                {
                                    DataRow[] findRows = Trade_History2.AsEnumerable().Where(row => row.Field<string>("구분") == match.Groups[4].Value && row.Field<string>("종목코드") == match.Groups[6].Value).ToArray();

                                    if (findRows.Any())
                                    {
                                        Int64 tmp_cnt = findRows[0]["총매수(개)"].ToString() == "" ? 0 : Convert.ToInt64(findRows[0]["총매수(개)"]);
                                        Int64 tmp_won = findRows[0]["총매수(원)"].ToString() == "" ? 0 : Convert.ToInt64(findRows[0]["총매수(원)"]);
                                        Int64 tmp_sell_won = findRows[0]["총매도(원)"].ToString() == "" ? 0 : Convert.ToInt64(findRows[0]["총매도(원)"]);

                                        Int64 tmp = tmp_won + Convert.ToInt64(match.Groups[7].Value) * Convert.ToInt64(match.Groups[8].Value.Replace(",", ""));
                                        Int64 tmp2 = tmp_sell_won - tmp;

                                        findRows[0]["총매수(개)"] = Convert.ToString(tmp_cnt + Convert.ToInt64(match.Groups[7].Value));
                                        findRows[0]["총매수(원)"] = Convert.ToString(tmp);
                                        findRows[0]["총수익(원)"] = Convert.ToString(tmp2);
                                        findRows[0]["총수익(%)"] = Convert.ToString(Math.Round((double)tmp2 / tmp * 100, 3));

                                        Trade_History2.AcceptChanges();
                                        dataGridView2.DataSource = Trade_History2;
                                    }
                                    else
                                    {
                                        Trade_History2.Rows.Add(
                                            match.Groups[4].Value, // 01
                                            match.Groups[6].Value, // A083450
                                            match.Groups[5].Value, // GST
                                            match.Groups[7].Value, // 851
                                            Convert.ToString(Convert.ToInt64(match.Groups[7].Value) * Convert.ToInt64(match.Groups[8].Value.Replace(",", ""))),
                                            "", //매도(개)
                                            "", //매도(원)
                                            "", //총수익(원)
                                            "" //총수익(%)
                                        );

                                        Trade_History2.AcceptChanges();
                                        dataGridView2.DataSource = Trade_History2;
                                    }
                                }
                                else
                                {
                                    DataRow[] findRows = Trade_History2.AsEnumerable().Where(row => row.Field<string>("구분") == match.Groups[4].Value && row.Field<string>("종목코드") == match.Groups[6].Value).ToArray();

                                    if (findRows.Any())
                                    {
                                        int tmp_cnt = findRows[0]["총매도(개)"].ToString() == "" ? 0 : Convert.ToInt32(findRows[0]["총매도(개)"]);
                                        int tmp_won = findRows[0]["총매도(원)"].ToString() == "" ? 0 : Convert.ToInt32(findRows[0]["총매도(원)"]);
                                        int tmp_buy_won = findRows[0]["총매수(원)"].ToString() == "" ? 0 : Convert.ToInt32(findRows[0]["총매수(원)"]);

                                        int tmp = tmp_won + Convert.ToInt32(match.Groups[7].Value) * Convert.ToInt32(match.Groups[8].Value.Replace(",", ""));
                                        int tmp2 = tmp - tmp_buy_won;

                                        findRows[0]["총매도(개)"] = Convert.ToString(tmp_cnt + Convert.ToInt64(match.Groups[7].Value));
                                        findRows[0]["총매도(원)"] = Convert.ToString(tmp);
                                        findRows[0]["총수익(원)"] = Convert.ToString(tmp2);
                                        findRows[0]["총수익(%)"] = tmp_buy_won == 0 ? "0" : Convert.ToString(Math.Round((double)tmp2 / tmp_buy_won * 100, 3));

                                        Trade_History2.AcceptChanges();
                                        dataGridView2.DataSource = Trade_History2;
                                    }
                                    else
                                    {
                                        Trade_History2.Rows.Add(
                                            match.Groups[4].Value, // 01
                                            match.Groups[6].Value, // A083450
                                            match.Groups[5].Value, // GST
                                            "", //매수(개)
                                            "", //매수(원)
                                            match.Groups[7].Value, // 851
                                            Convert.ToString(Convert.ToInt64(match.Groups[7].Value) * Convert.ToInt64(match.Groups[8].Value.Replace(",", ""))),
                                            "", //총수익(원)
                                            "" //총수익(%)
                                        );

                                        Trade_History2.AcceptChanges();
                                        dataGridView2.DataSource = Trade_History2;
                                    }
                                }
                            }
                        }
                    }
                    reader.Close();
                }
                //

            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 읽기 중 오류 발생: " + ex.Message);
            }
        }
    }
}
