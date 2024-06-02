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
using System.Linq;
using System.Windows.Forms;
using System.Net.WebSockets;

namespace WindowsFormsApp1
{
    class utility_KIS
    {
        public string appKey = utility.KIS_appkey;
        public string secretkey = utility.KIS_appsecret;
        //
        public string cano = utility.KIS_Account.Split('-')[0];
        public string acntPrdtCd = utility.KIS_Account.Split('-')[1];
        //
        public string Approval_key = "";
        public string access_token = "";
        public string account_value = "";

        //초기실행
        private async Task Initial_KIS()
        {
            //웹소켓발급(모의투자)
            await KIS_WebSocket();

            //접근토큰발급(모의투자)
            await KIS_Access();

            //투자계좌자산현황조회-예수금(모의투자미지원)
            await KIS_Depositt();

            //국내주식실시간체결통보-등록(모의투자) => 주문번호, 체결수량, 주문수량
            await KIS_Real_Order_Result();

            //국내주식매수매도주문(모의투자)
            //KIS_Order(string buy_sell, string code, string order_type, string order_amt, string order_price)

            //주식일별주문체결조회(모의투자) => 주문번호 => 주문번호, 평균가
            //KIS_buy_Mean_Price(string order_number)

            //기간별매매손익현황조회(모의투자미지원) => 종목번호 => 실현손익합계(수수료, 이자 제세금 가감 금액)
            //KIS_Profit_Check()
        }

        //-------------------------접근토큰발급(모의투자)-----------------------------

        //접근토큰받기
        public async Task<string> KIS_WebSocket()
        {
            string domain = "https://openapivts.koreainvestment.com:29443";// 모의투자
            //string domain = "https://openapi.koreainvestment.com:9443" //실전투자
            string endpoint = "/oauth2/Approval";

            // Construct the request data
            var requestData = new
            {
                grant_type = "client_credentials",
                appkey = appKey,
                appsecret = secretkey
            };

            // Serialize the request data to JSON
            string jsonData = JsonConvert.SerializeObject(requestData);

            // Make a POST request to the token endpoint
            using (var client = new HttpClient())
            {
                // Set the base address
                client.BaseAddress = new Uri(domain);

                // Create the request content
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync(endpoint, content);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Parse the JSON response
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse_WebSocket>(responseContent);
                    // Access the token and other fields
                    string approval_key = tokenResponse.approval_key;
                    //
                    Approval_key = approval_key;
                    return "WebSocket Success";
                }
                else
                {
                    return $"Failed to get token. Status code: {response.StatusCode}";
                }
            }
        }

        //받을 값 처리
        class TokenResponse_WebSocket
        {
            public string approval_key { get; set; }
        }

        //-------------------------접근토큰발급(모의투자)-----------------------------

