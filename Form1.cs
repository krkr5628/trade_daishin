using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Trade_Auto : Form
    {
        //-----------------------------------공용 신호----------------------------------------

        static public string[] arrCondition = { };
        static public string[] account;
        public int login_check = 1;
        private bool isRunned = false;

        //-----------------------------------------------Main------------------------------------------------
        public Trade_Auto()
        {
            InitializeComponent();

            //-------------------초기 동작-------------------

            //테이블 초기 세팅
            initial_Table();

            //기존 세팅 로드
            utility.setting_load_auto();

            //메인 시간 동작
            timer1.Start(); //시간 표시 - 1000ms

            //-------------------로그인 이벤트 동작-------------------
            axKHOpenAPI1.OnEventConnect += onEventConnect; //로그인 상태 확인(ID,NAME,계좌번호,KEYBOARD,FIREWALL,조건식)
            axKHOpenAPI1.OnReceiveConditionVer += onReceiveConditionVer; //조건식 조회

            //-------------------버튼-------------------
            Login_btn.Click += login_btn; //로그인
            Main_menu.Click += main_menu; //메인메뉴
            Trade_setting.Click += trade_setting; //설정창
            porfoilo_btn.Click += Porfoilo_btn_Click;//매매정보
            update_agree_btn.Click += Update_agree_btn_Click;//업데이트 및 동의사항

            Stock_search_btn.Click += stock_search_btn; //종목조회

            Real_time_search_btn.Click += real_time_search_btn; //실시간 조건식 등록
            Real_time_stop_btn.Click += real_time_stop_btn; //조건식 실시간 전체 중단

            All_clear_btn.Click += All_clear_btn_Click;
            profit_clear_btn.Click += Profit_clear_btn_Click;
            loss_clear_btn.Click += Loss_clear_btn_Click;

            //----------------데이터 조회 이벤트 동작-------------------
            axKHOpenAPI1.OnReceiveTrData += onReceiveTrData; //TR조회
            axKHOpenAPI1.OnReceiveTrCondition += onReceiveTrCondition; //매도 및 실시간 조건식 종목 정보 받기
            axKHOpenAPI1.OnReceiveRealCondition += onReceiveRealCondition; //실시간 조건식 편출입 종목 받기
            axKHOpenAPI1.OnReceiveRealData += onReceiveRealData; //실시간 조건식 시세 받기
            axKHOpenAPI1.OnReceiveChejanData += onReceiveChejanData; //매매 정보 받기
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
                await Task.Run(() =>
                {
                    axKHOpenAPI1.CommConnect();
                });

                timer3.Start(); //편입 종목 감시 - 200ms

            }
            else if (isRunned && t_now > t_end)
            {
                isRunned = false;
                real_time_stop(true);
            }
        }

        //화면번호
        private int _screenNo = 1001;
        private string GetScreenNo()
        {
            //화면번호 : 조회나 주문등 필요한 기능을 요청할때 이를 구별하기 위한 키값
            //0000(혹은 0)을 제외한 임의의 네자리 숫자
            //개수가 200개로 한정, 이 개수를 넘지 않도록 관리
            //200개를 넘는 경우 조회 결과나 주문 결과에 다른 데이터가 섞이거나 원하지 않는 결과가 나타날 수 있다.
            if (_screenNo < 1200)
                _screenNo++;
            else
                _screenNo = 1001;
            return _screenNo.ToString();
        }


        //CommRqData 에러 목록
        private void GetErrorMessage(int errorcode)
        {
            switch (errorcode)
            {
                case 0:
                    WriteLog_System("정상조회\n");
                    break;
                case 200:
                    WriteLog_System("시세과부화\n");
                    break;
                case 201:
                    WriteLog_System("조회전문작성 에러\n");
                    break;
            }
        }

        //로그창(System)
        private void WriteLog_System(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            log_window.AppendText($@"{"[" + time + "] " + message}");
        }

        //로그창(Order)
        private void WriteLog_Order(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            log_window3.AppendText($@"{"[" + time + "] " + message}");
        }

        //로그창(Stock)
        private void WriteLog_Stock(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            log_window2.AppendText($@"{"[" + time + "] " + message}");
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

        //-----------------------------------------initial-------------------------------------

        //초기 Table 값 입력
        private void initial_Table()
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("편입", typeof(string)); // '편입' '이탈'
            dataTable.Columns.Add("상태", typeof(string)); // '대기' '매수중 '매수완료' '매도중' '매도완료'
            dataTable.Columns.Add("종목코드", typeof(string));
            dataTable.Columns.Add("종목명", typeof(string));
            dataTable.Columns.Add("현재가", typeof(string)); // + - 부호를 통해 매수호가인지 매도 호가인지 현재가인지 파악한다.
            dataTable.Columns.Add("등락율", typeof(string));
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
            dataTable2.Columns.Add("종목코드", typeof(string)); //고정
            dataTable2.Columns.Add("종목명", typeof(string)); //고정
            dataTable2.Columns.Add("현재가", typeof(string)); //실시간 변경
            dataTable2.Columns.Add("보유수량", typeof(string)); //고정
            dataTable2.Columns.Add("평균단가", typeof(string)); //고정
            dataTable2.Columns.Add("평가금액", typeof(string));
            dataTable2.Columns.Add("수익률", typeof(string)); //실시간 변경
            dataTable2.Columns.Add("손익금액", typeof(string));
            dataTable2.Columns.Add("매도수량", typeof(string)); //고정
            dtCondStock_hold = dataTable2;
            dataGridView2.DataSource = dtCondStock_hold;

            DataTable dataTable3 = new DataTable();
            dataTable3.Columns.Add("종목번호", typeof(string));
            dataTable3.Columns.Add("종목명", typeof(string));
            dataTable3.Columns.Add("주문시간", typeof(string));
            dataTable3.Columns.Add("주문번호", typeof(string));
            dataTable3.Columns.Add("매매구분", typeof(string));
            dataTable3.Columns.Add("주문구분", typeof(string));
            dataTable3.Columns.Add("주문수량", typeof(string));
            dataTable3.Columns.Add("체결수량", typeof(string));
            dataTable3.Columns.Add("체결단가", typeof(string));
            dtCondStock_Transaction = dataTable3;
            dataGridView3.DataSource = dtCondStock_Transaction;

        }

        //초기 설정 반영
        public async Task initial_allow()
        {
            string[] mode = { "지정가", "시장가" };
            string[] hoo = { "5호가", "4호가", "3호가", "2호가", "1호가", "현재가", "시장가", "-1호가", "-2호가", "-3호가", "-4호가", "-5호가" };

            //초기 세팅
            acc_text.Text = utility.setting_account_number;
            total_money.Text = string.Format("{0:#,##0}", Convert.ToDecimal(utility.initial_balance));
            if (utility.buy_INDEPENDENT)
            {
                maxbuy_acc.Text = string.Concat(Enumerable.Repeat("0/", utility.Fomula_list_buy_text.Split(',').Length)) + utility.maxbuy_acc;
            }
            else
            {
                maxbuy_acc.Text = "0/" + utility.maxbuy_acc;
            }
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

            //초기세팅2
            all_profit.Text = "0";
            all_profit_percent.Text = "00.00%";
            today_tax.Text = "0";
            today_profit_percent_tax.Text = "00.00%";
            today_profit_tax.Text = "0";
            today_profit_percent.Text = "00.00%";
            today_profit.Text = "0";

            //초기세팅3
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

            //갱신 주기
            string[] ms = { "200", "400", "500", "1000", "2000", "5000" };
            update_interval.Items.AddRange(ms);

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

        //
        private void onEventConnect(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnEventConnectEvent e)
        {
            login_check = e.nErrCode;
            if (login_check == 0)
            {
                // 정상 처리
                WriteLog_System("로그인 성공\n");
                telegram_message("로그인 성공\n");

                //"ACCOUNT_CNT" : 보유계좌 갯수
                //"ACCLIST" 또는 "ACCNO" : 구분자 ';', 보유계좌 목록                
                string 계좌목록 = axKHOpenAPI1.GetLoginInfo("ACCLIST").Trim();

                System.Threading.Thread.Sleep(200);

                //계좌목록은 ';'문자로 분리된 문자열
                //분리된 계좌를 ComboBox에 추가 
                account = 계좌목록.Split(';');

                //계좌번호 존재 확인
                if (!account.Contains(utility.setting_account_number))
                {
                    WriteLog_System("계좌번호 재설정 요청 및 초기화 설정\n");
                    acc_text.Text = account[0];
                }

                //사용자 id를 UserId 라벨에 추가
                string 사용자id = axKHOpenAPI1.GetLoginInfo("USER_ID");
                User_id.Text = 사용자id;

                System.Threading.Thread.Sleep(200);

                //사용자 이름을 UserName 라벨에 추가
                string 사용자이름 = axKHOpenAPI1.GetLoginInfo("USER_NAME");
                User_name.Text = 사용자이름;

                System.Threading.Thread.Sleep(200);

                //접속서버 구분(1 : 모의투자, 나머지: 실거래서버)
                string 접속서버구분 = axKHOpenAPI1.GetLoginInfo("GetServerGubun");
                if (접속서버구분.Equals("1"))
                {
                    User_connection.Text = "모의\n";
                }
                else
                {
                    User_connection.Text = "실제\n";
                }

                System.Threading.Thread.Sleep(200);

                //"KEY_BSECGB" : 키보드 보안 해지여부(0 : 정상, 1 : 해지)
                string 키보드보안 = axKHOpenAPI1.GetLoginInfo("KEY_BSECGB");
                if (키보드보안.Equals("1"))
                {
                    Keyboard_wall.Text = "정상\n";
                }
                else
                {
                    Keyboard_wall.Text = "해지\n";
                }

                System.Threading.Thread.Sleep(200);

                //"FIREW_SECGB" : 방화벽 설정여부(0 : 미설정, 1 : 설정, 2 : 해지)
                string 방화벽 = axKHOpenAPI1.GetLoginInfo("FIREW_SECGB");
                if (방화벽.Equals("0"))
                {
                    Fire_wall.Text = "미설정\n";
                }
                else if (방화벽.Equals("1"))
                {
                    Fire_wall.Text = "설정\n";
                }
                else
                {
                    Fire_wall.Text = "해지\n";
                }

                System.Threading.Thread.Sleep(200);

                //여기서 부터 계좌번호 필요 => 계좌 없을 시 어떻게 할지 설정해야 함
                //일단 존재하는 계좌로 변경해서 조회함

                //예수금 받아오기
                GetCashInfo(acc_text.Text.Trim(), "예수금상세현황");

                System.Threading.Thread.Sleep(200);

                //당일 손익 받기
                today_profit_tax_load("NAN");

                System.Threading.Thread.Sleep(200);

                //매매내역 업데이트
                Transaction_Detail("");

                System.Threading.Thread.Sleep(200);

                //조건식 검색(계좌불필요) => 계좌 보유 현황 확인 => 초기 보유 종목 테이블 업데이트 => 실시간 조건 검색 시작
                if (axKHOpenAPI1.GetConditionLoad() == 1)
                {
                    WriteLog_System("조건식 검색 성공\n");
                }
                else
                {
                    WriteLog_System("조건식 검색 실패\n");
                }

            }
            else
            {
                switch (login_check)
                {
                    case 100:
                        WriteLog_System("사용자 정보교환 실패\n");
                        telegram_message("사용자 정보교환 실패\n");
                        break;
                    case 101:
                        WriteLog_System("서버접속 실패\n");
                        telegram_message("서버접속 실패\n");
                        break;
                    case 102:
                        WriteLog_System("버전처리 실패\n");
                        telegram_message("버전처리 실패\n");
                        break;
                }
            }
        }

        //예수금 조회
        private void GetCashInfo(string acctNo, string CashType)
        {
            //SetInputValue : 계좌번호, 비밀번호입력매체구분, 조회구분
            //비밀번호입력매체구 : 기본(00), 일반조회(2). 추정조회(3)
            //CommRqData(Request Name, TR CODE, 0, 화면 번호)
            axKHOpenAPI1.SetInputValue("계좌번호", acctNo);
            axKHOpenAPI1.SetInputValue("비밀번호입력매체구분", "00");
            axKHOpenAPI1.SetInputValue("조회구분", "2");

            //CommRqData(sRQName, sTRCode, nPreNext, sScreenNo)
            //CommRqData(임의 사용자 구분명, TR목록, 연속조회여부, 화면번호)
            //CommRqData를 하는 경우 KHOpenAPI Control의 OnReceiveTrData 이벤트가 호출
            int result = axKHOpenAPI1.CommRqData(CashType, "OPW00001", 0, GetScreenNo());
            GetErrorMessage(result);
        }

        //조건식 조회(조건식이 있어야 initial 작동 / initial을 통해 계좌를 받아와야 GetCashInfo)
        class ConditionInfo
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public DateTime? LastRequestTime { get; set; }
        }

        private List<ConditionInfo> conditionInfo = new List<ConditionInfo>();

        private void onReceiveConditionVer(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveConditionVerEvent e)
        {
            if (e.lRet != 1) return;
            conditionInfo.Clear();
            //사용자 조건식을 조건식의 고유번호와 조건식 이름을 한 쌍으로 하는 문자열
            // ';' 구분
            arrCondition = axKHOpenAPI1.GetConditionNameList().Trim().Split(';');
            foreach (var cond in arrCondition)
            {
                if (string.IsNullOrEmpty(cond)) continue;
                // '^' 구분 ex) 001^조건식1
                var item = cond.Split('^');
                conditionInfo.Add(new ConditionInfo
                {
                    Index = Convert.ToInt32(item[0]),
                    Name = item[1]
                });
            }
            WriteLog_System("조건식 조회 성공\n");

            System.Threading.Thread.Sleep(200);

            //계좌 보유 현황 확인 => 초기 보유 종목 테이블 업데이트 => 실시간 조건 검색 시작
            Account_before_initial(null, EventArgs.Empty);
        }

        //계좌 보유 현황 확인 => 초기 보유 종목 테이블 업데이트 => 실시간 조건 검색 시작
        private void Account_before_initial(object sender, EventArgs e)
        {
            //계좌 보유 종목 갱신
            if (utility.load_check && login_check == 0)
            {
                axKHOpenAPI1.SetInputValue("계좌번호", acc_text.Text);
                axKHOpenAPI1.SetInputValue("상장폐지조회구분", "0");
                axKHOpenAPI1.SetInputValue("비밀번호입력매체구분", "00");
                axKHOpenAPI1.CommRqData("계좌평가현황요청/초기", "OPW00004", 0, GetScreenNo());
            }
        }

        //초기 보유 종목 테이블 업데이트 & 실시간 조건 검색 시작
        private void Hold_Update()
        {
            if (dtCondStock_hold.Rows.Count == 0)
            {
                WriteLog_Stock("기존 보유 종목 없음\n");
                telegram_message("기존 보유 종목 없음.\n");
                if (utility.max_hold)
                {
                    //최대 보유 종목 에 대한 계산
                    max_hoid.Text = "0/" + utility.max_hold_text;
                }
                else
                {
                    max_hoid.Text = "0/10";
                }

                System.Threading.Thread.Sleep(200);

                //실시간 조건 검색 시작
                auto_allow();
                return;
            }

            //
            WriteLog_Stock("기존 보유 종목 있음\n");
            telegram_message("기존 보유 종목 있음\n");

            foreach (DataRow row in dtCondStock_hold.Rows)
            {
                string Code = row["종목코드"].ToString();

                // 각 항목 처리
                axKHOpenAPI1.SetInputValue("종목코드", Code);
                axKHOpenAPI1.CommRqData("기존보유/" + row["보유수량"].ToString(), "OPT10001", 0, GetScreenNo());
                //실시간 항목 등록(대비기호, 현재가. 등락율, 거래량)
                axKHOpenAPI1.SetRealReg(GetScreenNo(), Code, "10;12;13", "1");
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

            System.Threading.Thread.Sleep(200);

            //실시간 조건 검색 시작
            auto_allow();
        }

        //초기 매매 설정
        private async Task auto_allow()
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

            //자동 설정 여부
            if (utility.auto_trade_allow)
            {
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
            else
            {
                WriteLog_System("자동 매매 실행 미설정\n");
                telegram_message("자동 매매 실행 미설정\n");
            }
        }

        //계좌 보유 현황 확인
        private void Account_before(string code)
        {
            axKHOpenAPI1.SetInputValue("계좌번호", acc_text.Text);
            axKHOpenAPI1.SetInputValue("상장폐지조회구분", "0");
            axKHOpenAPI1.SetInputValue("비밀번호입력매체구분", "00");
            axKHOpenAPI1.CommRqData("계좌평가현황요청/" + code, "OPW00004", 0, GetScreenNo());
        }

        private void today_profit_tax_load(string load_type)
        {
            //당일 손익 + 당일 손일률 + 당일 수수료
            axKHOpenAPI1.SetInputValue("계좌번호", acc_text.Text);
            axKHOpenAPI1.SetInputValue("기준일자", "");
            axKHOpenAPI1.SetInputValue("단주구분", "2");
            axKHOpenAPI1.SetInputValue("현금신용구분", "0");
            if (load_type.Equals("NAN")) load_type = "";
            int result = axKHOpenAPI1.CommRqData("당일매매일지요청/" + load_type, "OPT10170", 0, GetScreenNo());
            WriteLog_System("[당일매매일지요청/에러확인] : " + result + "\n");
        }

        //체결내역업데이트(주문번호)
        private void Transaction_Detail(string order_number)
        {
            axKHOpenAPI1.SetInputValue("주문일자", DateTime.Now.ToString("yyyyMMdd"));
            axKHOpenAPI1.SetInputValue("계좌번호", acc_text.Text);
            axKHOpenAPI1.SetInputValue("비밀번호입력매체구분", "00");
            axKHOpenAPI1.SetInputValue("조회구분", "2");
            axKHOpenAPI1.SetInputValue("주식채권구분", "0");
            axKHOpenAPI1.SetInputValue("매도수구분", "0");
            axKHOpenAPI1.SetInputValue("종목코드", "");//종목코드
            axKHOpenAPI1.SetInputValue("시작주문번호", "");//시작주문번호
            int result = axKHOpenAPI1.CommRqData("계좌별주문체결내역상세요청/" + order_number, "OPW00007", 0, GetScreenNo());
            WriteLog_System("[계좌별주문체결내역상세요청/에러확인] : " + result + "\n");
        }

        //전체 종목 업데이트

        //--------------------------------TR TABLE--------------------------------------------

        //데이터 조회(예수금, 유가증권, 조건식, 일반 검색, 실시간 검색 등)
        private void onReceiveTrData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            //
            string[] name_split = e.sRQName.Split('/');
            string split_name = name_split[0];
            string condition_nameORcode = "";
            if (name_split.Length >= 2)
            {
                condition_nameORcode = name_split[1];
            }

            switch (split_name)
            {
                //실시간 조건 검색 초기 진입
                case "조건일반검색":

                    int count = axKHOpenAPI1.GetRepeatCnt(e.sTrCode, e.sRQName);
                    string time1 = DateTime.Now.ToString("HH:mm:ss");

                    for (int i = 0; i < count; i++)
                    {
                        int current_price = Math.Abs(Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "현재가").Trim()));
                        string code = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "종목코드").Trim();
                        //최소 및 최대 매수가 확인
                        if (current_price < Convert.ToInt32(utility.min_price) || current_price > Convert.ToInt32(utility.max_price)) continue;
                        //기존 보유 종목으로 인하여 포함된게 있을 경우 이탈
                        if (dtCondStock.Select($"종목코드 = {code}").Length == 1) continue;

                        string code_name = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "종목명").Trim();
                        //
                        WriteLog_Stock("[신규종목/초기/" + condition_nameORcode + "] : " + code + "-" + code_name + "\n");
                        //
                        string high1 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "상한가").Trim();
                        string now_hold1 = "0";
                        string condition1 = utility.buy_AND ? "호출" : "대기";
                        lock (buy_lock)
                        {
                            if (!buy_runningCodes.ContainsKey(code) && !utility.buy_AND)
                            {
                                condition1 = buy_check(code, code_name, string.Format("{0:#,##0}", current_price), time1, high1, false, condition_nameORcode);
                            }
                        }
                        //
                        if (condition1.StartsWith("매수중"))
                        {
                            now_hold1 = condition1.Split('/')[1];
                            condition1 = "매수중";
                        }
                        //
                        dtCondStock.Rows.Add(
                            "편입",
                            condition1,
                            code,
                            code_name,
                            string.Format("{0:#,##0}", current_price),
                            string.Format("{0:#,##0.00}%", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "등락율").Trim())),
                            string.Format("{0:#,##0}", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량").Trim())),
                            "진입가",
                            string.Format("{0:#,##0}", current_price),
                            string.Format("{0:#,##0}", current_price),
                            "00.00%",
                            "0/" + now_hold1,
                            condition_nameORcode,
                            time1,
                            "-",
                            "-",
                            "-",
                            "-",
                            string.Format("{0:#,##0}", Convert.ToDecimal(high1))
                        );
                    }

                    //OR 및 AND 모드에서는 중복제거
                    if (!utility.buy_INDEPENDENT)
                    {
                        RemoveDuplicateRows(dtCondStock, utility.buy_AND);
                    }
                    //
                    dataGridView1.DataSource = dtCondStock;
                    break;

                //실시간 조건 검색(상태(편입, 이탈, 매수, 매도), 종목코드, 종목명, 등락표시, 현재가, 등락율, 거래량, 편입가, 편입대비, 수익률, 편입시간, 매수조건식, 매도조건식) => 상태, 종목코드, 대비기호, 현재가. 등락율, 거래량
                case "조건실시간검색":
                    int current_price2 = Math.Abs(Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "현재가").Trim()));

                    //최소 및 최대 매수가 확인
                    if (current_price2 < Convert.ToInt32(utility.min_price) || current_price2 > Convert.ToInt32(utility.max_price)) break;

                    string time2 = DateTime.Now.ToString("HH:mm:ss");
                    string code2 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "종목코드").Trim();
                    string code_name2 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "종목명").Trim();
                    //
                    WriteLog_Stock("[신규종목/편입/" + condition_nameORcode + "] : " + code2 + "-" + code_name2 + "\n");
                    //
                    string high2 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "상한가").Trim();
                    string now_hold2 = "0";
                    string condition2 = utility.buy_AND ? "호출" : "대기";
                    lock (buy_lock)
                    {
                        if (!buy_runningCodes.ContainsKey(code2) && !utility.buy_AND)
                        {
                            condition2 = buy_check(code2, code_name2, string.Format("{0:#,##0}", current_price2), time2, high2, false, condition_nameORcode);
                        }
                    }
                    //
                    if (condition2.StartsWith("매수중"))
                    {
                        now_hold2 = condition2.Split('/')[1];
                        condition2 = "매수중";
                    }
                    //
                    dtCondStock.Rows.Add(
                        "편입",
                        condition2,
                        code2,
                        code_name2,
                        string.Format("{0:#,##0}", current_price2),
                        string.Format("{0:#,##0.00}%", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "등락율").Trim())),
                        string.Format("{0:#,##0}", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "거래량").Trim())),
                        "진입가",
                        string.Format("{0:#,##0}", current_price2),
                        "-",
                        "00.00%",
                        "0/" + now_hold2,
                        condition_nameORcode,
                        time2,
                        "-",
                        "-",
                        "-",
                        "-",
                        string.Format("{0:#,##0}", Convert.ToDecimal(high2))
                    );
                    dataGridView1.DataSource = dtCondStock;
                    break;

                //기존보유
                case "기존보유":
                    int current_price3 = Math.Abs(Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "현재가").Trim()));
                    string time3 = DateTime.Now.ToString("HH:mm:ss");
                    string code3 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "종목코드").Trim();
                    string code_name3 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "종목명").Trim();
                    string high3 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "상한가").Trim();
                    //
                    DataRow[] findRows = dtCondStock_hold.Select($"종목코드 = {code3}");
                    string average_price2 = findRows[0]["평균단가"].ToString();
                    //
                    WriteLog_Stock("[기존종목/편입] : " + code3 + "-" + code_name3 + "\n");
                    telegram_message("[기존종목/편입] : " + code3 + "-" + code_name3 + "\n");
                    //
                    dtCondStock.Rows.Add(
                        "편입",
                        "매수완료",
                        code3,
                        code_name3,
                        string.Format("{0:#,##0}", current_price3),
                        string.Format("{0:#,##0.00}%", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "등락율").Trim())),
                        string.Format("{0:#,##0}", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "거래량").Trim())),
                        "실매입",
                        average_price2,
                        "-",
                        "0.00%",
                        condition_nameORcode + "/" + condition_nameORcode,
                        "전일보유",
                        time3,
                        "-",
                        "-",
                        "-",
                        code3,
                        string.Format("{0:#,##0}", Convert.ToDecimal(high3))
                    );
                    dataGridView1.DataSource = dtCondStock;
                    break;

                //기존보유
                case "조건실시간검색_수동":
                    int current_price4 = Math.Abs(Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "현재가").Trim()));
                    string time4 = DateTime.Now.ToString("HH:mm:ss");
                    string code4 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "종목코드").Trim();
                    string code_name4 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "종목명").Trim();
                    string high4 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "상한가").Trim();
                    string average_price4 = string.Format("{0:#,##0}", Convert.ToDecimal(current_price4));
                    //매수완료 후 실제 편입가 업데이트
                    DataRow[] findRows1 = dtCondStock_Transaction.Select($"주문번호 = {condition_nameORcode}");
                    if (findRows1.Length != 0)
                    {
                        average_price4 = findRows1[0]["편입가"].ToString();
                    }
                    //
                    WriteLog_Stock("[HTS_수동/편입] : " + code4 + "-" + code_name4 + "\n");
                    //
                    dtCondStock.Rows.Add(
                        "편입",
                        "매수완료",
                        code4,
                        code_name4,
                        string.Format("{0:#,##0}", current_price4),
                        string.Format("{0:#,##0.00}%", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "등락율").Trim())),
                        string.Format("{0:#,##0}", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "거래량").Trim())),
                        "실매입",
                        average_price4,
                        "-",
                        "0.00%",
                        0 + "/" + 0,
                        "HTS보유",
                        time4,
                        "-",
                        time4,
                        "-",
                        condition_nameORcode,
                        string.Format("{0:#,##0}", Convert.ToDecimal(high4))
                    );
                    dataGridView1.DataSource = dtCondStock;
                    break;

                //예수금 초기 조회
                case "예수금상세현황":

                    //
                    User_money.Text = string.Format("{0:#,##0}", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "예수금").Trim()));
                    Current_User_money.Text = string.Format("{0:#,##0}", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "예수금").Trim()));

                    //업데이트
                    all_profit.Text = string.Format("{0:#,##0}", Convert.ToDecimal(Convert.ToInt32(Current_User_money.Text.Replace(",", "")) - Convert.ToInt32(total_money.Text.Replace(",", "")))); //수익
                    all_profit_percent.Text = string.Format("{0:#,##0.00}%", Convert.ToDecimal(Convert.ToDouble(all_profit.Text.Replace(",", "")) / Convert.ToDouble(total_money.Text.Replace(",", "")) * 100)); //수익률

                    WriteLog_System("[예수금] : " + User_money.Text + "\n");
                    telegram_message("[예수금] : " + User_money.Text + "\n");
                    break;

                //예수금 추가 조회
                case "예수금상세현황추가":
                    //
                    Current_User_money.Text = string.Format("{0:#,##0}", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "예수금").Trim()));

                    //업데이트
                    all_profit.Text = string.Format("{0:#,##0}", Convert.ToDecimal(Convert.ToInt32(Current_User_money.Text.Replace(",", "")) - Convert.ToInt32(total_money.Text.Replace(",", "")))); //수익
                    all_profit_percent.Text = string.Format("{0:#,##0.00}%", Convert.ToDecimal(Convert.ToDouble(all_profit.Text.Replace(",", "")) / Convert.ToDouble(total_money.Text.Replace(",", "")) * 100)); //수익률수익률

                    //
                    WriteLog_System("[예수금] : " + User_money.Text + "\n");
                    telegram_message("[예수금] : " + User_money.Text + "\n");
                    break;

                //계좌 보유 현황 조회
                case "계좌평가현황요청":
                    DataTable dataTable2 = new DataTable();
                    dataTable2.Columns.Add("종목코드", typeof(string));
                    dataTable2.Columns.Add("종목명", typeof(string));
                    dataTable2.Columns.Add("현재가", typeof(string));
                    dataTable2.Columns.Add("보유수량", typeof(string));
                    dataTable2.Columns.Add("평균단가", typeof(string));
                    dataTable2.Columns.Add("평가금액", typeof(string));
                    dataTable2.Columns.Add("수익률", typeof(string));
                    dataTable2.Columns.Add("손익금액", typeof(string));
                    dataTable2.Columns.Add("매도수량", typeof(string));

                    int count2 = axKHOpenAPI1.GetRepeatCnt(e.sTrCode, e.sRQName);

                    for (int i = 0; i < count2; i++)
                    {
                        string code = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "종목코드").Trim().Replace("A", "");
                        string average_price = string.Format("{0:#,##0}", Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "평균단가").Trim()));
                        dataTable2.Rows.Add(
                            code,
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "종목명").Trim(),
                            string.Format("{0:#,##0}", Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "현재가").Trim())),
                            string.Format("{0:#,##0}", Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "보유수량").Trim())),
                            average_price,
                            string.Format("{0:#,##0}", Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "평가금액").Trim())),
                            string.Format("{0:#,##0.00}%", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "손익율").Trim()) / 10000),
                            string.Format("{0:#,##0}", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "손익금액").Trim())),
                            string.Format("{0:#,##0}", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "금일매도수량").Trim()))
                        );
                    }
                    dtCondStock_hold = dataTable2;
                    dataGridView2.DataSource = dtCondStock_hold;
                    if (condition_nameORcode.Equals("초기"))
                    {
                        System.Threading.Thread.Sleep(200);
                        Hold_Update();
                    }
                    break;

                //개별 증권 데이터 조회
                case "주식기본정보":
                    WriteLog_Stock("------------------------------------\n");
                    WriteLog_Stock(string.Format("종목코드: {0}\n", axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "종목코드").Trim()));
                    WriteLog_Stock(string.Format("종목명: {0}\n", axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "종목명").Trim()));
                    WriteLog_Stock(string.Format("연중최고: {0:#,##0}\n", Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "연중최고").Trim())));
                    WriteLog_Stock(string.Format("연중최저: {0:#,##0}\n", Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "연중최저").Trim())));
                    WriteLog_Stock(string.Format("PER: {0:#,##0.00}\n", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "PER").Trim())));
                    WriteLog_Stock(string.Format("EPS: {0:#,##0}\n", Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "EPS").Trim())));
                    WriteLog_Stock(string.Format("ROE: {0:#,##0.00}\n", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "ROE").Trim())));
                    WriteLog_Stock(string.Format("PBR: {0:#,##0.00}\n", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "PBR").Trim())));
                    WriteLog_Stock(string.Format("EV: {0:#,##0.00}\n", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "EV").Trim())));
                    WriteLog_Stock(string.Format("BPS: {0:#,##0}\n", Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "BPS").Trim())));
                    WriteLog_Stock(string.Format("신용비율: {0:#,##0.00}%\n", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "신용비율").Trim())));
                    WriteLog_Stock(string.Format("외인소진률: {0:#,##0.00}%\n", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "외인소진률").Trim())));
                    WriteLog_Stock(string.Format("거래량: {0:#,##0}\n", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "거래량").Trim())));
                    WriteLog_Stock("------------------------------------\n");
                    break;

                case "당일매매일지요청":
                    //실질매수 : 0.015% / 실질매도 : 0.015% + 0.18%
                    //모의매수 : 0.35% / 실질매도 : 0.35% + 0.25%
                    int sum_profit_tax = Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "총손익금액").Trim().Replace(",", "")); //세후손익
                    int sum_tax = Convert.ToInt32(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "총수수료_세금").Trim().Replace(",", "")); //세금

                    today_profit.Text = string.Format("{0:#,##0}", Convert.ToDecimal(sum_profit_tax + sum_tax)); // 당일 손익
                    today_tax.Text = string.Format("{0:#,##0}", Convert.ToDecimal(sum_tax)); // 당일 세금
                    today_profit_tax.Text = string.Format("{0:#,##0}", Convert.ToDecimal(sum_profit_tax)); // 당일 세후 손익
                    today_profit_percent.Text = string.Format("{0:#,##0.00}%", Convert.ToDecimal(Convert.ToDouble(sum_profit_tax + sum_tax) / Convert.ToDouble(User_money.Text.Replace(",", "")) * 100)); // 당일 손익률
                    today_profit_percent_tax.Text = string.Format("{0:#,##0.00}%", Convert.ToDecimal(Convert.ToDouble(sum_profit_tax) / Convert.ToDouble(User_money.Text.Replace(",", "")) * 100)); // 당일 세후 손익률
                    if (condition_nameORcode.Equals("매도"))
                    {
                        WriteLog_System("[누적세전손익] : " + today_profit.Text + " / [누적세전손익률] : " + today_profit_percent.Text + "\n");
                        WriteLog_System("[누적세후손익] : " + today_profit_tax.Text + " / [누적세후손익률] : " + today_profit_percent_tax.Text + "\n");
                        telegram_message("[누적세전손익] : " + today_profit.Text + " / [누적세전손익률] : " + today_profit_percent.Text + "\n");
                        telegram_message("[누적세후손익] : " + today_profit_tax.Text + " / [누적세후손익률] : " + today_profit_percent_tax.Text + "\n");
                    }
                    break;

                //계좌별주문체결내역상세요청
                case "계좌별주문체결내역상세요청":
                    DataTable dataTable3 = new DataTable();
                    dataTable3.Columns.Add("종목번호", typeof(string));
                    dataTable3.Columns.Add("종목명", typeof(string));
                    dataTable3.Columns.Add("주문시간", typeof(string));
                    dataTable3.Columns.Add("주문번호", typeof(string));
                    dataTable3.Columns.Add("매매구분", typeof(string));
                    dataTable3.Columns.Add("주문구분", typeof(string));
                    dataTable3.Columns.Add("주문수량", typeof(string));
                    dataTable3.Columns.Add("체결수량", typeof(string));
                    dataTable3.Columns.Add("체결단가", typeof(string));

                    int count3 = axKHOpenAPI1.GetRepeatCnt(e.sTrCode, e.sRQName);

                    for (int i = 0; i < count3; i++)
                    {
                        string transaction_number = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "주문번호").Trim();
                        string average_price = string.Format("{0:#,##0}", Convert.ToDecimal(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "체결단가").Trim().TrimStart('0') == "" ? "0" : axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "체결단가").Trim().TrimStart('0')));
                        string gubun = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "매매구분").Trim();
                        //매수완료 후 실제 편입가 업데이트
                        if (transaction_number.Equals(condition_nameORcode))
                        {
                            var findRows2 = dtCondStock.AsEnumerable().Where(row => row.Field<string>("주문번호") == condition_nameORcode);

                            if (findRows2.Any())
                            {
                                DataRow row = findRows2.First();
                                if (gubun.Equals("매수"))
                                {
                                    row["편입상태"] = "실매입";
                                    row["편입가"] = average_price;
                                }
                                else{
                                    row["매도가"] = average_price;
                                }

                            }
                            dtCondStock.AcceptChanges();
                            dataGridView1.DataSource = dtCondStock;
                        }
                        //
                        dataTable3.Rows.Add(
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "종목번호").Trim().Replace("A", ""),
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "종목명").Trim(),
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "주문시간").Trim(),
                            transaction_number,
                            gubun,
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "주문구분").Trim(),
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "주문수량").Trim().TrimStart('0'),
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "체결수량").Trim().TrimStart('0'),
                            average_price
                        );
                    }
                    dtCondStock_Transaction = dataTable3;
                    dataGridView3.DataSource = dtCondStock_Transaction;
                    break;
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

        //------------------------------기본 BUTTON 모음-------------------------------------

        //main menu 실행
        private void main_menu(object sender, EventArgs e)
        {
            MessageBox.Show("준비중입니다.");
        }

        //update
        private void login_btn(object sender, EventArgs e)
        {
            //CommConnect를 하는 경우 KHOpenAPI Control의 OnEventConnect 이벤트가 호출
            axKHOpenAPI1.CommConnect();
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
            Setting newform2 = new Setting();
            newform2.ShowDialog(); //form2 닫기 전까지 form1 제어 불가능
        }

        //매매내역 확인
        private void Porfoilo_btn_Click(object sender, EventArgs e)
        {
            MessageBox.Show("준비중입니다.");
        }

        //업데이트 및 동의사항 확인
        private void Update_agree_btn_Click(object sender, EventArgs e)
        {
            MessageBox.Show("준비중입니다.");
        }

        //종목 조회 실행
        private void stock_search_btn(object sender, EventArgs e)
        {
            if (login_check != 0)
            {
                MessageBox.Show("로그인 중입니다.");
                return;
            }

            if (axKHOpenAPI1.GetConnectState() == 0)
            {
                MessageBox.Show("Open API 연결되어 있지 않습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrEmpty(Stock_code.Text.Trim()))
            {
                MessageBox.Show("종목코드를 입력해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            WriteLog_System("[종목 조회]\n");
            SearchStockInfo(Stock_code.Text.Trim());

        }

        private void SearchStockInfo(string code)
        {
            axKHOpenAPI1.SetInputValue("종목코드", code);
            int result = axKHOpenAPI1.CommRqData("주식기본정보", "OPT10001", 0, GetScreenNo());
            GetErrorMessage(result);
        }

        private void real_time_search_btn(object sender, EventArgs e)
        {
            reload();
        }

        private async Task reload()
        {
            initial_Table();
            auto_allow();
        }

        //조건식 실시간 중단 버튼
        private void real_time_stop_btn(object sender, EventArgs e)
        {
            real_time_stop(true);
        }

        private void real_time_stop(bool real_price_all_stop)
        {
            //실시간 중단이 선언되면 '실시간시작'이 가능해진다.
            Real_time_stop_btn.Enabled = false;
            Real_time_search_btn.Enabled = true;

            //매수 조건식 중단
            if (utility.buy_condition)
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
                        string[] tmp = condition[i].Split('^');
                        axKHOpenAPI1.SendConditionStop(GetScreenNo(), tmp[1], Convert.ToInt32(tmp[0])); //조건검색 중지
                    }
                    WriteLog_System("[실시간매수조건/중단]\n");
                    telegram_message("[실시간매수조건/중단]\n");
                }
            }

            //매수 조건식 중단
            if (utility.sell_condition)
            {
                // 검색된 조건식이 없을시
                if (string.IsNullOrEmpty(utility.Fomula_list_buy_text))
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
                        string[] tmp = condition[i].Split('^');
                        axKHOpenAPI1.SendConditionStop(GetScreenNo(), tmp[1], Convert.ToInt32(tmp[0])); //조건검색 중지
                    }
                    WriteLog_System("[실시간매도조건/중단]\n");
                    telegram_message("[실시간매도조건/중단]\n");
                }
            }

            //완전 전체 중단
            if (real_price_all_stop)
            {
                axKHOpenAPI1.SetRealRemove("ALL", "ALL"); //실시간 시세 중지
                timer3.Stop();//계좌 탐색 중단
                WriteLog_System("[실시간시세/중단]\n");
                telegram_message("[실시간시세/중단]\n");
            }
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
                        sell_order(row.Field<string>("현재가"), "청산매도/일반", row.Field<string>("주문번호"), row.Field<string>("수익률"));
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
                        sell_order(row.Field<string>("현재가"), "청산매도/수익", row.Field<string>("주문번호"), row.Field<string>("수익률"));
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
                        sell_order(row.Field<string>("현재가"), "청산매도/수익", row.Field<string>("주문번호"), row.Field<string>("수익률"));
                    }
                }
            }
            else
            {
                WriteLog_Order("손실청산 종목 없음\n");
            }
        }

        //------------------------------실시간 실행 초기 시작 모음-------------------------------------

        //매도 전용 조건식 검색
        private void normal_search(object sender, EventArgs e)
        {
            //검색된 조건식이 없을시
            if (string.IsNullOrEmpty(sell_condtion.Text)) return;

            //검색된 조건식이 있을시
            string[] condition = sell_condtion.Text.Split('^');
            var condInfo = conditionInfo.Find(f => f.Index == Convert.ToInt32(condition[0]));
            if (condInfo == null) return;

            //조건식에 대한 검색은 60초 마다 가능
            if (condInfo.LastRequestTime != null && condInfo.LastRequestTime >= DateTime.Now.AddSeconds(-60))
            {
                int second = 60 - (DateTime.Now - condInfo.LastRequestTime.Value).Seconds;
                WriteLog_System($"{second}초 후에 조회 가능합니다.\n");
                return;
            }

            condInfo.LastRequestTime = DateTime.Now;

            //종목 검색 요청
            //화면 번호, 조건식 이름, 조건식 번호, 조회 구분(0은 일반 검색, 1은 실시간 검색)
            int result = axKHOpenAPI1.SendCondition(GetScreenNo(), condition[1], Convert.ToInt32(condition[0]), 0);
            if (result == 1)
                WriteLog_System("조건식 일반 검색 성공\n");
            else
                WriteLog_System("조건식 일반 검색 실패\n");
        }

        //실시간 검색(조건식 로드 후 사용가능하다)
        private void real_time_search(object sender, EventArgs e)
        {
            //실시간 검색이 시작되면 '일반 검색'이 불가능해 진다.
            Real_time_stop_btn.Enabled = true;
            Real_time_search_btn.Enabled = false;

            //조건식이 로딩되었는지
            if (string.IsNullOrEmpty(utility.Fomula_list_buy_text))
            {
                WriteLog_System("조건식 선택 요청\n");
                telegram_message("조건식 선택 요청\n");
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
                var condInfo = conditionInfo.Find(f => f.Index == Convert.ToInt32(condition[0]) && f.Name.Equals(condition[1]));

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

                //종목 검색 요청
                //화면 번호, 조건식 이름, 조건식 번호, 조회 구분(0은 일반 검색, 1은 실시간 검색)

                System.Threading.Thread.Sleep(200);

                int result = axKHOpenAPI1.SendCondition(GetScreenNo(), condition[1], Convert.ToInt32(condition[0]), 1);
                if (result != 1)
                {
                    WriteLog_System("[실시간조건식/등록실패/" + Fomula + "] : 고유번호 및 이름 확인\n");
                    telegram_message("[실시간조건식/등록실패/" + Fomula + "] : 고유번호 및 이름 확인\n");
                }
            }
        }

        //-----------------------실시간 조건 검색------------------------------

        //조건식 초기 검색(일반, 실시간)
        private void onReceiveTrCondition(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrConditionEvent e)
        {
            string code = e.strCodeList.Trim();
            if (string.IsNullOrEmpty(code))
            {
                WriteLog_Stock("[실시간조건식/시작/" + e.strConditionName + "] : 초기 검색 종목 없음\n");
                telegram_message("[실시간조건식/시작/" + e.strConditionName + "] : 초기 검색 종목 없음\n");
                return;
            }

            //
            WriteLog_System("[실시간조건식/시작/" + e.strConditionName + "] : 초기 검색 종목 존재\n");
            telegram_message("[실시간조건식/시작/" + e.strConditionName + "] : 초기 검색 종목 존재\n");

            if (code.Length > 0) code = code.Remove(code.Length - 1);
            int codeCount = code.Split(';').Length;
            //
            //종목 데이터
            //종목코드 리스트, 연속조회여부(기본값0만존재), 종목코드 갯수, 종목(0 주식, 3 선물옵션), 사용자 구분명, 화면번호
            int error = axKHOpenAPI1.CommKwRqData(code, 0, codeCount, 0, "조건일반검색/" + e.strConditionName, GetScreenNo());
            WriteLog_System("[실시간조건식/시작/" + e.strConditionName + "] : 조회결과(" + error + ")\n");
        }

        //실시간 종목 편입 이탈
        private void onReceiveRealCondition(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveRealConditionEvent e)
        {
            switch (e.strType)
            {
                //종목 편입
                case "I":
                    //
                    DataRow[] findRows1 = dtCondStock.Select($"종목코드 = {e.sTrCode}");
                    string time1 = DateTime.Now.ToString("HH:mm:ss");

                    //기존에 포함됬던 종목
                    if (findRows1.Length != 0)
                    {
                        //OR과 AND의 경우 종목당 한번만 포함된다.
                        if (utility.buy_OR && findRows1[0]["편입"].Equals("이탈") && findRows1[0]["상태"].Equals("대기"))
                        {
                            findRows1[0]["편입"] = "편입";
                            findRows1[0]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
                            WriteLog_Stock("[기존종목/재편입] : " + e.sTrCode + " - " + findRows1[0]["종목명"] + " - " + e.strConditionName + "\n");
                        }
                        //OR과 AND의 경우 종목당 한번만 포함된다.
                        if (utility.buy_AND && findRows1[0]["편입"].Equals("이탈") && findRows1[0]["상태"].Equals("호출"))
                        {
                            findRows1[0]["편입"] = "편입";
                            findRows1[0]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
                            findRows1[0]["조건식"] = e.strConditionName.Trim();
                            WriteLog_Stock("[기존종목/재편입] : " + e.sTrCode + " - " + findRows1[0]["종목명"] + " - " + e.strConditionName + "\n");
                        }
                        //OR과 AND의 경우 종목당 한번만 포함된다.
                        if (utility.buy_AND && findRows1[0]["편입"].Equals("이탈") && findRows1[0]["상태"].Equals("주문"))
                        {                   
                            findRows1[0]["편입"] = "편입";
                            findRows1[0]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
                            findRows1[0]["조건식"] = e.strConditionName.Trim();
                            //
                            string code = findRows1[0]["종목코드"].ToString();
                            string code_name = findRows1[0]["종목명"].ToString();
                            string current_price = findRows1[0]["현재가"].ToString();
                            string high1 = findRows1[0]["상한가"].ToString();

                            if (!buy_runningCodes.ContainsKey(code))
                            {
                                string buyCheckResult = buy_check(code, code_name, current_price, time1, high1, false, e.strConditionName);
                                if (buyCheckResult == "매수중")
                                {
                                    findRows1[0]["상태"] = "매수중";
                                }
                                else
                                {
                                    findRows1[0]["상태"] = "주문";
                                }
                            }
                            WriteLog_Stock("[기존종목/재편입] : " + e.sTrCode + " - " + findRows1[0]["종목명"] + " - " + e.strConditionName + "\n");
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
                                    string buyCheckResult = buy_check(code, code_name, current_price, time1, high1, false, e.strConditionName);
                                    if (buyCheckResult == "매수중")
                                    {
                                        findRows1[0]["상태"] = "매수중";
                                    }
                                    else
                                    {
                                        findRows1[0]["상태"] = "주문";
                                    }
                                }
                                WriteLog_Stock("[기존종목/AND편입] : " + e.sTrCode + " - " + findRows1[0]["종목명"] + " - " + e.strConditionName + "\n");
                            }
                        }
                        //INDEPENDENT의 경우 조건식이 다르면 편입한다.
                        if (utility.buy_INDEPENDENT)
                        {
                            for (int i = 0; i < findRows1.Length; i++)
                            {
                                if (findRows1[i]["편입"].Equals("이탈") && findRows1[i]["상태"].Equals("대기") && e.strConditionName.Equals(findRows1[i]["조건식"]))
                                {
                                    findRows1[i]["편입"] = "편입";
                                    findRows1[i]["편입시각"] = DateTime.Now.ToString("HH:mm:ss");
                                    WriteLog_Stock("[기존종목/INDEPENDENT편입] : " + e.sTrCode + " - " + findRows1[i]["종목명"] + " - " + e.strConditionName + "\n");
                                    break;
                                }
                            }
                        }
                        dtCondStock.AcceptChanges();
                        //정렬
                        var sorted_Rows = from row in dtCondStock.AsEnumerable()
                                          orderby row.Field<string>("편입시각") ascending
                                          select row;
                        dtCondStock = sorted_Rows.CopyToDataTable();
                        dataGridView1.DataSource = dtCondStock;
                        return;
                    }

                    //종복비허용(종목당 한번만 포함) /중복허용ㅇ(중목에 대하여 이탈 & 대기 이거나 이탈 & 매도완료인 종목만) 
                    axKHOpenAPI1.SetInputValue("종목코드", e.sTrCode);
                    axKHOpenAPI1.CommRqData("조건실시간검색/" + e.strConditionName, "OPT10001", 0, GetScreenNo());

                    //실시간 항목 등록(대비기호, 현재가. 등락율, 거래량)
                    axKHOpenAPI1.SetRealReg(GetScreenNo(), e.sTrCode, "10;12;13", "1");
                    break;

                //종목 이탈
                case "D":
                    //검출된 종목이 이미 이탈했다면(기본적으로 I D가 번갈아가면서 발생하므로 그럴릴 없음? 있는듯?)
                    DataRow[] findRows = dtCondStock.Select($"종목코드 = {e.sTrCode}");
                    if (findRows.Length == 0) return;

                    bool isExitStock = false;
                    string logMessage = "";

                    if (utility.buy_OR)
                    {
                        isExitStock = true;
                        logMessage = "[기존종목/이탈] : " + e.sTrCode + " - " + findRows[0]["종목명"] + " - " + e.strConditionName;
                    }
                    else if (utility.buy_AND)
                    {
                        if (findRows[0]["상태"].Equals("호출"))
                        {
                            isExitStock = true;
                            logMessage = "[기존종목/이탈] : " + e.sTrCode + " - " + findRows[0]["종목명"] + " - " + e.strConditionName;
                        }
                        else if (findRows[0]["상태"].Equals("주문"))
                        {
                            findRows[0]["상태"] = "호출";
                        }
                    }
                    else if (utility.buy_INDEPENDENT)
                    {
                        for (int i = 0; i < findRows.Length; i++)
                        {
                            if (e.strConditionName.Equals(findRows[i]["조건식"]))
                            {
                                isExitStock = true;
                                logMessage = "[기존종목/INDEPENDENT이탈] : " + e.sTrCode + " - " + findRows[i]["종목명"] + " - " + e.strConditionName;
                                break;
                            }
                        }
                    }

                    if (isExitStock)
                    {
                        findRows[0]["편입"] = "이탈";
                        findRows[0]["이탈시각"] = DateTime.Now.ToString("HH:mm:ss");
                        WriteLog_Stock(logMessage + "\n");
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

                        if (findRows[0]["상태"].Equals("대기") || findRows[0]["상태"].Equals("매도완료"))
                        {
                            axKHOpenAPI1.SetRealRemove("ALL", e.sTrCode);
                        }
                    }
                    break;
            }
        }

        //실시간 시세(지속적 발생 / (현재가. 등락율, 거래량, 수익률)
        private void onReceiveRealData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveRealDataEvent e)
        {

            //종목 확인
            DataRow[] findRows = dtCondStock.Select($"종목코드 = {e.sRealKey}");
            DataRow[] findRows2 = dtCondStock_hold.Select($"종목코드 = {e.sRealKey}");

            if (findRows.Length == 0) return;

            //신규 값 받기
            string price = Regex.Replace(axKHOpenAPI1.GetCommRealData(e.sRealKey, 10).Trim(), @"[\+\-]", ""); //새로운 현재가
            string updown = axKHOpenAPI1.GetCommRealData(e.sRealKey, 12).Trim(); //새로운 등락율
            string amount = axKHOpenAPI1.GetCommRealData(e.sRealKey, 13).Trim(); //새로운 거래량
            string percent = "";

            //값 등록
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
                            sell_check_price(price.Equals("") ? findRows[0]["현재가"].ToString() : string.Format("{0:#,##0}", Convert.ToInt32(price)), percent, Convert.ToInt32(findRows[i]["보유수량"].ToString().Split('/')[0]), Convert.ToInt32(findRows[i]["편입가"].ToString().Replace(",", "")), order_num);
                            sell_runningCodes.Remove(order_num);
                        }
                    }
                }

                //신규 값 빈값 확인
                if (!price.Equals(""))
                {
                    findRows[i]["현재가"] = string.Format("{0:#,##0}", Convert.ToInt32(price)); //새로운 현재가
                }
                if (!updown.Equals(""))
                {
                    findRows[i]["등락율"] = string.Format("{0:#,##0.00}%", Convert.ToDecimal(updown)); //새로운 등락율
                }
                if (!amount.Equals(""))
                {
                    findRows[i]["거래량"] = string.Format("{0:#,##0}", Convert.ToInt32(amount)); //새로운 거래량
                }
                if (!percent.Equals(""))
                {
                    findRows[i]["수익률"] = percent;
                }
            }

            //
            if (findRows2.Length == 0)
            {
                return;
            }
            else
            {
                if (!price.Equals(""))
                {
                    findRows2[0]["현재가"] = string.Format("{0:#,##0}", Convert.ToInt32(price)); //새로운 현재가
                    findRows2[0]["평가금액"] = string.Format("{0:#,##0}", Convert.ToInt32(price) * Convert.ToInt32(findRows2[0]["보유수량"].ToString().Replace(",", "")));
                }
                if (!percent.Equals(""))
                {
                    findRows2[0]["수익률"] = percent;
                    findRows2[0]["손익금액"] = string.Format("{0:#,##0}", Convert.ToInt32(Convert.ToInt32(findRows2[0]["평가금액"].ToString().Replace(",", "")) * Convert.ToDouble(percent.Replace("%", "")) / 100));
                }
            }

            //적용
            dtCondStock.AcceptChanges();
            dataGridView1.DataSource = dtCondStock;

            //적용
            dtCondStock_hold.AcceptChanges();
            dataGridView2.DataSource = dtCondStock_hold;
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
            //특저 열 추출
            DataColumn columnStateColumn = dtCondStock.Columns["상태"];
            //AsEnumerable()은 DataTable의 행을 열거형으로 변환
            var filteredRows = dtCondStock.AsEnumerable()
                                        .Where(row => row.Field<string>(columnStateColumn) == "매수완료")
                                        .ToList();

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
                            if (utility.clear_sell)
                            {
                                sell_order("Nan", "청산매도/시간", order_num, row.Field<string>("수익률"));
                            }
                            //
                            if (utility.clear_sell_mode)
                            {
                                double percent_edit = double.Parse(row.Field<string>("수익률").Replace("%", ""));
                                double profit = double.Parse(utility.clear_sell_profit_text);
                                double loss = double.Parse(utility.clear_sell_loss_text);
                                if (utility.clear_sell_profit && percent_edit >= profit)
                                {
                                    sell_order("Nan", "청산매도/수익", order_num, row.Field<string>("수익률"));
                                }
                                if (utility.clear_sell_loss && percent_edit <= -loss)
                                {
                                    sell_order("Nan", "청산매도/손실", order_num, row.Field<string>("수익률"));
                                }
                            }
                            sell_runningCodes.Remove(order_num);
                        }
                    }
                }
            }
        }

        //--------------실시간 매수 조건 확인 및 매수 주문---------------------

        //매수 가능한 상태인지 확인
        private string buy_check(string code, string code_name, string price, string time, string high, bool check, string condition_name)
        {

            //매수 시간 확인
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

            //보유 종목 수 확인
            string[] hold_status = max_hoid.Text.Split('/');
            int hold = Convert.ToInt32(hold_status[0]);
            int hold_max = Convert.ToInt32(hold_status[1]);
            if (hold >= hold_max) return "대기";

            //매매 횟수 확인
            if (utility.buy_INDEPENDENT)
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

            //이전 종목 매수와의 TERM

            //기존에 포함된 종목이면 따로 변경해줘야 함
            if (check)
            {
                //편입 차트 상태 '매수중' 변경
                DataRow[] findRows = dtCondStock.Select($"종목코드 = {code}");
                findRows[0]["상태"] = "매수중";
                dtCondStock.AcceptChanges();
                dataGridView1.DataSource = dtCondStock;
            }

            //매수 주문(1초에 5회)
            //주문 방식 구분
            string[] order_method = buy_condtion_method.Text.Split('/');

            //시장가 주문
            if (order_method[0].Equals("시장가"))
            {

                //시장가에 대하여 주문 가능 개수 계산 => 기억해야 함 / 종목당매수금액 / 종목당매수수량 / 종목당매수비율 / 종목당최대매수금액
                //User_money.Text;
                int order_acc_market = buy_order_cal(Convert.ToInt32(high.Replace(",", "")));

                WriteLog_Order("[매수주문/시장가/주문접수] : " + code_name  + "(" + code + ") " + order_acc_market + "개\n");
                telegram_message("[매수주문/시장가/주문접수] : " + code_name + "(" + code + ") " + order_acc_market + "개\n");

                System.Threading.Thread.Sleep(200);

                int error = axKHOpenAPI1.SendOrder("시장가매수", GetScreenNo(), utility.setting_account_number, 1, code, order_acc_market, 0, "03", "");

                if (error == 0)
                {
                    //
                    WriteLog_Order("[매수주문/시장가/주문성공] : " + code_name + "(" + code + ") " + order_acc_market + "개\n");
                    telegram_message("[매수주문/시장가/주문성공] : " + code_name + "(" + code + ") " + order_acc_market + "개\n");

                    //보유 수량 업데이트
                    string[] hold_status_update = max_hoid.Text.Split('/');
                    int hold_update = Convert.ToInt32(hold_status_update[0]);
                    int hold_max_update = Convert.ToInt32(hold_status_update[1]);
                    max_hoid.Text = (hold_update + 1) + "/" + hold_max_update;

                    //매매 횟수업데이트
                    if (utility.buy_INDEPENDENT)
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

                    return "매수중/" + order_acc_market;

                }
                else if (error == -308)
                {
                    WriteLog_Order("[매수주문/시장가/주문실패] : " + code_name + "(" + code + ") " + "초당 5회 이상 주문 블가)\n");
                    telegram_message("[매수주문/시장가/주문실패] : " + code_name + "(" + code + ") " + "초당 5회 이상 주문 불가\n");

                    if (check)
                    {
                        //편입 차트 상태 '매수중' 변경
                        DataRow[] findRows = dtCondStock.Select($"종목코드 = {code}");
                        findRows[0]["상태"] = "대기";
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;
                    }

                    return "대기/0";
                }
                else
                {
                    WriteLog_Order("[매수주문/시장가/주문실패] : " + code_name + "(" + code + ") " + "에러코드(" + error + "\n");
                    telegram_message("[매수주문/시장가/주문실패] : " + code_name + "(" + code + ") " + "에러코드(" + error + "\n");

                    //편입 차트 상태 '매수중' 변경
                    DataRow[] findRows = dtCondStock.Select($"종목코드 = {code}");
                    findRows[0]["상태"] = "대기";
                    dtCondStock.AcceptChanges();
                    dataGridView1.DataSource = dtCondStock;

                    return "대기/0";
                }
            }
            //지정가 주문
            else
            {
                //지정가 계산
                int edited_price_hoga = hoga_cal(Convert.ToInt32(price), order_method[1].Equals("현재가") ? 0 : Convert.ToInt32(order_method[1].Replace("호가", "")));

                //지정가에 대하여 주문 가능 개수 계산
                int order_acc = buy_order_cal(edited_price_hoga);

                WriteLog_Order("[매수주문/지정가매수/접수] : " + code_name + "(" + code + ") " + order_acc + "개 " + price + "원\n");
                telegram_message("[매수주문/지정가매수/접수] : " + code_name + "(" + code + ") " + order_acc + "개 " + price + "원\n");

                int error = axKHOpenAPI1.SendOrder("지정가매수", GetScreenNo(), utility.setting_account_number, 1, code, order_acc, edited_price_hoga, "00", "");

                if (error == 0)
                {
                    //
                    WriteLog_Order("[매수주문/지정가매수/접수성공] : " + code_name + "(" + code + ") " + order_acc + "개 " + price + "원\n");
                    telegram_message("[매수주문/지정가매수/접수성공] : " + code_name + "(" + code + ") " + order_acc + "개 " + price + "원\n");

                    //보유 수량 업데이트
                    string[] hold_status_update = max_hoid.Text.Split('/');
                    int hold_update = Convert.ToInt32(hold_status_update[0]);
                    int hold_max_update = Convert.ToInt32(hold_status_update[1]);
                    max_hoid.Text = (hold_update + 1) + "/" + hold_max_update;

                    //매매 횟수업데이트
                    if (utility.buy_INDEPENDENT)
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

                    return "매수중/" + order_acc;

                }
                else if (error == -308)
                {
                    WriteLog_Order("[매수주문/지정가매수/주문실패] : " + code_name + "(" + code + ") " + "초당 5회 이상 주문 불가)\n");
                    telegram_message("[매수주문/지정가매수/주문실패] : " + code_name + "(" + code + ") " + "초당 5회 이상 주문 불가)\n");

                    //편입 차트 상태 '매수중' 변경
                    DataRow[] findRows = dtCondStock.Select($"종목코드 = {code}");
                    findRows[0]["상태"] = "대기";
                    dtCondStock.AcceptChanges();
                    dataGridView1.DataSource = dtCondStock;

                    return "대기";
                }
                else
                {
                    WriteLog_Order("[매수주문/지정가매수/주문실패] : " + code_name + "(" + code + ") " + "에러코드(" + error + "\n");
                    telegram_message("[매수주문/지정가매수/주문실패] : " + code_name + "(" + code + ") " + "에러코드(" + error + "\n");

                    //편입 차트 상태 '매수중' 변경
                    DataRow[] findRows = dtCondStock.Select($"종목코드 = {code}");
                    findRows[0]["상태"] = "대기";
                    dtCondStock.AcceptChanges();
                    dataGridView1.DataSource = dtCondStock;

                    return "대기";
                }
            }
        }

        //매수 주문 수량 계산
        private int buy_order_cal(int price)
        {
            int current_balance = Convert.ToInt32(User_money.Text.Replace(",", ""));
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
        private void sell_check_condition(string code, string price, string percent, string time, string order_num)
        {
            TimeSpan t_code = TimeSpan.Parse(time);
            TimeSpan t_start = TimeSpan.Parse(utility.sell_condition_start);
            TimeSpan t_end = TimeSpan.Parse(utility.sell_condition_end);

            if (t_code.CompareTo(t_start) < 0 || t_code.CompareTo(t_end) > 0)
            {
                WriteLog_Order("[조건식매도/매도시간이탈] : " + code + " - " + "조건식 매도 시간이 아닙니다." + "\n");
                return;
            }

            sell_order(price, "조건식매도", order_num, percent);
        }

        //실시간 가격 매도
        private void sell_check_price(string price, string percent, int hold, int buy_price, string order_num)
        {

            //익절
            if (utility.profit_percent)
            {
                double percent_edit = double.Parse(percent.Replace("%", ""));
                double profit = double.Parse(utility.profit_percent_text);
                if (percent_edit >= profit)
                {
                    sell_order(price, "익절매도", order_num, percent);
                    return;
                }
            }

            //익절원
            if (utility.profit_won)
            {
                int profit_amount = Convert.ToInt32(utility.profit_won_text);
                if ((hold * buy_price * double.Parse(percent.Replace("%", "")) / 100) >= profit_amount)
                {
                    sell_order(price, "익절원", order_num, percent);
                    return;
                }
            }

            //익절TS(대기)
            if (utility.profit_ts)
            {
                sell_order(price, "익절TS", order_num, percent);
                return;
            }

            //손절
            if (utility.loss_percent)
            {
                double percent_edit = double.Parse(percent.TrimEnd('%'));
                double loss = double.Parse(utility.loss_percent_text);
                if (percent_edit <= -loss)
                {
                    sell_order(price, "손절매도", order_num, percent);
                    return;
                }
            }

            //손절원
            if (utility.loss_won)
            {
                int loss_amount = Convert.ToInt32(utility.loss_won_text);
                if ((hold * buy_price * double.Parse(percent.Replace("%", "")) / 100) <= -loss_amount)
                {
                    sell_order(price, "익절원", order_num, percent);
                    return;
                }
            }
        }

        //--------------실시간 매도 주문---------------------

        //매도 주문(1초에 5회)
        private void sell_order(string price, string sell_message, string order_num, string percent)
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

                WriteLog_Order($"[{sell_message}/주문접수] : {code_name}({code}) {order_acc}개 {percent}\n");
                telegram_message($"[{sell_message}/주문접수] : {code_name}({code}) {order_acc}개 {percent}\n");

                //시장가 주문 + 청산주문
                if (sell_message.Split('/')[0].Equals("청산매도") || order_method[0].Equals("시장가"))
                {
                    int error = axKHOpenAPI1.SendOrder("시장가매도", GetScreenNo(), utility.setting_account_number, 2, code, order_acc, 0, "03", "");

                    if (error == 0)
                    {
                        WriteLog_Order($"[{sell_message}/시장가/주문성공] : {code_name}({code}) {order_acc}개");
                        WriteLog_Order($"[{sell_message}/시장가/주문상세] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                        telegram_message($"[{sell_message}/시장가//주문성공] : {code_name}({code}) {order_acc}개 {percent}\n");
                        telegram_message($"[{sell_message}/시장가/주문상세] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                    }
                    else if (error == -308)
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";

                        WriteLog_Order($"[{sell_message}/시장가//주문실패] : {code_name}({code}) 초당 5회 이상 주문 불가\n");
                        telegram_message($"[{sell_message}/시장가//주문실패] : {code_name}({code}) 초당 5회 이상 주문 불가\n");
                    }
                    else
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";

                        WriteLog_Order($"[{sell_message}/시장가//주문실패] : {code_name}({code}) 에러코드({error})\n");
                        telegram_message($"[{sell_message}/시장가//주문실패] : {code_name}({code}) 에러코드({error})\n");
                    }
                }
                //지정가 주문
                else
                {
                    int edited_price_hoga = hoga_cal(Convert.ToInt32(price), order_method[1].Equals("현재가") ? 0 : Convert.ToInt32(order_method[1].Replace("호가", "")));
                    int error = axKHOpenAPI1.SendOrder("시장가매도", GetScreenNo(), utility.setting_account_number, 2, code, order_acc, edited_price_hoga, "00", "");

                    if (error == 0)
                    {
                        WriteLog_Order($"[{sell_message}/지정가/주문성공] : {code_name}({code}) {order_acc}개 {edited_price_hoga}원");
                        WriteLog_Order($"[{sell_message}/지정가/주문상세] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                        telegram_message($"[{sell_message}/지정가/주문성공] : {code_name}({code}) {order_acc}개 {edited_price_hoga}원 {percent}\n");
                        telegram_message($"[{sell_message}/지정가/주문상세] : 편입가 {start_price}원, 현재가 {price}원, 수익 {percent}\n");
                    }
                    else if (error == -308)
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";

                        WriteLog_Order($"[{sell_message}/지정가/주문실패] : {code_name}({code}) 초당 5회 이상 주문 불가\n");
                        telegram_message($"[{sell_message}/지정가/주문실패] : {code_name}({code}) 초당 5회 이상 주문 불가\n");
                    }
                    else
                    {
                        //편입 차트 상태 '매수완료' 변경
                        row["상태"] = "매수완료";

                        WriteLog_Order($"[{sell_message}/지정가/주문실패] : {code_name}({code}) 에러코드({error})\n");
                        telegram_message($"[{sell_message}/지정가/주문실패] : {code_name}({code}) 에러코드({error})\n");
                    }
                }

                //최종 반영
                dtCondStock.AcceptChanges();
                dataGridView1.DataSource = dtCondStock;
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

        //------------주문 상태 확인 및 정정---------------------
        private void onReceiveChejanData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveChejanDataEvent e)
        {
            /*
            e.sGubun 0 신규주문
            9001(종목코드,업종코드);912(주문업무분류);913(주문상태)
            900(주문수량);901(주문가격);902(미체결수량);
            905(주문구분);906(매매구분);907(매도수구분);
            910(체결가);911(체결량);
            938(당일매매수수료);939(당일매매세금)

            9201(계좌번호);9203(주문번호);9205(관리자사번);
            302(종목명); 903(체결누계금액);904(원주문번호);908(주문 및 체결시간);909(체결번호)
            1(현재가);27(최우선 매도호가);28(최우선매수호가);
            914(단위체결가);915(단위체결량);919(거부사유);920(화면번호);921(터미널번호);922(신용구분);923(대출일);
            949(?);10010(?);969(?);819(?)
            */

            if (e.sGubun.Equals("0"))
            {
                /*
                //값확인(매수)
                WriteLog("----------------------------------+\n");
                WriteLog(axKHOpenAPI1.GetChejanData(9001) + "\n");//9001(종목코드,업종코드) A020150
                WriteLog(axKHOpenAPI1.GetChejanData(912) + "\n");//912(주문업무분류); JJ
                WriteLog(axKHOpenAPI1.GetChejanData(913) + "\n");//913(주문상태) 접수 체결
                WriteLog(axKHOpenAPI1.GetChejanData(900) + "\n");//900(주문수량) 18 => 고정값
                WriteLog(axKHOpenAPI1.GetChejanData(901) + "\n");//901(주문가격) 0(시장가)
                WriteLog(axKHOpenAPI1.GetChejanData(902) + "\n");//902(미체결수량) 18 0 16
                WriteLog(axKHOpenAPI1.GetChejanData(905) + "\n");//905(주문구분) +매수 -매도
                WriteLog(axKHOpenAPI1.GetChejanData(906) + "\n");//906(매매구분) 시장가
                WriteLog(axKHOpenAPI1.GetChejanData(907) + "\n");//907(매도수구분) 2(매수) 1(매도)
                WriteLog(axKHOpenAPI1.GetChejanData(910) + "\n");//910(체결가) 25000
                WriteLog(axKHOpenAPI1.GetChejanData(911) + "\n");//911(체결량) 18
                WriteLog(axKHOpenAPI1.GetChejanData(938) + "\n");// 938(당일매매수수료) 0 2830 =>누적
                WriteLog(axKHOpenAPI1.GetChejanData(939) + "\n");//939(당일매매세금) 0 0 =>누적
                WriteLog(axKHOpenAPI1.GetChejanData(908) + "\n");//908(주문 및 체결시간)
                WriteLog("----------------------------------+\n\n");
                */

                //매도수구분
                string Gubun = axKHOpenAPI1.GetChejanData(907);
                //추가로드- 종목코드
                string code = axKHOpenAPI1.GetChejanData(9001).Replace("A", "");
                //추가로드 - 종목이름
                string code_name = axKHOpenAPI1.GetChejanData(302).Trim();
                // 누적체결수량/주문수량
                string order_sum = axKHOpenAPI1.GetChejanData(900);
                string partial_sum = axKHOpenAPI1.GetChejanData(911);
                //미체결수량
                string left_Acc = axKHOpenAPI1.GetChejanData(902);

                WriteLog_Order("------------\n");
                WriteLog_Order($"[체결정보] 매도수구분 : {Gubun}  // 종목 : {code_name}({code})\n");
                WriteLog_Order($"[체결정보] 누적체결수량 : {partial_sum}/{order_sum} // 미체결수량 : {left_Acc}\n");
                WriteLog_Order("------------\n");


                //매수확인
                if (Gubun.Equals("2") && left_Acc.Equals("0"))
                {
                    //데이터 업데이트(Independent 모드에서 어떤 조건식으로 주문이 들어갔는지 알지 못하므로 먼저 처리가 끝난순으로 기입한다)
                    var findRows1 = dtCondStock.AsEnumerable()
                                                .Where(row => row.Field<string>("종목코드") == code &&
                                                              row.Field<string>("상태") == "매수중");

                    System.Threading.Thread.Sleep(200);

                    if (findRows1.Any())
                    {
                        //체결내역업데이트(주문번호)
                        Transaction_Detail(axKHOpenAPI1.GetChejanData(9203).Trim());

                        DataRow row = findRows1.First();
                        string order_number = axKHOpenAPI1.GetChejanData(9203).Trim();
                        row["주문번호"] = order_number;

                        System.Threading.Thread.Sleep(200);

                        //
                        row["보유수량"] = $"{partial_sum}/{order_sum}";

                        //편입 차트 상태 '매수완료' 변경 / 매수 완료 시각 업데이트
                        row["상태"] = "매수완료";
                        string buy_time = axKHOpenAPI1.GetChejanData(908).Trim();
                        row["매수시각"] = string.Format("{0:D2}:{1:D2}:{2:D2}", int.Parse(buy_time.Substring(0, 2)), int.Parse(buy_time.Substring(2, 2)), int.Parse(buy_time.Substring(4, 2)));
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

                        System.Threading.Thread.Sleep(200);

                        //당일 손익 + 당일 손일률 + 당일 수수료 업데이트
                        today_profit_tax_load("NAN");

                        System.Threading.Thread.Sleep(200);

                        //계좌보유현황업데이트
                        Account_before(code);

                        //"[매수주문/시장가/주문성공] : " + code_name + "(" + code + ") " + order_acc_market + "개\n")
                        //Message
                        WriteLog_Order($"[매수주문/정상완료] : {code_name}({code}) {partial_sum}개 {row.Field<string>("편입가")}원\n");
                        telegram_message($"[매수주문/정상완료] : {code_name}({code}) {partial_sum}개 {row.Field<string>("편입가")}원\n");
                    }
                    //HTS에서 매수할 경우
                    else
                    {
                        //체결내역업데이트(주문번호)
                        string order_number = axKHOpenAPI1.GetChejanData(9203).Trim();
                        Transaction_Detail(order_number);

                        System.Threading.Thread.Sleep(200);

                        axKHOpenAPI1.SetInputValue("종목코드", code);
                        axKHOpenAPI1.CommRqData("조건실시간검색_수동/" + order_number, "OPT10001", 0, GetScreenNo());

                        System.Threading.Thread.Sleep(200);

                        //실시간 항목 등록(대비기호, 현재가. 등락율, 거래량)
                        axKHOpenAPI1.SetRealReg(GetScreenNo(), code, "10;12;13", "1");

                        System.Threading.Thread.Sleep(200);

                        //당일 손익 + 당일 손일률 + 당일 수수료 업데이트
                        today_profit_tax_load("NAN");

                        System.Threading.Thread.Sleep(200);

                        //계좌보유현황업데이트
                        Account_before(code);

                        System.Threading.Thread.Sleep(200);

                        //
                        var findRows_manual = dtCondStock.AsEnumerable().Where(row => row.Field<string>("주문번호") == order_number);

                        if (findRows_manual.Any())
                        {
                            DataRow row = findRows_manual.First();
                            row["보유수량"] = $"{partial_sum}/{order_sum}";
                        }
                        dtCondStock.AcceptChanges();
                        dataGridView1.DataSource = dtCondStock;

                        //Message
                        WriteLog_Order($"[HTS매수주문/정상완료] : {code_name}({code}) {partial_sum}게\n");
                        telegram_message($"[HTS매수주문/정상완료] : {code_name}({code}) {partial_sum}게\n");
                    }
                }
                //매도확인
                else if (left_Acc.Equals("0"))
                {
                    //데이터 업데이트(Independent 모드에서 어떤 조건식으로 주문이 들어갔는지 알지 못하므로 먼저 처리가 끝난순으로 기입한다)
                    var findRows2 = dtCondStock.AsEnumerable()
                                                .Where(row => row.Field<string>("종목코드") == code &&
                                                              row.Field<string>("상태") == "매도중");

                    if (findRows2.Any())
                    {
                        DataRow row = findRows2.First();
                        string order_number = axKHOpenAPI1.GetChejanData(9203).Trim();
                        row["주문번호"] = order_number;

                        System.Threading.Thread.Sleep(200);

                        //체결내역업데이트(주문번호)
                        Transaction_Detail(order_number);

                        System.Threading.Thread.Sleep(200);


                        //
                        row["보유수량"] = $"{left_Acc}/0";

                        //중복거래허용
                        if (!utility.duplication_deny)
                        {
                            //편입 차트 상태 '대기' 변경
                            row["상태"] = "대기";
                            string sell_time = axKHOpenAPI1.GetChejanData(908).Trim();
                            row["매도시각"] = string.Format("{0:D2}:{1:D2}:{2:D2}", int.Parse(sell_time.Substring(0, 2)), int.Parse(sell_time.Substring(2, 2)), int.Parse(sell_time.Substring(4, 2)));
                            dtCondStock.AcceptChanges();
                            dataGridView1.DataSource = dtCondStock;
                        }
                        //중복거래비허용
                        else
                        {
                            //모든 화면에서 "code"종목 실시간 해지
                            axKHOpenAPI1.SetRealRemove("ALL", code);
                            string sell_time = axKHOpenAPI1.GetChejanData(908).Trim();
                            row["상태"] = "매도완료";
                            row["매도시각"] = string.Format("{0:D2}:{1:D2}:{2:D2}", int.Parse(sell_time.Substring(0, 2)), int.Parse(sell_time.Substring(2, 2)), int.Parse(sell_time.Substring(4, 2)));
                            dtCondStock.AcceptChanges();
                            dataGridView1.DataSource = dtCondStock;
                        }

                        System.Threading.Thread.Sleep(200);


                        //보유 수량 업데이트
                        string[] hold_status = max_hoid.Text.Split('/');
                        int hold = Convert.ToInt32(hold_status[0]);
                        int hold_max = Convert.ToInt32(hold_status[1]);
                        max_hoid.Text = $"{hold - 1}/{hold_max}";

                        System.Threading.Thread.Sleep(200);


                        //당일 손익 + 당일 손일률 + 당일 수수료 업데이트
                        today_profit_tax_load("매도");

                        System.Threading.Thread.Sleep(200);

                        //계좌보유현황업데이트
                        Account_before("");

                        System.Threading.Thread.Sleep(200);

                        //Message
                        WriteLog_Order($"[매도주문/정상완료] : {code_name}({code}) {partial_sum}개 {row.Field<string>("매도가")}원\n");
                        telegram_message($"[매도주문/정상완료] : {code_name}({code}) {partial_sum}개 {row.Field<string>("매도가")}원\n");

                        System.Threading.Thread.Sleep(200);

                        //예수금 업데이트
                        GetCashInfo(acc_text.Text.Trim(), "예수금상세현황추가");
                    }
                }
            }
            else
            {
                //매수 미체결

                //매도 미체결
            }
        }       
    }
}
