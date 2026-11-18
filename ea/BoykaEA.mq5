//+------------------------------------------------------------------+
//|  BoykaEA.mq5                                                     |
//|  Boyka AI — MQL5 Expert Advisor                                  |
//|  Checkpoint 2.3                                                  |
//|                                                                  |
//|  PURPOSE:                                                        |
//|    Connects a MetaTrader 5 demo account to the Boyka AI backend. |
//|    Polls for pending trade commands, executes them, and reports  |
//|    results back to the backend via REST.                         |
//|                                                                  |
//|  WARNING: DEMO ACCOUNTS ONLY.                                    |
//|    This EA checks ACCOUNT_TRADE_MODE_DEMO on startup and refuses |
//|    to run on live accounts.  It is not tested or warranted for   |
//|    real-money trading.                                           |
//|                                                                  |
//|  MT5 TERMINAL SETUP (required before running):                  |
//|    1. Open MT5 → Tools → Options → Expert Advisors.             |
//|    2. Enable "Allow WebRequest for listed URLs".                 |
//|    3. Add your BackendBaseUrl (e.g. https://localhost:7161) to  |
//|       the URL allow-list.                                        |
//|    Without this the EA cannot communicate with the backend.      |
//+------------------------------------------------------------------+

#property copyright "Boyka AI"
#property link      "https://github.com/BoykaAI"
#property version   "1.00"
#property strict

//--- Input parameters
input string BackendBaseUrl         = "https://localhost:7161"; // Backend API base URL
input string ApiKey                 = "";                       // EA API key (from POST /api/mt5/registrations)
input int    PollingIntervalSeconds = 5;                        // How often (seconds) to poll for commands
input bool   ExecutionEnabled       = false;                    // Safety switch: false = log commands but do not place orders
input string EaVersion              = "1.0.0";                 // EA version reported to backend in heartbeats
input int    MagicNumber            = 20261117;                 // Magic number stamped on every EA order
input int    SlippagePoints         = 10;                       // Maximum allowed slippage in points

#define HTTP_TIMEOUT_MS             5000

//+------------------------------------------------------------------+
//|  OnInit — called when EA is attached or terminal restarts        |
//+------------------------------------------------------------------+
int OnInit()
{
    //--- Refuse to run on live accounts
    if(AccountInfoInteger(ACCOUNT_TRADE_MODE) != ACCOUNT_TRADE_MODE_DEMO)
    {
        Print("[BoykaEA] ERROR: This EA may only run on a DEMO account. Aborting.");
        return INIT_FAILED;
    }

    //--- API key is mandatory
    if(StringLen(ApiKey) == 0)
    {
        Print("[BoykaEA] ERROR: ApiKey input is empty. "
              "Provision a key via POST /api/mt5/registrations and paste it in EA inputs.");
        return INIT_FAILED;
    }

    //--- Start the polling timer
    EventSetTimer(PollingIntervalSeconds);

    //--- Initial heartbeat so the backend knows we are online
    SendHeartbeat();

    Print("[BoykaEA] Initialized. Polling every ", PollingIntervalSeconds, " second(s).");
    return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//|  OnDeinit — called when EA is removed or terminal shuts down     |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
    EventKillTimer();
    Print("[BoykaEA] Stopped. Reason code: ", reason);
}

//+------------------------------------------------------------------+
//|  OnTimer — the main polling loop fired by EventSetTimer          |
//+------------------------------------------------------------------+
void OnTimer()
{
    SendHeartbeat();
    SendAccountSnapshot();
    PollAndExecuteCommand();
    SendPositionSnapshot();
}

