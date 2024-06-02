using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Setting : Form
    {
        private Trade_Auto_Daishin _trade_Auto_Daishin;

        public Setting(Trade_Auto_Daishin trade_Auto_Daishin)
        {
            InitializeComponent();

            //FORM1 불러오기
            _trade_Auto_Daishin = trade_Auto_Daishin;

            //초기값세팅
            setting_load_auto();

            //save & load
            save_button.Click += setting_save;
            setting_open.Click += setting_load;

            //즉시반영
            setting_allowed.Click += setting_allow;

            //조건식 동작
            Fomula_list_buy.DropDown += Fomula_list_buy_DropDown;
            Fomula_list_buy_Checked_box.MouseLeave += Fomula_list_buy_Checked_box_MouseLeave;
            Fomula_list_buy_Checked_box.ItemCheck += Fomula_list_buy_Checked_box_ItemCheck;

            //매매방식 자동 동작
            buy_set1.Leave += Buy_set1_Leave;
            buy_set2.Leave += Buy_set2_Leave;
            sell_set1.Leave += Sell_set1_Leave;
            sell_set2.Leave += Sell_set2_Leave;
            sell_set1_after.Leave += Sell_set_after1_Leave;
            sell_set2_after.Leave += Sell_set_after2_Leave;

            //TELEGRAM TEST
            telegram_test_button.Click += telegram_test;

            //미사용 항목 경고창(19개)
            profit_ts.CheckedChanged += HandleCheckedChanged;
            term_for_non_buy.CheckedChanged += HandleCheckedChanged;
            term_for_non_sell.CheckedChanged += HandleCheckedChanged;

            //--------------------------------------------

            //소수점 범위 확인
            profit_percent_text.Leave += Profit_percent_text_Leave;
            loss_percent_text.Leave += Loss_percent_text_Leave;
            profit_ts_text.Leave += Profit_ts_text_Leave;

            clear_sell_profit_text.Leave += Clear_sell_profit_text_Leave;
            clear_sell_loss_text.Leave += Clear_sell_loss_text_Leave;

            //정수값인지확인
            setting_account_number.Leave += Setting_Positive_numver;
            initial_balance.Leave += Initial_balance_Leave;

            maxbuy.Leave += Maxbuy_Leave;
            maxbuy_acc.Leave += Maxbuy_acc_Leave;
            min_price.Leave += Min_price_Leave;
            max_price.Leave += Max_price_Leave;

            max_hold_text.Leave += Max_hold_text_Leave;

            profit_won_text.Leave += Profit_won_text_Leave;

            loss_won_text.Leave += Loss_won_text_Leave;

            term_for_buy_text.Leave += Term_for_buy_text_Leave;
            term_for_sell_text.Leave += Term_for_sell_text_Leave;

            type0_start.Leave += Type0_start_Leave;
            type0_end.Leave += Type0_end_Leave;

            type0_all_start.Leave += Type0_all_start_Leave;
            type0_all_end.Leave += Type0_all_end_Leave;

            type0_isa_start.Leave += Type0_isa_start_Leave;
            type0_isa_end.Leave += Type0_isa_end_Leave;

            //시간확인
            market_start_time.Leave += Market_start_time_Leave;
            market_end_time.Leave += Market_end_time_Leave;

            buy_condition_start.Leave += Buy_condition_start_Leave;
            buy_condition_end.Leave += Buy_condition_end_Leave;

            sell_condition_start.Leave += Sell_condition_start_Leave;
            sell_condition_end.Leave += Sell_condition_end_Leave;

            clear_sell_start.Leave += Clear_sell_start_Leave;
            clear_sell_end.Leave += Clear_sell_end_Leave;

            Dual_Time_Start.Leave += Dual_Time_Start_Leave;
            Dual_Time_Stop.Leave += Dual_Time_Stop_Leave;

            //소수점이거나 정수인지 확인
            type1_start.Leave += Type1_start_Leave;
            type1_end.Leave += Type1_end_Leave;
            type2_start.Leave += Type2_start_Leave;
            type2_end.Leave += Type2_end_Leave;
            type3_start.Leave += Type3_start_Leave;
            type3_end.Leave += Type3_end_Leave;
            type4_start.Leave += Type4_start_Leave;
            type4_end.Leave += Type4_end_Leave;
            type5_start.Leave += Type5_start_Leave;
            type5_end.Leave += Type5_end_Leave;

            type1_all_start.Leave += Type1_all_start_Leave;
            type1_all_end.Leave += Type1_all_end_Leave;
            type2_all_start.Leave += Type2_all_start_Leave;
            type2_all_end.Leave += Type2_all_end_Leave;
            type3_all_start.Leave += Type3_all_start_Leave;
            type3_all_end.Leave += Type3_all_end_Leave;
            type4_all_start.Leave += Type4_all_start_Leave;
            type4_all_end.Leave += Type4_all_end_Leave;
            type5_all_start.Leave += Type5_all_start_Leave;
            type5_all_end.Leave += Type5_all_end_Leave;

            type1_isa_start.Leave += Type1_isa_start_Leave;
            type1_isa_end.Leave += Type1_isa_end_Leave;
            type2_isa_start.Leave += Type2_isa_start_Leave;
            type2_isa_end.Leave += Type2_isa_end_Leave;
            type3_isa_start.Leave += Type3_isa_start_Leave;
            type3_isa_end.Leave += Type3_isa_end_Leave;
            type4_isa_start.Leave += Type4_isa_start_Leave;
            type4_isa_end.Leave += Type4_isa_end_Leave;
            type5_isa_start.Leave += Type5_isa_start_Leave;
            type5_isa_end.Leave += Type5_isa_end_Leave;
        }

        //----------------------------미사용 항목 경고창----------------------------------------

        private void HandleCheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkedCheckBox = (CheckBox)sender;
            if (checkedCheckBox.Checked)
            {
                MessageBox.Show("준비중입니다.", "개발중", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // 필요한 경우 여기에 특정 함수를 호출합니다.
            }
            checkedCheckBox.Checked = false;
        }

        //----------------------------매매방식 확인----------------------------------------

        private void Buy_set1_Leave(object sender, EventArgs e)
        {
            ValidateOrderType1(sender, e, buy_set1, buy_set2);
        }

        private void Sell_set1_Leave(object sender, EventArgs e)
        {
            ValidateOrderType1(sender, e, sell_set1, sell_set2);
        }

        private void Buy_set2_Leave(object sender, EventArgs e)
        {
            ValidateOrderType2(sender, e, buy_set1, buy_set2);
        }

        private void Sell_set2_Leave(object sender, EventArgs e)
        {
            ValidateOrderType2(sender, e, sell_set1, sell_set2);
        }

        private void ValidateOrderType1(object sender, EventArgs e, ComboBox orderType, ComboBox orderPrice)
        {
            if (orderType.Text.Equals("") && orderPrice.Text.Equals(""))
            {
                orderType.SelectedIndex = 1;
                orderPrice.SelectedIndex = 6;
                MessageBox.Show("선택된 매매방식이 없습니다.", "기본값 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (orderType.Text.Equals("시장가"))
            {
                orderPrice.SelectedIndex = 6;
                return;
            }

            if (orderType.Text.Equals("지정가") && orderPrice.Text.Equals("시장가"))
            {
                orderPrice.SelectedIndex = 5;
                return;
            }
        }

        private void ValidateOrderType2(object sender, EventArgs e, ComboBox orderType, ComboBox orderPrice)
        {
            if (orderPrice.Text.Equals(""))
            {
                if (orderType.Text.Equals(""))
                {
                    orderType.SelectedIndex = 1;
                    orderPrice.SelectedIndex = 6;
                    MessageBox.Show("선택된 매매방식이 없습니다.", "기본값 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (orderType.Text.Equals("시장가"))
                {
                    orderPrice.SelectedIndex = 6;
                    return;
                }

                if (orderType.Text.Equals("지정가"))
                {
                    orderPrice.SelectedIndex = 5;
                    return;
                }
            }

            if (orderPrice.Text.Equals("시장가") && !orderType.Text.Equals("시장가"))
            {
                orderType.SelectedIndex = 1;
                return;
            }

            if (!orderPrice.Text.Equals("시장가") && !orderType.Text.Equals("지정가"))
            {
                orderType.SelectedIndex = 0;
                return;
            }
        }


        private void Sell_set_after1_Leave(object sender, EventArgs e)
        {
            if (sell_set1_after.Text.Equals(""))
            {
                if (sell_set2_after.Text.Equals(""))
                {
                    sell_set1_after.SelectedIndex = 0;
                    sell_set2_after.SelectedIndex = 5;
                    MessageBox.Show("선택된 매매방식이 없습니다.", "기본값 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else
                {
                    sell_set1_after.SelectedIndex = 0;
                    return;
                }
            }
        }

        private void Sell_set_after2_Leave(object sender, EventArgs e)
        {
            if (sell_set2_after.Text.Equals("") && sell_set1_after.Text.Equals(""))
            {
                if (sell_set1_after.Text.Equals(""))
                {
                    sell_set1_after.SelectedIndex = 0;
                    sell_set2_after.SelectedIndex = 5;
                    MessageBox.Show("선택된 매매방식이 없습니다.", "기본값 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else
                {
                    sell_set2_after.SelectedIndex = 5;
                    return;
                }
            }

            if (!sell_set2_after.Text.Equals("") && sell_set1_after.Text.Equals(""))
            {
                sell_set1_after.SelectedIndex = 0;
            }
        }

        //----------------------------소수점이 포함된 양의 숫자이거나 양의 정수인지 확인----------------------------------------

        private void Profit_percent_text_Leave(object sender, EventArgs e)
        {
            ValidateTextBoxInput(sender, e, profit_percent_text, "2.7");
        }

        private void Loss_percent_text_Leave(object sender, EventArgs e)
        {
            ValidateTextBoxInput(sender, e, loss_percent_text, "4");
        }

        private void Profit_ts_text_Leave(object sender, EventArgs e)
        {
            ValidateTextBoxInput(sender, e, profit_ts_text, "5");
        }

        private void Clear_sell_profit_text_Leave(object sender, EventArgs e)
        {
            ValidateTextBoxInput(sender, e, clear_sell_profit_text, "2");
        }

        private void Clear_sell_loss_text_Leave(object sender, EventArgs e)
        {
            ValidateTextBoxInput(sender, e, clear_sell_loss_text, "2");
        }

        private void ValidateTextBoxInput(object sender, EventArgs e, TextBox textBox, string defaultValue)
        {
            string input = textBox.Text;

            //입력된 값이 없을시
            if (string.IsNullOrWhiteSpace(input)) return;

            //숫자 혹은 소수점인지 확인
            bool hasDecimalPoint = false;
            int decimalPointCount = 0;

            foreach (char c in input)
            {
                if (char.IsDigit(c))
                {
                    continue;
                }
                else if (c == '.' && !hasDecimalPoint)
                {
                    hasDecimalPoint = true;
                    decimalPointCount++;
                }
                else
                {
                    textBox.Text = defaultValue;
                    MessageBox.Show("양수 혹은 점이 아닌 값이 있습니다.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            //소수점 형식 이탈 확인
            if (decimalPointCount > 1)
            {
                textBox.Text = defaultValue;
                MessageBox.Show("점을 한번만 입력하세요.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //범위 확인
            double intput_range = Convert.ToDouble(textBox.Text);
            double max = 1000000;
            double min = 0;
            if(intput_range < min || intput_range > max)
            {
                textBox.Text = defaultValue;
                MessageBox.Show("범위 : 0 이상  1,000,000 이하", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

        }

        //-----------------------------------양 정수 확인----------------------------------------

        private void Setting_Positive_numver(object sender, EventArgs e)
        {
            ValidateNumericInput(sender, e, setting_account_number, "0000000000", maxLength: 8);
        }

        private void Initial_balance_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput(sender, e, initial_balance, "1000000");
        }

        private void Maxbuy_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput(sender, e, maxbuy, "100000");
        }

        private void Maxbuy_acc_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput(sender, e, maxbuy_acc, "1");
        }

        private void Min_price_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput(sender, e, min_price, "1000", minValue: 0);
        }

        private void Max_price_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput(sender, e, max_price, "10000", minValue: Convert.ToInt32(min_price.Text) + 1);
        }

        private void Max_hold_text_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput(sender, e, max_hold_text, "0", maxValue: 50);
        }

        private void Profit_won_text_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput(sender, e, profit_won_text, "0");
        }

        private void Loss_won_text_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput(sender, e, loss_won_text, "0");
        }

        private void Term_for_buy_text_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput(sender, e, term_for_buy_text, "10000");
        }
        private void Term_for_sell_text_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput(sender, e, term_for_sell_text, "10000");
        }

        private void ValidateNumericInput(object sender, EventArgs e, TextBox textBox, string defaultValue, int? maxLength = null, int? minValue = null, int? maxValue = null)
        {
            string input = textBox.Text;

            foreach (char c in input)
            {
                if (!char.IsDigit(c))
                {
                    textBox.Text = defaultValue;
                    MessageBox.Show("정수가 아닌 값이 있습니다.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (maxLength.HasValue && input.Length != maxLength.Value)
            {
                textBox.Text = defaultValue;
                MessageBox.Show($"입력값은 {maxLength.Value}자리 숫자여야 합니다.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (minValue.HasValue && Convert.ToInt32(input) < minValue.Value)
            {
                textBox.Text = defaultValue;
                MessageBox.Show($"입력값은 {minValue.Value} 이상이어야 합니다.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (maxValue.HasValue && Convert.ToInt32(input) > maxValue.Value)
            {
                textBox.Text = defaultValue;
                MessageBox.Show($"입력값은 {maxValue.Value} 이하여야 합니다.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        //-----------------------------------양 혹은 음의 정수-------------------------------------
        private void Type0_start_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput2(sender, e, type0_start, "-5000");
        }
        private void Type0_end_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput2(sender, e, type0_end, "5000");
        }
        private void Type0_all_start_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput2(sender, e, type0_all_start, "-5000");
        }
        private void Type0_all_end_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput2(sender, e, type0_all_end, "5000");
        }
        private void Type0_isa_start_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput2(sender, e, type0_isa_start, "-5000");
        }
        private void Type0_isa_end_Leave(object sender, EventArgs e)
        {
            ValidateNumericInput2(sender, e, type0_isa_end, "5000");
        }

        private void ValidateNumericInput2(object sender, EventArgs e, TextBox textBox, string defaultValue)
        {
            string input = textBox.Text;

            int decimalMinus = 0;

            foreach (char c in input)
            {
                if (char.IsDigit(c))
                {
                    continue;
                }
                else if (c == '-')
                {
                    decimalMinus++;
                }
                else
                {
                    textBox.Text = defaultValue;
                    MessageBox.Show("마이너스와 숫자가 아닌 값이 아닌것이 있습니다.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            //마이너스 개수
            if (decimalMinus > 1)
            {
                textBox.Text = defaultValue;
                MessageBox.Show("마이너스을 한번만 입력하세요.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //범위 확인
            int intput_range = Convert.ToInt32(textBox.Text);
            int max = 1000000;
            int min = -1000000;
            if (intput_range < min || intput_range > max)
            {
                textBox.Text = defaultValue;
                MessageBox.Show("범위 :  -1,000,000 이상  1,000,000 이하", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

        }

        //-----------------------------------양 혹은 음 소수점 확인-------------------------------------
        private void Type1_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type1_start, "-5");
        }
        private void Type1_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type1_end, "5");
        }
        private void Type2_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type2_start, "-5");
        }
        private void Type2_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e,  type2_end, "5");
        }
        private void Type3_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type3_start, "-5");
        }
        private void Type3_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type3_end, "5");
        }
        private void Type4_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type4_start, "-5");
        }
        private void Type4_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type4_end, "5");
        }
        private void Type5_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type5_start, "-5");
        }
        private void Type5_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type5_end, "5");
        }

        private void Type1_all_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type1_all_start, "-5");
        }
        private void Type1_all_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type1_all_end, "5");
        }
        private void Type2_all_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type2_all_start, "-5");
        }
        private void Type2_all_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type2_all_end, "5");
        }
        private void Type3_all_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type3_all_start, "-5");
        }
        private void Type3_all_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type3_all_end, "5");
        }
        private void Type4_all_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type4_all_start, "-5");
        }
        private void Type4_all_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type4_all_end, "5");
        }
        private void Type5_all_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type5_all_start, "-5");
        }
        private void Type5_all_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type5_all_end, "5");
        }
        
        private void Type1_isa_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type1_isa_start, "-5");
        }
        private void Type1_isa_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type1_isa_end, "5");
        }
        private void Type2_isa_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type2_isa_start, "-5");
        }
        private void Type2_isa_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type2_isa_end, "5");
        }
        private void Type3_isa_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type3_isa_start, "-5");
        }
        private void Type3_isa_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type3_isa_end, "5");
        }
        private void Type4_isa_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type4_isa_start, "-5");
        }
        private void Type4_isa_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type4_isa_end, "5");
        }
        private void Type5_isa_start_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type5_isa_start, "-5");
        }
        private void Type5_isa_end_Leave(object sender, EventArgs e)
        {
            ValidatedecimalInput(sender, e, type5_isa_end, "5");
        }

        private void ValidatedecimalInput(object sender, EventArgs e, TextBox textBox, string defaultValue)
        {
            string input = textBox.Text;

            //입력된 값이 없을시
            if (string.IsNullOrWhiteSpace(input))
            {
                textBox.Text = defaultValue;
                MessageBox.Show("입력된 값이 없습니다.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //숫자 혹은 소수점인지 확인
            int decimalPointCount = 0;
            int decimalMinusCount = 0;

            foreach (char c in input)
            {
                if (char.IsDigit(c))
                {
                    continue;
                }
                else if (c == '.')
                {
                    decimalPointCount++;
                }
                else if (c == '-')
                {
                    decimalMinusCount++;
                }
                else
                {
                    textBox.Text = defaultValue;
                    MessageBox.Show("정수 혹은 점이 아닌 값이 있습니다.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            //소수점 형식 이탈 확인
            if (decimalPointCount > 1)
            {
                textBox.Text = defaultValue;
                MessageBox.Show("점을 한번만 입력하세요.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //마이너스 형식 이탈 확인
            if (decimalMinusCount > 1)
            {
                textBox.Text = defaultValue;
                MessageBox.Show("마이너스을 한번만 입력하세요.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //범위 확인
            int intput_range = Convert.ToInt32(textBox.Text);
            int max = 1000;
            int min = -1000;
            if (intput_range < min || intput_range > max)
            {
                textBox.Text = defaultValue;
                MessageBox.Show("범위 :  -1,000 이상  1,000 이하", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

        }      

        //-----------------------------------시간 입력 오류 확인----------------------------------------

        private void Market_start_time_Leave(object sender, EventArgs e)
        {
            ValidateTimeInput(sender, e, market_start_time, "08:45:00", new TimeSpan(8, 45, 0), new TimeSpan(18, 00, 00));
        }

        private void Market_end_time_Leave(object sender, EventArgs e)
        {
            ValidateTimeInput(sender, e, market_end_time, "18:00:00", new TimeSpan(8, 45, 0), new TimeSpan(18, 00, 00));
        }

        private void Buy_condition_start_Leave(object sender, EventArgs e)
        {
            ValidateTimeInput(sender, e, buy_condition_start, "09:00:00", new TimeSpan(9, 0, 0), new TimeSpan(15, 30, 00));
        }

        private void Buy_condition_end_Leave(object sender, EventArgs e)
        {
            ValidateTimeInput(sender, e, buy_condition_end, "15:30:00", new TimeSpan(9, 0, 0), new TimeSpan(15, 30, 00));
        }

        private void Sell_condition_start_Leave(object sender, EventArgs e)
        {
            ValidateTimeInput(sender, e, sell_condition_start, "09:00:00", new TimeSpan(9, 0, 0), new TimeSpan(18, 00, 00));
        }

        private void Sell_condition_end_Leave(object sender, EventArgs e)
        {
            ValidateTimeInput(sender, e, sell_condition_start, "18:00:00", new TimeSpan(9, 0, 0), new TimeSpan(18, 00, 00));
        }

        private void Clear_sell_start_Leave(object sender, EventArgs e)
        {
            ValidateTimeInput(sender, e, clear_sell_start, "09:00:00", new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
        }

        private void Clear_sell_end_Leave(object sender, EventArgs e)
        {
            ValidateTimeInput(sender, e, clear_sell_start, "18:00:00", new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
        }

        private void Dual_Time_Start_Leave(object sender, EventArgs e)
        {
            ValidateTimeInput(sender, e, Dual_Time_Start, "09:00:00", new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
        }

        private void Dual_Time_Stop_Leave(object sender, EventArgs e)
        {
            ValidateTimeInput(sender, e, Dual_Time_Start, "18:00:00", new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
        }

        private void ValidateTimeInput(object sender, EventArgs e, TextBox textBox, string defaultValue, TimeSpan minTime, TimeSpan maxTime)
        {
            string input = textBox.Text.Trim();

            DateTime inputTime;

            if (!DateTime.TryParse(input, out inputTime))
            {
                textBox.Text = defaultValue;
                MessageBox.Show("올바른 시간 형식(HH:mm:ss)으로 입력해주세요.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            TimeSpan inputTimeSpan = inputTime.TimeOfDay;

            if (inputTimeSpan < minTime || inputTimeSpan > maxTime)
            {
                textBox.Text = defaultValue;
                MessageBox.Show($"입력된 시간은 {minTime.ToString(@"hh\:mm\:ss")} ~ {maxTime.ToString(@"hh\:mm\:ss")} 범위를 벗어납니다.", "잘못된 입력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        //-----------------------------------최종확인----------------------------------------

        private bool check()
        {
            //로그인 및 자동실행

            //기본설정 및 추가 옵션 설정
            if (String.IsNullOrEmpty(account_list.Text))
            {
                MessageBox.Show("계좌번호를설정해주세요");
                return true;
            }


            //매매방식 및 매매방식(시간외)
            if (buy_set1.Text == "" || buy_set2.Text == "" || sell_set1.Text == "" || sell_set2.Text == "" || sell_set1_after.Text == "" || sell_set2_after.Text == "")
            {
                MessageBox.Show("모든 매매 방식을 설정해주세요.");
                return true;
            }

            if(buy_set1.Text == "지정가" && buy_set2.Text == "시장가")
            {
                MessageBox.Show("지정가로 선택시 호가를 선택하세요.");
                return true;
            }

            if (buy_set1.Text == "시장가" && !(buy_set2.Text == "시장가"))
            {
                MessageBox.Show("시장가로 선택시 시장가를 선택하세요.");
                return true;
            }

            if (sell_set1.Text == "지정가" && sell_set2.Text == "시장가")
            {
                MessageBox.Show("지정가로 선택시 호가를 선택하세요.");
                return true;
            }

            if (sell_set1.Text == "시장가" && !(sell_set2.Text == "시장가"))
            {
                MessageBox.Show("시장가로 선택시 시장가를 선택하세요.");
                return true;
            }


            //조건설정
            if (buy_condition.Checked && String.IsNullOrEmpty(Fomula_list_buy.Text) || sell_condition.Checked && String.IsNullOrEmpty(Fomula_list_sell.Text))
            {
                MessageBox.Show("저장하기 위해 조건식 1개 혹은 2개 필요");
                return true;
            }


            //매매설정


            //청산설정


            //지연설정


            //DUAL


            //지수 선물 연동


            //Telegram
            if (Telegram_Allow.Checked)
            {
                if (telegram_user_id.Text == "" || telegram_token.Text == "")
                {
                    MessageBox.Show("TELEGRAM USER_ID와 TOKE을 모두 입력하세요.");
                    return true;
                }
            }

            //KIS
            if (KIS_Independent.Checked && !KIS_Allow.Checked)
            {
                MessageBox.Show("KIS_Allow 를 체크하세요.");
                return true;
            }

            if (KIS_Allow.Checked)
            {
                if(KIS_Account.Text == "" || appkey.Text == "" || appsecret.Text == "" || kis_amount.Text == "")
                {
                    MessageBox.Show("KIS 계좌번호, appkey, appsecret, amount를 모두 입력하세요.");
                    return true;
                }

                if (int.TryParse(kis_amount.Text, out int result))
                {
                    if(result < 0)
                    {
                        MessageBox.Show("amount를 양의 정수로 입력하세요.");
                        return true;
                    }
                }
                else
                {
                    MessageBox.Show("amount를 양의 정수로 입력하세요.");
                    return true;
                }
            }

            return false;
        }

        //-----------------------------------조건식 동작----------------------------------------

        private void Fomula_list_buy_DropDown(object sender, EventArgs e)
        {
            //
            Fomula_list_buy.DropDownHeight = 1;
            Fomula_list_buy_Checked_box.Visible = true;
            Fomula_list_buy_Checked_box.BringToFront();

            //430 203
            //301 135
            Fomula_list_buy_Checked_box.BringToFront();
        }

        private void Fomula_list_buy_Checked_box_MouseLeave(object sender, EventArgs e)
        {
            Fomula_list_buy_Checked_box.Visible = false;
        }

        private void Fomula_list_buy_Checked_box_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            List<String> SelectedIndexText_join_tmp = new List<string>();

            if (e.NewValue == CheckState.Checked)
            {
                SelectedIndexText_join_tmp.Add(Fomula_list_buy_Checked_box.Items[e.Index].ToString());
            }

            //그 외 항목 중에서 체크 항목의 포함
            for (int i = 0; i < Fomula_list_buy_Checked_box.Items.Count; i++)
            {   
                if (Fomula_list_buy_Checked_box.GetItemChecked(i) && i != e.Index)
                {
                    SelectedIndexText_join_tmp.Add(Fomula_list_buy_Checked_box.Items[i].ToString());
                }
            }
            Fomula_list_buy.Text = String.Join(",", SelectedIndexText_join_tmp);
        }

        //-----------------------------------초기 실행---------------------------------------

        //초기 자동 실행
        private async Task setting_load_auto()
        {
            //조건식 로딩
            onReceiveConditionVer(Trade_Auto_Daishin.account, Trade_Auto_Daishin.arrCondition);

            //매도매수 목록 배치
            mode_hoo();

            match(utility.system_route);
        }

        //계좌 및 조건식 리스트 받아오기
        public void onReceiveConditionVer(string[] user_account, string[] Condition)
        {
            //계좌 추가
            for (int i = 0; i < user_account.Length; i++)
            {
                account_list.Items.Add(user_account[i]);
            }
            //매도 조건
            Fomula_list_sell.Items.AddRange(Condition);

            //매수 조건
            Fomula_list_buy_Checked_box.Items.AddRange(Condition);
        }

        private void mode_hoo()
        {
            //매수매도방식
            string[] mode = { "지정가", "시장가" };
            string[] mode2 = { "지정가" };
            string[] hoo = { "5호가", "4호가", "3호가", "2호가", "1호가", "현재가", "시장가", "-1호가", "-2호가", "-3호가", "-4호가", "-5호가" };
            string[] hoo2 = { "5호가", "4호가", "3호가", "2호가", "1호가", "현재가", "-1호가", "-2호가", "-3호가", "-4호가", "-5호가" };
            buy_set1.Items.AddRange(mode);
            buy_set2.Items.AddRange(hoo);
            sell_set1.Items.AddRange(mode);
            sell_set2.Items.AddRange(hoo);
            sell_set1_after.Items.AddRange(mode2);
            sell_set2_after.Items.AddRange(hoo2);
        }

        //-----------------------------------열기 및 반영----------------------------------------

        //setting 열기
        private void setting_load(object sender, EventArgs e)
        {
            //다이얼로그 창 뜨고 선택
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                String filepath = openFileDialog1.FileName;
                match(filepath);
            }
        }

        //즉시 반영
        private void setting_allow(object sender, EventArgs e)
        {
            setting_allow_after();
        }

        private async Task setting_allow_after()
        {
            await Task.Run(() =>
            {
                utility.system_route = setting_name.Text;
            });
            await utility.setting_load_auto();
            await Task.Run(() =>
            {
                _trade_Auto_Daishin.initial_allow();
                //
                _trade_Auto_Daishin.real_time_stop(true);
                //
                _trade_Auto_Daishin.auto_allow();
                //
                MessageBox.Show("반영이 완료되었습니다.");
            });
        }

        //-----------------------------------조건식 입력 오류 확인----------------------------------------

        //settubg  저장
        private void setting_save(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "파일 저장 경로 지정하세요";
            saveFileDialog.Filter = "텍스트 파일 (*.txt)|*.txt";

            //최종점검
            if (check()) return;

            //저장
            List<String> tmp = new List<String>();

            tmp.Add("아이디/" + real_id_text.Text);
            tmp.Add("비밀번호/" + real_password_text.Text);
            tmp.Add("공인인증서/" + real_cert_password_text.Text);
            tmp.Add("자동실행/" + Convert.ToString(auto_trade_allow.Checked));
            tmp.Add("자동운영시간/" + market_start_time.Text + "/" + market_end_time.Text);
            tmp.Add("계좌번호/" + account_list.Text);
            tmp.Add("초기자산/" + initial_balance.Text);
            tmp.Add("종목당매수금액/" + Convert.ToString(buy_per_price.Checked) + "/" + buy_per_price_text.Text);
            tmp.Add("종목당매수수량/" + Convert.ToString(buy_per_amount.Checked) + "/" + buy_per_amount_text.Text);
            tmp.Add("종목당매수비율/" + Convert.ToString(buy_per_percent.Checked) + "/" + buy_per_percent_text.Text);
            tmp.Add("종목당최대매수금액/" + maxbuy.Text);
            tmp.Add("최대매수종목수/" + maxbuy_acc.Text);
            tmp.Add("종목최소매수가/" + min_price.Text);
            tmp.Add("종목최대매수가/" + max_price.Text);
            tmp.Add("최대보유종목수/" + Convert.ToString(max_hold.Checked) + "/" + max_hold_text.Text);
            tmp.Add("당일중복매수금지/" + Convert.ToString(duplication_deny.Checked));
            tmp.Add("매수시간전검출매수금지/" + Convert.ToString(before_time_deny.Checked));
            tmp.Add("보유종목매수금지/" + Convert.ToString(hold_deny.Checked));
            //
            tmp.Add("매수조건;" + Convert.ToString(buy_condition.Checked) + ";" + buy_condition_start.Text + ";" + buy_condition_end.Text + ";" + Convert.ToString(buy_condition_index.Checked) + ";" + (Fomula_list_buy.Text.Equals("") ? "9999" : Fomula_list_buy.Text) + ";" + Convert.ToString(buy_mode_or.Checked) + ";" + Convert.ToString(buy_mode_and.Checked) + ";" + Convert.ToString(buy_mode_independent.Checked) + ";" + Convert.ToString(buy_mode_dual.Checked));
            tmp.Add("매도조건;" + Convert.ToString(sell_condition.Checked) + ";" + sell_condition_start.Text + ";" + sell_condition_end.Text + ";" + Convert.ToString(Fomula_list_sell.SelectedIndex) + ";" + (Fomula_list_sell.Text == "" ? "9999" : Fomula_list_sell.Text));
            tmp.Add("익절/" + Convert.ToString(profit_percent.Checked) + "/" + profit_percent_text.Text);
            tmp.Add("익절원/" + Convert.ToString(profit_won.Checked) + "/" + profit_won_text.Text);
            tmp.Add("익절TS/" + Convert.ToString(profit_ts.Checked) + "/" + profit_ts_text.Text);
            tmp.Add("익절동시호가/" + Convert.ToString(profit_after1.Checked));//익정동시호가
            tmp.Add("익절시간외단일가/" + Convert.ToString(profit_after2.Checked));//익절시간외단일가
            tmp.Add("손절/" + Convert.ToString(loss_percent.Checked) + "/" + loss_percent_text.Text);
            tmp.Add("손절원/" + Convert.ToString(loss_won.Checked) + "/" + loss_won_text.Text);
            tmp.Add("손절동시호가/" + Convert.ToString(loss_after1.Checked));//익정동시호가
            tmp.Add("손절시간외단일가/" + Convert.ToString(loss_after2.Checked));//익절시간외단일가
                                                                         //
            tmp.Add("전체청산/" + Convert.ToString(clear_sell.Checked) + "/" + clear_sell_start.Text + "/" + clear_sell_end.Text);
            tmp.Add("개별청산/" + Convert.ToString(clear_sell_mode.Checked));//익절시간외단일가
            tmp.Add("청산익절/" + Convert.ToString(clear_sell_profit.Checked) + "/" + clear_sell_profit_text.Text);
            tmp.Add("청산익절동시호가/" + Convert.ToString(clear_sell_profit_after1.Checked));
            tmp.Add("청산익절시간외단일가/" + Convert.ToString(clear_sell_profit_after2.Checked));
            tmp.Add("청산손절/" + Convert.ToString(clear_sell_loss.Checked) + "/" + clear_sell_loss_text.Text);
            tmp.Add("청산손절동시호가/" + Convert.ToString(clear_sell_loss_after1.Checked));
            tmp.Add("청산손절시간외단일가/" + Convert.ToString(clear_sell_loss_after2.Checked));
            tmp.Add("청산인덱스/" + Convert.ToString(clear_index.Checked));
            //
            tmp.Add("종목매수텀/" + Convert.ToString(term_for_buy.Checked) + "/" + term_for_buy_text.Text);
            tmp.Add("종목매도텀/" + Convert.ToString(term_for_sell.Checked) + "/" + term_for_sell_text.Text);
            tmp.Add("미체결매수취소/" + Convert.ToString(term_for_non_buy.Checked));
            tmp.Add("미체결매수취소/" + Convert.ToString(term_for_non_sell.Checked));
            //
            tmp.Add("매수설정/" + Convert.ToString(buy_set1.SelectedIndex) + "/" + Convert.ToString(buy_set2.SelectedIndex));
            tmp.Add("매도설정/" + Convert.ToString(sell_set1.SelectedIndex) + "/" + Convert.ToString(sell_set2.SelectedIndex));
            tmp.Add("매도설정_시간외/" + Convert.ToString(sell_set1_after.SelectedIndex) + "/" + Convert.ToString(sell_set2_after.SelectedIndex));
            //
            tmp.Add("외국인선물/" + Convert.ToString(Foreign_commodity.Checked));
            tmp.Add("코스피선물/" + Convert.ToString(kospi_commodity.Checked));
            tmp.Add("코스닥선물/" + Convert.ToString(kosdak_commodity.Checked));
            tmp.Add("DOW/" + Convert.ToString(dow_index.Checked));
            tmp.Add("SP/" + Convert.ToString(sp_index.Checked));
            tmp.Add("NASDAQ/" + Convert.ToString(nasdaq_index.Checked));
            //
            tmp.Add("type0/" + Convert.ToString(type0_selection.Checked) + "/" + type0_start.Text + "/" + type0_end.Text);
            tmp.Add("type1/" + Convert.ToString(type1_selection.Checked) + "/" + type1_start.Text + "/" + type1_end.Text);
            tmp.Add("type2/" + Convert.ToString(type2_selection.Checked) + "/" + type2_start.Text + "/" + type2_end.Text);
            tmp.Add("type3/" + Convert.ToString(type3_selection.Checked) + "/" + type3_start.Text + "/" + type3_end.Text);
            tmp.Add("type4/" + Convert.ToString(type4_selection.Checked) + "/" + type4_start.Text + "/" + type4_end.Text);
            tmp.Add("type5/" + Convert.ToString(type5_selection.Checked) + "/" + type5_start.Text + "/" + type5_end.Text);
            //
            tmp.Add("type0_ALL/" + Convert.ToString(type0_selection_all.Checked) + "/" + type0_all_start.Text + "/" + type0_all_end.Text);
            tmp.Add("type1_ALL/" + Convert.ToString(type1_selection_all.Checked) + "/" + type1_all_start.Text + "/" + type1_all_end.Text);
            tmp.Add("type2_ALL/" + Convert.ToString(type2_selection_all.Checked) + "/" + type2_all_start.Text + "/" + type2_all_end.Text);
            tmp.Add("type3_ALL/" + Convert.ToString(type3_selection_all.Checked) + "/" + type3_all_start.Text + "/" + type3_all_end.Text);
            tmp.Add("type4_ALL/" + Convert.ToString(type4_selection_all.Checked) + "/" + type4_all_start.Text + "/" + type4_all_end.Text);
            tmp.Add("type5_ALL/" + Convert.ToString(type5_selection_all.Checked) + "/" + type5_all_start.Text + "/" + type5_all_end.Text);
            //
            tmp.Add("Telegram_Allow/" + Convert.ToString(Telegram_Allow.Checked));
            tmp.Add("텔레그램ID/" + telegram_user_id.Text);
            tmp.Add("텔레그램token/" + telegram_token.Text);
            //
            tmp.Add("KIS_Allow/" + Convert.ToString(KIS_Allow.Checked));
            tmp.Add("KIS_Independent/" + Convert.ToString(KIS_Independent.Checked));
            tmp.Add("KIS_Account/" + KIS_Account.Text);
            tmp.Add(appkey.Text);
            tmp.Add(appsecret.Text);
            tmp.Add("KIS_amount/" + kis_amount.Text);
            //
            tmp.Add("Dual_Time/" + Convert.ToString(Dual_Time.Checked));
            tmp.Add("Dual_Time_Start/" + Dual_Time_Start.Text);
            tmp.Add("Dual_Time_Stop/" + Dual_Time_Stop.Text);
            tmp.Add("Dual_Index/" + Convert.ToString(dual_index.Checked));
            //
            tmp.Add("type0_Dual/" + Convert.ToString(type0_selection_isa.Checked) + "/" + type0_isa_start.Text + "/" + type0_isa_end.Text);
            tmp.Add("type1_Dual/" + Convert.ToString(type1_selection_isa.Checked) + "/" + type1_isa_start.Text + "/" + type1_isa_end.Text);
            tmp.Add("type2_Dual/" + Convert.ToString(type2_selection_isa.Checked) + "/" + type2_isa_start.Text + "/" + type2_isa_end.Text);
            tmp.Add("type3_Dual/" + Convert.ToString(type3_selection_isa.Checked) + "/" + type3_isa_start.Text + "/" + type3_isa_end.Text);
            tmp.Add("type4_Dual/" + Convert.ToString(type4_selection_isa.Checked) + "/" + type4_isa_start.Text + "/" + type4_isa_end.Text);
            tmp.Add("type5_Dual/" + Convert.ToString(type5_selection_isa.Checked) + "/" + type5_isa_start.Text + "/" + type5_isa_end.Text);

            // 저장할 파일 경로
            string filePath = $@"C:\Auto_Trade_Creon\Log\setting.txt";

            // StreamWriter를 사용하여 파일 저장
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.Write(String.Join("", tmp));
                    writer.Close();
                    MessageBox.Show("파일이 저장되었습니다: " + filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 저장 중 오류 발생: " + ex.Message);
            }
        }

        //매칭
        private void match(string filepath)
        {
            StreamReader reader = new StreamReader(filepath);

            //파일 주소 확인
            setting_name.Text = filepath;

            //아이디
            String[] real_id_tmp = reader.ReadLine().Split('/');
            real_id_text.Text = real_id_tmp[1];

            //비밀번호
            String[] real_password_tmp = reader.ReadLine().Split('/');
            real_password_text.Text = real_password_tmp[1];

            //공인인증서 비밀번호
            String[] real_cert_password_tmp = reader.ReadLine().Split('/');
            real_cert_password_text.Text = real_cert_password_tmp[1];

            //자동실행
            String[] auto_trade_allow_tmp = reader.ReadLine().Split('/');
            auto_trade_allow.Checked = Convert.ToBoolean(auto_trade_allow_tmp[1]);

            //자동 운영 시간
            String[] time_tmp = reader.ReadLine().Split('/');
            market_start_time.Text = time_tmp[1];
            market_end_time.Text = time_tmp[2];

            //계좌 번호
            String[] account_tmp = reader.ReadLine().Split('/');
            setting_account_number.Text = account_tmp[1];

            //초기 자산
            String[] balance_tmp = reader.ReadLine().Split('/');
            initial_balance.Text = balance_tmp[1];

            //종목당매수금액
            String[] buy_per_price_tmp = reader.ReadLine().Split('/');
            buy_per_price.Checked = Convert.ToBoolean(buy_per_price_tmp[1]);
            buy_per_price_text.Text = buy_per_price_tmp[2];

            //종목당매수수량
            String[] buy_per_amount_tmp = reader.ReadLine().Split('/');
            buy_per_amount.Checked = Convert.ToBoolean(buy_per_amount_tmp[1]);
            buy_per_amount_text.Text = buy_per_amount_tmp[2];

            //종목당매수비율
            String[] buy_per_percemt_tmp = reader.ReadLine().Split('/');
            buy_per_percent.Checked = Convert.ToBoolean(buy_per_percemt_tmp[1]);
            buy_per_percent_text.Text = buy_per_percemt_tmp[2];

            //종목당최대매수금액
            String[] maxbuy_tmp = reader.ReadLine().Split('/');
            maxbuy.Text = maxbuy_tmp[1];

            //최대매수종목수
            String[] maxbuy_acc_tmp = reader.ReadLine().Split('/');
            maxbuy_acc.Text = maxbuy_acc_tmp[1];

            //종목최소매수가
            String[] min_price_tmp = reader.ReadLine().Split('/');
            min_price.Text = min_price_tmp[1];

            //종목최대매수가
            String[] max_price_tmp = reader.ReadLine().Split('/');
            max_price.Text = max_price_tmp[1];

            //최대보유종목수
            String[] max_hold_tmp = reader.ReadLine().Split('/');
            max_hold.Checked = Convert.ToBoolean(max_hold_tmp[1]);
            max_hold_text.Text = max_hold_tmp[2];

            //당일중복매수금지
            String[] duplication_deny_tmp = reader.ReadLine().Split('/');
            duplication_deny.Checked = Convert.ToBoolean(duplication_deny_tmp[1]);

            //매수시간전검출매수금지
            String[] before_time_deny_tmp = reader.ReadLine().Split('/');
            before_time_deny.Checked = Convert.ToBoolean(before_time_deny_tmp[1]);

            //보유종목매수금지
            String[] hold_deny_tmp = reader.ReadLine().Split('/');
            hold_deny.Checked = Convert.ToBoolean(hold_deny_tmp[1]);

            //매수조건
            String[] buy_condition_tmp = reader.ReadLine().Split(';');
            buy_condition.Checked = Convert.ToBoolean(buy_condition_tmp[1]);
            buy_condition_start.Text = buy_condition_tmp[2];
            buy_condition_end.Text = buy_condition_tmp[3];
            buy_condition_index.Checked = Convert.ToBoolean(buy_condition_tmp[4]);
            //
            if (!buy_condition_tmp[5].Equals("9999"))
            {
                string[] Selectedtext_temp = buy_condition_tmp[5].Split(',');
                string SelectedIndexTextJoin_temp = "";
                for (int i = 0; i < Selectedtext_temp.Length; i++)
                {
                    for (int j = 0; j < Fomula_list_buy_Checked_box.Items.Count; j++)
                    {
                        if (Fomula_list_buy_Checked_box.Items[j].ToString().Equals(Selectedtext_temp[i]))
                        {
                            Fomula_list_buy_Checked_box.SetItemChecked(j, true);
                            SelectedIndexTextJoin_temp += Selectedtext_temp[i] + ",";
                            break;
                        }
                    }
                }
                if (!SelectedIndexTextJoin_temp.Equals("")) SelectedIndexTextJoin_temp = SelectedIndexTextJoin_temp.Remove(SelectedIndexTextJoin_temp.Length - 1);
                Fomula_list_buy.Text = SelectedIndexTextJoin_temp;
            }
            //
            buy_mode_or.Checked = Convert.ToBoolean(buy_condition_tmp[6]);
            buy_mode_and.Checked = Convert.ToBoolean(buy_condition_tmp[7]);
            buy_mode_independent.Checked = Convert.ToBoolean(buy_condition_tmp[8]);
            buy_mode_dual.Checked = Convert.ToBoolean(buy_condition_tmp[9]);

            //매도조건
            String[] sell_condition_tmp = reader.ReadLine().Split(';');
            sell_condition.Checked = Convert.ToBoolean(sell_condition_tmp[1]);
            sell_condition_start.Text = sell_condition_tmp[2];
            sell_condition_end.Text = sell_condition_tmp[3];
            Fomula_list_sell.SelectedIndex = Convert.ToInt32(sell_condition_tmp[4]);
            Fomula_list_sell.Text = sell_condition_tmp[5];

            //익절
            String[] profit_percent_tmp = reader.ReadLine().Split('/');
            profit_percent.Checked = Convert.ToBoolean(profit_percent_tmp[1]);
            profit_percent_text.Text = profit_percent_tmp[2];

            //익절원
            String[] profit_won_tmp = reader.ReadLine().Split('/');
            profit_won.Checked = Convert.ToBoolean(profit_won_tmp[1]);
            profit_won_text.Text = profit_won_tmp[2];

            //익절TS(대기)
            String[] profit_ts_tmp = reader.ReadLine().Split('/');
            profit_ts.Checked = Convert.ToBoolean(profit_ts_tmp[1]);
            profit_ts_text.Text = profit_ts_tmp[2];

            //익정동시호가
            String[] profit_after1_tmp = reader.ReadLine().Split('/');
            profit_after1.Checked = Convert.ToBoolean(profit_after1_tmp[1]);

            //익절시간외단일가
            String[] profit_after2_tmp = reader.ReadLine().Split('/');
            profit_after2.Checked = Convert.ToBoolean(profit_after2_tmp[1]);

            //손절
            String[] loss_percent_tmp = reader.ReadLine().Split('/');
            loss_percent.Checked = Convert.ToBoolean(loss_percent_tmp[1]);
            loss_percent_text.Text = loss_percent_tmp[2];

            //손절원
            String[] loss_won_tmp = reader.ReadLine().Split('/');
            loss_won.Checked = Convert.ToBoolean(loss_won_tmp[1]);
            loss_won_text.Text = loss_won_tmp[2];

            //손절동시호가
            String[] loss_after1_tmp = reader.ReadLine().Split('/');
            loss_after1.Checked = Convert.ToBoolean(loss_after1_tmp[1]);

            //손절시간외단일가
            String[] loss_after2_tmp = reader.ReadLine().Split('/');
            loss_after2.Checked = Convert.ToBoolean(loss_after2_tmp[1]);

            //전체청산
            String[] clear_sell_tmp = reader.ReadLine().Split('/');
            clear_sell.Checked = Convert.ToBoolean(clear_sell_tmp[1]);
            clear_sell_start.Text = clear_sell_tmp[2];
            clear_sell_end.Text = clear_sell_tmp[3];

            //청산모드선택
            String[] clear_sell_mode_tmp = reader.ReadLine().Split('/');
            clear_sell_mode.Checked = Convert.ToBoolean(clear_sell_mode_tmp[1]);

            //청산익절
            String[] clear_sell_profit_tmp = reader.ReadLine().Split('/');
            clear_sell_profit.Checked = Convert.ToBoolean(clear_sell_profit_tmp[1]);
            clear_sell_profit_text.Text = clear_sell_profit_tmp[2];

            //청산익절동시호가
            String[] clear_sell_profit_after1_tmp = reader.ReadLine().Split('/');
            clear_sell_profit_after1.Checked = Convert.ToBoolean(clear_sell_profit_after1_tmp[1]);

            //청산익절시간외단일가
            String[] clear_sell_profit_after2_tmp = reader.ReadLine().Split('/');
            clear_sell_profit_after2.Checked = Convert.ToBoolean(clear_sell_profit_after2_tmp[1]);

            //청산손절
            String[] clear_sell_loss_tmp = reader.ReadLine().Split('/');
            clear_sell_loss.Checked = Convert.ToBoolean(clear_sell_loss_tmp[1]);
            clear_sell_loss_text.Text = clear_sell_loss_tmp[2];

            //청산손절동시호가
            String[] clear_sell_loss_after1_tmp = reader.ReadLine().Split('/');
            clear_sell_loss_after1.Checked = Convert.ToBoolean(clear_sell_loss_after1_tmp[1]);

            //청산익절시간외단일가
            String[] clear_sell_loss_after2_tmp = reader.ReadLine().Split('/');
            clear_sell_loss_after2.Checked = Convert.ToBoolean(clear_sell_loss_after2_tmp[1]);

            //청산인덱스
            String[] clear_index_tmp = reader.ReadLine().Split('/');
            clear_index.Checked = Convert.ToBoolean(clear_index_tmp[1]);

            //종목매수텀(대기)
            String[] term_for_buy_tmp = reader.ReadLine().Split('/');
            term_for_buy.Checked = Convert.ToBoolean(term_for_buy_tmp[1]);
            term_for_buy_text.Text = term_for_buy_tmp[2];

            //종목매도텀(대기)
            String[] term_for_sell_tmp = reader.ReadLine().Split('/');
            term_for_sell.Checked = Convert.ToBoolean(term_for_sell_tmp[1]);
            term_for_sell_text.Text = term_for_sell_tmp[2];

            //미체결매수취소(대기)
            String[] term_for_non_buy_tmp = reader.ReadLine().Split('/');
            term_for_non_buy.Checked = Convert.ToBoolean(term_for_non_buy_tmp[1]);

            //미체결매도취소(대기)
            String[] term_for_non_sell_tmp = reader.ReadLine().Split('/');
            term_for_non_sell.Checked = Convert.ToBoolean(term_for_non_sell_tmp[1]);

            //매수설정
            String[] buy_set_tmp = reader.ReadLine().Split('/');
            buy_set1.SelectedIndex = Convert.ToInt32(buy_set_tmp[1]);
            buy_set2.SelectedIndex = Convert.ToInt32(buy_set_tmp[2]);

            //매도설정
            String[] sell_set_tmp = reader.ReadLine().Split('/');
            sell_set1.SelectedIndex = Convert.ToInt32(sell_set_tmp[1]);
            sell_set2.SelectedIndex = Convert.ToInt32(sell_set_tmp[2]);

            //매도설정
            String[] sell_set_after_tmp = reader.ReadLine().Split('/');
            sell_set1_after.SelectedIndex = Convert.ToInt32(sell_set_after_tmp[1]);
            sell_set2_after.SelectedIndex = Convert.ToInt32(sell_set_after_tmp[2]);

            //외국누적선물
            String[] Foreign_commodity_tmp = reader.ReadLine().Split('/');
            Foreign_commodity.Checked = Convert.ToBoolean(Foreign_commodity_tmp[1]);

            //코스피선물
            String[] kospi_commodity_tmp = reader.ReadLine().Split('/');
            kospi_commodity.Checked = Convert.ToBoolean(kospi_commodity_tmp[1]);

            //코스닥선물
            String[] kosdak_commodity_tmp = reader.ReadLine().Split('/');
            kosdak_commodity.Checked = Convert.ToBoolean(kosdak_commodity_tmp[1]);

            //DOW30
            String[] dow_index_tmp = reader.ReadLine().Split('/');
            dow_index.Checked = Convert.ToBoolean(dow_index_tmp[1]);

            //SP500
            String[] sp_index_tmp = reader.ReadLine().Split('/');
            sp_index.Checked = Convert.ToBoolean(sp_index_tmp[1]);

            //NASDAQ100
            String[] nasdaq_index_tmp = reader.ReadLine().Split('/');
            nasdaq_index.Checked = Convert.ToBoolean(nasdaq_index_tmp[1]);

            //#0
            String[] type0_selection_tmp = reader.ReadLine().Split('/');
            type0_selection.Checked = Convert.ToBoolean(type0_selection_tmp[1]);
            type0_start.Text = Convert.ToString(type0_selection_tmp[2]);
            type0_end.Text = Convert.ToString(type0_selection_tmp[3]);

            //#1
            String[] type1_selection_tmp = reader.ReadLine().Split('/');
            type1_selection.Checked = Convert.ToBoolean(type1_selection_tmp[1]);
            type1_start.Text = Convert.ToString(type1_selection_tmp[2]);
            type1_end.Text = Convert.ToString(type1_selection_tmp[3]);

            //#2
            String[] type2_selection_tmp = reader.ReadLine().Split('/');
            type2_selection.Checked = Convert.ToBoolean(type2_selection_tmp[1]);
            type2_start.Text = Convert.ToString(type2_selection_tmp[2]);
            type2_end.Text = Convert.ToString(type2_selection_tmp[3]);

            //#3
            String[] type3_selection_tmp = reader.ReadLine().Split('/');
            type3_selection.Checked = Convert.ToBoolean(type3_selection_tmp[1]);
            type3_start.Text = Convert.ToString(type3_selection_tmp[2]);
            type3_end.Text = Convert.ToString(type3_selection_tmp[3]);

            //#4
            String[] type4_selection_tmp = reader.ReadLine().Split('/');
            type4_selection.Checked = Convert.ToBoolean(type4_selection_tmp[1]);
            type4_start.Text = Convert.ToString(type4_selection_tmp[2]);
            type4_end.Text = Convert.ToString(type4_selection_tmp[3]);

            //#5
            String[] type5_selection_tmp = reader.ReadLine().Split('/');
            type5_selection.Checked = Convert.ToBoolean(type5_selection_tmp[1]);
            type5_start.Text = Convert.ToString(type5_selection_tmp[2]);
            type5_end.Text = Convert.ToString(type5_selection_tmp[3]);

            //#0
            String[] type0_selection_all_tmp = reader.ReadLine().Split('/');
            type0_selection_all.Checked = Convert.ToBoolean(type0_selection_all_tmp[1]);
            type0_all_start.Text = Convert.ToString(type0_selection_all_tmp[2]);
            type0_all_end.Text = Convert.ToString(type0_selection_all_tmp[3]);

            //#1
            String[] type1_selection_all_tmp = reader.ReadLine().Split('/');
            type1_selection_all.Checked = Convert.ToBoolean(type1_selection_all_tmp[1]);
            type1_all_start.Text = Convert.ToString(type1_selection_all_tmp[2]);
            type1_all_end.Text = Convert.ToString(type1_selection_all_tmp[3]);

            //#2
            String[] type2_selection_all_tmp = reader.ReadLine().Split('/');
            type2_selection_all.Checked = Convert.ToBoolean(type2_selection_all_tmp[1]);
            type2_all_start.Text = Convert.ToString(type2_selection_all_tmp[2]);
            type2_all_end.Text = Convert.ToString(type2_selection_all_tmp[3]);

            //#3
            String[] type3_selection_all_tmp = reader.ReadLine().Split('/');
            type3_selection_all.Checked = Convert.ToBoolean(type3_selection_all_tmp[1]);
            type3_all_start.Text = Convert.ToString(type3_selection_all_tmp[2]);
            type3_all_end.Text = Convert.ToString(type3_selection_all_tmp[3]);

            //#4
            String[] type4_selection_all_tmp = reader.ReadLine().Split('/');
            type4_selection_all.Checked = Convert.ToBoolean(type4_selection_all_tmp[1]);
            type4_all_start.Text = Convert.ToString(type4_selection_all_tmp[2]);
            type4_all_end.Text = Convert.ToString(type4_selection_all_tmp[3]);

            //#5
            String[] type5_selection_all_tmp = reader.ReadLine().Split('/');
            type5_selection_all.Checked = Convert.ToBoolean(type5_selection_all_tmp[1]);
            type5_all_start.Text = Convert.ToString(type5_selection_all_tmp[2]);
            type5_all_end.Text = Convert.ToString(type5_selection_all_tmp[3]);

            //텔레그램Telegram_Allow
            String[] Telegram_Allow_tmp = reader.ReadLine().Split('/');
            Telegram_Allow.Checked = Convert.ToBoolean(Telegram_Allow_tmp[1]);

            //텔레그램ID
            String[] telegram_user_id_tmp = reader.ReadLine().Split('/');
            telegram_user_id.Text = telegram_user_id_tmp[1];

            //텔레그램TOKEN
            String[] telegram_token_tmp = reader.ReadLine().Split('/');
            telegram_token.Text = telegram_token_tmp[1];

            //한국투자증권KIS_Allow
            String[] KIS_Allow_tmp = reader.ReadLine().Split('/');
            KIS_Allow.Checked = Convert.ToBoolean(KIS_Allow_tmp[1]);

            //한국투자증권KIS_AllowKIS_Independent_tmp
            String[] KIS_Independent_tmp = reader.ReadLine().Split('/');
            KIS_Independent.Checked = Convert.ToBoolean(KIS_Independent_tmp[1]);

            //한국투자증권Account
            String[] KIS_Account_tmp = reader.ReadLine().Split('/');
            KIS_Account.Text = KIS_Account_tmp[1];

            //한국투자증권appkey
            String KIS_appkey_tmp = reader.ReadLine();
            appkey.Text = KIS_appkey_tmp;

            //한국투자증권appsecret
            String KIS_appsecret_tmp = reader.ReadLine();
            appsecret.Text = KIS_appsecret_tmp;

            //한국투자증권N등분
            String[] KIS_amount_tmp = reader.ReadLine().Split('/');
            kis_amount.Text = KIS_amount_tmp[1];

            //Dual_Time
            String[] Dual_Time_tmp = reader.ReadLine().Split('/');
            Dual_Time.Checked = Convert.ToBoolean(Dual_Time_tmp[1]);

            //Dual_Time_Start
            String[] Dual_Time_Start_tmp = reader.ReadLine().Split('/');
            Dual_Time_Start.Text = Dual_Time_Start_tmp[1];

            //Dual_Time_Stop
            String[] Dual_Time_Stop_tmp = reader.ReadLine().Split('/');
            Dual_Time_Stop.Text = Dual_Time_Stop_tmp[1];

            //Dual_Index
            String[] Dual_Index_tmp = reader.ReadLine().Split('/');
            dual_index.Checked = Convert.ToBoolean(Dual_Index_tmp[1]);

            //#1
            String[] type0_selection_isa_tmp = reader.ReadLine().Split('/');
            type0_selection_isa.Checked = Convert.ToBoolean(type0_selection_isa_tmp[1]);
            type0_isa_start.Text = Convert.ToString(type0_selection_isa_tmp[2]);
            type0_isa_end.Text = Convert.ToString(type0_selection_isa_tmp[3]);

            //#1
            String[] type1_selection_isa_tmp = reader.ReadLine().Split('/');
            type1_selection_isa.Checked = Convert.ToBoolean(type1_selection_isa_tmp[1]);
            type1_isa_start.Text = Convert.ToString(type1_selection_isa_tmp[2]);
            type1_isa_end.Text = Convert.ToString(type1_selection_isa_tmp[3]);

            //#2
            String[] type2_selection_isa_tmp = reader.ReadLine().Split('/');
            type2_selection_isa.Checked = Convert.ToBoolean(type2_selection_isa_tmp[1]);
            type2_isa_start.Text = Convert.ToString(type2_selection_isa_tmp[2]);
            type2_isa_end.Text = Convert.ToString(type2_selection_isa_tmp[3]);

            //#3
            String[] type3_selection_isa_tmp = reader.ReadLine().Split('/');
            type3_selection_isa.Checked = Convert.ToBoolean(type3_selection_isa_tmp[1]);
            type3_isa_start.Text = Convert.ToString(type3_selection_isa_tmp[2]);
            type3_isa_end.Text = Convert.ToString(type3_selection_isa_tmp[3]);

            //#4
            String[] type4_selection_isa_tmp = reader.ReadLine().Split('/');
            type4_selection_isa.Checked = Convert.ToBoolean(type4_selection_isa_tmp[1]);
            type4_isa_start.Text = Convert.ToString(type4_selection_isa_tmp[2]);
            type4_isa_end.Text = Convert.ToString(type4_selection_isa_tmp[3]);

            //#5
            String[] type5_selection_isa_tmp = reader.ReadLine().Split('/');
            type5_selection_isa.Checked = Convert.ToBoolean(type5_selection_isa_tmp[1]);
            type5_isa_start.Text = Convert.ToString(type5_selection_isa_tmp[2]);
            type5_isa_end.Text = Convert.ToString(type5_selection_isa_tmp[3]);

            reader.Close();
        }

        //-----------------------------------Tekegram 테스트----------------------------------------

        //Telegram 테스트
        private void telegram_test(object sender, EventArgs e)
        {
            string test_message = "TELEGRAM CONNECTION CHECK";
            string urlString = $"https://api.telegram.org/bot{telegram_token.Text}/sendMessage?chat_id={telegram_user_id.Text}&text={test_message}";

            try
            {
                WebRequest request = WebRequest.Create(urlString);
                Stream stream = request.GetResponse().GetResponseStream();

            }
            catch (WebException ex)
            {
                // HTTP 상태 코드 확인
                HttpWebResponse response = (HttpWebResponse)ex.Response;
                if (response != null)
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                            // 인증 실패 (잘못된 토큰 또는 사용자 ID)
                            MessageBox.Show("인증 실패: 토큰과 사용자 ID를 확인하세요.");
                            break;
                        case HttpStatusCode.BadRequest:
                            // 잘못된 요청 (메시지 내용이 비어있는 경우 등)
                            MessageBox.Show("잘못된 요청: 메시지 내용을 확인하세요.");
                            break;
                        default:
                            MessageBox.Show($"오류: {response.StatusCode} - {response.StatusDescription}");
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("응답이 없습니다.");
                }
            }
            catch (Exception ex)
            {
                // 기타 예외 처리
                MessageBox.Show($"오류: {ex.Message}");
            }
        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }
    }
}
