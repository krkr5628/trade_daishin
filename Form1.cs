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
//
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Principal;
//
using CPUTILLib; //Daishin
using CPTRADELib; //Daishin

namespace WindowsFormsApp1
{
    public partial class Trade_Auto_Daishin : Form
    {
        //-----------------------------------공용 신호----------------------------------------

        public static string[] arrCondition = { };
        public static string[] account;
        public int login_check = 1;
        private bool isRunned = false;

        //-----------------------------------공통 Obj----------------------------------------

        private CPUTILLib.CpCybos CpCybos; //?
        private CPUTILLib.CpStockCode CpStockCode; //?
        private CPUTILLib.CpCodeMgr CpCodeMgr; //?
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

        //-----------------------------------전용 신호j----------------------------------------

        private string Master_code = "01";
        //private string Master_code = "10"; //TEST용
        private string ISA_code = "11";
        private bool Checked_Trade_Init; //?
        private string Master_Condition = "";
        private string ISA_Condition = "";

        //-----------------------------------------------Main------------------------------------------------
        public Trade_Auto_Daishin()
        {
            InitializeComponent();

            //-------------------초기 동작-------------------

            //테이블 초기 세팅
            initial_Table();

            //기존 세팅 로드
            utility.setting_load_auto();

            //초기 선언
            initial_load();

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
        }

        //-----------------------------------storage----------------------------------------

        //telegram용 초당 1회 전송 저장소
        private Queue<String> telegram_chat = new Queue<string>();

        //실시간 조건 검색 용 테이블(누적 저장)
        private DataTable dtCondStock = new DataTable();

        //실시간 계좌 보유 현황 용 테이블(누적 저장)
        private DataTable dtCondStock_hold = new DataTable();

        //
        private DataTable dtCondStock_Transaction = new DataTable();

        //-----------------------------------lock---------------------------------------- 
        // 락 객체 생성
        private static object buy_lock = new object();
        private static object sell_lock = new object();

        private static Dictionary<string, bool> buy_runningCodes = new Dictionary<string, bool>();
        private static Dictionary<string, bool> sell_runningCodes = new Dictionary<string, bool>();

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

        //로그창(System)
        private void WriteLog_System(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            log_window.AppendText($@"{"[" + time + "] " + message}");
            log_full.Add($"[{time}][System] : {message}");
        }

        //로그창(Order)
        private void WriteLog_Order(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            log_window3.AppendText($@"{"[" + time + "] " + message}");
            log_full.Add($"[[{time}][Order] : {message}");
            log_trade.Add($"[{time}][Order] : {message}");
        }

        //로그창(Stock)
        private void WriteLog_Stock(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            log_window2.AppendText($@"{"[" + time + "] " + message}");
            log_full.Add($"[{time}][Stock] : {message}");
        }

        //매매로그 맟 전체로그 저장
        private List<string> log_trade = new List<string>();
        private List<string> log_full = new List<string>();

        //FORM CLOSED 후 LOG 저장
        //Process.Kill()에서 비정상 작동할 가능성 높음
        private void Form_FormClosed(object sender, FormClosedEventArgs e)
        {

            string formattedDate = DateTime.Now.ToString("yyyyMMdd");

            // 저장할 파일 경로
            string filePath = $@"C:\Auto_Trade\Auto_Trade_Creon\Log\{formattedDate}_full.txt";
            string filePath2 = $@"C:\Auto_Trade\Auto_Trade_Creon\Log_Trade\{formattedDate}_trade.txt";

            // StreamWriter를 사용하여 파일 저장
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.Write(String.Join("",log_full));
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 저장 중 오류 발생: " + ex.Message);
            }
        }

        //telegram_chat
        private void telegram_message(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string message_edtied = "[" + time + "] " + message;
            telegram_chat.Enqueue(message_edtied);
        }

        //telegram_send(초당 1개씩 전송)
        private async void telegram_send(string message)
        {
            string urlString = $"https://api.telegram.org/bot{utility.telegram_token}/sendMessage?chat_id={utility.telegram_user_id}&text={message}";

            WebRequest request = WebRequest.Create(urlString);
            request.Timeout = 60000; // 60초로 Timeout 설정

            //await은 비동기 작업이 완료될떄까지 기다린다.
            //using 문은 IDisposable 인터페이스를 구현한 객체의 리소스를 안전하게 해제하는 데 사용
            using (WebResponse response = await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string responseString = await reader.ReadToEndAsync();
            }
        }

        //일련번호 받기
        //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=284&seq=239&page=1&searchString=CssWatchStgSubscribe&p=8841&v=8643&m=9505
        private int condition_sub_code(string condition_code)
        {
            CssWatchStgSubscribe.SetInputValue(0, condition_code);
            //
            int result = CssWatchStgSubscribe.BlockRequest();
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

        //-----------------------------------------initial-------------------------------------

        //초기 Table 값 입력
        private void initial_Table()
        {
            DataTable dataTable = new DataTable();
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
            dtCondStock = dataTable;
            dataGridView1.DataSource = dtCondStock;

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

        }

        //초기 설정 변수
        private string sell_condtion_method_after;

        //초기 설정 반영
        public async Task initial_allow()
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
            User_money.Text = "0"; 
            User_money_isa.Text = "0";
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
            WriteLog_System("세팅 반영 완료\n");
            telegram_message("세팅 반영 완료\n");
        }