//+------------------------------------------------------------------+
//|  SendHeartbeat                                                   |
//|  Reports EA liveness and basic account metrics to the backend.   |
//+------------------------------------------------------------------+
void SendHeartbeat()
{
    double balance  = AccountInfoDouble(ACCOUNT_BALANCE);
    double equity   = AccountInfoDouble(ACCOUNT_EQUITY);
    string currency = AccountInfoString(ACCOUNT_CURRENCY);

    string json = StringFormat(
        "{\"eaVersion\":\"%s\","
        "\"terminalConnected\":true,"
        "\"accountBalance\":%.8f,"
        "\"accountEquity\":%.8f,"
        "\"accountCurrency\":\"%s\"}",
        EscapeJson(EaVersion),
        balance,
        equity,
        EscapeJson(currency));

    string responseBody;
    int statusCode = HttpPost(BackendBaseUrl + "/api/mt5/heartbeat", json, responseBody);

    if(statusCode == -1)
        Print("[BoykaEA] SendHeartbeat: network error or URL not in allowed list.");
    else
        Print("[BoykaEA] SendHeartbeat: HTTP ", statusCode);
}

//+------------------------------------------------------------------+
//|  SendAccountSnapshot                                             |
//|  Posts a detailed account snapshot to the backend.              |
//+------------------------------------------------------------------+
void SendAccountSnapshot()
{
    double balance    = AccountInfoDouble(ACCOUNT_BALANCE);
    double equity     = AccountInfoDouble(ACCOUNT_EQUITY);
    double margin     = AccountInfoDouble(ACCOUNT_MARGIN);
    double freeMargin = AccountInfoDouble(ACCOUNT_MARGIN_FREE);
    long   accountLogin = AccountInfoInteger(ACCOUNT_LOGIN);
    string company      = AccountInfoString(ACCOUNT_COMPANY);
    string currency   = AccountInfoString(ACCOUNT_CURRENCY);
    string server     = AccountInfoString(ACCOUNT_SERVER);
    long   leverage   = AccountInfoInteger(ACCOUNT_LEVERAGE);

    string json = StringFormat(
        "{\"accountLogin\":%I64d,"
        "\"brokerCompany\":\"%s\","
        "\"balance\":%.8f,"
        "\"equity\":%.8f,"
        "\"margin\":%.8f,"
        "\"freeMargin\":%.8f,"
        "\"currency\":\"%s\","
        "\"serverName\":\"%s\","
        "\"leverage\":%d}",
        accountLogin,
        EscapeJson(company),
        balance,
        equity,
        margin,
        freeMargin,
        EscapeJson(currency),
        EscapeJson(server),
        (int)leverage);

    string responseBody;
    int statusCode = HttpPost(BackendBaseUrl + "/api/mt5/account-snapshots", json, responseBody);

    if(statusCode == -1)
        Print("[BoykaEA] SendAccountSnapshot: network error or URL not in allowed list.");
    else
        Print("[BoykaEA] SendAccountSnapshot: HTTP ", statusCode);
}

