using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
//
using System.IO;
using System.Collections;
using System.Timers;
using System.Threading;
//
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Principal;
using Newtonsoft.Json.Linq;
//
using CPUTILLib; //Daishin
using CPTRADELib; //Daishin

namespace WindowsFormsApp1
{
    public partial class Trade_Auto_Daishin : Form
    {
        //-----------------------------------공통 Obj----------------------------------------

        private CPUTILLib.CpCybos CpCybos; //?
        private CPUTILLib.CpStockCode CpStockCode; //?
        private CPUTILLib.CpCodeMgr CpCodeMgr; //?
        private CPSYSDIBLib.CpSvrNew7224 CpSvrNew7224; //외국인선물
        private CPUTILLib.CpFutureCode cpFuture; //코스피선물옵션
        private CPUTILLib.CpKFutureCode cpKFuture; //코스닥선물옵션
        private CPSYSDIBLib.FutOptChart FutOptChart; //선물옵션차트
        private CPUTILLib.CpUsCode CpUsCode; //해외지수 목록
        private DSCBO1Lib.CpFore8312 CpFore8312; //해외지수 수신
        private CPTRADELib.CpTdUtil CpTdUtil; //?
        private CPTRADELib.CpTd6033 CpTd6033; //계좌별 D+2 예수금
        private CPTRADELib.CpTdNew5331B CpTdNew5331B;//계좌별 매도 가능 수량
        private CPTRADELib.CpTd6032 CpTd6032; //매도실현손익(제세금, 수수료 포함)
        private CPTRADELib.CpTd5341 CpTd5341; //매매내역
        private CPSYSDIBLib.CssStgList CssStgList; //조건식 받기
        //private CPSYSDIBLib.CssWatchStgControl CssWatchStgControl; // 실시간 조건식 등록 및 해제
        private CPSYSDIBLib.CssStgFind CssStgFind; //초기 종목 검색 리스트
        private CPSYSDIBLib.MarketEye MarketEye; //초기 종목 검색 정보
        private CPSYSDIBLib.CssWatchStgSubscribe CssWatchStgSubscribe; // 일련번호 받기
        private CPSYSDIBLib.CssAlert CssAlert; // 종목 편출입
        private DSCBO1Lib.StockCur StockCur; // 실시간 종목 시세(관심종목)
        private CPTRADELib.CpTd0311 CpTd0311; //현금주문
        private CPTRADELib.CpTd0322 CpTd0322; //시간외종가
        private CPTRADELib.CpTd0386 CpTd0386; //시간외단일가
        private DSCBO1Lib.CpConclusion CpConclusion; //실시간 체결 내역
        private CPTRADELib.CpTd0326 CpTd0326; //시간외종가 취소주문
        private CPTRADELib.CpTd0387 CpTd0387; //시간외단일가 취소주문
        private CPTRADELib.CpTd0314 CpTd0314; //정규장 취소주문

        //-----------------------------------공용 신호----------------------------------------

        public static string[] arrCondition = { };
        public static string[] account;
        public int login_check = 1;

        //-----------------------------------전용 신호----------------------------------------

        private string Master_code = "01";
        //private string Master_code = "10"; //TEST용
        private string ISA_code = "11";
        private bool Checked_Trade_Init; //?
        private string ISA_Condition = "";

        //-----------------------------------인증 관련 신호----------------------------------------

        public static string Authentication = "1ab2c3d4e5f6g7h8i9"; //미인증(false) / 인증(true)
        public static bool Authentication_Check = true; //미인증(false) / 인증(true)
        private int sample_balance = 500000; //500,000원(미인증 매매 금액 제한)

        //-----------------------------------storage----------------------------------------

        //telegram용 초당 1회 전송 저장소
        //private Queue<String> telegram_chat = new Queue<string>();

        //매매로그 맟 전체로그 저장
        private List<string> log_trade = new List<string>();
        private List<string> log_full = new List<string>();

        //실시간 조건 검색 용 테이블(누적 저장)
        private DataTable dtCondStock = new DataTable();

        //실시간 계좌 보유 현황 용 테이블(누적 저장)
        private DataTable dtCondStock_hold = new DataTable();

        //
        private DataTable dtCondStock_Transaction = new DataTable();

        //-----------------------------------lock---------------------------------------- 
        // 락 객체 생성
        private object buy_lock = new object();
        private object sell_lock = new object();

        private List<Tuple<string, string>> waiting_Codes = new List<Tuple<string, string>>();
        private Dictionary<string, bool> buy_runningCodes = new Dictionary<string, bool>();
        private Dictionary<string, bool> sell_runningCodes = new Dictionary<string, bool>();

        //------------------------------기본 BUTTON 모음-------------------------------------

        //main menu 실행
        private void main_menu(object sender, EventArgs e)
        {
            MessageBox.Show("준비중입니다.");
        }

        //설정창 실행
        private void trade_setting(object sender, EventArgs e)
        {
            if (!utility.load_check)
            {
                MessageBox.Show("초기 세팅 반영중");
                return;
            }
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }
            if (arrCondition.Length == 0)
            {
                MessageBox.Show("조건식 로딩중");
                return;
            }
            Setting newform2 = new Setting(this);
            newform2.ShowDialog(); //form2 닫기 전까지 form1 UI 제어 불가능
        }

