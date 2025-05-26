USE [candidates]
GO
CREATE PROCEDURE [dbo].[sp_EmployeeContractsReport]
    @StartDate DATE,
    @EndDate DATE,
    @TargetCurrency NVARCHAR(3),
    @ExchangeRateDate DATE
AS
BEGIN
    SET NOCOUNT ON;

   
    ;WITH
-- if exchange rate does not exist for selected date, take the next one
LatestExchangeRates AS (
        SELECT
            er.currency_from,
            er.currency_to,
            er.exchange_rate            
        FROM dbo.exchange_rates er
JOIN (SELECT m.currency_from, m.currency_to, MAX(m.exchange_rate_date) LastRateDate
 FROM dbo.exchange_rates m
 WHERE m.currency_to = @TargetCurrency
AND m.exchange_rate_date <= @ExchangeRateDate
 GROUP BY m.currency_from, m.currency_to) mx
ON er.currency_from = mx.currency_from
and er.currency_to = mx.currency_to
and er.exchange_rate_date = mx.LastRateDate

    ),
-- filtered contracts in selected period
    ContractsInPeriod AS (
        SELECT
            c.employee_id,
            c.contract_id,
            c.contract_value,
            c.currency_code,
            c.entered_date,
            c.duration,
            c.contract_value * ISNULL(er.exchange_rate, 1.0) AS converted_value
        FROM dbo.contracts c
        LEFT JOIN LatestExchangeRates er
            ON c.currency_code = er.currency_from
        WHERE c.entered_date BETWEEN @StartDate AND @EndDate
    ),
-- sumary data
AggregatedData AS (
        SELECT
            c.employee_id,
            COUNT(*) AS contract_count,
            SUM(c.converted_value) AS total_value,
            AVG(CAST(c.duration AS FLOAT)) AS average_duration,
            MAX(c.converted_value) AS max_value,
            MAX(c.entered_date) AS last_contract_date
        FROM ContractsInPeriod c
        GROUP BY c.employee_id
    )
-- final report
    SELECT
        e.employee_id AS [id zaposlenog],
        e.name AS [ime zaposlenog],
        CASE
            WHEN e.active_to IS NULL OR e.active_to > GETDATE() THEN 'DA'
            ELSE 'NE'
        END AS [da li je trenutno zaposlen u kompaniji],
        ISNULL(a.contract_count, 0) AS [broj sklopljenih ugovora],
        ISNULL(a.total_value, 0.00) AS [vrednost sklopljenih ugovora],
        @TargetCurrency AS [valuta koja se koristi],
        ISNULL(a.average_duration, 0.0) AS [prosecno trajanje ugovora],
        ISNULL(a.max_value, 0.00) AS [najvredniji sklopljen ugovor],
        a.last_contract_date AS [poslednji sklopljen ugovor]
    FROM dbo.employees e
    LEFT JOIN AggregatedData a ON e.employee_id = a.employee_id
    ORDER BY e.employee_id;
END