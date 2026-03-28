# Stock Closing Price Sync (.NET 10 Console)

此專案是一個 **.NET 10 Console** 批次程式，會：

1. 讀取資料表 `[dbo].[stock_info]` 的股票代碼清單。
2. 呼叫 TWSE OpenAPI：
   `https://openapi.twse.com.tw/v1/exchangeReport/STOCK_DAY_ALL`
3. 依代碼交集，取出 `Date`（民國年）與 `ClosingPrice`。
4. 轉換日期後，寫入 `[dbo].[stock_closing_price]`（Upsert）。

---

## 技術與套件

- .NET 10 Console
- Dapper
- SQL Server (`Microsoft.Data.SqlClient`)
- Generic Host / DI / HttpClient

---

## 重要設定

`appsettings.json`

```json
{
  "ConnectionStrings": {
    "SafeSchooling": "Encrypt=True;TrustServerCertificate=True;Database=SafeSchooling;Server=db.cyut.edu.tw;User ID=stockul;Password='s7dJr'"
  },
  "Twse": {
    "StockDayAllEndpoint": "https://openapi.twse.com.tw/v1/exchangeReport/STOCK_DAY_ALL"
  }
}
```

---

## 資料流程說明

- `stock_info.stockNo` -> 取得目標股票清單。
- API 回傳 `Date` 格式為民國年：
  - 例如 `1150326` => 西元 `2026-03-26`（`115 + 1911`）。
- 同步寫入欄位：
  - `stockNo`
  - `closedDate`
  - `price`
- 其餘欄位（`createBy/createAt/createIP/updateBy/updateAt/updateIP`）由程式補值。

---

## 寫入策略（Upsert）

使用 SQL `MERGE`：

- 若 `[stockNo]+[closedDate]` 已存在 -> 更新 `price/update*`
- 若不存在 -> 新增完整資料

符合 `PK_stock_closing_price(stockNo, closedDate)` 的唯一鍵限制。

---

## 本機執行

```bash
dotnet restore
dotnet build
dotnet run
```

---

## Kubernetes 每日 18:00 排程

已提供：

- `k8s/cronjob.yaml`
- `k8s/secret.sample.yaml`

重點：

- `schedule: "0 18 * * *"`
- `timeZone: "Asia/Taipei"`
- 使用環境變數覆蓋 .NET 設定（`__` 雙底線格式）

部署前請先替換：

- `image`（容器映像）
- `namespace`
- Secret 內容

---

## Docker 建置

```bash
docker build -t stock-closing-price-sync:latest .
```