        //초기선언
        private void initial_load()
        {
            CpCybos = new CPUTILLib.CpCybos();
            CpTdUtil = new CPTRADELib.CpTdUtil();
            cpFuture = new CPUTILLib.CpFutureCode(); //코스피 선물옵션
            cpKFuture = new CPUTILLib.CpKFutureCode(); //코스닥선물옵션
            FutOptChart = new CPSYSDIBLib.FutOptChart(); //선물옵션차트
            CpUsCode = new CPUTILLib.CpUsCode(); //해외지수
            CpTd6033 = new CPTRADELib.CpTd6033(); //계좌별 D+2 예수금 현황
            CpTdNew5331B = new  CPTRADELib.CpTdNew5331B();//계좌별 매도 가능 수량
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
        }

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
            if (arrCondition.Length == 0)
            {
                MessageBox.Show("조건식 로딩중");
                return;
            }
            Update newform2 = new Update();
            newform2.ShowDialog(); //form2 닫기 전까지 form1 UI 제어 불가능
        }

        //종목 조회 실행
        private void stock_search_btn(object sender, EventArgs e)
        {
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
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
            auto_allow();
        }

        //조건식 실시간 중단 버튼
        private void real_time_stop_btn(object sender, EventArgs e)
        {
            real_time_stop(true);
        }

        //전체 청산 버튼
        private void All_clear_btn_Click(object sender, EventArgs e)
        {
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }

            if (dtCondStock.Rows.Count > 0)
            {
                foreach (DataRow row in dtCondStock.Rows)
                {
                    if (row["상태"].ToString() == "매수완료")
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
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }

            if (dtCondStock.Rows.Count > 0)
            {
                foreach (DataRow row in dtCondStock.Rows)
                {
                    double percent_edit = double.Parse(row.Field<string>("수익률").TrimEnd('%'));
                    if (row["상태"].ToString() == "매수완료" && percent_edit >= 0)
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
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }

            if (dtCondStock.Rows.Count > 0)
            {
                foreach (DataRow row in dtCondStock.Rows)
                {
                    double percent_edit = double.Parse(row.Field<string>("수익률").TrimEnd('%'));
                    if (row["상태"].ToString() == "매수완료" && percent_edit < 0)
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

        //------------------------------------Main_Start---------------------------------

        //timer1(1000ms) : 주기 고정
        private void ClockEvent(object sender, EventArgs e)
        {
            //시간표시
            timetimer.Text = DateTime.Now.ToString("yy MM-dd (ddd) HH:mm:ss");

            //Telegram 전송
            if (utility.load_check && utility.Telegram_Allow && telegram_chat.Count > 0)
            {
                telegram_send(telegram_chat.Dequeue());
            }

            if (utility.load_check) Opeartion_Time();

        }

        //운영시간 확인
        private async void Opeartion_Time()
        {
            //운영시간 확인
            DateTime t_now = DateTime.Now;
            DateTime t_start = DateTime.Parse(utility.market_start_time);
            DateTime t_end = DateTime.Parse(utility.market_end_time);

            //운영시간 아님
            if (!isRunned && t_now >= t_start && t_now <= t_end)
            {
                isRunned = true;
                //초기 설정 반영
                await initial_allow();

                //로그인
                this.Invoke((MethodInvoker)delegate
                {
                    Initial_Daishin();
                });
            }
            else if (isRunned && t_now > t_end)
            {
                isRunned = false;
                real_time_stop(true);
            }
        }

        //------------------------------------Login---------------------------------

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
                WriteLog_System("관리자 권한 실행 요망");
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
                initial_process();
            }
        }

        //------------------------------------Login이후 동작---------------------------------

        private bool hold_update_initial = true;

        private void initial_process()
        {
            //계좌 번호
            Account();
            //D+2 예수금 + 계좌 보유 종목
            GetCashInfo_Seperate();
            Hold_Update();
            //
            hold_update_initial = false;
            //매도실현손익(제세금, 수수료 포함)
            today_profit_tax_load_seperate();
            //매매내역
            Transaction_Detail_seperate("", "");
            //지수
            Index_load();
            //
            Condition_load(); //조건식 로드
            //
            CssAlert.Subscribe(); //실시간 편출입 받기
            CpConclusion.Subscribe(); //실시간 체결 등록
            //
            timer2.Start(); //체결 내역 업데이트 - 100ms
            //
            timer3.Start(); //편입 종목 감시 - 200ms
        }

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
        private void GetCashInfo_Seperate()
        {
            GetCashInfo(Master_code);
            if(utility.buy_DUAL) GetCashInfo(ISA_code);
        }

        private void GetCashInfo(string acc_gubun)
        {
            if (TradeInit())
            {
                CpTd6033.SetInputValue(0, acc_text.Text);
                CpTd6033.SetInputValue(1, acc_gubun);
                CpTd6033.SetInputValue(2, 14);
                CpTd6033.SetInputValue(3, "1");
                //
                int result = CpTd6033.BlockRequest();
                //
                if (result == 0)
                {
                    //예수금 받기
                    string day2money = string.Format("{0:#,##0}", Convert.ToDecimal(CpTd6033.GetHeaderValue(9).ToString().Trim()));
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

                    //보유계좌 테이블 반영
                    dtCondStock_hold.AcceptChanges();
                    dataGridView2.DataSource = dtCondStock_hold;

                    //1회성 업데이트 : 초기 보유 계좌 + 고정 예수금
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

            }
        }

        //체결내역업데이트(주문번호) => 매매내역 정보
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=291&seq=174&page=2&searchString=&p=&v=&m=
        private void Transaction_Detail_seperate(string order_number, string trade)
        {
            Transaction_Detail(order_number, Master_code, trade);
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

            Transaction_Detail_Check(order_number, gubun, trade);
        }

        private void Transaction_Detail_Check(string order_number, string gubun, string trade) 
        {
            //수신확인
            if (CpTd6033.GetDibStatus() == 1)
            {
                WriteLog_System("DibRq 요청 수신대기 : 체결내역업데이트(주문번호)");

                System.Threading.Thread.Sleep(1000);
                Transaction_Detail_Check(order_number, gubun, trade);
                return;
            }
            Transaction_Detail_Request(order_number, gubun, trade);
        }

        private void Transaction_Detail_Request(string order_number, string gubun, string trade)
        {
            //정상조회확인
            int result = CpTd5341.BlockRequest();
            //
            if (result == 0)
            {
                for (int i = 0; i < Convert.ToInt32(CpTd5341.GetHeaderValue(6)); i++)
                {
                    string transaction_number = Convert.ToString(CpTd5341.GetDataValue(1, i)).Trim(); //주문번호 => long
                    string average_price = string.Format("{0:#,##0}", Convert.ToDecimal(CpTd5341.GetDataValue(11, i))); // 체결단가 => long
                    string gubun2 = CpTd5341.GetDataValue(35, i) == "1" ? "매도" : "매수"; //매매구분(매도,매수) => string
                    string order_sum = Convert.ToString(CpTd5341.GetDataValue(9, i)); //총체결수량 => long

                    //매수완료 후 실제 편입가 업데이트
                    if (transaction_number.Equals(order_number))
                    {
                        var findRows2 = dtCondStock.AsEnumerable().Where(row => row.Field<string>("주문번호") == order_number);
                        if (findRows2.Any())
                        {
                            DataRow row = findRows2.First();
                            if (trade.Equals("매수"))
                            {
                                row["편입상태"] = "실매입";
                                row["편입가"] = average_price;
                                //
                                WriteLog_Order($"[매수주문/정상완료/{gubun}] : {row["종목명"]}({row["종목코드"]}) {order_sum}개 {average_price}원\n");
                                telegram_message($"[매수주문/정상완료/{gubun}] : {row["종목명"]}({row["종목코드"]}) {order_sum}개 {average_price}원\n");
                            }
                            else{
                                row["매도가"] = average_price;
                                //
                                WriteLog_Order($"[매도주문/정상완료/{gubun}] : {row["종목명"]}({row["종목코드"]}) {order_sum}개 {average_price}원\n");
                                telegram_message($"[매도주문/정상완료/{gubun}] : {row["종목명"]}({row["종목코드"]}) {order_sum}개 {average_price}원\n");
                            }
                        }
                        //테이블 반영
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;
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
                dtCondStock_Transaction.AcceptChanges();
                dataGridView3.DataSource = dtCondStock_Transaction;
            }
        }

        //------------------------------------인덱스 목록 받기---------------------------------        
        private void Index_load()
        {
            US_INDEX();
            if(utility.kospi_commodity || utility.kosdak_commodity)
            {
                Initial_kor_index();
            }
        }

        private bool index_buy = false;
        private bool index_clear = false;
        private bool index_dual = false;

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
                int result = CpFore8312.BlockRequest();
                //
                if (result == 0)
                {
                    //string tmp = CpFore8312.GetHeaderValue(0);//해외지수코드 => string
                    //string tmp3 = CpFore8312.GetHeaderValue(3);//심볼명 => string
                    //float tmp4 = CpFore8312.GetHeaderValue(4);//현재가 => long
                    float tmp5 = CpFore8312.GetHeaderValue(6);//등락률 => float
                    //int tmp6 = CpFore8312.GetHeaderValue(13);//거래일자 => long
                    //WriteLog_System($"{tmp}/{tmp3}/{tmp4.ToString()}/{tmp5.ToString()}/{tmp6.ToString()}\n");

                    dow_index.Text = tmp5.ToString();

                    if (utility.buy_condition_index)
                    {
                        if (utility.type3_selection)
                        {
                            double start = Convert.ToDouble(utility.type3_start);
                            double end = Convert.ToDouble(utility.type3_end);
                            if(tmp5 < start || end < tmp5)
                            {
                                index_buy = true;
                                WriteLog_System($"[BUY] OVER DOW30 INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[BUY] OVER DOW30 INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (utility.clear_index)
                    {
                        if (utility.type3_selection_all)
                        {
                            double start = Convert.ToDouble(utility.type3_start_all);
                            double end = Convert.ToDouble(utility.type3_end_all);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_clear = true;
                                WriteLog_System($"[CLEAR] OVER DOW30 INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[CLEAR] OVER DOW30 INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (utility.Dual_Index)
                    {
                        if (utility.type3_selection_isa)
                        {
                            double start = Convert.ToDouble(utility.type3_start_isa);
                            double end = Convert.ToDouble(utility.type3_end_isa);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_dual = true;
                                WriteLog_System($"[DUAL] OVER DOW30 INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[DUAL]OVER DOW30 INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }
                }
            }

            //S&P500
            if (utility.sp_index)
            {
                CpFore8312.SetInputValue(0, "SPX");
                CpFore8312.SetInputValue(1, '2');
                CpFore8312.SetInputValue(2, 2);
                //
                int result2 = CpFore8312.BlockRequest();
                //
                if (result2 == 0)
                {
                    //string tmp = CpFore8312.GetHeaderValue(0);//해외지수코드 => string
                    //string tmp3 = CpFore8312.GetHeaderValue(3);//심볼명 => string
                    //float tmp4 = CpFore8312.GetHeaderValue(4);//현재가 => long
                    float tmp5 = CpFore8312.GetHeaderValue(6);//등락률 => float
                    //int tmp6 = CpFore8312.GetHeaderValue(13);//거래일자 => long
                    //WriteLog_System($"{tmp}/{tmp3}/{tmp4.ToString()}/{tmp5.ToString()}/{tmp6.ToString()}\n");

                    sp_index.Text = tmp5.ToString();

                    if (utility.buy_condition_index)
                    {
                        if (utility.type4_selection)
                        {
                            double start = Convert.ToDouble(utility.type4_start);
                            double end = Convert.ToDouble(utility.type4_end);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_buy = true;
                                WriteLog_System($"[BUY] OVER S&P500 INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[BUY] OVER S&P500 INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (utility.clear_index)
                    {
                        if (utility.type4_selection_all)
                        {
                            double start = Convert.ToDouble(utility.type4_start_all);
                            double end = Convert.ToDouble(utility.type4_end_all);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_clear = true;
                                WriteLog_System($"[CLEAR] OVER S&P500 INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[CLEAR] OVER S&P500 INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (utility.Dual_Index)
                    {
                        if (utility.type4_selection_isa)
                        {
                            double start = Convert.ToDouble(utility.type4_start_isa);
                            double end = Convert.ToDouble(utility.type4_end_isa);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_dual = true;
                                WriteLog_System($"[DUAL] OVER S&P500 INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[DUAL] OVER S&P500 INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }
                }
            }

            //NASDAQ100
            if (utility.nasdaq_index)
            {
                CpFore8312.SetInputValue(0, "COMP");
                CpFore8312.SetInputValue(1, '2');
                CpFore8312.SetInputValue(2, 2);
                //
                int result3 = CpFore8312.BlockRequest();
                //
                if (result3 == 0)
                {
                    //string tmp = CpFore8312.GetHeaderValue(0);//해외지수코드 => string
                    //string tmp3 = CpFore8312.GetHeaderValue(3);//심볼명 => string
                    //float tmp4 = CpFore8312.GetHeaderValue(4);//현재가 => long
                    float tmp5 = CpFore8312.GetHeaderValue(6);//등락률 => float
                    //int tmp6 = CpFore8312.GetHeaderValue(13);//거래일자 => long
                    //WriteLog_System($"{tmp}/{tmp3}/{tmp4.ToString()}/{tmp5.ToString()}/{tmp6.ToString()}\n");

                    nasdaq_index.Text = tmp5.ToString();

                    if (utility.buy_condition_index)
                    {
                        if (utility.type5_selection)
                        {
                            double start = Convert.ToDouble(utility.type5_start);
                            double end = Convert.ToDouble(utility.type5_end);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_buy = true;
                                WriteLog_System($"[BUY] OVER NASDAQ INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[BUY] OVER NASDAQ INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (utility.clear_index)
                    {
                        if (utility.type5_selection_all)
                        {
                            double start = Convert.ToDouble(utility.type5_start_all);
                            double end = Convert.ToDouble(utility.type5_end_all);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_clear = true;
                                WriteLog_System($"[CLEAR] OVER NASDAQ INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[CLEAR] OVER NASDAQ INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }

                    if (utility.Dual_Index)
                    {
                        if (utility.type5_selection_isa)
                        {
                            double start = Convert.ToDouble(utility.type5_start_isa);
                            double end = Convert.ToDouble(utility.type5_end_isa);
                            if (tmp5 < start || end < tmp5)
                            {
                                index_dual = true;
                                WriteLog_System($"[DUAL] OVER NASDAQ INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                WriteLog_System("Trade Stop\n");
                                telegram_message($"[DUAL]OVER NASDAQ INDEX RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                telegram_message("Trade Stop\n");
                            }
                        }
                    }
                }
            }
        }

        private string index_time = DateTime.Now.ToString("yyyyMMdd");
        private int[] items = { 0, 1, 4, 5 }; //날짜,시간,저가,종가
        private string sCode1 = "";
        private string sCode2 = "";
        private string sKCode1 = "";
        private string sKCode2 = "";

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

        //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=284&seq=6&page=5&searchString=%ec%84%a0%eb%ac%bc&p=8841&v=8643&m=9505
        //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=287&seq=9&page=1&searchString=&p=&v=&m=
        //https://money2.creontrade.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read_Page.aspx?boardseq=284&seq=105&page=3&searchString=%ec%84%a0%eb%ac%bc&p=8841&v=8643&m=9505
        private void KOR_INDEX()
        {
            //KOSPI 200 OPTIONS

            //KOSPI 200 FUTURES
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
                    
                    //저가,종가,고가
                    kospi_index_series[0] = Math.Round((tmp6 - tmp4) / tmp4 * 100, 2); //저가
                    kospi_index_series[1] = Math.Round((tmp5 - tmp4) / tmp4 * 100, 2); //종가
                    kospi_index_series[2] = Math.Round((tmp7 - tmp4) / tmp4 * 100, 2); //고가

                    this.Invoke((MethodInvoker)delegate
                    {
                        kospi_index.Text = kospi_index_series[0] + "/" + kospi_index_series[2];
                        //WriteLog_System($"{tmp}/{tmp1}/{tmp3}/{tmp4.ToString()}/{tmp5.ToString()}/{tmp6.ToString()}/{tmp7.ToString()}\n");

                        if (utility.buy_condition_index)
                        {
                            if (utility.type1_selection)
                            {
                                double start = Convert.ToDouble(utility.type1_start);
                                double end = Convert.ToDouble(utility.type1_end);
                                if (kospi_index_series[0] < start || end < kospi_index_series[2])
                                {
                                    if (!index_buy)
                                    {
                                        WriteLog_System($"[Buy] OVER KOSPI200 Commodity RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                        WriteLog_System("Trade Stop\n");
                                        telegram_message($"[Buy] OVER KOSPI200 Commodity RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                        telegram_message("Trade Stop\n");
                                    }
                                    index_buy = true;
                                }
                            }
                        }

                        if (utility.clear_index)
                        {
                            if (utility.type1_selection_all)
                            {
                                double start = Convert.ToDouble(utility.type1_start_all);
                                double end = Convert.ToDouble(utility.type1_end_all);
                                if (kospi_index_series[0] < start || end < kospi_index_series[2])
                                {
                                    if (!index_clear)
                                    {
                                        WriteLog_System($"[CLEAR] OVER KOSPI200 Commodity RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                        WriteLog_System("Trade Stop\n");
                                        telegram_message($"[CLEAR] OVER KOSPI200 Commodity RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                        telegram_message("Trade Stop\n");
                                    }
                                    index_clear = true;
                                }
                            }
                        }

                        if (utility.Dual_Index)
                        {
                            if (utility.type1_selection_isa)
                            {
                                double start = Convert.ToDouble(utility.type1_start_isa);
                                double end = Convert.ToDouble(utility.type1_end_isa);
                                if (kospi_index_series[0] < start || end < kospi_index_series[2])
                                {
                                    if (!index_dual)
                                    {
                                        WriteLog_System($"[Dual] OVER KOSPI200 Commodity RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                        WriteLog_System("Trade Stop\n");
                                        telegram_message($"[Dual] OVER KOSPI200 Commodity RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                        telegram_message("Trade Stop\n");
                                    }
                                    index_dual = true;
                                }
                            }
                        }
                    });
                }
            }

            //KOSDAK 150 FUTURES
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

                    //저가,종가,고가
                    kosdaq_index_series[0] = Math.Round((tmp6 - tmp4) / tmp4 * 100, 2); //저가
                    kosdaq_index_series[1] = Math.Round((tmp5 - tmp4) / tmp4 * 100, 2); //종가
                    kosdaq_index_series[2] = Math.Round((tmp7 - tmp4) / tmp4 * 100, 2); //고가

                    this.Invoke((MethodInvoker)delegate
                    {
                        kosdaq_index.Text = kosdaq_index_series[0] + "/" + kosdaq_index_series[2];
                        //WriteLog_System($"{tmp}/{tmp1}/{tmp3}/{tmp4.ToString()}/{tmp5.ToString()}/{tmp6.ToString()}/{tmp7.ToString()}\n");

                        if (utility.buy_condition_index)
                        {
                            if (utility.type2_selection)
                            {
                                double start = Convert.ToDouble(utility.type2_start);
                                double end = Convert.ToDouble(utility.type2_end);
                                if (kosdaq_index_series[0] < start || end < kosdaq_index_series[2])
                                {
                                    if (!index_buy)
                                    {
                                        WriteLog_System($"[Buy] OVER KOSDAK150 Commodity RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                        WriteLog_System("Trade Stop\n");
                                        telegram_message($"[Buy] OVER KOSDAK150  Commodity RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                        telegram_message("Trade Stop\n");
                                    }
                                    index_buy = true;
                                }
                            }
                        }

                        if (utility.clear_index)
                        {
                            if (utility.type2_selection_all)
                            {
                                double start = Convert.ToDouble(utility.type2_start_all);
                                double end = Convert.ToDouble(utility.type2_end_all);
                                if (kosdaq_index_series[0] < start || end < kosdaq_index_series[2])
                                {
                                    if (!index_clear)
                                    {
                                        WriteLog_System($"[Clear] OVER KOSDAK150 Commodity RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                        WriteLog_System("Trade Stop\n");
                                        telegram_message($"[Clear] OVER KOSDAK150  Commodity RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                        telegram_message("Trade Stop\n");
                                    }
                                    index_clear = true;
                                }
                            }
                        }

                        if (utility.Dual_Index)
                        {
                            if (utility.type2_selection_isa)
                            {
                                double start = Convert.ToDouble(utility.type2_start_isa);
                                double end = Convert.ToDouble(utility.type2_end_isa);
                                if (kosdaq_index_series[0] < start || end < kosdaq_index_series[2])
                                {
                                    if (!index_dual)
                                    {
                                        WriteLog_System($"[Dual] OVER KOSDAK150 Commodity RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                        WriteLog_System("Trade Stop\n");
                                        telegram_message($"[Dual] OVER KOSDAK150  Commodity RANGE : START({start}) - END({end}) - NOW({tmp5})\n");
                                        telegram_message("Trade Stop\n");
                                    }
                                    index_dual = true;
                                }
                            }
                        }
                    });
                }
            }
        }

        //------------------------------------종목 정보 받기 및 시세 등록---------------------------------

        //종목 정보 받기(전일, 초기, 편출입)
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=285&seq=131&page=1&searchString=MarketEye&p=&v=&m=
        private void Stock_info(string condition_name, string Code, string hold_num, string order_number, string gubun)
        {
            //종목코드, 시간, 현재가, 거래량, 종목명, 상한가
            int[] items = {0, 1, 4, 10, 17, 33};
            MarketEye.SetInputValue(0, items);
            MarketEye.SetInputValue(1, Code);
            //
            int result = MarketEye.BlockRequest();
            //
            System.Threading.Thread.Sleep(200);
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
                WriteLog_Stock($"[{condition_name}/편입] : {Code_name}({Code})\n");
                //telegram_message($"[{condition_name}/편입] : {Code_name}({Code})\n");
                //
                DataRow[] findRows = dtCondStock_hold.Select($"종목코드 = '{Code}'");
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

                    //
                    lock (buy_lock)
                    {
                        if (!buy_runningCodes.ContainsKey(Code) && !utility.buy_AND)
                        {
                            Status = buy_check(Code, Code_name, string.Format("{0:#,##0}", Current_Price), time, high, false, condition_name);
                        }
                    }
                }
                //"매수중/" + real_gubun + "/" + order_number + "/" + order_acc_market;
                if (Status.StartsWith("매수중"))
                {
                    string[] tmp = Status.Split('/');
                    Status = tmp[0];
                    gubun = tmp[1];
                    order_number = tmp[2];
                    now_hold = tmp[3];
                }
                //
                dtCondStock.Rows.Add(
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
                    string.Format("{0:#,##0}", Convert.ToInt32(high)) //상한가 => long or float)
                );

                /*
                //OR 및 AND 모드에서는 중복제거
                if (!utility.buy_INDEPENDENT || !utility.buy_DUAL)
                {
                    RemoveDuplicateRows(dtCondStock, utility.buy_AND);
                }
                */

                dtCondStock.AcceptChanges();
                dataGridView1.DataSource = dtCondStock;
                
                //실시간 시세 등록
                StockCur.SetInputValue(0, Code);
                StockCur.Subscribe();
            }
            else
            {
                WriteLog_Stock($"[{condition_name}/편입/수신실패] : {Code}\n");
                int status = MarketEye.GetDibStatus();
                string status_message = MarketEye.GetDibMsg1();
                WriteLog_Stock($"[{condition_name}/편입/수신실패] : {status} / {status_message} \n");
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
                                string buyCheckResult = buy_check(code, code_name, current_price, time1, high1, false, condition);
                                if (buyCheckResult == "매수중")
                                {
                                    dtCondStock.Rows[i][statusColumnIndex] = "매수중";
                                    dtCondStock.Rows[i]["보유수량"] = "0/" + buyCheckResult.Split('/')[1];
                                }
                                else
                                {
                                    dtCondStock.Rows[i][statusColumnIndex] = "주문";
                                }
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

            //종목 확인
            DataRow[] findRows = dtCondStock.Select($"종목코드 = '{Stock_code}'");

            if (findRows.Length == 0) return;

            //신규 값 받기
            string price = Convert.ToString(StockCur.GetHeaderValue(13)); //새로운 현재가
            string amount = Convert.ToString(StockCur.GetHeaderValue(9)); //새로운 거래량
            string percent = "";

            //값 등록
            if (findRows.Length != 0)
            {
                bool ischanged = false;

                for (int i = 0; i < findRows.Length; i++)
                {
                    //신규 값 계산
                    if (!price.Equals(""))
                    {
                        double native_price = Convert.ToDouble(price);
                        double native_percent = (native_price - Convert.ToDouble(findRows[i]["편입가"].ToString().Replace(",", ""))) / Convert.ToDouble(findRows[i]["편입가"].ToString().Replace(",", "")) * 100;
                        percent = string.Format("{0:#,##0.00}%", Convert.ToDecimal(native_percent)); //새로운 수익률
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
                                sell_check_price(price.Equals("") ? findRows[i]["현재가"].ToString() : string.Format("{0:#,##0}", Convert.ToInt32(price)), percent, Convert.ToInt32(findRows[i]["보유수량"].ToString().Split('/')[0]), Convert.ToInt32(findRows[i]["편입가"].ToString().Replace(",", "")), order_num, findRows[i]["구분코드"].ToString());
                                sell_runningCodes.Remove(order_num);
                            }
                        }
                    }

                    //신규 값 빈값 확인
                    if (!price.Equals(""))
                    {
                        findRows[i]["현재가"] = string.Format("{0:#,##0}", Convert.ToInt32(price)); //새로운 현재가
                        ischanged = true;
                    }
                    if (!amount.Equals(""))
                    {
                        findRows[i]["거래량"] = string.Format("{0:#,##0}", Convert.ToInt32(amount)); //새로운 거래량
                        ischanged = true;
                    }
                    if (!percent.Equals(""))
                    {
                        findRows[i]["수익률"] = percent;
                        ischanged = true;
                    }
                }

                if (ischanged)
                {
                    //적용
                    dtCondStock.AcceptChanges();
                    dataGridView1.DataSource = dtCondStock;
                }
            }             

            DataRow[] findRows2 = dtCondStock_hold.Select($"종목코드 = '{Stock_code}'");

            //
            if (findRows2.Length != 0)
            {
                bool ischanged = false;

                //Dual 모드라면 구분코드로 인해 동일 종목에 대하여 2개 들어올 수 있음(수정 요망)
                if (!price.Equals(""))
                {
                    findRows2[0]["현재가"] = string.Format("{0:#,##0}", Convert.ToInt32(price)); //새로운 현재가
                    findRows2[0]["평가금액"] = string.Format("{0:#,##0}", Convert.ToInt32(price) * Convert.ToInt32(findRows2[0]["보유수량"].ToString().Replace(",", "")));
                    ischanged = true;
                }
                if (!percent.Equals(""))
                {
                    findRows2[0]["수익률"] = percent;
                    findRows2[0]["손익금액"] = string.Format("{0:#,##0}", Convert.ToInt32(Convert.ToInt32(findRows2[0]["평가금액"].ToString().Replace(",", "")) * Convert.ToDouble(percent.Replace("%", "")) / 100));
                    ischanged = true;
                }

                if (ischanged)
                {
                    //적용
                    dtCondStock_hold.AcceptChanges();
                    dataGridView2.DataSource = dtCondStock_hold;
                }
            }
        }

        //------------------------------------조건식 등록 절차---------------------------------

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
            //
            auto_allow_check();
        }

        //
        private void auto_allow_check()
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

            //AND 모드에서는 조건식이 2개 이상이어야 한다.
            if (utility.buy_AND && condition_length < 2)
            {
                WriteLog_System("AND 모드 조건식 2개 이상 필요\n");
                telegram_message("AND 모드 조건식 2개 이상 필요\n");
                WriteLog_System("자동 매매 정지\n");
                telegram_message("자동 매매 정지\n");
                return;
            }

            //Dual 모드에서는 조건식이 2개 이상이어야 한다.
            if (utility.buy_DUAL && condition_length != 2)
            {
                WriteLog_System("DUAL 모드 조건식 2개 필요\n");
                telegram_message("DUAL 모드 조건식 2개 필요\n");
                WriteLog_System("자동 매매 정지\n");
                telegram_message("자동 매매 정지\n");
                return;
            }

            //자동 설정 여부
            if (utility.auto_trade_allow)
            {
                auto_allow();
            }
            else
            {
                WriteLog_System("자동 매매 실행 미설정\n");
                telegram_message("자동 매매 실행 미설정\n");
            }
        }

        //초기 매매 설정
        public void auto_allow()
        {
            //DUAL 모드에서 계좌별 조건식 설정
            if (utility.buy_DUAL)
            {
                string[] tmp = utility.Fomula_list_buy_text.Split(',');
                Master_Condition = tmp[0].Split('^')[1];
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

            //자동 매도 조건식 설정 여부
            if (utility.sell_condition)
            {
                WriteLog_System("실시간 조건식 매도 시작\n");
                telegram_message("실시간 조건식 매도 시작\n");
                real_time_search(null, EventArgs.Empty);

            }
            else
            {
                WriteLog_System("자동 조건식 매도 미설정\n");
                telegram_message("자동 조건식 매도 미설정\n");
            }
        }

        //------------------------------실시간 실행 초기 시작 모음-------------------------------------

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

            System.Threading.Thread.Sleep(200);

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
                stock_initial(condition[0], condition[1]);

                //일련번호
                int condition_serial = condition_sub_code(condition[0]);

                //종목 검색 요청
                CssWatchStgControl.SetInputValue(0, condition[0]); //전략ID
                CssWatchStgControl.SetInputValue(1, condition_serial); //감시 일련번호
                CssWatchStgControl.SetInputValue(2, '1'); //감시시작

                System.Threading.Thread.Sleep(200);

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
        private void stock_initial(string condition_code, string condition_name)
        {
            CssStgFind.SetInputValue(0, condition_code); //전략ID
            CssStgFind.SetInputValue(1, 'N'); //전략ID

            System.Threading.Thread.Sleep(200);
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
                    return;
                }
                //
                WriteLog_Stock("[실시간조건식/시작/" + condition_name + "] : 초기 검색 종목 존재\n");
                telegram_message("[실시간조건식/시작/" + condition_name + "] : 초기 검색 종목 존재\n");
                //
                for (int i = 0; i < initial_num; i++)
                {
                    Stock_info(condition_name, CssStgFind.GetDataValue(0, i), "0", CssStgFind.GetDataValue(0, i), "") ;
                }
            }
        }

        //실시간 종목 편입 이탈
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=288&seq=241&page=1&searchString=CssAlert&p=&v=&m=      
        private void stock_in_out()
        {
            string Condition_ID = CssAlert.GetHeaderValue(0); // 전략ID
            var condInfo = conditionInfo.Find(f => f.Index == Condition_ID);
            string Condition_Name = condInfo.Name;
            string Stock_Code = CssAlert.GetHeaderValue(2); // 종목코드
            string gubun = CssAlert.GetHeaderValue(3).ToString();

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
                        if (findRows1.Length != 0 && findRows1[0]["상태"].Equals("매수완료"))
                        {
                            sell_check_condition(Stock_Code, findRows1[0]["현재가"].ToString(), findRows1[0]["수익률"].ToString(), time1, findRows1[0]["주문번호"].ToString(), findRows1[0]["구분코드"].ToString());
                        }
                        return;
                    }

                    //기존에 포함됬던 종목
                    if(!findRows1.Any())
                    {
                        if (dtCondStock.Rows.Count >= 30)
                        {
                            WriteLog_Stock($"[신규편입불가/{Condition_Name}/{Stock_Code}] : 최대 감시 종목(30개) 초과 \n");
                            return;
                        }

                        Stock_info(Condition_Name, Stock_Code, "0", Stock_Code, "");
                    }
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
                        }
                        else
                        {
                            if (Condition_Name.Equals(findRows1[0]["조건식"]))
                            {
                                if(findRows1[0]["편입"].Equals("이탈") && findRows1[0]["상태"].Equals("대기"))
                                {
                                    findRows1[0]["편입"] = "편입";
                                    findRows1[0]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
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

                            dtCondStock.AcceptChanges();

                            //정렬
                            var sorted_Rows = from row in dtCondStock.AsEnumerable()
                                              orderby row.Field<string>("편입시각") ascending
                                              select row;
                            dtCondStock = sorted_Rows.CopyToDataTable();
                            dtCondStock.AcceptChanges();
                            dataGridView1.DataSource = dtCondStock;
                            return;
                        }
                        
                        if(issingle)
                        {
                            if (dtCondStock.Rows.Count >= 30)
                            {
                                WriteLog_Stock($"[신규편입불가/{Condition_Name}/{Stock_Code}] : 최대 감시 종목(30개) 초과 \n");
                                return;
                            }

                            Stock_info(Condition_Name, Stock_Code, "0", Stock_Code, "");
                        }
                    }
                    else 
                    {
                        //OR과 AND의 경우 종목당 한번만 포함된다.
                        if (utility.buy_OR && findRows1[0]["편입"].Equals("이탈") && findRows1[0]["상태"].Equals("대기"))
                        {
                            findRows1[0]["편입"] = "편입";
                            findRows1[0]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
                            WriteLog_Stock($"[기존종목/재편입/{Condition_Name}] : {findRows1[0]["종목명"]}({Stock_Code})\n");

                            dtCondStock.AcceptChanges();

                            //정렬
                            var sorted_Rows = from row in dtCondStock.AsEnumerable()
                                              orderby row.Field<string>("편입시각") ascending
                                              select row;
                            dtCondStock = sorted_Rows.CopyToDataTable();
                            dtCondStock.AcceptChanges();
                            dataGridView1.DataSource = dtCondStock;
                            return;
                        }

                        //OR과 AND의 경우 종목당 한번만 포함된다.
                        if (utility.buy_AND && findRows1[0]["편입"].Equals("이탈") && findRows1[0]["상태"].Equals("호출"))
                        {
                            findRows1[0]["편입"] = "편입";
                            findRows1[0]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
                            findRows1[0]["조건식"] = Condition_Name;
                            WriteLog_Stock($"[기존종목/재편입/{Condition_Name}] : {findRows1[0]["종목명"]}({Stock_Code})\n");

                            dtCondStock.AcceptChanges();

                            //정렬
                            var sorted_Rows = from row in dtCondStock.AsEnumerable()
                                              orderby row.Field<string>("편입시각") ascending
                                              select row;
                            dtCondStock = sorted_Rows.CopyToDataTable();
                            dtCondStock.AcceptChanges();
                            dataGridView1.DataSource = dtCondStock;
                            return;
                        }

                        //OR과 AND의 경우 종목당 한번만 포함된다.
                        if (utility.buy_AND && findRows1[0]["편입"].Equals("이탈") && findRows1[0]["상태"].Equals("주문"))
                        {                   
                            findRows1[0]["편입"] = "편입";
                            findRows1[0]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
                            findRows1[0]["조건식"] = Condition_Name;
                            //
                            string code = findRows1[0]["종목코드"].ToString();
                            string code_name = findRows1[0]["종목명"].ToString();
                            string current_price = findRows1[0]["현재가"].ToString();
                            string high1 = findRows1[0]["상한가"].ToString();

                            if (!buy_runningCodes.ContainsKey(code))
                            {
                                string buyCheckResult = buy_check(code, code_name, current_price, time1, high1, false, Condition_Name);
                                if (buyCheckResult == "매수중")
                                {
                                    findRows1[0]["상태"] = "매수중";
                                }
                                else
                                {
                                    findRows1[0]["상태"] = "주문";
                                }
                            }

                            WriteLog_Stock($"[기존종목/재편입/{Condition_Name}] : {findRows1[0]["종목명"]}({Stock_Code})\n");

                            dtCondStock.AcceptChanges();

                            //정렬
                            var sorted_Rows = from row in dtCondStock.AsEnumerable()
                                              orderby row.Field<string>("편입시각") ascending
                                              select row;
                            dtCondStock = sorted_Rows.CopyToDataTable();
                            dtCondStock.AcceptChanges();
                            dataGridView1.DataSource = dtCondStock;
                            return;
                        }

                        //AND의 경우 포함된 종목이 한번 더 발견되어야 매수를 시작할 수 있다.
                        if (utility.buy_AND && findRows1[0]["편입"].Equals("편입") && findRows1[0]["상태"].Equals("호출"))
                        {
                            //
                            lock (buy_lock)
                            {                              
                                findRows1[0]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
                                string code = findRows1[0]["종목코드"].ToString();
                                string code_name = findRows1[0]["종목명"].ToString();
                                string current_price = findRows1[0]["현재가"].ToString();
                                string high1 = findRows1[0]["상한가"].ToString();

                                if (!buy_runningCodes.ContainsKey(code))
                                {
                                    string buyCheckResult = buy_check(code, code_name, current_price, time1, high1, false, Condition_Name);
                                    if (buyCheckResult == "매수중")
                                    {
                                        findRows1[0]["상태"] = "매수중";
                                    }
                                    else
                                    {
                                        findRows1[0]["상태"] = "주문";
                                    }
                                }
                                WriteLog_Stock($"[기존종목/AND편입/{Condition_Name}] : {findRows1[0]["종목명"]}({Stock_Code})\n");

                                dtCondStock.AcceptChanges();

                                //정렬
                                var sorted_Rows = from row in dtCondStock.AsEnumerable()
                                                  orderby row.Field<string>("편입시각") ascending
                                                  select row;
                                dtCondStock = sorted_Rows.CopyToDataTable();
                                dtCondStock.AcceptChanges();
                                dataGridView1.DataSource = dtCondStock;
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

                    bool isExitStock = false;
                    string logMessage = "";

                    if (utility.buy_OR)
                    {
                        isExitStock = true;
                        logMessage = $"[기존종목/이탈/{Condition_Name}] : {findRows[0]["종목명"]}({Stock_Code})\n";
                    }
                    else if (utility.buy_AND)
                    {
                        if (findRows[0]["상태"].Equals("호출"))
                        {
                            isExitStock = true;
                            logMessage = $"[기존종목/이탈/{Condition_Name}] : {findRows[0]["종목명"]}({Stock_Code}) 완전이탈 \n";
                        }
                        else if (findRows[0]["상태"].Equals("주문"))
                        {
                            findRows[0]["상태"] = "호출";
                            logMessage = $"[기존종목/이탈/{Condition_Name}] : {findRows[0]["종목명"]}({Stock_Code}) 부분이탈\n";
                        }
                    }
                    else if (utility.buy_INDEPENDENT || utility.buy_DUAL)
                    {
                        for (int i = 0; i < findRows.Length; i++)
                        {
                            if (Condition_Name.Equals(findRows[i]["조건식"]))
                            {
                                isExitStock = true;
                                logMessage = $"[기존종목/INDEPENDENT이탈/{Condition_Name}] : {findRows[i]["종목명"]}({Stock_Code})\n";
                                break;
                            }
                        }
                    }

                    if (isExitStock)
                    {
                        findRows[0]["편입"] = "이탈";
                        findRows[0]["이탈시각"] = DateTime.Now.ToString("HH:mm:ss");
                        WriteLog_Stock(logMessage);
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

                        if (findRows[0]["상태"].Equals("매도완료"))
                        {
                            StockCur.SetInputValue(0, Stock_code);
                            StockCur.Unsubscribe();
                        }
                    }

                    break;
            }
        }

        //--------------편입 이후 종목에 대한 매수 매도 감시(200ms)---------------------

        //timer3(200ms) : 09시 30분 이후 매수 시작인 것에 대하여 이전에 진입한 종목 중 편입 상태인 종목에 대한 매수
        private void Transfer_Timer(object sender, EventArgs e)
        {
            //편입 상태 이면서 대기 종목인 녀석에 대한 검증
            account_check_buy();

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

            //지수연동청산
            if (index_clear)
            {
                account_check_sell();
            }

        }

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
                            buy_check(code, row.Field<string>("종목명"), row.Field<string>("현재가").Replace(",", ""), time, row.Field<string>("상한가"), true, row.Field<string>("조건식"));
                            buy_runningCodes.Remove(code);
                        }
                    }
                }
            }
        }

        private void account_check_sell()
        {
            if (utility.clear_sell)
            {
                All_clear_btn_Click(null, EventArgs.Empty);
                return;
            }
            else if(utility.clear_sell_mode)
            {
                if (!utility.clear_sell_profit || !utility.clear_sell_loss)
                {
                    WriteLog_System("청산 모드 선택 요청\n");
                    telegram_message("청산 모드 선택 요청\n");
                    return;
                }

                //특저 열 추출
                DataColumn columnStateColumn = dtCondStock.Columns["상태"];
                
                //AsEnumerable()은 DataTable의 행을 열거형으로 변환
                var filteredRows = dtCondStock.AsEnumerable().Where(row => row.Field<string>(columnStateColumn) == "매수완료").ToList();
                
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

        //매수 가능한 상태인지 확인
        //https://money2.daishin.com/e5/mboard/ptype_basic/HTS_Plus_Helper/DW_Basic_Read.aspx?boardseq=291&seq=159&page=3&searchString=&p=&v=&m=
        private string buy_check(string code, string code_name, string price, string time, string high, bool check, string condition_name)
        {
            //계좌 구분 코드
            string gubun = Master_code;
            if (utility.buy_DUAL && condition_name.Equals(ISA_Condition))
            {
                gubun = ISA_code;
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

            //지수 확인
            if (gubun == Master_code && index_buy)
            {
                return "대기";
            }

            //지수 확인
            if (gubun == ISA_code && index_dual)
            {
                return "대기";
            }

            //매수지연(기본값 200 => 프로그램 여러 호출단에서 기본 간격 200ms가 존재하므로 기본 지연 + 입력값
            int term = 200;
            if(utility.term_for_buy) term = Convert.ToInt32(utility.term_for_buy_text);

            //기존에 포함된 종목이면 따로 변경해줘야 함
            if (check)
            {
                //편입 차트 상태 '매수중' 변경
                DataRow[] findRows = dtCondStock.Select($"종목코드 = '{code}'");
                findRows[0]["상태"] = "매수중";
                dtCondStock.AcceptChanges();
                dataGridView1.DataSource = dtCondStock;
            }

            //매수 주문(1초에 5회)
            //주문 방식 구분
            string[] order_method = buy_condtion_method.Text.Split('/');

            //매수지연
            System.Threading.Thread.Sleep(term);

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
                    return "대기";
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

                        //매매 수량이 최대치에 도달했을 경우(실시간조건식검색중단/실시간 시세는 유지)
                        if (trade_status_already_update + 1 == trade_status_limit_update)
                        {
                            real_time_stop(false);
                        }
                    }

                    return "매수중/" + real_gubun + "/" + order_number + "/" + order_acc_market;

                }
                else
                {
                    WriteLog_Order($"[매수주문/시장가/주문실패/{gubun}] : " + code_name + "(" + code + ") " + "에러코드(" + error_message(error) + ")\n");
                    telegram_message($"[매수주문/시장가/주문실패/{gubun}] : " + code_name + "(" + code + ") " + "에러코드(" + error_message(error) + ")\n");

                    if (check)
                    {
                        //편입 차트 상태 '매수중' 변경
                        DataRow[] findRows = dtCondStock.Select($"종목코드 = '{code}'");
                        findRows[0]["상태"] = "대기";
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;
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
                int error = CpTd0311.BlockRequest();
  
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

                        //매매 수량이 최대치에 도달했을 경우(실시간조건식검색중단/실시간 시세는 유지)
                        if (trade_status_already_update + 1 == trade_status_limit_update)
                        {
                            real_time_stop(false);
                        }
                    }

                    return "매수중/" + real_gubun + "/" + order_number + "/" + order_acc;

                }
                else
                {
                    WriteLog_Order($"[매수주문/지정가매수/주문실패/{gubun}] : " + code_name + "(" + code + ") " + "에러코드(" + error_message(error) + "\n");
                    telegram_message($"[매수주문/지정가매수/주문실패/{gubun}] : " + code_name + "(" + code + ") " + "에러코드(" + error_message(error) + "\n");

                    if (check)
                    {
                        //편입 차트 상태 '매수중' 변경
                        DataRow[] findRows = dtCondStock.Select($"종목코드 = '{code}'");
                        findRows[0]["상태"] = "대기";
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;
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
                current_balance = Convert.ToInt32(User_money.Text.Replace(",", ""));
            }
            else
            {
                current_balance = Convert.ToInt32(User_money_isa.Text.Replace(",", ""));
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

        //--------------실시간 매도 조건 확인---------------------

        //조건식 매도(대기)
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
        private void sell_check_price(string price, string percent, int hold, int buy_price, string order_num, string gubun)
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

            //익절TS(대기)
            if (utility.profit_ts)
            {
                sell_order(price, "익절TS", order_num, percent, gubun);
                return;
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

        //--------------실시간 매도 주문---------------------

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

                row["상태"] = "매도중";

                //보유수량계산
                string[] tmp = row["보유수량"].ToString().Split('/');
                int order_acc = Convert.ToInt32(tmp[0]);

                //주문 방식 구분
                string[] order_method = buy_condtion_method.Text.Split('/');

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

                //매도지연(기본값 200 => 프로그램 여러 호출단에서 기본 간격 200ms가 존재하므로 기본 지연 + 입력값
                int term = 200;
                if (utility.term_for_sell) term = Convert.ToInt32(utility.term_for_sell_text);

                WriteLog_Order($"[{sell_message}/주문접수/{gubun}] : {code_name}({code}) {order_acc}개 {percent}\n");
                telegram_message($"[{sell_message}/주문접수/{gubun}] : {code_name}({code}) {order_acc}개 {percent}\n");

                //매도지연
                System.Threading.Thread.Sleep(term);

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

                    int error = CpTd0322.BlockRequest();

                    if (error == 0)
                    {

                        row["주문번호"] = Convert.ToString(CpTd0322.GetHeaderValue(5));
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

                        WriteLog_Order($"[{sell_message}/시간외종가/주문성공/{gubun}] : {code_name}({code}) {order_acc}개\n");
                        WriteLog_Order($"[{sell_message}/시간외종가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                        telegram_message($"[{sell_message}/시간외종가//주문성공/{gubun}] : {code_name}({code}) {order_acc}개 {percent}\n");
                        telegram_message($"[{sell_message}/시간외종가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                    }
                    else
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

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

                    int error = CpTd0386.BlockRequest();

                    if (error == 0)
                    {

                        row["주문번호"] = Convert.ToString(CpTd0386.GetHeaderValue(5));
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

                        WriteLog_Order($"[{sell_message}/시간외단일가/주문성공/{gubun}] : {code_name}({code}) {order_acc}개\n");
                        WriteLog_Order($"[{sell_message}/시간외단일가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                        telegram_message($"[{sell_message}/시간외단일가//주문성공/{gubun}] : {code_name}({code}) {order_acc}개 {percent}\n");
                        telegram_message($"[{sell_message}/시간외단일가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                    }
                    else
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

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
                    
                    int error = CpTd0311.BlockRequest();

                    if (error == 0)
                    {

                        row["주문번호"] = Convert.ToString(CpTd0311.GetHeaderValue(8));
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

                        WriteLog_Order($"[{sell_message}/시장가/주문성공/{gubun}] : {code_name}({code}) {order_acc}개\n");
                        WriteLog_Order($"[{sell_message}/시장가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                        telegram_message($"[{sell_message}/시장가//주문성공/{gubun}] : {code_name}({code}) {order_acc}개 {percent}\n");
                        telegram_message($"[{sell_message}/시장가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                    }
                    else
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

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

                    int error = CpTd0311.BlockRequest();

                    if (error == 0)
                    {

                        row["주문번호"] = Convert.ToString(CpTd0311.GetHeaderValue(8));
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

                        WriteLog_Order($"[{sell_message}/지정가/주문성공/{gubun}] : {code_name}({code}) {order_acc}개 {edited_price_hoga}원\n");
                        WriteLog_Order($"[{sell_message}/지정가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                        telegram_message($"[{sell_message}/지정가/주문성공/{gubun}] : {code_name}({code}) {order_acc}개 {edited_price_hoga}\n원 {percent}\n");
                        telegram_message($"[{sell_message}/지정가/주문상세/{gubun}] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                    }
                    else if (error == -308)
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

                        WriteLog_Order($"[{sell_message}/지정가/주문실패/{gubun}] : {code_name}({code}) 초당 5회 이상 주문 불가\n");
                        telegram_message($"[{sell_message}/지정가/주문실패/{gubun}] : {code_name}({code}) 초당 5회 이상 주문 불가\n");
                    }
                    else
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

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

            string[] tmp = { gugu, code, code_name, order_number, trade_Gubun, hold_sum, time, gubun };

            Trade_check_save.Enqueue(tmp);
        }

        //100ms 마다 값을 점검한다.
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
                    string gubun = tmp[7]; //시간

                    Trade_check_save.Dequeue();

                    string order_sum = findRows[0]["보유수량"].ToString().Split('/')[1];

                    WriteLog_Order($"[체결/{code_name}({code})/{trade_Gubun}/{gubun}] : {hold_sum}/{order_sum}\n");

                    //매수확인
                    if (trade_Gubun.Equals("매수") && order_sum == hold_sum)
                    {
                        //체결내역업데이트(주문번호)
                        dtCondStock_Transaction.Clear();
                        Transaction_Detail_seperate(order_number, "매수");

                        System.Threading.Thread.Sleep(200);

                        findRows[0]["보유수량"] = $"{hold_sum}/{order_sum}";
                        findRows[0]["상태"] = "매수완료";
                        findRows[0]["매수시각"] = time;
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

                        //매도실현손익(제세금, 수수료 포함)
                        today_profit_tax_load_seperate();

                        System.Threading.Thread.Sleep(200);

                        //D+2 예수금 + 계좌 보유 종목
                        dtCondStock_hold.Clear();
                        GetCashInfo_Seperate();

                    }
                    //매도확인
                    else if (trade_Gubun.Equals("매도") && hold_sum.Equals("0"))
                    {
                        //체결내역업데이트(주문번호)
                        dtCondStock_Transaction.Clear();
                        Transaction_Detail_seperate(order_number, "매도");

                        System.Threading.Thread.Sleep(200);

                        findRows[0]["보유수량"] = $"{hold_sum}/0";

                        //중복거래허용
                        if (!utility.duplication_deny)
                        {
                            //편입 차트 상태 '대기' 변경
                            findRows[0]["상태"] = "대기";
                            findRows[0]["매도시각"] = time;
                            dtCondStock.AcceptChanges();
                            dataGridView1.DataSource = dtCondStock;
                        }
                        //중복거래비허용
                        else
                        {
                            //code 종목 실시간 해지
                            StockCur.SetInputValue(0, code);
                            StockCur.Unsubscribe();
                            //
                            findRows[0]["상태"] = "매도완료";
                            findRows[0]["매도시각"] = time;
                            dtCondStock.AcceptChanges();
                            dataGridView1.DataSource = dtCondStock;
                        }
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

                        System.Threading.Thread.Sleep(200);

                        //보유 수량 업데이트
                        string[] hold_status = max_hoid.Text.Split('/');
                        int hold = Convert.ToInt32(hold_status[0]);
                        int hold_max = Convert.ToInt32(hold_status[1]);
                        max_hoid.Text = $"{hold - 1}/{hold_max}";

                        //당일 손익 + 당일 손일률 + 당일 수수료 업데이트
                        today_profit_tax_load_seperate();

                        //D+2 예수금 + 계좌 보유 종목
                        dtCondStock_hold.Clear();
                        GetCashInfo_Seperate();
                    }
                    else
                    {
                        //매수 미체결

                        //매도 미체결
                    }
                }
            }
        }

        //--------------------------------------조건식중단-------------------------------------------------------------

        public void real_time_stop(bool real_price_all_stop)
        {
            //실시간 중단이 선언되면 '실시간시작'이 가능해진다.
            Real_time_stop_btn.Enabled = false;
            Real_time_search_btn.Enabled = true;

            //지수 업데이트 중단
            minuteTimer.Stop();
            minuteTimer.Dispose();
            minuteTimer = null;

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
                }
            }

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
                }
            }

            //완전 전체 중단
            if (real_price_all_stop)
            {
                CssAlert.Unsubscribe(); //실시간 편출입 해지
                CpConclusion.Unsubscribe(); //실시간 체결 해지
                //
                timer3.Stop();//계좌 탐색 중단
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
    }
}