//+------------------------------------------------------------------+
//|  PollAndExecuteCommand                                           |
//|  Polls the backend for the next pending command, validates it,   |
//|  submits a market order, and acknowledges the result.            |
//+------------------------------------------------------------------+
void PollAndExecuteCommand()
{
    string responseBody;
    int statusCode = HttpGet(BackendBaseUrl + "/api/mt5/commands/next", responseBody);

    if(statusCode == -1)
    {
        Print("[BoykaEA] PollAndExecuteCommand: network error or URL not in allowed list.");
        return;
    }

    if(statusCode == 204)
        return; // No pending command — normal empty-queue response

    if(statusCode != 200)
    {
        Print("[BoykaEA] PollAndExecuteCommand: unexpected HTTP ", statusCode, " — ", responseBody);
        return;
    }

    //--- Parse required fields from JSON response
    string commandId  = JsonExtractString(responseBody, "commandId");
    string accountId  = JsonExtractString(responseBody, "accountId");
    string symbol     = JsonExtractString(responseBody, "symbol");
    int    direction  = (int)JsonExtractDouble(responseBody, "direction");
    double volume     = JsonExtractDouble(responseBody, "volume");
    double entryPrice = JsonExtractDouble(responseBody, "entryPrice");
    double stopLoss   = JsonExtractDouble(responseBody, "stopLoss");
    double takeProfit = JsonExtractDouble(responseBody, "takeProfit");
    string expiresAt  = JsonExtractString(responseBody, "expiresAtUtc");

    if(StringLen(commandId) == 0 || StringLen(symbol) == 0)
    {
        Print("[BoykaEA] PollAndExecuteCommand: could not parse commandId or symbol from response.");
        return;
    }

    Print("[BoykaEA] Command received: ", commandId,
          " symbol=", symbol, " dir=", direction, " vol=", volume);

    //--- Check expiry
    datetime expiresDt = ParseIso8601(expiresAt);
    if(expiresDt > 0 && TimeGMT() > expiresDt)
    {
        Print("[BoykaEA] Command ", commandId, " has expired. Acknowledging as UNKNOWN.");
        MqlTradeResult dummyResult = {};
        AcknowledgeCommand(commandId, accountId, symbol, direction, volume, "UNKNOWN",
                           0, 0.0, 0.0, 0,
                           "Command expired before execution attempt.");
        return;
    }

    //--- Execution safety switch
    if(!ExecutionEnabled)
    {
        Print("[BoykaEA] ExecutionEnabled=false. Acknowledging command ", commandId, " as REJECTED.");
        AcknowledgeCommand(commandId, accountId, symbol, direction, volume, "REJECTED",
                           0, 0.0, 0.0, 0,
                           "Execution disabled on this EA installation.");
        return;
    }

    //--- Make sure the symbol is in Market Watch
    if(!SymbolSelect(symbol, true))
    {
        Print("[BoykaEA] Symbol ", symbol, " not available. Error: ", GetLastError());
        AcknowledgeCommand(commandId, accountId, symbol, direction, volume, "REJECTED",
                           0, 0.0, 0.0, 0,
                           "Symbol not available in terminal: " + symbol);
        return;
    }

    //--- Read and validate broker volume constraints
    double volMin  = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN);
    double volMax  = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX);
    double volStep = SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP);

    if(volStep <= 0.0) volStep = 0.01; // Fallback guard

    // Round volume to the nearest valid step
    double adjVolume = MathFloor(volume / volStep) * volStep;
    if(adjVolume < volMin) adjVolume = volMin;
    if(adjVolume > volMax) adjVolume = volMax;

    Print("[BoykaEA] Volume adjusted: requested=", volume, " adjusted=", adjVolume,
          " (min=", volMin, " max=", volMax, " step=", volStep, ")");

    //--- Determine order type and execution price
    ENUM_ORDER_TYPE orderType;
    double execPrice;

    if(direction == 1) // Buy
    {
        orderType = ORDER_TYPE_BUY;
        execPrice = SymbolInfoDouble(symbol, SYMBOL_ASK);
    }
    else // Sell
    {
        orderType = ORDER_TYPE_SELL;
        execPrice = SymbolInfoDouble(symbol, SYMBOL_BID);
    }

    //--- Build trade request
    MqlTradeRequest request = {};
    MqlTradeResult  result  = {};

    request.action       = TRADE_ACTION_DEAL;
    request.symbol       = symbol;
    request.volume       = adjVolume;
    request.type         = orderType;
    request.price        = execPrice;
    request.sl           = stopLoss;
    request.tp           = takeProfit;
    request.deviation    = SlippagePoints;
    request.magic        = MagicNumber;
    request.type_filling = ORDER_FILLING_FOK;

    // Build the comment — MT5 limits order comments to 31 characters
    string fullComment = "BoykaAI:" + commandId;
    if(StringLen(fullComment) > 31)
        fullComment = StringSubstr(fullComment, 0, 31);
    request.comment = fullComment;

    //--- Submit the order
    if(!OrderSend(request, result))
    {
        int lastErr = GetLastError();
        Print("[BoykaEA] OrderSend failed. retcode=", result.retcode,
              " GetLastError=", lastErr, " comment=", result.comment);

        // Try IOC filling if FOK was rejected due to filling policy
        if(result.retcode == 10030) // TRADE_RETCODE_INVALID_FILL
        {
            Print("[BoykaEA] Retrying with ORDER_FILLING_IOC.");
            request.type_filling = ORDER_FILLING_IOC;
            if(!OrderSend(request, result))
            {
                Print("[BoykaEA] IOC retry also failed. retcode=", result.retcode);
            }
        }
    }

    //--- Determine status string from retcode
    string status = RetcodeToStatus(result.retcode);
    string errMsg = (status != "FILLED") ? result.comment : "";

    Print("[BoykaEA] OrderSend result: retcode=", result.retcode,
          " status=", status, " ticket=", result.order,
          " price=", result.price, " volume=", result.volume);

    AcknowledgeCommand(commandId, accountId, symbol, direction, adjVolume, status,
                       (long)result.order, result.price, result.volume,
                       (int)result.retcode, errMsg);
}