        public async Task<String> KIS_Access()
        {
            string domain = "https://openapivts.koreainvestment.com:29443";// 모의투자
            //string domain = "https://openapi.koreainvestment.com:9443" //실전투자
            string endpoint = "/oauth2/tokenP";

            // Construct the request data
            var requestData = new
            {
                grant_type = "client_credentials",
                appkey = appKey,
                appsecret = secretkey
            };

            // Serialize the request data to JSON
            string jsonData = JsonConvert.SerializeObject(requestData);

            // Make a POST request to the token endpoint
            using (var client = new HttpClient())
            {
                // Set the base address
                client.BaseAddress = new Uri(domain);

                // Create the request content
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync(endpoint, content);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Parse the JSON response
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                    // Access the token and other fields
                    string accessToken = tokenResponse.Access_token;
                    //
                    access_token = accessToken;
                    return "Access Key Success";
                }
                else
                {
                    return $"Failed to get token. Status code: {response.StatusCode}";
                }
            }
        }

        //받을 값 처리
        class TokenResponse
        {
            public string Access_token { get; set; }
        }

        //-------------------------투자계좌자산현황조회예수금(모의투자미지원)-----------------------------

        public async Task<string> KIS_Depositt()
        {
            string domain = "https://openapi.koreainvestment.com:9443";
            string endpoint = "/uapi/domestic-stock/v1/trading/inquire-account-balance";

            // Construct the query parameters
            var queryParams = new Dictionary<string, string>
            {
                { "CANO", cano },
                { "ACNT_PRDT_CD", acntPrdtCd },
            };

            string queryString = string.Join("&", queryParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
            string url = $"{domain}{endpoint}?{queryString}&INQR_DVSN_1&BSPR_BF_DT_APLY_YN";

            // Make a POST request to the token endpoint
            using (var client = new HttpClient())
            {

                client.BaseAddress = new Uri(domain);

                // Set authorization header and other headers
                client.DefaultRequestHeaders.Add("authorization", "Bearer " + access_token);
                client.DefaultRequestHeaders.Add("appkey", appKey);
                client.DefaultRequestHeaders.Add("appsecret", secretkey);
                client.DefaultRequestHeaders.Add("tr_id", "CTRP6548R");
                client.DefaultRequestHeaders.Add("custtype", "P");

                // Create the request content
                var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

                // Send the Get request
                HttpResponseMessage response = await client.GetAsync("");

                MessageBox.Show(response.ToString());

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content
                    string responseContent = await response.Content.ReadAsStringAsync();
                    //WriteLog_System(responseContent);
                    // Parse the JSON response
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse<Output2Data>>(responseContent);
                    // Access the token and other fields
                    string Nass_tot_amt = tokenResponse.output2.nass_tot_amt; //순자산총금액
                    string Tot_asst_amt = tokenResponse.output2.tot_asst_amt; //총자산금액
                    string Tot_dncl_amt = tokenResponse.output2.tot_dncl_amt; //총예수금액
                    string Dncl_amt = tokenResponse.output2.dncl_amt; //예수금액
                    //
                    account_value = Tot_asst_amt;
                    return "Account Value Success";
                }
                else
                {
                    return $"Failed to get token. Status code: {response.StatusCode}";
                }
            }
        }
        public class TokenResponse<T1>
        {
            public T1 output2 { get; set; }
        }

        public class Output2Data
        {
            public string nass_tot_amt { get; set; } //순자산총금액
            public string tot_asst_amt { get; set; } //총자산금액
            public string tot_dncl_amt { get; set; } //총예수금액
            public string dncl_amt { get; set; } //예수금액
        }

        //-------------------------국내주식매수매도주문(모의투자)-----------------------------

        public async Task KIS_Order(string buy_sell, string code, string order_type, string order_amt, string order_price)
        {
            //string domain = "https://openapivts.koreainvestment.com:29443"; //모의투자
            string domain = "https://openapi.koreainvestment.com:9443"; //실전투자
            string endpoint = "/uapi/domestic-stock/v1/trading/order-cash";
            //string buy_sell_code = "VTTC0802U"; //기본 모의투자 매수
            string buy_sell_code = "TTTC0802U"; //기본 실전투자 매수

            //
            if (buy_sell.Equals("sell"))
            {
                //buy_sell_code = "VTTC0801U"; //기본 모의투자 매도
                buy_sell_code = "TTTC0801U"; //기본 실전투자 매도
            }

            // Construct the request data
            var requestData = new
            {
                CANO = cano,
                ACNT_PRDT_CD = acntPrdtCd,
                PDNO = code, //종목코드
                ORD_DVSN = order_type, //시장가
                ORD_QTY = order_amt, //주문수량계산
                ORD_UNPR = order_price, //주문단가 => 주문단가 없는 주문은 0
            };

            // Serialize the request data to JSON
            string jsonData = JsonConvert.SerializeObject(requestData);

            // Make a POST request to the token endpoint
            using (var client = new HttpClient())
            {
                // Set the base address
                client.BaseAddress = new Uri(domain);

                // Set authorization header and other headers
                client.DefaultRequestHeaders.Add("authorization", "Bearer " + access_token);
                client.DefaultRequestHeaders.Add("appkey", appKey);
                client.DefaultRequestHeaders.Add("appsecret", secretkey);
                client.DefaultRequestHeaders.Add("tr_id", buy_sell_code);
                client.DefaultRequestHeaders.Add("custtype", "P");

                // Create the request content
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync(endpoint, content);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Parse the JSON response
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse_Order>(responseContent);
                    // Access the token and other fields
                    string rt_cd = tokenResponse.rt_cd;
                    string msg_cd = tokenResponse.msg_cd;
                    string msg = tokenResponse.msg;
                    string ODNO = tokenResponse.ODNO;
                    //문자열 보간 방식
                    //WriteLog_System($"Nass_tot_amt: {rt_cd}\n"); //0 : 성공 / 나머지 실패
                    //WriteLog_System($"Tot_asst_amt: {msg_cd}\n"); //응답코드
                    //WriteLog_System($"Tot_dncl_amt: {msg}\n"); //응답메세지
                    //WriteLog_System($"Dncl_amt: {ODNO}\n"); //주문번호
                }
                else
                {
                    MessageBox.Show($"Failed to get token. Status code: {response.StatusCode}");
                }
            }
        }
        class TokenResponse_Order
        {
            public string rt_cd { get; set; } //0 : 성공 / 나머지 실패
            public string msg_cd { get; set; } //응답코드
            public string msg { get; set; } //응답메세지
            public string ODNO { get; set; } //주문번호
        }

        //-------------------------국내주식실시간체결통보-등록(모의투자)-----------------------------
        public async Task KIS_Real_Order_Result()
        {
            string domain = "ws://ops.koreainvestment.com:31000"; //모의투자
            //string domain = "ws://ops.koreainvestment.com:21000"; //실전투자
            string endpoint = "/tryitout/H0STCNI0";

            // Construct the request data
            var requestData = new
            {
                approval_key = Approval_key,
                custtype = "P",
                tr_type = "1", //1 :등록, 2 : 해제
                tr_id = "H0STCNI9",//모의투자 실시간 주식 체결통보
                //tr_id = "H0STCNI0",//실전투자 실시간 주식 체결통보
                tr_key = "kiki5628" //HTS ID
            };

            // Serialize the request data to JSON
            string jsonData = JsonConvert.SerializeObject(requestData);

            // Make a POST request to the token endpoint
            //using (ClientWebSocket client = new ClientWebSocket())
            using (var client = new HttpClient())
            {
                // Set the base address
                client.BaseAddress = new Uri(domain);

                // Create the request content
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync(endpoint, content);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Parse the JSON response
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse_Order_Real>(responseContent);
                    // Access the token and other fields
                    string ODER_NO = tokenResponse.ODER_NO;
                    string ELN_BYOV_CLS = tokenResponse.ELN_BYOV_CLS;
                    string CNTG_QTY = tokenResponse.CNTG_QTY;
                    string ODER_QTY = tokenResponse.ODER_QTY;
                    //문자열 보간 방식
                    //WriteLog_System($"Nass_tot_amt: {ODER_NO}\n"); //주문번호
                    //WriteLog_System($"Tot_asst_amt: {ELN_BYOV_CLS}\n"); //매도매수구분 01 매도 02 매수
                    //WriteLog_System($"Tot_dncl_amt: {CNTG_QTY}\n"); //체결수량
                    //WriteLog_System($"Dncl_amt: {ODER_QTY}\n"); //주문수량
                }
                else
                {
                    MessageBox.Show($"Failed to get token. Status code: {response.StatusCode}");
                }
            }
        }
        class TokenResponse_Order_Real
        {
            public string ODER_NO { get; set; } //주문번호
            public string ELN_BYOV_CLS { get; set; } //매도매수구분 01 매도 02 매수
            public string CNTG_QTY { get; set; } //체결수량
            public string ODER_QTY { get; set; } //주문수량
        }

        //-------------------------주식일별주문체결조회(모의투자)-----------------------------

        //주문번호, 평균가
        public async Task KIS_buy_Mean_Price(string order_number)
        {
            string domain = "https://openapivts.koreainvestment.com:29443"; //모의투자
            //string domain = "https://openapi.koreainvestment.com:9443"; //실전투자
            string endpoint = "/uapi/domestic-stock/v1/trading/inquire-daily-ccld";

            // Construct the request data
            var requestData = new
            {
                authorization = access_token,
                appkey = appKey,
                appsecret = secretkey,
                tr_id = "VTTC8001R",
                //tr_id = "TTTC8001R",//실전
                CANO = cano,
                ACNT_PRDT_CD = acntPrdtCd,
                INQR_STRT_DT = DateTime.Now.ToString("yyyyMMdd"),
                INQR_END_DT = DateTime.Now.ToString("yyyyMMdd"),
                SLL_BUY_DVSN_CD = "00",
                INQR_DVSN = "00",
                PDNO = "", //전체조회
                CCLD_DVSN = "00",
                ORD_GNO_BRNO = "",
                ODNO = "",
                INQR_DVSN_3 = "00",
                INQR_DVSN_1 = "",
                CTX_AREA_FK100 = "",
                CTX_AREA_NK100 = ""
            };

            // Serialize the request data to JSON
            string jsonData = JsonConvert.SerializeObject(requestData);

            // Make a POST request to the token endpoint
            using (var client = new HttpClient())
            {
                // Set the base address
                client.BaseAddress = new Uri(domain);

                // Create the request content
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync(endpoint, content);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Parse the JSON response
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse_Mean_Price>(responseContent);
                    // Access the token and other fields
                    string odno = "";
                    string avg_prvs = "";
                    //
                    if (tokenResponse.output1 != null)
                    {
                        var targetData = tokenResponse.output1.FirstOrDefault(data => data.odno.Equals(requestData));
                        if (targetData != null)
                        {
                            odno = targetData.odno;
                            avg_prvs = targetData.avg_prvs;
                        }
                    }
                    //문자열 보간 방식
                    //WriteLog_System($"Nass_tot_amt: {odno}\n"); //주문번호
                    //WriteLog_System($"Tot_asst_amt: {avg_prvs}\n"); //평균가
                }
                else
                {
                    MessageBox.Show($"Failed to get token. Status code: {response.StatusCode}");
                }
            }
        }
        public class TokenResponse_Mean_Price
        {
            public List<output1Data> output1 { get; set; }
        }

        public class output1Data
        {
            public string odno { get; set; } //주문번호
            public string avg_prvs { get; set; } //평균가
        }
        //-------------------------기간별매매손익현황조회매입단가실현손익(모의투자미지원)-----------------------------

        //실현손익합계
        public async Task KIS_Profit_Check()
        {
            string domain = "https://openapi.koreainvestment.com:9443"; //실전투자
            string endpoint = "/uapi/domestic-stock/v1/trading/inquire-period-trade-profite";

            // Construct the request data
            var requestData = new
            {
                authorization = access_token,
                appkey = appKey,
                appsecret = secretkey,
                tr_id = "TTTC8715R",
                custtype = "P",
                CANO = cano,
                SORT_DVSN = "00",
                ACNT_PRDT_CD = acntPrdtCd,
                PDNO = "",
                INQR_STRT_DT = DateTime.Now.ToString("yyyyMMdd"),
                INQR_END_DT = DateTime.Now.ToString("yyyyMMdd"),
                CTX_AREA_NK100 = "",
                CBLC_DVSN = "00",
                CTX_AREA_FK100 = ""
            };

            // Serialize the request data to JSON
            string jsonData = JsonConvert.SerializeObject(requestData);

            // Make a POST request to the token endpoint
            using (var client = new HttpClient())
            {
                // Set the base address
                client.BaseAddress = new Uri(domain);

                // Create the request content
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync(endpoint, content);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Parse the JSON response
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse_Profit<ProfitData>>(responseContent);
                    // Access the token and other fields
                    string tot_rlzt_pfls = tokenResponse.output2.tot_rlzt_pfls;
                    //문자열 보간 방식
                    //WriteLog_System($"Nass_tot_amt: {tot_rlzt_pfls}\n"); //실현손익합계(수수료 제외한 금액)
                }
                else
                {
                    MessageBox.Show($"Failed to get token. Status code: {response.StatusCode}");
                }
            }
        }
        public class TokenResponse_Profit<T1>
        {
            public T1 output2 { get; set; }
        }

        public class ProfitData
        {
            public string tot_rlzt_pfls { get; set; } //실현손익합계(수수료 제외한 금액)
        }

    }
}
