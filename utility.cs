﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;


namespace WindowsFormsApp1
{
    class utility
    {
        //check 변수
        public static string system_route = "C:\\Users\\krkr5\\OneDrive\\바탕 화면\\project\\password\\setting.txt";
        public static bool load_check = false;

        //global 변수
        public static bool auto_trade_allow; //자동실행 여부
        public static string market_start_time; //자동 운영 시간
        public static string market_end_time; //계좌 번호
        public static string setting_account_number; //계좌 번호
        public static string initial_balance; //초기 자산
        public static bool buy_per_price; 

        public static string buy_per_price_text;//종목당매수금액
        public static bool buy_per_amount;
        public static string buy_per_amount_text;//종목당매수수량
        public static bool buy_per_percent; 
        public static string buy_per_percent_text;//종목당매수비율
        public static string maxbuy; //종목당최대매수금액
        public static string maxbuy_acc;//최대매수종목수
        public static string min_price;//종목최소매수가
        public static string max_price;//종목최대매수가

        public static bool max_hold; 
        public static string max_hold_text;//최대보유종목수
        public static bool duplication_deny;//당일중복매수금지
        public static bool before_time_deny;//매수시간전검출매수금지
        public static bool hold_deny;//보유종목매수금지

        public static bool buy_condition; //매수조건
        public static string buy_condition_start;
        public static string buy_condition_end;
        public static string Fomula_list_buy_text;
        public static bool buy_OR;
        public static bool buy_AND;
        public static bool buy_INDEPENDENT;

        public static bool sell_condition; //매도조건
        public static string sell_condition_start;
        public static string sell_condition_end;
        public static int Fomula_list_sell;
        public static string Fomula_list_sell_text;

        public static bool profit_percent;
        public static string profit_percent_text; //익절
        public static bool profit_won;
        public static string profit_won_text; //익절원
        public static bool profit_ts;
        public static string profit_ts_text; //익절TS
        public static bool profit_after1; //익정동시호가
        public static bool profit_after2; //익절시간외단일가
        public static bool profit_index_connection1; //익절 인덱스 연동1
        public static bool profit_index_connection2; //익절 인덱스 연동2

        public static bool loss_percent;
        public static string loss_percent_text; //손절
        public static bool loss_won;
        public static string loss_won_text; //손절원
        public static bool loss_after1; //손절동시호가
        public static bool loss_after2; //손절시간외단일가
        public static bool loss_index_connection1; //손절 인덱스 연동1
        public static bool loss_index_connection2; //손절 인덱스 연동2

        public static bool clear_sell; //전체청산
        public static bool clear_sell_mode; //개별청산
        public static string clear_sell_start;
        public static string clear_sell_end;
        public static bool clear_sell_profit; 
        public static string clear_sell_profit_text; //청산익절
        public static bool clear_sell_profit_after1; //동시호가익절
        public static bool clear_sell_profit_after2;//청산익절시간외단일가
        public static bool clear_sell_index_connection1; //일반 & 익절 인덱스 연동1
        public static bool clear_sell_index_connection2; //일반 & 익절 인덱스 연동2
        public static bool clear_sell_loss;
        public static string clear_sell_loss_text;//청산손절
        public static bool clear_sell_loss_after1;//동시호가손절
        public static bool clear_sell_loss_after2;//청산익절시간외단일가
        public static bool clear_sell_loss_index_connection1; //청산 인덱스 연동1
        public static bool clear_sell_loss_index_connection2; //청산 인덱스 연동2

        public static bool term_for_buy;
        public static string term_for_buy_text;//종목매수텀
        public static bool term_for_sell;
        public static string term_for_sell_text;//종목매도텀
        public static bool term_for_non_buy; //미체결매수취소
        public static bool term_for_non_sell;//미체결매도취소

        public static int buy_set1; //매수설정
        public static int buy_set2;
        public static int sell_set1; //매도설정
        public static int sell_set2;