        //매매내역 확인
        private void Porfoilo_btn_Click(object sender, EventArgs e)
        {
            if (!utility.load_check)
            {
                MessageBox.Show("초기 세팅 반영중");
                return;
            }
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }
            if (arrCondition.Length == 0)
            {
                MessageBox.Show("조건식 로딩중");
                return;
            }
            Transaction newform2 = new Transaction();
            newform2.ShowDialog(); //form2 닫기 전까지 form1 UI 제어 불가능
        }

        //전체로그 확인
        private void Log_btn_Click(object sender, EventArgs e)
        {
            if (!utility.load_check)
            {
                MessageBox.Show("초기 세팅 반영중");
                return;
            }
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }
            if (arrCondition.Length == 0)
            {
                MessageBox.Show("조건식 로딩중");
                return;
            }
            Log newform2 = new Log();
            newform2.ShowDialog(); //form2 닫기 전까지 form1 UI 제어 불가능
        }

        //업데이트 및 동의사항 확인
        private void Update_agree_btn_Click(object sender, EventArgs e)
        {
            if (!utility.load_check)
            {
                MessageBox.Show("초기 세팅 반영중");
                return;
            }
            if(login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }
            if (arrCondition.Length == 0)
            {
                MessageBox.Show("조건식 로딩중");
                return;
            }
            //
            Update newform2 = new Update(this);
            newform2.ShowDialog(); //form2 닫기 전까지 form1 UI 제어 불가능
        }

        //종목 조회 실행
        private void stock_search_btn(object sender, EventArgs e)
        {
            if (!utility.load_check)
            {
                MessageBox.Show("초기 세팅 반영중");
                return;
            }
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }
            if (arrCondition.Length == 0)
            {
                MessageBox.Show("조건식 로딩중");
                return;
            }
            if (string.IsNullOrEmpty(Stock_code.Text.Trim()))
            {
                WriteLog_System($"[종목검색] : 종목코드를 입력해주세요\n");
                return;
            }

            //종목코드, 시간, 현재가, 거래량, 종목명, 상한가
            int[] items = { 0, 1, 4, 10, 17, 33 };
            MarketEye.SetInputValue(0, items);
            MarketEye.SetInputValue(1, Stock_code.Text.Trim());
            //
            int result = MarketEye.BlockRequest();
            //
            if (result == 0)
            {
                string tmp1 = MarketEye.GetDataValue(4, 0); //종목명 => string
                string tmp2 = Convert.ToInt32(MarketEye.GetDataValue(2, 0)); //현재가 => long or float
                string tmp3 = Convert.ToInt32(MarketEye.GetDataValue(3, 0)); //거래량 => ulong
                string tmp4 = Convert.ToInt32(MarketEye.GetDataValue(5, 0)); //상한가 => long or float)
                WriteLog_System($"[종목검색] : {tmp1}/{tmp2}/{tmp3}/{tmp4})\n");
            }
        }

        //조건식 실시간 시작 버튼
        private void real_time_search_btn(object sender, EventArgs e)
        {
            if (!utility.load_check)
            {
                MessageBox.Show("초기 세팅 반영중");
                return;
            }
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }
            if (arrCondition.Length == 0)
            {
                MessageBox.Show("조건식 로딩중");
                return;
            }

            //
            dtCondStock.Clear();

            if (dataGridView1.InvokeRequired)
            {
                dataGridView1.Invoke((MethodInvoker)delegate {
                    bindingSource.ResetBindings(false);
                });
            }
            else
            {
                bindingSource.ResetBindings(false);
            }

            //D+2 예수금 + 계좌 보유 종목 + 차트 반영
            dtCondStock_hold.Clear();
            GetCashInfo_Seperate(true);

            System.Threading.Thread.Sleep(250);

            //매매내역
            dtCondStock_Transaction.Clear();
            Transaction_Detail_seperate("", "");

            System.Threading.Thread.Sleep(250);

            //매도실현손익(제세금, 수수료 포함)
            today_profit_tax_load_seperate();

            System.Threading.Thread.Sleep(250);

            auto_allow_check(true);
        }

        //조건식 실시간 중단 버튼
        private void real_time_stop_btn(object sender, EventArgs e)
        {
            if (!utility.load_check)
            {
                MessageBox.Show("초기 세팅 반영중");
                return;
            }
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }
            if (arrCondition.Length == 0)
            {
                MessageBox.Show("조건식 로딩중");
                return;
            }
            real_time_stop(true);
        }

        //전체 청산 버튼
        private void All_clear_btn_Click(object sender, EventArgs e)
        {
            if (!utility.load_check)
            {
                MessageBox.Show("초기 세팅 반영중");
                return;
            }
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }
            if (arrCondition.Length == 0)
            {
                MessageBox.Show("조건식 로딩중");
                return;
            }

            if (dtCondStock.Rows.Count > 0)
            {
                foreach (DataRow row in dtCondStock.Rows)
                {
                    if (row["상태"].ToString() == "매수완료" && row["상태"].ToString() == "TS매수완료")
                    {
                        sell_order(row.Field<string>("현재가"), "청산매도/일반", row.Field<string>("주문번호"), row.Field<string>("수익률"), row.Field<string>("구분코드"));
                    }
                }
            }
            else
            {
                WriteLog_Order("전체청산 종목 없음\n");
            }
        }

        //수익 종목 청산 버튼
        private void Profit_clear_btn_Click(object sender, EventArgs e)
        {
            if(!utility.load_check)
            {
                MessageBox.Show("초기 세팅 반영중");
                return;
            }
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }
            if (arrCondition.Length == 0)
            {
                MessageBox.Show("조건식 로딩중");
                return;
            }

            if (dtCondStock.Rows.Count > 0)
            {
                foreach (DataRow row in dtCondStock.Rows)
                {
                    double percent_edit = double.Parse(row.Field<string>("수익률").TrimEnd('%'));
                    if (row["상태"].ToString() == "매수완료" && row["상태"].ToString() == "TS매수완료" && percent_edit >= 0)
                    {
                        sell_order(row.Field<string>("현재가"), "청산매도/수익", row.Field<string>("주문번호"), row.Field<string>("수익률"), row.Field<string>("구분코드"));
                    }
                }
            }
            else
            {
                WriteLog_Order("수익청산 종목 없음\n");
            }
        }

        //손실 종목 청산 버튼
        private void Loss_clear_btn_Click(object sender, EventArgs e)
        {
            if (!utility.load_check)
            {
                MessageBox.Show("초기 세팅 반영중");
                return;
            }
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }
            if (arrCondition.Length == 0)
            {
                MessageBox.Show("조건식 로딩중");
                return;
            }

            if (dtCondStock.Rows.Count > 0)
            {
                foreach (DataRow row in dtCondStock.Rows)
                {
                    double percent_edit = double.Parse(row.Field<string>("수익률").TrimEnd('%'));
                    if (row["상태"].ToString() == "매수완료" && row["상태"].ToString() == "TS매수완료" && percent_edit < 0)
                    {
                        sell_order(row.Field<string>("현재가"), "청산매도/손실", row.Field<string>("주문번호"), row.Field<string>("수익률"), row.Field<string>("구분코드"));
                    }
                }
            }
            else
            {
                WriteLog_Order("손실청산 종목 없음\n");
            }
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            //D+2 예수금 + 계좌 보유 종목
            dtCondStock_hold.Clear();
            GetCashInfo_Seperate(false);

            System.Threading.Thread.Sleep(250);

            //매매내역
            dtCondStock_Transaction.Clear();
            Transaction_Detail_seperate("", "");

            System.Threading.Thread.Sleep(250);

            //매도실현손익(제세금, 수수료 포함)
            today_profit_tax_load_seperate();

        }

        private void Select_cancel_Click(object sender, EventArgs e)
        {
            DataRow[] findRows = dtCondStock.AsEnumerable().Where(row => row.Field<string>("상태") == "매수중" && row.Field<bool>("선택")).ToArray();

            if (findRows.Any())
            {
                //
                for (int i = 0; i < findRows.Length; i++)
                {
                    //string trade_type, string order_number, string gubun, string code_name, string code, string order_acc
                    order_close("매수", findRows[i]["주문번호"].ToString(), findRows[i]["구분코드"].ToString(), findRows[i]["종목명"].ToString(), findRows[i]["종목코드"].ToString(), findRows[i]["보유수량"].ToString().Split('/')[1]);
                    System.Threading.Thread.Sleep(750);
                }
            }

            //

            DataRow[] findRows2 = dtCondStock.AsEnumerable().Where(row => row.Field<string>("상태") == "매도중" && row.Field<bool>("선택")).ToArray();

            if (findRows2.Any())
            {
                //
                for (int i = 0; i < findRows2.Length; i++)
                {
                    order_close("매도", findRows2[i]["주문번호"].ToString(), findRows2[i]["구분코드"].ToString(), findRows2[i]["종목명"].ToString(), findRows2[i]["종목코드"].ToString(), findRows2[i]["보유수량"].ToString().Split('/')[1]);
                    System.Threading.Thread.Sleep(750);
                }
            }
        }      

        //------------------------------------------로그-------------------------------------------

        //로그창(System)
        private void WriteLog_System(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss:fff");
            log_window.AppendText($@"{"[" + time + "] " + message}");
            log_full.Add($"[{time}][System] : {message}");
        }

        //로그창(Order)
        private void WriteLog_Order(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss:fff");
            log_window3.AppendText($@"{"[" + time + "] " + message}");
            log_full.Add($"[{time}][Order] : {message}");
            log_trade.Add($"[{time}][Order] : {message}");
        }

        //로그창(Stock)
        private void WriteLog_Stock(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss:fff");
            log_window2.AppendText($@"{"[" + time + "] " + message}");
            log_full.Add($"[{time}][Stock] : {message}");
        }

        //telegram_chat
        private void telegram_message(string message)
        {
            if (!utility.Telegram_Allow) return;
            string time = DateTime.Now.ToString("HH:mm:ss:fff");
            string message_edtied = "[" + time + "] " + message;
            telegram_send(message_edtied);
        }

        //telegram_send(초당 100개 제한)
        private async void telegram_send(string message)
        {
            string urlString = $"https://api.telegram.org/bot{utility.telegram_token}/sendMessage?chat_id={utility.telegram_user_id}&text={message}";

            bool success = false;

            while (!success)
            {
                try
                {
                    WebRequest request = WebRequest.Create(urlString);
                    request.Timeout = 60000; // 60초로 Timeout 설정

                    //await은 비동기 작업이 완료될떄까지 기다린다.
                    //using 문은 IDisposable 인터페이스를 구현한 객체의 리소스를 안전하게 해제하는 데 사용
                    using (WebResponse response = await request.GetResponseAsync())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string responseString = await reader.ReadToEndAsync();
                        success = true;
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse response && response.StatusCode == (HttpStatusCode)429)
                    {
                        WriteLog_System($"FLOOD_WAIT: Waiting for 30s...");
                        await Task.Delay(30000);
                    }
                    else
                    {
                        WriteLog_System("Telegram 전송 오류 발생 : " + ex.Message);
                    }
                }
            }
        }

        public static int update_id = 0;
        private DateTime time_start = DateTime.Now;

        //Telegram 메시지 수신
        private async Task Telegram_Receive()
        {         
            //string apiUrl = $"https://api.telegram.org/bot{utility.telegram_token}/getUpdates";  

            while (true)
            {
                try
                {
                    string requestUrl = $"https://api.telegram.org/bot{utility.telegram_token}/getUpdates" + (update_id == 0 ? "" : $"?offset={update_id + 1}");
                    WebRequest request = WebRequest.Create(requestUrl);
                    using (WebResponse response = await request.GetResponseAsync())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string response_message = await reader.ReadToEndAsync();
                        JObject jsonData = JObject.Parse(response_message);
                        JArray resultArray = (JArray)jsonData["result"];
                        //
                        if (resultArray.Count > 0)
                        {
                            foreach (var result in resultArray)
                            {
                                string message = Convert.ToString(result["message"]["text"]);
                                int current_message_number = Convert.ToInt32(result["update_id"]);
                                //
                                long unixTimestamp = Convert.ToInt64(result["message"]["date"]);
                                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
                                DateTime localDateTime = dateTime.ToLocalTime();
                                //
                                if (current_message_number > update_id && localDateTime >= time_start)
                                {
                                    //로그인 진행중
                                    if (!login_complete)
                                    {
                                        telegram_message($"[TELEGRAM] : 로그인 진행중\n");
                                        continue;
                                    }
                                    //초기값 로드 진행중
                                    if (!initial_process_complete)
                                    {
                                        telegram_message($"[TELEGRAM] : 초기값 로드 진행중\n");
                                        continue;
                                    }
                                    //
                                    WriteLog_System($"[TELEGRAM] : {message} / {current_message_number}\n"); // 수신된 메시지 확인
                                    telegram_function(message);
                                    update_id = current_message_number;
                                }
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse httpResponse && httpResponse.StatusCode == HttpStatusCode.Conflict)
                    {
                        // 409 충돌 오류 처리
                        WriteLog_Order($"[TELEGRAM/ERROR] 409 Conflict: {ex.Message}\n");
                    }
                    else
                    {
                        WriteLog_Order($"[TELEGRAM/ERROR] : {ex.Message}\n");
                    }
                }

                // 일정한 간격으로 API를 호출하여 새로운 메시지 확인
                await Task.Delay(1000); // 1초마다 확인
            }
            /*           
            {"ok":true,"result":
                [{"update_id":000000000,
                  "message":
                    {"message_id":22222,
                    "from":{"id":34566778,"is_bot":false,"first_name":"Sy","last_name":"CH","username":"k456","language_code":"ko"}
                    ,"chat":{"id":69sdfg,"first_name":"Ssdfg","last_name":"CsdfgI","username":"ksdfg28","type":"private"}
                    ,"date":1717078874,
                    "text":"Hello"
                    }
                }]
            }
            */
        }

        //FORM CLOSED 후 LOG 저장
        //Process.Kill()에서 비정상 작동할 가능성 높음
        private void Form_FormClosed(object sender, FormClosedEventArgs e)
        {

            string formattedDate = DateTime.Now.ToString("yyyyMMdd");

            // 저장할 파일 경로
            string filePath = $@"C:\Auto_Trade_Creon\Log\{formattedDate}_full.txt";
            string filePath2 = $@"C:\Auto_Trade_Creon\Log_Trade\{formattedDate}_trade.txt";
            string filePath3 = "C:\\Auto_Trade_Creon\\Setting\\setting_daishin.txt";

            // StreamWriter를 사용하여 파일 저장
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.Write(String.Join("", log_full));
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 저장 중 오류 발생: " + ex.Message);
            }

            // StreamWriter를 사용하여 파일 저장
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath2, true))
                {
                    writer.Write(String.Join("", log_trade));
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 저장 중 오류 발생: " + ex.Message);
            }

            //Telegram Message Last Number
            try
            {
                if (!File.Exists(filePath3))
                {
                    MessageBox.Show("세이브 파일이 존재하지 않습니다.");
                    return;
                }

                // 파일의 모든 줄을 읽어오기
                var lines = File.ReadAllLines(filePath3).ToList();

                // 파일이 비어 있지 않은지 확인
                if (lines.Any())
                {
                    lines[lines.Count - 2] = "Telegram_Last_Chat_update_id/" + Convert.ToString(update_id);

                    File.WriteAllLines(filePath3, lines);
                }
                else
                {
                    MessageBox.Show("파일 형식 오류 : 새로운 세이브 파일 다운로드 요망");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 저장 중 오류 발생: " + ex.Message);
            }

        }

        //------------------------------------------공용기능-------------------------------------------

        //모든주문오브젝트는사용하기전에, 필수적으로 TradeInit을호출
        private bool TradeInit()
        {
            if (Checked_Trade_Init)
                return true;

            int rv = CpTdUtil.TradeInit(0);

            if (rv == 0)
            {
                Checked_Trade_Init = true;
                return true;
            }
            else if (rv == -1)
            {
                MessageBox.Show("계좌 비밀번호 오류 포함 에러.");
                Checked_Trade_Init = false;
                return false;
            }
            else if (rv == 1)
            {
                MessageBox.Show("OTP/보안카드키가 잘못되었습니다.");
                Checked_Trade_Init = false;
                return false;
            }
            else
            {
                MessageBox.Show("Error");
                Checked_Trade_Init = false;
                return false;
            }
        }

        //일련번호 받기
        //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=284&seq=239&page=1&searchString=CssWatchStgSubscribe&p=8841&v=8643&m=9505
        private int condition_sub_code(string condition_code)
        {
            CssWatchStgSubscribe.SetInputValue(0, condition_code);

            int check = 0;

            //수신확인
            while (true)
            {
                if (CssWatchStgSubscribe.GetDibStatus() == 1)
                {
                    check++;
                    WriteLog_System("[DibRq요청/수신대기/5초] : 일련번호\n");
                    System.Threading.Thread.Sleep(5000);
                }
                else if(check == 5)
                {
                    break;
                }
                else
                {
                    break;
                }
            }
            //
            if(check == 5)
            {
                WriteLog_System("[일련번호/수신실패] : 재부팅 요망\n");
                telegram_message("[일련번호/수신실패] : 재부팅 요망\n");
                return -1;
            }
            //
            int result = CssWatchStgSubscribe.BlockRequest();
            //
            if (result == 0)
            {
                return CssWatchStgSubscribe.GetHeaderValue(0);
            }
            return -1;
        }

        private string error_message(int error)
        {
            switch (error)
            {
                case 1:
                    return "통신요청실패";
                case 2:
                    return "주문확인창에서취소";
                case 3:
                    return "그외의내부오류";
                case 4:
                    return "주문요청제한개수초과";
            }
            return "기타에러(" + error.ToString() + ")";
        }

        //-----------------------------------------------Main------------------------------------------------

        public Trade_Auto_Daishin()
        {
            InitializeComponent();

            //-------------------초기 동작-------------------

            //기존 세팅 로드
            utility.setting_load_auto();

            //메인 시간 동작
            timer1.Start(); //시간 표시 - 1000ms

            //----------종료_동작---------
            this.FormClosed += new FormClosedEventHandler(Form_FormClosed);

            //-------------------버튼-------------------
            Main_menu.Click += main_menu; //메인메뉴
            Trade_setting.Click += trade_setting; //설정창
            porfoilo_btn.Click += Porfoilo_btn_Click;//매매정보
            Log_btn.Click += Log_btn_Click;//로그정보
            update_agree_btn.Click += Update_agree_btn_Click;//업데이트 및 동의사항

            Stock_search_btn.Click += stock_search_btn; //종목조회

            Real_time_search_btn.Click += real_time_search_btn; //실시간 조건식 등록
            Real_time_stop_btn.Click += real_time_stop_btn; //조건식 실시간 전체 중단

            All_clear_btn.Click += All_clear_btn_Click;
            profit_clear_btn.Click += Profit_clear_btn_Click;
            loss_clear_btn.Click += Loss_clear_btn_Click;

            Refresh.Click += Refresh_Click;
            select_cancel.Click += Select_cancel_Click;
        }

        //--------------------------------------------------------------Main_Timer---------------------------------------------------------------
        private bool isRunned = true;
        private bool isRunned2 = false;
        private bool isRunned3 = false;

        private bool initial_process_complete = false;

        private bool first_index = false;
        private bool second_index = false;

        private DateTime index1 = DateTime.Parse("08:59:00");
        private DateTime index2 = DateTime.Parse("09:00:00");

        //08:45:00 ~ 18:00:00
        //timer1(1000ms) : 주기 고정
        private async void ClockEvent(object sender, EventArgs e)
        {
            //시간표시
            timetimer.Text = DateTime.Now.ToString("yy MM-dd (ddd) HH:mm:ss");

            if (utility.load_check && !isRunned3)
            {
                isRunned3 = true;
                /*
                var response = WindowsFormsApp1.Update.SendAuthCodeAsync("");
                if (response.ToString().StartsWith("ALLOW")) 
                {
                    Authentication_Check = true;
                    WriteLog_System($"인증 : 유효기간({response.ToString().Split(',')[1]})\n");
                    telegram_message($"인증 : 유효기간({response.ToString().Split(',')[1]})\n");
                }
                else
                {
                    WriteLog_System("미인증 : 50만원 제한\n");
                    telegram_message("미인증 : 50만원 제한\n");
                }
                */
                isRunned = false;
            }

            if (!isRunned)
            {
                isRunned = true;

                //테이블 초기 세팅
                await initial_Table();

                //초기 선언
                await initial_load();

                //초기 설정 반영
                await initial_allow(false);

                if (utility.Telegram_Allow)
                {
                    Telegram_Receive();
                }

                //로그인
                this.Invoke((MethodInvoker)delegate
                {
                    Initial_Daishin();
                });

            }

            //운영시간 확인
            DateTime t_now = DateTime.Now;
            DateTime t_start = DateTime.Parse(utility.market_start_time);
            DateTime t_end = DateTime.Parse(utility.market_end_time);

            //메인 동작 실행
            //기본 값 모두 실행 후 각 종 값 수신
            if (initial_process_complete)
            {
                if (!isRunned2 && t_now >= t_start && t_now <= t_end)
                {
                    isRunned2 = true;

                    auto_allow_check(false);                  
                }
                else if (isRunned2 && t_now > t_end)
                {
                    isRunned2 = false;
                    real_time_stop(true);
                }

                //인덱스전송
                //DateTime index1 = DateTime.Parse("08:59:00");
                //DateTime index2 = DateTime.Parse("09:00:00");

                if (!first_index && index1 <= t_now)
                {
                    first_index = true;
                    WriteLog_System($"[INDEX/08:59:00] : {Foreign.Text}/{kospi_index.Text}/{kosdaq_index.Text}/{dow_index.Text}/{sp_index.Text}/{nasdaq_index.Text}\n");
                    telegram_message($"[INDEX/08:59:00] : {Foreign.Text}/{kospi_index.Text}/{kosdaq_index.Text}/{dow_index.Text}/{sp_index.Text}/{nasdaq_index.Text}\n");
                }

                if (!second_index && index2 <= t_now)
                {
                    second_index = true;
                    WriteLog_System($"[INDEX/09:00:00] : {Foreign.Text}/{kospi_index.Text}/{kosdaq_index.Text}/{dow_index.Text}/{sp_index.Text}/{nasdaq_index.Text}\n");
                    telegram_message($"[INDEX/09:00:00] : {Foreign.Text}/{kospi_index.Text}/{kosdaq_index.Text}/{dow_index.Text}/{sp_index.Text}/{nasdaq_index.Text}\n");
                }
            }
        }

        //-----------------------------------------initial-------------------------------------

        //초기 Table 값 입력
        private async Task initial_Table()
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("선택", typeof(bool));
            dataTable.Columns.Add("구분코드", typeof(string));
            dataTable.Columns.Add("편입", typeof(string)); // '편입' '이탈'
            dataTable.Columns.Add("상태", typeof(string)); // '대기' '매수중 '매수완료' '매도중' '매도완료'
            dataTable.Columns.Add("종목코드", typeof(string));
            dataTable.Columns.Add("종목명", typeof(string));
            dataTable.Columns.Add("현재가", typeof(string)); // + - 부호를 통해 매수호가인지 매도 호가인지 현재가인지 파악한다.
            dataTable.Columns.Add("거래량", typeof(string));
            dataTable.Columns.Add("편입상태", typeof(string));
            dataTable.Columns.Add("편입가", typeof(string));
            dataTable.Columns.Add("매도가", typeof(string));
            dataTable.Columns.Add("수익률", typeof(string));
            dataTable.Columns.Add("보유수량", typeof(string)); //보유수량
            dataTable.Columns.Add("조건식", typeof(string));
            dataTable.Columns.Add("편입시각", typeof(string));
            dataTable.Columns.Add("이탈시각", typeof(string));
            dataTable.Columns.Add("매수시각", typeof(string));
            dataTable.Columns.Add("매도시각", typeof(string));
            dataTable.Columns.Add("주문번호", typeof(string));
            dataTable.Columns.Add("상한가", typeof(string)); //상한가 => 시장가 계산용
            dataTable.Columns.Add("편입최고", typeof(string)); //당일최고
            dataTable.Columns.Add("매매진입", typeof(string)); //매매진입시각
            dtCondStock = dataTable;

            dataGridView1.DefaultCellStyle.Font = new Font("굴림", 8F, FontStyle.Regular);
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("굴림", 8F, FontStyle.Bold);

            DataTable dataTable2 = new DataTable();
            dataTable2.Columns.Add("구분코드", typeof(string));
            dataTable2.Columns.Add("종목코드", typeof(string)); //고정
            dataTable2.Columns.Add("종목명", typeof(string)); //고정
            dataTable2.Columns.Add("현재가", typeof(string)); //실시간 변경
            dataTable2.Columns.Add("보유수량", typeof(string)); //고정
            dataTable2.Columns.Add("평균단가", typeof(string)); //고정
            dataTable2.Columns.Add("평가금액", typeof(string));
            dataTable2.Columns.Add("수익률", typeof(string)); //실시간 변경
            dataTable2.Columns.Add("손익금액", typeof(string));
            dataTable2.Columns.Add("체결수량", typeof(string)); //고정
            dtCondStock_hold = dataTable2;
            dataGridView2.DataSource = dtCondStock_hold;

            dataGridView2.DefaultCellStyle.Font = new Font("굴림", 8F, FontStyle.Regular);
            dataGridView2.ColumnHeadersDefaultCellStyle.Font = new Font("굴림", 8F, FontStyle.Bold);

            DataTable dataTable3 = new DataTable();
            dataTable3.Columns.Add("구분코드", typeof(string));
            dataTable3.Columns.Add("종목번호", typeof(string));
            dataTable3.Columns.Add("종목명", typeof(string));
            dataTable3.Columns.Add("주문번호", typeof(string));
            dataTable3.Columns.Add("매매구분", typeof(string));
            dataTable3.Columns.Add("주문구분", typeof(string));
            dataTable3.Columns.Add("주문수량", typeof(string));
            dataTable3.Columns.Add("체결수량", typeof(string));
            dataTable3.Columns.Add("체결단가", typeof(string));
            dtCondStock_Transaction = dataTable3;
            dataGridView3.DataSource = dtCondStock_Transaction;

            dataGridView3.DefaultCellStyle.Font = new Font("굴림", 8F, FontStyle.Regular);
            dataGridView3.ColumnHeadersDefaultCellStyle.Font = new Font("굴림", 8F, FontStyle.Bold);

            InitializeDataGridView();
            dataGridView1.CurrentCellDirtyStateChanged += DataGridView1_CurrentCellDirtyStateChanged;
            dataGridView1.CurrentCellDirtyStateChanged += DataGridView1_CurrentCellDirtyStateChanged;
        }

        private BindingSource bindingSource;

        //데이터 바인딩(속도, 변경, 고급 기능 등)
        private void InitializeDataGridView()
        {
            bindingSource = new BindingSource();
            bindingSource.DataSource = dtCondStock;

            dataGridView1.DataSource = bindingSource;

            // Set the bool column to display as a checkbox
            dataGridView1.Columns["선택"].ReadOnly = false;
            dataGridView1.Columns["선택"].Width = 50;
            dataGridView1.Columns["구분코드"].Width = 60;
            dataGridView1.Columns["편입"].Width = 50;
            dataGridView1.Columns["상태"].Width = 50;
        }

        //현재 셀이 편집 중인지 확인하여 즉시 Grid에 반영
        private void DataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        //혅재 셀의 값이 반경되었다면 DataTable에 반영
        private void DataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns["선택"].Index)
            {
                // Update DataTable based on the DataGridView change
                bindingSource.EndEdit();
            }
        }

        //초기선언
        private async Task initial_load()
        {
            CpCybos = new CPUTILLib.CpCybos();
            CpTdUtil = new CPTRADELib.CpTdUtil();
            CpSvrNew7224 = new CPSYSDIBLib.CpSvrNew7224(); //외국인선물
            cpFuture = new CPUTILLib.CpFutureCode(); //코스피 선물옵션
            cpKFuture = new CPUTILLib.CpKFutureCode(); //코스닥선물옵션
            FutOptChart = new CPSYSDIBLib.FutOptChart(); //선물옵션차트
            CpUsCode = new CPUTILLib.CpUsCode(); //해외지수
            CpTd6033 = new CPTRADELib.CpTd6033(); //계좌별 D+2 예수금 현황
            CpTdNew5331B = new CPTRADELib.CpTdNew5331B();//계좌별 매도 가능 수량
            CpTd6032 = new CPTRADELib.CpTd6032();//매도실현손익(제세금, 수수료 포함)
            CpTd5341 = new CPTRADELib.CpTd5341(); //매매내역
            CpUsCode = new CPUTILLib.CpUsCode(); //해외지수
            CpFore8312 = new DSCBO1Lib.CpFore8312(); //해외지수 수신
            CssStgList = new CPSYSDIBLib.CssStgList(); //조건식 받기
            //CssWatchStgControl = new CPSYSDIBLib.CssWatchStgControl(); // 실시간 조건식 등록 및 해제
            CssStgFind = new CPSYSDIBLib.CssStgFind(); //초기 종목 검색 리스트
            MarketEye = new CPSYSDIBLib.MarketEye(); //초기 종목 검색 정보
            CssWatchStgSubscribe = new CPSYSDIBLib.CssWatchStgSubscribe(); // 일련번호 받기
            CssAlert = new CPSYSDIBLib.CssAlert(); // 종목 편출입
            CssAlert.Received += new CPSYSDIBLib._ISysDibEvents_ReceivedEventHandler(stock_in_out);
            StockCur = new DSCBO1Lib.StockCur(); // 실시간 종목 시세(관심종목)
            StockCur.Received += new DSCBO1Lib._IDibEvents_ReceivedEventHandler(Stock_real_price);
            //
            CpTd0311 = new CPTRADELib.CpTd0311(); //현금주문
            CpTd0322 = new CPTRADELib.CpTd0322(); //시간외종가
            CpTd0386 = new CPTRADELib.CpTd0386(); //시간외단일가
                                                  //
            CpConclusion = new DSCBO1Lib.CpConclusion(); //실시간 체결 내역
            CpConclusion.Received += new DSCBO1Lib._IDibEvents_ReceivedEventHandler(Trade_Check);
            //
            Checked_Trade_Init = false;
            //
            CpTd0326 = new CPTRADELib.CpTd0326(); //시간외종가 취소주문
            CpTd0387 = new CPTRADELib.CpTd0387(); //시간외단일가 취소주문
            CpTd0314 = new CPTRADELib.CpTd0314(); //정규장 취소주문
        }

        //초기 설정 변수
        private string sell_condtion_method_after;

        //초기 설정 반영
        public async Task initial_allow(bool check)
        {
            string[] mode = { "지정가", "시장가" };
            string[] hoo = { "5호가", "4호가", "3호가", "2호가", "1호가", "현재가", "시장가", "-1호가", "-2호가", "-3호가", "-4호가", "-5호가" };
            string[] hoo2 = { "5호가", "4호가", "3호가", "2호가", "1호가", "현재가", "-1호가", "-2호가", "-3호가", "-4호가", "-5호가" };

            //초기 세팅
            acc_text.Text = utility.setting_account_number;
            acc_isa_text.Text = utility.setting_account_number;
            total_money.Text = string.Format("{0:#,##0}", Convert.ToDecimal(utility.initial_balance));
            total_money_isa.Text = string.Format("{0:#,##0}", Convert.ToDecimal(utility.initial_balance));
            Current_User_money.Text = "0";
            Current_User_money_isa.Text = "0";
            if (!check)
            {
                User_money.Text = "0";
                User_money_isa.Text = "0";
            }
            max_hoid.Text = "0/0";
            //
            if (utility.buy_INDEPENDENT || utility.buy_DUAL)
            {
                maxbuy_acc.Text = string.Concat(Enumerable.Repeat("0/", utility.Fomula_list_buy_text.Split(',').Length)) + utility.maxbuy_acc;
            }
            else
            {
                maxbuy_acc.Text = "0/" + utility.maxbuy_acc;
            }
            //
            User_id.Text = utility.real_id;
            operation_start.Text = utility.market_start_time;
            operation_stop.Text = utility.market_end_time;
            search_start.Text = utility.buy_condition_start;
            search_stop.Text = utility.buy_condition_end;
            clear_sell.Text = Convert.ToString(utility.clear_sell);
            clear_sell_time.Text = utility.clear_sell_start;
            profit.Text = utility.profit_percent_text;
            loss.Text = utility.loss_percent_text;
            buy_condition.Text = utility.Fomula_list_buy_text;
            buy_condtion_method.Text = mode[utility.buy_set1] + "/" + hoo[utility.buy_set2];
            sell_condtion.Text = utility.Fomula_list_sell_text;
            sell_condtion_method.Text = mode[utility.sell_set1] + "/" + hoo[utility.sell_set2];
            sell_condtion_method_after = mode[utility.sell_set1_after] + "/" + hoo2[utility.sell_set2_after];

            //초기세팅2
            all_profit.Text = "0";
            all_profit_percent.Text = "00.00%";
            all_profit_isa.Text = "0";
            all_profit_percent_isa.Text = "00.00%";
            //today_tax.Text = "0";
            today_profit_percent_tax.Text = "00.00%";
            today_profit_tax.Text = "0";
            today_profit_percent_tax_isa.Text = "00.00%";
            today_profit_tax_isa.Text = "0";
            //today_profit_percent.Text = "00.00%";
            //today_profit.Text = "0";

            Foreign.Text = "-";
            kospi_index.Text = "-";
            kosdaq_index.Text = "-";
            dow_index.Text = "-";
            sp_index.Text = "-";
            nasdaq_index.Text = "-";

            //초기세팅4
            if (utility.buy_OR)
            {
                trading_mode.Text = "OR_모드";
            }
            else if (utility.buy_AND)
            {
                trading_mode.Text = "AND_모드";
            }
            else
            {
                trading_mode.Text = "독립_모드";
            }

            //KIS
            KIS_RUN.Text = Convert.ToString(utility.KIS_Allow); //사용여부
            KIS_Independent.Text = Convert.ToString(utility.KIS_Independent);
            KIS_Account_Number.Text = utility.KIS_Account;
            KIS_N.Text = utility.KIS_amount; //N등분
            KIS_ACCOUNT.Text = "0";//예수금
            KIS_Profit.Text = "0";

            //
            update_id = utility.Telegram_last_chat_update_id;

            //
            Authentication = utility.Auth;

            //
            if (Authentication_Check)
            {
                Authentic.Text = "인증";
            }
            else
            {
                Authentic.Text = "미인증";
            }

            //
            WriteLog_System("세팅 반영 완료\n");
            telegram_message("세팅 반영 완료\n");
        }

        //------------------------------------Login---------------------------------

        private bool login_complete = false;

        //관리자 권한 실행 및 프로그램 설치 확인
        private void Initial_Daishin()
        {

            //관리자권한 실행 확인
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                WriteLog_System("[실행/정상] 관리자 권한 실행\n");
            }
            else
            {
                WriteLog_System("관리자 권한 실행 요망\n");
                return;
            }

            //CreaonApi 실행
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Creon\\dstarter");
            string path = key.GetValue("path").ToString();
            if (path == "")
            {
                WriteLog_System("[설치/실패] 크레온 플러스 설치 요망\n");
            }
            else
            {
                //프로세스 명 얻기
                string programName = System.IO.Path.GetFileNameWithoutExtension("C:\\daishin\\CYBOSPLUS\\CpStart.exe");
                //프로세스 실행중인지 확인
                if (Process.GetProcessesByName(programName).Length > 0)
                {
                    RequestConnection();
                }
                else
                {
                    Process.Start(path + "\\coStarter.exe", $"/prj:cp /id:{utility.real_id} /pwd:{utility.real_password} /pwdcert:{utility.real_cert_password} /autostart");
                    RequestConnection();
                }
            }

        }

        //타이머 생성
        private System.Timers.Timer Timer_Connection;
        private int Timer_Count;

        //연결 시간 체크를 위한 타이머 생성
        public void RequestConnection()
        {
            Timer_Connection = new System.Timers.Timer();
            Timer_Connection.Interval = 1000;
            //타이머가 지정된 시간 간격이 지나고 난 후 발생하는 이벤트
            Timer_Connection.Elapsed += new ElapsedEventHandler(TimerConnection_Elapsed);
            Timer_Connection.Start();
            Timer_Count = 0;
        }

        //연결 시간 체크
        private void TimerConnection_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Timer_Count > 180)
            {
                WriteLog_System("[연결/비정상] API 업데이트 확인 요망\n");
                telegram_message("[연결/비정상] API 업데이트 확인 요망\n");
                //
                Timer_Connection.Stop();
                Timer_Connection.Dispose();
                Timer_Connection = null;
                Timer_Count = 0;
            }

            Timer_Count += 1;

            // UI 스레드로 작업을 전달
            this.Invoke((MethodInvoker)delegate
            {
                Connection_Check();
            });
        }

        //연결확인
        private void Connection_Check()
        {
            //공통 Object?
            CpCybos = new CPUTILLib.CpCybos();

            //연결 확인
            if (CpCybos.IsConnect != 0)
            {
                Timer_Connection.Stop();
                Timer_Connection.Dispose();
                Timer_Connection = null;
                Timer_Count = 0;
                login_check = 0;
                WriteLog_System("[연결/정상] 연결\n");
                telegram_message("[연결/정상] 연결\n");
                //
                login_complete = true;
                //
                initial_process(false);
            }
        }

        //------------------------------------Login이후 동작---------------------------------

        private bool hold_update_initial = true;   

        //함수간 간격 200ms
        public void initial_process(bool check)
        {
            if (check)
            {
                dtCondStock.Clear();
                if (dataGridView1.InvokeRequired)
                {
                    dataGridView1.Invoke((MethodInvoker)delegate {
                        bindingSource.ResetBindings(false);
                    });
                }
                else
                {
                    bindingSource.ResetBindings(false);
                }
                //
                dtCondStock_hold.Clear();
                dtCondStock_Transaction.Clear();
            };

            //한번만 실행시켜주면 됨
            if (!check)
            {
                timer3.Start(); //체결 내역 업데이트 - 200ms
                CpConclusion.Subscribe(); //실시간 체결 등록
            }

            System.Threading.Thread.Sleep(250);

            //계좌 번호
            Account();

            System.Threading.Thread.Sleep(250);

            //D+2 예수금 + 계좌 보유 종목
            GetCashInfo_Seperate(true);

            //장전 예수금 1회 업데이트
            hold_update_initial = false;

            System.Threading.Thread.Sleep(250);

            //매도실현손익(제세금, 수수료 포함)
            today_profit_tax_load_seperate();

            System.Threading.Thread.Sleep(250);

            //매매내역
            Transaction_Detail_seperate("", "");

            System.Threading.Thread.Sleep(250);

            //지수
            Index_load();

            System.Threading.Thread.Sleep(250);

            //
            Condition_load(); //조건식 로드

            System.Threading.Thread.Sleep(250);

            initial_process_complete = true;
        }

        //------------------------------------Login이후 동작 함수 목록--------------------------------- 

        //계좌번호목록(마스터계좌)
        private void Account()
        {
            if (TradeInit())
            {
                //0번 메인 계좌
                account = (string[])CpTdUtil.AccountNumber;
                //계좌번호 존재
                if (account.Length > 0)
                {
                    if (!account.Contains(utility.setting_account_number))
                    {
                        acc_text.Text = account[0];
                        acc_isa_text.Text = account[0];
                        WriteLog_System("계좌번호 재설정 요청 및 초기화\n");
                    }
                    WriteLog_System("[계좌번호/설정] : " + account[0] + "\n");
                }
                else{
                    WriteLog_System("계좌번호 검색 실패\n");
                }
            }
        }

        //예수금 정보(D+2) + 계좌보유수량
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=291&seq=176&page=2&searchString=&p=&v=&m=
        private void GetCashInfo_Seperate(bool update)
        {
            GetCashInfo(Master_code, update);

            System.Threading.Thread.Sleep(200);

            if (utility.buy_DUAL) GetCashInfo(ISA_code, update);
        }

        private void GetCashInfo(string acc_gubun, bool update)
        {
            if (TradeInit())
            {
                CpTd6033.SetInputValue(0, acc_text.Text);
                CpTd6033.SetInputValue(1, acc_gubun);
                CpTd6033.SetInputValue(2, 14);
                CpTd6033.SetInputValue(3, "1");
                //
                int check = 0;
                //
                //수신확인
                while (true)
                {
                    if (CpTd6033.GetDibStatus() == 1)
                    {
                        check++;
                        WriteLog_System("[DibRq요청/수신대기/5초] : 예수금 정보(D+2) + 계좌보유수량\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check == 5)
                {
                    WriteLog_System("[예수금 정보(D+2) + 계좌보유수량/수신실패] : 재부팅 요망\n");
                    telegram_message("[예수금 정보(D+2) + 계좌보유수량/수신실패] : 재부팅 요망\n");
                    return;
                }
                //
                int result = CpTd6033.BlockRequest();
                //
                if (result == 0)
                {
                    //예수금 받기
                    string day2money = string.Format("{0:#,##0}", Convert.ToDecimal(CpTd6033.GetHeaderValue(9).ToString().Trim()));

                    //1회성 업데이트 : 초기 예수금
                    if (hold_update_initial)
                    {
                        if (acc_gubun.Equals(Master_code))
                        {
                            User_money.Text = day2money;
                        }
                        else
                        {
                            User_money_isa.Text = day2money;
                        }
                    }

                    //변동 예수금
                    if (acc_gubun.Equals(Master_code))
                    {
                        Current_User_money.Text = day2money;
                        //전체 수익 업데이트
                        all_profit.Text = string.Format("{0:#,##0}", Convert.ToDecimal(Convert.ToInt32(Current_User_money.Text.Replace(",", "")) - Convert.ToInt32(total_money.Text.Replace(",", "")))); //수익
                        all_profit_percent.Text = string.Format("{0:#,##0.00}%", Convert.ToDecimal(Convert.ToDouble(all_profit.Text.Replace(",", "")) / Convert.ToDouble(total_money.Text.Replace(",", "")) * 100)); //수익률
                        WriteLog_System($"[예수금/{acc_gubun}] : {day2money}\n");
                        telegram_message($"[예수금/{acc_gubun}] : {day2money}\n");
                    }
                    else
                    {
                        Current_User_money_isa.Text = day2money;
                        //전체 수익 업데이트
                        all_profit_isa.Text = string.Format("{0:#,##0}", Convert.ToDecimal(Convert.ToInt32(Current_User_money.Text.Replace(",", "")) - Convert.ToInt32(total_money.Text.Replace(",", "")))); //수익
                        all_profit_percent_isa.Text = string.Format("{0:#,##0.00}%", Convert.ToDecimal(Convert.ToDouble(all_profit.Text.Replace(",", "")) / Convert.ToDouble(total_money.Text.Replace(",", "")) * 100)); //수익률
                        WriteLog_System($"[예수금/{acc_gubun}] : {day2money}\n");
                        telegram_message($"[예수금/{acc_gubun}] : {day2money}\n");
                    }

                    //계좌보유수량
                    for (int i = 0; i < Convert.ToInt32(CpTd6033.GetHeaderValue(7)); i++)
                    {
                        dtCondStock_hold.Rows.Add(
                            acc_gubun,
                            CpTd6033.GetDataValue(12, i), //종목코드(A Type) => string
                            CpTd6033.GetDataValue(0, i), //종목명 => string
                            "0", //현재가
                            string.Format("{0:#,##0}", Convert.ToString(CpTd6033.GetDataValue(15, i))), //매도가능수량 => long
                            string.Format("{0:#,##0}", CpTd6033.GetDataValue(17, i)), //체결장부단가 => double
                            string.Format("{0:#,##0}", CpTd6033.GetDataValue(9, i)), //평가금액(단위:원)(천원미만내림) => longlong
                            string.Format("{0:#,##0.00}%", Convert.ToDecimal(CpTd6033.GetDataValue(11, i)) / 10000), //수익률 => double
                            string.Format("{0:#,##0}", CpTd6033.GetDataValue(10, i)), //평가손익(단위:원)(천원미만내림) => longlong
                            string.Format("{0:#,##0}", CpTd6033.GetDataValue(6, i)) //금일체결수량 => long
                        );
                    }
                    
                    /*
                    //보유계좌 테이블 반영
                    dtCondStock_hold.AcceptChanges();
                    dataGridView2.DataSource = dtCondStock_hold;
                    */

                    if (dataGridView2.InvokeRequired)
                    {
                        dataGridView2.Invoke((MethodInvoker)delegate {
                            dataGridView2.DataSource = dtCondStock_hold;
                            dataGridView2.Refresh();
                        });
                    }
                    else
                    {
                        dataGridView2.DataSource = dtCondStock_hold;
                        dataGridView2.Refresh();
                    }

                    //기존 보유 종목 차트 업데이트
                    if (update)
                    {
                        Hold_Update();
                    }
                }
                else
                {
                    WriteLog_Order($"[예수금및계좌보유/수신실패] : {error_message(result)} / {CpTd6033.GetDibMsg1()}\n");
                }
            }
        }

        //초기 보유 종목 테이블 업데이트
        private void Hold_Update()
        {
            if (dtCondStock_hold.Rows.Count == 0)
            {
                WriteLog_Stock("기존 보유 종목 없음\n");
                telegram_message("기존 보유 종목 없음\n");
                if (utility.max_hold)
                {
                    //최대 보유 종목 에 대한 계산
                    max_hoid.Text = "0/" + utility.max_hold_text;
                }
                else
                {
                    max_hoid.Text = "0/10";
                }
                return;
            }

            WriteLog_Stock("기존 보유 종목 있음\n");
            telegram_message("기존 보유 종목 있음\n");

            foreach (DataRow row in dtCondStock_hold.Rows)
            {
                string gubun = row["구분코드"].ToString();
                string Code = row["종목코드"].ToString();
                string Hold_num = row["보유수량"].ToString();
                //
                Stock_info("전일보유", Code, Hold_num, Code, gubun);
                //
                System.Threading.Thread.Sleep(250);
            }

            //
            if (utility.max_hold)
            {
                //최대 보유 종목 에 대한 계산
                max_hoid.Text = dtCondStock_hold.Rows.Count + "/" + utility.max_hold_text;
            }
            else
            {
                max_hoid.Text = dtCondStock_hold.Rows.Count + "/10";
            }
        }

        //매도실현손익(제세금, 수수료 포함)
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=291&seq=264&page=1&searchString=&p=&v=&m=
        private void today_profit_tax_load_seperate()
        {
            today_profit_tax_load("", Master_code);

            System.Threading.Thread.Sleep(200);

            if (utility.buy_DUAL) today_profit_tax_load("", ISA_code);
        }

        private void today_profit_tax_load(string load_type, string acc_gubun)
        {
            //실질매수 : 0.015% / 실질매도 : 0.015% + 0.18%
            //모의매수 : 0.35% / 실질매도 : 0.35% + 0.25%
            if (TradeInit())
            {
                CpTd6032.SetInputValue(0, acc_text.Text);
                CpTd6032.SetInputValue(1, acc_gubun);
                //
                int check = 0;
                //
                //수신확인
                while (true)
                {
                    if (CpTd6032.GetDibStatus() == 1)
                    {
                        check++;
                        WriteLog_System("[DibRq요청/수신대기/5초] : 매도실현손익\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check == 5)
                {
                    WriteLog_System("[매도실현손익/수신실패] : 재부팅 요망\n");
                    telegram_message("[매도실현손익/수신실패] : 재부팅 요망\n");
                    return;
                }
                //
                int result = CpTd6032.BlockRequest();
                //
                if (result == 0)
                {
                    //int sum_profit_tax = Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "총손익금액").Trim().Replace(",", "")); //세후손익
                    //int sum_tax = Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "총수수료_세금").Trim().Replace(",", "")); //세금              
                    //today_profit.Text = string.Format("{0:#,##0}", Convert.ToDecimal(sum_profit_tax + sum_tax)); // 당일 손익
                    //today_tax.Text = string.Format("{0:#,##0}", Convert.ToDecimal(sum_tax)); // 당일 세금
                    //today_profit_percent.Text = string.Format("{0:#,##0.00}%", Convert.ToDecimal(Convert.ToDouble(sum_profit_tax + sum_tax) / Convert.ToDouble(User_money.Text.Replace(",", "")) * 100)); // 당일 손익률
                    int profit = CpTd6032.GetHeaderValue(2).Equals("") ? 0 : Convert.ToInt32(CpTd6032.GetHeaderValue(2).Trim());

                    if (acc_gubun.Equals(Master_code))
                    {
                        today_profit_tax.Text = string.Format("{0:#,##0}", Convert.ToDecimal(profit)); // 당일 세후 매도 실현 손익 금액(천원) => string
                        today_profit_percent_tax.Text = string.Format("{0:#,##0.00}%", Convert.ToDecimal(CpTd6032.GetHeaderValue(3))); // 당일 세후 손익률 => float
                        if (load_type.Equals("매도"))
                        {
                            //WriteLog_System("[누적세전손익] : " + today_profit.Text + " / [누적세전손익률] : " + today_profit_percent.Text + "\n");
                            WriteLog_System($"[누적세후손익/{acc_gubun}] : {today_profit_tax.Text} / [누적세후손익률] : {today_profit_percent_tax.Text}\n");
                            //telegram_message("[누적세전손익] : " + today_profit.Text + " / [누적세전손익률] : " + today_profit_percent.Text + "\n");
                            telegram_message($"[누적세후손익/{acc_gubun}] : {today_profit_tax.Text} / [누적세후손익률] : {today_profit_percent_tax.Text}\n");
                        }
                    }
                    else
                    {
                        today_profit_tax_isa.Text = string.Format("{0:#,##0}", Convert.ToDecimal(profit)); // 당일 세후 매도 실현 손익 금액(천원) => string
                        today_profit_percent_tax_isa.Text = string.Format("{0:#,##0.00}%", Convert.ToDecimal(CpTd6032.GetHeaderValue(3))); // 당일 세후 손익률 => float
                        if (load_type.Equals("매도"))
                        {
                            //WriteLog_System("[누적세전손익] : " + today_profit.Text + " / [누적세전손익률] : " + today_profit_percent.Text + "\n");
                            WriteLog_System($"[누적세후손익/{acc_gubun}] : {today_profit_tax_isa.Text} / [누적세후손익률] : {today_profit_percent_tax_isa.Text}\n");
                            //telegram_message("[누적세전손익] : " + today_profit.Text + " / [누적세전손익률] : " + today_profit_percent.Text + "\n");
                            telegram_message($"[누적세후손익/{acc_gubun}] : {today_profit_tax_isa.Text} / [누적세후손익률] : {today_profit_percent_tax_isa.Text}\n");
                        }
                    }
                }
                else
                {
                    WriteLog_Order($"[매도실현손익/수신실패] : {error_message(result)} / {CpTd6032.GetDibMsg1()}\n");
                }
            }
        }

        //체결내역업데이트(주문번호) => 매매내역 정보
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=291&seq=174&page=2&searchString=&p=&v=&m=
        private void Transaction_Detail_seperate(string order_number, string trade)
        {
            Transaction_Detail(order_number, Master_code, trade);

            System.Threading.Thread.Sleep(200);

            if (utility.buy_DUAL) Transaction_Detail(order_number, ISA_code, trade);
        }

        private void Transaction_Detail(string order_number, string gubun, string trade)
        {
            //초기값 세팅
            CpTd5341.SetInputValue(0, acc_text.Text);
            CpTd5341.SetInputValue(1, gubun);
            //CCpTd5341.SetInputValue(2, "");
            //CpTd5341.SetInputValue(3, "");
            CpTd5341.SetInputValue(4, '1'); //정렬 '1' 역순
            CpTd5341.SetInputValue(5, 20);
            CpTd5341.SetInputValue(6, '3');
            //
            int check = 0;
            //
            //수신확인
            while (true)
            {
                if (CpTd5341.GetDibStatus() == 1)
                {
                    check++;
                    WriteLog_System("[DibRq요청/수신대기/5초] : 체결내역업데이트\n");
                    System.Threading.Thread.Sleep(5000);
                }
                else if (check == 5)
                {
                    break;
                }
                else
                {
                    break;
                }
            }
            //
            if (check == 5)
            {
                WriteLog_System("[체결내역업데이트/수신실패] : 재부팅 요망\n");
                telegram_message("[체결내역업데이트/수신실패] : 재부팅 요망\n");
                return;
            }
            //정상조회확인
            int result = CpTd5341.BlockRequest();
            //
            if (result == 0)
            {
                int count = Convert.ToInt32(CpTd5341.GetHeaderValue(6));
                if (count == 0 && trade.Equals("매수") || trade.Equals("매도"))
                {
                    WriteLog_System($"[체결내역업데이트/{gubun}] : 실매입 가격 업데이트 오류(체결내역없음)\n");
                    return;
                }

                WriteLog_System($"[체결내역업데이트/{gubun}] : 성공\n");
                //
                for (int i = 0; i < count; i++)
                {
                    string transaction_number = Convert.ToString(CpTd5341.GetDataValue(1, i)).Trim(); //주문번호 => long
                    string average_price = string.Format("{0:#,##0}", Convert.ToDecimal(CpTd5341.GetDataValue(11, i))); // 체결단가 => long
                    string gubun2 = CpTd5341.GetDataValue(35, i) == "1" ? "매도" : "매수"; //매매구분(매도,매수) => string
                    string order_sum = Convert.ToString(CpTd5341.GetDataValue(9, i)); //총체결수량 => long

                    //매매완료 후 실제 편입가 업데이트
                    if (transaction_number.Equals(order_number))
                    {
                        var findRows2 = dtCondStock.AsEnumerable().Where(row => row.Field<string>("주문번호") == order_number);
                        //
                        if (findRows2.Any())
                        {
                            DataRow row = findRows2.First();
                            if (trade.Equals("매수"))
                            {
                                row["편입상태"] = "실매입";
                                row["편입가"] = average_price;
                                //
                                if (utility.profit_ts)
                                {
                                    row["상태"] = "TS매수완료";
                                    row["편입최고"] = average_price;
                                }
                                else
                                {
                                    row["상태"] = "매수완료";
                                }
                                //
                                WriteLog_Order($"[매수주문/정상완료/{gubun}] : {row["종목명"]}({row["종목코드"]}) {order_sum}개 {average_price}원\n");
                                telegram_message($"[매수주문/정상완료/{gubun}] : {row["종목명"]}({row["종목코드"]}) {order_sum}개 {average_price}원\n");
                            }
                            else
                            {
                                if (!utility.duplication_deny)
                                {
                                    row["상태"] = "대기";
                                }
                                else
                                {
                                    row["상태"] = "매도완료";
                                }
                                //
                                row["매도가"] = average_price;
                                //
                                WriteLog_Order($"[매도주문/정상완료/{gubun}] : {row["종목명"]}({row["종목코드"]}) {order_sum}개 {average_price}원\n");
                                telegram_message($"[매도주문/정상완료/{gubun}] : {row["종목명"]}({row["종목코드"]}) {order_sum}개 {average_price}원\n");
                            }
                        }

                        //테이블 반영
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }
                    }

                    //기본동작
                    dtCondStock_Transaction.Rows.Add(
                        CpTd5341.GetDataValue(0, i), //상품관리구분코드 => string
                        CpTd5341.GetDataValue(3, i), //종목코드 => string
                        CpTd5341.GetDataValue(4, i), //종목명 => string
                        transaction_number,
                        gubun2,
                        CpTd5341.GetDataValue(6, i), //주문호가구분(01보통, 03시장가) => string
                        Convert.ToString(CpTd5341.GetDataValue(7, i)), //주문수량 => long
                        order_sum, //총체결수량 => long
                        average_price
                    );
                }
                /*
                dtCondStock_Transaction.AcceptChanges();
                dataGridView3.DataSource = dtCondStock_Transaction;
                */
                if (dataGridView3.InvokeRequired)
                {
                    dataGridView3.Invoke((MethodInvoker)delegate {
                        dataGridView3.DataSource = dtCondStock_Transaction;
                        dataGridView3.Refresh();
                    });
                }
                else
                {
                    dataGridView3.DataSource = dtCondStock_Transaction;
                    dataGridView3.Refresh();
                }

            }
            else
            {
                WriteLog_Order($"[체결내역/수신실패] : {error_message(result)} / {CpTd5341.GetDibMsg1()}\n");
            }
        }

        //------------------------------------인덱스 목록 받기---------------------------------     
        
        private void Index_load()
        {         
            US_INDEX();

            System.Threading.Thread.Sleep(200);

            if (utility.kospi_commodity || utility.kosdak_commodity)
            {
                Initial_kor_index();
            }
        }

        private bool index_buy = false;
        private bool index_clear = false;
        private bool index_dual = false;

        private bool index_run = false;

        private bool index_stop = false;
        private bool index_skip = false;

        //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=284&seq=12&page=1&searchString=CpUtil&p=8841&v=8643&m=9505
        //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=284&seq=91&page=2&searchString=%ec%a7%80%ec%88%98&p=8841&v=8643&m=9505
        private void US_INDEX()
        {
            //.DJI SPX COMP
            /*
            var codes = CpUsCode.GetUsCodeList(USTYPE.USTYPE_COUNTRY);
            foreach(string tmp in codes)
            {
                WriteLog_Order(tmp+"\n");
            }
            */

            //다우존스
            if (utility.dow_index)
            {
                CpFore8312.SetInputValue(0, ".DJI");
                CpFore8312.SetInputValue(1, '2');
                CpFore8312.SetInputValue(2, 2);
                //
                int check = 0;
                //
                //수신확인
                while (true)
                {
                    if (CpFore8312.GetDibStatus() == 1)
                    {
                        check++;
                        WriteLog_System("[DibRq요청/수신대기/5초] : DOW30\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check == 5)
                {
                    WriteLog_System("[DOW30/수신실패] : 재부팅 요망\n");
                    telegram_message("[DOW30/수신실패] : 재부팅 요망\n");
                    return;
                }
                int result = CpFore8312.BlockRequest();
                //
                if (result == 0)
                {
                    //string tmp = CpFore8312.GetHeaderValue(0);//해외지수코드 => string
                    //string tmp3 = CpFore8312.GetHeaderValue(3);//심볼명 => string
                    //float tmp4 = CpFore8312.GetHeaderValue(4);//현재가 => long
                    float tmp5 = CpFore8312.GetHeaderValue(6);//등락률 => float
                    string tmp6 = Convert.ToString(CpFore8312.GetHeaderValue(13));//거래일자 => long //20240607
                    //WriteLog_System($"{tmp}/{tmp3}/{tmp4.ToString()}/{tmp5.ToString()}/{tmp6.ToString()}\n");

                    dow_index.Text = tmp5.ToString();
                    //
                    if (!index_run) index_stop_skip(tmp6);
                    //
                    if (index_skip) WriteLog_System("[DOW30/SKIP] : 미국 전영업일 휴무\n");

                    if (!index_skip && utility.buy_condition_index)
                    {
                        if (utility.type3_selection)
                        {
                            double start = Convert.ToDouble(utility.type3_start);
                            double end = Convert.ToDouble(utility.type3_end);
                            if(tmp5 < start || end < tmp5)
                            {
                                index_buy = true;
                                WriteLog_System($"[BUY/이탈] DOW30 RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[BUY/이탈] DOW30 RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if(!index_skip && utility.clear_index)
                    {
                        if (utility.type3_selection_all)
                        {
                            double start = Convert.ToDouble(utility.type3_start_all);
                            double end = Convert.ToDouble(utility.type3_end_all);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_clear = true;
                                WriteLog_System($"[CLEAR/이탈] DOW30 RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[CLEAR/이탈] DOW30 RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (!index_skip && utility.Dual_Index)
                    {
                        if (utility.type3_selection_isa)
                        {
                            double start = Convert.ToDouble(utility.type3_start_isa);
                            double end = Convert.ToDouble(utility.type3_end_isa);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_dual = true;
                                WriteLog_System($"[DUAL/이탈] DOW30 RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[DUAL/이탈] DOW30 RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }
                }
                else
                {
                    WriteLog_Order($"[DOW30/수신실패] : {error_message(result)} / {CpFore8312.GetDibMsg1()}\n");
                }
            }

            System.Threading.Thread.Sleep(200);

            //S&P500
            if (utility.sp_index)
            {
                CpFore8312.SetInputValue(0, "SPX");
                CpFore8312.SetInputValue(1, '2');
                CpFore8312.SetInputValue(2, 2);
                //
                int check = 0;
                //
                //수신확인
                while (true)
                {
                    if (CpFore8312.GetDibStatus() == 1)
                    {
                        check++;
                        WriteLog_System("[DibRq요청/수신대기/5초] : S&P500\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check == 5)
                {
                    WriteLog_System("[S&P500/수신실패] : 재부팅 요망\n");
                    telegram_message("[S&P500/수신실패] : 재부팅 요망\n");
                    return;
                }
                //
                int result2 = CpFore8312.BlockRequest();
                //
                if (result2 == 0)
                {
                    //string tmp = CpFore8312.GetHeaderValue(0);//해외지수코드 => string
                    //string tmp3 = CpFore8312.GetHeaderValue(3);//심볼명 => string
                    //float tmp4 = CpFore8312.GetHeaderValue(4);//현재가 => long
                    float tmp5 = CpFore8312.GetHeaderValue(6);//등락률 => float
                    string tmp6 = Convert.ToString(CpFore8312.GetHeaderValue(13));//거래일자 => long //20240607
                    //WriteLog_System($"{tmp}/{tmp3}/{tmp4.ToString()}/{tmp5.ToString()}/{tmp6.ToString()}\n");

                    sp_index.Text = tmp5.ToString();
                    //
                    if (!index_run) index_stop_skip(tmp6);
                    //
                    if (index_skip) WriteLog_System("[S&P500/SKIP] : 미국 전영업일 휴무\n");

                    if (!index_skip && utility.buy_condition_index)
                    {
                        if (utility.type4_selection)
                        {
                            double start = Convert.ToDouble(utility.type4_start);
                            double end = Convert.ToDouble(utility.type4_end);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_buy = true;
                                WriteLog_System($"[BUY/이탈] S&P500 RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[BUY/이탈] S&P500 RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (!index_skip && utility.clear_index)
                    {
                        if (utility.type4_selection_all)
                        {
                            double start = Convert.ToDouble(utility.type4_start_all);
                            double end = Convert.ToDouble(utility.type4_end_all);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_clear = true;
                                WriteLog_System($"[CLEAR/이탈] S&P500 RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[CLEAR/이탈] S&P500 RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (!index_skip && utility.Dual_Index)
                    {
                        if (utility.type4_selection_isa)
                        {
                            double start = Convert.ToDouble(utility.type4_start_isa);
                            double end = Convert.ToDouble(utility.type4_end_isa);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_dual = true;
                                WriteLog_System($"[DUAL/이탈] S&P500 RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[DUAL/이탈] S&P500 RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }
                }
                else
                {
                    WriteLog_Order($"[S&P500/수신실패] : {error_message(result2)} / {CpFore8312.GetDibMsg1()}\n");
                }
            }

            System.Threading.Thread.Sleep(200);

            //NASDAQ100
            if (utility.nasdaq_index)
            {
                CpFore8312.SetInputValue(0, "COMP");
                CpFore8312.SetInputValue(1, '2');
                CpFore8312.SetInputValue(2, 2);
                //
                int check = 0;
                //
                //수신확인
                while (true)
                {
                    if (CpFore8312.GetDibStatus() == 1)
                    {
                        check++;
                        WriteLog_System("[DibRq요청/수신대기/5초] : NASDAQ1000\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check == 5)
                {
                    WriteLog_System("[NASDAQ100/수신실패] : 재부팅 요망\n");
                    telegram_message("[NASDAQ100/수신실패] : 재부팅 요망\n");
                    return;
                }
                //
                int result3 = CpFore8312.BlockRequest();
                //
                if (result3 == 0)
                {
                    //string tmp = CpFore8312.GetHeaderValue(0);//해외지수코드 => string
                    //string tmp3 = CpFore8312.GetHeaderValue(3);//심볼명 => string
                    //float tmp4 = CpFore8312.GetHeaderValue(4);//현재가 => long
                    float tmp5 = CpFore8312.GetHeaderValue(6);//등락률 => float
                    string tmp6 = Convert.ToString(CpFore8312.GetHeaderValue(13));//거래일자 => long //20240607
                    //WriteLog_System($"{tmp}/{tmp3}/{tmp4.ToString()}/{tmp5.ToString()}/{tmp6.ToString()}\n");

                    nasdaq_index.Text = tmp5.ToString();
                    //
                    if (!index_run) index_stop_skip(tmp6);
                    //
                    if (index_skip) WriteLog_System("[NASDAQ100/SKIP] : 미국 전영업일 휴무\n");

                    if (!index_skip && utility.buy_condition_index)
                    {
                        if (utility.type5_selection)
                        {
                            double start = Convert.ToDouble(utility.type5_start);
                            double end = Convert.ToDouble(utility.type5_end);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_buy = true;
                                WriteLog_System($"[BUY/이탈] NASDAQ RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[BUY/이탈] NASDAQ RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (!index_skip && utility.clear_index)
                    {
                        if (utility.type5_selection_all)
                        {
                            double start = Convert.ToDouble(utility.type5_start_all);
                            double end = Convert.ToDouble(utility.type5_end_all);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_clear = true;
                                WriteLog_System($"[CLEAR/이탈] NASDAQ RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[CLEAR/이탈] NASDAQ RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (!index_skip && utility.Dual_Index)
                    {
                        if (utility.type5_selection_isa)
                        {
                            double start = Convert.ToDouble(utility.type5_start_isa);
                            double end = Convert.ToDouble(utility.type5_end_isa);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_dual = true;
                                WriteLog_System($"[DUAL/이탈] NASDAQ RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[DUAL/이탈] NASDAQ RANGE : START({start}) <=  NOW({tmp5}) <= END({end})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }
                }
                else
                {
                    WriteLog_Order($"[NASDAQ/수신실패] : {error_message(result3)} / {CpFore8312.GetDibMsg1()}\n");
                }
            }
        }

        private void index_stop_skip(string date)
        {
            if (utility.Foreign_Stop || utility.Foreign_Skip)
            {
                if (!Thread.CurrentThread.CurrentCulture.Name.Equals("ko-KR"))
                {
                    WriteLog_System("시스템 언어 한국어 변경 요망\n");
                    return;
                }
                //
                if (DateTime.TryParseExact(date, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime givenDate))
                {
                    index_run = true;

                    //날짜 추출
                    string today_week = DateTime.Now.ToString("ddd");

                    //현재날짜(시간 부분 제외)
                    DateTime currentDate = DateTime.Now.Date;

                    //날짜 차이 계산
                    TimeSpan difference = currentDate - givenDate;

                    //월요일
                    if (today_week.Equals("월") && Math.Abs(difference.Days) > 3)
                    {
                        if (utility.Foreign_Stop) index_stop = true;
                        if (utility.Foreign_Skip) index_skip = true;
                        WriteLog_System("미국장 전영업일 휴무\n");
                        telegram_message("미국장 전영업일 휴무\n");
                    }
                    else if (!today_week.Equals("월") && Math.Abs(difference.Days) > 1)
                    {
                        if (utility.Foreign_Stop) index_stop = true;
                        if (utility.Foreign_Skip) index_skip = true;
                        WriteLog_System("미국장 전영업일 휴무\n");
                        telegram_message("미국장 전영업일 휴무\n");
                    }
                }
                else
                {
                    WriteLog_System("날짜 형식 변경 : 개발자 문의 요망\n");
                }
            }
        }

        private string index_time = DateTime.Now.ToString("yyyyMMdd");
        private int[] items = { 0, 1, 4, 5 }; //날짜,시간,저가,종가
        private string sCode1 = "";
        private string sCode2 = "";
        private string sKCode1 = "";
        private string sKCode2 = "";

        //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=284&seq=6&page=5&searchString=%ec%84%a0%eb%ac%bc&p=8841&v=8643&m=9505
        private void Initial_kor_index()
        {
            //월물확인
            sCode1 = cpFuture.GetData(0, (short)0);
            string sName1 = cpFuture.GetData(1, (short)0);
            WriteLog_System($"코스피최근월물 : {sCode1}/{sName1}\n");

            sCode2 = cpFuture.GetData(0, (short)2);
            string sName2 = cpFuture.GetData(1, (short)2);
            WriteLog_System($"코스피다음월물 : {sCode2}/{sName2}\n");

            sKCode1 = cpKFuture.GetData(0, (short)0);
            string sKName1 = cpKFuture.GetData(1, (short)0);
            WriteLog_System($"코스닥최근월물 : {sKCode1}/{sKName1}\n");

            sKCode2 = cpKFuture.GetData(0, (short)2);
            string sKName2 = cpKFuture.GetData(1, (short)2);
            WriteLog_System($"코스닥다음월물 : {sKCode2}/{sKName2}\n");

            Index_timer();
        }

        private System.Timers.Timer minuteTimer;

        private void Index_timer()
        {
            // 현재 시간을 기준으로 다음 분의 첫 번째 초까지의 시간을 계산
            DateTime now = DateTime.Now;

            // 다음 분의 00초를 계산
            DateTime nextMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
            double intervalToNextMinute = (nextMinute - now).TotalMilliseconds;

            // 첫 번째 타이머를 설정하여 다음 분 00초에 실행
            minuteTimer = new System.Timers.Timer(intervalToNextMinute);
            minuteTimer.Elapsed += (sender, e) =>
            {
                // 타이머를 중지하고 해제
                minuteTimer.Stop();
                minuteTimer.Dispose();

                // 매 1분마다 실행되는 타이머 설정
                StartMinuteTimer();

                // 특정 함수 호출
                KOR_INDEX();
            };
            minuteTimer.AutoReset = false;
            minuteTimer.Start();

            // 특정 함수 호출
            KOR_INDEX();
        }

        private void StartMinuteTimer()
        {
            minuteTimer = new System.Timers.Timer(60000); // 1분 = 60,000 밀리초
            minuteTimer.Elapsed += OnTimedEvent;
            minuteTimer.AutoReset = true;
            minuteTimer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            KOR_INDEX();
        }

        private double[] kospi_index_series = new double[3];
        private double[] kosdaq_index_series = new double[3];

        private void KOR_INDEX()
        {
            //FOREIGNER
            //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=284&seq=146&page=2&searchString=%ec%98%b5%ec%85%98&p=&v=&m=
            if (utility.Foreign_commodity)
            {
                CpSvrNew7224.SetInputValue(0, '1');
                CpSvrNew7224.SetInputValue(1, 'D');
                CpSvrNew7224.SetInputValue(2, 2);
                CpSvrNew7224.SetInputValue(3, '2');
                CpSvrNew7224.SetInputValue(4, '1');
                CpSvrNew7224.SetInputValue(5, 5);
                CpSvrNew7224.SetInputValue(6, Convert.ToInt32(index_time));
                CpSvrNew7224.SetInputValue(7, Convert.ToInt32(index_time));
                CpSvrNew7224.SetInputValue(8, '1');
                //
                int check = 0;
                //
                //수신확인
                while (true)
                {
                    if (CpSvrNew7224.GetDibStatus() == 1)
                    {
                        check++;
                        WriteLog_System("[DibRq요청/수신대기/5초] : 외국인선물\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check == 5)
                {
                    WriteLog_System("[외국인선물/수신실패] : 재부팅 요망\n");
                    telegram_message("[외국인선물/수신실패] : 재부팅 요망\n");
                    return;
                }
                //
                int result = CpSvrNew7224.BlockRequest();
                //
                if (CpSvrNew7224.GetDibStatus() == 1)
                {
                    string status_message = CpFore8312.GetDibMsg1();
                    WriteLog_System($"[선물/수신실패] : DibRq 요청 수신대기(60초후 재시도) - {status_message}\n");
                    telegram_message("[선물/수신실패] : DibRq 요청 수신대기(60초후 재시도)\n");
                    return;
                }
                //
                if (result == 0)
                {
                    string tmp6 = Convert.ToString(CpSvrNew7224.GetDataValue(0, 0)); //일자(long)
                    string tmp7 = Convert.ToString(CpSvrNew7224.GetDataValue(1, 0)); //매도수량(long)
                    string tmp8 = Convert.ToString(CpSvrNew7224.GetDataValue(5, 0)); //매수수량(long)
                    long tmp9 = Convert.ToInt32(CpSvrNew7224.GetDataValue(9, 0)); //순매수수량(long)

                    //8시 45분전에 수신시 혹은 최초 수신시 0값이 나오는 경우가 있음
                    if (tmp6 == "0" || tmp7 == "0" || tmp8 == "0" || tmp9 == 0)
                    {
                        WriteLog_System($"[수신오류] 외국인 선물 누적 : 일자({tmp6}), 매도수량({tmp7}), 매수수량({tmp8}), 순매수수량({tmp9})\n");
                        telegram_message($"[수신오류] 외국인 선물 누적  : 60초 뒤 재시도\n");
                        return;
                    }

                    Foreign.Text = tmp9.ToString();

                    if (utility.buy_condition_index)
                    {
                        if (utility.type0_selection && !index_buy)
                        {
                            double start = Convert.ToDouble(utility.type0_start);
                            double end = Convert.ToDouble(utility.type0_end);
                            if (tmp9 < start || end < tmp9)
                            {
                                index_buy = true;
                                WriteLog_System($"[BUY/이탈] FOREIGN RANGE : START({start}) <=  NOW({tmp9}) <= END({end})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[BUY/이탈] FOREIGN RANGE : START({start}) <=  NOW({tmp9}) <= END({end})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (utility.clear_index)
                    {
                        if (utility.type0_selection_all && !index_clear)
                        {
                            double start = Convert.ToDouble(utility.type0_start_all);
                            double end = Convert.ToDouble(utility.type0_end_all);
                            if (tmp9 < start || end < tmp9)
                            {
                                index_clear = true;
                                WriteLog_System($"[CLEAR/이탈] FOREIGNRANGE : START({start}) <=  NOW({tmp9}) <= END({end})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[CLEAR/이탈] FOREIGN RANGE : START({start}) <=  NOW({tmp9}) <= END({end})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (utility.Dual_Index)
                    {
                        if (utility.type0_selection_isa && !index_dual)
                        {
                            double start = Convert.ToDouble(utility.type0_start_isa);
                            double end = Convert.ToDouble(utility.type0_end_isa);
                            if (tmp9 < start || end < tmp9)
                            {
                                index_dual = true;
                                WriteLog_System($"[DUAL/이탈] FOREIGN RANGE : START({start}) <=  NOW({tmp9}) <= END({end})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[DUAL/이탈] FOREIGN RANGE : START({start}) <=  NOW({tmp9}) <= END({end})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }
                }
                else
                {
                    WriteLog_Order($"[외국인선물/수신실패] : {error_message(result)} / {CpSvrNew7224.GetDibMsg1()}\n");
                }
            }

            System.Threading.Thread.Sleep(200);

            //KOSPI 200 FUTURES
            //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=284&seq=105&page=3&searchString=%ec%84%a0%eb%ac%bc&p=8841&v=8643&m=9505
            if (utility.kospi_commodity)
            {
                FutOptChart.SetInputValue(0, sCode1); //종목코드
                FutOptChart.SetInputValue(1, '2'); //요청구분 1 기간데이터 2 개수 데이터
                                                   //FutOptChart.SetInputValue(2,); //요청종료일 시간 요청인 경우 입력
                FutOptChart.SetInputValue(3, Convert.ToInt32(index_time)); //요청시작일
                FutOptChart.SetInputValue(4, 1); //요청개수
                FutOptChart.SetInputValue(5, items); //필드배열
                FutOptChart.SetInputValue(6, 'D'); //차트구분
                FutOptChart.SetInputValue(7, (short)1); //주기?
                FutOptChart.SetInputValue(8, '0'); //갭보정여부
                FutOptChart.SetInputValue(9, '0'); //수정주가
                //
                int check = 0;
                //
                //수신확인
                while (true)
                {
                    if (FutOptChart.GetDibStatus() == 1)
                    {
                        check++;
                        WriteLog_System("[DibRq요청/수신대기/5초] : KOSPI200\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check == 5)
                {
                    WriteLog_System("[KOSPI200/수신실패] : 재부팅 요망\n");
                    telegram_message("[KOSPI200/수신실패] : 재부팅 요망\n");
                    return;
                }
                //                                   //
                int reuslt_kospi = FutOptChart.BlockRequest();
                //
                if (reuslt_kospi == 0)
                {
                    //string tmp = FutOptChart.GetHeaderValue(0);//종목코드
                    //int tmp1 = FutOptChart.GetHeaderValue(1);//필드개수
                    //string[] tmp2 = FutOptChart.GetHeaderValue(2);//필드명
                    //int tmp3 = FutOptChart.GetHeaderValue(3);//수신개수
                    float tmp4 = FutOptChart.GetHeaderValue(6);//전일종가
                    float tmp5 = FutOptChart.GetHeaderValue(7);//현재가
                    float tmp6 = FutOptChart.GetHeaderValue(14);//금일저가
                    float tmp7 = FutOptChart.GetHeaderValue(13);//금일고가

                    //8시 45분전에 수신시 혹은 최초 수신시 0값이 나오는 경우가 있음
                    if (tmp4 == 0 || tmp5 == 0 || tmp6 == 0 || tmp7 == 0)
                    {
                        WriteLog_System($"[수신오류] KOSPI200 : 전일종가({tmp4}), 종가({tmp5}), 저가({tmp6}), 고가({tmp7})\n");
                        telegram_message($"[수신오류] KOSPI200 : 60초 뒤 재시도\n");
                        return;
                    }

                    //저가,종가,고가
                    kospi_index_series[0] = Math.Round((tmp6 - tmp4) / tmp4 * 100, 2); //저가
                    kospi_index_series[1] = Math.Round((tmp5 - tmp4) / tmp4 * 100, 2); //현재가
                    kospi_index_series[2] = Math.Round((tmp7 - tmp4) / tmp4 * 100, 2); //고가

                    //this.Invoke((MethodInvoker)delegate

                    kospi_index.Text = String.Format($"L({kospi_index_series[0]})/H({kospi_index_series[2]})");
                    //WriteLog_System($"{tmp}/{tmp1}/{tmp3}/{tmp4.ToString()}/{tmp5.ToString()}/{tmp6.ToString()}/{tmp7.ToString()}\n");

                    if (utility.buy_condition_index)
                    {
                        if (utility.type1_selection && !index_buy)
                        {
                            double start = Convert.ToDouble(utility.type1_start);
                            double end = Convert.ToDouble(utility.type1_end);
                            if (kospi_index_series[0] < start || end < kospi_index_series[2])
                            {
                                WriteLog_System($"[Buy/이탈] KOSPI200 RANGE\n");
                                WriteLog_System($"SET_LOW({start}) <= LOW({kospi_index_series[0]})\n");
                                WriteLog_System($"HIGH({kospi_index_series[2]}) <= SET_HIGH({end})\n");
                                WriteLog_System("Trade Stop\n");

                                telegram_message($"[Buy/이탈] KOSPI200 RANGE\n");
                                telegram_message($"SET_LOW({start}) <= LOW({kospi_index_series[0]})\n");
                                telegram_message($"HIGH({kospi_index_series[2]}) <= SET_HIGH({end})\n");
                                telegram_message("Trade Stop\n");

                                index_buy = true;
                            }
                        }
                    }

                    if (utility.clear_index)
                    {
                        if (utility.type1_selection_all && !index_clear)
                        {
                            double start = Convert.ToDouble(utility.type1_start_all);
                            double end = Convert.ToDouble(utility.type1_end_all);
                            if (kospi_index_series[0] < start || end < kospi_index_series[2])
                            {
                                WriteLog_System($"[CLEAR/이탈] KOSPI200 RANGE\n");
                                WriteLog_System($"SET_LOW({start}) <= LOW({kospi_index_series[0]})\n");
                                WriteLog_System($"HIGH({kospi_index_series[2]}) <= SET_HIGH({end})\n");
                                WriteLog_System("Trade Stop\n");

                                telegram_message($"[CLEAR/이탈] KOSPI200 RANGE\n");
                                telegram_message($"SET_LOW({start}) <= LOW({kospi_index_series[0]})\n");
                                telegram_message($"HIGH({kospi_index_series[2]}) <= SET_HIGH({end})\n");
                                telegram_message("Trade Stop\n");

                                index_clear = true;
                            }
                        }
                    }

                    if (utility.Dual_Index)
                    {
                        if (utility.type1_selection_isa && !index_dual)
                        {
                            double start = Convert.ToDouble(utility.type1_start_isa);
                            double end = Convert.ToDouble(utility.type1_end_isa);
                            if (kospi_index_series[0] < start || end < kospi_index_series[2])
                            {
                                WriteLog_System($"[DUAL/이탈] KOSPI200 RANGE\n");
                                WriteLog_System($"SET_LOW({start}) <= LOW({kospi_index_series[0]})\n");
                                WriteLog_System($"HIGH({kospi_index_series[2]}) <= SET_HIGH({end})\n");
                                WriteLog_System("Trade Stop\n");

                                telegram_message($"[DUAL/이탈] KOSPI200 RANGE\n");
                                telegram_message($"SET_LOW({start}) <= LOW({kospi_index_series[0]})\n");
                                telegram_message($"HIGH({kospi_index_series[2]}) <= SET_HIGH({end})\n");
                                telegram_message("Trade Stop\n");

                                index_dual = true;
                            }
                        }
                    }
                }
                else
                {
                    WriteLog_Order($"[코스피선물/수신실패] : {error_message(reuslt_kospi)} / {FutOptChart.GetDibMsg1()}\n");
                }
            }

            System.Threading.Thread.Sleep(200);

            //KOSDAK 150 FUTURES
            //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=287&seq=9&page=1&searchString=&p=&v=&m=
            if (utility.kosdak_commodity)
            {
                FutOptChart.SetInputValue(0, sKCode1); //종목코드
                FutOptChart.SetInputValue(1, '2'); //요청구분 1 기간데이터 2 개수 데이터
                                                   //FutOptChart.SetInputValue(2,); //요청종료일 시간 요청인 경우 입력
                FutOptChart.SetInputValue(3, Convert.ToInt32(index_time)); //요청시작일
                FutOptChart.SetInputValue(4, 1); //요청개수
                FutOptChart.SetInputValue(5, items); //필드배열
                FutOptChart.SetInputValue(6, 'D'); //차트구분
                FutOptChart.SetInputValue(7, (short)1); //주기?
                FutOptChart.SetInputValue(8, '0'); //갭보정여부
                FutOptChart.SetInputValue(9, '0'); //수정주가
                //
                int check = 0;
                //
                //수신확인
                while (true)
                {
                    if (FutOptChart.GetDibStatus() == 1)
                    {
                        check++;
                        WriteLog_System("[DibRq요청/수신대기/5초] : KOSDAK150\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check == 5)
                {
                    WriteLog_System("[KOSDAK150/수신실패] : 재부팅 요망\n");
                    telegram_message("[KOSDAK150/수신실패] : 재부팅 요망\n");
                    return;
                }
                // 
                int reuslt_kosdask = FutOptChart.BlockRequest();
                //
                if (reuslt_kosdask == 0)
                {
                    //string tmp = FutOptChart.GetHeaderValue(0);//종목코드
                    //int tmp1 = FutOptChart.GetHeaderValue(1);//필드개수
                    //string[] tmp2 = FutOptChart.GetHeaderValue(2);//필드명
                    //int tmp3 = FutOptChart.GetHeaderValue(3);//수신개수
                    float tmp4 = FutOptChart.GetHeaderValue(6);//전일종가
                    float tmp5 = FutOptChart.GetHeaderValue(7);//현재가
                    float tmp6 = FutOptChart.GetHeaderValue(14);//금일저가
                    float tmp7 = FutOptChart.GetHeaderValue(13);//금일고가

                    //8시 45분전에 수신시 혹은 최초 수신시 0값이 나오는 경우가 있음
                    if (tmp4 == 0 || tmp5 == 0 || tmp6 == 0 || tmp7 == 0)
                    {
                        WriteLog_System($"[수신오류] KOSDAK150 : 전일종가({tmp4}), 종가({tmp5}), 저가({tmp6}), 고가({tmp7})\n");
                        telegram_message($"[수신오류] KOSDAK150 : 60초 뒤 재시도\n");
                        return;
                    }

                    //저가,종가,고가
                    kosdaq_index_series[0] = Math.Round((tmp6 - tmp4) / tmp4 * 100, 2); //저가
                    kosdaq_index_series[1] = Math.Round((tmp5 - tmp4) / tmp4 * 100, 2); //종가
                    kosdaq_index_series[2] = Math.Round((tmp7 - tmp4) / tmp4 * 100, 2); //고가

                    //this.Invoke((MethodInvoker)delegate
                    kosdaq_index.Text = String.Format($"L({kosdaq_index_series[0]})/H({kosdaq_index_series[2]})");
                    //WriteLog_System($"{tmp}/{tmp1}/{tmp3}/{tmp4.ToString()}/{tmp5.ToString()}/{tmp6.ToString()}/{tmp7.ToString()}\n");

                    if (utility.buy_condition_index)
                    {
                        if (utility.type2_selection && !index_buy)
                        {
                            double start = Convert.ToDouble(utility.type2_start);
                            double end = Convert.ToDouble(utility.type2_end);
                            if (kosdaq_index_series[0] < start || end < kosdaq_index_series[2])
                            {
                                WriteLog_System($"[Buy/이탈] KOSDAK150 RANGE\n");
                                WriteLog_System($"SET_LOW({start}) <= LOW({kosdaq_index_series[0]})\n");
                                WriteLog_System($"HIGH({kosdaq_index_series[2]}) <= SET_HIGH({end})\n");
                                WriteLog_System("Trade Stop\n");

                                telegram_message($"[Buy/이탈] KOSDAK150 RANGE\n");
                                telegram_message($"SET_LOW({start}) <= LOW({kosdaq_index_series[0]})\n");
                                telegram_message($"HIGH({kosdaq_index_series[2]}) <= SET_HIGH({end})\n");
                                telegram_message("Trade Stop\n");

                                index_buy = true;
                            }
                        }
                    }

                    if (utility.clear_index)
                    {
                        if (utility.type2_selection_all && !index_clear)
                        {
                            double start = Convert.ToDouble(utility.type2_start_all);
                            double end = Convert.ToDouble(utility.type2_end_all);
                            if (kosdaq_index_series[0] < start || end < kosdaq_index_series[2])
                            {
                                WriteLog_System($"[Clear/이탈] KOSDAK150 RANGE\n");
                                WriteLog_System($"SET_LOW({start}) <= LOW({kosdaq_index_series[0]})\n");
                                WriteLog_System($"HIGH({kosdaq_index_series[2]}) <= SET_HIGH({end})\n");
                                WriteLog_System("Trade Stop\n");

                                telegram_message($"[Clear/이탈] KOSDAK150 RANGE\n");
                                telegram_message($"SET_LOW({start}) <= LOW({kosdaq_index_series[0]})\n");
                                telegram_message($"HIGH({kosdaq_index_series[2]}) <= SET_HIGH({end})\n");
                                telegram_message("Trade Stop\n");

                                index_clear = true;
                            }
                        }
                    }

                    if (utility.Dual_Index)
                    {
                        if (utility.type2_selection_isa && !index_dual)
                        {
                            double start = Convert.ToDouble(utility.type2_start_isa);
                            double end = Convert.ToDouble(utility.type2_end_isa);
                            if (kosdaq_index_series[0] < start || end < kosdaq_index_series[2])
                            {
                                WriteLog_System($"[DUAL/이탈] KOSDAK150 RANGE\n");
                                WriteLog_System($"SET_LOW({start}) <= LOW({kosdaq_index_series[0]})\n");
                                WriteLog_System($"HIGH({kosdaq_index_series[2]}) <= SET_HIGH({end})\n");
                                WriteLog_System("Trade Stop\n");

                                telegram_message($"[DUAL/이탈] KOSDAK150 RANGE\n");
                                telegram_message($"SET_LOW({start}) <= LOW({kosdaq_index_series[0]})\n");
                                telegram_message($"HIGH({kosdaq_index_series[2]}) <= SET_HIGH({end})\n");
                                telegram_message("Trade Stop\n");

                                index_dual = true;
                            }
                        }
                    }
                }
                else
                {
                    WriteLog_Order($"[코스닥선물/수신실패] : {error_message(reuslt_kosdask)} / {FutOptChart.GetDibMsg1()}\n");
                }
            }
        }

        //------------------------------------조건식 수신---------------------------------

        //조건식 조회(조건식이 있어야 initial 작동 / initial을 통해 계좌를 받아와야 GetCashInfo)
        class ConditionInfo
        {
            public string Index { get; set; }
            public string Name { get; set; }
            public DateTime? LastRequestTime { get; set; }
        }

        private List<ConditionInfo> conditionInfo = new List<ConditionInfo>();

        private void Condition_load()
        {
            CssStgList.SetInputValue(0, '1');
            //
            int check = 0;
            //
            //수신확인
            while (true)
            {
                if (CssStgList.GetDibStatus() == 1)
                {
                    check++;
                    WriteLog_System("[DibRq요청/수신대기/5초] : 조건식 조회\n");
                    System.Threading.Thread.Sleep(5000);
                }
                else if (check == 5)
                {
                    break;
                }
                else
                {
                    break;
                }
            }
            //
            if (check == 5)
            {
                WriteLog_System("[조건식 조회/수신실패] : 재부팅 요망\n");
                telegram_message("[조건식 조회/수신실패] : 재부팅 요망\n");
                return;
            }
            //
            int result = CssStgList.BlockRequest();
            //
            if (result == 0)
            {
                List<String> condi_tmp = new List<string>();
                //초기화
                conditionInfo.Clear();
                //
                for (int i = 0; i < Convert.ToInt32(CssStgList.GetHeaderValue(0)); i++)
                {
                    string index_tmp = CssStgList.GetDataValue(1, i); //전략ID
                    string name_tmp = CssStgList.GetDataValue(0, i); //전략명
                    conditionInfo.Add(new ConditionInfo
                    {
                        Index = index_tmp,
                        Name = name_tmp
                    });
                    condi_tmp.Add(index_tmp + "^" + name_tmp);
                }
                //
                WriteLog_System("조건식 조회 성공\n");
                arrCondition = condi_tmp.ToArray();
            }
            else
            {
                WriteLog_Order($"[조건식로드/수신실패] : {error_message(result)} / {CssStgList.GetDibMsg1()}\n");
            }
        }

        //------------------------------실시간 실행 초기 점검-------------------------------------

        //초기 매매 설정
        private void auto_allow_check(bool skip)
        {
            //계좌 없으면 이탈
            if (!account.Contains(utility.setting_account_number))
            {
                WriteLog_System("계좌번호 재설정 요청\n");
                telegram_message("계좌번호 재설정 요청\n");
                WriteLog_System("자동 매매 정지\n");
                telegram_message("자동 매매 정지\n");
                return;
            }

            int condition_length = utility.Fomula_list_buy_text.Split(',').Length;

            //조건식 없으면 이탈
            if (utility.buy_condition && condition_length == 0)
            {
                WriteLog_System("설정된 매수 조건식 없음\n");
                telegram_message("설정된 매수 조건식 없음\n");
                WriteLog_System("자동 매매 정지\n");
                telegram_message("자동 매매 정지\n");
                return;
            }

            int condition_length2 = utility.Fomula_list_sell_text == "9999" ? 0 : 1;

            //조건식 없으면 이탈
            if (utility.sell_condition && condition_length2 == 0)
            {
                WriteLog_System("설정된 매도 조건식 없음\n");
                telegram_message("설정된 매도 조건식 없음\n");
                WriteLog_System("자동 매매 정지\n");
                telegram_message("자동 매매 정지\n");
                return;
            }

            //AND 모드에서는 조건식이 2개
            if (utility.buy_AND && condition_length != 2)
            {
                WriteLog_System("AND 모드 조건식 2개 필요\n");
                telegram_message("AND 모드 조건식 2개 필요\n");
                WriteLog_System("자동 매매 정지\n");
                telegram_message("자동 매매 정지\n");
                return;
            }

            //Independent 모드에서는 조건식이 2개
            if (utility.buy_INDEPENDENT && condition_length != 2)
            {
                WriteLog_System("Independent 모드 조건식 2개 필요\n");
                telegram_message("IndependentL 모드 조건식 2개 필요\n");
                WriteLog_System("자동 매매 정지\n");
                telegram_message("자동 매매 정지\n");
                return;
            }

            //Dual 모드에서는 조건식이 2개
            if (utility.buy_DUAL && condition_length != 2)
            {
                WriteLog_System("DUAL 모드 조건식 2개 필요\n");
                telegram_message("DUAL 모드 조건식 2개 필요\n");
                WriteLog_System("자동 매매 정지\n");
                telegram_message("자동 매매 정지\n");
                return;
            }

            //
            if (index_stop && !skip)
            {
                WriteLog_System("미국 전영업일 휴무 : 중단\n");
                telegram_message("미국 전영업일 휴무 : 중단\n");
                WriteLog_System("자동 매매 정지\n");
                telegram_message("자동 매매 정지\n");
                return;
            }

            //자동 설정 여부
            if (!utility.auto_trade_allow && !skip)
            {
                WriteLog_System("자동 매매 실행 미설정\n");
                telegram_message("자동 매매 실행 미설정\n");
                return;
            }

            //계좌 탐색 - 200ms 
            timer2.Start();

            //실시간 편출입 받기
            CssAlert.Subscribe();

            //DUAL 모드에서 계좌별 조건식 설정
            if (utility.buy_DUAL)
            {
                string[] tmp = utility.Fomula_list_buy_text.Split(',');
                ISA_Condition = tmp[1].Split('^')[1];
            }

            //자동 매수 조건식 설정 여부
            if (utility.buy_condition)
            {
                real_time_search(null, EventArgs.Empty);
            }
            else
            {
                WriteLog_System("자동 조건식 매수 미설정\n");
                telegram_message("자동 조건식 매수 미설정\n");
            }

            System.Threading.Thread.Sleep(250);

            //자동 매도 조건식 설정 여부
            if (utility.sell_condition)
            {
                WriteLog_System("실시간 조건식 매도 시작\n");
                telegram_message("실시간 조건식 매도 시작\n");
                normal_search(null, EventArgs.Empty);

            }
            else
            {
                WriteLog_System("자동 조건식 매도 미설정\n");
                telegram_message("자동 조건식 매도 미설정\n");
            }
        }

        //조건식 감시 등록 목록
        private List<CPSYSDIBLib.CssWatchStgControl> Condition_Profile2 = new List<CPSYSDIBLib.CssWatchStgControl>();

        //매도 전용 조건식 검색
        private void normal_search(object sender, EventArgs e)
        {
            //실시간 검색이 시작되면 '일반 검색'이 불가능해 진다.
            Real_time_stop_btn.Enabled = true;
            Real_time_search_btn.Enabled = false;

            //조건식이 로딩되었는지
            if (string.IsNullOrEmpty(utility.Fomula_list_sell_text))
            {
                WriteLog_System("매도 조건식 선택 요청\n");
                telegram_message("매도 조건식 선택 요청\n");
                WriteLog_System("실시간 매매 중단\n");
                telegram_message("실시간 매매 중단\n");
                Real_time_stop_btn.Enabled = false;
                Real_time_search_btn.Enabled = true;
                return;
            }

            //검색된 조건식이 있을시
            string[] condition = utility.Fomula_list_sell_text.Split('^');
            var condInfo = conditionInfo.Find(f => f.Index == condition[0] && f.Name == condition[1]);

            //로드된 조건식 목록에 설정된 조건식이 존재하지 않는 경우 이탈
            if (condInfo == null)
            {
                WriteLog_System("[실시간매도조건식/미존재/" + utility.Fomula_list_sell_text + "] : HTS 조건식 리스트 미포함\n");
                telegram_message("[실시간매도조건식/미존재/" + utility.Fomula_list_sell_text + "] : HTS 조건식 리스트 미포함\n");
                Real_time_stop_btn.Enabled = false;
                Real_time_search_btn.Enabled = true;
                return;
            }

            //조건식에 대한 검색은 60초 마다 가능
            if (condInfo.LastRequestTime != null && condInfo.LastRequestTime >= DateTime.Now.AddSeconds(-60))
            {
                int second = 60 - (DateTime.Now - condInfo.LastRequestTime.Value).Seconds;
                WriteLog_System($"{second}초 후에 조회 가능합니다.\n");
                Real_time_stop_btn.Enabled = false;
                Real_time_search_btn.Enabled = true;
                return;
            }

            //마지막 조건식 검색 시각 업데이트
            condInfo.LastRequestTime = DateTime.Now;

            //실시간 조건식 등록 및 해제
            CPSYSDIBLib.CssWatchStgControl CssWatchStgControl = new CPSYSDIBLib.CssWatchStgControl();
            Condition_Profile2.Add(CssWatchStgControl);

            //일련번호
            int condition_serial = condition_sub_code(condition[0]);

            //종목 검색 요청
            CssWatchStgControl.SetInputValue(0, condition[0]); //전략ID
            CssWatchStgControl.SetInputValue(1, condition_serial); //감시 일련번호
            CssWatchStgControl.SetInputValue(2, '1'); //감시시작
            //
            int check = 0;
            //
            //수신확인
            while (true)
            {
                if (CssWatchStgControl.GetDibStatus() == 1)
                {
                    check++;
                    WriteLog_System("[DibRq요청/수신대기/5초] : 매도 전용 조건식 검색\n");
                    System.Threading.Thread.Sleep(5000);
                }
                else if (check == 5)
                {
                    break;
                }
                else
                {
                    break;
                }
            }
            //
            if (check == 5)
            {
                WriteLog_System("[매도 전용 조건식 검색/수신실패] : 재부팅 요망\n");
                telegram_message("[매도 전용 조건식 검색/수신실패] : 재부팅 요망\n");
                return;
            }
            //
            int result = CssWatchStgControl.BlockRequest();
            //
            if (result == 0)
            {
                int result_type = Convert.ToInt32(CssWatchStgControl.GetHeaderValue(0));
                if (result_type == 0)
                {
                    WriteLog_System($"매도[조건식/등록/{condition[1]}] : 초기상태 \n");
                    telegram_message($"[매도조건식/등록/{condition[1]}] : 초기상태 \n");
                    WriteLog_System($"[매도조건식/등록/{condition[1]}] : 메시지({CssWatchStgControl.GetDibMsg1()})\n");
                }
                else if (result_type == 1)
                {
                    WriteLog_System($"[매도조건식/등록/{condition[1]}] : 감시중 \n");
                    telegram_message($"[매도조건식/등록/{condition[1]}] : 감시중 \n");
                    WriteLog_System($"[매도조건식/등록/{condition[1]}] : 메시지({CssWatchStgControl.GetDibMsg1()})\n");
                }
                else if (result_type == 2)
                {
                    WriteLog_System($"[매도조건식/등록/{condition[1]}] : 감시중단 \n");
                    telegram_message($"[매도조건식/등록/{condition[1]}] : 감시중단 \n");
                    WriteLog_System($"[매도조건식/등록/{condition[1]}] : 메시지({CssWatchStgControl.GetDibMsg1()})\n");
                }
                else
                {
                    WriteLog_System($"[매도조건식/등록/{condition[1]}] : 등록취소 \n");
                    telegram_message($"[매도조건식/등록/{condition[1]}] : 등록취소 \n");
                    WriteLog_System($"[매도조건식/등록/{condition[1]}] : 메시지({CssWatchStgControl.GetDibMsg1()})\n");
                }
            }
            else
            {
                WriteLog_System("[실시간조건식/등록실패/" + utility.Fomula_list_sell_text + "] : 고유번호 및 이름 확인\n");
                telegram_message("[실시간조건식/등록실패/" + utility.Fomula_list_sell_text + "] : 고유번호 및 이름 확인\n");
                WriteLog_System($"[조건식/등록/{condition[1]}] : 메시지({CssWatchStgControl.GetDibMsg1()})\n");
            }

        }

        //조건식 감시 등록 목록
        private List<CPSYSDIBLib.CssWatchStgControl> Condition_Profile = new List<CPSYSDIBLib.CssWatchStgControl>();

        //실시간 검색(조건식 로드 후 사용가능하다)
        private void real_time_search(object sender, EventArgs e)
        {
            //실시간 검색이 시작되면 '일반 검색'이 불가능해 진다.
            Real_time_stop_btn.Enabled = true;
            Real_time_search_btn.Enabled = false;

            //조건식이 로딩되었는지
            if (string.IsNullOrEmpty(utility.Fomula_list_buy_text))
            {
                WriteLog_System("매수 조건식 선택 요청\n");
                telegram_message("매수 조건식 선택 요청\n");
                WriteLog_System("실시간 매매 중단\n");
                telegram_message("실시간 매매 중단\n");
                Real_time_stop_btn.Enabled = false;
                Real_time_search_btn.Enabled = true;
                return;
            }

            foreach (string Fomula in utility.Fomula_list_buy_text.Split(','))
            {
                //검색된 조건식이 있을시
                string[] condition = Fomula.Split('^');
                var condInfo = conditionInfo.Find(f => f.Index == condition[0] && f.Name == condition[1]);

                //로드된 조건식 목록에 설정된 조건식이 존재하지 않는 경우 이탈
                if (condInfo == null)
                {
                    WriteLog_System("[실시간조건식/미존재/" + Fomula + "] : HTS 조건식 리스트 미포함\n");
                    telegram_message("[실시간조건식/미존재/" + Fomula + "] : HTS 조건식 리스트 미포함\n");
                    Real_time_stop_btn.Enabled = false;
                    Real_time_search_btn.Enabled = true;
                    continue;
                }

                //조건식에 대한 검색은 60초 마다 가능
                if (condInfo.LastRequestTime != null && condInfo.LastRequestTime >= DateTime.Now.AddSeconds(-60))
                {
                    int second = 60 - (DateTime.Now - condInfo.LastRequestTime.Value).Seconds;
                    WriteLog_System($"{second}초 후에 조회 가능합니다.\n");
                    Real_time_stop_btn.Enabled = false;
                    Real_time_search_btn.Enabled = true;
                    return;
                }

                //마지막 조건식 검색 시각 업데이트
                condInfo.LastRequestTime = DateTime.Now;

                //실시간 조건식 등록 및 해제
                CPSYSDIBLib.CssWatchStgControl CssWatchStgControl = new CPSYSDIBLib.CssWatchStgControl();
                Condition_Profile.Add(CssWatchStgControl);

                //초기종목받기
                if (stock_initial(condition[0], condition[1])) return;

                System.Threading.Thread.Sleep(250);

                //일련번호
                int condition_serial = condition_sub_code(condition[0]);

                //종목 검색 요청
                CssWatchStgControl.SetInputValue(0, condition[0]); //전략ID
                CssWatchStgControl.SetInputValue(1, condition_serial); //감시 일련번호
                CssWatchStgControl.SetInputValue(2, '1'); //감시시작
                //
                int check = 0;
                //
                //수신확인
                while (true)
                {
                    if (CssWatchStgControl.GetDibStatus() == 1)
                    {
                        check++;
                        WriteLog_System("[DibRq요청/수신대기/5초] : 매수 전용 조건식 검색\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check == 5)
                {
                    WriteLog_System("[매수 전용 조건식 검색/수신실패] : 재부팅 요망\n");
                    telegram_message("[매수 전용 조건식 검색/수신실패] : 재부팅 요망\n");
                    return;
                }
                //
                int result = CssWatchStgControl.BlockRequest();
                //
                if (result == 0)
                {
                    int result_type = Convert.ToInt32(CssWatchStgControl.GetHeaderValue(0));
                    if (result_type == 0)
                    {
                        WriteLog_System($"[조건식/등록/{condition[1]}] : 초기상태 \n");
                        telegram_message($"[조건식/등록/{condition[1]}] : 초기상태 \n");
                        WriteLog_System($"[조건식/등록/{condition[1]}] : 메시지({CssWatchStgControl.GetDibMsg1()})\n");
                    }
                    else if (result_type == 1)
                    {
                        WriteLog_System($"[조건식/등록/{condition[1]}] : 감시중 \n");
                        telegram_message($"[조건식/등록/{condition[1]}] : 감시중 \n");
                        WriteLog_System($"[조건식/등록/{condition[1]}] : 메시지({CssWatchStgControl.GetDibMsg1()})\n");
                    }
                    else if (result_type == 2)
                    {
                        WriteLog_System($"[조건식/등록/{condition[1]}] : 감시중단 \n");
                        telegram_message($"[조건식/등록/{condition[1]}] : 감시중단 \n");
                        WriteLog_System($"[조건식/등록/{condition[1]}] : 메시지({CssWatchStgControl.GetDibMsg1()})\n");
                    }
                    else
                    {
                        WriteLog_System($"[조건식/등록/{condition[1]}] : 등록취소 \n");
                        telegram_message($"[조건식/등록/{condition[1]}] : 등록취소 \n");
                        WriteLog_System($"[조건식/등록/{condition[1]}] : 메시지({CssWatchStgControl.GetDibMsg1()})\n");
                    }
                }
                else
                {
                    WriteLog_System("[실시간조건식/등록실패/" + Fomula + "] : 고유번호 및 이름 확인\n");
                    telegram_message("[실시간조건식/등록실패/" + Fomula + "] : 고유번호 및 이름 확인\n");
                    WriteLog_System($"[조건식/등록/{condition[1]}] : 메시지({CssWatchStgControl.GetDibMsg1()})\n");
                }
            }
        }

        //-----------------------실시간 조건 검색------------------------------

        //초기 종목 검색
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=284&seq=238&page=1&searchString=CssStgFind&p=&v=&m=
        private bool stock_initial(string condition_code, string condition_name)
        {
            CssStgFind.SetInputValue(0, condition_code); //전략ID
            CssStgFind.SetInputValue(1, 'N'); //전략ID
            //
            int check = 0;
            //
            //수신확인
            while (true)
            {
                if (CssStgFind.GetDibStatus() == 1)
                {
                    check++;
                    WriteLog_System("[DibRq요청/수신대기/5초] : 초기 종목 검색\n");
                    System.Threading.Thread.Sleep(5000);
                }
                else if (check == 5)
                {
                    break;
                }
                else
                {
                    break;
                }
            }
            //
            if (check == 5)
            {
                WriteLog_System("[초기 종목 검색/수신실패] : 재부팅 요망\n");
                telegram_message("[초기 종목 검색/수신실패] : 재부팅 요망\n");
                return true;
            }
            //
            int result = CssStgFind.BlockRequest();
            //
            if (result == 0)
            {
                int initial_num = Convert.ToInt32(CssStgFind.GetHeaderValue(1));
                //
                if (initial_num == 0)
                {
                    WriteLog_Stock("[실시간조건식/시작/" + condition_name + "] : 초기 검색 종목 없음\n");
                    telegram_message("[실시간조건식/시작/" + condition_name + "] : 초기 검색 종목 없음\n");
                    return false;
                }
                //
                WriteLog_Stock("[실시간조건식/시작/" + condition_name + "] : 초기 검색 종목 존재\n");
                telegram_message("[실시간조건식/시작/" + condition_name + "] : 초기 검색 종목 존재\n");
                //
                if(initial_num > 50)
                {
                    //
                    WriteLog_Stock("[실시간조건식/중단/" + condition_name + "] : 초기 검색 종목 50개 초과 제한\n");
                    telegram_message("[실시간조건식/시작/" + condition_name + "] : 초기 검색 종목 50개 초과 제한\n");
                    return true;
                }
                //
                //계좌 구분 코드
                string gubun_acc_fresh = Master_code;
                if (utility.buy_DUAL && condition_name.Equals(ISA_Condition))
                {
                    gubun_acc_fresh = ISA_code;
                }
                //
                for (int i = 0; i < initial_num; i++)
                {
                    string code = Convert.ToString(CssStgFind.GetDataValue(0, i));
                    Stock_info(condition_name, code, "0", code, gubun_acc_fresh);
                    System.Threading.Thread.Sleep(250);
                }
            }
            else
            {
                WriteLog_Order($"[초기종목정보/수신실패] : {error_message(result)} / {CssStgFind.GetDibMsg1()}\n");
            }
            return false;
        }

        //------------------------------------종목 정보 받기 및 시세 등록---------------------------------

        //종목 정보 받기(전일, 초기, 편출입)
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=285&seq=131&page=1&searchString=MarketEye&p=&v=&m=
        private void Stock_info(string condition_name, string Code, string hold_num, string order_number, string gubun)
        {
            //
            System.Threading.Thread.Sleep(200);

            //종목코드, 시간, 현재가, 거래량, 종목명, 상한가
            int[] items = {0, 1, 4, 10, 17, 33};
            MarketEye.SetInputValue(0, items);
            MarketEye.SetInputValue(1, Code);
            //
            int check = 0;
            //
            //수신확인
            while (true)
            {
                if (MarketEye.GetDibStatus() == 1)
                {
                    check++;
                    WriteLog_System("[DibRq요청/수신대기/5초] : 종목 정보 받기\n");
                    System.Threading.Thread.Sleep(5000);
                }
                else if (check == 5)
                {
                    break;
                }
                else
                {
                    break;
                }
            }
            //
            if (check == 5)
            {
                WriteLog_System("[종목 정보 받기/수신실패] : 재부팅 요망\n");
                telegram_message("[종목 정보 받기/수신실패] : 재부팅 요망\n");
                return;
            }
            //
            int result = MarketEye.BlockRequest();
            //
            if (result == 0)
            {
                //
                long Native_Price = Convert.ToInt64(MarketEye.GetDataValue(2, 0)); //현재가 => long or float
                string Current_Price = string.Format("{0:#,##0}", Native_Price); //현재가 => long or float
                string time = DateTime.Now.ToString("HH:mm:ss");
                string Code_name = MarketEye.GetDataValue(4, 0); //종목명 => string
                string Status = "매수완료";
                string now_hold = hold_num;
                string high = Convert.ToString(MarketEye.GetDataValue(5, 0));

                //
                DataRow[] findRows1 = dtCondStock.Select($"종목코드 = '{Code}'");

                bool and_mode = false;

                //초기검색 항목 점검용
                if (findRows1.Any() && utility.buy_OR)
                {
                    WriteLog_Stock($"[{condition_name}/편입] : {Code_name}({Code}) OR 모드 중복\n");
                    return;
                }

                if (findRows1.Any() && utility.buy_AND && condition_name != Convert.ToString(findRows1[0]["조건식"]))
                {
                    WriteLog_Stock($"[{condition_name}/편입] : {Code_name}({Code}) AND 모드 중복\n");
                    and_mode = true;
                }
                //
                if (!and_mode)
                {
                    WriteLog_Stock($"[{condition_name}/편입] : {Code_name}({Code})\n");
                }

                //telegram_message($"[{condition_name}/편입] : {Code_name}({Code})\n");

                //
                if (condition_name != "HTS매매" && condition_name != "전일보유")
                {
                    //
                    Status = utility.buy_AND ? "호출" : "대기";

                    //최소 및 최대 매수가 확인
                    if (Native_Price < Convert.ToInt32(utility.min_price) || Native_Price > Convert.ToInt32(utility.max_price))
                    {
                        WriteLog_Stock($"[{condition_name}/편입실패] : {Code_name}({Code}) 가격 최소 및 최대 범위 이탈\n");
                        return;
                    }

                    //초기검색 항목 점검
                    if (!utility.buy_AND && !and_mode)
                    {
                        lock (buy_lock)
                        {
                            if (!buy_runningCodes.ContainsKey(Code))
                            {
                                buy_runningCodes[Code] = true;
                                Status = buy_check(Code, Code_name, string.Format("{0:#,##0}", Current_Price), time, high, false, condition_name, gubun);
                                buy_runningCodes.Remove(Code);
                            }
                        }
                    }
                    else if(utility.buy_AND && and_mode)
                    {
                        lock (buy_lock)
                        {
                            if (!buy_runningCodes.ContainsKey(Code) && !utility.buy_AND)
                            {
                                buy_runningCodes[Code] = true;
                                Status = buy_check(Code, Code_name, string.Format("{0:#,##0}", Current_Price), time, high, true, condition_name, gubun);
                                buy_runningCodes.Remove(Code);
                                //
                                findRows1[0]["상태"] = Status;
                                //
                                if (dataGridView1.InvokeRequired)
                                {
                                    dataGridView1.Invoke((MethodInvoker)delegate {
                                        bindingSource.ResetBindings(false);
                                    });
                                }
                                else
                                {
                                    bindingSource.ResetBindings(false);
                                }
                                //
                                return;
                            }
                        }
                    }
                }

                //HTS매매 혹은 전일보유 종목일 경우
                DataRow[] findRows = dtCondStock_hold.Select($"종목코드 = '{Code}'");

                //"매수중/" + real_gubun + "/" + order_number + "/" + order_acc_market;
                if (Status.StartsWith("매수중"))
                {
                    string[] tmp = Status.Split('/');
                    Status = tmp[0];
                    order_number = tmp[1];
                    now_hold = tmp[2];
                }

                //초기검색 항목 점검
                if(utility.buy_AND && !and_mode)
                {
                    Status = "호출";
                }

                //
                dtCondStock.Rows.Add(
                    false,
                    gubun,
                    "편입",
                    Status,
                    Code,
                    Code_name,
                    Current_Price,
                    string.Format("{0:#,##0}", Convert.ToString(MarketEye.GetDataValue(3, 0))), //거래량 => ulong
                    condition_name == "HTS매매" || condition_name == "전일보유" ? "실매입" : "진입가",
                    condition_name == "HTS매매" || condition_name == "전일보유" ? findRows[0]["평균단가"].ToString() : Current_Price,
                    "-",
                    "0.00%",
                    hold_num + "/" + now_hold,
                    condition_name,
                    time,
                    "-",
                    condition_name == "HTS매매" ? time : "-",
                    "-",
                    order_number,
                    string.Format("{0:#,##0}", Convert.ToInt32(high)), //상한가 => long or float)
                    Current_Price, //당일 최고 TS
                    "-"
                );

                /*
                //OR 및 AND 모드에서는 중복제거 => 초기 종목 검색시 중복 제거 필수
                if (!utility.buy_INDEPENDENT || !utility.buy_DUAL)
                {
                    RemoveDuplicateRows(dtCondStock, utility.buy_AND);
                }
                */

                if (dataGridView1.InvokeRequired)
                {
                    dataGridView1.Invoke((MethodInvoker)delegate {
                        bindingSource.ResetBindings(false);
                    });
                }
                else
                {
                    bindingSource.ResetBindings(false);
                }

                //실시간 시세 등록
                StockCur.SetInputValue(0, Code);
                StockCur.Subscribe();
            }
            else
            {
                WriteLog_Order($"[종목정보/수신실패] : {error_message(result)} / {MarketEye.GetDibMsg1()}\n");
            }
        }

        //중복제거
        public void RemoveDuplicateRows(DataTable dtCondStock, bool utilityBuyAnd)
        {
            //시간
            string time1 = DateTime.Now.ToString("HH:mm:ss");

            // 열 인덱스 가져오기
            int columnIndex = dtCondStock.Columns["종목명"].Ordinal;
            int statusColumnIndex = dtCondStock.Columns["상태"].Ordinal;
            int codeColumnIndex = dtCondStock.Columns["종목코드"].Ordinal;
            int currentPriceColumnIndex = dtCondStock.Columns["현재가"].Ordinal;
            int highPriceColumnIndex = dtCondStock.Columns["상한가"].Ordinal;
            int conditionColumnIndex = dtCondStock.Columns["조건식"].Ordinal;

            // 중복 행 제거를 위한 HashSet 생성
            HashSet<string> uniqueValues = new HashSet<string>();

            // 제거할 행의 인덱스 리스트
            List<int> rowsToRemove = new List<int>();

            // 행을 역순으로 순회하면서 중복 행 확인
            for (int i = dtCondStock.Rows.Count - 1; i >= 0; i--)
            {
                string currentValue = dtCondStock.Rows[i][columnIndex].ToString();

                // 현재 값이 HashSet에 없으면 추가
                if (!uniqueValues.Contains(currentValue))
                {
                    uniqueValues.Add(currentValue);
                }
                // 현재 값이 이미 있으면 제거할 행 리스트에 추가
                else
                {
                    rowsToRemove.Add(i);

                    // utility.buy_AND가 True 상태이면 buy_check 함수 실행
                    if (utilityBuyAnd)
                    {
                        lock (buy_lock)
                        {
                            string code = dtCondStock.Rows[i][codeColumnIndex].ToString();
                            string code_name = currentValue;
                            string current_price = string.Format("{0:#,##0}", dtCondStock.Rows[i][currentPriceColumnIndex]);
                            string high1 = dtCondStock.Rows[i][highPriceColumnIndex].ToString();
                            string condition = dtCondStock.Rows[i][conditionColumnIndex].ToString();

                            if (!buy_runningCodes.ContainsKey(code))
                            {
                                buy_runningCodes[code] = true;
                                buy_check(code, code_name, current_price, time1, high1, true, condition, "01");
                                buy_runningCodes.Remove(code);
                            }
                        }
                    }
                }
            }

            // 제거할 행 목록에 따라 역순으로 행 제거
            foreach (int rowIndex in rowsToRemove)
            {
                dtCondStock.Rows.RemoveAt(rowIndex);
            }
        }

        //실시간 시세 등록(현재가. 등락율, 거래량)
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=288&seq=16&page=6&searchString=&p=&v=&m=
        private void Stock_real_price()
        {
            string Stock_code = StockCur.GetHeaderValue(0);//종목코드 => string
            string price = Convert.ToString(StockCur.GetHeaderValue(13)); //새로운 현재가
            string amount = Convert.ToString(StockCur.GetHeaderValue(9)); //새로운 거래량
            double native_percent = 0;
            string percent = "";

            //종목 확인
            DataRow[] findRows = dtCondStock.Select($"종목코드 = '{Stock_code}'");

            if (findRows.Length != 0)
            {
                for (int i = 0; i < findRows.Length; i++)
                {
                    //신규 값 계산
                    if (!price.Equals(""))
                    {
                        double native_price = Convert.ToDouble(price);
                        native_percent = (native_price - Convert.ToDouble(findRows[i]["편입가"].ToString().Replace(",", ""))) / Convert.ToDouble(findRows[i]["편입가"].ToString().Replace(",", "")) * 100;
                        percent = string.Format("{0:#,##0.00}%", Convert.ToDecimal(native_percent)); //새로운 수익률
                    }

                    //신규 값 빈값 확인
                    if (!price.Equals(""))
                    {
                        findRows[i]["현재가"] = string.Format("{0:#,##0}", Convert.ToInt32(price)); //새로운 현재가
                        //
                        if (Convert.ToString(findRows[i]["상태"]) == "TS매수완료" && Convert.ToInt32(findRows[i]["편입최고"]) < Convert.ToInt32(price))
                        {
                            if(native_percent >= double.Parse(utility.profit_ts_text))
                            {
                                findRows[i]["상태"] = "매수완료";
                            }
                            findRows[i]["편입최고"] = string.Format("{0:#,##0}", Convert.ToInt32(price)); //새로운 현재가
                        }
                    }
                    if (!amount.Equals(""))
                    {
                        findRows[i]["거래량"] = string.Format("{0:#,##0}", Convert.ToInt32(amount)); //새로운 거래량
                    }
                    if (!percent.Equals(""))
                    {
                        findRows[i]["수익률"] = percent;
                    }

                    //매도 확인
                    if (findRows[i]["상태"].Equals("매수완료") && !percent.Equals(""))
                    {
                        lock (sell_lock)
                        {
                            string order_num = findRows[i]["주문번호"].ToString();
                            if (!sell_runningCodes.ContainsKey(order_num))
                            {
                                sell_runningCodes[order_num] = true;
                                if (utility.profit_ts)
                                {
                                    if (Convert.ToInt32(findRows[i]["편입최고"]) > Convert.ToInt32(price))
                                    {
                                        double down_percent_real = (Convert.ToDouble(price) - Convert.ToDouble(findRows[i]["편입최고"].ToString().Replace(",", ""))) / Convert.ToDouble(findRows[i]["편입최고"].ToString().Replace(",", "")) * 100;
                                        sell_check_price(price.Equals("") ? findRows[i]["현재가"].ToString() : string.Format("{0:#,##0}", Convert.ToInt32(price)), percent, Convert.ToInt32(findRows[i]["보유수량"].ToString().Split('/')[0]), Convert.ToInt32(findRows[i]["편입가"].ToString().Replace(",", "")), order_num, findRows[i]["구분코드"].ToString(), down_percent_real);
                                    }
                                }
                                else
                                {
                                    sell_check_price(price.Equals("") ? findRows[i]["현재가"].ToString() : string.Format("{0:#,##0}", Convert.ToInt32(price)), percent, Convert.ToInt32(findRows[i]["보유수량"].ToString().Split('/')[0]), Convert.ToInt32(findRows[i]["편입가"].ToString().Replace(",", "")), order_num, findRows[i]["구분코드"].ToString());
                                }
                                sell_runningCodes.Remove(order_num);
                            }
                        }
                    }
                }

                //적용
                if (dataGridView1.InvokeRequired)
                {
                    dataGridView1.Invoke((MethodInvoker)delegate {
                        bindingSource.ResetBindings(false);
                    });
                }
                else
                {
                    bindingSource.ResetBindings(false);
                }
            }


            DataRow[] findRows2 = dtCondStock_hold.Select($"종목코드 = '{Stock_code}'");

            if (findRows2.Length != 0)
            {
                //Dual 모드라면 구분코드로 인해 동일 종목에 대하여 2개 들어올 수 있음(수정 요망)
                for (int i = 0; i < findRows2.Length; i++)
                {
                    if (!price.Equals(""))
                    {
                        findRows2[i]["현재가"] = string.Format("{0:#,##0}", Convert.ToInt32(price)); //새로운 현재가
                        findRows2[i]["평가금액"] = string.Format("{0:#,##0}", Convert.ToInt32(price) * Convert.ToInt32(findRows2[i]["보유수량"].ToString().Replace(",", "")));
                    }
                    if (!percent.Equals(""))
                    {
                        findRows2[i]["수익률"] = percent;
                        findRows2[i]["손익금액"] = string.Format("{0:#,##0}", Convert.ToInt32(Convert.ToInt32(findRows2[i]["평가금액"].ToString().Replace(",", "")) * Convert.ToDouble(percent.Replace("%", "")) / 100));
                    }
                }

                //적용
                /*
                dtCondStock_hold.AcceptChanges();
                dataGridView2.DataSource = dtCondStock_hold;
                */
                if (dataGridView2.InvokeRequired)
                {
                    dataGridView2.Invoke((MethodInvoker)delegate {
                        dataGridView2.DataSource = dtCondStock_hold;
                        dataGridView2.Refresh();
                    });
                }
                else
                {
                    dataGridView2.DataSource = dtCondStock_hold;
                    dataGridView2.Refresh();
                }
            }
        }

        //-----------------------종목 편출입------------------------------

        //실시간 종목 편입 이탈
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=288&seq=241&page=1&searchString=CssAlert&p=&v=&m=      
        private void stock_in_out()
        {
            if(conditionInfo.Count() == 0)
            {
                WriteLog_System("조건식 로딩후 재실행\n");
                telegram_message("조건식 로딩후 재실행\n");
                real_time_stop_btn(this, EventArgs.Empty);
                return;
            }
            string Condition_ID = CssAlert.GetHeaderValue(0); // 전략ID
            var condInfo = conditionInfo.Find(f => f.Index == Condition_ID);
            string Condition_Name = condInfo.Name;
            string Stock_Code = CssAlert.GetHeaderValue(2); // 종목코드
            string gubun = CssAlert.GetHeaderValue(3).ToString();
            //
            //계좌 구분 코드
            string gubun_acc_fresh = Master_code;
            if (utility.buy_DUAL && Condition_Name.Equals(ISA_Condition))
            {
                gubun_acc_fresh = ISA_code;
            }
            //
            switch (gubun)
            {
                //편출입구분
                case "49" :
                    DataRow[] findRows1 = dtCondStock.Select($"종목코드 = '{Stock_Code}'");
                    string time1 = DateTime.Now.ToString("HH:mm:ss");

                    //매도 조건식일 경우
                    if (utility.sell_condition && utility.Fomula_list_sell_text.Split('^')[1] == Condition_ID)
                    {
                        if (findRows1.Any())
                        {
                            for (int i = 0; i < findRows1.Length; i++)
                            {
                                if (findRows1[i]["상태"].Equals("매수완료"))
                                {
                                    lock (sell_lock)
                                    {
                                        if (!sell_runningCodes.ContainsKey(findRows1[i]["주문번호"].ToString()))
                                        {
                                            sell_runningCodes[findRows1[i]["주문번호"].ToString()] = true;
                                            sell_check_condition(Stock_Code, findRows1[i]["현재가"].ToString(), findRows1[i]["수익률"].ToString(), time1, findRows1[i]["주문번호"].ToString(), findRows1[i]["구분코드"].ToString());
                                            sell_runningCodes.Remove(findRows1[i]["주문번호"].ToString());
                                        }
                                    }
                                }
                            }
                        }
 
                        return;
                    }

                    //신규종목
                    if(!findRows1.Any())
                    {
                        if (dtCondStock.Rows.Count > 100)
                        {
                            WriteLog_Stock($"[신규편입불가/{Condition_Name}/{Stock_Code}] : 최대 감시 종목(100개) 초과 \n");
                            return;
                        }
                        //
                        if(!waiting_Codes.Contains(Tuple.Create(Stock_Code, Condition_Name)))
                        {
                            waiting_Codes.Add(Tuple.Create(Stock_Code, Condition_Name));
                            Stock_info(Condition_Name, Stock_Code, "0", Stock_Code, gubun_acc_fresh);
                            waiting_Codes.Remove(Tuple.Create(Stock_Code, Condition_Name));
                        }
                        //
                        System.Threading.Thread.Sleep(250);
                    }
                    //기존에 포함됬던 종목
                    else if (utility.buy_INDEPENDENT || utility.buy_DUAL)
                    {
                        bool isentry = false;
                        bool issingle = false;
                        //
                        if(findRows1.Length == 2)
                        {
                            for (int i = 0; i < findRows1.Length; i++)
                            {
                                if (Condition_Name.Equals(findRows1[i]["조건식"]) && findRows1[i]["편입"].Equals("이탈") && findRows1[i]["상태"].Equals("대기"))
                                {
                                    findRows1[i]["편입"] = "편입";
                                    findRows1[i]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
                                    isentry = true;
                                }
                            }

                            //
                            if (dataGridView1.InvokeRequired)
                            {
                                dataGridView1.Invoke((MethodInvoker)delegate {
                                    bindingSource.ResetBindings(false);
                                });
                            }
                            else
                            {
                                bindingSource.ResetBindings(false);
                            }
                        }
                        else if(findRows1.Length == 1)
                        {
                            if (Condition_Name.Equals(findRows1[0]["조건식"]))
                            {
                                if(findRows1[0]["편입"].Equals("이탈"))
                                {
                                    findRows1[0]["편입"] = "편입";
                                    findRows1[0]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
                                    //
                                    if (dataGridView1.InvokeRequired)
                                    {
                                        dataGridView1.Invoke((MethodInvoker)delegate {
                                            bindingSource.ResetBindings(false);
                                        });
                                    }
                                    else
                                    {
                                        bindingSource.ResetBindings(false);
                                    }
                                    //
                                    isentry = true;
                                }
                            }
                            else
                            {
                                issingle = true;
                            }
                        }

                        if (isentry)
                        {
                            WriteLog_Stock($"[기존종목/INDEPENDENT편입/{Condition_Name}] : {findRows1[0]["종목명"]}({Stock_Code})\n");

                            //정렬
                            dtCondStock = dtCondStock.AsEnumerable().OrderBy(row => row.Field<string>("편입시각")).CopyToDataTable();
                            bindingSource.DataSource = dtCondStock;

                            //
                            return;
                        }
                        
                        if(issingle)
                        {
                            if (dtCondStock.Rows.Count > 100)
                            {
                                WriteLog_Stock($"[신규편입불가/{Condition_Name}/{Stock_Code}] : 최대 감시 종목(100개) 초과 \n");
                                return;
                            }
                            //
                            if (!waiting_Codes.Contains(Tuple.Create(Stock_Code, Condition_Name)))
                            {
                                waiting_Codes.Add(Tuple.Create(Stock_Code, Condition_Name));
                                Stock_info(Condition_Name, Stock_Code, "0", Stock_Code, gubun_acc_fresh);
                                waiting_Codes.Remove(Tuple.Create(Stock_Code, Condition_Name));
                            }

                            System.Threading.Thread.Sleep(250);
                        }
                    }
                    else 
                    {
                        //OR과 경우 종목당 한번만 포함된다.
                        if (utility.buy_OR && findRows1[0]["편입"].Equals("이탈") && findRows1[0]["상태"].Equals("대기"))
                        {
                            findRows1[0]["편입"] = "편입";
                            findRows1[0]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
                            WriteLog_Stock($"[기존종목/재편입/{Condition_Name}] : {findRows1[0]["종목명"]}({Stock_Code})\n");

                            //정렬
                            dtCondStock = dtCondStock.AsEnumerable().OrderBy(row => row.Field<string>("편입시각")).CopyToDataTable();
                            bindingSource.DataSource = dtCondStock;

                            //
                            if (dataGridView1.InvokeRequired)
                            {
                                dataGridView1.Invoke((MethodInvoker)delegate {
                                    bindingSource.ResetBindings(false);
                                });
                            }
                            else
                            {
                                bindingSource.ResetBindings(false);
                            }

                            //
                            return;
                        }

                        //AND의 경우 종목당 한번만 포함된다.
                        if (utility.buy_AND && findRows1[0]["편입"].Equals("이탈") && findRows1[0]["상태"].Equals("호출"))
                        {
                            findRows1[0]["편입"] = "편입";
                            findRows1[0]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
                            findRows1[0]["조건식"] = Condition_Name;

                            WriteLog_Stock($"[기존종목/AND재편입/{Condition_Name}] : {findRows1[0]["종목명"]}({Stock_Code})\n");

                            //정렬
                            dtCondStock = dtCondStock.AsEnumerable().OrderBy(row => row.Field<string>("편입시각")).CopyToDataTable();
                            bindingSource.DataSource = dtCondStock;

                            //
                            if (dataGridView1.InvokeRequired)
                            {
                                dataGridView1.Invoke((MethodInvoker)delegate {
                                    bindingSource.ResetBindings(false);
                                });
                            }
                            else
                            {
                                bindingSource.ResetBindings(false);
                            }

                            //
                            return;
                        }

                        //AND의 경우 포함된 종목이 한번 더 발견되어야 매수를 시작할 수 있다.
                        if (utility.buy_AND && findRows1[0]["편입"].Equals("편입") && findRows1[0]["상태"].Equals("호출"))
                        {
                            //
                            lock (buy_lock)
                            {
                                string code = findRows1[0]["종목코드"].ToString();
                                string code_name = findRows1[0]["종목명"].ToString();
                                string current_price = findRows1[0]["현재가"].ToString();
                                string high1 = findRows1[0]["상한가"].ToString();
                                string gubun_acc = findRows1[0]["구분코드"].ToString();

                                findRows1[0]["상태"] = "대기";

                                //정렬
                                dtCondStock = dtCondStock.AsEnumerable().OrderBy(row => row.Field<string>("편입시각")).CopyToDataTable();
                                bindingSource.DataSource = dtCondStock;

                                //
                                if (dataGridView1.InvokeRequired)
                                {
                                    dataGridView1.Invoke((MethodInvoker)delegate {
                                        bindingSource.ResetBindings(false);
                                    });
                                }
                                else
                                {
                                    bindingSource.ResetBindings(false);
                                }

                                WriteLog_Stock($"[기존종목/AND완전편입/{Condition_Name}] : {findRows1[0]["종목명"]}({Stock_Code})\n");

                                //호출에서 주문으로 변경되었으므로 매수 가능
                                if (!buy_runningCodes.ContainsKey(code))
                                {
                                    buy_runningCodes[code] = true;
                                    buy_check(code, code_name, current_price, time1, high1, true, Condition_Name, gubun_acc);
                                    buy_runningCodes.Remove(code);
                                }

                                return;
                            }
                        }  
                    }

                    WriteLog_Stock($"[기존종목/편입/{Condition_Name}] : {findRows1[0]["종목명"]}({Stock_Code}) 재편입 대상 없음\n");

                    break;

                //종목 이탈
                case "50":
                    //검출된 종목이 이미 이탈했다면(기본적으로 I D가 번갈아가면서 발생하므로 그럴릴 없음? 있는듯?)
                    DataRow[] findRows = dtCondStock.Select($"종목코드 = '{Stock_Code}'");

                    if (findRows.Length == 0)
                    {
                        WriteLog_Stock($"[기존종목/이탈/{Condition_Name}] : {Stock_Code} 이탈 대상 없음\n");
                        return;
                    }

                    //매도 조건식일 경우
                    if (utility.sell_condition &&  utility.Fomula_list_sell_text.Split('^')[1] == Condition_ID) return;


                    if (utility.buy_OR && findRows[0]["편입"].Equals("편입") && findRows[0]["상태"].Equals("대기"))
                    {
                        findRows[0]["편입"] = "이탈";
                        findRows[0]["이탈시각"] = DateTime.Now.ToString("HH:mm:ss");
                        WriteLog_Stock($"[기존종목/OR이탈/{Condition_Name}] : {findRows[0]["종목명"]}({Stock_Code})\n");
                        //
                        if (findRows[0]["상태"].Equals("매도완료") & findRows.Length == 1)
                        {
                            StockCur.SetInputValue(0, Stock_code);
                            StockCur.Unsubscribe();
                        }
                        //
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }
                    }
                    else if (utility.buy_AND)
                    {
                        if (findRows[0]["편입"].Equals("편입") &&  findRows[0]["상태"].Equals("호출"))
                        {
                            findRows[0]["편입"] = "이탈";
                            findRows[0]["이탈시각"] = DateTime.Now.ToString("HH:mm:ss");
                            WriteLog_Stock($"[기존종목/AND이탈/{Condition_Name}] : {findRows[0]["종목명"]}({Stock_Code}) 완전이탈 \n");
                            //
                            if (dataGridView1.InvokeRequired)
                            {
                                dataGridView1.Invoke((MethodInvoker)delegate
                                {
                                    bindingSource.ResetBindings(false);
                                });
                            }
                            else
                            {
                                bindingSource.ResetBindings(false);
                            }
                        }
                        else if (findRows[0]["편입"].Equals("편입") && findRows[0]["상태"].Equals("대기"))
                        {
                            findRows[0]["상태"] = "호출";
                            WriteLog_Stock($"[기존종목/AND이탈/{Condition_Name}] : {findRows[0]["종목명"]}({Stock_Code}) 부분이탈\n");
                            //
                            if (dataGridView1.InvokeRequired)
                            {
                                dataGridView1.Invoke((MethodInvoker)delegate
                                {
                                    bindingSource.ResetBindings(false);
                                });
                            }
                            else
                            {
                                bindingSource.ResetBindings(false);
                            }
                        }
                    }
                    else if (utility.buy_INDEPENDENT || utility.buy_DUAL)
                    {
                        for (int i = 0; i < findRows.Length; i++)
                        {
                            if (Condition_Name.Equals(findRows[i]["조건식"]) && findRows[i]["편입"].Equals("편입") && findRows[i]["상태"].Equals("대기"))
                            {
                                findRows[i]["편입"] = "이탈";
                                findRows[i]["이탈시각"] = DateTime.Now.ToString("HH:mm:ss");
                                WriteLog_Stock($"[기존종목/INDEPENDENT이탈/{Condition_Name}] : {findRows[i]["종목명"]}({Stock_Code})\n");
                                //
                                if (findRows[i]["상태"].Equals("매도완료") & findRows.Length == 1)
                                {
                                    StockCur.SetInputValue(0, Stock_code);
                                    StockCur.Unsubscribe();
                                }
                                //
                                if (dataGridView1.InvokeRequired)
                                {
                                    dataGridView1.Invoke((MethodInvoker)delegate {
                                        bindingSource.ResetBindings(false);
                                    });
                                }
                                else
                                {
                                    bindingSource.ResetBindings(false);
                                }

                                break;
                            }
                        }
                    }

                    break;
            }
        }

        //--------------편입 이후 종목에 대한 매수 매도 감시(500ms)---------------------

        //timer2(200ms) : 편입된 종목에 대하여 매수 및 청산 확인
        private void Transfer_Timer(object sender, EventArgs e)
        {
            //편입 상태 이면서 대기 종목인 녀석에 대한 검증
            account_check_buy();

            order_cancel_check();

            //지수연동청산
            if (index_clear)
            {
                account_check_sell();
            }

            //매도 완료 종목에 대한 청산 검증
            if (utility.clear_sell || utility.clear_sell_mode)
            {
                //청산 매도 시간 확인
                TimeSpan t_code = TimeSpan.Parse(DateTime.Now.ToString("HH:mm:ss"));
                TimeSpan t_start = TimeSpan.Parse(utility.clear_sell_start);
                TimeSpan t_end = TimeSpan.Parse(utility.clear_sell_end);

                if (t_code.CompareTo(t_start) < 0 || t_code.CompareTo(t_end) > 0) return;

                account_check_sell();
            }
        }

        //이전 매수 종목 매수 확인
        private void account_check_buy()
        {
            string time = DateTime.Now.ToString("HH:mm:ss");

            //특저 열 추출
            DataColumn columnEditColumn = dtCondStock.Columns["편입"];
            DataColumn columnStateColumn = dtCondStock.Columns["상태"];
            //AsEnumerable()은 DataTable의 행을 열거형으로 변환
            var filteredRows = dtCondStock.AsEnumerable()
                                        .Where(row => row.Field<string>(columnEditColumn) == "편입" &&
                                                      row.Field<string>(columnStateColumn) == "대기" || row.Field<string>(columnStateColumn) == "주문")
                                        .ToList();

            //검출 종목에 대한 확인
            if (filteredRows.Count > 0)
            {
                foreach (DataRow row in filteredRows)
                {
                    //자동 시간전 검출 매수 확인
                    TimeSpan t_code = TimeSpan.Parse(row.Field<string>("편입시각"));
                    TimeSpan t_start = TimeSpan.Parse(utility.buy_condition_start);
                    if (utility.before_time_deny)
                    {
                        if (t_code.CompareTo(t_start) < 0) continue;
                        // result가 0보다 작으면 time1 < time2
                        // result가 0이면 time1 = time2
                        // result가 0보다 크면 time1 > time2
                    }

                    //중복 
                    lock (buy_lock)
                    {
                        string code = row.Field<string>("종목코드");
                        if (!buy_runningCodes.ContainsKey(code))
                        {
                            buy_runningCodes[code] = true;
                            buy_check(code, row.Field<string>("종목명"), row.Field<string>("현재가").Replace(",", ""), time, row.Field<string>("상한가"), true, row.Field<string>("조건식"), row.Field<string>("구분코드"));
                            buy_runningCodes.Remove(code);
                        }
                    }
                }
            }
        }

        //자동 취소 확인
        private void order_cancel_check()
        {
            if (utility.term_for_non_buy)
            {
                DataRow[] findRows = dtCondStock.AsEnumerable().Where(row => row.Field<string>("상태") == "매수중").ToArray();

                if (findRows.Any())
                {
                    TimeSpan t_now = TimeSpan.Parse(DateTime.Now.ToString("HH:mm:ss"));
                    //
                    for(int i = 0; i < findRows.Length; i++)
                    {
                        TimeSpan t_last = TimeSpan.Parse(findRows[i]["매매진입"].ToString());
                        //
                        if (t_now - t_last >= TimeSpan.FromMilliseconds(Convert.ToInt32(utility.term_for_non_buy_text)))
                        {
                            //string trade_type, string order_number, string gubun, string code_name, string code, string order_acc
                            order_close("매수", findRows[i]["주문번호"].ToString(), findRows[i]["구분코드"].ToString(), findRows[i]["종목명"].ToString(), findRows[i]["종목코드"].ToString(), findRows[i]["보유수량"].ToString().Split('/')[1]);
                        }

                        System.Threading.Thread.Sleep(750);
                    }
                }
            }

            if (utility.term_for_non_buy)
            {
                DataRow[] findRows = dtCondStock.AsEnumerable().Where(row => row.Field<string>("상태") == "매도중").ToArray();

                if (findRows.Any())
                {
                    TimeSpan t_now = TimeSpan.Parse(DateTime.Now.ToString("HH:mm:ss"));
                    //
                    for (int i = 0; i < findRows.Length; i++)
                    {
                        TimeSpan t_last = TimeSpan.Parse(findRows[i]["매매진입"].ToString());
                        //
                        if (t_now - t_last >= TimeSpan.FromMilliseconds(Convert.ToInt32(utility.term_for_non_buy_text)))
                        {
                            order_close("매도", findRows[i]["주문번호"].ToString(), findRows[i]["구분코드"].ToString(), findRows[i]["종목명"].ToString(), findRows[i]["종목코드"].ToString(), findRows[i]["보유수량"].ToString().Split('/')[1]);
                        }

                        System.Threading.Thread.Sleep(750);
                    }
                }
            }
        }

        //청산 확인
        private void account_check_sell()
        {
            if (utility.clear_sell)
            {
                //특저 열 추출
                DataColumn columnStateColumn = dtCondStock.Columns["상태"];

                //AsEnumerable()은 DataTable의 행을 열거형으로 변환
                var filteredRows = dtCondStock.AsEnumerable().Where(row => row.Field<string>(columnStateColumn) == "매수완료" || row.Field<string>(columnStateColumn) == "TS매수완료").ToList();

                //검출 종목에 대한 확인
                if (filteredRows.Count > 0)
                {
                    foreach (DataRow row in filteredRows)
                    {
                        lock (sell_lock)
                        {
                            string order_num = row.Field<string>("주문번호"); ;
                            if (!sell_runningCodes.ContainsKey(order_num))
                            {
                                sell_runningCodes[order_num] = true;
                                //
                                sell_order("Nan", "청산매도/일반", order_num, row.Field<string>("수익률"), row.Field<string>("구분코드"));
                                //
                                sell_runningCodes.Remove(order_num);
                            }
                        }
                    }
                }
            }
            else if(utility.clear_sell_mode)
            {
                if (!utility.clear_sell_profit && !utility.clear_sell_loss)
                {
                    WriteLog_System("청산 모드 선택 요청\n");
                    telegram_message("청산 모드 선택 요청\n");
                    return;
                }

                //특저 열 추출
                DataColumn columnStateColumn = dtCondStock.Columns["상태"];
                
                //AsEnumerable()은 DataTable의 행을 열거형으로 변환
                var filteredRows = dtCondStock.AsEnumerable().Where(row => row.Field<string>(columnStateColumn) == "매수완료" || row.Field<string>(columnStateColumn) == "TS매수완료").ToList();
                
                //검출 종목에 대한 확인
                if (filteredRows.Count > 0)
                {
                    foreach (DataRow row in filteredRows)
                    {
                        lock (sell_lock)
                        {
                            string order_num = row.Field<string>("주문번호"); ;
                            if (!sell_runningCodes.ContainsKey(order_num))
                            {
                                sell_runningCodes[order_num] = true;
                                //
                                double percent_edit = double.Parse(row.Field<string>("수익률").Replace("%", ""));
                                double profit = double.Parse(utility.clear_sell_profit_text);
                                double loss = double.Parse(utility.clear_sell_loss_text);
                                if (utility.clear_sell_profit && percent_edit >= profit)
                                {
                                    sell_order("Nan", "청산매도/수익", order_num, row.Field<string>("수익률"), row.Field<string>("구분코드"));
                                }
                                //
                                if (utility.clear_sell_loss && percent_edit <= -loss)
                                {
                                    sell_order("Nan", "청산매도/손실", order_num, row.Field<string>("수익률"), row.Field<string>("구분코드"));
                                }
                                //
                                sell_runningCodes.Remove(order_num);
                            }
                        }
                    }
                }
            }
        }

        //--------------실시간 매수 조건 확인 및 매수 주문---------------------

        private string last_buy_time = "08:59:59";

        //매수 가능한 상태인지 확인
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=291&seq=159&page=3&searchString=&p=&v=&m=
        private string buy_check(string code, string code_name, string price, string time, string high, bool check, string condition_name, string gubun)
        {
            //지수 확인
            if (index_buy && gubun == Master_code)
            {
                return "대기";
            }

            //지수 확인
            if (index_dual && gubun == ISA_code)
            {
                return "대기";
            }

            //매수 시간 확인
            if (utility.buy_DUAL && utility.Dual_Time && gubun == ISA_code)
            {
                TimeSpan t_code = TimeSpan.Parse(time);
                TimeSpan t_start = TimeSpan.Parse(utility.Dual_Time_Start);
                TimeSpan t_end = TimeSpan.Parse(utility.Dual_Time_Stop);

                if (t_code.CompareTo(t_start) < 0 || t_code.CompareTo(t_end) > 0)
                {
                    // result가 0보다 작으면 time1 < time2
                    // result가 0이면 time1 = time2
                    // result가 0보다 크면 time1 > time2
                    return "대기";
                }
            }
            else
            {
                TimeSpan t_code = TimeSpan.Parse(time);
                TimeSpan t_start = TimeSpan.Parse(utility.buy_condition_start);
                TimeSpan t_end = TimeSpan.Parse(utility.buy_condition_end);

                if (t_code.CompareTo(t_start) < 0 || t_code.CompareTo(t_end) > 0)
                {
                    // result가 0보다 작으면 time1 < time2
                    // result가 0이면 time1 = time2
                    // result가 0보다 크면 time1 > time2
                    return "대기";
                }
            }

            //보유 종목 수 확인
            string[] hold_status = max_hoid.Text.Split('/');
            int hold = Convert.ToInt32(hold_status[0]);
            int hold_max = Convert.ToInt32(hold_status[1]);
            if (hold >= hold_max) return "대기";

            //매매 횟수 확인
            if (utility.buy_INDEPENDENT || utility.buy_DUAL)
            {
                string[] trade_status = maxbuy_acc.Text.Split('/');
                string[] condition_num = utility.Fomula_list_buy_text.Split(',');
                for (int i = 0; i < condition_num.Length; i++)
                {
                    if (condition_num[i].Split('^')[1].Equals(condition_name))
                    {
                        if (Convert.ToInt32(trade_status[i]) >= Convert.ToInt32(trade_status[trade_status.Length - 1]))
                        {
                            return "대기";
                        }
                        break;
                    }
                }
            }
            else
            {
                string[] trade_status = maxbuy_acc.Text.Split('/');
                int trade_status_already = Convert.ToInt32(trade_status[0]);
                int trade_status_limit = Convert.ToInt32(trade_status[1]);
                if (trade_status_already >= trade_status_limit) return "대기";
            }

            //보유 종목 매수 확인
            if (utility.hold_deny && gubun == Master_code)
            {
                var findRows2 = dtCondStock_hold.AsEnumerable()
                                                .Where(row2 => row2.Field<string>("종목코드") == code &&
                                                              row2.Field<string>("구분코드") == Master_code);
                if (findRows2.Any())
                {
                    return "대기";
                }
            }

            //보유 종목 매수 확인
            if (utility.hold_deny && gubun == ISA_code)
            {
                var findRows2 = dtCondStock_hold.AsEnumerable()
                                                .Where(row2 => row2.Field<string>("종목코드") == code &&
                                                              row2.Field<string>("구분코드") == ISA_code);
                if (findRows2.Any())
                {
                    return "대기";
                }
            }

            //최소 주문간 간격 750ms
            if (utility.term_for_buy)
            {
                TimeSpan t_now = TimeSpan.Parse(time);
                TimeSpan t_last = TimeSpan.Parse(last_buy_time);

                if (t_now - t_last < TimeSpan.FromMilliseconds(Convert.ToInt32(utility.term_for_buy_text)))
                {
                    //WriteLog_Order($"[매수간격] 설정({utility.term_for_buy_text}), 현재({(t_now - t_last2).ToString()})\n");
                    return "대기";
                }
                last_buy_time = t_now.ToString();
            }
            else
            {
                TimeSpan t_now = TimeSpan.Parse(time);
                TimeSpan t_last = TimeSpan.Parse(last_buy_time);

                if (t_now - t_last < TimeSpan.FromMilliseconds(750))
                {
                    //WriteLog_Order($"[매수간격] 설정({utility.term_for_buy_text}), 현재({(t_now - t_last2).ToString()})\n");
                    return "대기";
                }
                last_buy_time = t_now.ToString();
            }

            //매수 주문(1초에 5회)
            //주문 방식 구분
            string[] order_method = buy_condtion_method.Text.Split('/');

            //시장가 주문
            if (order_method[0].Equals("시장가"))
            {

                //시장가에 대하여 주문 가능 개수 계산 => 기억해야 함 / 종목당매수금액 / 종목당매수수량 / 종목당매수비율 / 종목당최대매수금액
                //User_money.Text;
                int order_acc_market = buy_order_cal(Convert.ToInt32(high.Replace(",", "")), gubun);

                if(order_acc_market == 0)
                {
                    WriteLog_Order($"[매수주문/시장가/주문실패/{gubun}] : " + code_name + "(" + code + ") " + "예수금 부족 0개 주문\n");
                    telegram_message($"[매수주문/시장가/주문실패/{gubun}] : " + code_name + "(" + code + ") " + "예수금 부족 0개 주문\n");

                    if (check)
                    {
                        DataRow[] findRows = dtCondStock.AsEnumerable().Where(row2 => row2.Field<string>("종목코드") == code && row2.Field<string>("조건식") == condition_name).ToArray();

                        findRows[0]["상태"] = "부족";

                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }

                    }

                    return "부족";
                }

                WriteLog_Order($"[매수주문/시장가/주문접수/{gubun}] : {code_name}({code}) {order_acc_market}개\n");
                telegram_message($"[매수주문/시장가/주문접수/{gubun}] : {code_name}({code}) {order_acc_market}개\n");
                //
                CpTd0311.SetInputValue(0, "2"); //매수
                CpTd0311.SetInputValue(1, acc_text.Text); //계좌번호
                CpTd0311.SetInputValue(2, gubun); //상품관리구분코드
                CpTd0311.SetInputValue(3, code); //종목코드
                CpTd0311.SetInputValue(4, order_acc_market); //주문수량
                CpTd0311.SetInputValue(5, 0); //주문단가
                CpTd0311.SetInputValue(7, "0"); //주문조건구분코드
                CpTd0311.SetInputValue(8, "03"); //시장가
                //
                int check2 = 0;
                //
                //수신확인
                while (true)
                {
                    if (CpTd0311.GetDibStatus() == 1)
                    {
                        check2++;
                        WriteLog_System("[DibRq요청/수신대기/5초] : 시장가 주문\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check2 == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check2 == 5)
                {
                    WriteLog_System("[시장가 주문/수신실패] : 재부팅 요망\n");
                    telegram_message("[시장가 주문/수신실패] : 재부팅 요망\n");
                    return "대기";
                }
                //
                int error = CpTd0311.BlockRequest();
                //
                if (error == 0)
                {
                    //
                    WriteLog_Order($"[매수주문/시장가/주문성공/{gubun}] : {code_name}({code}) {order_acc_market}개\n");
                    telegram_message($"[매수주문/시장가/주문성공/{gubun}] : {code_name}({code}) {order_acc_market}개\n");

                    //업데이트
                    string real_gubun = Convert.ToString(CpTd0311.GetHeaderValue(2)); // 구분 
                    string order_number = Convert.ToString(CpTd0311.GetHeaderValue(8)); //주문번호

                    //보유 수량 업데이트
                    string[] hold_status_update = max_hoid.Text.Split('/');
                    int hold_update = Convert.ToInt32(hold_status_update[0]);
                    int hold_max_update = Convert.ToInt32(hold_status_update[1]);
                    max_hoid.Text = (hold_update + 1) + "/" + hold_max_update;

                    string time2 = DateTime.Now.ToString("HH:mm:ss");

                    //기존 종목 포함 및 AND 모드 포함
                    if (check)
                    {
                        DataRow[] findRows = dtCondStock.AsEnumerable().Where(row2 => row2.Field<string>("종목코드") == code && row2.Field<string>("조건식") == condition_name).ToArray();

                        findRows[0]["상태"] = "매수중";
                        findRows[0]["주문번호"] = order_number;
                        findRows[0]["보유수량"] = 0 + "/" + order_acc_market;
                        findRows[0]["매매진입"] = time2;

                        // 데이터 변경 사항을 바인딩 소스에 알리기 (UI 스레드에서 수행)
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }
                    }

                    //매매 횟수업데이트
                    if (utility.buy_INDEPENDENT || utility.buy_DUAL)
                    {
                        string[] trade_status = maxbuy_acc.Text.Split('/');
                        string[] condition_num = utility.Fomula_list_buy_text.Split(',');
                        for (int i = 0; i < condition_num.Length; i++)
                        {
                            if (condition_num[i].Split('^')[1].Equals(condition_name))
                            {
                                trade_status[i] = Convert.ToString(Convert.ToInt32(trade_status[i]) + 1);
                                maxbuy_acc.Text = String.Join("/", trade_status);
                                break;
                            }
                        }
                    }
                    else
                    {
                        string[] trade_status_update = maxbuy_acc.Text.Split('/');
                        int trade_status_already_update = Convert.ToInt32(trade_status_update[0]);
                        int trade_status_limit_update = Convert.ToInt32(trade_status_update[1]);
                        maxbuy_acc.Text = trade_status_already_update + 1 + "/" + trade_status_limit_update;
                    }

                    return "매수중/" + order_number + "/" + order_acc_market + "/" + time2;

                }
                else
                {
                    WriteLog_Order($"[매수주문/시장가/주문실패/{gubun}] : {code_name}({code}) 에러코드({error_message(error)}) {CpTd0311.GetDibMsg1()}\n");
                    telegram_message($"[매수주문/시장가/주문실패/{gubun}] : " + code_name + "(" + code + ") " + "에러코드(" + error_message(error) + ")\n");

                    if (check)
                    {
                        DataRow[] findRows = dtCondStock.AsEnumerable().Where(row2 => row2.Field<string>("종목코드") == code && row2.Field<string>("조건식") == condition_name).ToArray();

                        findRows[0]["상태"] = "대기";

                        // 데이터 변경 사항을 바인딩 소스에 알리기 (UI 스레드에서 수행)
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }
                    }

                    return "대기";
                }
            }
            //지정가 주문
            else
            {
                //지정가 계산
                int edited_price_hoga = hoga_cal(Convert.ToInt32(price), order_method[1].Equals("현재가") ? 0 : Convert.ToInt32(order_method[1].Replace("호가", "")));

                //지정가에 대하여 주문 가능 개수 계산
                int order_acc = buy_order_cal(edited_price_hoga, gubun);

                if (order_acc == 0)
                {
                    WriteLog_Order($"[매수주문/지정가/주문실패/{gubun}] : " + code_name + "(" + code + ") " + "예수금 부족 0개 주문\n");
                    telegram_message($"[매수주문/지정가/주문실패/{gubun}] : " + code_name + "(" + code + ") " + "예수금 부족 0개 주문\n");
                    return "대기";
                }

                WriteLog_Order($"[매수주문/지정가매수/접수/{gubun}] : {code_name}({code}) {order_acc}개 {price}원\n");
                telegram_message($"[매수주문/지정가매수/접수/{gubun}] : {code_name}({code}) {order_acc}개 {price}원\n");

                CpTd0311.SetInputValue(0, "2"); //매수
                CpTd0311.SetInputValue(1, acc_text.Text); //계좌번호
                CpTd0311.SetInputValue(2, gubun); //상품관리구분코드
                CpTd0311.SetInputValue(3, code); //종목코드
                CpTd0311.SetInputValue(4, order_acc); //주문수량
                CpTd0311.SetInputValue(5, edited_price_hoga); //주문단가
                CpTd0311.SetInputValue(7, "0"); //주문조건구분코드
                CpTd0311.SetInputValue(8, "01"); //지장가
                //
                int check2 = 0;
                //
                //수신확인
                while (true)
                {
                    if (CpTd0311.GetDibStatus() == 1)
                    {
                        check2++;
                        WriteLog_System("[DibRq요청/수신대기/5초] : 지정가 주문\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check2 == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check2 == 5)
                {
                    WriteLog_System("[지정가 주문/수신실패] : 재부팅 요망\n");
                    telegram_message("[지정가 주문/수신실패] : 재부팅 요망\n");
                    return "대기";
                }
                //
                int error = CpTd0311.BlockRequest();
                //
                if (error == 0)
                {
                    //
                    WriteLog_Order($"[매수주문/지정가매수/접수성공/{gubun}] : {code_name}({code}) {order_acc}개 {price}원\n");
                    telegram_message($"[매수주문/지정가매수/접수성공/{gubun}] : {code_name}({code}) {order_acc}개 {price}원\n");

                    //업데이트
                    string real_gubun = Convert.ToString(CpTd0311.GetHeaderValue(2)); // 구분 
                    string order_number = Convert.ToString(CpTd0311.GetHeaderValue(8)); //주문번호

                    //보유 수량 업데이트
                    string[] hold_status_update = max_hoid.Text.Split('/');
                    int hold_update = Convert.ToInt32(hold_status_update[0]);
                    int hold_max_update = Convert.ToInt32(hold_status_update[1]);
                    max_hoid.Text = (hold_update + 1) + "/" + hold_max_update;

                    string time2 = DateTime.Now.ToString("HH:mm:ss");

                    //기존 종목 포함 및 AND 모드 포함
                    if (check)
                    {
                        DataRow[] findRows = dtCondStock.AsEnumerable().Where(row2 => row2.Field<string>("종목코드") == code && row2.Field<string>("조건식") == condition_name).ToArray();

                        findRows[0]["상태"] = "매수중";
                        findRows[0]["주문번호"] = order_number;
                        findRows[0]["보유수량"] = 0 + "/" + order_acc;
                        findRows[0]["매매진입"] = time2;

                        // 데이터 변경 사항을 바인딩 소스에 알리기 (UI 스레드에서 수행)
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }
                    }

                    //매매 횟수업데이트(Independent Mode)
                    if (utility.buy_INDEPENDENT || utility.buy_DUAL)
                    {
                        string[] trade_status = maxbuy_acc.Text.Split('/');
                        string[] condition_num = utility.Fomula_list_buy_text.Split(',');
                        for (int i = 0; i < condition_num.Length; i++)
                        {
                            if (condition_num[i].Split('^')[1].Equals(condition_name))
                            {
                                trade_status[i] = Convert.ToString(Convert.ToInt32(trade_status[i]) + 1);
                                maxbuy_acc.Text = String.Join("/", trade_status);
                                break;
                            }
                        }
                    }
                    else
                    {
                        string[] trade_status_update = maxbuy_acc.Text.Split('/');
                        int trade_status_already_update = Convert.ToInt32(trade_status_update[0]);
                        int trade_status_limit_update = Convert.ToInt32(trade_status_update[1]);
                        maxbuy_acc.Text = trade_status_already_update + 1 + "/" + trade_status_limit_update;

                    }

                    return "매수중/" + order_number + "/" + order_acc + "/" + time2;

                }
                else
                {
                    WriteLog_Order($"[매수주문/지정가매수/주문실패/{gubun}] : {code_name }({ code}) 에러코드({error_message(error)}) {CpTd0311.GetDibMsg1()}\n");
                    telegram_message($"[매수주문/지정가매수/주문실패/{gubun}] : {code_name }({ code}) 에러코드({error_message(error)}) {CpTd0311.GetDibMsg1()}\n");

                    if (check)
                    {
                        DataRow[] findRows = dtCondStock.AsEnumerable().Where(row2 => row2.Field<string>("종목코드") == code && row2.Field<string>("조건식") == condition_name).ToArray();

                        findRows[0]["상태"] = "대기";

                        // 데이터 변경 사항을 바인딩 소스에 알리기 (UI 스레드에서 수행)
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }
                    }

                    return "대기";
                }
            }
        }

        //매수 주문 수량 계산
        private int buy_order_cal(int price, string gubun)
        {
            int current_balance = 0;

            if (gubun == "01")
            {
                int current_balance_tmp = Convert.ToInt32(User_money.Text.Replace(",", ""));
                //
                if (Authentication_Check)
                {
                    if(current_balance_tmp > sample_balance)
                    {
                        current_balance = sample_balance;
                    }
                    else
                    {
                        current_balance = current_balance_tmp;
                    }
                }
                else
                {
                    current_balance = current_balance_tmp;
                }
            }
            else
            {
                int current_balance_tmp = Convert.ToInt32(User_money_isa.Text.Replace(",", ""));
                //
                if (Authentication_Check)
                {
                    if (current_balance_tmp > sample_balance)
                    {
                        current_balance = sample_balance;
                    }
                    else
                    {
                        current_balance = current_balance_tmp;
                    }
                }
                else
                {
                    current_balance = current_balance_tmp;
                }
            }

            int max_buy = Convert.ToInt32(utility.maxbuy);
            //
            if (utility.buy_per_percent)
            {
                //매수비율
                int ratio = Convert.ToInt32(utility.buy_per_percent_text);

                // 예수금 활용 비율 계산 (0.XX 형태로 변환)
                double buy_Percent = ratio / 100.0;

                // 주문 가능 금액 계산 (예수금 * 활용 비율)
                double order_Amount = current_balance * buy_Percent;

                // 상한가 기준 최대 주문 가능 수량 계산 (내림)
                int quantity = (int)Math.Floor(order_Amount / (double)price);

                // 실제 주문 금액 계산
                double actual_Order_Amount = quantity * price;

                //종목당 최대 매수 금액 비교
                if (actual_Order_Amount > (double)max_buy)
                {
                    quantity = (int)Math.Floor((double)max_buy / price);
                }
                // 실제 주문 금액이 주문 가능 금액을 초과하는 경우 수량 조정
                else if (actual_Order_Amount > (double)order_Amount)
                {
                    quantity--;
                }

                return quantity;
            }
            else if (utility.buy_per_amount)
            {
                //매수개수
                int max_amount = Convert.ToInt32(utility.buy_per_amount_text);

                // 상한가 기준 최대 주문 가능 금액 계산
                double max_Order_Amount = max_amount * price;

                // 예수금 - 최대 주문 가능 금액 - 종목당최대주문금액  중 작은 값으로 실제 주문 가능 금액 결정
                double order_Amount = Math.Min(Math.Min(current_balance, (int)max_Order_Amount), max_buy);

                // 실제 주문 가능 수량 계산 (내림)
                return (int)Math.Floor(order_Amount / price);
            }
            else
            {
                //매수금액
                int max_amount = Convert.ToInt32(utility.buy_per_price_text);

                // 예수금과 - 최대 주문 가능 금액 - 종목당최대주문금액 중 작은 값으로 실제 주문 가능 금액 결정
                double order_Amount = Math.Min(Math.Min(current_balance, max_amount), max_buy);

                // 실제 주문 가능 수량 계산 (내림)
                return (int)Math.Floor(order_Amount / price);
            }
        }

        //--------------실시간 매도 주문---------------------

        //조건식 매도
        private void sell_check_condition(string code, string price, string percent, string time, string order_num, string gubun)
        {
            TimeSpan t_code = TimeSpan.Parse(time);
            TimeSpan t_start = TimeSpan.Parse(utility.sell_condition_start);
            TimeSpan t_end = TimeSpan.Parse(utility.sell_condition_end);

            if (t_code.CompareTo(t_start) < 0 || t_code.CompareTo(t_end) > 0)
            {
                WriteLog_Order("[조건식매도/매도시간이탈] : " + code + " - " + "조건식 매도 시간이 아닙니다." + "\n");
                return;
            }

            sell_order(price, "조건식매도", order_num, percent, gubun);
        }

        //실시간 가격 매도
        private void sell_check_price(string price, string percent, int hold, int buy_price, string order_num, string gubun, double down_percent = 0)
        {
            //익절
            if (utility.profit_percent)
            {
                double percent_edit = double.Parse(percent.Replace("%", ""));
                double profit = double.Parse(utility.profit_percent_text);
                if (percent_edit >= profit)
                {
                    sell_order(price, "익절매도", order_num, percent, gubun);
                    return;
                }
            }

            //익절원
            if (utility.profit_won)
            {
                int profit_amount = Convert.ToInt32(utility.profit_won_text);
                if ((hold * buy_price * double.Parse(percent.Replace("%", "")) / 100) >= profit_amount)
                {
                    sell_order(price, "익절원", order_num, percent, gubun);
                    return;
                }
            }

            //익절TS
            if (utility.profit_ts)
            {
                if(Math.Abs(down_percent) >= double.Parse(utility.profit_ts_text2))
                {
                    sell_order(price, "익절TS", order_num, percent, gubun);
                    return;
                }
            }

            //손절
            if (utility.loss_percent)
            {
                double percent_edit = double.Parse(percent.TrimEnd('%'));
                double loss = double.Parse(utility.loss_percent_text);
                if (percent_edit <= -loss)
                {
                    sell_order(price, "손절매도", order_num, percent, gubun);
                    return;
                }
            }

            //손절원
            if (utility.loss_won)
            {
                int loss_amount = Convert.ToInt32(utility.loss_won_text);
                if ((hold * buy_price * double.Parse(percent.Replace("%", "")) / 100) <= -loss_amount)
                {
                    sell_order(price, "손절원", order_num, percent, gubun);
                    return;
                }
            }
        }

        //매도 주문(1초에 5회)
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=291&seq=159&page=3&searchString=&p=&v=&m=
        private void sell_order(string price, string sell_message, string order_num, string percent, string gubun)
        {
            //
            var findRows = dtCondStock.AsEnumerable().Where(row => row.Field<string>("주문번호") == order_num);

            if (findRows.Any())
            {
                DataRow row = findRows.First();

                string start_price = row["편입가"].ToString();
                string code = row["종목코드"].ToString();
                string code_name = row["종목명"].ToString();

                //보유수량계산
                string[] tmp = row["보유수량"].ToString().Split('/');
                int order_acc = Convert.ToInt32(tmp[0]);

                //주문 방식 구분
                string[] order_method = buy_condtion_method.Text.Split('/');

                //주문시간 확인(0정규장, 1시간외종가, 2시간외단일가
                int market_time = 0;

                TimeSpan t_now = TimeSpan.Parse(DateTime.Now.ToString("HH:mm:ss"));

                //주문간 간격
                if (utility.term_for_sell)
                {
                    TimeSpan t_last2 = TimeSpan.Parse(last_buy_time);

                    if (t_now - t_last2 < TimeSpan.FromMilliseconds(Convert.ToInt32(utility.term_for_sell_text)))
                    {
                        //WriteLog_Order($"[매도간격] 설정({utility.term_for_sell_text}), 현재({(t_now - t_last2).ToString()})\n");
                        return;
                    }
                    last_buy_time = t_now.ToString();
                }
                else
                {
                    TimeSpan t_last2 = TimeSpan.Parse(last_buy_time);

                    if (t_now - t_last2 < TimeSpan.FromMilliseconds(750))
                    {
                        //WriteLog_Order($"[매도간격] 설정({utility.term_for_sell_text}), 현재({(t_now - t_last2).ToString()})\n");
                        return;
                    }
                    last_buy_time = t_now.ToString();
                }

                TimeSpan t_time0 = TimeSpan.Parse("15:30:00");
                TimeSpan t_time1 = TimeSpan.Parse("15:40:00");
                TimeSpan t_time2 = TimeSpan.Parse("16:00:00");
                TimeSpan t_time3 = TimeSpan.Parse("18:00:00");

                // result가 0보다 작으면 time1 < time2
                // result가 0이면 time1 = time2
                // result가 0보다 크면 time1 > time2
                if (t_time0.CompareTo(t_now) <= 0 && t_now.CompareTo(t_time1) < 0)
                {
                    WriteLog_Order($"[{sell_message}/주문접수/{gubun}] : {code_name}({code}) {order_acc}개 {percent} 정규장 종료\n");
                    return;
                }
                else if (t_time1.CompareTo(t_now) <= 0 && t_now.CompareTo(t_time2) < 0)
                {
                    market_time = 1;
                }
                else if(t_time2.CompareTo(t_now) <= 0 && t_now.CompareTo(t_time3) < 0)
                {
                    market_time = 2;
                }
                else if(t_now.CompareTo(t_time3) >= 0)
                {
                    WriteLog_Order($"[{sell_message}/주문접수/{gubun}] : {code_name}({code}) {order_acc}개 {percent} 시간외단일가 종료\n");
                    return;
                }

                WriteLog_Order($"[{sell_message}/주문접수/{gubun}] : {code_name}({code}) {order_acc}개 {percent}\n");
                telegram_message($"[{sell_message}/주문접수/{gubun}] : {code_name}({code}) {order_acc}개 {percent}\n");

                string time2 = DateTime.Now.ToString("HH:mm:ss");

                //시간외종가
                //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=291&seq=177&page=1&searchString=&p=&v=&m=
                if (market_time == 1)
                {
                    if (sell_message.Equals("청산매도/일반") || sell_message.Equals("청산매도/수익") && !utility.clear_sell_profit_after1)
                    {
                        return;
                    }
                    else if(sell_message.Equals("청산매도/손실") && !utility.clear_sell_loss_after1)
                    {
                        return;
                    }
                    else if (sell_message.Equals("익절매도") || sell_message.Equals("익절원") || sell_message.Equals("익절TS") && !utility.profit_after1)
                    {
                        return;
                    }
                    else if (sell_message.Equals(" 손절매도") || sell_message.Equals("손절원") && !utility.loss_after1)
                    {
                        return;
                    }

                    //
                    CpTd0322.SetInputValue(0, "1"); //매도
                    CpTd0322.SetInputValue(1, acc_text.Text); //계좌번호
                    CpTd0322.SetInputValue(2, gubun); //상품관리구분코드
                    CpTd0322.SetInputValue(3, code); //종목코드
                    CpTd0322.SetInputValue(4, order_acc); //주문수량
                    //
                    int check2 = 0;
                    //
                    //수신확인
                    while (true)
                    {
                        if (CpTd0322.GetDibStatus() == 1)
                        {
                            check2++;
                            WriteLog_System("[DibRq요청/수신대기/5초] : 시간외종가 주문\n");
                            System.Threading.Thread.Sleep(5000);
                        }
                        else if (check2 == 5)
                        {
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }
                    //
                    if (check2 == 5)
                    {
                        WriteLog_System("[시간외종가 주문/수신실패] : 재부팅 요망\n");
                        telegram_message("[시간외종가 주문/수신실패] : 재부팅 요망\n");
                        return;
                    }
                    //
                    int error = CpTd0322.BlockRequest();
                    //
                    if (error == 0)
                    {
                        row["상태"] = "매도중";
                        row["주문번호"] = Convert.ToString(CpTd0322.GetHeaderValue(5));
                        row["매매진입"] = time2;

                        // 데이터 변경 사항을 바인딩 소스에 알리기 (UI 스레드에서 수행)
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }

                        WriteLog_Order($"[{sell_message}/시간외종가/주문성공/{gubun}] : {code_name}({code}) {order_acc}개\n");
                        WriteLog_Order($"[{sell_message}/시간외종가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                        telegram_message($"[{sell_message}/시간외종가//주문성공/{gubun}] : {code_name}({code}) {order_acc}개 {percent}\n");
                        telegram_message($"[{sell_message}/시간외종가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                    }
                    else
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";
                        //
                        // 데이터 변경 사항을 바인딩 소스에 알리기 (UI 스레드에서 수행)
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }

                        WriteLog_Order($"[{sell_message}/시간외종가//주문실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                        telegram_message($"[{sell_message}/시간외종가//주문실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                    }

                }
                //시간외단일가
                //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=284&seq=183&page=1&searchString=%eb%8b%a8%ec%9d%bc%ea%b0%80&p=8841&v=8643&m=9505
                else if (market_time == 2)
                {
                    if (sell_message.Equals("청산매도/일반") || sell_message.Equals("청산매도/수익") && !utility.clear_sell_profit_after2)
                    {
                        return;
                    }
                    else if (sell_message.Equals("청산매도/손실") && !utility.clear_sell_loss_after2)
                    {
                        return;
                    }
                    else if (sell_message.Equals("익절매도") || sell_message.Equals("익절원") || sell_message.Equals("익절TS") && !utility.profit_after2)
                    {
                        return;
                    }
                    else if (sell_message.Equals(" 손절매도") || sell_message.Equals("손절원") && !utility.loss_after2)
                    {
                        return;
                    }

                    order_method = sell_condtion_method_after.Split('/');
                    //
                    int edited_price_hoga = hoga_cal(Convert.ToInt32(price), order_method[1].Equals("현재가") ? 0 : Convert.ToInt32(order_method[1].Replace("호가", "")));

                    //
                    CpTd0386.SetInputValue(0, "1"); //매도
                    CpTd0386.SetInputValue(1, acc_text.Text); //계좌번호
                    CpTd0386.SetInputValue(2, gubun); //상품관리구분코드
                    CpTd0386.SetInputValue(3, code); //종목코드
                    CpTd0386.SetInputValue(4, order_acc); //주문수량
                    CpTd0386.SetInputValue(5, edited_price_hoga); //주문단가
                    //
                    int check2 = 0;
                    //
                    //수신확인
                    while (true)
                    {
                        if (CpTd0386.GetDibStatus() == 1)
                        {
                            check2++;
                            WriteLog_System("[DibRq요청/수신대기/5초] : 시간외단일가 주문\n");
                            System.Threading.Thread.Sleep(5000);
                        }
                        else if (check2 == 5)
                        {
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }
                    //
                    if (check2 == 5)
                    {
                        WriteLog_System("[시간외단일가 주문/수신실패] : 재부팅 요망\n");
                        telegram_message("[시간외단일가 주문/수신실패] : 재부팅 요망\n");
                        return;
                    }
                    //
                    int error = CpTd0386.BlockRequest();
                    //
                    if (error == 0)
                    {
                        row["상태"] = "매도중";
                        row["주문번호"] = Convert.ToString(CpTd0386.GetHeaderValue(5));
                        row["매매진입"] = time2;

                        // 데이터 변경 사항을 바인딩 소스에 알리기 (UI 스레드에서 수행)
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }

                        WriteLog_Order($"[{sell_message}/시간외단일가/주문성공/{gubun}] : {code_name}({code}) {order_acc}개\n");
                        WriteLog_Order($"[{sell_message}/시간외단일가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                        telegram_message($"[{sell_message}/시간외단일가//주문성공/{gubun}] : {code_name}({code}) {order_acc}개 {percent}\n");
                        telegram_message($"[{sell_message}/시간외단일가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                    }
                    else
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";
                        //
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }

                        WriteLog_Order($"[{sell_message}/시간외종가//주문실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                        telegram_message($"[{sell_message}/시간외종가//주문실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                    }
                }
                //시장가 주문 + 청산주문
                else if (sell_message.Split('/')[0].Equals("청산매도") || order_method[0].Equals("시장가"))
                {
                    CpTd0311.SetInputValue(0, "1"); //매도
                    CpTd0311.SetInputValue(1, acc_text.Text); //계좌번호
                    CpTd0311.SetInputValue(2, gubun); //상품관리구분코드
                    CpTd0311.SetInputValue(3, code); //종목코드
                    CpTd0311.SetInputValue(4, order_acc); //주문수량
                    CpTd0311.SetInputValue(5, 0); //주문단가
                    CpTd0311.SetInputValue(7, "0"); //주문조건구분코드
                    CpTd0311.SetInputValue(8, "03"); //시장가
                    //
                    int check2 = 0;
                    //
                    //수신확인
                    while (true)
                    {
                        if (CpTd0311.GetDibStatus() == 1)
                        {
                            check2++;
                            WriteLog_System("[DibRq요청/수신대기/5초] : 시장가 주문\n");
                            System.Threading.Thread.Sleep(5000);
                        }
                        else if (check2 == 5)
                        {
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }
                    //
                    if (check2 == 5)
                    {
                        WriteLog_System("[시장가 주문/수신실패] : 재부팅 요망\n");
                        telegram_message("[시장가 주문/수신실패] : 재부팅 요망\n");
                        return;
                    }
                    int error = CpTd0311.BlockRequest();
                    //
                    if (error == 0)
                    {
                        row["상태"] = "매도중";
                        row["주문번호"] = Convert.ToString(CpTd0311.GetHeaderValue(8));
                        row["매매진입"] = time2;
                        //
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }

                        WriteLog_Order($"[{sell_message}/시장가/주문성공/{gubun}] : {code_name}({code}) {order_acc}개\n");
                        WriteLog_Order($"[{sell_message}/시장가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                        telegram_message($"[{sell_message}/시장가//주문성공/{gubun}] : {code_name}({code}) {order_acc}개 {percent}\n");
                        telegram_message($"[{sell_message}/시장가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                    }
                    else
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";
                        //
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }

                        WriteLog_Order($"[{sell_message}/시장가//주문실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                        telegram_message($"[{sell_message}/시장가//주문실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                    }
                }
                //지정가 주문
                else
                {
                    int edited_price_hoga = hoga_cal(Convert.ToInt32(price), order_method[1].Equals("현재가") ? 0 : Convert.ToInt32(order_method[1].Replace("호가", "")));

                    CpTd0311.SetInputValue(0, "1"); //매도
                    CpTd0311.SetInputValue(1, acc_text.Text); //계좌번호
                    CpTd0311.SetInputValue(2, gubun); //상품관리구분코드
                    CpTd0311.SetInputValue(3, code); //종목코드
                    CpTd0311.SetInputValue(4, order_acc); //주문수량
                    CpTd0311.SetInputValue(5, edited_price_hoga); //주문단가
                    CpTd0311.SetInputValue(7, "0"); //주문조건구분코드
                    CpTd0311.SetInputValue(8, "01"); //지정가
                    //
                    int check2 = 0;
                    //
                    //수신확인
                    while (true)
                    {
                        if (CpTd0311.GetDibStatus() == 1)
                        {
                            check2++;
                            WriteLog_System("[DibRq요청/수신대기/5초] : 지정가 주문\n");
                            System.Threading.Thread.Sleep(5000);
                        }
                        else if (check2 == 5)
                        {
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }
                    //
                    if (check2 == 5)
                    {
                        WriteLog_System("[지정가 주문/수신실패] : 재부팅 요망\n");
                        telegram_message("[지정가 주문/수신실패] : 재부팅 요망\n");
                        return;
                    }
                    int error = CpTd0311.BlockRequest();
                    //
                    if (error == 0)
                    {
                        row["상태"] = "매도중";
                        row["주문번호"] = Convert.ToString(CpTd0311.GetHeaderValue(8));
                        row["매매진입"] = time2;

                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }

                        WriteLog_Order($"[{sell_message}/지정가/주문성공/{gubun}] : {code_name}({code}) {order_acc}개 {edited_price_hoga}원\n");
                        WriteLog_Order($"[{sell_message}/지정가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                        telegram_message($"[{sell_message}/지정가/주문성공/{gubun}] : {code_name}({code}) {order_acc}개 {edited_price_hoga}\n원 {percent}\n");
                        telegram_message($"[{sell_message}/지정가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                    }
                    else if (error == -308)
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";
                        //
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }

                        WriteLog_Order($"[{sell_message}/지정가/주문실패/{gubun}] : {code_name}({code}) 초당 5회 이상 주문 불가\n");
                        telegram_message($"[{sell_message}/지정가/주문실패/{gubun}] : {code_name}({code}) 초당 5회 이상 주문 불가\n");
                    }
                    else
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";
                        //
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }

                        WriteLog_Order($"[{sell_message}/지정가/주문실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                        telegram_message($"[{sell_message}/지정가/주문실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                    }
                }             
            }
        }

        //------------호가 계산---------------------
        private int hoga_cal(int price, int hoga)
        {
            int[] hogaUnits = { 1, 5, 10, 50, 100, 500, 1000 }; // 이미지에서 제공된 단위
            int[] hogaRanges = { 0, 2000, 5000, 10000, 50000, 200000 }; // 이미지에서 제공된 범위

            if (hoga == 0) return price;

            for (int i = hogaRanges.Length - 1; i >= 0; i--)
            {
                if (price > hogaRanges[i])
                {
                    int increment = hoga * hogaUnits[i];
                    int nextPrice = price + increment;

                    // Check if the next price crosses the range boundary
                    if (nextPrice > hogaRanges[i])
                    {
                        // Adjust the increment to match the new range
                        int remainingIncrement = hogaRanges[i] - price;
                        return price + remainingIncrement;
                    }

                    return nextPrice;
                }
            }
            return price;
        }

        //#################################################여기서 부터 시작############################################
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=291&seq=155&page=3&searchString=&p=&v=&m=
        //------------주문 상태 확인 및 정정---------------------

        //주문번호가 업데이트 않된 경우가 있어서 임시 저장한다.
        private Queue<string[]> Trade_check_save = new Queue<string[]>();

        private void Trade_Check()
        {
            //string account = CpConclusion.GetHeaderValue(7); //계좌번호
            //string account_ = CpConclusion.GetHeaderValue(8); //상품관리코드
            //string stock_acc = Convert.ToString(CpConclusion.GetHeaderValue(3)); //체결수량
            //string trade_deal = CpConclusion.GetHeaderValue(14); //체결구분코드(1체결,2확인,3거부,4접수)

            //
            string gugu = Convert.ToString(CpConclusion.GetHeaderValue(14));
            string code = CpConclusion.GetHeaderValue(9); //종목코드
            string code_name = CpConclusion.GetHeaderValue(2); //종목명
            string order_number = Convert.ToString(CpConclusion.GetHeaderValue(5)); //주문번호
            string trade_Gubun = CpConclusion.GetHeaderValue(12) == "1" ? "매도" : "매수"; //매매구분(1매도,2매수)
            string hold_sum = Convert.ToString(CpConclusion.GetHeaderValue(23)); //체결기준잔고수량
            string time = DateTime.Now.ToString("HH:mm:ss"); //시간
            string gubun = Convert.ToString(CpConclusion.GetHeaderValue(8)); //상품관리구분코드
            string cancel = Convert.ToString(CpConclusion.GetHeaderValue(16)); //정정취소구분코드

            string[] tmp = { gugu, code, code_name, order_number, trade_Gubun, hold_sum, time, gubun, cancel };

            WriteLog_System($"[체결수신] : {gugu}/{code}/{code_name}/{order_number}/{trade_Gubun}/{hold_sum}/{gubun}/{cancel}\n");

            Trade_check_save.Enqueue(tmp);
        }

        //timer3 : 200ms 마다 값을 점검한다.
        private void Trade_Check_Event(object sender, EventArgs e)
        {
            if(Trade_check_save.Count != 0)
            {
                string[] tmp = Trade_check_save.Peek();

                DataRow[] findRows = dtCondStock.AsEnumerable().Where(row => row.Field<string>("주문번호") == tmp[3]).ToArray();

                if (findRows.Any())
                {
                    string gugu = tmp[0];
                    string code = tmp[1]; //종목코드
                    string code_name = tmp[2]; //종목명
                    string order_number = tmp[3]; //주문번호
                    string trade_Gubun = tmp[4]; //매매구분(1매도,2매수)
                    string hold_sum = tmp[5]; //체결기준잔고수량
                    string time = tmp[6]; //시간
                    string gubun = tmp[7];
                    string cancel = tmp[8];

                    Trade_check_save.Dequeue();

                    string order_sum = findRows[0]["보유수량"].ToString().Split('/')[1];

                    if (cancel.Equals("1"))
                    {
                        WriteLog_Order($"[체결상세/{code_name}({code})/{trade_Gubun}/{gubun}] : {hold_sum}/{order_sum}\n");

                        //매수확인
                        if (trade_Gubun.Equals("매수") && order_sum == hold_sum)
                        {
                            //체결내역업데이트(주문번호)
                            dtCondStock_Transaction.Clear();
                            Transaction_Detail_seperate(order_number, "매수");

                            System.Threading.Thread.Sleep(250);

                            findRows[0]["보유수량"] = $"{hold_sum}/{order_sum}";
                            findRows[0]["매수시각"] = time;
                            //
                            if (dataGridView1.InvokeRequired)
                            {
                                dataGridView1.Invoke((MethodInvoker)delegate {
                                    bindingSource.ResetBindings(false);
                                });
                            }
                            else
                            {
                                bindingSource.ResetBindings(false);
                            }

                            //매도실현손익(제세금, 수수료 포함)
                            today_profit_tax_load_seperate();

                            System.Threading.Thread.Sleep(250);

                            //D+2 예수금 + 계좌 보유 종목
                            dtCondStock_hold.Clear();
                            GetCashInfo_Seperate(false);

                            System.Threading.Thread.Sleep(250);
                        }
                        //매도확인
                        else if (trade_Gubun.Equals("매도") && hold_sum.Equals("0"))
                        {
                            //체결내역업데이트(주문번호)
                            dtCondStock_Transaction.Clear();
                            Transaction_Detail_seperate(order_number, "매도");

                            System.Threading.Thread.Sleep(250);

                            findRows[0]["보유수량"] = $"{hold_sum}/0";

                            //중복거래허용
                            if (!utility.duplication_deny)
                            {
                                //편입 차트 상태 '대기' 변경
                                findRows[0]["매도시각"] = time;
                                //
                                if (dataGridView1.InvokeRequired)
                                {
                                    dataGridView1.Invoke((MethodInvoker)delegate {
                                        bindingSource.ResetBindings(false);
                                    });
                                }
                                else
                                {
                                    bindingSource.ResetBindings(false);
                                }
                            }
                            //중복거래비허용
                            else
                            {
                                //code 종목 실시간 해지
                                StockCur.SetInputValue(0, code);
                                StockCur.Unsubscribe();
                                //
                                findRows[0]["매도시각"] = time;
                                //
                                if (dataGridView1.InvokeRequired)
                                {
                                    dataGridView1.Invoke((MethodInvoker)delegate {
                                        bindingSource.ResetBindings(false);
                                    });
                                }
                                else
                                {
                                    bindingSource.ResetBindings(false);
                                }
                            }

                            System.Threading.Thread.Sleep(250);

                            //보유 수량 업데이트
                            string[] hold_status = max_hoid.Text.Split('/');
                            int hold = Convert.ToInt32(hold_status[0]);
                            int hold_max = Convert.ToInt32(hold_status[1]);
                            max_hoid.Text = $"{hold - 1}/{hold_max}";

                            //당일 손익 + 당일 손일률 + 당일 수수료 업데이트
                            today_profit_tax_load_seperate();

                            System.Threading.Thread.Sleep(250);

                            //D+2 예수금 + 계좌 보유 종목
                            dtCondStock_hold.Clear();
                            GetCashInfo_Seperate(false);

                            System.Threading.Thread.Sleep(250);
                        }
                    }
                    else if(cancel == "3" && gugu == "2") //최소주문 && 확인
                    {
                        /*
                         [15:13:24:413][System] : [체결수신] : 4/A074600/원익QnC/3644/매수/138/10/3
                         [15:13:24:414][System] : [체결수신] : 2/A074600/원익QnC/3644/매수/138/10/3
                         [15:18:18:100][System] : [체결수신] : 4/A074600/원익QnC/3674/매도/10/10/3
                         [15:18:18:100][System] : [체결수신] : 2/A074600/원익QnC/3674/매도/10/10/3
                        */

                        //체결내역업데이트(주문번호)
                        dtCondStock_Transaction.Clear();
                        Transaction_Detail_seperate("", "");

                        if (trade_Gubun.Equals("매수"))
                        {
                            if(hold_sum == "0")
                            {
                                findRows[0]["보유수량"] = $"{hold_sum}/{hold_sum}";
                                if (utility.buy_AND)
                                {
                                    findRows[0]["상태"] = "주문";
                                }
                                else
                                {
                                    findRows[0]["상태"] = "대기";
                                }
                                //보유 수량 업데이트
                                string[] hold_status = max_hoid.Text.Split('/');
                                int hold = Convert.ToInt32(hold_status[0]);
                                int hold_max = Convert.ToInt32(hold_status[1]);
                                max_hoid.Text = $"{hold - 1}/{hold_max}";
                            }
                            else
                            {
                                findRows[0]["보유수량"] = $"{hold_sum}/{hold_sum}";
                            }
                        }
                        else
                        {
                            if (hold_sum == "0")
                            {
                                findRows[0]["보유수량"] = $"{hold_sum}/{order_sum}";
                                findRows[0]["상태"] = "매도완료";
                                //
                                //보유 수량 업데이트
                                string[] hold_status = max_hoid.Text.Split('/');
                                int hold = Convert.ToInt32(hold_status[0]);
                                int hold_max = Convert.ToInt32(hold_status[1]);
                                max_hoid.Text = $"{hold - 1}/{hold_max}";
                            }
                            else
                            {
                                findRows[0]["보유수량"] = $"{hold_sum}/{order_sum}";
                                findRows[0]["상태"] = "매수완료";
                            }
                        }

                        //반영
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke((MethodInvoker)delegate {
                                bindingSource.ResetBindings(false);
                            });
                        }
                        else
                        {
                            bindingSource.ResetBindings(false);
                        }

                        //매도실현손익(제세금, 수수료 포함)
                        today_profit_tax_load_seperate();

                        System.Threading.Thread.Sleep(250);

                        //D+2 예수금 + 계좌 보유 종목
                        dtCondStock_hold.Clear();
                        GetCashInfo_Seperate(false);

                        System.Threading.Thread.Sleep(250);
                    }
                }
                else
                {
                    WriteLog_System($"[체결내역] 주문번호 없음 - {tmp[3]}\n");
                }
            }
        }

        //--------------------------------------미체결 주문-------------------------------------------------------------

        private void order_close(string trade_type, string order_number, string gubun, string code_name, string code, string order_acc)
        {
            //주문시간 확인(0정규장, 1시간외종가, 2시간외단일가
            int market_time = 0;

            TimeSpan t_now = TimeSpan.Parse(DateTime.Now.ToString("HH:mm:ss"));
            TimeSpan t_time0 = TimeSpan.Parse("15:30:00");
            TimeSpan t_time1 = TimeSpan.Parse("15:40:00");
            TimeSpan t_time2 = TimeSpan.Parse("16:00:00");
            TimeSpan t_time3 = TimeSpan.Parse("18:00:00");

            // result가 0보다 작으면 time1 < time2
            // result가 0이면 time1 = time2
            // result가 0보다 크면 time1 > time2
            if (t_time0.CompareTo(t_now) <= 0 && t_now.CompareTo(t_time1) < 0)
            {
                WriteLog_Order($"[{trade_type}/ 주문취소/정규장종료/{gubun}] : {code_name}({code}) {order_acc}개\n");
                return;
            }
            else if (t_time1.CompareTo(t_now) <= 0 && t_now.CompareTo(t_time2) < 0)
            {
                market_time = 1;
            }
            else if (t_time2.CompareTo(t_now) <= 0 && t_now.CompareTo(t_time3) < 0)
            {
                market_time = 2;
            }
            else if (t_now.CompareTo(t_time3) >= 0)
            {
                WriteLog_Order($"[{trade_type}/주문취소/시간외단일가종료/{gubun}] : {code_name}({code}) {order_acc}개\n");
                return;
            }

            var findRows = dtCondStock.AsEnumerable().Where(row2 => row2.Field<string>("주문번호") == order_number);

            if (!findRows.Any())
            {
                WriteLog_Order($"[{trade_type}/주문취소/실패/{gubun}] : {code_name}({code}) 매도중\n");
                telegram_message($"[{trade_type}/주문취소/실패/{gubun}] : {code_name}({code}) 매도중\n");
            }

            WriteLog_Order($"[{trade_type}/주문취소/접수/{gubun}] : {code_name}({code}) {order_acc}개\n");
            telegram_message($"[{trade_type}/주문취소/접수/{gubun}] : {code_name}({code}) {order_acc}개\n");

            string time2 = DateTime.Now.ToString("HH:mm:ss");

            DataRow row = findRows.First();

            //시간외종가
            //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=291&seq=179&page=1&searchString=&p=&v=&m=
            if (market_time == 1)
            {

                CpTd0326.SetInputValue(1, Convert.ToInt32(order_number)); //원주문번호(long)
                CpTd0326.SetInputValue(2, acc_text.Text); //계좌번호
                CpTd0326.SetInputValue(3, gubun); //상품관리구분코드
                CpTd0326.SetInputValue(4, code); //종목코드
                CpTd0326.SetInputValue(5, 0); //취소수량(일단 주문수량 대체)
                //
                int check2 = 0;
                //
                //수신확인
                while (true)
                {
                    if (CpTd0326.GetDibStatus() == 1)
                    {
                        check2++;
                        WriteLog_System($"[DibRq요청/수신대기/5초] : 시간외종가 {trade_type}주문취소 주문\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check2 == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check2 == 5)
                {
                    WriteLog_System($"[{trade_type}/주문취소/시간외종가/수신실패] : 재부팅 요망\n");
                    telegram_message($"[{trade_type}/주문취소/시간외종가/수신실패] : 재부팅 요망\n");
                    return;
                }
                //
                int error = CpTd0326.BlockRequest();
                //
                if (error == 0)
                {
                    string order_number_cancel = Convert.ToString(CpTd0314.GetHeaderValue(6).ToString());
                    if (order_number_cancel == "0")
                    {
                        WriteLog_Order($"[{trade_type}/주문취소/시간외종가/취소실패/{gubun}] : {code_name}({code}) {order_acc}개 {trade_type} 주문 완료\n");
                        telegram_message($"[{trade_type}/주문취소/시간외종가/취소실패/{gubun}] : {code_name}({code}) {order_acc}개 {trade_type} 주문 완료\n");
                        return;
                    }
                    //
                    if (trade_type.Equals("매수"))
                    {
                        dtCondStock_Transaction.Clear();
                        Transaction_Detail_seperate(order_number, "매수");
                        //
                        row["상태"] = "매수완료";
                        row["주문번호"] = order_number_cancel;
                        //row["보유수량"] = Convert.ToString(Convert.ToInt32(order_acc) - cancel_acc) + "/" + Convert.ToString(Convert.ToInt32(order_acc) - cancel_acc);
                    }
                    else
                    {
                        dtCondStock_Transaction.Clear();
                        Transaction_Detail_seperate(order_number, "매도");
                        //
                        row["상태"] = "매수완료";
                        row["주문번호"] = order_number_cancel;
                        //row["보유수량"] = Convert.ToString(Convert.ToInt32(order_acc) - cancel_acc) + "/" + order_acc;
                    }
                    //
                    if (dataGridView1.InvokeRequired)
                    {
                        dataGridView1.Invoke((MethodInvoker)delegate {
                            bindingSource.ResetBindings(false);
                        });
                    }
                    else
                    {
                        bindingSource.ResetBindings(false);
                    }

                    WriteLog_Order($"[{trade_type}/주문취소/시간외종가/취소성공/{gubun}] : {code_name}({code}) {order_acc}개\n");
                    telegram_message($"[{trade_type}/주문취소/시간외종가//취소성공/{gubun}] : {code_name}({code}) {order_acc}개\n");                   
                }
                else
                {
                    if (trade_type.Equals("매수"))
                    {
                        row["상태"] = "매수중";
                    }
                    else
                    {
                        row["상태"] = "매도중";
                    }
                    //
                    if (dataGridView1.InvokeRequired)
                    {
                        dataGridView1.Invoke((MethodInvoker)delegate {
                            bindingSource.ResetBindings(false);
                        });
                    }
                    else
                    {
                        bindingSource.ResetBindings(false);
                    }

                    WriteLog_Order($"[{trade_type}/주문취소/시간외종가/취소실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                    telegram_message($"[{trade_type}/주문취소/시간외종가/취소실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                }
            }
            //시간외단일가
            //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=291&seq=184&page=1&searchString=&p=&v=&m=
            else if (market_time == 2)
            {
                CpTd0387.SetInputValue(1, Convert.ToInt32(order_number)); //원주문번호(long)
                CpTd0387.SetInputValue(2, acc_text.Text); //계좌번호
                CpTd0387.SetInputValue(3, gubun); //상품관리구분코드
                CpTd0387.SetInputValue(4, code.Trim()); //종목코드
                CpTd0387.SetInputValue(5, 0); //취소수량(일단 주문수량 대체)
                //
                int check2 = 0;
                //
                //수신확인
                while (true)
                {
                    if (CpTd0387.GetDibStatus() == 1)
                    {
                        check2++;
                        WriteLog_System($"[DibRq요청/수신대기/5초] : 시간외단일가 {trade_type}주문취소 주문\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check2 == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check2 == 5)
                {
                    WriteLog_System($"[{trade_type}/주문취소/시간외단일가/수신실패] : 재부팅 요망\n");
                    telegram_message($"[{trade_type}/주문취소/시간외단일가/수신실패] : 재부팅 요망\n");
                    return;
                }
                //
                int error = CpTd0387.BlockRequest();
                //
                if (error == 0)
                {
                    string order_number_cancel = Convert.ToString(CpTd0314.GetHeaderValue(6).ToString());
                    if (order_number_cancel == "0")
                    {
                        WriteLog_Order($"[{trade_type}/주문취소/시간외단일가/취소실패/{gubun}] : {code_name}({code}) {order_acc}개 {trade_type} 주문 완료\n");
                        telegram_message($"[{trade_type}/주문취소/시간외단일가/취소실패/{gubun}] : {code_name}({code}) {order_acc}개 {trade_type} 주문 완료\n");
                        return;
                    }
                    //
                    if (trade_type.Equals("매수"))
                    {
                        dtCondStock_Transaction.Clear();
                        Transaction_Detail_seperate(order_number, "매수");
                        //
                        row["상태"] = "매수완료";
                        row["주문번호"] = order_number_cancel;
                        //row["보유수량"] = Convert.ToString(Convert.ToInt32(order_acc) - cancel_acc) + "/" + Convert.ToString(Convert.ToInt32(order_acc) - cancel_acc);
                    }
                    else
                    {
                        dtCondStock_Transaction.Clear();
                        Transaction_Detail_seperate(order_number, "매도");
                        //
                        row["상태"] = "매수완료";
                        row["주문번호"] = order_number_cancel;
                        //row["보유수량"] = Convert.ToString(Convert.ToInt32(order_acc) - cancel_acc) + "/" + order_acc;
                    }
                    //
                    if (dataGridView1.InvokeRequired)
                    {
                        dataGridView1.Invoke((MethodInvoker)delegate {
                            bindingSource.ResetBindings(false);
                        });
                    }
                    else
                    {
                        bindingSource.ResetBindings(false);
                    }
                    //
                    WriteLog_Order($"[{trade_type}/주문취소/시간외단일가/취소성공/{gubun}] : {code_name}({code}) {order_acc}개\n");
                    telegram_message($"[{trade_type}/주문취소/시간외단일가//취소성공/{gubun}] : {code_name}({code}) {order_acc}개\n");            
                }
                else
                {
                    //편입 차트 상태 '매수완료' 변경
                    if (trade_type.Equals("매수"))
                    {
                        row["상태"] = "매수중";
                    }
                    else
                    {
                        row["상태"] = "매도중";
                    }
                    //
                    if (dataGridView1.InvokeRequired)
                    {
                        dataGridView1.Invoke((MethodInvoker)delegate {
                            bindingSource.ResetBindings(false);
                        });
                    }
                    else
                    {
                        bindingSource.ResetBindings(false);
                    }

                    WriteLog_Order($"[{trade_type}/주문취소/시간외단일가/취소실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                    telegram_message($"[{trade_type}/주문취소/시간외단일가/취소실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                }
            }
            //정규장
            //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=291&seq=162&page=2&searchString=&p=&v=&m=
            else
            {
                CpTd0314.SetInputValue(1, Convert.ToInt32(order_number)); //원주문번호(long)
                CpTd0314.SetInputValue(2, acc_text.Text); //계좌번호
                CpTd0314.SetInputValue(3, gubun); //상품관리구분코드
                CpTd0314.SetInputValue(4, code); //종목코드
                CpTd0314.SetInputValue(5, 0); //취소수량(일단 주문수량 대체)
                //
                int check2 = 0;
                //
                //수신확인
                while (true)
                {
                    if (CpTd0314.GetDibStatus() == 1)
                    {
                        check2++;
                        WriteLog_System($"[DibRq요청/수신대기/5초] : 정규장 {trade_type}주문취소 주문\n");
                        System.Threading.Thread.Sleep(5000);
                    }
                    else if (check2 == 5)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                //
                if (check2 == 5)
                {
                    WriteLog_System($"[{trade_type}/주문취소/정규장/수신실패] : 재부팅 요망\n");
                    telegram_message($"[{trade_type}/주문취소/정규장/수신실패] : 재부팅 요망\n");
                    return;
                }
                //
                int error = CpTd0314.BlockRequest();
                //
                if (error == 0)
                {
                    string order_number_cancel = Convert.ToString(CpTd0314.GetHeaderValue(6).ToString());
                    if (order_number_cancel == "0")
                    {
                        WriteLog_Order($"[{trade_type}/주문취소/정규장/취소실패/{gubun}] : {code_name}({code}) {order_acc}개 {trade_type} 주문 완료\n");
                        telegram_message($"[{trade_type}/주문취소/정규장/취소실패/{gubun}] : {code_name}({code}) {order_acc}개 {trade_type} 주문 완료\n");
                        return;
                    }

                    int cancel_acc = Convert.ToInt32(CpTd0314.GetHeaderValue(5));
                    //
                    if (trade_type.Equals("매수"))
                    {
                        dtCondStock_Transaction.Clear();
                        Transaction_Detail_seperate(order_number, "매수");
                        //
                        row["상태"] = "매수완료";
                        row["주문번호"] = order_number_cancel;
                        //row["보유수량"] = Convert.ToString(Convert.ToInt32(order_acc) - cancel_acc) + "/" + Convert.ToString(Convert.ToInt32(order_acc) - cancel_acc);
                        //체결내역업데이트(주문번호)
                    }
                    else
                    {
                        dtCondStock_Transaction.Clear();
                        Transaction_Detail_seperate(order_number, "매도");
                        //
                        row["상태"] = "매수완료";
                        row["주문번호"] = order_number_cancel;
                        //row["보유수량"] = Convert.ToString(Convert.ToInt32(order_acc) - cancel_acc) + "/" + order_acc;
                    }
                    //
                    if (dataGridView1.InvokeRequired)
                    {
                        dataGridView1.Invoke((MethodInvoker)delegate {
                            bindingSource.ResetBindings(false);
                        });
                    }
                    else
                    {
                        bindingSource.ResetBindings(false);
                    }

                    WriteLog_Order($"[{trade_type}/주문취소/정규장/취소성공/{gubun}] : {code_name}({code}) {order_acc}개\n");                  
                    telegram_message($"[{trade_type}/주문취소/정규장//취소성공/{gubun}] : {code_name}({code}) {order_acc}개\n");
                }
                else
                {
                    //편입 차트 상태 '매수완료' 변경
                    if (trade_type.Equals("매수"))
                    {
                        row["상태"] = "매수중";
                    }
                    else
                    {
                        row["상태"] = "매도중";
                    }
                    //
                    if (dataGridView1.InvokeRequired)
                    {
                        dataGridView1.Invoke((MethodInvoker)delegate {
                            bindingSource.ResetBindings(false);
                        });
                    }
                    else
                    {
                        bindingSource.ResetBindings(false);
                    }

                    WriteLog_Order($"[{trade_type}/주문취소/정규장/취소실패/{gubun}] : {code_name}({code})");
                    WriteLog_Order($"{CpTd0314.GetDibMsg1()}\n");
                    telegram_message($"[{trade_type}/주문취소/정규장/취소실패/{gubun}] : {code_name}({code}) {error_message(error)}\n");
                }
            }
        }

        //--------------------------------------조건식중단-------------------------------------------------------------

        public void real_time_stop(bool real_price_all_stop)
        {           
            //실시간 중단이 선언되면 '실시간시작'이 가능해진다.
            Real_time_stop_btn.Enabled = false;
            Real_time_search_btn.Enabled = true;

            //매수 조건식 중단
            if (utility.buy_condition && Condition_Profile.Count != 0)
            {
                // 검색된 조건식이 없을시
                if (string.IsNullOrEmpty(utility.Fomula_list_buy_text))
                {
                    WriteLog_System("[실시간매수조건/중단실패] : 조건식없음\n");
                    telegram_message("[실시간매수조건/중단실패] : 조건식없음\n");
                    Real_time_stop_btn.Enabled = true;
                    Real_time_search_btn.Enabled = false;
                }
                else
                {
                    //검색된 매수 조건식이 있을시
                    string[] condition = utility.Fomula_list_buy_text.Split(',');
                    for (int i = 0; i < condition.Length; i++)
                    {
                        CPSYSDIBLib.CssWatchStgControl CssWatchStgControl = Condition_Profile[i];
                        //
                        string[] tmp = condition[i].Split('^');
                        //
                        int conditon_SubCode = condition_sub_code(tmp[0]);
                        if (conditon_SubCode == -1)
                        {
                            WriteLog_System($"[Condition/{tmp[1]}] : 일련번호 없음 \n");
                            return;
                        }

                        CssWatchStgControl.SetInputValue(0, tmp[0]); //전략ID
                        CssWatchStgControl.SetInputValue(1, conditon_SubCode); //감시 일련번호
                        CssWatchStgControl.SetInputValue(2, '3'); //감시 취소
                        //
                        int check2 = 0;
                        //
                        //수신확인
                        while (true)
                        {
                            if (CssWatchStgControl.GetDibStatus() == 1)
                            {
                                check2++;
                                WriteLog_System("[DibRq요청/수신대기/5초] : 매수 조건식 중단\n");
                                System.Threading.Thread.Sleep(5000);
                            }
                            else if (check2 == 5)
                            {
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }
                        //
                        if (check2 == 5)
                        {
                            WriteLog_System("[매수 조건식 중단/수신실패] : 재부팅 요망\n");
                            telegram_message("[매수 조건식 중단/수신실패] : 재부팅 요망\n");
                            return;
                        }                                          //
                        int result = CssWatchStgControl.BlockRequest();
                        //
                        if (result == 0)
                        {
                            int result_type = Convert.ToInt32(CssWatchStgControl.GetHeaderValue(0));
                            if (result_type == 0)
                            {
                                WriteLog_System($"[실시간매수조건/{tmp[1]}] : 초기상태 \n");
                                telegram_message($"[실시간매수조건/{tmp[1]}] : 초기상태 \n");
                            }
                            else if (result_type == 1)
                            {
                                WriteLog_System($"[실시간매수조건/{tmp[1]}] : 감시중 \n");
                                telegram_message($"[실시간매수조건/{tmp[1]}] : 감시중 \n");
                            }
                            else if (result_type == 2)
                            {
                                WriteLog_System($"[실시간매수조건/{tmp[1]}] : 감시중단 \n");
                                telegram_message($"[실시간매수조건/{tmp[1]}] : 감시중단 \n");
                            }
                            else
                            {
                                WriteLog_System($"[실시간매수조건/{tmp[1]}] : 등록취소 \n");
                                telegram_message($"[실시간매수조건/{tmp[1]}] : 등록취소 \n");
                            }
                        }
                    }

                    //기존 항목 비우기
                    Condition_Profile.Clear();
                }
            }

            System.Threading.Thread.Sleep(250);

            //매도 조건식 중단
            if (utility.sell_condition && Condition_Profile2.Count != 0)
            {
                // 검색된 조건식이 없을시
                if (string.IsNullOrEmpty(utility.Fomula_list_sell_text))
                {
                    WriteLog_System("[실시간매도조건/중단실패] : 조건식없음\n");
                    telegram_message("[실시간매도조건/중단실패] : 조건식없음\n");
                    Real_time_stop_btn.Enabled = true;
                    Real_time_search_btn.Enabled = false;
                }
                else
                {
                    //검색된 매수 조건식이 있을시
                    string[] condition = utility.Fomula_list_sell_text.Split(',');
                    for (int i = 0; i < condition.Length; i++)
                    {
                        CPSYSDIBLib.CssWatchStgControl CssWatchStgControl = Condition_Profile2[i];
                        //
                        string[] tmp = condition[i].Split('^');
                        //
                        int conditon_SubCode = condition_sub_code(tmp[0]);
                        if (conditon_SubCode == -1)
                        {
                            WriteLog_System($"[Condition/{tmp[1]}] : 일련번호 없음 \n");
                            return;
                        }
                        CssWatchStgControl.SetInputValue(0, tmp[0]); //전략ID
                        CssWatchStgControl.SetInputValue(1, conditon_SubCode); //감시 일련번호
                        CssWatchStgControl.SetInputValue(2, '3'); //감시 취소
                        //
                        int check2 = 0;
                        //
                        //수신확인
                        while (true)
                        {
                            if (CssWatchStgControl.GetDibStatus() == 1)
                            {
                                check2++;
                                WriteLog_System("[DibRq요청/수신대기/5초] : 매도 조건식 중단\n");
                                System.Threading.Thread.Sleep(5000);
                            }
                            else if (check2 == 5)
                            {
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }
                        //
                        if (check2 == 5)
                        {
                            WriteLog_System("[매도 조건식 중단/수신실패] : 재부팅 요망\n");
                            telegram_message("[매도 조건식 중단/수신실패] : 재부팅 요망\n");
                            return;
                        }                                          //
                        int result = CssWatchStgControl.BlockRequest();
                        //
                        if (result == 0)
                        {
                            int result_type = Convert.ToInt32(CssWatchStgControl.GetHeaderValue(0));
                            if (result_type == 0)
                            {
                                WriteLog_System($"[실시간매도조건/{tmp[1]}] : 초기상태 \n");
                                telegram_message($"[실시간매도조건/{tmp[1]}] : 초기상태 \n");
                            }
                            else if (result_type == 1)
                            {
                                WriteLog_System($"[실시간매도조건/{tmp[1]}] : 감시중 \n");
                                telegram_message($"[실시간매도조건/{tmp[1]}] : 감시중 \n");
                            }
                            else if (result_type == 2)
                            {
                                WriteLog_System($"[실시간매도조건/{tmp[1]}] : 감시중단 \n");
                                telegram_message($"[실시간매도조건/{tmp[1]}] : 감시중단 \n");
                            }
                            else
                            {
                                WriteLog_System($"[실시간매도조건/{tmp[1]}] : 등록취소 \n");
                                telegram_message($"[실시간매도조건/{tmp[1]}] : 등록취소 \n");
                            }
                        }
                    }

                    //기존 항목 비우기
                    Condition_Profile2.Clear();
                }
            }

            //완전 전체 중단
            if (real_price_all_stop)
            {

                //계좌탐색중단
                timer2.Stop();

                //실시간 편출입 중단
                CssAlert.Unsubscribe();

                //
                if (minuteTimer != null)
                {
                    minuteTimer.Stop();
                    minuteTimer.Dispose();
                    minuteTimer = null;
                }
                //
                if (dtCondStock.Rows.Count > 0)
                {
                    foreach (DataRow row in dtCondStock.Rows)
                    {
                        StockCur.SetInputValue(0, row["종목코드"].ToString());
                        StockCur.Unsubscribe();
                    }
                }
                WriteLog_System("[실시간시세/중단]\n");
                telegram_message("[실시간시세/중단]\n");
            }
        }

        //--------------------------------------Telegram Function-------------------------------------------------------------

        private void telegram_function(string message)
        {
            switch (message)
            {
                case "/HELP":
                    telegram_message("[명령어 리스트]\n/HELP : 명령어 리스트\n/REBOOT : 프로그램 재실행\n/SHUTDOWN : 프로그램 종료\n" +
                        "/START : 조건식 시작\n/STOP : 조건식 중단\n/CLEAR : 전체 청산\n/CLEAR_PLUS : 수익 청산\n/CLEAR_MINUS : 손실 청산\n" +
                        "/L1 : 시스템 로그\n/L2 : 주문 로그\n/L3 : 편출입 로그\n" +
                        "/T1 : 편출입 차트\n/T2 : 보유 차트\n/T3 : 매매내역 차트\n");
                    break;
                case "/REBOOT":
                    telegram_message("프로그램 재실행\n");
                    Application.Restart();
                    break;
                case "/SHUTDOWN":
                    telegram_message("프로그램 종료\n");
                    Application.Exit();
                    break;
                case "/START":
                    telegram_message("조건식 실시간 검색 시작\n");
                    real_time_search_btn(this, EventArgs.Empty);
                    break;
                case "/STOP":
                    telegram_message("조건식 실시간 검색 중단\n");
                    real_time_stop_btn(this, EventArgs.Empty);
                    break;
                case "/CLEAR":
                    telegram_message("전체 청산 실행\n");
                    All_clear_btn_Click(this, EventArgs.Empty);
                    break;
                case "/CLEAR_PLUS":
                    telegram_message("수익 청산 실행\n");
                    Profit_clear_btn_Click(this, EventArgs.Empty);
                    break;
                case "/CLEAR_MINUS":
                    telegram_message("손실 청산 실행\n");
                    Loss_clear_btn_Click(this, EventArgs.Empty);
                    break;
                case "/L1":
                    telegram_message("시스템 로그 수신\n");
                    telegram_message($"\n{log_window.Text}\n");
                    break;
                case "/L2":
                    telegram_message("주문 로그 수신\n");
                    telegram_message($"\n{log_window3.Text}\n");
                    break;
                case "/L3":
                    telegram_message("편출입 로그 수신\n");
                    telegram_message($"\n{log_window2.Text}\n");
                    break;
                case "/T1":
                    telegram_message("편출입 차트 수신\n");
                    //
                    string send_meesage = string.Join("/", dtCondStock.Columns.Cast<DataColumn>().Select(column => column.ColumnName)) + "\n";
                    foreach (DataRow row in dtCondStock.Rows)
                    {
                        send_meesage += "---------------------\n";
                        send_meesage += string.Join("/", row.ItemArray.Select(item => item.ToString())) +"\n";
                    }
                    send_meesage += "---------------------\n";
                    //
                    telegram_message($"\n{send_meesage}\n");
                    break;
                case "/T2":
                    telegram_message("보유 차트 수신\n");
                    //
                    string send_meesage2 = string.Join("/", dtCondStock_hold.Columns.Cast<DataColumn>().Select(column => column.ColumnName)) + "\n";
                    foreach (DataRow row in dtCondStock_hold.Rows)
                    {
                        send_meesage2+= "---------------------\n";
                        send_meesage2 += string.Join("/", row.ItemArray.Select(item => item.ToString())) + "\n";
                    }
                    send_meesage2 += "---------------------\n";
                    //
                    telegram_message($"\n{send_meesage2}\n");
                    break;
                case "/T3":
                    telegram_message("매매내역 차트 수신\n");
                    //
                    string send_meesage3 = string.Join("/", dtCondStock_Transaction.Columns.Cast<DataColumn>().Select(column => column.ColumnName)) + "\n";
                    foreach (DataRow row in dtCondStock_Transaction.Rows)
                    {
                        send_meesage3 += "---------------------\n";
                        send_meesage3 += string.Join("/", row.ItemArray.Select(item => item.ToString())) + "\n";
                    }
                    send_meesage3 += "---------------------\n";
                    //
                    telegram_message($"\n{send_meesage3}\n");
                    break;
                default:
                    telegram_message("명령어 없음 : 명령어 리스트(/F) 요청\n");
                    break;

            }
        }

        //--------------------------------------WEBHOK-------------------------------------------------------------

    }
}