//+------------------------------------------------------------------+
//|  AcknowledgeCommand                                              |
//|  Posts execution result back to the backend for the given        |
//|  commandId.                                                      |
//+------------------------------------------------------------------+
void AcknowledgeCommand(
    string commandId,
    string accountId,
    string symbol,
    int    direction,
    double requestedVolume,
    string status,
    long   brokerTicket,
    double executionPrice,
    double executedVolume,
    int    brokerRetcode,
    string errorMessage)
{
    string dirStr = (direction == 1) ? "BUY" : "SELL";
    string errField = (StringLen(errorMessage) > 0)
        ? ("\"" + EscapeJson(errorMessage) + "\"")
        : "null";

    string json = StringFormat(
        "{\"accountId\":\"%s\","
        "\"status\":\"%s\","
        "\"brokerTicket\":%d,"
        "\"symbol\":\"%s\","
        "\"direction\":\"%s\","
        "\"requestedVolume\":%.8f,"
        "\"executedVolume\":%.8f,"
        "\"executionPrice\":%.8f,"
        "\"brokerRetcode\":%d,"
        "\"errorMessage\":%s,"
        "\"executedAtUtc\":\"%s\"}",
        EscapeJson(accountId),
        EscapeJson(status),
        brokerTicket,
        EscapeJson(symbol),
        dirStr,
        requestedVolume,
        executedVolume,
        executionPrice,
        brokerRetcode,
        errField,
        ToIso8601Utc());

    string url = BackendBaseUrl + "/api/mt5/commands/" + commandId + "/acknowledge";
    string responseBody;
    int statusCode = HttpPost(url, json, responseBody);

    if(statusCode == -1)
        Print("[BoykaEA] AcknowledgeCommand: network error or URL not in allowed list.");
    else
        Print("[BoykaEA] AcknowledgeCommand: HTTP ", statusCode,
              " commandId=", commandId, " status=", status);
}

//+------------------------------------------------------------------+
//|  SendPositionSnapshot                                            |
//|  Sends a snapshot of all currently open positions to the backend.|
//+------------------------------------------------------------------+
void SendPositionSnapshot()
{
    int total = PositionsTotal();
    string posArray = "";

    for(int i = 0; i < total; i++)
    {
        // PositionGetTicket selects the indexed position for subsequent
        // PositionGet* calls and is supported by current MT5 builds.
        ulong ticket = PositionGetTicket(i);
        if(ticket == 0)
            continue;

        string posSym      = PositionGetString(POSITION_SYMBOL);
        long   posType     = PositionGetInteger(POSITION_TYPE);
        double posVol      = PositionGetDouble(POSITION_VOLUME);
        double openPrice   = PositionGetDouble(POSITION_PRICE_OPEN);
        double currentPrice= PositionGetDouble(POSITION_PRICE_CURRENT);
        double posSl       = PositionGetDouble(POSITION_SL);
        double posTp       = PositionGetDouble(POSITION_TP);
        double profit      = PositionGetDouble(POSITION_PROFIT);
        datetime openTime  = (datetime)PositionGetInteger(POSITION_TIME);

        string dirStr = (posType == POSITION_TYPE_BUY) ? "BUY" : "SELL";
        string openedAt = StructToIso8601(openTime);

        string posJson = StringFormat(
            "{\"brokerTicket\":%I64u,"
            "\"symbol\":\"%s\","
            "\"direction\":\"%s\","
            "\"volume\":%.8f,"
            "\"openPrice\":%.8f,"
            "\"currentPrice\":%.8f,"
            "\"stopLoss\":%.8f,"
            "\"takeProfit\":%.8f,"
            "\"profit\":%.8f,"
            "\"openedAtUtc\":\"%s\"}",
            ticket,
            EscapeJson(posSym),
            dirStr,
            posVol,
            openPrice,
            currentPrice,
            posSl,
            posTp,
            profit,
            openedAt);

        if(StringLen(posArray) > 0)
            posArray += ",";
        posArray += posJson;
    }

    string json = "{\"positions\":[" + posArray + "]}";

    string responseBody;
    int statusCode = HttpPost(BackendBaseUrl + "/api/mt5/positions/snapshot", json, responseBody);

    if(statusCode == -1)
        Print("[BoykaEA] SendPositionSnapshot: network error or URL not in allowed list.");
    else
        Print("[BoykaEA] SendPositionSnapshot: HTTP ", statusCode,
              " positions=", total);
}

