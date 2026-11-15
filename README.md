# StockPilot

A multi-warehouse inventory app built with ASP.NET Core MVC, EF Core and SQL Server. It tracks stock across locations, moves units between warehouses safely, and flags items that fall below their reorder level.

## Features
- Stock tracked per product and per warehouse
- Atomic transfers: both warehouses update together, or nothing changes
- Low-stock detection, calculated across all warehouses
- REST API with Swagger documentation
- MVC page that loads live low-stock data from the API
- Unit tests for the core business rules

## Tech stack
C#, .NET 10, ASP.NET Core MVC, Web API, Entity Framework Core, SQL Server (LocalDB), xUnit, Swagger, HTML/CSS/JavaScript, Git

## API
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/stock/low` | Items below their reorder level |
| POST | `/api/stock/transfer` | Move stock between warehouses (returns 400 if stock is insufficient) |

Example request:
```json
{ "productId": 1, "fromWarehouseId": 1, "toWarehouseId": 2, "quantity": 5 }
```

## Run it locally
1. Install the .NET 10 SDK and Visual Studio (ASP.NET and web development workload, which includes LocalDB).
2. Clone the repo and open `StockPilot.sln`.
3. Press F5. The database and sample data are created automatically on first run.
4. Open `/swagger` to try the API.

## Run the tests
Open Test Explorer in Visual Studio and choose Run All Tests, or run `dotnet test` in the solution folder.

## Design decisions
- Stock lives in its own table, because one product can sit in many warehouses.
- Business rules are in `StockService`, separate from the controllers, so they can be tested without HTTP.
- Transfers use a single `SaveChanges` call, so a failed transfer never leaves partial changes.

## Roadmap
- Per-warehouse view on the inventory page
- EF Core migrations instead of `EnsureCreated`
- Purchase orders and sales-channel order import
- GitHub Actions CI and Azure deployment

## Screenshots
[Add a screenshot of the inventory page and the Swagger page]