        public static bool kospi_index;
        public static bool kosdak_index;
        public static bool kospi_commodity;
        public static bool kosdak_commodity;

        public static string type1_selection;
        public static string type1_start;
        public static string type1_end;
        public static string type2_selection;
        public static string type2_start;
        public static string type2_end;
        public static string type3_selection;
        public static string type3_start;
        public static string type3_end;
        public static string type4_selection;
        public static string type4_start;
        public static string type4_end;

        public static bool Telegram_Allow;
        public static string telegram_user_id;
        public static string telegram_token;

        public static bool KIS_Allow;
        public static bool KIS_Independent;
        public static string KIS_Account;
        public static string KIS_appkey;
        public static string KIS_appsecret;
        public static string KIS_amount;

        //utility 목록
        public static async Task setting_load_auto()
        {
            //windows server 2022 영문 기준 바탕화면에 파일을 해제했을 떄 기준으로 주소 변경
            await auto_load(system_route);
            await Task.Run(() =>
            {
                load_check = true;
            });
        }
        public static async Task auto_load(string filepath)
        {
            StreamReader reader = new StreamReader(filepath);

            //자동실행
            String[] auto_trade_allow_tmp = reader.ReadLine().Split('/');
            auto_trade_allow = Convert.ToBoolean(auto_trade_allow_tmp[1]);

            //자동 운영 시간
            String[] time_tmp = reader.ReadLine().Split('/');
            market_start_time = time_tmp[1];
            market_end_time = time_tmp[2];

            //계좌 번호
            String[] account_tmp = reader.ReadLine().Split('/');
            setting_account_number = account_tmp[1];

            //초기 자산
            String[] balance_tmp = reader.ReadLine().Split('/');
            initial_balance = balance_tmp[1];

            //종목당매수금액
            String[] buy_per_price_tmp = reader.ReadLine().Split('/');
            buy_per_price = Convert.ToBoolean(buy_per_price_tmp[1]);
            buy_per_price_text = buy_per_price_tmp[2];

            //종목당매수수량
            String[] buy_per_amount_tmp = reader.ReadLine().Split('/');
            buy_per_amount = Convert.ToBoolean(buy_per_amount_tmp[1]);
            buy_per_amount_text = buy_per_amount_tmp[2];

            //종목당매수비율
            String[] buy_per_percemt_tmp = reader.ReadLine().Split('/');
            buy_per_percent = Convert.ToBoolean(buy_per_percemt_tmp[1]);
            buy_per_percent_text = buy_per_percemt_tmp[2];

            //종목당최대매수금액
            String[] maxbuy_tmp = reader.ReadLine().Split('/');
            maxbuy = maxbuy_tmp[1];

            //최대매수종목수
            String[] maxbuy_acc_tmp = reader.ReadLine().Split('/');
            maxbuy_acc = maxbuy_acc_tmp[1];

            //종목최소매수가
            String[] min_price_tmp = reader.ReadLine().Split('/');
            min_price = min_price_tmp[1];

            //종목최대매수가
            String[] max_price_tmp = reader.ReadLine().Split('/');
            max_price = max_price_tmp[1];

            //최대보유종목수
            String[] max_hold_tmp = reader.ReadLine().Split('/');
            max_hold = Convert.ToBoolean(max_hold_tmp[1]);
            max_hold_text = max_hold_tmp[2];

            //당일중복매수금지
            String[] duplication_deny_tmp = reader.ReadLine().Split('/');
            duplication_deny= Convert.ToBoolean(duplication_deny_tmp[1]);

            //매수시간전검출매수금지
            String[] before_time_deny_tmp = reader.ReadLine().Split('/');
            before_time_deny = Convert.ToBoolean(before_time_deny_tmp[1]);           

            //보유종목매수금지
            String[] hold_deny_tmp = reader.ReadLine().Split('/');
            hold_deny = Convert.ToBoolean(hold_deny_tmp[1]);

            //매수조건
            String[] buy_condition_tmp = reader.ReadLine().Split('/');
            buy_condition = Convert.ToBoolean(buy_condition_tmp[1]);
            buy_condition_start = buy_condition_tmp[2];
            buy_condition_end = buy_condition_tmp[3];
            Fomula_list_buy_text = buy_condition_tmp[4];
            buy_OR = Convert.ToBoolean(buy_condition_tmp[5]);
            buy_AND = Convert.ToBoolean(buy_condition_tmp[6]);
            buy_INDEPENDENT = Convert.ToBoolean(buy_condition_tmp[7]);

            //매도조건
            String[] sell_condition_tmp = reader.ReadLine().Split('/');
            sell_condition = Convert.ToBoolean(sell_condition_tmp[1]);
            sell_condition_start = sell_condition_tmp[2];
            sell_condition_end = sell_condition_tmp[3];
            Fomula_list_sell= Convert.ToInt32(sell_condition_tmp[4]);
            Fomula_list_sell_text = sell_condition_tmp[5];

            //익절
            String[] profit_percent_tmp = reader.ReadLine().Split('/');
            profit_percent = Convert.ToBoolean(profit_percent_tmp[1]);
            profit_percent_text = profit_percent_tmp[2];

            //익절원
            String[] profit_won_tmp = reader.ReadLine().Split('/');
            profit_won = Convert.ToBoolean(profit_won_tmp[1]);
            profit_won_text = profit_won_tmp[2];

            //익절TS
            String[] profit_ts_tmp = reader.ReadLine().Split('/');
            profit_ts = Convert.ToBoolean(profit_ts_tmp[1]);
            profit_ts_text = profit_ts_tmp[2];

            //익정동시호가
            String[] profit_after1_tmp = reader.ReadLine().Split('/');
            profit_after1 = Convert.ToBoolean(profit_after1_tmp[1]);

            //익절시간외단일가
            String[] profit_after2_tmp = reader.ReadLine().Split('/');
            profit_after2 = Convert.ToBoolean(profit_after2_tmp[1]);

            //익절인덱스연동1 
            String[] profit_index_connection1_tmp = reader.ReadLine().Split('/');
            profit_index_connection1 = Convert.ToBoolean(profit_index_connection1_tmp[1]);

            //익절인덱스연동2
            String[] profit_index_connection2_tmp = reader.ReadLine().Split('/');
            profit_index_connection2 = Convert.ToBoolean(profit_index_connection2_tmp[1]);

            //손절
            String[] loss_percent_tmp = reader.ReadLine().Split('/');
            loss_percent = Convert.ToBoolean(loss_percent_tmp[1]);
            loss_percent_text = loss_percent_tmp[2];

            //손절원
            String[] loss_won_tmp = reader.ReadLine().Split('/');
            loss_won = Convert.ToBoolean(loss_won_tmp[1]);
            loss_won_text = loss_won_tmp[2];

            //손절동시호가
            String[] loss_after1_tmp = reader.ReadLine().Split('/');
            loss_after1 = Convert.ToBoolean(loss_after1_tmp[1]);

            //손절시간외단일가
            String[] loss_after2_tmp = reader.ReadLine().Split('/');
            loss_after2 = Convert.ToBoolean(loss_after2_tmp[1]);


            //손절인덱스연동1
            String[] loss_index_connection1_tmp = reader.ReadLine().Split('/');
            loss_index_connection1 = Convert.ToBoolean(loss_index_connection1_tmp[1]);

            //손절인덱스연동2
            String[] loss_index_connection2_tmp = reader.ReadLine().Split('/');
            loss_index_connection2 = Convert.ToBoolean(loss_index_connection2_tmp[1]);

            //전체청산
            String[] clear_sell_tmp = reader.ReadLine().Split('/');
            clear_sell = Convert.ToBoolean(clear_sell_tmp[1]);
            clear_sell_start = clear_sell_tmp[2];
            clear_sell_end = clear_sell_tmp[3];

            //청산모드선택
            String[] clear_sell_mode_tmp = reader.ReadLine().Split('/');
            clear_sell_mode = Convert.ToBoolean(clear_sell_mode_tmp[1]);

            //청산익절
            String[] clear_sell_profit_tmp = reader.ReadLine().Split('/');
            clear_sell_profit = Convert.ToBoolean(clear_sell_profit_tmp[1]);
            clear_sell_profit_text = clear_sell_profit_tmp[2];

            //청산익절동시호가
            String[] clear_sell_profit_after1_tmp = reader.ReadLine().Split('/');
            clear_sell_profit_after1 = Convert.ToBoolean(clear_sell_profit_after1_tmp[1]);

            //청산익절시간외단일가
            String[] clear_sell_profit_after2_tmp = reader.ReadLine().Split('/');
            clear_sell_profit_after2 = Convert.ToBoolean(clear_sell_profit_after2_tmp[1]);

            //청산일반익절인덱스연동1
            String[] clear_sell_index_connection1_tmp = reader.ReadLine().Split('/');
            clear_sell_index_connection1 = Convert.ToBoolean(clear_sell_index_connection1_tmp[1]);

            //청산일반익절인덱스연동2
            String[] clear_sell_index_connection2_tmp = reader.ReadLine().Split('/');
            clear_sell_index_connection2 = Convert.ToBoolean(clear_sell_index_connection2_tmp[1]);

            //청산손절
            String[] clear_sell_loss_tmp = reader.ReadLine().Split('/');
            clear_sell_loss= Convert.ToBoolean(clear_sell_loss_tmp[1]);
            clear_sell_loss_text = clear_sell_loss_tmp[2];

            //청산손절동시호가
            String[] clear_sell_loss_after1_tmp = reader.ReadLine().Split('/');
            clear_sell_loss_after1 = Convert.ToBoolean(clear_sell_loss_after1_tmp[1]);

            //청산익절시간외단일가
            String[] clear_sell_loss_after2_tmp = reader.ReadLine().Split('/');
            clear_sell_loss_after2 = Convert.ToBoolean(clear_sell_loss_after2_tmp[1]);

            //청산손절인덱스연동1
            String[] clear_sell_loss_index_connection1_tmp = reader.ReadLine().Split('/');
            clear_sell_loss_index_connection1 = Convert.ToBoolean(clear_sell_loss_index_connection1_tmp[1]);

            //청산손절인덱스연동2
            String[] clear_sell_loss_index_connection2_tmp = reader.ReadLine().Split('/');
            clear_sell_loss_index_connection2 = Convert.ToBoolean(clear_sell_loss_index_connection2_tmp[1]);

            //종목매수텀(대기)
            String[] term_for_buy_tmp = reader.ReadLine().Split('/');
            term_for_buy = Convert.ToBoolean(term_for_buy_tmp[1]);
            term_for_buy_text = term_for_buy_tmp[2];

            //종목매도텀(대기)
            String[] term_for_sell_tmp = reader.ReadLine().Split('/');
            term_for_sell = Convert.ToBoolean(term_for_sell_tmp[1]);
            term_for_sell_text = term_for_sell_tmp[2];

            //미체결매수취소(대기)
            String[] term_for_non_buy_tmp = reader.ReadLine().Split('/');
            term_for_non_buy = Convert.ToBoolean(term_for_non_buy_tmp[1]);

            //미체결매도취소(대기)
            String[] term_for_non_sell_tmp = reader.ReadLine().Split('/');
            term_for_non_sell = Convert.ToBoolean(term_for_non_sell_tmp[1]);

            //매수설정
            String[] buy_set_tmp = reader.ReadLine().Split('/');
            buy_set1 = Convert.ToInt32(buy_set_tmp[1]);
            buy_set2 = Convert.ToInt32(buy_set_tmp[2]);

            //매도설정
            String[] sell_set_tmp = reader.ReadLine().Split('/');
            sell_set1 = Convert.ToInt32(sell_set_tmp[1]);
            sell_set2 = Convert.ToInt32(sell_set_tmp[2]);

            //코스피지수
            String[] kospi_index_tmp = reader.ReadLine().Split('/');
            kospi_index = Convert.ToBoolean(kospi_index_tmp[1]);

            //코스닥지수
            String[] kosdak_index_tmp = reader.ReadLine().Split('/');
            kosdak_index = Convert.ToBoolean(kosdak_index_tmp[1]);

            //코스피선물
            String[] kospi_commodity_tmp = reader.ReadLine().Split('/');
            kospi_commodity = Convert.ToBoolean(kospi_commodity_tmp[1]);

            //코스닥선물
            String[] kosdak_commodity_tmp = reader.ReadLine().Split('/');
            kosdak_commodity = Convert.ToBoolean(kosdak_commodity_tmp[1]);

            //#1
            String[] type1_selection_tmp = reader.ReadLine().Split('/');
            type1_selection = Convert.ToString(type1_selection_tmp[1]);
            type1_start = Convert.ToString(type1_selection_tmp[2]);
            type1_end = Convert.ToString(type1_selection_tmp[3]);

            //#2
            String[] type2_selection_tmp = reader.ReadLine().Split('/');
            type2_selection = Convert.ToString(type2_selection_tmp[1]);
            type2_start = Convert.ToString(type2_selection_tmp[2]);
            type2_end = Convert.ToString(type2_selection_tmp[3]);

            //#3
            String[] type3_selection_tmp = reader.ReadLine().Split('/');
            type3_selection = Convert.ToString(type3_selection_tmp[1]);
            type3_start = Convert.ToString(type3_selection_tmp[2]);
            type3_end = Convert.ToString(type3_selection_tmp[3]);

            //#4
            String[] type4_selection_tmp = reader.ReadLine().Split('/');
            type4_selection = Convert.ToString(type4_selection_tmp[1]);
            type4_start = Convert.ToString(type4_selection_tmp[2]);
            type4_end = Convert.ToString(type4_selection_tmp[3]);

            //텔레그램Telegram_Allow
            String[] Telegram_Allow_tmp = reader.ReadLine().Split('/');
            Telegram_Allow = Convert.ToBoolean(Telegram_Allow_tmp[1]);

            //텔레그램ID
            String[] telegram_user_id_tmp = reader.ReadLine().Split('/');
            telegram_user_id = telegram_user_id_tmp[1];

            //텔레그램TOKEN
            String[] telegram_token_tmp = reader.ReadLine().Split('/');
            telegram_token = telegram_token_tmp[1];

            //한국투자증권KIS_Allow
            String[] KIS_Allow_tmp = reader.ReadLine().Split('/');
            KIS_Allow = Convert.ToBoolean(KIS_Allow_tmp[1]);

            //한국투자증권KIS_Independent
            String[] KIS_Independent_tmp = reader.ReadLine().Split('/');
            KIS_Independent = Convert.ToBoolean(KIS_Independent_tmp[1]);

            //한국투자증권Account
            String[] KIS_Account_tmp = reader.ReadLine().Split('/');
            KIS_Account = KIS_Account_tmp[1];

            //한국투자증권appkey
            String KIS_appkey_tmp = reader.ReadLine();
            KIS_appkey = KIS_appkey_tmp;

            //한국투자증권appsecret
            String KIS_appsecret_tmp = reader.ReadLine();
            KIS_appsecret = KIS_appsecret_tmp;

            //한국투자증권KIS_amount
            String[] KIS_amount_tmp = reader.ReadLine().Split('/');
            KIS_amount = KIS_amount_tmp[1];

            reader.Close();

        }
    }
}