//+------------------------------------------------------------------+
//|  HttpPost                                                        |
//|  Sends a POST request with JSON body and the EA API key header.  |
//|  Returns the HTTP status code, or -1 on a network/config error.  |
//+------------------------------------------------------------------+
int HttpPost(string url, string jsonBody, string &responseBody)
{
    string headers = "Content-Type: application/json\r\n"
                   + "X-EA-Api-Key: " + ApiKey + "\r\n";

    char   data[];
    char   result[];
    string resultHeaders;

    // Encode the JSON string to a UTF-8 byte array.
    // StringToCharArray appends a null terminator; subtract 1 to exclude it.
    int len = StringToCharArray(jsonBody, data, 0, WHOLE_ARRAY, CP_UTF8);
    if(len > 1)
        ArrayResize(data, len - 1);

    int statusCode = WebRequest("POST", url, headers, HTTP_TIMEOUT_MS,
                                data, result, resultHeaders);

    if(statusCode == -1)
    {
        int err = GetLastError();
        Print("[BoykaEA] HttpPost error: GetLastError=", err,
              " URL=", url,
              " — Ensure URL is in MT5 allowed WebRequest list.");
        responseBody = "";
        return -1;
    }

    responseBody = CharArrayToString(result, 0, WHOLE_ARRAY, CP_UTF8);
    return statusCode;
}

//+------------------------------------------------------------------+
//|  HttpGet                                                         |
//|  Sends a GET request with the EA API key header.                 |
//|  Returns the HTTP status code, or -1 on a network/config error.  |
//+------------------------------------------------------------------+
int HttpGet(string url, string &responseBody)
{
    string headers = "X-EA-Api-Key: " + ApiKey + "\r\n";

    char   data[];    // Empty body for GET
    char   result[];
    string resultHeaders;

    int statusCode = WebRequest("GET", url, headers, HTTP_TIMEOUT_MS,
                                data, result, resultHeaders);

    if(statusCode == -1)
    {
        int err = GetLastError();
        Print("[BoykaEA] HttpGet error: GetLastError=", err,
              " URL=", url,
              " — Ensure URL is in MT5 allowed WebRequest list.");
        responseBody = "";
        return -1;
    }

    responseBody = CharArrayToString(result, 0, WHOLE_ARRAY, CP_UTF8);
    return statusCode;
}

//+------------------------------------------------------------------+
//|  RetcodeToStatus                                                 |
//|  Maps an MT5 trade retcode to the backend acknowledge status.    |
//+------------------------------------------------------------------+
string RetcodeToStatus(uint retcode)
{
    switch(retcode)
    {
        case TRADE_RETCODE_DONE:         return "FILLED";
        case TRADE_RETCODE_DONE_PARTIAL: return "PARTIAL";
        case TRADE_RETCODE_REJECT:
        case TRADE_RETCODE_CANCEL:
        case TRADE_RETCODE_INVALID:      return "REJECTED";
        default:
            if(retcode == 0) return "UNKNOWN"; // OrderSend returned false before attempting
            return "FAILED";
    }
}

//+------------------------------------------------------------------+
//|  ToIso8601Utc                                                    |
//|  Returns the current UTC time as "YYYY-MM-DDTHH:MM:SSZ".        |
//+------------------------------------------------------------------+
string ToIso8601Utc()
{
    return StructToIso8601(TimeGMT());
}

