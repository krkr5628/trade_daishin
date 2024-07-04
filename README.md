# Trade_daishin
# 다음 사항
- 모든 기능 테스트 필요

# 예정 사항
- 시나리오 모드, 관심 종목 모드
- 한국투자증권 연동
- TradingView Webhook 연동

# 자동 매매 서비스
❓ Problem1 : 증권사 자체 프로그램을 매일 켜야하거나 수수료가 비싸다. 
               => 키움 Katch => 매일 켜야함
               => 대신(크레온) 서버매매 => 각 방향 0.19%

❓ Problem2 : 상용 프로그램은 비용적인 것도 있지만 비율로 거래하는 것이 없다. => 개수 혹은 금액으로 거래

❓ Problem3 : 키움을 기반으로 하는 것은 많아도 대신이나 한국투자증권은 없다.

‼ Idea1 : 원하는 기능이 있는 증권사 API 기반 매매 프로그램을 만들고 이를 기반으로 다른 증권사 것도 만들자.

‼ Idea2 : 중단된 퀀트 프로그램을 지속적으로 개발하여 해당 매매 프로그램과 연동해 사용할 수 있도록 하자.

💯 Solution : API 기반 자체 매매 프로그램 + 퀀트 프로그램

# 사용기술
- C#, Visual Studio(2019), Github

# 사용 API
- Crean Api
- KIS Open_API(예정)

# 참고 자료
- 팡규의 자동 매매 프로그램, 번개트레이더
- 기타 : GPT 4o, Claude Sonnet

# FORM0 : 자동실행 및 업데이트
![image](https://github.com/krkr5628/trade_daishin/assets/75410553/e132d15e-d9e1-40a8-8594-83193ffa7f37)

# FORM1 : 메인 화면
![image](https://github.com/krkr5628/trade_daishin/assets/75410553/4ca53edd-73fe-40ac-a4e1-e8660e3135a5)

# FORM2 : 매매 설정
![image](https://github.com/krkr5628/trade_daishin/assets/75410553/d49b23cc-91e0-460a-8fb0-4b7afc6250e6)

# FORM3 : 매매 확인
![image](https://github.com/krkr5628/trade_daishin/assets/75410553/7c7e8635-0493-4530-a218-7ca3c88e793a)

# FORM4 : 로그 확인
![image](https://github.com/krkr5628/trade_daishin/assets/75410553/ad3000c8-926a-469c-9118-1447d5482d79)

# FORM5 : 업데이트 및 동의사항
![image](https://github.com/krkr5628/trade_daishin/assets/75410553/45af573c-95ee-4a1a-8634-f4c18ebd9f99)

# 특징
- 증권사 실시간 조건 검색을 활용하여 매수 매도 가능
- 클라우드 VM을 통해 24시간 자동 거래 가능 => API 업데이트 발생시 수동 재실행
- 정

# KIS(개발중)
- 한국투자증권의 OPEN_API를 통해 거래
- 분할 숫자를 넣어서 예수금의 N비율만큼 거래

# TradingView Webhook(개발중)
- TradingView Webhook 기반 매수 거래

# 업데이트 및 동의사항(개발중)
- 누적 업데이트 사항
- 사용상 책임소재에 대한 동의사항
- 인증번호 확인(개발중)