//+------------------------------------------------------------------+
//|  StructToIso8601                                                 |
//|  Formats a datetime value as "YYYY-MM-DDTHH:MM:SSZ".            |
//+------------------------------------------------------------------+
string StructToIso8601(datetime dt)
{
    MqlDateTime s;
    TimeToStruct(dt, s);
    return StringFormat("%04d-%02d-%02dT%02d:%02d:%02dZ",
                        s.year, s.mon, s.day,
                        s.hour, s.min, s.sec);
}

//+------------------------------------------------------------------+
//|  ParseIso8601                                                    |
//|  Parses "YYYY-MM-DDTHH:MM:SSZ" into a datetime value.           |
//|  Returns 0 if the string cannot be parsed.                       |
//+------------------------------------------------------------------+
datetime ParseIso8601(string s)
{
    // Expected format: "2026-11-17T18:00:00Z"  (minimum 19 chars)
    if(StringLen(s) < 19)
        return 0;

    int year  = (int)StringToInteger(StringSubstr(s, 0, 4));
    int mon   = (int)StringToInteger(StringSubstr(s, 5, 2));
    int day   = (int)StringToInteger(StringSubstr(s, 8, 2));
    int hour  = (int)StringToInteger(StringSubstr(s, 11, 2));
    int min   = (int)StringToInteger(StringSubstr(s, 14, 2));
    int sec   = (int)StringToInteger(StringSubstr(s, 17, 2));

    if(year < 2000 || mon < 1 || mon > 12 || day < 1 || day > 31)
        return 0;

    MqlDateTime st;
    st.year  = year;
    st.mon   = mon;
    st.day   = day;
    st.hour  = hour;
    st.min   = min;
    st.sec   = sec;
    st.day_of_week = 0;
    st.day_of_year = 0;

    return StructToTime(st);
}

//+------------------------------------------------------------------+
//|  EscapeJson                                                      |
//|  Minimal JSON string escaping: replaces " with \" and \ with \\.|
//+------------------------------------------------------------------+
string EscapeJson(string s)
{
    // Replace backslash first to avoid double-escaping
    StringReplace(s, "\\", "\\\\");
    StringReplace(s, "\"", "\\\"");
    return s;
}

//+------------------------------------------------------------------+
//|  JsonExtractString                                               |
//|  Extracts the value of a named string field from a flat JSON     |
//|  object.  Returns "" if the key is not found.                    |
//|                                                                  |
//|  Simple approach: finds "key":"...", no nested object support.   |
//+------------------------------------------------------------------+
string JsonExtractString(string json, string key)
{
    string searchKey = "\"" + key + "\":\"";
    int pos = StringFind(json, searchKey);
    if(pos < 0) return "";

    int start = pos + StringLen(searchKey);
    int end   = StringFind(json, "\"", start);
    if(end < 0) return "";

    return StringSubstr(json, start, end - start);
}

//+------------------------------------------------------------------+
//|  JsonExtractDouble                                               |
//|  Extracts the numeric value of a named field from a flat JSON    |
//|  object.  Returns 0.0 if the key is not found.                  |
//+------------------------------------------------------------------+
double JsonExtractDouble(string json, string key)
{
    string searchKey = "\"" + key + "\":";
    int pos = StringFind(json, searchKey);
    if(pos < 0) return 0.0;

    int start = pos + StringLen(searchKey);

    // Skip whitespace
    while(start < StringLen(json))
    {
        ushort ch = StringGetCharacter(json, start);
        if(ch != ' ' && ch != '\t') break;
        start++;
    }

    // Read until a JSON delimiter is found
    string numStr = "";
    int i = start;
    while(i < StringLen(json))
    {
        ushort ch = StringGetCharacter(json, i);
        if(ch == ',' || ch == '}' || ch == ']' || ch == ' ' || ch == '\r' || ch == '\n')
            break;
        numStr += ShortToString(ch);
        i++;
    }

    return StringToDouble(numStr);
}
//+------------------------------------------------------------------+
